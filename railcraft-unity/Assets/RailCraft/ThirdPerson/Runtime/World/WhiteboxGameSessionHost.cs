using System;
using System.Collections.Generic;
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
        [SerializeField, TextArea] private string initialObjective =
            "执行标准工单 RC-EMU-01：前往知识工位确认零件与装配要求";
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

        /// <summary>
        /// Package-level answer entry point used by the current compact
        /// material flow. The legacy SubmitAnswer method remains available to
        /// old adapters and save-compatible tests.
        /// </summary>
        public WorkPackageAnswerResult SubmitWorkPackageAnswer(
            WorkPackageId workPackageId,
            string questionId,
            int selectedOptionIndex)
        {
            var packageSession = Session as IWorkPackageGameSession;
            if (packageSession == null)
                throw new InvalidOperationException("The configured session does not support work packages.");

            var result = packageSession.SubmitWorkPackageAnswer(
                workPackageId,
                questionId,
                selectedOptionIndex);
            StateChanged?.Invoke();
            if (result.QuizResult != null)
            {
                var quizResult = result.QuizResult;
                var worldResult = new WorldAnswerResult(
                    quizResult.IsCorrect,
                    result.PackageUnlocked,
                    quizResult.CorrectOptionIndex,
                    null,
                    quizResult.Status.ToString());
                AnswerEvaluated?.Invoke(new WhiteboxAnswerEvaluatedEvent(questionId, worldResult));
            }
            return result;
        }

        public WorldCollectionResult CollectPart(PartId partId)
        {
            var result = Session.CollectPart(partId);
            if (result.Changed)
                StateChanged?.Invoke();
            return result;
        }

        public WorkPackageCollectionResult CollectWorkPackage(WorkPackageId workPackageId)
        {
            var packageSession = Session as IWorkPackageGameSession;
            if (packageSession == null)
                throw new InvalidOperationException("The configured session does not support work packages.");

            var result = packageSession.CollectWorkPackage(workPackageId);
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

        public WorkPackageInstallationResult InstallWorkPackage(
            ModuleId moduleId,
            WorkPackageId workPackageId)
        {
            var packageSession = Session as IWorkPackageGameSession;
            if (packageSession == null)
                throw new InvalidOperationException("The configured session does not support work packages.");

            var previouslyInstalled = new HashSet<PartId>();
            if (WhiteboxWorkPackageCatalog.TryGet(workPackageId, out var package))
            {
                foreach (var partId in package.RequiredParts)
                {
                    if (Session.IsPartInstalled(moduleId, partId))
                        previouslyInstalled.Add(partId);
                }
            }

            var result = packageSession.InstallWorkPackage(moduleId, workPackageId);
            if (result.Changed)
            {
                StateChanged?.Invoke();
                foreach (var partId in result.InstalledParts)
                {
                    if (previouslyInstalled.Contains(partId))
                        continue;
                    MilestoneReached?.Invoke(WhiteboxMilestoneEvent.ForPart(partId, moduleId));
                }
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
            // Validate and apply the domain snapshot before changing any
            // presentation state. A rejected save must leave the selected
            // variant and its visual routing untouched.
            Session.RestoreSnapshot(snapshot);
            SelectAssemblyVariant(snapshot.AssemblyVariant);
            completionAnnounced = Session.IsVehicleComplete;
            currentObjective = Session.IsVehicleComplete
                ? "标准实训完成，车辆通过调试检验"
                : "已恢复标准实训进度，请继续当前工位";
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
            SetObjective("标准实训完成，车辆通过调试检验");
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
