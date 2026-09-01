using UnityEngine;

namespace RailCraft.Interaction
{
    public enum DragMotionConstraint
    {
        Free,
        Vertical
    }

    public sealed class DraggableModule : MonoBehaviour
    {
        [SerializeField] private string stepId;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private DragMotionConstraint motionConstraint;

        private Vector3 startPosition;
        private Quaternion lockedRotation;
        private bool isDragging;
        private bool isSnapping;
        private bool isReturning;

        public string StepId => stepId;
        public Collider InteractionCollider => interactionCollider;
        public Transform VisualRoot => visualRoot == null ? transform : visualRoot;
        public bool IsDragging => isDragging;
        public bool IsSnapping => isSnapping;
        public bool IsReturning => isReturning;
        public DragMotionConstraint MotionConstraint => motionConstraint;

        public void Configure(string configuredStepId, Collider configuredCollider, Transform configuredVisualRoot)
        {
            stepId = configuredStepId;
            interactionCollider = configuredCollider;
            visualRoot = configuredVisualRoot;
        }

        public void SetMotionConstraint(DragMotionConstraint configuredConstraint)
        {
            motionConstraint = configuredConstraint;
        }

        internal bool BeginDrag()
        {
            if (isDragging || isSnapping || isReturning
                || interactionCollider == null || !interactionCollider.enabled)
                return false;

            startPosition = transform.position;
            lockedRotation = transform.rotation;
            isDragging = true;
            return true;
        }

        internal void DragTo(Vector3 position)
        {
            if (!isDragging)
                return;

            transform.position = motionConstraint == DragMotionConstraint.Vertical
                ? new Vector3(startPosition.x, position.y, startPosition.z)
                : position;
            transform.rotation = lockedRotation;
        }

        internal void BeginSnap()
        {
            isDragging = false;
            isSnapping = true;
            isReturning = false;
            if (interactionCollider != null)
                interactionCollider.enabled = false;
        }

        internal void FinishSnap(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            isSnapping = false;
            isReturning = false;
        }

        internal Vector3 StartPosition => startPosition;
        internal Quaternion LockedRotation => lockedRotation;

        internal void BeginReturn()
        {
            isDragging = false;
            isSnapping = false;
            isReturning = true;
        }

        internal void FinishReturn()
        {
            transform.SetPositionAndRotation(startPosition, lockedRotation);
            isDragging = false;
            isSnapping = false;
            isReturning = false;
        }

        internal void CancelDragAndRestoreStart()
        {
            FinishReturn();
        }
    }
}
