using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.ThirdPerson.Player
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionScanner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private Transform interactionOrigin;
        [SerializeField] private ThirdPersonInputLock inputLock;

        [Header("Input")]
        [SerializeField] private InputActionReference interactActionReference;

        [Header("Scan")]
        [SerializeField, Min(0.01f)] private float scanRadius = 2.5f;
        [SerializeField, Range(0f, 180f)] private float maximumViewAngle = 80f;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField, Min(0.01f)] private float scanInterval = 0.08f;

        private readonly InputActionSlot interactInput = new InputActionSlot();
        private readonly Collider[] overlapResults = new Collider[128];
        private readonly RaycastHit[] lineOfSightHits = new RaycastHit[64];
        private readonly Dictionary<IPlayerInteractable, float> candidateScores =
            new Dictionary<IPlayerInteractable, float>();
        private float nextScanTime;
        private IPlayerInteractable currentTarget;

        public IPlayerInteractable CurrentTarget => IsInteractableAlive(currentTarget)
            ? currentTarget
            : null;
        public string CurrentPrompt => CurrentTarget == null
            ? string.Empty
            : CurrentTarget.InteractionPrompt;
        public bool InputLocked => inputLock != null && inputLock.InputLocked;

        public event Action<IPlayerInteractable> TargetChanged;

        public void Configure(
            Transform origin,
            ThirdPersonInputLock sharedInputLock)
        {
            UnsubscribeFromInputLock();
            interactionOrigin = origin == null ? transform : origin;
            inputLock = sharedInputLock;
            if (playerObject == null)
                playerObject = gameObject;
            SubscribeToInputLock();
            ScanNow();
        }

        public void ConfigurePlayer(GameObject player)
        {
            playerObject = player == null ? gameObject : player;
        }

        public void ConfigureInput(InputActionReference requestedInteractAction)
        {
            interactInput.Release();
            interactActionReference = requestedInteractAction;
            if (isActiveAndEnabled)
                BindInputAction();
        }

        /// <param name="maxViewAngle">
        /// Maximum angular deviation in degrees between the origin's forward direction and a target.
        /// Use 180 to scan in every direction.
        /// </param>
        public void ConfigureScan(float radius, float maxViewAngle, LayerMask layers)
        {
            scanRadius = Mathf.Max(0.01f, radius);
            maximumViewAngle = Mathf.Clamp(maxViewAngle, 0f, 180f);
            interactionLayers = layers;
            ScanNow();
        }

        public void SetInputLocked(bool locked)
        {
            if (inputLock != null)
                inputLock.SetInputLocked(locked);
        }

        public void ScanNow()
        {
            if (InputLocked)
            {
                SetCurrentTarget(null);
                return;
            }

            if (interactionOrigin == null)
                interactionOrigin = transform;
            if (playerObject == null)
                playerObject = gameObject;

            Physics.SyncTransforms();
            candidateScores.Clear();
            var originPosition = interactionOrigin.position;
            var context = new InteractionContext(playerObject);
            var resultCount = Physics.OverlapSphereNonAlloc(
                originPosition,
                scanRadius,
                overlapResults,
                interactionLayers,
                QueryTriggerInteraction.Collide);

            for (var index = 0; index < resultCount; index++)
            {
                var collider = overlapResults[index];
                var candidate = FindInteractable(collider);
                if (candidate == null || !candidate.CanInteract(context))
                    continue;

                var targetPoint = collider.ClosestPoint(originPosition);
                var offset = targetPoint - originPosition;
                if (offset.sqrMagnitude < 0.0001f && candidate is Component component)
                    offset = component.transform.position - originPosition;

                var angle = offset.sqrMagnitude < 0.0001f
                    ? 0f
                    : Vector3.Angle(interactionOrigin.forward, offset);
                if (angle > maximumViewAngle)
                    continue;

                if (!HasClearLineOfSight(originPosition, offset, candidate))
                    continue;

                var distance = Mathf.Sqrt(offset.sqrMagnitude);
                var normalizedAngle = maximumViewAngle <= 0.001f
                    ? 0f
                    : angle / maximumViewAngle;
                var score = distance + normalizedAngle * scanRadius * 0.5f;
                if (!candidateScores.TryGetValue(candidate, out var existingScore) || score < existingScore)
                    candidateScores[candidate] = score;
            }

            var bestTarget = default(IPlayerInteractable);
            var bestScore = float.PositiveInfinity;
            foreach (var candidate in candidateScores)
            {
                if (candidate.Value >= bestScore)
                    continue;

                bestTarget = candidate.Key;
                bestScore = candidate.Value;
            }

            SetCurrentTarget(bestTarget);
        }

        private bool HasClearLineOfSight(
            Vector3 originPosition,
            Vector3 offset,
            IPlayerInteractable candidate)
        {
            var distance = offset.magnitude;
            if (distance <= 0.001f)
                return true;

            var hitCount = Physics.RaycastNonAlloc(
                originPosition,
                offset / distance,
                lineOfSightHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);
            for (var index = 0; index < hitCount; index++)
            {
                var hitCollider = lineOfSightHits[index].collider;
                if (hitCollider == null || IsPlayerCollider(hitCollider.transform))
                    continue;

                var hitInteractable = FindInteractable(hitCollider);
                if (ReferenceEquals(hitInteractable, candidate))
                    continue;

                return false;
            }

            return true;
        }

        private bool IsPlayerCollider(Transform colliderTransform)
        {
            if (playerObject == null || colliderTransform == null)
                return false;

            var playerTransform = playerObject.transform;
            return colliderTransform == playerTransform ||
                colliderTransform.IsChildOf(playerTransform);
        }

        public bool TryInteract()
        {
            var targetToUse = CurrentTarget;
            if (InputLocked || targetToUse == null)
                return false;

            if (playerObject == null)
                playerObject = gameObject;
            var context = new InteractionContext(playerObject);
            if (!targetToUse.CanInteract(context))
            {
                ScanNow();
                return false;
            }

            targetToUse.Interact(context);
            ScanNow();
            return true;
        }

        private void Reset()
        {
            playerObject = gameObject;
            interactionOrigin = transform;
            inputLock = GetComponent<ThirdPersonInputLock>();
        }

        private void Awake()
        {
            if (playerObject == null)
                playerObject = gameObject;
            if (interactionOrigin == null)
                interactionOrigin = transform;
        }

        private void OnEnable()
        {
            BindInputAction();
            SubscribeToInputLock();
            nextScanTime = 0f;
        }

        private void OnDisable()
        {
            interactInput.Release();
            UnsubscribeFromInputLock();
            SetCurrentTarget(null);
        }

        private void OnValidate()
        {
            scanRadius = Mathf.Max(0.01f, scanRadius);
            maximumViewAngle = Mathf.Clamp(maximumViewAngle, 0f, 180f);
            scanInterval = Mathf.Max(0.01f, scanInterval);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextScanTime)
            {
                nextScanTime = Time.unscaledTime + scanInterval;
                ScanNow();
            }

            if (!InputLocked &&
                interactInput.Action != null &&
                interactInput.Action.WasPressedThisFrame())
            {
                TryInteract();
            }
        }

        private void BindInputAction()
        {
            if (interactInput.Action != null)
                return;

            interactInput.Bind(interactActionReference, CreateDefaultInteractAction);
        }

        private void SubscribeToInputLock()
        {
            if (isActiveAndEnabled && inputLock != null)
                inputLock.InputLockChanged += HandleInputLockChanged;
        }

        private void UnsubscribeFromInputLock()
        {
            if (inputLock != null)
                inputLock.InputLockChanged -= HandleInputLockChanged;
        }

        private void HandleInputLockChanged(bool locked)
        {
            if (locked)
                SetCurrentTarget(null);
            else
                ScanNow();
        }

        private void SetCurrentTarget(IPlayerInteractable target)
        {
            if (ReferenceEquals(currentTarget, target))
                return;

            currentTarget = target;
            TargetChanged?.Invoke(CurrentTarget);
        }

        private static IPlayerInteractable FindInteractable(Collider collider)
        {
            if (collider == null)
                return null;

            var behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPlayerInteractable interactable)
                    return interactable;
            }

            return null;
        }

        private static bool IsInteractableAlive(IPlayerInteractable interactable)
        {
            if (interactable == null)
                return false;
            return !(interactable is UnityEngine.Object unityObject) || unityObject != null;
        }

        private static InputAction CreateDefaultInteractAction()
        {
            var action = new InputAction(
                "ThirdPersonInteract",
                InputActionType.Button,
                "<Keyboard>/e");
            action.AddBinding("<Gamepad>/buttonWest");
            return action;
        }
    }
}
