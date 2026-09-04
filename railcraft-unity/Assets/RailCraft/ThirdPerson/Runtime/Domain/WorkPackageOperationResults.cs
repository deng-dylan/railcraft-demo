using System.Collections.Generic;

namespace RailCraft.ThirdPerson.Domain
{
    /// <summary>
    /// Outcome of answering a question through a material work-package gate.
    /// The underlying quiz result is retained so world/UI adapters can reuse
    /// the existing answer feedback without inferring state from a package.
    /// </summary>
    public enum WorkPackageAnswerStatus
    {
        Correct,
        Incorrect,
        InvalidOption,
        UnknownQuestion,
        QuestionNotInPackage,
        UnknownWorkPackage,
        NotMaterialPackage
    }

    public sealed class WorkPackageAnswerResult
    {
        internal WorkPackageAnswerResult(
            WorkPackageAnswerStatus status,
            WorkPackageId workPackageId,
            string questionId,
            QuizSubmissionResult quizResult,
            bool knowledgeComplete,
            bool packageUnlocked,
            int answeredQuestionCount,
            int requiredQuestionCount,
            int unlockedPartCount,
            int requiredPartCount)
        {
            Status = status;
            WorkPackageId = workPackageId;
            QuestionId = questionId ?? string.Empty;
            QuizResult = quizResult;
            KnowledgeComplete = knowledgeComplete;
            PackageNewlyUnlocked = packageUnlocked;
            AnsweredQuestionCount = answeredQuestionCount;
            RequiredQuestionCount = requiredQuestionCount;
            UnlockedPartCount = unlockedPartCount;
            RequiredPartCount = requiredPartCount;
        }

        public WorkPackageAnswerStatus Status { get; }
        public WorkPackageId WorkPackageId { get; }
        public WorkPackageId PackageId => WorkPackageId;
        public string QuestionId { get; }
        public QuizSubmissionResult QuizResult { get; }
        public QuizSubmissionResult SubmissionResult => QuizResult;
        public bool IsCorrect => Status == WorkPackageAnswerStatus.Correct;
        public bool Accepted => IsCorrect;
        public bool KnowledgeComplete { get; }
        public bool IsKnowledgeComplete => KnowledgeComplete;
        /// <summary>True only when this answer performed the unlock transition.</summary>
        public bool PackageNewlyUnlocked { get; }
        // Compatibility aliases used by the current station presentation.
        public bool PackageUnlocked => PackageNewlyUnlocked;
        public bool RewardUnlocked => PackageNewlyUnlocked;
        /// <summary>Current package state after evaluating this answer.</summary>
        public bool IsPackageUnlocked => RequiredPartCount > 0 &&
            UnlockedPartCount >= RequiredPartCount;
        /// <summary>True when a valid answer changed attempts/history statistics.</summary>
        public bool Changed => Status == WorkPackageAnswerStatus.Correct ||
            Status == WorkPackageAnswerStatus.Incorrect;
        public int AnsweredQuestionCount { get; }
        public int RequiredQuestionCount { get; }
        public int UnlockedPartCount { get; }
        public int RequiredPartCount { get; }
        public int CorrectOptionIndex => QuizResult?.CorrectOptionIndex ?? -1;
        public PartId? RewardPart => QuizResult?.RewardPart;
    }

    public enum WorkPackageCollectionStatus
    {
        Collected,
        AlreadyCollected,
        Locked,
        UnknownWorkPackage,
        NotMaterialPackage,
        InvalidState
    }

    public sealed class WorkPackageCollectionResult
    {
        internal WorkPackageCollectionResult(
            WorkPackageCollectionStatus status,
            WorkPackageId workPackageId,
            IReadOnlyList<PartId> collectedParts,
            int collectedPartCount,
            int requiredPartCount)
        {
            Status = status;
            WorkPackageId = workPackageId;
            var copy = collectedParts == null
                ? new List<PartId>()
                : new List<PartId>(collectedParts);
            CollectedParts = copy.AsReadOnly();
            CollectedPartCount = collectedPartCount;
            RequiredPartCount = requiredPartCount;
        }

        public WorkPackageCollectionStatus Status { get; }
        public WorkPackageId WorkPackageId { get; }
        public WorkPackageId PackageId => WorkPackageId;
        public IReadOnlyList<PartId> CollectedParts { get; }
        public IReadOnlyList<PartId> Parts => CollectedParts;
        public int CollectedPartCount { get; }
        public int Count => CollectedPartCount;
        public int RequiredPartCount { get; }
        public bool IsComplete => CollectedPartCount >= RequiredPartCount && RequiredPartCount > 0;
        public bool IsPackageComplete => IsComplete;
        public bool Changed => Status == WorkPackageCollectionStatus.Collected;
        public bool Accepted => Status == WorkPackageCollectionStatus.Collected
            || Status == WorkPackageCollectionStatus.AlreadyCollected;
    }

    public enum WorkPackageInstallationStatus
    {
        Installed,
        AlreadyInstalled,
        MissingFromInventory,
        PackageNotInModule,
        UnknownWorkPackage,
        NotMaterialPackage,
        UnknownModule,
        InvalidState
    }

    public sealed class WorkPackageInstallationResult
    {
        internal WorkPackageInstallationResult(
            WorkPackageInstallationStatus status,
            WorkPackageId workPackageId,
            ModuleId moduleId,
            IReadOnlyList<PartId> installedParts,
            int installedPartCount,
            int requiredPartCount,
            bool isModuleComplete)
        {
            Status = status;
            WorkPackageId = workPackageId;
            ModuleId = moduleId;
            var copy = installedParts == null
                ? new List<PartId>()
                : new List<PartId>(installedParts);
            InstalledParts = copy.AsReadOnly();
            InstalledPartCount = installedPartCount;
            RequiredPartCount = requiredPartCount;
            IsModuleComplete = isModuleComplete;
        }

        public WorkPackageInstallationStatus Status { get; }
        public WorkPackageId WorkPackageId { get; }
        public WorkPackageId PackageId => WorkPackageId;
        public ModuleId ModuleId { get; }
        public IReadOnlyList<PartId> InstalledParts { get; }
        public IReadOnlyList<PartId> Parts => InstalledParts;
        public int InstalledPartCount { get; }
        public int Count => InstalledPartCount;
        public int RequiredPartCount { get; }
        public bool IsModuleComplete { get; }
        public bool IsPackageComplete => InstalledPartCount >= RequiredPartCount && RequiredPartCount > 0;
        public bool Changed => Status == WorkPackageInstallationStatus.Installed;
        public bool Accepted => Status == WorkPackageInstallationStatus.Installed
            || Status == WorkPackageInstallationStatus.AlreadyInstalled;
    }
}
