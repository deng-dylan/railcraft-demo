using NUnit.Framework;

namespace RailCraft.ThirdPerson.Player.Tests
{
    public sealed class ThirdPersonLocomotionAnimatorTests
    {
        [TestCase(-1f, 7f, 0f)]
        [TestCase(0f, 7f, 0f)]
        [TestCase(3.5f, 7f, 0.5f)]
        [TestCase(7f, 7f, 1f)]
        [TestCase(14f, 7f, 1f)]
        [TestCase(1f, 0f, 1f)]
        public void NormalizeSpeedClampsToAnimatorBlendRange(
            float planarSpeed,
            float maximumSpeed,
            float expected)
        {
            Assert.That(
                ThirdPersonLocomotionAnimator.NormalizeSpeed(planarSpeed, maximumSpeed),
                Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
