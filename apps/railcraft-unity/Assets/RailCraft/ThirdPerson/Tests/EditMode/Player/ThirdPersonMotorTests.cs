using NUnit.Framework;
using UnityEngine;

namespace RailCraft.ThirdPerson.Player.Tests
{
    public sealed class ThirdPersonMotorTests
    {
        [Test]
        public void ForwardInputFollowsCameraYawOnTheGroundPlane()
        {
            var direction = ThirdPersonMotor.CalculateCameraRelativeMove(
                Vector2.up,
                Vector3.right + Vector3.up,
                Vector3.back);

            Assert.That(direction.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(direction.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void SprintUsesConfiguredSprintSpeed()
        {
            var player = new GameObject("ThirdPersonMotorTest");
            var camera = new GameObject("MovementCameraTest");
            player.SetActive(false);
            try
            {
                var controller = player.AddComponent<CharacterController>();
                var motor = player.AddComponent<ThirdPersonMotor>();
                camera.transform.rotation = Quaternion.identity;
                motor.Configure(controller, camera.transform, null);
                motor.ConfigureMovement(3f, 8f, 720f, -20f);

                var walking = motor.CalculatePlanarVelocity(Vector2.up, false);
                var sprinting = motor.CalculatePlanarVelocity(Vector2.up, true);

                Assert.That(walking.magnitude, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(sprinting.magnitude, Is.EqualTo(8f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(camera);
            }
        }

        [Test]
        public void InputLockIsSharedWithTheMotor()
        {
            var player = new GameObject("ThirdPersonMotorLockTest");
            player.SetActive(false);
            try
            {
                var controller = player.AddComponent<CharacterController>();
                var inputLock = player.AddComponent<ThirdPersonInputLock>();
                var motor = player.AddComponent<ThirdPersonMotor>();
                motor.Configure(controller, null, inputLock);

                motor.SetInputLocked(true);

                Assert.That(motor.InputLocked, Is.True);
                Assert.That(inputLock.InputLocked, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
