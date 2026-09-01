using System.Collections;
using NUnit.Framework;
using RailCraft.Flow;
using RailCraft.Presentation;
using RailCraft.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RailCraft.Tests.PlayMode
{
    public sealed class QuizPresenterTests
    {
        [UnityTest]
        public IEnumerator WrongAnswerShowsRetryAndKeepsPanelOpen()
        {
            using var fixture = QuizPresenterFixture.Create();

            fixture.ClickWrongAnswer();
            yield return null;

            Assert.That(fixture.View.FeedbackText, Is.EqualTo("回答错误，请重新选择。"));
            Assert.That(fixture.View.IsVisible, Is.True);
            Assert.That(fixture.Presenter.CurrentQuestionIndex, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FinalCorrectAnswerHidesQuizAndUnlocksStepOnce()
        {
            using var fixture = QuizPresenterFixture.CreateAtFinalQuestion();

            fixture.ClickCorrectAnswer();
            fixture.ClickCorrectAnswer();
            yield return null;

            Assert.That(fixture.View.IsVisible, Is.False);
            Assert.That(fixture.StepUnlockedCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PresenterRendersFourAndTwoOptionQuestionsInOrder()
        {
            using var fixture = QuizPresenterFixture.Create();
            Assert.That(fixture.View.OptionCount, Is.EqualTo(4));
            Assert.That(fixture.View.QuestionCounterText, Is.EqualTo("知识准备题 1/2"));

            fixture.ClickCorrectAnswer();
            yield return null;

            Assert.That(fixture.View.OptionCount, Is.EqualTo(2));
            Assert.That(fixture.View.QuestionCounterText, Is.EqualTo("知识准备题 2/2"));
        }

        [UnityTest]
        public IEnumerator OptionsStayDisabledDuringConfiguredTransition()
        {
            using var fixture = QuizPresenterFixture.Create(0.05f);

            fixture.ClickWrongAnswer();
            Assert.That(fixture.View.AreOptionsInteractable, Is.False);
            yield return new WaitForSecondsRealtime(0.02f);
            Assert.That(fixture.View.AreOptionsInteractable, Is.False);
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(fixture.View.AreOptionsInteractable, Is.True);
        }

        [UnityTest]
        public IEnumerator RepeatedShowStepResumesAtStateMachineQuestionIndex()
        {
            using var fixture = QuizPresenterFixture.Create(0.05f);

            fixture.ClickCorrectAnswer();
            fixture.Presenter.ShowStep(fixture.Step);
            yield return null;

            Assert.That(fixture.Machine.Snapshot.QuestionIndex, Is.EqualTo(1));
            Assert.That(fixture.Presenter.CurrentQuestionIndex, Is.EqualTo(1));
            Assert.That(fixture.View.PromptText, Is.EqualTo("判断题"));
        }

        [UnityTest]
        public IEnumerator DisabledPresenterCannotAdvanceTheStateMachine()
        {
            using var fixture = QuizPresenterFixture.Create();

            fixture.Presenter.enabled = false;
            fixture.ClickCorrectAnswer();
            yield return null;

            Assert.That(fixture.Machine.Snapshot.Phase, Is.EqualTo(FlowPhase.KnowledgeGate));
            Assert.That(fixture.Machine.Snapshot.QuestionIndex, Is.EqualTo(0));
            fixture.Presenter.enabled = true;
            yield return null;
            Assert.That(fixture.View.AreOptionsInteractable, Is.True);
        }

        [UnityTest]
        public IEnumerator DisablingDuringFinalTransitionRecoversOneUnlockOnEnable()
        {
            using var fixture = QuizPresenterFixture.Create(0.05f);
            fixture.ClickCorrectAnswer();
            yield return new WaitForSecondsRealtime(0.07f);
            Assert.That(fixture.Presenter.CurrentQuestionIndex, Is.EqualTo(1));

            fixture.ClickCorrectAnswer();
            fixture.Presenter.enabled = false;
            yield return new WaitForSecondsRealtime(0.07f);
            Assert.That(fixture.StepUnlockedCount, Is.EqualTo(0));

            fixture.Presenter.enabled = true;
            yield return null;
            Assert.That(fixture.StepUnlockedCount, Is.EqualTo(1));
            Assert.That(fixture.View.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator StepHudShowsSecondCommissioningWithoutRepeatingKnowledgeCount()
        {
            var root = new GameObject("hud.fixture");
            try
            {
                var stage = CreateText(root.transform, "StageNameText");
                var progress = CreateText(root.transform, "ProgressText");
                var knowledge = CreateText(root.transform, "KnowledgeText");
                var hint = CreateText(root.transform, "HintText");
                var view = root.AddComponent<StepHudView>();
                view.Configure(stage, progress, knowledge, hint);

                view.Show(null, 13, 4, 4, true);
                yield return null;

                Assert.That(view.StageNameText, Is.EqualTo("当前阶段：再次调试"));
                Assert.That(view.KnowledgeText, Is.EqualTo("知识准备：4/4"));
                Assert.That(view.HintText, Is.EqualTo("操作提示：拖动高亮模块到发光接口"));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        private static Text CreateText(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.AddComponent<Text>();
        }
    }
}
