using System;

namespace RailCraft.ThirdPerson.Domain
{
    public enum AssemblyFlowStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public enum EngineerGrade
    {
        JuniorEngineer,
        IntermediateEngineer,
        SeniorEngineer
    }

    public sealed class SessionProgressSummary
    {
        internal SessionProgressSummary(
            AssemblyFlowStatus flowStatus,
            DateTimeOffset? startedAtUtc,
            DateTimeOffset? completedAtUtc,
            TimeSpan elapsedTime,
            int answerAttemptCount,
            int correctAnswerCount,
            double answerAccuracy,
            int score,
            EngineerGrade engineerGrade,
            string engineerGradeDisplayName)
        {
            FlowStatus = flowStatus;
            StartedAtUtc = startedAtUtc;
            CompletedAtUtc = completedAtUtc;
            ElapsedTime = elapsedTime;
            AnswerAttemptCount = answerAttemptCount;
            CorrectAnswerCount = correctAnswerCount;
            AnswerAccuracy = answerAccuracy;
            Score = score;
            EngineerGrade = engineerGrade;
            EngineerGradeDisplayName = engineerGradeDisplayName;
        }

        public AssemblyFlowStatus FlowStatus { get; }
        public DateTimeOffset? StartedAtUtc { get; }
        public DateTimeOffset? CompletedAtUtc { get; }
        public TimeSpan ElapsedTime { get; }
        public int AnswerAttemptCount { get; }
        public int CorrectAnswerCount { get; }
        public double AnswerAccuracy { get; }
        public double AnswerAccuracyPercent => AnswerAccuracy * 100d;
        public int Score { get; }
        public EngineerGrade EngineerGrade { get; }
        public string EngineerGradeDisplayName { get; }
    }

    [Serializable]
    public sealed class ModuleAssemblySnapshot
    {
        public ModuleId ModuleId;
        public PartId[] InstalledParts = Array.Empty<PartId>();
        public ModuleId[] InstalledModules = Array.Empty<ModuleId>();
    }

    [Serializable]
    public sealed class WhiteboxGameSessionSnapshot
    {
        public const int CurrentSchemaVersion = 1;
        public const long MissingTimestamp = -1L;

        public int SchemaVersion = CurrentSchemaVersion;
        public AssemblyFlowStatus FlowStatus = AssemblyFlowStatus.Pending;
        public long StartedAtUnixMilliseconds = MissingTimestamp;
        public long CompletedAtUnixMilliseconds = MissingTimestamp;
        public long PausedAtUnixMilliseconds = MissingTimestamp;
        public int AnswerAttemptCount;
        public int CorrectAnswerCount;
        public string[] CorrectQuestionIds = Array.Empty<string>();
        public PartId[] UnlockedParts = Array.Empty<PartId>();
        public PartId[] CollectedParts = Array.Empty<PartId>();
        public PartId[] InventoryParts = Array.Empty<PartId>();
        public ModuleAssemblySnapshot[] Modules = Array.Empty<ModuleAssemblySnapshot>();
        public CommissioningPhase CommissioningPhase = CommissioningPhase.Locked;
        public bool InitialTestAttempted;

        // Added without changing the schema number so old v0.2 saves remain
        // readable. JsonUtility leaves this field at the default FuxingDemo
        // value when loading an older save.
        public AssemblyVariantId AssemblyVariant = AssemblyVariantId.FuxingDemo;
    }
}
