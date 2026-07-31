using RailCraft.Interaction;
using UnityEngine;

namespace RailCraft.Tests.EditMode.Fixtures
{
    internal sealed class DropTargetFixture
    {
        public DropTarget Target { get; }
        private readonly GameObject targetObject;
        private readonly GameObject anchorObject;

        private DropTargetFixture(DropTarget target, GameObject targetObject, GameObject anchorObject)
        {
            Target = target;
            this.targetObject = targetObject;
            this.anchorObject = anchorObject;
        }

        public static DropTargetFixture Create(
            string targetId,
            string acceptedStepId,
            IDragAuthorization authorization)
        {
            var targetObject = new GameObject(targetId);
            var anchorObject = new GameObject(targetId + ".anchor");
            anchorObject.transform.SetParent(targetObject.transform);
            var target = targetObject.AddComponent<DropTarget>();
            target.Configure(targetId, acceptedStepId, anchorObject.transform, 0.45f, 0.5f);
            target.SetAuthorization(authorization);
            return new DropTargetFixture(target, targetObject, anchorObject);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(anchorObject);
            Object.DestroyImmediate(targetObject);
        }
    }
}
