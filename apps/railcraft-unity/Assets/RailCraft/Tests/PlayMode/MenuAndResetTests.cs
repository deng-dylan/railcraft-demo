using System.Collections;
using NUnit.Framework;
using RailCraft.Flow;
using RailCraft.Presentation;
using RailCraft.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailCraft.Tests.PlayMode
{
    public sealed class MenuAndResetTests
    {
        [UnityTest]
        public IEnumerator MainMenuContainsRequiredActionsAndNoContinueButton()
        {
            using var fixture = MenuFixture.Create();
            yield return null;

            Assert.That(fixture.FindButton("继续游戏"), Is.Null);
            Assert.That(fixture.FindButton("开始体验"), Is.Not.Null);
            Assert.That(fixture.FindButton("操作说明"), Is.Not.Null);
            Assert.That(fixture.FindButton("设置"), Is.Not.Null);
            Assert.That(fixture.FindButton("退出"), Is.Not.Null);
            Assert.That(fixture.MainMenu.IsVisible, Is.True);
            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.MainMenu));
        }

        [UnityTest]
        public IEnumerator StartExperienceShowsExactGuidanceThenEntersKnowledgeGate()
        {
            using var fixture = MenuFixture.Create();

            fixture.ClickButton("开始体验");
            yield return null;

            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(fixture.MainMenu.IsVisible, Is.False);
            Assert.That(fixture.Guidance.IsVisible, Is.True);
            Assert.That(fixture.GuidanceCopy, Is.EqualTo(GuidancePresenter.RequiredCopy));
            Assert.That(fixture.FindButton("开始装配"), Is.Not.Null);

            fixture.ClickButton("开始装配");
            yield return null;

            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.KnowledgeGate));
            Assert.That(fixture.Guidance.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator OperationInstructionsReturnToMenuWithoutStartingRun()
        {
            using var fixture = MenuFixture.Create();

            fixture.ClickButton("操作说明");
            yield return null;

            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.MainMenu));
            Assert.That(fixture.Guidance.IsVisible, Is.True);
            Assert.That(fixture.FindButton("返回主菜单"), Is.Not.Null);
            fixture.ClickButton("返回主菜单");
            yield return null;

            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.MainMenu));
            Assert.That(fixture.MainMenu.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator SettingsContainGraphicsAndWindowModeOnlyAndApplyInMemory()
        {
            using var fixture = MenuFixture.Create();
            fixture.OpenSettings();
            yield return null;

            Assert.That(fixture.HasControl("画质"), Is.True);
            Assert.That(fixture.HasControl("窗口模式"), Is.True);
            Assert.That(fixture.HasControl("音乐"), Is.False);
            Assert.That(fixture.HasControl("音效"), Is.False);
            Assert.That(fixture.Settings.QualityOptionCount, Is.EqualTo(3));
            Assert.That(fixture.Settings.WindowModeOptionCount, Is.EqualTo(2));
            Assert.That(QualitySettings.names, Is.EqualTo(new[] { "Low", "Medium", "High" }));

            for (var quality = 0; quality < 3; quality++)
            {
                fixture.SelectQuality(quality);
                yield return null;
                Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(quality));
            }

            fixture.SelectWindowMode(0);
            yield return null;
            Assert.That(Screen.fullScreenMode, Is.EqualTo(FullScreenMode.Windowed));
            Assert.That(fixture.Settings.SelectedQuality, Is.EqualTo(2));
            Assert.That(fixture.Settings.SelectedWindowMode, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ResetRequiresConfirmationThenReturnsToGuidanceAndClearsVisuals()
        {
            using var fixture = MenuFixture.CreateWithProgress();
            var phaseBeforeReset = fixture.Phase;
            var visualsBeforeReset = fixture.InstalledVisualCount;
            var installedVisual = fixture.FirstInstalledVisual;
            Assert.That(installedVisual, Is.Not.Null);

            fixture.ClickButton("重置流程");
            yield return null;

            Assert.That(fixture.Reset.IsConfirmationVisible, Is.True);
            Assert.That(fixture.ResetConfirmationCopy, Is.EqualTo(ResetPresenter.RequiredConfirmationCopy));
            Assert.That(fixture.Phase, Is.EqualTo(phaseBeforeReset));
            Assert.That(fixture.InstalledVisualCount, Is.EqualTo(visualsBeforeReset));

            fixture.ClickButton("取消");
            yield return null;
            Assert.That(fixture.Reset.IsConfirmationVisible, Is.False);
            Assert.That(fixture.Phase, Is.EqualTo(phaseBeforeReset));

            fixture.ConfirmReset();
            yield return null;

            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(fixture.InstalledVisualCount, Is.EqualTo(0));
            Assert.That(installedVisual == null, Is.True,
                "The installed runtime object should be destroyed by the confirmed reset.");
            Assert.That(fixture.CurrentModule, Is.Null);
            Assert.That(fixture.QuestionsAnswered, Is.EqualTo(0));
            Assert.That(fixture.CompletedUniqueSteps, Is.EqualTo(0));
            Assert.That(fixture.CommissioningAttempt, Is.EqualTo(0));
            Assert.That(fixture.Guidance.IsVisible, Is.True);
            Assert.That(fixture.MainMenu.IsVisible, Is.False);
            Assert.That(fixture.FindButton("开始装配"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator UiConfirmedResetCancelsAnActiveDrag()
        {
            using var fixture = MenuFixture.CreateWithProgress();
            fixture.UnlockCurrentStepAndBeginDrag();
            Assert.That(fixture.IsPartDragActive, Is.True);

            fixture.ConfirmReset();
            yield return null;

            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(fixture.IsPartDragActive, Is.False);
            Assert.That(fixture.InstalledVisualCount, Is.EqualTo(0));
            Assert.That(fixture.CurrentModule, Is.Null);
        }

        [UnityTest]
        public IEnumerator ExternalRestartUsesTheSameGuidanceDestination()
        {
            using var fixture = MenuFixture.CreateWithProgress();

            fixture.ResetControllerDirectly();
            yield return null;

            Assert.That(fixture.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(fixture.Guidance.IsVisible, Is.True);
            Assert.That(fixture.MainMenu.IsVisible, Is.False);
            Assert.That(fixture.FindButton("开始装配"), Is.Not.Null);
        }
    }
}
