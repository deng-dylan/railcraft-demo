using UnityEngine;

namespace RailCraft.Interaction
{
    public sealed class DraggableModule : MonoBehaviour
    {
        [SerializeField] private string stepId;
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private Transform visualRoot;

        private Vector3 startPosition;
        private Quaternion lockedRotation;
        private bool isDragging;
        private bool isSnapping;

        public string StepId => stepId;
        public Collider InteractionCollider => interactionCollider;
        public Transform VisualRoot => visualRoot == null ? transform : visualRoot;
        public bool IsDragging => isDragging;
        public bool IsSnapping => isSnapping;

        public void Configure(string configuredStepId, Collider configuredCollider, Transform configuredVisualRoot)
        {
            stepId = configuredStepId;
            interactionCollider = configuredCollider;
            visualRoot = configuredVisualRoot;
        }

        internal bool BeginDrag()
        {
            if (isDragging || isSnapping || interactionCollider == null || !interactionCollider.enabled)
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

            transform.position = position;
            transform.rotation = lockedRotation;
        }

        internal void BeginSnap()
        {
            isDragging = false;
            isSnapping = true;
            if (interactionCollider != null)
                interactionCollider.enabled = false;
        }

        internal void FinishSnap(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            isSnapping = false;
        }

        internal Vector3 StartPosition => startPosition;
        internal Quaternion LockedRotation => lockedRotation;

        internal void EndRejectedDrag()
        {
            isDragging = false;
            isSnapping = false;
        }
    }
}
