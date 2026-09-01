using NUnit.Framework;
using RailCraft.Tests.EditMode.Fixtures;

namespace RailCraft.Tests.EditMode
{
    public sealed class DropTargetTests
    {
        [Test]
        public void TargetAcceptsOnlyMatchingUnlockedStep()
        {
            var fixture = DropTargetFixture.Create(
                "target.frame",
                "frame_module",
                new FakeAuthorization("frame_module", true));

            try
            {
                Assert.That(fixture.Target.CanAccept("frame_module"), Is.True);
                Assert.That(fixture.Target.CanAccept("wheelset_axlebox_a"), Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [Test]
        public void TargetRejectsMatchingStepWhenAuthorizationIsLocked()
        {
            var fixture = DropTargetFixture.Create(
                "target.frame",
                "frame_module",
                new FakeAuthorization("frame_module", false));

            try
            {
                Assert.That(fixture.Target.CanAccept("frame_module"), Is.False);
            }
            finally
            {
                fixture.Dispose();
            }
        }
    }
}
