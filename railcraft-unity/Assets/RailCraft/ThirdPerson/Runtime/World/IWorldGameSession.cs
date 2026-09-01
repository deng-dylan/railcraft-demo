using System.Collections.Generic;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Narrow port used by scene behaviours. Tests can inject a deterministic fake,
    /// while production delegates all progression rules to the domain session.
    /// </summary>
    public interface IWorldGameSession
    {
        IReadOnlyList<PartId> InventoryParts { get; }
        bool AreAllModulesComplete { get; }
        bool IsLandingComplete { get; }
        bool IsVehicleComplete { get; }
        CommissioningPhase CommissioningPhase { get; }
        AssemblyFlowStatus FlowStatus { get; }
        SessionProgressSummary Progress { get; }
        bool IsTimingPaused { get; }

        WorldAnswerResult SubmitAnswer(string questionId, int selectedOptionIndex);
        WorldCollectionResult CollectPart(PartId partId);
        WorldPartInstallResult InstallPart(ModuleId moduleId, PartId partId);
        WorldModuleInstallResult InstallModule(ModuleId targetModuleId, ModuleId childModuleId);
        WorldCommissioningResult RunCommissioning();
        WorldCommissioningResult PerformRetuning();
        WorldCommissioningResult PerformInspection();
        bool InventoryContains(PartId partId);
        bool IsPartInstalled(ModuleId moduleId, PartId partId);
        bool IsModuleComplete(ModuleId moduleId);
        bool IsModuleInstalled(ModuleId targetModuleId, ModuleId childModuleId);
        WhiteboxGameSessionSnapshot ExportSnapshot();
        void RestoreSnapshot(WhiteboxGameSessionSnapshot snapshot);
        void PauseTiming();
        void ResumeTiming();
        void Reset();
    }
}
