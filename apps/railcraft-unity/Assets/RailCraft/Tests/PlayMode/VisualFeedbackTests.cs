using System.Collections;
using NUnit.Framework;
using RailCraft.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailCraft.Tests.PlayMode
{
    public sealed class VisualFeedbackTests
    {
        [UnityTest]
        public IEnumerator CurrentModuleAndTargetAreHighlighted()
        {
            using var fixture = VisualFixture.CreateAtStep("brake_module");

            yield return null;

            Assert.That(fixture.CurrentModuleHighlight, Is.True);
            Assert.That(fixture.CurrentTargetHighlight, Is.True);
            Assert.That(fixture.UnrelatedHighlightCount, Is.EqualTo(0));
            AssertCyan(fixture.CurrentModuleBaseColor);
            AssertCyan(fixture.CurrentModuleEmissionColor);
            AssertAmber(fixture.CurrentTargetBaseColor);
            AssertAmber(fixture.CurrentTargetEmissionColor);
        }

        [UnityTest]
        public IEnumerator LockedFuturePreviewIsStaticNeutralGreyWithoutEmission()
        {
            using var fixture = VisualFixture.CreateAtStep("brake_module");

            yield return null;
            Assert.That(fixture.LockedFutureRendererCount, Is.EqualTo(3));
            Assert.That(fixture.LockedFutureHighlightCount, Is.EqualTo(3));
            var initialBaseColors = fixture.LockedFutureBaseColors;
            foreach (var color in initialBaseColors)
                AssertNeutralGrey(color);
            foreach (var emission in fixture.LockedFutureEmissionColors)
                AssertBlackRgb(emission);

            yield return new WaitForSecondsRealtime(0.08f);
            var laterBaseColors = fixture.LockedFutureBaseColors;
            for (var index = 0; index < initialBaseColors.Length; index++)
                AssertColorClose(initialBaseColors[index], laterBaseColors[index], 0.005f);
        }

        [UnityTest]
        public IEnumerator RejectedDropShowsErrorWithoutMovingProgress()
        {
            using var fixture = VisualFixture.CreateAtStep("brake_module");

            fixture.DropOnWrongTarget();
            yield return null;

            Assert.That(fixture.FeedbackText,
                Is.EqualTo("安装位置不匹配，请拖到当前发光接口。"));
            Assert.That(fixture.CompletedStepCount, Is.EqualTo(4));
            Assert.That(fixture.IsRejectedEffectActive, Is.True);
            Assert.That(fixture.IsSuccessEffectActive, Is.False);
            Assert.That(fixture.RejectedDuration, Is.EqualTo(0.35f).Within(0.01f));
            Assert.That(fixture.LastEffectModuleIsFlashing, Is.True);
            AssertRed(fixture.LastEffectModuleEmissionColor);
            AssertRed(fixture.LastEffectTargetEmissionColor);

            yield return new WaitForSecondsRealtime(fixture.RejectedDuration + 0.05f);
            Assert.That(fixture.IsRejectedEffectActive, Is.False);
            Assert.That(fixture.LastEffectModuleIsFlashing, Is.False);
        }

        [UnityTest]
        public IEnumerator AcceptedDropShowsGreenFlashForConfiguredDuration()
        {
            using var fixture = VisualFixture.CreateAtStep("brake_module");

            fixture.DropOnCurrentTarget();
            yield return null;

            Assert.That(fixture.CompletedStepCount, Is.EqualTo(5));
            Assert.That(fixture.IsSuccessEffectActive, Is.True);
            Assert.That(fixture.IsRejectedEffectActive, Is.False);
            Assert.That(fixture.SuccessDuration, Is.EqualTo(0.6f).Within(0.01f));
            Assert.That(fixture.LastEffectModuleIsFlashing, Is.True);
            AssertGreen(fixture.LastEffectModuleEmissionColor);
            AssertGreen(fixture.LastEffectTargetEmissionColor);

            yield return new WaitForSecondsRealtime(fixture.SuccessDuration + 0.05f);
            Assert.That(fixture.IsSuccessEffectActive, Is.False);
            Assert.That(fixture.LastEffectModuleIsFlashing, Is.False);
        }

        [UnityTest]
        public IEnumerator DisablingSnapEffectsClearsPersistentAndTransientRendererState()
        {
            using var fixture = VisualFixture.CreateAtStep("brake_module");
            fixture.DropOnWrongTarget();
            Assert.That(fixture.IsRejectedEffectActive, Is.True);
            Assert.That(fixture.PersistentHighlightCount, Is.GreaterThan(0));
            Assert.That(fixture.ActiveFlashCount, Is.GreaterThan(0));

            fixture.DisableSnapEffects();
            yield return null;

            Assert.That(fixture.IsRejectedEffectActive, Is.False);
            Assert.That(fixture.IsSuccessEffectActive, Is.False);
            Assert.That(fixture.PersistentHighlightCount, Is.EqualTo(0));
            Assert.That(fixture.ActiveFlashCount, Is.EqualTo(0));
            Assert.That(fixture.AllObservedPropertyBlocksCleared, Is.True);
        }

        private static void AssertCyan(Color color)
        {
            Assert.That(color.g, Is.GreaterThan(color.r + 0.15f));
            Assert.That(color.b, Is.GreaterThan(color.r + 0.15f));
        }

        private static void AssertAmber(Color color)
        {
            Assert.That(color.r, Is.GreaterThan(color.b + 0.15f));
            Assert.That(color.g, Is.GreaterThan(color.b + 0.08f));
        }

        private static void AssertRed(Color color)
        {
            Assert.That(color.r, Is.GreaterThan(color.g + 0.35f));
            Assert.That(color.r, Is.GreaterThan(color.b + 0.35f));
        }

        private static void AssertGreen(Color color)
        {
            Assert.That(color.g, Is.GreaterThan(color.r + 0.35f));
            Assert.That(color.g, Is.GreaterThan(color.b + 0.35f));
        }

        private static void AssertNeutralGrey(Color color)
        {
            var minimum = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var maximum = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            Assert.That(maximum - minimum, Is.LessThan(0.1f));
        }

        private static void AssertBlackRgb(Color color)
        {
            Assert.That(Mathf.Abs(color.r), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(color.g), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(color.b), Is.LessThan(0.001f));
        }

        private static void AssertColorClose(Color expected, Color actual, float tolerance)
        {
            Assert.That(Mathf.Abs(expected.r - actual.r), Is.LessThan(tolerance));
            Assert.That(Mathf.Abs(expected.g - actual.g), Is.LessThan(tolerance));
            Assert.That(Mathf.Abs(expected.b - actual.b), Is.LessThan(tolerance));
        }
    }
}
