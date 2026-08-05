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

        public WhiteboxGameSession()
            : this(WhiteboxGameCatalog.CreateDefault())
        {
        }

        public WhiteboxGameSession(WhiteboxGameCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Inventory = new PartInventory();
            CommissioningState = new CommissioningState();
            foreach (var definition in Catalog.Modules)
                moduleStates.Add(definition.Id, new ModuleAssemblyState(definition));
        }

        public WhiteboxGameCatalog Catalog { get; }
        public PartInventory Inventory { get; }
        public CommissioningState CommissioningState { get; }
        public CommissioningPhase CommissioningPhase => CommissioningState.Phase;

        public IReadOnlyList<PartId> UnlockedParts => PartSnapshot(unlockedParts);
        public IReadOnlyList<PartId> CollectedParts => PartSnapshot(collectedParts);
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

            if (!question.IsCorrectOption(selectedOptionIndex))
            {
                return new QuizSubmissionResult(
                    QuizSubmissionStatus.Incorrect,
                    question.Id,
                    question.RewardPart,
                    false,
                    question.CorrectOptionIndex);
            }

            var rewardUnlocked = unlockedParts.Add(question.RewardPart);
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
                    CommissioningState.InitialTestAttempted = true;
                    CommissioningState.Phase = CommissioningPhase.NeedsRetuning;
                    return CommissioningResult(CommissioningStatus.Failed, true, false);

                case CommissioningPhase.ReadyForRetest:
                    CommissioningState.Phase = CommissioningPhase.InService;
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

            CommissioningState.Phase = CommissioningPhase.ReadyForRetest;
            return CommissioningResult(CommissioningStatus.Inspected, true, false);
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
    }
}
