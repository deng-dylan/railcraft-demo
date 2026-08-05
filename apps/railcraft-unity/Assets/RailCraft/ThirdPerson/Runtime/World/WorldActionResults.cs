using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.World
{
    public readonly struct WorldAnswerResult
    {
        public WorldAnswerResult(
            bool isCorrect,
            bool rewardUnlocked,
            int correctOptionIndex,
            PartId? rewardPart,
            string status)
        {
            IsCorrect = isCorrect;
            RewardUnlocked = rewardUnlocked;
            CorrectOptionIndex = correctOptionIndex;
            RewardPart = rewardPart;
            Status = status ?? string.Empty;
        }

        public bool IsCorrect { get; }
        public bool RewardUnlocked { get; }
        public int CorrectOptionIndex { get; }
        public PartId? RewardPart { get; }
        public string Status { get; }
    }

    public readonly struct WorldCollectionResult
    {
        public WorldCollectionResult(bool accepted, bool changed, PartId partId, string status)
        {
            Accepted = accepted;
            Changed = changed;
            PartId = partId;
            Status = status ?? string.Empty;
        }

        public bool Accepted { get; }
        public bool Changed { get; }
        public PartId PartId { get; }
        public string Status { get; }
    }

    public readonly struct WorldPartInstallResult
    {
        public WorldPartInstallResult(
            bool accepted,
            bool changed,
            ModuleId moduleId,
            PartId partId,
            bool isModuleComplete,
            string status)
        {
            Accepted = accepted;
            Changed = changed;
            ModuleId = moduleId;
            PartId = partId;
            IsModuleComplete = isModuleComplete;
            Status = status ?? string.Empty;
        }

        public bool Accepted { get; }
        public bool Changed { get; }
        public ModuleId ModuleId { get; }
        public PartId PartId { get; }
        public bool IsModuleComplete { get; }
        public string Status { get; }
    }

    public readonly struct WorldModuleInstallResult
    {
        public WorldModuleInstallResult(
            bool accepted,
            bool changed,
            ModuleId targetModuleId,
            ModuleId childModuleId,
            bool isTargetModuleComplete,
            bool isVehicleComplete,
            string status)
        {
            Accepted = accepted;
            Changed = changed;
            TargetModuleId = targetModuleId;
            ChildModuleId = childModuleId;
            IsTargetModuleComplete = isTargetModuleComplete;
            IsVehicleComplete = isVehicleComplete;
            Status = status ?? string.Empty;
        }

        public bool Accepted { get; }
        public bool Changed { get; }
        public ModuleId TargetModuleId { get; }
        public ModuleId ChildModuleId { get; }
        public ModuleId ModuleId => ChildModuleId;
        public bool IsTargetModuleComplete { get; }
        public bool IsVehicleComplete { get; }
        public string Status { get; }
    }

    public readonly struct WorldCommissioningResult
    {
        public WorldCommissioningResult(
            bool accepted,
            bool changed,
            bool passed,
            CommissioningPhase phase,
            string status)
        {
            Accepted = accepted;
            Changed = changed;
            Passed = passed;
            Phase = phase;
            Status = status ?? string.Empty;
        }

        public bool Accepted { get; }
        public bool Changed { get; }
        public bool Passed { get; }
        public CommissioningPhase Phase { get; }
        public string Status { get; }
    }
}
