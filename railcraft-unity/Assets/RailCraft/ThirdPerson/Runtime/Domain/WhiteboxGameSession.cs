using System;
using System.Collections.Generic;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class WhiteboxGameSession
    {
        private readonly Dictionary<ModuleId, ModuleAssemblyState> moduleStates =
            new Dictionary<ModuleId, ModuleAssemblyState>();
        private readonly HashSet<PartId> unlockedParts = new HashSet<PartId>();
        private readonly HashSet<PartId> collectedParts = new HashSet<PartId>();
        private readonly HashSet<string> correctQuestionIds = new HashSet<string>(
            StringComparer.Ordinal);
        private readonly Func<DateTimeOffset> utcNowProvider;

        public WhiteboxGameSession()
            : this(WhiteboxGameCatalog.CreateDefault())
        {
        }

        public WhiteboxGameSession(WhiteboxGameCatalog catalog)
            : this(catalog, () => DateTimeOffset.UtcNow)
        {
        }

        public WhiteboxGameSession(
            WhiteboxGameCatalog catalog,
            Func<DateTimeOffset> utcNowProvider)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.utcNowProvider = utcNowProvider ??
                throw new ArgumentNullException(nameof(utcNowProvider));
            Inventory = new PartInventory();
            CommissioningState = new CommissioningState();
            foreach (var definition in Catalog.Modules)
                moduleStates.Add(definition.Id, new ModuleAssemblyState(definition));
        }

        public WhiteboxGameCatalog Catalog { get; }
        public PartInventory Inventory { get; }
        public CommissioningState CommissioningState { get; }
        public CommissioningPhase CommissioningPhase => CommissioningState.Phase;

        public AssemblyFlowStatus FlowStatus { get; private set; } = AssemblyFlowStatus.Pending;
        public DateTimeOffset? StartedAtUtc { get; private set; }
        public DateTimeOffset? CompletedAtUtc { get; private set; }
        public DateTimeOffset? PausedAtUtc { get; private set; }
        public bool IsTimingPaused => PausedAtUtc.HasValue;
        public int AnswerAttemptCount { get; private set; }
        public int CorrectAnswerCount { get; private set; }
        public double AnswerAccuracy =>
            AnswerAttemptCount == 0 ? 0d : (double)CorrectAnswerCount / AnswerAttemptCount;
        public double AnswerAccuracyPercent => AnswerAccuracy * 100d;
        public int Score => (int)Math.Round(
            AnswerAccuracyPercent,
            MidpointRounding.AwayFromZero);

        public EngineerGrade Grade
        {
            get
            {
                if (Score >= 90)
                    return global::RailCraft.ThirdPerson.Domain.EngineerGrade.SeniorEngineer;
                if (Score >= 70)
                    return global::RailCraft.ThirdPerson.Domain.EngineerGrade.IntermediateEngineer;
                return global::RailCraft.ThirdPerson.Domain.EngineerGrade.JuniorEngineer;
            }
        }

        public EngineerGrade EngineerGrade => Grade;

        public string EngineerGradeDisplayName
        {
            get
            {
                switch (Grade)
                {
                    case global::RailCraft.ThirdPerson.Domain.EngineerGrade.SeniorEngineer:
                        return "高级工程师";
                    case global::RailCraft.ThirdPerson.Domain.EngineerGrade.IntermediateEngineer:
                        return "中级工程师";
                    default:
                        return "初级工程师";
                }
            }
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                if (!StartedAtUtc.HasValue)
                    return TimeSpan.Zero;

                var end = CompletedAtUtc ?? PausedAtUtc ?? GetUtcNow();
                if (end < StartedAtUtc.Value)
                    return TimeSpan.Zero;
                return end - StartedAtUtc.Value;
            }
        }

        public SessionProgressSummary Progress => new SessionProgressSummary(
            FlowStatus,
            StartedAtUtc,
            CompletedAtUtc,
            ElapsedTime,
            AnswerAttemptCount,
            CorrectAnswerCount,
            AnswerAccuracy,
            Score,
            Grade,
            EngineerGradeDisplayName);

        public IReadOnlyList<PartId> UnlockedParts => PartSnapshot(unlockedParts);
        public IReadOnlyList<PartId> CollectedParts => PartSnapshot(collectedParts);
        public IReadOnlyList<string> CorrectQuestionIds => QuestionSnapshot(correctQuestionIds);
        public IReadOnlyList<ModuleId> InstalledVehicleModules =>
            GetModuleState(ModuleId.Landing).InstalledModules;

        public bool AreAllModulesComplete
        {
            get
            {
                foreach (var state in moduleStates.Values)
                {
                    if (!state.IsComplete)
                        return false;
                }
                return true;
            }
        }

        public bool CanBeginFinalAssembly
        {
            get
            {
                var landing = Catalog.GetModule(ModuleId.Landing);
                foreach (var childModuleId in landing.RequiredModules)
                {
                    if (!IsModuleComplete(childModuleId))
                        return false;
                }
                return true;
            }
        }

        public bool IsLandingComplete => IsModuleComplete(ModuleId.Landing);
        public bool IsVehicleComplete => CommissioningState.IsInService;

        public QuizSubmissionResult SubmitAnswer(string questionId, int selectedOptionIndex)
        {
            if (!Catalog.TryGetQuestion(questionId, out var question))
            {
                return new QuizSubmissionResult(
                    QuizSubmissionStatus.UnknownQuestion,
                    questionId,
                    null,
                    false,
                    -1);
            }

            if (!question.IsValidOption(selectedOptionIndex))
            {
                return new QuizSubmissionResult(
                    QuizSubmissionStatus.InvalidOption,
                    question.Id,
                    question.RewardPart,
                    false,
                    question.CorrectOptionIndex);
            }

            var isCorrect = question.IsCorrectOption(selectedOptionIndex);
            RecordAnswer(isCorrect);
            if (!isCorrect)
            {
                return new QuizSubmissionResult(
                    QuizSubmissionStatus.Incorrect,
                    question.Id,
                    question.RewardPart,
                    false,
                    question.CorrectOptionIndex);
            }

            var rewardUnlocked = unlockedParts.Add(question.RewardPart);
            if (FlowStatus != AssemblyFlowStatus.Completed)
                correctQuestionIds.Add(question.Id);
            return new QuizSubmissionResult(
                QuizSubmissionStatus.Correct,
                question.Id,
                question.RewardPart,
                rewardUnlocked,
                question.CorrectOptionIndex);
        }

        public PartCollectionResult CollectPart(PartId partId)
        {
            if (!Catalog.TryGetPart(partId, out _))
                return new PartCollectionResult(PartCollectionStatus.UnknownPart, partId);
            if (!unlockedParts.Contains(partId))
                return new PartCollectionResult(PartCollectionStatus.Locked, partId);
            if (collectedParts.Contains(partId))
                return new PartCollectionResult(PartCollectionStatus.AlreadyCollected, partId);

            if (!Inventory.Grant(partId))
                throw new InvalidOperationException($"Inventory already contains unclaimed part {partId}.");

            collectedParts.Add(partId);
            BeginProgress();
            return new PartCollectionResult(PartCollectionStatus.Collected, partId);
        }

        public PartInstallationResult InstallPart(ModuleId moduleId, PartId partId)
        {
            if (!moduleStates.TryGetValue(moduleId, out var state))
            {
                return new PartInstallationResult(
                    PartInstallationStatus.UnknownModule,
                    moduleId,
                    partId,
                    false);
            }

            if (!Catalog.TryGetPart(partId, out _))
            {
                return new PartInstallationResult(
                    PartInstallationStatus.UnknownPart,
                    moduleId,
                    partId,
                    state.IsComplete);
            }

            if (!state.Definition.Requires(partId))
            {
                return new PartInstallationResult(
                    PartInstallationStatus.PartNotInRecipe,
                    moduleId,
                    partId,
                    state.IsComplete);
            }

            if (state.HasInstalled(partId))
            {
                return new PartInstallationResult(
                    PartInstallationStatus.AlreadyInstalled,
                    moduleId,
                    partId,
                    state.IsComplete);
            }

            if (!Inventory.Contains(partId))
            {
                return new PartInstallationResult(
                    PartInstallationStatus.MissingFromInventory,
                    moduleId,
                    partId,
                    state.IsComplete);
            }

            if (!Inventory.Consume(partId))
                throw new InvalidOperationException($"Failed to consume inventory part {partId}.");
            if (!state.Install(partId))
                throw new InvalidOperationException($"Failed to install part {partId} in module {moduleId}.");

            BeginProgress();
            UpdateCommissioningAvailability();
            return new PartInstallationResult(
                PartInstallationStatus.Installed,
                moduleId,
                partId,
                state.IsComplete);
        }

        public ModuleInstallationResult InstallModule(
            ModuleId targetModuleId,
            ModuleId childModuleId)
        {
            if (!moduleStates.TryGetValue(targetModuleId, out var targetState))
            {
                return ModuleResult(
                    ModuleInstallationStatus.UnknownTargetModule,
                    targetModuleId,
                    childModuleId,
                    false);
            }

            if (!moduleStates.TryGetValue(childModuleId, out var childState))
            {
                return ModuleResult(
                    ModuleInstallationStatus.UnknownChildModule,
                    targetModuleId,
                    childModuleId,
                    targetState.IsComplete);
            }

            if (!targetState.Definition.Requires(childModuleId))
            {
                return ModuleResult(
                    ModuleInstallationStatus.ModuleNotInRecipe,
                    targetModuleId,
                    childModuleId,
                    targetState.IsComplete);
            }

            if (targetState.HasInstalled(childModuleId))
            {
                return ModuleResult(
                    ModuleInstallationStatus.AlreadyInstalled,
                    targetModuleId,
                    childModuleId,
                    targetState.IsComplete);
            }

            if (!childState.IsComplete)
            {
                return ModuleResult(
                    ModuleInstallationStatus.ChildModuleIncomplete,
                    targetModuleId,
                    childModuleId,
                    targetState.IsComplete);
            }

            if (!targetState.Install(childModuleId))
                throw new InvalidOperationException(
                    $"Failed to install child module {childModuleId} in module {targetModuleId}.");

            BeginProgress();
            UpdateCommissioningAvailability();
            return ModuleResult(
                ModuleInstallationStatus.Installed,
                targetModuleId,
                childModuleId,
                targetState.IsComplete);
        }

        // Compatibility entry point for adapters that previously installed a module
        // directly into the final vehicle. The assembly DAG guarantees one parent.
        public ModuleInstallationResult InstallModule(ModuleId moduleId)
        {
            if (!moduleStates.ContainsKey(moduleId))
            {
                return new ModuleInstallationResult(
                    ModuleInstallationStatus.UnknownModule,
                    moduleId,
                    moduleId,
                    false,
                    IsVehicleComplete);
            }

            if (TryGetParentModule(moduleId, out var parentModuleId))
                return InstallModule(parentModuleId, moduleId);

            return ModuleResult(
                ModuleInstallationStatus.ModuleNotInRecipe,
                moduleId,
                moduleId,
                IsModuleComplete(moduleId));
        }

        public CommissioningResult RunCommissioning()
        {
            UpdateCommissioningAvailability();
            if (!IsLandingComplete)
                return CommissioningResult(CommissioningStatus.AssemblyIncomplete, false, false);

            switch (CommissioningState.Phase)
            {
                case CommissioningPhase.ReadyForInitialTest:
                    BeginProgress();
                    CommissioningState.InitialTestAttempted = true;
                    CommissioningState.Phase = CommissioningPhase.NeedsRetuning;
                    return CommissioningResult(CommissioningStatus.Failed, true, false);

                case CommissioningPhase.ReadyForRetest:
                    CommissioningState.Phase = CommissioningPhase.InService;
                    CompleteProgress();
                    return CommissioningResult(CommissioningStatus.Passed, true, true);

                case CommissioningPhase.InService:
                    return CommissioningResult(CommissioningStatus.Passed, false, true);

                default:
                    return CommissioningResult(CommissioningStatus.InvalidPhase, false, false);
            }
        }

        public CommissioningResult PerformRetuning()
        {
            UpdateCommissioningAvailability();
            if (!IsLandingComplete)
                return CommissioningResult(CommissioningStatus.AssemblyIncomplete, false, false);
            if (CommissioningState.Phase != CommissioningPhase.NeedsRetuning)
                return CommissioningResult(CommissioningStatus.InvalidPhase, false, false);

            BeginProgress();
            CommissioningState.Phase = CommissioningPhase.ReadyForInspection;
            return CommissioningResult(CommissioningStatus.Retuned, true, false);
        }

        public CommissioningResult PerformInspection()
        {
            UpdateCommissioningAvailability();
            if (!IsLandingComplete)
                return CommissioningResult(CommissioningStatus.AssemblyIncomplete, false, false);
            if (CommissioningState.Phase != CommissioningPhase.ReadyForInspection)
                return CommissioningResult(CommissioningStatus.InvalidPhase, false, false);

            BeginProgress();
            CommissioningState.Phase = CommissioningPhase.ReadyForRetest;
            return CommissioningResult(CommissioningStatus.Inspected, true, false);
        }

        public WhiteboxGameSessionSnapshot ExportSnapshot()
        {
            var moduleSnapshots = new ModuleAssemblySnapshot[Catalog.Modules.Count];
            for (var index = 0; index < Catalog.Modules.Count; index++)
            {
                var state = GetModuleState(Catalog.Modules[index].Id);
                moduleSnapshots[index] = new ModuleAssemblySnapshot
                {
                    ModuleId = state.Definition.Id,
                    InstalledParts = CopyToArray(state.InstalledParts),
                    InstalledModules = CopyToArray(state.InstalledModules)
                };
            }

            return new WhiteboxGameSessionSnapshot
            {
                SchemaVersion = WhiteboxGameSessionSnapshot.CurrentSchemaVersion,
                FlowStatus = FlowStatus,
                StartedAtUnixMilliseconds = EncodeTimestamp(StartedAtUtc),
                CompletedAtUnixMilliseconds = EncodeTimestamp(CompletedAtUtc),
                PausedAtUnixMilliseconds = EncodeTimestamp(PausedAtUtc),
                AnswerAttemptCount = AnswerAttemptCount,
                CorrectAnswerCount = CorrectAnswerCount,
                CorrectQuestionIds = CopyToArray(CorrectQuestionIds),
                UnlockedParts = CopyToArray(UnlockedParts),
                CollectedParts = CopyToArray(CollectedParts),
                InventoryParts = CopyToArray(Inventory.Parts),
                Modules = moduleSnapshots,
                CommissioningPhase = CommissioningState.Phase,
                InitialTestAttempted = CommissioningState.InitialTestAttempted
            };
        }

        public void RestoreSnapshot(WhiteboxGameSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var candidate = new WhiteboxGameSession(Catalog, utcNowProvider);
            candidate.ApplySnapshot(snapshot);
            CopyStateFrom(candidate);
        }

        public static WhiteboxGameSession FromSnapshot(WhiteboxGameSessionSnapshot snapshot)
        {
            var session = new WhiteboxGameSession();
            session.RestoreSnapshot(snapshot);
            return session;
        }

        public ModuleAssemblyState GetModuleState(ModuleId moduleId)
        {
            if (!moduleStates.TryGetValue(moduleId, out var state))
                throw new KeyNotFoundException($"Unknown module id: {moduleId}.");
            return state;
        }

        public bool TryGetModuleState(ModuleId moduleId, out ModuleAssemblyState state)
        {
            return moduleStates.TryGetValue(moduleId, out state);
        }

        public bool IsPartUnlocked(PartId partId)
        {
            return unlockedParts.Contains(partId);
        }

        public bool IsPartCollected(PartId partId)
        {
            return collectedParts.Contains(partId);
        }

        public bool IsModuleComplete(ModuleId moduleId)
        {
            return moduleStates.TryGetValue(moduleId, out var state) && state.IsComplete;
        }

        public bool IsModuleInstalled(ModuleId targetModuleId, ModuleId childModuleId)
        {
            return moduleStates.TryGetValue(targetModuleId, out var state) &&
                   state.HasInstalled(childModuleId);
        }

        public bool IsModuleInstalled(ModuleId moduleId)
        {
            if (moduleId == ModuleId.Landing)
                return IsLandingComplete;
            return TryGetParentModule(moduleId, out var parentModuleId) &&
                   IsModuleInstalled(parentModuleId, moduleId);
        }

        public void Reset()
        {
            unlockedParts.Clear();
            collectedParts.Clear();
            Inventory.Reset();
            CommissioningState.Reset();
            foreach (var state in moduleStates.Values)
                state.Reset();

            FlowStatus = AssemblyFlowStatus.Pending;
            StartedAtUtc = null;
            CompletedAtUtc = null;
            PausedAtUtc = null;
            AnswerAttemptCount = 0;
            CorrectAnswerCount = 0;
            correctQuestionIds.Clear();
        }

        private void ApplySnapshot(WhiteboxGameSessionSnapshot snapshot)
        {
            if (snapshot.SchemaVersion != WhiteboxGameSessionSnapshot.CurrentSchemaVersion)
            {
                throw new ArgumentException(
                    $"Unsupported session snapshot schema: {snapshot.SchemaVersion}.",
                    nameof(snapshot));
            }

            if (!Enum.IsDefined(typeof(AssemblyFlowStatus), snapshot.FlowStatus))
                throw new ArgumentException("Snapshot has an unknown flow status.", nameof(snapshot));
            if (!Enum.IsDefined(typeof(CommissioningPhase), snapshot.CommissioningPhase))
                throw new ArgumentException("Snapshot has an unknown commissioning phase.", nameof(snapshot));
            if (snapshot.AnswerAttemptCount < 0 || snapshot.CorrectAnswerCount < 0 ||
                snapshot.CorrectAnswerCount > snapshot.AnswerAttemptCount)
            {
                throw new ArgumentException("Snapshot answer statistics are invalid.", nameof(snapshot));
            }

            AddPartSet(snapshot.UnlockedParts, unlockedParts, "unlocked parts", snapshot);
            AddPartSet(snapshot.CollectedParts, collectedParts, "collected parts", snapshot);
            AddCorrectQuestionSet(snapshot.CorrectQuestionIds, snapshot);
            AddInventory(snapshot.InventoryParts, snapshot);
            ApplyModuleSnapshots(snapshot.Modules, snapshot);

            foreach (var partId in collectedParts)
            {
                if (!unlockedParts.Contains(partId))
                    throw new ArgumentException("Collected parts must be unlocked.", nameof(snapshot));
            }

            ValidateMaterialAccounting(snapshot);
            ValidateAssemblyDependencies(snapshot);

            CommissioningState.Phase = snapshot.CommissioningPhase;
            CommissioningState.InitialTestAttempted = snapshot.InitialTestAttempted;
            FlowStatus = snapshot.FlowStatus;
            StartedAtUtc = DecodeTimestamp(snapshot.StartedAtUnixMilliseconds, "start", snapshot);
            CompletedAtUtc = DecodeTimestamp(snapshot.CompletedAtUnixMilliseconds, "completion", snapshot);
            PausedAtUtc = DecodeTimestamp(snapshot.PausedAtUnixMilliseconds, "pause", snapshot);
            AnswerAttemptCount = snapshot.AnswerAttemptCount;
            CorrectAnswerCount = snapshot.CorrectAnswerCount;

            if (CorrectAnswerCount < unlockedParts.Count)
            {
                throw new ArgumentException(
                    "Snapshot has fewer correct answers than distinct unlocked parts.",
                    nameof(snapshot));
            }
            if (CorrectAnswerCount < correctQuestionIds.Count)
            {
                throw new ArgumentException(
                    "Snapshot has fewer correct answers than recorded question ids.",
                    nameof(snapshot));
            }

            ValidateRestoredProgress(snapshot);
        }

        public void PauseTiming()
        {
            if (FlowStatus == AssemblyFlowStatus.InProgress && !PausedAtUtc.HasValue)
                PausedAtUtc = GetUtcNow();
        }

        public void ResumeTiming()
        {
            if (FlowStatus != AssemblyFlowStatus.InProgress || !PausedAtUtc.HasValue)
                return;

            var resumedAt = GetUtcNow();
            if (StartedAtUtc.HasValue && resumedAt > PausedAtUtc.Value)
                StartedAtUtc = StartedAtUtc.Value + (resumedAt - PausedAtUtc.Value);
            PausedAtUtc = null;
        }

        private void AddPartSet(
            PartId[] parts,
            HashSet<PartId> destination,
            string label,
            WhiteboxGameSessionSnapshot snapshot)
        {
            if (parts == null)
                throw new ArgumentException($"Snapshot {label} cannot be null.", nameof(snapshot));

            foreach (var partId in parts)
            {
                if (!Catalog.TryGetPart(partId, out _))
                    throw new ArgumentException($"Snapshot {label} contains unknown part {partId}.", nameof(snapshot));
                if (!destination.Add(partId))
                    throw new ArgumentException($"Snapshot {label} contains duplicate part {partId}.", nameof(snapshot));
            }
        }

        private void AddCorrectQuestionSet(
            string[] questionIds,
            WhiteboxGameSessionSnapshot snapshot)
        {
            // This field was added compatibly to schema 1. Older JSON saves omit it.
            if (questionIds == null)
                return;

            foreach (var questionId in questionIds)
            {
                if (string.IsNullOrWhiteSpace(questionId) ||
                    !Catalog.TryGetQuestion(questionId, out var question))
                {
                    throw new ArgumentException(
                        $"Snapshot contains unknown correct question id: {questionId}.",
                        nameof(snapshot));
                }
                if (!unlockedParts.Contains(question.RewardPart))
                {
                    throw new ArgumentException(
                        "Recorded correct questions must have an unlocked reward part.",
                        nameof(snapshot));
                }
                if (!correctQuestionIds.Add(question.Id))
                {
                    throw new ArgumentException(
                        $"Snapshot contains duplicate correct question id: {question.Id}.",
                        nameof(snapshot));
                }
            }
        }

        private void AddInventory(
            PartId[] parts,
            WhiteboxGameSessionSnapshot snapshot)
        {
            if (parts == null)
                throw new ArgumentException("Snapshot inventory cannot be null.", nameof(snapshot));

            foreach (var partId in parts)
            {
                if (!Catalog.TryGetPart(partId, out _))
                    throw new ArgumentException($"Snapshot inventory contains unknown part {partId}.", nameof(snapshot));
                if (!Inventory.Grant(partId))
                    throw new ArgumentException($"Snapshot inventory contains duplicate part {partId}.", nameof(snapshot));
            }
        }

        private void ApplyModuleSnapshots(
            ModuleAssemblySnapshot[] snapshots,
            WhiteboxGameSessionSnapshot owner)
        {
            if (snapshots == null)
                throw new ArgumentException("Snapshot modules cannot be null.", nameof(owner));
            if (snapshots.Length != Catalog.Modules.Count)
                throw new ArgumentException("Snapshot must contain every assembly node exactly once.", nameof(owner));

            var seenModules = new HashSet<ModuleId>();
            foreach (var snapshot in snapshots)
            {
                if (snapshot == null)
                    throw new ArgumentException("Snapshot modules cannot contain null entries.", nameof(owner));
                if (!moduleStates.TryGetValue(snapshot.ModuleId, out var state))
                    throw new ArgumentException($"Snapshot contains unknown module {snapshot.ModuleId}.", nameof(owner));
                if (!seenModules.Add(snapshot.ModuleId))
                    throw new ArgumentException($"Snapshot contains duplicate module {snapshot.ModuleId}.", nameof(owner));
                if (snapshot.InstalledParts == null || snapshot.InstalledModules == null)
                    throw new ArgumentException("Snapshot module inputs cannot be null.", nameof(owner));

                foreach (var partId in snapshot.InstalledParts)
                {
                    if (!Catalog.TryGetPart(partId, out _))
                        throw new ArgumentException($"Snapshot contains unknown installed part {partId}.", nameof(owner));
                    if (!state.Definition.Requires(partId))
                        throw new ArgumentException($"Part {partId} is not in module {snapshot.ModuleId} recipe.", nameof(owner));
                    if (!state.Install(partId))
                        throw new ArgumentException($"Module {snapshot.ModuleId} repeats part {partId}.", nameof(owner));
                }

                foreach (var childModuleId in snapshot.InstalledModules)
                {
                    if (!moduleStates.ContainsKey(childModuleId))
                        throw new ArgumentException($"Snapshot contains unknown child module {childModuleId}.", nameof(owner));
                    if (!state.Definition.Requires(childModuleId))
                    {
                        throw new ArgumentException(
                            $"Module {childModuleId} is not in module {snapshot.ModuleId} recipe.",
                            nameof(owner));
                    }
                    if (!state.Install(childModuleId))
                        throw new ArgumentException($"Module {snapshot.ModuleId} repeats child {childModuleId}.", nameof(owner));
                }
            }
        }

        private void ValidateMaterialAccounting(WhiteboxGameSessionSnapshot snapshot)
        {
            var installedParts = new HashSet<PartId>();
            foreach (var state in moduleStates.Values)
            {
                foreach (var partId in state.InstalledParts)
                {
                    if (!installedParts.Add(partId))
                        throw new ArgumentException($"Part {partId} is installed more than once.", nameof(snapshot));
                    if (!collectedParts.Contains(partId))
                        throw new ArgumentException($"Installed part {partId} was not collected.", nameof(snapshot));
                    if (Inventory.Contains(partId))
                        throw new ArgumentException($"Installed part {partId} also appears in inventory.", nameof(snapshot));
                }
            }

            foreach (var partId in Inventory.Parts)
            {
                if (!collectedParts.Contains(partId))
                    throw new ArgumentException($"Inventory part {partId} was not collected.", nameof(snapshot));
            }

            foreach (var partId in collectedParts)
            {
                if (!Inventory.Contains(partId) && !installedParts.Contains(partId))
                    throw new ArgumentException($"Collected part {partId} has no inventory or assembly location.", nameof(snapshot));
            }
        }

        private void ValidateAssemblyDependencies(WhiteboxGameSessionSnapshot snapshot)
        {
            foreach (var state in moduleStates.Values)
            {
                foreach (var childModuleId in state.InstalledModules)
                {
                    if (!moduleStates[childModuleId].IsComplete)
                    {
                        throw new ArgumentException(
                            $"Installed child module {childModuleId} is incomplete.",
                            nameof(snapshot));
                    }
                }
            }
        }

        private void ValidateRestoredProgress(WhiteboxGameSessionSnapshot snapshot)
        {
            var landingComplete = IsLandingComplete;
            if (landingComplete && CommissioningState.Phase == CommissioningPhase.Locked)
                throw new ArgumentException("Completed landing must unlock commissioning.", nameof(snapshot));
            if (!landingComplete && CommissioningState.Phase != CommissioningPhase.Locked)
                throw new ArgumentException("Commissioning requires a completed landing assembly.", nameof(snapshot));

            var expectedInitialAttempt = CommissioningState.Phase == CommissioningPhase.NeedsRetuning ||
                                         CommissioningState.Phase == CommissioningPhase.ReadyForInspection ||
                                         CommissioningState.Phase == CommissioningPhase.ReadyForRetest ||
                                         CommissioningState.Phase == CommissioningPhase.InService;
            if (CommissioningState.InitialTestAttempted != expectedInitialAttempt)
                throw new ArgumentException("Snapshot commissioning attempt flag is inconsistent.", nameof(snapshot));

            switch (FlowStatus)
            {
                case AssemblyFlowStatus.Pending:
                    if (StartedAtUtc.HasValue || CompletedAtUtc.HasValue || PausedAtUtc.HasValue ||
                        HasGameplayProgress())
                        throw new ArgumentException("Pending snapshot contains gameplay progress.", nameof(snapshot));
                    break;

                case AssemblyFlowStatus.InProgress:
                    if (!StartedAtUtc.HasValue || CompletedAtUtc.HasValue ||
                        CommissioningState.Phase == CommissioningPhase.InService ||
                        !HasGameplayProgress() ||
                        (PausedAtUtc.HasValue && PausedAtUtc.Value < StartedAtUtc.Value))
                    {
                        throw new ArgumentException("In-progress snapshot is inconsistent.", nameof(snapshot));
                    }
                    break;

                case AssemblyFlowStatus.Completed:
                    if (!StartedAtUtc.HasValue || !CompletedAtUtc.HasValue ||
                        PausedAtUtc.HasValue ||
                        CompletedAtUtc.Value < StartedAtUtc.Value ||
                        CommissioningState.Phase != CommissioningPhase.InService ||
                        !landingComplete)
                    {
                        throw new ArgumentException("Completed snapshot is inconsistent.", nameof(snapshot));
                    }
                    break;
            }
        }

        private bool HasGameplayProgress()
        {
            if (AnswerAttemptCount > 0 || unlockedParts.Count > 0 || collectedParts.Count > 0 ||
                Inventory.Count > 0 || CommissioningState.Phase != CommissioningPhase.Locked)
            {
                return true;
            }

            foreach (var state in moduleStates.Values)
            {
                if (state.InstalledInputCount > 0)
                    return true;
            }
            return false;
        }

        private void CopyStateFrom(WhiteboxGameSession source)
        {
            Reset();
            foreach (var partId in source.unlockedParts)
                unlockedParts.Add(partId);
            foreach (var partId in source.collectedParts)
                collectedParts.Add(partId);
            foreach (var questionId in source.correctQuestionIds)
                correctQuestionIds.Add(questionId);
            foreach (var partId in source.Inventory.Parts)
                Inventory.Grant(partId);

            foreach (var definition in Catalog.Modules)
            {
                var sourceState = source.GetModuleState(definition.Id);
                var targetState = GetModuleState(definition.Id);
                foreach (var partId in sourceState.InstalledParts)
                    targetState.Install(partId);
                foreach (var childModuleId in sourceState.InstalledModules)
                    targetState.Install(childModuleId);
            }

            CommissioningState.Phase = source.CommissioningState.Phase;
            CommissioningState.InitialTestAttempted = source.CommissioningState.InitialTestAttempted;
            FlowStatus = source.FlowStatus;
            StartedAtUtc = source.StartedAtUtc;
            CompletedAtUtc = source.CompletedAtUtc;
            PausedAtUtc = source.PausedAtUtc;
            AnswerAttemptCount = source.AnswerAttemptCount;
            CorrectAnswerCount = source.CorrectAnswerCount;
        }

        private ModuleInstallationResult ModuleResult(
            ModuleInstallationStatus status,
            ModuleId targetModuleId,
            ModuleId childModuleId,
            bool isTargetModuleComplete)
        {
            return new ModuleInstallationResult(
                status,
                targetModuleId,
                childModuleId,
                isTargetModuleComplete,
                IsVehicleComplete);
        }

        private CommissioningResult CommissioningResult(
            CommissioningStatus status,
            bool changed,
            bool passed)
        {
            return new CommissioningResult(
                status,
                changed,
                CommissioningState.Phase,
                passed);
        }

        private void RecordAnswer(bool isCorrect)
        {
            if (FlowStatus == AssemblyFlowStatus.Completed)
                return;

            BeginProgress();
            AnswerAttemptCount++;
            if (isCorrect)
                CorrectAnswerCount++;
        }

        private void BeginProgress()
        {
            if (FlowStatus != AssemblyFlowStatus.Pending)
                return;

            StartedAtUtc = GetUtcNow();
            CompletedAtUtc = null;
            PausedAtUtc = null;
            FlowStatus = AssemblyFlowStatus.InProgress;
        }

        private void CompleteProgress()
        {
            ResumeTiming();
            var completedAt = GetUtcNow();
            if (!StartedAtUtc.HasValue)
                StartedAtUtc = completedAt;
            if (completedAt < StartedAtUtc.Value)
                completedAt = StartedAtUtc.Value;

            CompletedAtUtc = completedAt;
            FlowStatus = AssemblyFlowStatus.Completed;
        }

        private DateTimeOffset GetUtcNow()
        {
            return utcNowProvider().ToUniversalTime();
        }

        private void UpdateCommissioningAvailability()
        {
            if (IsLandingComplete)
                CommissioningState.Unlock();
        }

        private bool TryGetParentModule(ModuleId childModuleId, out ModuleId parentModuleId)
        {
            foreach (var definition in Catalog.Modules)
            {
                if (definition.Requires(childModuleId))
                {
                    parentModuleId = definition.Id;
                    return true;
                }
            }

            parentModuleId = default;
            return false;
        }

        private IReadOnlyList<PartId> PartSnapshot(HashSet<PartId> source)
        {
            var snapshot = new List<PartId>();
            foreach (var definition in Catalog.Parts)
            {
                if (source.Contains(definition.Id))
                    snapshot.Add(definition.Id);
            }
            return snapshot.AsReadOnly();
        }

        private IReadOnlyList<string> QuestionSnapshot(HashSet<string> source)
        {
            var snapshot = new List<string>();
            foreach (var question in Catalog.Questions)
            {
                if (source.Contains(question.Id))
                    snapshot.Add(question.Id);
            }
            return snapshot.AsReadOnly();
        }

        private static T[] CopyToArray<T>(IReadOnlyList<T> source)
        {
            var copy = new T[source.Count];
            for (var index = 0; index < source.Count; index++)
                copy[index] = source[index];
            return copy;
        }

        private static long EncodeTimestamp(DateTimeOffset? timestamp)
        {
            return timestamp.HasValue
                ? timestamp.Value.ToUnixTimeMilliseconds()
                : WhiteboxGameSessionSnapshot.MissingTimestamp;
        }

        private static DateTimeOffset? DecodeTimestamp(
            long value,
            string label,
            WhiteboxGameSessionSnapshot snapshot)
        {
            if (value == WhiteboxGameSessionSnapshot.MissingTimestamp)
                return null;
            if (value < 0)
                throw new ArgumentException($"Snapshot {label} timestamp is invalid.", nameof(snapshot));

            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(value);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new ArgumentException(
                    $"Snapshot {label} timestamp is outside the supported range.",
                    nameof(snapshot),
                    exception);
            }
        }
    }
}
