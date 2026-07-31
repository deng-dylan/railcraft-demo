using System;
using RailCraft.Interaction;
using UnityEngine;

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class DragFixture : IDisposable
    {
        public DraggableModule Module { get; }
        public DropTarget Target { get; }
        public DragDropController Controller { get; }
        public Vector3 StartPosition { get; }

        private readonly GameObject moduleObject;
        private readonly GameObject targetObject;
        private readonly GameObject anchorObject;
        private readonly GameObject controllerObject;

        private DragFixture(
            DraggableModule module,
            DropTarget target,
            DragDropController controller,
            GameObject moduleObject,
            GameObject targetObject,
            GameObject anchorObject,
            GameObject controllerObject)
        {
            Module = module;
            Target = target;
            Controller = controller;
            this.moduleObject = moduleObject;
            this.targetObject = targetObject;
            this.anchorObject = anchorObject;
            this.controllerObject = controllerObject;
            StartPosition = module.transform.position;
        }

        public static DragFixture CreateUnlocked(string acceptedStepId = "frame_module")
        {
            var moduleObject = new GameObject("module");
            moduleObject.transform.position = new Vector3(1f, 2f, 3f);
            moduleObject.transform.rotation = Quaternion.Euler(17f, 31f, 7f);
            var module = moduleObject.AddComponent<DraggableModule>();
            var collider = moduleObject.AddComponent<BoxCollider>();
            module.Configure("frame_module", collider, moduleObject.transform);

            var targetObject = new GameObject("target");
            var anchorObject = new GameObject("snap.anchor");
            anchorObject.transform.position = new Vector3(5f, 2f, 3f);
            anchorObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var target = targetObject.AddComponent<DropTarget>();
            target.Configure("target.frame", acceptedStepId, anchorObject.transform, 0.05f, 0.75f);

            var controllerObject = new GameObject("drag.controller");
            var controller = controllerObject.AddComponent<DragDropController>();
            controller.Configure(
                new FakeAuthorization("frame_module"),
                new[] { target },
                null);

            return new DragFixture(
                module,
                target,
                controller,
                moduleObject,
                targetObject,
                anchorObject,
                controllerObject);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(controllerObject);
            UnityEngine.Object.Destroy(anchorObject);
            UnityEngine.Object.Destroy(targetObject);
            UnityEngine.Object.Destroy(moduleObject);
        }
    }
}
