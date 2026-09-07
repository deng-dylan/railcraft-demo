using UnityEngine;

namespace RailCraft.ThirdPerson.Player
{
    /// <summary>
    /// Keeps a visual-only humanoid animator in sync with the existing
    /// CharacterController motor. Movement and collision remain authoritative
    /// on the player root; imported characters never use root motion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThirdPersonLocomotionAnimator : MonoBehaviour
    {
        public const string SpeedParameterName = "Speed";
        public const string GroundedParameterName = "Grounded";

        private static readonly int SpeedParameter = Animator.StringToHash(SpeedParameterName);
        private static readonly int GroundedParameter = Animator.StringToHash(GroundedParameterName);

        [SerializeField] private Animator animator;
        [SerializeField, Min(0.01f)] private float maximumSpeed = 7f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.08f;

        private bool hasSpeedParameter;
        private bool hasGroundedParameter;

        public Animator Animator => animator;
        public float MaximumSpeed => maximumSpeed;

        public void Configure(
            Animator configuredAnimator,
            float configuredMaximumSpeed,
            float configuredSpeedDampTime = 0.08f)
        {
            animator = configuredAnimator;
            maximumSpeed = Mathf.Max(0.01f, configuredMaximumSpeed);
            speedDampTime = Mathf.Max(0f, configuredSpeedDampTime);
            if (animator != null)
                animator.applyRootMotion = false;
            RefreshParameterContract();
        }

        public void SetMaximumSpeed(float configuredMaximumSpeed)
        {
            maximumSpeed = Mathf.Max(0.01f, configuredMaximumSpeed);
        }

        public void ApplyMotion(float planarSpeed, bool grounded, float deltaTime)
        {
            if (animator == null || !animator.isActiveAndEnabled)
                return;

            if (hasSpeedParameter)
            {
                var normalizedSpeed = NormalizeSpeed(planarSpeed, maximumSpeed);
                if (speedDampTime > 0f && deltaTime > 0f)
                    animator.SetFloat(SpeedParameter, normalizedSpeed, speedDampTime, deltaTime);
                else
                    animator.SetFloat(SpeedParameter, normalizedSpeed);
            }

            if (hasGroundedParameter)
                animator.SetBool(GroundedParameter, grounded);
        }

        public static float NormalizeSpeed(float planarSpeed, float configuredMaximumSpeed)
        {
            var safeMaximum = Mathf.Max(0.01f, configuredMaximumSpeed);
            return Mathf.Clamp01(Mathf.Max(0f, planarSpeed) / safeMaximum);
        }

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>(true);
            RefreshParameterContract();
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
                animator.applyRootMotion = false;
            RefreshParameterContract();
        }

        private void OnValidate()
        {
            maximumSpeed = Mathf.Max(0.01f, maximumSpeed);
            speedDampTime = Mathf.Max(0f, speedDampTime);
            RefreshParameterContract();
        }

        private void RefreshParameterContract()
        {
            hasSpeedParameter = HasParameter(SpeedParameter, AnimatorControllerParameterType.Float);
            hasGroundedParameter = HasParameter(GroundedParameter, AnimatorControllerParameterType.Bool);
        }

        private bool HasParameter(int nameHash, AnimatorControllerParameterType parameterType)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            var parameters = animator.parameters;
            for (var index = 0; index < parameters.Length; index++)
            {
                if (parameters[index].nameHash == nameHash &&
                    parameters[index].type == parameterType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
