using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.ThirdPerson.Player
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.Camera controlledCamera;
        [SerializeField] private Transform target;
        [SerializeField] private ThirdPersonInputLock inputLock;

        [Header("Input")]
        [SerializeField] private InputActionReference lookActionReference;
        [SerializeField] private InputActionReference zoomActionReference;
        [SerializeField] private bool captureCursor = true;

        [Header("Orbit")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.55f, 0f);
        [SerializeField] private float yaw;
        [SerializeField] private float pitch = 22f;
        [SerializeField] private float distance = 4.5f;
        [SerializeField] private float minimumDistance = 2f;
        [SerializeField] private float maximumDistance = 8f;
        [SerializeField] private float minimumPitch = 8f;
        [SerializeField] private float maximumPitch = 70f;
        [SerializeField, Min(0f)] private float lookSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float zoomSensitivity = 0.01f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.08f;
        [SerializeField, Min(0.01f)] private float minimumCollisionDistance = 0.25f;

        private readonly InputActionSlot lookInput = new InputActionSlot();
        private readonly InputActionSlot zoomInput = new InputActionSlot();
        private readonly RaycastHit[] collisionHits = new RaycastHit[16];
        private float currentDistance;

        public Transform Target => target;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public float Distance => distance;
        public float CurrentDistance => currentDistance;
        public bool InputLocked => inputLock != null && inputLock.InputLocked;

        public void Configure(
            UnityEngine.Camera camera,
            Transform followTarget,
            ThirdPersonInputLock sharedInputLock)
        {
            controlledCamera = camera == null ? GetComponent<UnityEngine.Camera>() : camera;
            target = followTarget;
            SetInputLock(sharedInputLock);
            SnapNow();
        }

        public void ConfigureInput(
            InputActionReference requestedLookAction,
            InputActionReference requestedZoomAction)
        {
            ReleaseInputActions();
            lookActionReference = requestedLookAction;
            zoomActionReference = requestedZoomAction;
            if (isActiveAndEnabled)
                BindInputActions();
        }

        public void ConfigureLimits(
            float requestedMinimumDistance,
            float requestedMaximumDistance,
            float requestedMinimumPitch,
            float requestedMaximumPitch)
        {
            minimumDistance = Mathf.Max(0.01f, requestedMinimumDistance);
            maximumDistance = Mathf.Max(minimumDistance, requestedMaximumDistance);
            minimumPitch = Mathf.Clamp(requestedMinimumPitch, -89f, 89f);
            maximumPitch = Mathf.Clamp(requestedMaximumPitch, minimumPitch, 89f);
            ClampView();
            SnapNow();
        }

        public void ConfigureCollision(
            LayerMask layers,
            float requestedRadius,
            float requestedPadding,
            float requestedMinimumDistance = 0.25f)
        {
            collisionLayers = layers;
            collisionRadius = Mathf.Max(0.01f, requestedRadius);
            collisionPadding = Mathf.Max(0f, requestedPadding);
            minimumCollisionDistance = Mathf.Max(0.01f, requestedMinimumDistance);
            SnapNow();
        }

        public void SetPivotOffset(Vector3 requestedPivotOffset)
        {
            pivotOffset = requestedPivotOffset;
            SnapNow();
        }

        public void SetCursorCapture(bool shouldCapture)
        {
            captureCursor = shouldCapture;
            ApplyCursorState();
        }

        public void SetInputLocked(bool locked)
        {
            if (inputLock != null)
                inputLock.SetInputLocked(locked);
        }

        public void SetView(float requestedYaw, float requestedPitch, float requestedDistance)
        {
            yaw = Mathf.Repeat(requestedYaw, 360f);
            pitch = requestedPitch;
            distance = requestedDistance;
            ClampView();
            SnapNow();
        }

        public void ApplyLook(Vector2 pointerDelta)
        {
            if (InputLocked)
                return;

            yaw = Mathf.Repeat(yaw + pointerDelta.x * lookSensitivity, 360f);
            pitch -= pointerDelta.y * lookSensitivity;
            ClampView();
        }

        public void ApplyZoom(float scrollDelta)
        {
            if (InputLocked)
                return;

            distance -= scrollDelta * zoomSensitivity;
            ClampView();
        }

        public void SnapNow()
        {
            if (controlledCamera == null)
                controlledCamera = GetComponent<UnityEngine.Camera>();
            if (controlledCamera == null || target == null)
                return;

            ClampView();
            var pivot = target.position + pivotOffset;
            var orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            var directionFromPivot = -(orbitRotation * Vector3.forward).normalized;
            currentDistance = ResolveCollisionDistance(pivot, directionFromPivot, distance);
            var cameraPosition = pivot + directionFromPivot * currentDistance;
            controlledCamera.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(pivot - cameraPosition, Vector3.up));
        }

        private void Reset()
        {
            controlledCamera = GetComponent<UnityEngine.Camera>();
        }

        private void Awake()
        {
            if (controlledCamera == null)
                controlledCamera = GetComponent<UnityEngine.Camera>();
        }

        private void OnEnable()
        {
            BindInputActions();
            SubscribeToInputLock();
            ApplyCursorState();
            SnapNow();
        }

        private void OnDisable()
        {
            ReleaseInputActions();
            UnsubscribeFromInputLock();
            if (Application.isPlaying && captureCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnValidate()
        {
            minimumDistance = Mathf.Max(0.01f, minimumDistance);
            maximumDistance = Mathf.Max(minimumDistance, maximumDistance);
            minimumPitch = Mathf.Clamp(minimumPitch, -89f, 89f);
            maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 89f);
            lookSensitivity = Mathf.Max(0f, lookSensitivity);
            zoomSensitivity = Mathf.Max(0f, zoomSensitivity);
            collisionRadius = Mathf.Max(0.01f, collisionRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);
            minimumCollisionDistance = Mathf.Max(0.01f, minimumCollisionDistance);
            ClampView();
        }

        private void LateUpdate()
        {
            if (!InputLocked)
            {
                if (lookInput.Action != null)
                    ApplyLook(lookInput.Action.ReadValue<Vector2>());
                if (zoomInput.Action != null)
                    ApplyZoom(zoomInput.Action.ReadValue<float>());
            }

            SnapNow();
        }

        private void SetInputLock(ThirdPersonInputLock requestedInputLock)
        {
            UnsubscribeFromInputLock();
            inputLock = requestedInputLock;
            SubscribeToInputLock();
            ApplyCursorState();
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

        private void HandleInputLockChanged(bool _)
        {
            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            if (!Application.isPlaying)
                return;

            if (!captureCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Cursor.lockState = InputLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = InputLocked;
        }

        private void BindInputActions()
        {
            if (lookInput.Action != null)
                return;

            lookInput.Bind(lookActionReference, CreateDefaultLookAction);
            zoomInput.Bind(zoomActionReference, CreateDefaultZoomAction);
        }

        private void ReleaseInputActions()
        {
            lookInput.Release();
            zoomInput.Release();
        }

        private void ClampView()
        {
            distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
        }

        private float ResolveCollisionDistance(
            Vector3 pivot,
            Vector3 directionFromPivot,
            float desiredDistance)
        {
            var hitCount = Physics.SphereCastNonAlloc(
                pivot,
                collisionRadius,
                directionFromPivot,
                collisionHits,
                desiredDistance,
                collisionLayers,
                QueryTriggerInteraction.Ignore);
            var resolvedDistance = desiredDistance;
            for (var index = 0; index < hitCount; index++)
            {
                var hit = collisionHits[index];
                if (hit.collider == null || IsTargetCollider(hit.collider.transform))
                    continue;

                resolvedDistance = Mathf.Min(
                    resolvedDistance,
                    Mathf.Max(minimumCollisionDistance, hit.distance - collisionPadding));
            }

            return resolvedDistance;
        }

        private bool IsTargetCollider(Transform colliderTransform)
        {
            if (target == null || colliderTransform == null)
                return false;

            return colliderTransform == target ||
                colliderTransform.IsChildOf(target) ||
                target.IsChildOf(colliderTransform);
        }

        private static InputAction CreateDefaultLookAction()
        {
            return new InputAction(
                "ThirdPersonLook",
                InputActionType.Value,
                "<Pointer>/delta",
                expectedControlType: "Vector2");
        }

        private static InputAction CreateDefaultZoomAction()
        {
            return new InputAction(
                "ThirdPersonZoom",
                InputActionType.Value,
                "<Mouse>/scroll/y",
                expectedControlType: "Axis");
        }
    }
}
