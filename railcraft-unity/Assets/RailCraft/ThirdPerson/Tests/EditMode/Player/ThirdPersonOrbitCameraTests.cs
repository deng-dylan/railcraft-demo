using NUnit.Framework;
using UnityEngine;

namespace RailCraft.ThirdPerson.Player.Tests
{
    public sealed class ThirdPersonOrbitCameraTests
    {
        [Test]
        public void ViewValuesAreClampedToConfiguredLimits()
        {
            var target = new GameObject("CameraTargetTest");
            var cameraObject = new GameObject("OrbitCameraTest");
            cameraObject.SetActive(false);
            try
            {
                var camera = cameraObject.AddComponent<UnityEngine.Camera>();
                var orbit = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
                orbit.Configure(camera, target.transform, null);
                orbit.ConfigureLimits(2f, 6f, 10f, 60f);

                orbit.SetView(-30f, 90f, 20f);

                Assert.That(orbit.Yaw, Is.EqualTo(330f).Within(0.0001f));
                Assert.That(orbit.Pitch, Is.EqualTo(60f).Within(0.0001f));
                Assert.That(orbit.Distance, Is.EqualTo(6f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void CollisionPullsTheCameraInFrontOfAnObstacle()
        {
            var target = new GameObject("CollisionTargetTest");
            var cameraObject = new GameObject("CollisionCameraTest");
            var wall = new GameObject("CameraWallTest");
            cameraObject.SetActive(false);
            try
            {
                var camera = cameraObject.AddComponent<UnityEngine.Camera>();
                var orbit = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
                wall.transform.SetPositionAndRotation(
                    new Vector3(0f, 1.55f, -2f),
                    Quaternion.identity);
                var wallCollider = wall.AddComponent<BoxCollider>();
                wallCollider.size = new Vector3(4f, 4f, 0.2f);
                Physics.SyncTransforms();

                orbit.Configure(camera, target.transform, null);
                orbit.ConfigureLimits(1f, 8f, 0f, 70f);
                orbit.ConfigureCollision(~0, 0.2f, 0.05f);
                orbit.SetView(0f, 0f, 4f);

                Assert.That(orbit.CurrentDistance, Is.LessThan(4f));
                Assert.That(camera.transform.position.z, Is.GreaterThan(-2f));
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
            }
        }
    }
}
