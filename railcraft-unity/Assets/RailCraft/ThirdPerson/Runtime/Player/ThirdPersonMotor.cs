using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.ThirdPerson.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform movementCamera;
        [SerializeField] private ThirdPersonInputLock inputLock;

        [Header("Input")]
        [SerializeField] private InputActionReference moveActionReference;
        [SerializeField] private InputActionReference sprintActionReference;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7.5f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedVelocity = -2f;

        private readonly InputActionSlot moveInput = new InputActionSlot();
        private readonly InputActionSlot sprintInput = new InputActionSlot();
        private float verticalVelocity;

        public bool InputLocked => inputLock != null && inputLock.InputLocked;
        public float VerticalVelocity => verticalVelocity;
        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;

        public void Configure(
            CharacterController controller,
            Transform cameraTransform,
            ThirdPersonInputLock sharedInputLock)
        {
            characterController = controller == null ? GetComponent<CharacterController>() : controller;
            movementCamera = cameraTransform;
            inputLock = sharedInputLock;
        }

        public void ConfigureMovement(
            float requestedWalkSpeed,
            float requestedSprintSpeed,
            float requestedTurnSpeed,
            float requestedGravity,
            float requestedGroundedVelocity = -2f)
        {
            walkSpeed = Mathf.Max(0f, requestedWalkSpeed);
            sprintSpeed = Mathf.Max(walkSpeed, requestedSprintSpeed);
            turnSpeed = Mathf.Max(0f, requestedTurnSpeed);
            gravity = Mathf.Min(0f, requestedGravity);
            groundedVelocity = Mathf.Min(0f, requestedGroundedVelocity);
        }

        public void ConfigureInput(
            InputActionReference requestedMoveAction,
            InputActionReference requestedSprintAction)
        {
            ReleaseInputActions();
            moveActionReference = requestedMoveAction;
            sprintActionReference = requestedSprintAction;
            if (isActiveAndEnabled)
                BindInputActions();
        }

        public void SetInputLocked(bool locked)
        {
            if (inputLock != null)
                inputLock.SetInputLocked(locked);
        }

        public Vector3 CalculatePlanarVelocity(Vector2 input, bool sprinting)
        {
            var cameraForward = movementCamera == null ? transform.forward : movementCamera.forward;
            var cameraRight = movementCamera == null ? transform.right : movementCamera.right;
            var direction = CalculateCameraRelativeMove(input, cameraForward, cameraRight);
            return direction * (sprinting ? sprintSpeed : walkSpeed);
        }

        public void TickMovement(Vector2 move, bool sprinting, float deltaTime)
        {
            if (characterController == null || deltaTime <= 0f)
                return;

            var planarVelocity = CalculatePlanarVelocity(move, sprinting);
            if (planarVelocity.sqrMagnitude > 0.0001f)
            {
                var desiredRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    desiredRotation,
                    turnSpeed * deltaTime);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundedVelocity;
            else
                verticalVelocity += gravity * deltaTime;

            var velocity = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(velocity * deltaTime);
        }

        public static Vector3 CalculateCameraRelativeMove(
            Vector2 input,
            Vector3 cameraForward,
            Vector3 cameraRight)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            cameraForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            cameraRight = Vector3.ProjectOnPlane(cameraRight, Vector3.up);

            if (cameraForward.sqrMagnitude < 0.0001f)
                cameraForward = Vector3.forward;
            if (cameraRight.sqrMagnitude < 0.0001f)
                cameraRight = Vector3.right;

            cameraForward.Normalize();
            cameraRight.Normalize();
            return Vector3.ClampMagnitude(
                cameraForward * input.y + cameraRight * input.x,
                1f);
        }

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (movementCamera == null && UnityEngine.Camera.main != null)
                movementCamera = UnityEngine.Camera.main.transform;
        }

        private void OnEnable()
        {
            BindInputActions();
        }

        private void OnDisable()
        {
            ReleaseInputActions();
        }

        private void OnValidate()
        {
            walkSpeed = Mathf.Max(0f, walkSpeed);
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            gravity = Mathf.Min(0f, gravity);
            groundedVelocity = Mathf.Min(0f, groundedVelocity);
        }

        private void Update()
        {
            if (characterController == null)
                return;

            var move = InputLocked || moveInput.Action == null
                ? Vector2.zero
                : moveInput.Action.ReadValue<Vector2>();
            var sprinting = !InputLocked &&
                sprintInput.Action != null &&
                sprintInput.Action.IsPressed();
            TickMovement(move, sprinting, Time.deltaTime);
        }

        private void BindInputActions()
        {
            if (moveInput.Action != null)
                return;

            moveInput.Bind(moveActionReference, CreateDefaultMoveAction);
            sprintInput.Bind(sprintActionReference, CreateDefaultSprintAction);
        }

        private void ReleaseInputActions()
        {
            moveInput.Release();
            sprintInput.Release();
        }

        private static InputAction CreateDefaultMoveAction()
        {
            var action = new InputAction(
                "ThirdPersonMove",
                InputActionType.Value,
                expectedControlType: "Vector2");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            action.AddBinding("<Gamepad>/leftStick");
            return action;
        }

        private static InputAction CreateDefaultSprintAction()
        {
            var action = new InputAction(
                "ThirdPersonSprint",
                InputActionType.Button,
                "<Keyboard>/leftShift");
            action.AddBinding("<Keyboard>/rightShift");
            action.AddBinding("<Gamepad>/leftStickPress");
            return action;
        }
    }
}
