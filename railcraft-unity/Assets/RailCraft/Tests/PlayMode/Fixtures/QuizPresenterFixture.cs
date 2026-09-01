using System;
using RailCraft.Content;
using RailCraft.Flow;
using RailCraft.Presentation;
using UnityEngine;

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class QuizPresenterFixture : IDisposable
    {
        private readonly GameObject presenterObject;

        public QuizPresenter Presenter { get; }
        public MemoryQuizView View { get; }
        public StepDefinition Step { get; }
        public GuidedFlowMachine Machine { get; }
        public int StepUnlockedCount { get; private set; }

        private QuizPresenterFixture(GameObject presenterObject, QuizPresenter presenter,
            MemoryQuizView view, StepDefinition step, GuidedFlowMachine machine)
        {
            this.presenterObject = presenterObject;
            Presenter = presenter;
            View = view;
            Step = step;
            Machine = machine;
            Presenter.StepUnlocked += _ => StepUnlockedCount++;
        }

        public static QuizPresenterFixture Create(float transitionDuration = 0f)
        {
            var questions = new[]
            {
                new QuestionDefinition
                {
                    id = "quiz_1",
                    type = "single_choice",
                    prompt = "四选一测试题",
                    options = new[] { "A", "B", "C", "D" },
                    correctOptionIndex = 1
                },
                new QuestionDefinition
                {
                    id = "quiz_2",
                    type = "true_false",
                    prompt = "判断题",
                    options = new[] { "正确", "错误" },
                    correctOptionIndex = 0
                }
            };
            var step = new StepDefinition
            {
                id = "quiz_step",
                order = 1,
                displayName = "知识准备测试",
                phase = "bogie_assembly",
                assetKey = "module.frame",
                dropTargetId = "target.frame",
                questionIds = new[] { "quiz_1", "quiz_2" }
            };
            var content = new ContentBundle(questions, new FlowDefinition
            {
                schemaVersion = 1,
                contentVersion = "quiz-fixture",
                failFirstCommissioning = true,
                steps = new[] { step }
            });
            var machine = new GuidedFlowMachine(content);
            machine.StartNewRun();
            machine.ConfirmGuidance();

            var presenterObject = new GameObject("quiz.presenter");
            var presenter = presenterObject.AddComponent<QuizPresenter>();
            var view = new MemoryQuizView();
            presenter.Configure(machine, content, view, transitionDuration);
            presenter.ShowStep(step);
            return new QuizPresenterFixture(presenterObject, presenter, view, step, machine);
        }

        public static QuizPresenterFixture CreateAtFinalQuestion()
        {
            var fixture = Create();
            fixture.ClickCorrectAnswer();
            return fixture;
        }

        public void ClickWrongAnswer()
        {
            var correct = Presenter.CurrentQuestion.correctOptionIndex;
            View.Click((correct + 1) % Presenter.CurrentQuestion.options.Length);
        }

        public void ClickCorrectAnswer()
        {
            View.Click(Presenter.CurrentQuestion.correctOptionIndex);
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(presenterObject);
        }
    }

    internal sealed class MemoryQuizView : IQuizView
    {
        public event Action<int> OptionSelected;

        public bool IsVisible { get; private set; }
        public bool AreOptionsInteractable { get; private set; }
        public string StageNameText { get; private set; }
        public string QuestionCounterText { get; private set; }
        public string PromptText { get; private set; }
        public string FeedbackText { get; private set; }
        public int OptionCount { get; private set; }

        public void ShowQuestion(string stageName, int questionNumber, int questionCount,
            QuestionDefinition question)
        {
            IsVisible = true;
            StageNameText = stageName;
            QuestionCounterText = $"知识准备题 {questionNumber}/{questionCount}";
            PromptText = question.prompt;
            FeedbackText = string.Empty;
            OptionCount = question.options.Length;
            AreOptionsInteractable = true;
        }

        public void SetFeedback(string message)
        {
            FeedbackText = message ?? string.Empty;
        }

        public void SetOptionsInteractable(bool interactable)
        {
            AreOptionsInteractable = interactable;
        }

        public void Hide()
        {
            IsVisible = false;
            AreOptionsInteractable = false;
        }

        public void Click(int optionIndex)
        {
            if (IsVisible && AreOptionsInteractable)
                OptionSelected?.Invoke(optionIndex);
        }
    }
}
