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
        public IEnumerator DraggingPreservesAuthoredRotation()
        {
            var fixture = DragFixture.CreateUnlocked();
            var initial = fixture.Module.transform.rotation;

            try
            {
                Assert.That(fixture.Controller.TryBeginDrag(fixture.Module), Is.True);
                fixture.Controller.DragTo(new Vector3(3f, 4f, 5f));
                yield return null;

                Assert.That(Quaternion.Angle(initial, fixture.Module.transform.rotation), Is.LessThan(0.01f));
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator AcceptedDropRaisesCompletionOnlyAfterSnapFinishes()
        {
            var fixture = DragFixture.CreateUnlocked();
            var completedCount = 0;
            fixture.Controller.DropCompleted += _ => completedCount++;

            try
            {
                fixture.Controller.TryBeginDrag(fixture.Module);
                fixture.Controller.DragTo(fixture.Target.SnapAnchor.position);
                var result = fixture.Controller.ReleaseAt(fixture.Target.SnapAnchor.position);

                Assert.That(result.Accepted, Is.True);
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
        public IEnumerator WrongTargetReturnsModuleWithoutChangingItsRotation()
        {
            var fixture = DragFixture.CreateUnlocked("wheelset_axlebox_a");
            var initialRotation = fixture.Module.transform.rotation;
            DragDropResult rejected = null;
            fixture.Controller.DropRejected += result => rejected = result;

            try
            {
                fixture.Controller.TryBeginDrag(fixture.Module);
                fixture.Controller.DragTo(fixture.Target.SnapAnchor.position);
                var result = fixture.Controller.ReleaseAt(fixture.Target.SnapAnchor.position);

                Assert.That(result.Code, Is.EqualTo("wrong_target"));
                Assert.That(rejected, Is.SameAs(result));
                yield return new WaitForSeconds(0.3f);

                Assert.That(Vector3.Distance(fixture.Module.transform.position, fixture.StartPosition),
                    Is.LessThan(0.001f));
                Assert.That(Quaternion.Angle(initialRotation, fixture.Module.transform.rotation), Is.LessThan(0.01f));
                Assert.That(fixture.Module.InteractionCollider.enabled, Is.True);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ReleasingTwiceDuringSnapDoesNotDuplicateCompletion()
        {
            var fixture = DragFixture.CreateUnlocked();
            var completedCount = 0;
            fixture.Controller.DropCompleted += _ => completedCount++;

            try
            {
                fixture.Controller.TryBeginDrag(fixture.Module);
                fixture.Controller.DragTo(fixture.Target.SnapAnchor.position);
                fixture.Controller.ReleaseAt(fixture.Target.SnapAnchor.position);

                Assert.That(fixture.Controller.ReleaseAt(fixture.Target.SnapAnchor.position).Code,
                    Is.EqualTo("not_dragging"));
                yield return new WaitForSeconds(0.08f);

                Assert.That(completedCount, Is.EqualTo(1));
            }
            finally
            {
                fixture.Dispose();
            }
        }
    }
}
