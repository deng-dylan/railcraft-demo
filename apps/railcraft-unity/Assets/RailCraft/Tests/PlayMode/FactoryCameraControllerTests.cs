using System.Collections;
using NUnit.Framework;
using RailCraft.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailCraft.Tests.PlayMode
{
    public sealed class FactoryCameraControllerTests
    {
        [UnityTest]
        public IEnumerator ZoomClampsToConfiguredRange()
        {
            using var fixture = CameraFixture.Create();

            fixture.Camera.ApplyZoom(1000f);
            yield return null;
            Assert.That(fixture.Camera.Distance, Is.InRange(3.5f, 18f));

            fixture.Camera.ApplyZoom(-1000f);
            yield return null;
            Assert.That(fixture.Camera.Distance, Is.InRange(3.5f, 18f));
        }

        [UnityTest]
        public IEnumerator CameraInputDoesNotRotateDraggedPart()
        {
            using var fixture = CameraFixture.CreateWithDraggable();
            var initial = fixture.Module.rotation;

            fixture.Camera.SetInteractionLocked(true);
            fixture.Camera.ApplyOrbit(new Vector2(100f, 50f));
            yield return null;

            Assert.That(Quaternion.Angle(initial, fixture.Module.rotation), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator OrbitPitchClampsAndCameraStaysAboveFactoryFloor()
        {
            using var fixture = CameraFixture.Create();

            fixture.Camera.ApplyOrbit(new Vector2(0f, -10000f));
            yield return null;
            Assert.That(fixture.Camera.Pitch, Is.InRange(15f, 75f));
            Assert.That(fixture.UnityCamera.transform.position.y, Is.GreaterThan(0f));

            fixture.Camera.ApplyOrbit(new Vector2(0f, 20000f));
            yield return null;
            Assert.That(fixture.Camera.Pitch, Is.InRange(15f, 75f));
            Assert.That(fixture.UnityCamera.transform.position.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator InteractionLockSuppressesOrbitChanges()
        {
            using var fixture = CameraFixture.Create();
            var yaw = fixture.Camera.Yaw;
            var pitch = fixture.Camera.Pitch;

            fixture.Camera.SetInteractionLocked(true);
            fixture.Camera.ApplyOrbit(new Vector2(80f, 40f));
            yield return null;

            Assert.That(fixture.Camera.Yaw, Is.EqualTo(yaw).Within(0.001f));
            Assert.That(fixture.Camera.Pitch, Is.EqualTo(pitch).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator InteractionLockSuppressesPanAndKeyboardMove()
        {
            using var fixture = CameraFixture.Create();
            var focus = fixture.Camera.FocusPosition;

            fixture.Camera.SetInteractionLocked(true);
            fixture.Camera.ApplyPan(new Vector2(80f, 40f));
            fixture.Camera.ApplyMove(Vector2.one, 1f);
            yield return null;

            Assert.That(Vector3.Distance(focus, fixture.Camera.FocusPosition), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator ShotDirectorReturnsToStableOverviewAfterStepFocus()
        {
            using var fixture = CameraFixture.Create();

            Assert.That(fixture.Director.Focus("step"), Is.True);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(Vector3.Distance(fixture.Camera.FocusPosition, fixture.StepPosition), Is.LessThan(0.01f));
            Assert.That(fixture.Director.IsTransitioning, Is.False);

            Assert.That(fixture.Director.Focus("overview"), Is.True);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(Vector3.Distance(fixture.Camera.FocusPosition, fixture.OverviewPosition), Is.LessThan(0.01f));
            Assert.That(fixture.Director.CurrentShotId, Is.EqualTo("overview"));
        }

        [UnityTest]
        public IEnumerator NewShotInterruptsPreviousTransitionAndWins()
        {
            using var fixture = CameraFixture.Create();

            fixture.Director.Focus("step");
            yield return null;
            fixture.Director.Focus("overview");
            yield return new WaitForSecondsRealtime(0.08f);

            Assert.That(Vector3.Distance(fixture.Camera.FocusPosition, fixture.OverviewPosition), Is.LessThan(0.01f));
            Assert.That(fixture.Director.CurrentShotId, Is.EqualTo("overview"));
            Assert.That(fixture.Director.IsTransitioning, Is.False);
        }

        [UnityTest]
        public IEnumerator UnknownShotLeavesCameraUnchanged()
        {
            using var fixture = CameraFixture.Create();
            var focus = fixture.Camera.FocusPosition;

            Assert.That(fixture.Director.Focus("missing"), Is.False);
            yield return null;

            Assert.That(Vector3.Distance(focus, fixture.Camera.FocusPosition), Is.LessThan(0.001f));
            Assert.That(fixture.Director.IsTransitioning, Is.False);
        }
    }
}
