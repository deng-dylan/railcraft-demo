using System;
using System.Collections.Generic;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Production adapter. It keeps MonoBehaviours independent from domain result classes
    /// without duplicating any progression rule.
    /// </summary>
    public sealed class DomainWorldGameSession : IWorldGameSession
    {
        private readonly WhiteboxGameSession session;

        public DomainWorldGameSession()
            : this(new WhiteboxGameSession())
        {
        }

        public DomainWorldGameSession(WhiteboxGameSession configuredSession)
        {
            session = configuredSession ?? throw new ArgumentNullException(nameof(configuredSession));
        }

        public WhiteboxGameSession DomainSession => session;
        public IReadOnlyList<PartId> InventoryParts => session.Inventory.Parts;
        public bool AreAllModulesComplete => session.AreAllModulesComplete;
        public bool IsLandingComplete => session.IsLandingComplete;
        public bool IsVehicleComplete => session.IsVehicleComplete;
        public CommissioningPhase CommissioningPhase => session.CommissioningState.Phase;

        public WorldAnswerResult SubmitAnswer(string questionId, int selectedOptionIndex)
        {
            var result = session.SubmitAnswer(questionId, selectedOptionIndex);
            return new WorldAnswerResult(
                result.IsCorrect,
                result.RewardUnlocked,
                result.CorrectOptionIndex,
                result.RewardPart,
                result.Status.ToString());
        }

        public WorldCollectionResult CollectPart(PartId partId)
        {
            var result = session.CollectPart(partId);
            var accepted = result.Status == PartCollectionStatus.Collected
                || result.Status == PartCollectionStatus.AlreadyCollected;
            return new WorldCollectionResult(accepted, result.Changed, result.PartId, result.Status.ToString());
        }

        public WorldPartInstallResult InstallPart(ModuleId moduleId, PartId partId)
        {
            var result = session.InstallPart(moduleId, partId);
            var accepted = result.Status == PartInstallationStatus.Installed
                || result.Status == PartInstallationStatus.AlreadyInstalled;
            return new WorldPartInstallResult(
                accepted,
                result.Changed,
                result.ModuleId,
                result.PartId,
                result.IsModuleComplete,
                result.Status.ToString());
        }

        public WorldModuleInstallResult InstallModule(ModuleId targetModuleId, ModuleId childModuleId)
        {
            var result = session.InstallModule(targetModuleId, childModuleId);
            var accepted = result.Status == ModuleInstallationStatus.Installed
                || result.Status == ModuleInstallationStatus.AlreadyInstalled;
            return new WorldModuleInstallResult(
                accepted,
                result.Changed,
                result.TargetModuleId,
                result.ChildModuleId,
                result.IsTargetModuleComplete,
                result.IsVehicleComplete,
                result.Status.ToString());
        }

        public WorldCommissioningResult RunCommissioning()
        {
            return ConvertCommissioningResult(session.RunCommissioning());
        }

        public WorldCommissioningResult PerformRetuning()
        {
            return ConvertCommissioningResult(session.PerformRetuning());
        }

        public WorldCommissioningResult PerformInspection()
        {
            return ConvertCommissioningResult(session.PerformInspection());
        }

        public bool InventoryContains(PartId partId)
        {
            return session.Inventory.Contains(partId);
        }

        public bool IsPartInstalled(ModuleId moduleId, PartId partId)
        {
            return session.TryGetModuleState(moduleId, out var state) && state.HasInstalled(partId);
        }

        public bool IsModuleComplete(ModuleId moduleId)
        {
            return session.TryGetModuleState(moduleId, out var state) && state.IsComplete;
        }

        public bool IsModuleInstalled(ModuleId targetModuleId, ModuleId childModuleId)
        {
            return session.IsModuleInstalled(targetModuleId, childModuleId);
        }

        public void Reset()
        {
            session.Reset();
        }

        private static WorldCommissioningResult ConvertCommissioningResult(CommissioningResult result)
        {
            var accepted = result.Status != CommissioningStatus.AssemblyIncomplete
                && result.Status != CommissioningStatus.InvalidPhase;
            return new WorldCommissioningResult(
                accepted,
                result.Changed,
                result.Passed,
                result.Phase,
                result.Status.ToString());
        }
    }
}
