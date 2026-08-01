using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class FactoryCameraController : MonoBehaviour
    {
        private const string FactoryMapName = "Factory";

        [SerializeField] private UnityEngine.Camera controlledCamera;
        [SerializeField] private Transform focusTarget;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private float distance = 9f;
        [SerializeField] private float yaw = 35f;
        [SerializeField] private float pitch = 35f;
        [SerializeField] private float minimumDistance = 3.5f;
        [SerializeField] private float maximumDistance = 18f;
        [SerializeField] private float minimumPitch = 15f;
        [SerializeField] private float maximumPitch = 75f;
        [SerializeField] private float orbitSensitivity = 0.15f;
        [SerializeField] private float panSensitivity = 0.008f;
        [SerializeField] private float zoomSensitivity = 0.012f;
        [SerializeField] private float keyboardMoveSpeed = 4f;
        [SerializeField] private float factoryFloorHeight;

        private InputActionMap factoryMap;
        private InputAction orbitPress;
        private InputAction panPress;
        private InputAction pointerDelta;
        private InputAction zoomAction;
        private InputAction moveAction;
        private bool interactionLocked;
        private Vector3 fallbackFocus = new Vector3(0f, 1f, 0f);

        public float Distance => distance;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public Vector3 FocusPosition => focusTarget == null ? fallbackFocus : focusTarget.position;
        public bool InteractionLocked => interactionLocked;

        public void Configure(UnityEngine.Camera camera, Transform focus, InputActionAsset controls)
        {
            UnbindActions();
            controlledCamera = camera;
            focusTarget = focus;
            inputActions = controls;
            fallbackFocus = focus == null ? fallbackFocus : focus.position;
            BindActions();
            UpdateRig();
        }

        public void SetInteractionLocked(bool locked)
        {
            interactionLocked = locked;
        }

        public void ApplyZoom(float delta)
        {
            distance = Mathf.Clamp(distance - delta * zoomSensitivity,
                minimumDistance, maximumDistance);
            UpdateRig();
        }

        public void ApplyOrbit(Vector2 delta)
        {
            if (interactionLocked)
                return;

            yaw = Mathf.Repeat(yaw + delta.x * orbitSensitivity, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * orbitSensitivity,
                minimumPitch, maximumPitch);
            UpdateRig();
        }

        public void ApplyPan(Vector2 delta)
        {
            if (interactionLocked || controlledCamera == null)
                return;

            var right = controlledCamera.transform.right;
            var forward = Vector3.ProjectOnPlane(controlledCamera.transform.forward, Vector3.up).normalized;
            var offset = (-right * delta.x - forward * delta.y) * panSensitivity * distance;
            SetFocus(FocusPosition + offset);
        }

        public void ApplyMove(Vector2 input, float deltaTime)
        {
            if (interactionLocked || input.sqrMagnitude <= 0f)
                return;

            var heading = Quaternion.Euler(0f, yaw, 0f);
            var offset = heading * new Vector3(input.x, 0f, input.y);
            SetFocus(FocusPosition + offset * keyboardMoveSpeed * deltaTime);
        }

        public void SetView(Vector3 focus, float requestedDistance, float requestedYaw, float requestedPitch)
        {
            distance = Mathf.Clamp(requestedDistance, minimumDistance, maximumDistance);
            yaw = Mathf.Repeat(requestedYaw, 360f);
            pitch = Mathf.Clamp(requestedPitch, minimumPitch, maximumPitch);
            SetFocus(focus);
        }

        private void OnEnable()
        {
            BindActions();
            UpdateRig();
        }

        private void OnDisable()
        {
            UnbindActions();
        }

        private void LateUpdate()
        {
            if (factoryMap == null)
                BindActions();

            if (factoryMap != null)
            {
                var zoom = zoomAction.ReadValue<float>();
                if (Mathf.Abs(zoom) > 0.001f)
                    ApplyZoom(zoom);

                if (!interactionLocked)
                {
                    var delta = pointerDelta.ReadValue<Vector2>();
                    if (orbitPress.IsPressed())
                        ApplyOrbit(delta);
                    if (panPress.IsPressed())
                        ApplyPan(delta);
                    ApplyMove(moveAction.ReadValue<Vector2>(), Time.unscaledDeltaTime);
                }
            }

            UpdateRig();
        }

        private void BindActions()
        {
            if (!isActiveAndEnabled || inputActions == null || factoryMap != null)
                return;

            factoryMap = inputActions.FindActionMap(FactoryMapName, false);
            if (factoryMap == null)
                return;

            orbitPress = factoryMap.FindAction("OrbitPress", true);
            panPress = factoryMap.FindAction("PanPress", true);
            pointerDelta = factoryMap.FindAction("PointerDelta", true);
            zoomAction = factoryMap.FindAction("Zoom", true);
            moveAction = factoryMap.FindAction("Move", true);
            factoryMap.Enable();
        }

        private void UnbindActions()
        {
            if (factoryMap != null)
                factoryMap.Disable();
            factoryMap = null;
            orbitPress = null;
            panPress = null;
            pointerDelta = null;
            zoomAction = null;
            moveAction = null;
        }

        private void SetFocus(Vector3 position)
        {
            position.y = Mathf.Max(position.y, factoryFloorHeight + 0.05f);
            fallbackFocus = position;
            if (focusTarget != null)
                focusTarget.position = position;
            UpdateRig();
        }

        private void UpdateRig()
        {
            if (controlledCamera == null)
                return;

            distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            var focus = FocusPosition;
            focus.y = Mathf.Max(focus.y, factoryFloorHeight + 0.05f);
            var orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            var cameraPosition = focus - orbitRotation * Vector3.forward * distance;
            cameraPosition.y = Mathf.Max(cameraPosition.y, factoryFloorHeight + 0.1f);
            controlledCamera.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(focus - cameraPosition, Vector3.up));
        }
    }
}
