using System;
using System.Collections;
using System.Reflection;
using RailCraft.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class DragFixture : IDisposable
    {
        public DraggableModule Module { get; }
        public DropTarget Target { get; }
        public DragDropController Controller { get; }
        public Camera Camera { get; }
        public Mouse Mouse { get; }
        public TestOrbitBehaviour OrbitController { get; }
        public Vector3 StartPosition { get; }

        private readonly GameObject moduleObject;
        private readonly GameObject targetObject;
        private readonly GameObject anchorObject;
        private readonly GameObject controllerObject;
        private readonly GameObject cameraObject;
        private readonly GameObject orbitObject;
        private readonly InputTestFixture inputFixture;

        private DragFixture(
            DraggableModule module,
            DropTarget target,
            DragDropController controller,
            Camera camera,
            Mouse mouse,
            TestOrbitBehaviour orbitController,
            GameObject moduleObject,
            GameObject targetObject,
            GameObject anchorObject,
            GameObject controllerObject,
            GameObject cameraObject,
            GameObject orbitObject,
            InputTestFixture inputFixture)
        {
            Module = module;
            Target = target;
            Controller = controller;
            Camera = camera;
            Mouse = mouse;
            OrbitController = orbitController;
            this.moduleObject = moduleObject;
            this.targetObject = targetObject;
            this.anchorObject = anchorObject;
            this.controllerObject = controllerObject;
            this.cameraObject = cameraObject;
            this.orbitObject = orbitObject;
            this.inputFixture = inputFixture;
            StartPosition = module.transform.position;
        }

        public static DragFixture CreateUnlocked(string acceptedStepId = "frame_module")
        {
            var inputFixture = new InputTestFixture();
            inputFixture.Setup();
            var mouse = InputSystem.AddDevice<Mouse>();

            var cameraObject = new GameObject("drag.camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;
            var camera = cameraObject.AddComponent<Camera>();

            var moduleObject = new GameObject("module");
            moduleObject.transform.position = new Vector3(0f, 0f, 5f);
            moduleObject.transform.rotation = Quaternion.Euler(17f, 31f, 7f);
            var module = moduleObject.AddComponent<DraggableModule>();
            var collider = moduleObject.AddComponent<BoxCollider>();
            module.Configure("frame_module", collider, moduleObject.transform);

            var targetObject = new GameObject("target");
            var anchorObject = new GameObject("snap.anchor");
            anchorObject.transform.position = new Vector3(3f, 0f, 5f);
            anchorObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var target = targetObject.AddComponent<DropTarget>();
            target.Configure("target.frame", acceptedStepId, anchorObject.transform, 0.05f, 0.8f);

            var orbitObject = new GameObject("orbit.controller");
            var orbitController = orbitObject.AddComponent<TestOrbitBehaviour>();
            var controllerObject = new GameObject("drag.controller");
            var controller = controllerObject.AddComponent<DragDropController>();
            controller.Configure(
                new FakeAuthorization("frame_module"),
                new[] { target },
                orbitController);
            SetPrivateField(controller, "interactionCamera", camera);
            SetPrivateField(controller, "draggableModules", new[] { module });

            return new DragFixture(
                module,
                target,
                controller,
                camera,
                mouse,
                orbitController,
                moduleObject,
                targetObject,
                anchorObject,
                controllerObject,
                cameraObject,
                orbitObject,
                inputFixture);
        }

        public IEnumerator BeginPointerDrag()
        {
            yield return null;
            Physics.SyncTransforms();
            yield return MovePointerTo(ModuleScreenPosition);
            QueueLeftButton(true);
            yield return null;
        }

        public IEnumerator DragAcrossScreen(Vector2 destination)
        {
            yield return BeginPointerDrag();
            yield return MovePointerTo(destination);
        }

        public IEnumerator ReleasePointer()
        {
            QueueLeftButton(false);
            yield return null;
        }

        public IEnumerator MovePointerTo(Vector2 position)
        {
            inputFixture.Move(Mouse.position, position);
            yield return null;
        }

        public Vector2 ModuleScreenPosition => Camera.WorldToScreenPoint(Module.transform.position);
        public Vector2 TargetScreenPosition => Camera.WorldToScreenPoint(Target.SnapAnchor.position);

        public void Dispose()
        {
            controllerObject.SetActive(false);
            UnityEngine.Object.Destroy(controllerObject);
            UnityEngine.Object.Destroy(orbitObject);
            UnityEngine.Object.Destroy(anchorObject);
            UnityEngine.Object.Destroy(targetObject);
            UnityEngine.Object.Destroy(moduleObject);
            UnityEngine.Object.Destroy(cameraObject);
            inputFixture.TearDown();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private void QueueLeftButton(bool pressed)
        {
            if (pressed)
                inputFixture.Press(Mouse.leftButton);
            else
                inputFixture.Release(Mouse.leftButton);
        }
    }

    internal sealed class TestOrbitBehaviour : MonoBehaviour
    {
    }
}
