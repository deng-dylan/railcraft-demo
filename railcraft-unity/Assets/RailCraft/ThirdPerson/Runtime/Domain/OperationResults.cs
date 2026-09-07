namespace RailCraft.ThirdPerson.Domain
{
    public enum QuizSubmissionStatus
    {
        Correct,
        Incorrect,
        InvalidOption,
        UnknownQuestion
    }

    public sealed class QuizSubmissionResult
    {
        internal QuizSubmissionResult(
            QuizSubmissionStatus status,
            string questionId,
            PartId? rewardPart,
            bool rewardUnlocked,
            int correctOptionIndex)
        {
            Status = status;
            QuestionId = questionId;
            RewardPart = rewardPart;
            RewardUnlocked = rewardUnlocked;
            CorrectOptionIndex = correctOptionIndex;
        }

        public QuizSubmissionStatus Status { get; }
        public string QuestionId { get; }
        public PartId? RewardPart { get; }
        public bool RewardUnlocked { get; }
        public int CorrectOptionIndex { get; }
        public bool IsCorrect => Status == QuizSubmissionStatus.Correct;
    }

    public enum PartCollectionStatus
    {
        Collected,
        AlreadyCollected,
        Locked,
        UnknownPart
    }

    public sealed class PartCollectionResult
    {
        internal PartCollectionResult(PartCollectionStatus status, PartId partId)
        {
            Status = status;
            PartId = partId;
        }

        public PartCollectionStatus Status { get; }
        public PartId PartId { get; }
        public bool Changed => Status == PartCollectionStatus.Collected;
    }

    public enum PartInstallationStatus
    {
        Installed,
        AlreadyInstalled,
        MissingFromInventory,
        PartNotInRecipe,
        UnknownPart,
        UnknownModule
    }

    public sealed class PartInstallationResult
    {
        internal PartInstallationResult(
            PartInstallationStatus status,
            ModuleId moduleId,
            PartId partId,
            bool isModuleComplete)
        {
            Status = status;
            ModuleId = moduleId;
            PartId = partId;
            IsModuleComplete = isModuleComplete;
        }

        public PartInstallationStatus Status { get; }
        public ModuleId ModuleId { get; }
        public PartId PartId { get; }
        public bool IsModuleComplete { get; }
        public bool Changed => Status == PartInstallationStatus.Installed;
    }

    public enum ModuleInstallationStatus
    {
        Installed,
        AlreadyInstalled,
        ChildModuleIncomplete,
        ModuleNotInRecipe,
        UnknownTargetModule,
        UnknownChildModule,
        FinalAssemblyLocked,
        UnknownModule
    }

    public sealed class ModuleInstallationResult
    {
        internal ModuleInstallationResult(
            ModuleInstallationStatus status,
            ModuleId moduleId,
            bool isVehicleComplete)
            : this(status, moduleId, moduleId, false, isVehicleComplete)
        {
        }

        internal ModuleInstallationResult(
            ModuleInstallationStatus status,
            ModuleId targetModuleId,
            ModuleId childModuleId,
            bool isTargetModuleComplete,
            bool isVehicleComplete)
        {
            Status = status;
            TargetModuleId = targetModuleId;
            ChildModuleId = childModuleId;
            IsTargetModuleComplete = isTargetModuleComplete;
            IsVehicleComplete = isVehicleComplete;
        }

        public ModuleInstallationStatus Status { get; }
        public ModuleId TargetModuleId { get; }
        public ModuleId ChildModuleId { get; }
        public ModuleId ModuleId => ChildModuleId;
        public bool IsTargetModuleComplete { get; }
        public bool IsVehicleComplete { get; }
        public bool Changed => Status == ModuleInstallationStatus.Installed;
    }
}
