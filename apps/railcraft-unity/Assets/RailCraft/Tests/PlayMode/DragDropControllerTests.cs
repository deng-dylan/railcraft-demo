using System.Collections;
using NUnit.Framework;
using RailCraft.Interaction;
using RailCraft.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailCraft.Tests.PlayMode
{
    public sealed class DragDropControllerTests
    {
        [UnityTest]
        public IEnumerator PointerDragStartsAtTheModuleRootWithoutSurfaceJump()
        {
            var fixture = DragFixture.CreateUnlocked();

            try
            {
                yield return null;
                Physics.SyncTransforms();
                var ray = fixture.Camera.ScreenPointToRay(fixture.ModuleScreenPosition);
                Assert.That(fixture.Module.InteractionCollider.Raycast(
                    ray, out _, float.PositiveInfinity), Is.True,
                    "The real screen ray must hit the configured module collider.");
                yield return fixture.BeginPointerDrag();

                Assert.That(fixture.Mouse.leftButton.isPressed, Is.True,
                    "The Input System test mouse must hold the left button.");
                Assert.That(fixture.Controller.IsPartDragActive, Is.True);
                Assert.That(fixture.Module.transform.position.z,
                    Is.EqualTo(fixture.StartPosition.z).Within(0.001f));

                yield return fixture.MovePointerTo(fixture.ModuleScreenPosition + new Vector2(6f, 0f));

                Assert.That(fixture.Module.transform.position.z,
                    Is.EqualTo(fixture.StartPosition.z).Within(0.001f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator PointerDraggingPreservesAuthoredRotation()
        {
            var fixture = DragFixture.CreateUnlocked();
            var initial = fixture.Module.transform.rotation;

            try
            {
                yield return fixture.DragAcrossScreen(fixture.TargetScreenPosition);

                Assert.That(Quaternion.Angle(initial, fixture.Module.transform.rotation), Is.LessThan(0.01f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator PointerAcceptedDropRaisesCompletionOnlyAfterSnapFinishes()
        {
            var fixture = DragFixture.CreateUnlocked();
            var completedCount = 0;
            fixture.Controller.DropCompleted += _ => completedCount++;

            try
            {
                yield return fixture.DragAcrossScreen(fixture.TargetScreenPosition);
                yield return fixture.ReleasePointer();

                Assert.That(completedCount, Is.EqualTo(0));
                Assert.That(fixture.Module.InteractionCollider.enabled, Is.False);

                yield return new WaitForSeconds(0.08f);

                Assert.That(completedCount, Is.EqualTo(1));
                Assert.That(Vector3.Distance(
                    fixture.Module.transform.position,
                    fixture.Target.SnapAnchor.position), Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(fixture.Target.SnapAnchor.rotation, fixture.Module.transform.rotation),
                    Is.LessThan(0.01f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator RejectedReturnLocksTheModuleUntilItReachesItsStartPose()
        {
            var fixture = DragFixture.CreateUnlocked("wheelset_axlebox_a");

            try
            {
                yield return fixture.DragAcrossScreen(fixture.TargetScreenPosition);
                yield return fixture.ReleasePointer();
                yield return fixture.BeginPointerDrag();

                Assert.That(fixture.Controller.IsPartDragActive, Is.False);
                Assert.That(fixture.OrbitController.enabled, Is.True);
                Assert.That(Vector3.Distance(fixture.Module.transform.position, fixture.StartPosition),
                    Is.GreaterThan(0.001f));

                yield return new WaitForSeconds(0.3f);

                Assert.That(Vector3.Distance(fixture.Module.transform.position, fixture.StartPosition),
                    Is.LessThan(0.001f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DisablingDuringActiveDragRestoresPoseAndEmitsOneEndSignal()
        {
            var fixture = DragFixture.CreateUnlocked();
            var endSignals = 0;
            fixture.Controller.PartDragStateChanged += isDragging =>
            {
                if (!isDragging)
                    endSignals++;
            };

            try
            {
                yield return fixture.BeginPointerDrag();
                fixture.Controller.enabled = false;
                yield return null;

                Assert.That(Vector3.Distance(fixture.Module.transform.position, fixture.StartPosition),
                    Is.LessThan(0.001f));
                Assert.That(fixture.Module.IsDragging, Is.False);
                Assert.That(fixture.Module.IsSnapping, Is.False);
                Assert.That(fixture.OrbitController.enabled, Is.True);
                Assert.That(endSignals, Is.EqualTo(1));

                fixture.Controller.enabled = true;
                yield return null;
                Assert.That(fixture.Module.IsDragging || fixture.Module.IsSnapping, Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CancellingAllInteractionsRestoresCameraAndEndsActiveDragOnce()
        {
            var fixture = DragFixture.CreateUnlocked();
            var endSignals = 0;
            fixture.Controller.PartDragStateChanged += isDragging =>
            {
                if (!isDragging)
                    endSignals++;
            };

            try
            {
                yield return fixture.BeginPointerDrag();
                Assert.That(fixture.OrbitController.enabled, Is.False);

                fixture.Controller.CancelAllInteractions();

                Assert.That(fixture.Controller.IsPartDragActive, Is.False);
                Assert.That(fixture.OrbitController.enabled, Is.True);
                Assert.That(endSignals, Is.EqualTo(1));
                Assert.That(Vector3.Distance(fixture.Module.transform.position, fixture.StartPosition),
                    Is.LessThan(0.001f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CancellingWhileIdleDoesNotDisableCameraOrbit()
        {
            var fixture = DragFixture.CreateUnlocked();
            try
            {
                Assert.That(fixture.OrbitController.enabled, Is.True);

                fixture.Controller.CancelAllInteractions();
                yield return null;

                Assert.That(fixture.OrbitController.enabled, Is.True);
                Assert.That(fixture.Controller.IsPartDragActive, Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DisablingFromDragEndCannotReenterTheController()
        {
            var fixture = DragFixture.CreateUnlocked();
            var reentryAccepted = false;
            fixture.Controller.PartDragStateChanged += isDragging =>
            {
                if (!isDragging)
                {
                    fixture.Controller.enabled = false;
                    reentryAccepted = fixture.Controller.TryBeginDrag(fixture.Module);
                }
            };

            try
            {
                yield return fixture.BeginPointerDrag();
                fixture.Controller.enabled = false;
                yield return null;

                Assert.That(reentryAccepted, Is.False);
                Assert.That(fixture.Controller.IsPartDragActive, Is.False);
                Assert.That(fixture.Module.IsDragging || fixture.Module.IsSnapping || fixture.Module.IsReturning,
                    Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DisablingDuringRejectedReturnRestoresStartPoseWithoutDuplicateEndSignal()
        {
            var fixture = DragFixture.CreateUnlocked("wheelset_axlebox_a");
            var endSignals = 0;
            fixture.Controller.PartDragStateChanged += isDragging =>
            {
                if (!isDragging)
                    endSignals++;
            };

            try
            {
                yield return fixture.DragAcrossScreen(fixture.TargetScreenPosition);
                yield return fixture.ReleasePointer();
                yield return null;
                fixture.Controller.enabled = false;
                yield return null;

                Assert.That(Vector3.Distance(fixture.Module.transform.position, fixture.StartPosition),
                    Is.LessThan(0.001f));
                Assert.That(fixture.Module.IsDragging || fixture.Module.IsSnapping, Is.False);
                Assert.That(fixture.OrbitController.enabled, Is.True);
                Assert.That(endSignals, Is.EqualTo(1));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DisablingDuringSnapCompletesOnceAndLeavesTheModuleReusable()
        {
            var fixture = DragFixture.CreateUnlocked();
            var completedCount = 0;
            var endSignals = 0;
            fixture.Controller.DropCompleted += _ => completedCount++;
            fixture.Controller.PartDragStateChanged += isDragging =>
            {
                if (!isDragging)
                    endSignals++;
            };

            try
            {
                yield return fixture.DragAcrossScreen(fixture.TargetScreenPosition);
                yield return fixture.ReleasePointer();
                yield return null;
                fixture.Controller.enabled = false;
                yield return null;

                Assert.That(completedCount, Is.EqualTo(1));
                Assert.That(endSignals, Is.EqualTo(1));
                Assert.That(Vector3.Distance(fixture.Module.transform.position, fixture.Target.SnapAnchor.position),
                    Is.LessThan(0.001f));
                Assert.That(fixture.Module.InteractionCollider.enabled, Is.False);
                Assert.That(fixture.Module.IsDragging || fixture.Module.IsSnapping, Is.False);

                fixture.Controller.enabled = true;
                yield return null;
                Assert.That(fixture.Module.IsDragging || fixture.Module.IsSnapping, Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DisablingFromDragEndStillFinalizesTheRegisteredSnap()
        {
            var fixture = DragFixture.CreateUnlocked();
            var completedCount = 0;
            fixture.Controller.DropCompleted += _ => completedCount++;
            fixture.Controller.PartDragStateChanged += isDragging =>
            {
                if (!isDragging)
                    fixture.Controller.enabled = false;
            };

            try
            {
                yield return fixture.DragAcrossScreen(fixture.TargetScreenPosition);
                yield return fixture.ReleasePointer();
                yield return null;

                Assert.That(completedCount, Is.EqualTo(1));
                Assert.That(Vector3.Distance(fixture.Module.transform.position,
                    fixture.Target.SnapAnchor.position), Is.LessThan(0.001f));
                Assert.That(fixture.Module.IsDragging || fixture.Module.IsSnapping || fixture.Module.IsReturning,
                    Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }
    }
}
