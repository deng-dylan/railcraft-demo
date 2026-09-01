using System;
using System.Collections.Generic;
using RailCraft.Content;

namespace RailCraft.Flow
{
    public sealed class GuidedFlowMachine
    {
        private readonly ContentBundle content;
        private readonly Dictionary<string, QuestionDefinition> questionsById;
        private FlowPhase phase;
        private int stepIndex;
        private int questionIndex;
        private int commissioningAttempt;

        public GuidedFlowMachine(ContentBundle content)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            questionsById = BuildQuestionLookup(content.Questions);
            phase = FlowPhase.MainMenu;
        }

        public FlowSnapshot Snapshot => new FlowSnapshot(
            phase,
            stepIndex,
            questionIndex,
            CurrentStepId(),
            commissioningAttempt);

        public void StartNewRun()
        {
            if (phase != FlowPhase.MainMenu)
                return;

            phase = FlowPhase.Guidance;
        }

        public void ConfirmGuidance()
        {
            if (phase != FlowPhase.Guidance)
                return;

            phase = FlowPhase.KnowledgeGate;
        }

        public AnswerResult SubmitAnswer(int optionIndex)
        {
            if (phase != FlowPhase.KnowledgeGate || !HasCurrentStep())
                return new AnswerResult(false, null, -1);

            var question = CurrentQuestion();
            if (question == null)
                return new AnswerResult(false, null, -1);

            var isCorrect = optionIndex == question.correctOptionIndex;
            var result = new AnswerResult(isCorrect, question.id, question.correctOptionIndex);
            if (!isCorrect)
                return result;

            questionIndex++;
            if (questionIndex == CurrentStep().questionIds.Length)
            {
                questionIndex = 0;
                phase = FlowPhase.StepReady;
            }

            return result;
        }

        public DropDecision ConfirmDrop(string stepId)
        {
            if (phase != FlowPhase.StepReady || !HasCurrentStep())
                return new DropDecision(false, "not_ready");

            if (!string.Equals(stepId, CurrentStep().id, StringComparison.Ordinal))
                return new DropDecision(false, "wrong_step");

            phase = FlowPhase.Snapping;
            return new DropDecision(true, "accepted");
        }

        public void ConfirmSnapAnimation()
        {
            if (phase != FlowPhase.Snapping || !HasCurrentStep())
                return;

            var stepId = CurrentStep().id;
            if (stepId == "commissioning")
            {
                commissioningAttempt++;
                if (content.Flow.failFirstCommissioning && commissioningAttempt == 1)
                {
                    phase = FlowPhase.Rework;
                    return;
                }

                stepIndex += 2;
                questionIndex = 0;
                phase = FlowPhase.KnowledgeGate;
                return;
            }

            if (stepId == "inspection")
            {
                phase = FlowPhase.SecondCommissioning;
                return;
            }

            if (stepId == "release")
            {
                stepIndex = content.Flow.steps.Length;
                questionIndex = 0;
                phase = FlowPhase.Completed;
                return;
            }

            AdvanceToNextKnowledgeGate();
        }

        public void ConfirmReworkAcknowledged()
        {
            if (phase != FlowPhase.Rework)
                return;

            stepIndex++;
            questionIndex = 0;
            phase = FlowPhase.KnowledgeGate;
        }

        public void CompleteSecondCommissioning()
        {
            if (phase != FlowPhase.SecondCommissioning)
                return;

            commissioningAttempt++;
            stepIndex++;
            questionIndex = 0;
            phase = FlowPhase.KnowledgeGate;
        }

        public void Reset()
        {
            stepIndex = 0;
            questionIndex = 0;
            commissioningAttempt = 0;
            phase = FlowPhase.Guidance;
        }

        private void AdvanceToNextKnowledgeGate()
        {
            stepIndex++;
            questionIndex = 0;
            phase = FlowPhase.KnowledgeGate;
        }

        private bool HasCurrentStep()
        {
            return stepIndex >= 0 && stepIndex < content.Flow.steps.Length;
        }

        private StepDefinition CurrentStep()
        {
            return HasCurrentStep() ? content.Flow.steps[stepIndex] : null;
        }

        private string CurrentStepId()
        {
            if (phase == FlowPhase.MainMenu || phase == FlowPhase.Guidance || phase == FlowPhase.Completed)
                return null;

            var step = CurrentStep();
            return step == null ? null : step.id;
        }

        private QuestionDefinition CurrentQuestion()
        {
            var step = CurrentStep();
            if (step == null || questionIndex < 0 || questionIndex >= step.questionIds.Length)
                return null;

            return questionsById.TryGetValue(step.questionIds[questionIndex], out var question)
                ? question
                : null;
        }

        private static Dictionary<string, QuestionDefinition> BuildQuestionLookup(
            QuestionDefinition[] questions)
        {
            var lookup = new Dictionary<string, QuestionDefinition>(StringComparer.Ordinal);
            foreach (var question in questions)
                lookup.Add(question.id, question);
            return lookup;
        }
    }
}
