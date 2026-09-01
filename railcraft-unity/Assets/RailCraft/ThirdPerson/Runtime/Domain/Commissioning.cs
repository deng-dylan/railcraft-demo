namespace RailCraft.ThirdPerson.Domain
{
    public enum CommissioningPhase
    {
        Locked,
        ReadyForInitialTest,
        NeedsRetuning,
        ReadyForInspection,
        ReadyForRetest,
        InService
    }

    public enum CommissioningStatus
    {
        Failed,
        Passed,
        Retuned,
        Inspected,
        AssemblyIncomplete,
        InvalidPhase
    }

    public sealed class CommissioningState
    {
        internal CommissioningState()
        {
            Reset();
        }

        public CommissioningPhase Phase { get; internal set; }
        public bool InitialTestAttempted { get; internal set; }
        public bool IsInService => Phase == CommissioningPhase.InService;

        internal void Unlock()
        {
            if (Phase == CommissioningPhase.Locked)
                Phase = CommissioningPhase.ReadyForInitialTest;
        }

        internal void Reset()
        {
            Phase = CommissioningPhase.Locked;
            InitialTestAttempted = false;
        }
    }

    public sealed class CommissioningResult
    {
        internal CommissioningResult(
            CommissioningStatus status,
            bool changed,
            CommissioningPhase phase,
            bool passed)
        {
            Status = status;
            Changed = changed;
            Phase = phase;
            Passed = passed;
        }

        public CommissioningStatus Status { get; }
        public bool Changed { get; }
        public CommissioningPhase Phase { get; }
        public bool Passed { get; }
    }
}
