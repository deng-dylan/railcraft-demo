using System;
using System.Collections;
using System.Collections.Generic;
using RailCraft.Content;
using RailCraft.Flow;
using UnityEngine;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class QuizPresenter : MonoBehaviour
    {
        [SerializeField] private QuizView serializedView;
        [SerializeField] private float transitionDuration = 0.2f;

        private GuidedFlowMachine machine;
        private IQuizView view;
        private readonly Dictionary<string, QuestionDefinition> questions =
            new Dictionary<string, QuestionDefinition>(StringComparer.Ordinal);
        private StepDefinition currentStep;
        private int currentQuestionIndex;
        private bool stepUnlockedRaised;
        private bool transitionActive;
        private bool pendingCorrect;
        private bool pendingFinalQuestion;
        private bool isRenderingQuestion;
        private Coroutine transition;

        public event Action<string> StepUnlocked;

        public StepDefinition CurrentStep => currentStep;
        public int CurrentQuestionIndex => currentQuestionIndex;
        public QuestionDefinition CurrentQuestion => ResolveCurrentQuestion();
        public bool IsTransitioning => transitionActive;

        public void ConfigureView(QuizView configuredView, float configuredTransitionDuration = 0.2f)
        {
            serializedView = configuredView;
            transitionDuration = Mathf.Max(0f, configuredTransitionDuration);
        }

        public void Configure(GuidedFlowMachine configuredMachine, ContentBundle content,
            IQuizView configuredView = null, float configuredTransitionDuration = 0.2f)
        {
            if (configuredMachine == null)
                throw new ArgumentNullException(nameof(configuredMachine));
            if (content?.Questions == null)
                throw new ArgumentNullException(nameof(content));

            StopTransition();
            UnsubscribeView();
            machine = configuredMachine;
            view = configuredView ?? serializedView;
            if (view == null)
                throw new InvalidOperationException("QuizPresenter requires a quiz view.");
            transitionDuration = Mathf.Max(0f, configuredTransitionDuration);
            questions.Clear();
            foreach (var question in content.Questions)
                questions.Add(question.id, question);
            currentStep = null;
            currentQuestionIndex = 0;
            stepUnlockedRaised = false;
            view.OptionSelected += HandleOptionSelected;
        }

        public void ShowStep(StepDefinition step)
        {
            if (machine == null || view == null)
                throw new InvalidOperationException("QuizPresenter must be configured before showing a step.");
            if (step?.questionIds == null || step.questionIds.Length == 0)
                throw new ArgumentException("A quiz step must contain at least one question.", nameof(step));
            if (machine.Snapshot.Phase != FlowPhase.KnowledgeGate
                || !string.Equals(machine.Snapshot.CurrentStepId, step.id, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "QuizPresenter can only show the state machine's active knowledge-gate step.");

            StopTransition();
            currentStep = step;
            currentQuestionIndex = machine.Snapshot.QuestionIndex;
            if (currentQuestionIndex < 0 || currentQuestionIndex >= step.questionIds.Length)
                throw new InvalidOperationException("The state machine question index is outside the active step.");
            stepUnlockedRaised = false;
            ShowCurrentQuestion();
        }

        public void CancelAndHide()
        {
            StopTransition();
            currentStep = null;
            currentQuestionIndex = 0;
            stepUnlockedRaised = false;
            view?.Hide();
        }

        public void SubmitAnswer(int optionIndex)
        {
            HandleOptionSelected(optionIndex);
        }

        private void HandleOptionSelected(int optionIndex)
        {
            if (transitionActive || currentStep == null || stepUnlockedRaised
                || !isActiveAndEnabled || !gameObject.activeInHierarchy
                || machine.Snapshot.Phase != FlowPhase.KnowledgeGate)
                return;

            var snapshot = machine.Snapshot;
            if (!string.Equals(snapshot.CurrentStepId, currentStep.id, StringComparison.Ordinal))
                return;
            if (snapshot.QuestionIndex != currentQuestionIndex)
            {
                currentQuestionIndex = snapshot.QuestionIndex;
                ShowCurrentQuestion();
                return;
            }

            var question = ResolveCurrentQuestion();
            if (question == null)
                throw new InvalidOperationException("The active quiz question is missing from the content bundle.");

            view.SetOptionsInteractable(false);
            var result = machine.SubmitAnswer(optionIndex);
            if (!string.Equals(result.QuestionId, question.id, StringComparison.Ordinal))
                throw new InvalidOperationException("Quiz view and state machine answered different questions.");
            if (!result.IsCorrect)
            {
                view.SetFeedback("回答错误，请重新选择。");
                BeginTransition(false, false);
                return;
            }

            view.SetFeedback("回答正确。");
            var finalQuestion = machine.Snapshot.Phase == FlowPhase.StepReady;
            if (!finalQuestion)
                currentQuestionIndex = machine.Snapshot.QuestionIndex;
            BeginTransition(true, finalQuestion);
        }

        private void BeginTransition(bool correct, bool finalQuestion)
        {
            transitionActive = true;
            pendingCorrect = correct;
            pendingFinalQuestion = finalQuestion;
            if (transitionDuration <= 0f)
            {
                CompleteTransition(correct, finalQuestion);
                return;
            }
            transition = StartCoroutine(TransitionAfterDelay(correct, finalQuestion));
        }

        private IEnumerator TransitionAfterDelay(bool correct, bool finalQuestion)
        {
            yield return new WaitForSecondsRealtime(transitionDuration);
            CompleteTransition(correct, finalQuestion);
        }

        private void CompleteTransition(bool correct, bool finalQuestion)
        {
            transition = null;
            transitionActive = false;
            pendingCorrect = false;
            pendingFinalQuestion = false;
            if (!correct)
            {
                view.SetOptionsInteractable(true);
                return;
            }

            if (!finalQuestion)
            {
                ShowCurrentQuestion();
                return;
            }

            view.Hide();
            if (stepUnlockedRaised)
                return;
            stepUnlockedRaised = true;
            StepUnlocked?.Invoke(currentStep.id);
        }

        private void ShowCurrentQuestion()
        {
            if (isRenderingQuestion)
                return;
            var question = ResolveCurrentQuestion();
            if (question == null)
                throw new InvalidOperationException("The active quiz question is missing from the content bundle.");
            isRenderingQuestion = true;
            try
            {
                view.ShowQuestion(currentStep.displayName, currentQuestionIndex + 1,
                    currentStep.questionIds.Length, question);
            }
            finally
            {
                isRenderingQuestion = false;
            }
        }

        private QuestionDefinition ResolveCurrentQuestion()
        {
            if (currentStep?.questionIds == null
                || currentQuestionIndex < 0
                || currentQuestionIndex >= currentStep.questionIds.Length)
                return null;
            return questions.TryGetValue(currentStep.questionIds[currentQuestionIndex], out var question)
                ? question
                : null;
        }

        private void StopTransition(bool clearPending = true)
        {
            if (transition != null)
                StopCoroutine(transition);
            transition = null;
            transitionActive = false;
            if (clearPending)
            {
                pendingCorrect = false;
                pendingFinalQuestion = false;
            }
        }

        private void UnsubscribeView()
        {
            if (view != null)
                view.OptionSelected -= HandleOptionSelected;
        }

        private void OnDestroy()
        {
            StopTransition();
            UnsubscribeView();
        }

        private void OnDisable()
        {
            StopTransition(false);
        }

        private void OnEnable()
        {
            if (machine == null || view == null || currentStep == null || stepUnlockedRaised)
                return;

            var snapshot = machine.Snapshot;
            if (pendingCorrect && pendingFinalQuestion
                && snapshot.Phase == FlowPhase.StepReady
                && string.Equals(snapshot.CurrentStepId, currentStep.id, StringComparison.Ordinal))
            {
                CompleteTransition(true, true);
                return;
            }

            pendingCorrect = false;
            pendingFinalQuestion = false;
            if (snapshot.Phase != FlowPhase.KnowledgeGate
                || !string.Equals(snapshot.CurrentStepId, currentStep.id, StringComparison.Ordinal))
                return;
            currentQuestionIndex = snapshot.QuestionIndex;
            ShowCurrentQuestion();
        }
    }
}
