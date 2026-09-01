using System;
using RailCraft.CameraSystem;
using UnityEngine;

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class CameraFixture : IDisposable
    {
        private readonly GameObject root;

        public FactoryCameraController Camera { get; }
        public CameraShotDirector Director { get; }
        public Transform Module { get; }
        public UnityEngine.Camera UnityCamera { get; }
        public Vector3 OverviewPosition { get; }
        public Vector3 StepPosition { get; }

        private CameraFixture(GameObject root, FactoryCameraController camera,
            CameraShotDirector director, UnityEngine.Camera unityCamera, Transform module,
            Vector3 overviewPosition, Vector3 stepPosition)
        {
            this.root = root;
            Camera = camera;
            Director = director;
            UnityCamera = unityCamera;
            Module = module;
            OverviewPosition = overviewPosition;
            StepPosition = stepPosition;
        }

        public static CameraFixture Create()
        {
            return CreateInternal(false);
        }

        public static CameraFixture CreateWithDraggable()
        {
            return CreateInternal(true);
        }

        private static CameraFixture CreateInternal(bool includeModule)
        {
            var root = new GameObject("camera.fixture");
            var focus = new GameObject("focus").transform;
            focus.SetParent(root.transform, false);
            focus.position = new Vector3(0f, 1f, 0f);

            var cameraObject = new GameObject("camera");
            cameraObject.transform.SetParent(root.transform, false);
            var unityCamera = cameraObject.AddComponent<UnityEngine.Camera>();

            var controller = root.AddComponent<FactoryCameraController>();
            controller.Configure(unityCamera, focus, null);

            var overviewAnchor = new GameObject("overview.anchor").transform;
            overviewAnchor.SetParent(root.transform, false);
            overviewAnchor.position = new Vector3(0f, 1f, 0f);
            var stepAnchor = new GameObject("step.anchor").transform;
            stepAnchor.SetParent(root.transform, false);
            stepAnchor.position = new Vector3(5f, 1.5f, 2f);
            var director = root.AddComponent<CameraShotDirector>();
            director.Configure(controller, new[]
            {
                new FactoryCameraShot
                {
                    shotId = "overview",
                    focusAnchor = overviewAnchor,
                    distance = 12f,
                    yaw = 35f,
                    pitch = 38f
                },
                new FactoryCameraShot
                {
                    shotId = "step",
                    focusAnchor = stepAnchor,
                    distance = 6f,
                    yaw = 75f,
                    pitch = 28f
                }
            }, 0.05f);

            Transform module = null;
            if (includeModule)
            {
                module = new GameObject("draggable.module").transform;
                module.SetParent(root.transform, false);
                module.SetPositionAndRotation(new Vector3(1f, 0.5f, 2f),
                    Quaternion.Euler(17f, 31f, 7f));
            }

            return new CameraFixture(root, controller, director, unityCamera, module,
                overviewAnchor.position, stepAnchor.position);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(root);
        }
    }
}
