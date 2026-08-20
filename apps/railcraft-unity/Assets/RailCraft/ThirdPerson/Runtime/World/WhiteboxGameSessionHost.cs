using System;
using RailCraft.ThirdPerson.Domain;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Scene-level owner for one whitebox play session and its presentation events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiteboxGameSessionHost : MonoBehaviour
    {
        [SerializeField, TextArea] private string initialObjective = "寻找零件工位，答题解锁轨道车辆零件";
        [SerializeField] private AssemblyVariantId initialAssemblyVariant = AssemblyVariantId.FuxingDemo;

        private IWorldGameSession session;
        private bool completionAnnounced;
        private string currentObjective;
        private AssemblyVariantId selectedAssemblyVariant = AssemblyVariantId.FuxingDemo;

        public event Action StateChanged;
        public event Action SessionReset;
        public event Action<WhiteboxAnswerEvaluatedEvent> AnswerEvaluated;
        public event Action<string> FeedbackRequested;
        public event Action<string> ObjectiveChanged;
        public event Action<AssemblyVariantId> AssemblyVariantChanged;
        public event Action VehicleCompleted;
        public event Action<WhiteboxMilestoneEvent> MilestoneReached;

        public IWorldGameSession Session => session ?? (session = new DomainWorldGameSession());
        public AssemblyVariantId SelectedAssemblyVariant => selectedAssemblyVariant;
        public AssemblyVariantDefinition SelectedAssemblyVariantDefinition =>
            AssemblyVariantCatalog.Get(selectedAssemblyVariant);
        public string CurrentObjective => string.IsNullOrWhiteSpace(currentObjective)
            ? initialObjective
            : currentObjective;

        public void Configure(IWorldGameSession configuredSession, string configuredInitialObjective = null)
        {
            Configure(
                configuredSession,
                configuredInitialObjective,
                initialAssemblyVariant);
        }

        public void Configure(
            IWorldGameSession configuredSession,
            string configuredInitialObjective,
            AssemblyVariantId configuredInitialAssemblyVariant)
        {
            session = configuredSession ?? throw new ArgumentNullException(nameof(configuredSession));
            if (configuredInitialObjective != null)
                initialObjective = configuredInitialObjective;
            initialAssemblyVariant = AssemblyVariantCatalog.Clamp(configuredInitialAssemblyVariant);
            selectedAssemblyVariant = initialAssemblyVariant;
            currentObjective = initialObjective;
            completionAnnounced = Session.IsVehicleComplete;
            StateChanged?.Invoke();
            ObjectiveChanged?.Invoke(CurrentObjective);
            AssemblyVariantChanged?.Invoke(selectedAssemblyVariant);
        }

        /// <summary>
        /// Selects the playable vehicle plan. The caller normally invokes this
        /// before starting a new saved session; changing it does not mutate the
        /// domain progress until the caller resets or starts that session.
        /// </summary>
        public void SelectAssemblyVariant(AssemblyVariantId variant)
        {
            var normalized = AssemblyVariantCatalog.Clamp(variant);
            if (selectedAssemblyVariant == normalized)
                return;

            selectedAssemblyVariant = normalized;
            AssemblyVariantChanged?.Invoke(selectedAssemblyVariant);
            StateChanged?.Invoke();
        }

        public WhiteboxGameSessionSnapshot ExportSnapshot()
        {
            var snapshot = Session.ExportSnapshot();
            snapshot.AssemblyVariant = selectedAssemblyVariant;
            return snapshot;
        }

        public WorldAnswerResult SubmitAnswer(string questionId, int selectedOptionIndex)
        {
            var result = Session.SubmitAnswer(questionId, selectedOptionIndex);
            StateChanged?.Invoke();
            AnswerEvaluated?.Invoke(new WhiteboxAnswerEvaluatedEvent(questionId, result));
            return result;
        }

        public WorldCollectionResult CollectPart(PartId partId)
        {
            var result = Session.CollectPart(partId);
            if (result.Changed)
                StateChanged?.Invoke();
            return result;
        }

        public WorldPartInstallResult InstallPart(ModuleId moduleId, PartId partId)
        {
            var result = Session.InstallPart(moduleId, partId);
            if (result.Changed)
            {
                StateChanged?.Invoke();
                MilestoneReached?.Invoke(WhiteboxMilestoneEvent.ForPart(partId, moduleId));
            }
            return result;
        }

        public WorldModuleInstallResult InstallModule(ModuleId targetModuleId, ModuleId childModuleId)
        {
            var result = Session.InstallModule(targetModuleId, childModuleId);
            if (result.Changed)
            {
                StateChanged?.Invoke();
                MilestoneReached?.Invoke(WhiteboxMilestoneEvent.ForModule(childModuleId));
            }
            AnnounceCompletionIfNeeded();
            return result;
        }

        public WorldCommissioningResult RunCommissioning()
        {
            return ApplyCommissioningResult(Session.RunCommissioning());
        }

        public WorldCommissioningResult PerformRetuning()
        {
            return ApplyCommissioningResult(Session.PerformRetuning());
        }

        public WorldCommissioningResult PerformInspection()
        {
            return ApplyCommissioningResult(Session.PerformInspection());
        }

        public void SetObjective(string objective)
        {
            var value = objective ?? string.Empty;
            if (string.Equals(currentObjective, value, StringComparison.Ordinal))
                return;

            currentObjective = value;
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        public void NotifyFeedback(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                FeedbackRequested?.Invoke(message);
        }

        public void ResetSession()
        {
            Session.Reset();
            completionAnnounced = false;
            currentObjective = initialObjective;
            SessionReset?.Invoke();
            StateChanged?.Invoke();
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        public void RestoreSession(WhiteboxGameSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            SelectAssemblyVariant(snapshot.AssemblyVariant);
            Session.RestoreSnapshot(snapshot ?? throw new ArgumentNullException(nameof(snapshot)));
            completionAnnounced = Session.IsVehicleComplete;
            currentObjective = Session.IsVehicleComplete
                ? "调试检验合格，车辆投入使用"
                : "已恢复装配进度，请继续当前流程";
            SessionReset?.Invoke();
            StateChanged?.Invoke();
            ObjectiveChanged?.Invoke(CurrentObjective);
            if (Session.IsVehicleComplete)
                VehicleCompleted?.Invoke();
        }

        private void Awake()
        {
            currentObjective = initialObjective;
            selectedAssemblyVariant = AssemblyVariantCatalog.Clamp(initialAssemblyVariant);
            _ = Session;
        }

        private void AnnounceCompletionIfNeeded()
        {
            if (completionAnnounced || !Session.IsVehicleComplete)
                return;

            completionAnnounced = true;
            SetObjective("调试检验合格，车辆投入使用");
            VehicleCompleted?.Invoke();
        }

        private WorldCommissioningResult ApplyCommissioningResult(WorldCommissioningResult result)
        {
            if (result.Changed)
            {
                StateChanged?.Invoke();
                MilestoneReached?.Invoke(
                    WhiteboxMilestoneEvent.ForCommissioning(result.Phase));
            }
            AnnounceCompletionIfNeeded();
            return result;
        }
    }
}
