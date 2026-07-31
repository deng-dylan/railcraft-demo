namespace RailCraft.Flow
{
    public sealed class FlowSnapshot
    {
        public FlowPhase Phase { get; }
        public int StepIndex { get; }
        public int QuestionIndex { get; }
        public string CurrentStepId { get; }
        public int CommissioningAttempt { get; }

        public FlowSnapshot(
            FlowPhase phase,
            int stepIndex,
            int questionIndex,
            string currentStepId,
            int commissioningAttempt)
        {
            Phase = phase;
            StepIndex = stepIndex;
            QuestionIndex = questionIndex;
            CurrentStepId = currentStepId;
            CommissioningAttempt = commissioningAttempt;
        }
    }
}
