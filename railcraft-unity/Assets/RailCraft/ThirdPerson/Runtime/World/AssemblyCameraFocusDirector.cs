using System;
using System.Collections.Generic;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    public enum AssemblyCameraFocusState
    {
        Idle,
        Focusing,
        Holding,
        Returning
    }

    [Serializable]
    public sealed class AssemblyFocusBinding
    {
        [SerializeField] private ModuleId moduleId;
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Transform cameraPose;
        [SerializeField, Min(0.1f)] private float fallbackDistance = 5f;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 0.8f, 0f);

        public AssemblyFocusBinding()
        {
        }

        public AssemblyFocusBinding(
            ModuleId configuredModuleId,
            Transform configuredFocusTarget,
            Transform configuredCameraPose = null,
            float configuredFallbackDistance = 5f,
            Vector3? configuredFocusOffset = null)
        {
            moduleId = configuredModuleId;
            focusTarget = configuredFocusTarget;
            cameraPose = configuredCameraPose;
            fallbackDistance = Mathf.Max(0.1f, configuredFallbackDistance);
            focusOffset = configuredFocusOffset ?? new Vector3(0f, 0.8f, 0f);
        }

        public ModuleId ModuleId => moduleId;
        public Transform FocusTarget => focusTarget;
        public Transform CameraPose => cameraPose;
        public float FallbackDistance => Mathf.Max(0.1f, fallbackDistance);
        public Vector3 FocusOffset => focusOffset;
    }

    /// <summary>
    /// Plays a short camera cue when a configured module first becomes complete, then
    /// restores the player's previous orbit angles and distance. It runs after the
    /// normal orbit camera so the existing camera controller remains untouched.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class AssemblyCameraFocusDirector : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private UnityEngine.Camera controlledCamera;
        [SerializeField] private ThirdPersonOrbitCamera orbitCamera;
        [SerializeField] private AssemblyFocusBinding[] focusBindings =
            Array.Empty<AssemblyFocusBinding>();

        [Header("Timing")]
        [SerializeField, Min(0f)] private float focusDuration = 0.45f;
        [SerializeField, Min(0f)] private float holdDuration = 0.9f;
        [SerializeField, Min(0f)] private float returnDuration = 0.5f;

        private readonly Dictionary<ModuleId, bool> knownModuleStates =
            new Dictionary<ModuleId, bool>();
        private bool subscribed;
        private bool hasSavedView;
        private float stateElapsed;
        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private float savedYaw;
        private float savedPitch;
        private float savedDistance;
        private Vector3 focusStartPosition;
        private Quaternion focusStartRotation;
        private Vector3 focusPosition;
        private Quaternion focusRotation;
        private Vector3 returnStartPosition;
        private Quaternion returnStartRotation;
        private Transform currentTarget;

        public AssemblyCameraFocusState State { get; private set; } =
            AssemblyCameraFocusState.Idle;
        public bool IsActive => State != AssemblyCameraFocusState.Idle;
        public Transform CurrentTarget => currentTarget;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            UnityEngine.Camera configuredCamera,
            ThirdPersonOrbitCamera configuredOrbitCamera,
            IEnumerable<AssemblyFocusBinding> configuredBindings)
        {
            CancelAndRestore();
            Unsubscribe();

            sessionHost = configuredSessionHost;
            orbitCamera = configuredOrbitCamera;
            controlledCamera = configuredCamera != null
                ? configuredCamera
                : orbitCamera == null
                    ? GetComponent<UnityEngine.Camera>()
                    : orbitCamera.GetComponent<UnityEngine.Camera>();
            focusBindings = CopyBindings(configuredBindings);

            CaptureKnownModuleStates();
            Subscribe();
        }

        public void ConfigureTimings(
            float configuredFocusDuration,
            float configuredHoldDuration,
            float configuredReturnDuration)
        {
            focusDuration = Mathf.Max(0f, configuredFocusDuration);
            holdDuration = Mathf.Max(0f, configuredHoldDuration);
            returnDuration = Mathf.Max(0f, configuredReturnDuration);
        }

        /// <summary>
        /// Starts a cue without a module event. When cameraPose is null, a useful view
        /// is derived from the current camera-to-target direction.
        /// </summary>
        public bool PlayFocus(
            Transform focusTarget,
            Transform cameraPose = null,
            float fallbackDistance = 5f,
            Vector3? focusOffset = null)
        {
            if (focusTarget == null)
                return false;

            ResolveCameraReference();
            if (controlledCamera == null)
                return false;

            if (IsActive)
                CancelAndRestore();

            SaveCurrentView();
            currentTarget = focusTarget;
            focusStartPosition = controlledCamera.transform.position;
            focusStartRotation = controlledCamera.transform.rotation;
            ResolveFocusPose(
                focusTarget,
                cameraPose,
                Mathf.Max(0.1f, fallbackDistance),
                focusOffset ?? new Vector3(0f, 0.8f, 0f),
                out focusPosition,
                out focusRotation);

            State = AssemblyCameraFocusState.Focusing;
            stateElapsed = 0f;
            ApplyFocusProgress(0f);
            Advance(0f);
            return true;
        }

        public void CancelAndRestore()
        {
            if (hasSavedView)
                RestoreSavedView();

            hasSavedView = false;
            currentTarget = null;
            stateElapsed = 0f;
            State = AssemblyCameraFocusState.Idle;
        }

        /// <summary>
        /// Deterministic state-machine step used by LateUpdate and EditMode tests.
        /// </summary>
        public void Advance(float unscaledDeltaTime)
        {
            if (!IsActive)
                return;

            var remaining = Mathf.Max(0f, unscaledDeltaTime);
            for (var safety = 0; safety < 8 && IsActive; safety++)
            {
                var duration = GetCurrentStateDuration();
                if (duration <= 0f)
                {
                    CompleteCurrentState();
                    continue;
                }

                var timeAvailable = Mathf.Max(0f, duration - stateElapsed);
                var step = Mathf.Min(remaining, timeAvailable);
                stateElapsed += step;
                remaining -= step;
                ApplyCurrentState(stateElapsed / duration);

                if (stateElapsed + 0.00001f < duration)
                    break;

                CompleteCurrentState();
                if (remaining <= 0f)
                    break;
            }
        }

        private void Awake()
        {
            ResolveCameraReference();
        }

        private void OnEnable()
        {
            CaptureKnownModuleStates();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            CancelAndRestore();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            CancelAndRestore();
        }

        private void OnValidate()
        {
            focusDuration = Mathf.Max(0f, focusDuration);
            holdDuration = Mathf.Max(0f, holdDuration);
            returnDuration = Mathf.Max(0f, returnDuration);
        }

        private void LateUpdate()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void ResolveCameraReference()
        {
            if (controlledCamera != null)
                return;

            if (orbitCamera != null)
                controlledCamera = orbitCamera.GetComponent<UnityEngine.Camera>();
            if (controlledCamera == null)
                controlledCamera = GetComponent<UnityEngine.Camera>();
        }

        private void SaveCurrentView()
        {
            savedCameraPosition = controlledCamera.transform.position;
            savedCameraRotation = controlledCamera.transform.rotation;
            if (orbitCamera != null)
            {
                savedYaw = orbitCamera.Yaw;
                savedPitch = orbitCamera.Pitch;
                savedDistance = orbitCamera.Distance;
            }

            hasSavedView = true;
        }

        private void RestoreSavedView()
        {
            if (orbitCamera != null && orbitCamera.Target != null)
            {
                orbitCamera.SetView(savedYaw, savedPitch, savedDistance);
                orbitCamera.SnapNow();
                return;
            }

            if (controlledCamera != null)
            {
                controlledCamera.transform.SetPositionAndRotation(
                    savedCameraPosition,
                    savedCameraRotation);
            }
        }

        private void ResolveFocusPose(
            Transform focusTarget,
            Transform cameraPose,
            float fallbackDistance,
            Vector3 focusOffset,
            out Vector3 position,
            out Quaternion rotation)
        {
            if (cameraPose != null)
            {
                position = cameraPose.position;
                rotation = cameraPose.rotation;
                return;
            }

            var lookPoint = focusTarget.position + focusOffset;
            var directionFromTarget = controlledCamera.transform.position - lookPoint;
            if (directionFromTarget.sqrMagnitude < 0.0001f)
                directionFromTarget = new Vector3(1f, 0.55f, -1f);
            directionFromTarget.Normalize();

            position = lookPoint + directionFromTarget * fallbackDistance;
            var lookDirection = lookPoint - position;
            rotation = lookDirection.sqrMagnitude < 0.0001f
                ? controlledCamera.transform.rotation
                : Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        private float GetCurrentStateDuration()
        {
            switch (State)
            {
                case AssemblyCameraFocusState.Focusing:
                    return focusDuration;
                case AssemblyCameraFocusState.Holding:
                    return holdDuration;
                case AssemblyCameraFocusState.Returning:
                    return returnDuration;
                default:
                    return 0f;
            }
        }

        private void ApplyCurrentState(float progress)
        {
            switch (State)
            {
                case AssemblyCameraFocusState.Focusing:
                    ApplyFocusProgress(progress);
                    break;
                case AssemblyCameraFocusState.Holding:
                    SetCameraPose(focusPosition, focusRotation);
                    break;
                case AssemblyCameraFocusState.Returning:
                    ApplyReturnProgress(progress);
                    break;
            }
        }

        private void ApplyFocusProgress(float progress)
        {
            var eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            SetCameraPose(
                Vector3.LerpUnclamped(focusStartPosition, focusPosition, eased),
                Quaternion.SlerpUnclamped(focusStartRotation, focusRotation, eased));
        }

        private void ApplyReturnProgress(float progress)
        {
            var eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            SetCameraPose(
                Vector3.LerpUnclamped(returnStartPosition, savedCameraPosition, eased),
                Quaternion.SlerpUnclamped(returnStartRotation, savedCameraRotation, eased));
        }

        private void SetCameraPose(Vector3 position, Quaternion rotation)
        {
            if (controlledCamera != null)
                controlledCamera.transform.SetPositionAndRotation(position, rotation);
        }

        private void CompleteCurrentState()
        {
            stateElapsed = 0f;
            switch (State)
            {
                case AssemblyCameraFocusState.Focusing:
                    SetCameraPose(focusPosition, focusRotation);
                    State = AssemblyCameraFocusState.Holding;
                    break;
                case AssemblyCameraFocusState.Holding:
                    returnStartPosition = controlledCamera == null
                        ? focusPosition
                        : controlledCamera.transform.position;
                    returnStartRotation = controlledCamera == null
                        ? focusRotation
                        : controlledCamera.transform.rotation;
                    State = AssemblyCameraFocusState.Returning;
                    break;
                case AssemblyCameraFocusState.Returning:
                    RestoreSavedView();
                    hasSavedView = false;
                    currentTarget = null;
                    State = AssemblyCameraFocusState.Idle;
                    break;
            }
        }

        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled || sessionHost == null)
                return;

            sessionHost.StateChanged += HandleSessionStateChanged;
            sessionHost.SessionReset += HandleSessionReset;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (sessionHost != null)
            {
                sessionHost.StateChanged -= HandleSessionStateChanged;
                sessionHost.SessionReset -= HandleSessionReset;
            }
            subscribed = false;
        }

        private void HandleSessionStateChanged()
        {
            if (sessionHost == null)
                return;

            AssemblyFocusBinding cueToPlay = null;
            for (var index = 0; index < focusBindings.Length; index++)
            {
                var binding = focusBindings[index];
                if (binding == null)
                    continue;

                var moduleId = binding.ModuleId;
                var isComplete = sessionHost.Session.IsModuleComplete(moduleId);
                var wasComplete = knownModuleStates.TryGetValue(moduleId, out var knownComplete) &&
                    knownComplete;
                knownModuleStates[moduleId] = isComplete;
                if (cueToPlay == null && !wasComplete && isComplete && binding.FocusTarget != null)
                    cueToPlay = binding;
            }

            if (cueToPlay != null)
            {
                PlayFocus(
                    cueToPlay.FocusTarget,
                    cueToPlay.CameraPose,
                    cueToPlay.FallbackDistance,
                    cueToPlay.FocusOffset);
            }
        }

        private void HandleSessionReset()
        {
            CancelAndRestore();
            CaptureKnownModuleStates();
        }

        private void CaptureKnownModuleStates()
        {
            knownModuleStates.Clear();
            if (sessionHost == null || focusBindings == null)
                return;

            for (var index = 0; index < focusBindings.Length; index++)
            {
                var binding = focusBindings[index];
                if (binding != null)
                {
                    knownModuleStates[binding.ModuleId] =
                        sessionHost.Session.IsModuleComplete(binding.ModuleId);
                }
            }
        }

        private static AssemblyFocusBinding[] CopyBindings(
            IEnumerable<AssemblyFocusBinding> bindings)
        {
            if (bindings == null)
                return Array.Empty<AssemblyFocusBinding>();

            var copy = new List<AssemblyFocusBinding>();
            foreach (var binding in bindings)
            {
                if (binding != null)
                    copy.Add(binding);
            }

            return copy.ToArray();
        }
    }
}
