using System;
using System.Collections.Generic;
using System.Text;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Unlocks engineering knowledge from gameplay milestones and presents the popup/catalog views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiteboxKnowledgePresenter : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private ThirdPersonInputLock inputLock;
        [SerializeField] private Button catalogButton;
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Text popupTitleText;
        [SerializeField] private Text popupBodyText;
        [SerializeField] private Button popupCloseButton;
        [SerializeField] private GameObject catalogRoot;
        [SerializeField] private Text catalogBodyText;
        [SerializeField] private Button catalogCloseButton;
        [SerializeField] private Button secondaryCatalogButton;

        private EngineeringKnowledgeCatalog catalog;
        private EngineeringKnowledgeProgress knowledgeProgress;
        private WhiteboxGameCatalog gameCatalog;
        private bool subscribed;
        private bool inputStateCaptured;
        private bool inputWasLocked;
        private readonly List<KnowledgeEntry> pendingAnswerEntries = new List<KnowledgeEntry>();

        public EngineeringKnowledgeProgress Progress => knowledgeProgress;
        public bool IsCatalogUnlocked => knowledgeProgress?.IsCatalogEntranceUnlocked == true;
        public IReadOnlyList<KnowledgeEntry> UnlockedEntries =>
            knowledgeProgress?.UnlockedEntries ?? Array.Empty<KnowledgeEntry>();
        public int PendingPopupCount => pendingAnswerEntries.Count;
        public bool IsAnyViewOpen =>
            popupRoot != null && popupRoot.activeSelf ||
            catalogRoot != null && catalogRoot.activeSelf;

        public void Configure(
            WhiteboxGameSessionHost host,
            ThirdPersonInputLock configuredInputLock,
            Button configuredCatalogButton,
            GameObject configuredPopupRoot,
            Text configuredPopupTitleText,
            Text configuredPopupBodyText,
            Button configuredPopupCloseButton,
            GameObject configuredCatalogRoot,
            Text configuredCatalogBodyText,
            Button configuredCatalogCloseButton,
            Button configuredSecondaryCatalogButton = null,
            EngineeringKnowledgeCatalog configuredCatalog = null,
            WhiteboxGameCatalog configuredGameCatalog = null)
        {
            Unsubscribe();
            RemoveButtonListeners();
            CloseAllViews();

            sessionHost = host ?? throw new ArgumentNullException(nameof(host));
            inputLock = configuredInputLock;
            catalogButton = configuredCatalogButton;
            popupRoot = configuredPopupRoot;
            popupTitleText = configuredPopupTitleText;
            popupBodyText = configuredPopupBodyText;
            popupCloseButton = configuredPopupCloseButton;
            catalogRoot = configuredCatalogRoot;
            catalogBodyText = configuredCatalogBodyText;
            catalogCloseButton = configuredCatalogCloseButton;
            secondaryCatalogButton = configuredSecondaryCatalogButton;
            gameCatalog = configuredGameCatalog ?? WhiteboxGameCatalog.CreateDefault();
            catalog = configuredCatalog ?? EngineeringKnowledgeCatalog.CreateDefault(gameCatalog);
            knowledgeProgress = new EngineeringKnowledgeProgress(catalog);

            if (popupRoot != null)
                popupRoot.SetActive(false);
            if (catalogRoot != null)
                catalogRoot.SetActive(false);

            AddButtonListeners();
            Subscribe();
            RebuildFromSnapshot(sessionHost.Session.ExportSnapshot());
        }

        public KnowledgeUnlockResult RecordCorrectAnswer(string questionId)
        {
            EnsureProgress();
            var result = knowledgeProgress.RecordCorrectAnswer(questionId);
            PresentUnlock(result);
            RefreshCatalogView();
            return result;
        }

        public void RebuildFromSession()
        {
            if (sessionHost == null)
                return;
            RebuildFromSnapshot(sessionHost.Session.ExportSnapshot());
        }

        public void RebuildFromSnapshot(WhiteboxGameSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            EnsureProgress();
            knowledgeProgress.Reset();
            pendingAnswerEntries.Clear();

            RebuildAnswerKnowledge(snapshot);
            RebuildModuleKnowledge(snapshot);
            RebuildCommissioningKnowledge(snapshot.CommissioningPhase);
            if (snapshot.CommissioningPhase == CommissioningPhase.InService ||
                snapshot.FlowStatus == AssemblyFlowStatus.Completed)
            {
                knowledgeProgress.RecordVehicleCompleted();
            }

            CloseAllViews();
            RefreshCatalogView();
        }

        public void OpenCatalog()
        {
            EnsureProgress();
            if (!knowledgeProgress.IsCatalogEntranceUnlocked || catalogRoot == null)
                return;
            CaptureAndLockInput();
            RefreshCatalogView();
            catalogRoot.SetActive(true);
        }

        public void CloseCatalog()
        {
            if (catalogRoot != null)
                catalogRoot.SetActive(false);
            RestoreInputIfNoViewOpen();
        }

        public void CloseKnowledgePopup()
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);
            RestoreInputIfNoViewOpen();
        }

        public void CloseAllViews()
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);
            if (catalogRoot != null)
                catalogRoot.SetActive(false);
            RestoreCapturedInput();
        }

        /// <summary>
        /// Presents answer knowledge after the quiz has released its own input lock.
        /// Normal gameplay invokes this in LateUpdate; tests may flush it directly.
        /// </summary>
        public void FlushPendingKnowledgePopup()
        {
            if (pendingAnswerEntries.Count == 0)
                return;
            var entries = pendingAnswerEntries.ToArray();
            pendingAnswerEntries.Clear();
            PresentEntries(entries);
        }

        public static string BuildCatalogText(IReadOnlyList<KnowledgeEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            if (entries.Count == 0)
                return "尚未解锁工程知识。";

            var builder = new StringBuilder();
            builder.Append("工程知识图鉴 · 已解锁 ").Append(entries.Count).Append(" 项\n\n");
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                builder.Append(index + 1).Append(". ").Append(entry.Title).Append('\n');
                builder.Append(entry.Body);
                if (index < entries.Count - 1)
                    builder.Append("\n\n");
            }
            return builder.ToString();
        }

        private void OnEnable()
        {
            AddButtonListeners();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            RemoveButtonListeners();
            CloseAllViews();
        }

        private void LateUpdate()
        {
            FlushPendingKnowledgePopup();
        }

        private void OnMilestoneReached(WhiteboxMilestoneEvent milestone)
        {
            EnsureProgress();
            var newlyUnlocked = new List<KnowledgeEntry>();

            if (milestone.Kind == WhiteboxMilestoneKind.PartInstalled)
            {
                foreach (var entry in catalog.GetEntriesForPart(milestone.PartId))
                {
                    if (entry.Category == KnowledgeEntryCategory.Part &&
                        knowledgeProgress.IsUnlocked(entry.Id))
                    {
                        newlyUnlocked.Add(entry);
                    }
                }
            }
            else if (milestone.Kind == WhiteboxMilestoneKind.ModuleInstalled)
            {
                foreach (var entry in catalog.GetEntriesForModule(milestone.ModuleId))
                {
                    if (knowledgeProgress.IsUnlocked(entry.Id))
                        newlyUnlocked.Add(entry);
                }
            }

            // A module can become complete on either its last part or its last child module.
            foreach (var definition in gameCatalog.Modules)
            {
                if (!sessionHost.Session.IsModuleComplete(definition.Id))
                    continue;
                AppendNewEntries(
                    newlyUnlocked,
                    knowledgeProgress.RecordCompletedModule(definition.Id));
            }

            if (milestone.Kind == WhiteboxMilestoneKind.Commissioning)
            {
                AppendNewEntries(
                    newlyUnlocked,
                    knowledgeProgress.RecordCommissioningPhase(milestone.CommissioningPhase));
            }

            QueueEntries(newlyUnlocked);
            RefreshCatalogView();
        }

        private void OnAnswerEvaluated(WhiteboxAnswerEvaluatedEvent answer)
        {
            if (!answer.Result.IsCorrect)
                return;

            EnsureProgress();
            var result = knowledgeProgress.RecordCorrectAnswer(answer.QuestionId);
            foreach (var entry in result.NewlyUnlockedEntries)
            {
                if (entry.Category == KnowledgeEntryCategory.QuestionExplanation)
                    pendingAnswerEntries.Add(entry);
            }
            RefreshCatalogView();
        }

        private void OnVehicleCompleted()
        {
            EnsureProgress();
            QueueEntries(knowledgeProgress.RecordVehicleCompleted().NewlyUnlockedEntries);
            RefreshCatalogView();
        }

        private void QueueEntries(IReadOnlyList<KnowledgeEntry> entries)
        {
            if (entries == null)
                return;
            foreach (var entry in entries)
            {
                if (entry != null)
                    pendingAnswerEntries.Add(entry);
            }
        }

        private void OnSessionReset()
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);
            if (catalogRoot != null)
                catalogRoot.SetActive(false);
            pendingAnswerEntries.Clear();
            inputStateCaptured = false;
            inputWasLocked = false;
            RebuildFromSession();

            if (sessionHost.Session.FlowStatus == AssemblyFlowStatus.Pending)
                inputLock?.SetInputLocked(false);
            else if (sessionHost.Session.IsVehicleComplete)
                inputLock?.SetInputLocked(true);
        }

        private void RebuildAnswerKnowledge(WhiteboxGameSessionSnapshot snapshot)
        {
            if (snapshot.CorrectQuestionIds != null && snapshot.CorrectQuestionIds.Length > 0)
            {
                foreach (var questionId in snapshot.CorrectQuestionIds)
                    knowledgeProgress.RecordCorrectAnswer(questionId);
                return;
            }

            var recordedParts = new HashSet<PartId>();
            foreach (var partId in snapshot.UnlockedParts ?? Array.Empty<PartId>())
            {
                if (!recordedParts.Add(partId))
                    continue;

                // A v1 snapshot stores rewarded parts, so the earliest matching answer is the
                // deterministic reconstruction point when the exact question id is unavailable.
                foreach (var entry in catalog.Entries)
                {
                    if (entry.UnlockKind == KnowledgeUnlockKind.CorrectAnswer &&
                        entry.RelatedPart == partId)
                    {
                        knowledgeProgress.RecordCorrectAnswer(entry.SourceQuestionId);
                        break;
                    }
                }
            }
        }

        private void RebuildModuleKnowledge(WhiteboxGameSessionSnapshot snapshot)
        {
            foreach (var definition in gameCatalog.Modules)
            {
                if (IsModuleComplete(snapshot, definition))
                    knowledgeProgress.RecordCompletedModule(definition.Id);
            }
        }

        private void RebuildCommissioningKnowledge(CommissioningPhase phase)
        {
            var completedCount = WhiteboxAssemblyProgressPresenter
                .GetCompletedCommissioningStepCount(phase);
            var orderedPhases = new[]
            {
                CommissioningPhase.NeedsRetuning,
                CommissioningPhase.ReadyForInspection,
                CommissioningPhase.ReadyForRetest,
                CommissioningPhase.InService
            };
            for (var index = 0; index < completedCount; index++)
                knowledgeProgress.RecordCommissioningPhase(orderedPhases[index]);
        }

        private static bool IsModuleComplete(
            WhiteboxGameSessionSnapshot snapshot,
            ModuleDefinition definition)
        {
            ModuleAssemblySnapshot moduleSnapshot = null;
            foreach (var candidate in snapshot.Modules ?? Array.Empty<ModuleAssemblySnapshot>())
            {
                if (candidate != null && candidate.ModuleId == definition.Id)
                {
                    moduleSnapshot = candidate;
                    break;
                }
            }
            if (moduleSnapshot == null)
                return false;

            var installedParts = new HashSet<PartId>(
                moduleSnapshot.InstalledParts ?? Array.Empty<PartId>());
            var installedModules = new HashSet<ModuleId>(
                moduleSnapshot.InstalledModules ?? Array.Empty<ModuleId>());
            foreach (var partId in definition.RequiredParts)
            {
                if (!installedParts.Contains(partId))
                    return false;
            }
            foreach (var moduleId in definition.RequiredModules)
            {
                if (!installedModules.Contains(moduleId))
                    return false;
            }
            return true;
        }

        private void PresentUnlock(KnowledgeUnlockResult result)
        {
            if (result != null)
                PresentEntries(result.NewlyUnlockedEntries);
        }

        private void PresentEntries(IReadOnlyList<KnowledgeEntry> entries)
        {
            if (entries == null || entries.Count == 0 || popupRoot == null)
                return;

            CaptureAndLockInput();
            var appendToOpenPopup = popupRoot.activeSelf &&
                popupBodyText != null &&
                !string.IsNullOrWhiteSpace(popupBodyText.text);
            if (popupTitleText != null)
            {
                popupTitleText.text = appendToOpenPopup
                    ? "工程知识已更新"
                    : entries.Count == 1
                        ? $"工程知识：{entries[0].Title}"
                        : $"工程知识提示（{entries.Count}项）";
            }
            if (popupBodyText != null)
            {
                var builder = new StringBuilder();
                if (appendToOpenPopup)
                    builder.Append(popupBodyText.text).Append("\n\n");
                foreach (var entry in entries)
                {
                    if (builder.Length > 0 &&
                        (builder.Length < 2 ||
                         builder[builder.Length - 1] != '\n' ||
                         builder[builder.Length - 2] != '\n'))
                        builder.Append("\n\n");
                    if (entries.Count > 1 || appendToOpenPopup)
                        builder.Append(entry.Title).Append('\n');
                    builder.Append(entry.Body);
                }
                popupBodyText.text = builder.ToString();
            }
            popupRoot.SetActive(true);
        }

        private void RefreshCatalogView()
        {
            if (catalogButton != null)
                catalogButton.interactable = IsCatalogUnlocked;
            if (secondaryCatalogButton != null)
                secondaryCatalogButton.interactable = IsCatalogUnlocked;
            if (catalogBodyText != null)
                catalogBodyText.text = BuildCatalogText(UnlockedEntries);
        }

        private void CaptureAndLockInput()
        {
            if (!inputStateCaptured)
            {
                inputWasLocked = inputLock != null && inputLock.InputLocked;
                inputStateCaptured = true;
            }
            inputLock?.SetInputLocked(true);
        }

        private void RestoreInputIfNoViewOpen()
        {
            var popupOpen = popupRoot != null && popupRoot.activeSelf;
            var catalogOpen = catalogRoot != null && catalogRoot.activeSelf;
            if (!popupOpen && !catalogOpen)
                RestoreCapturedInput();
        }

        private void RestoreCapturedInput()
        {
            if (!inputStateCaptured)
                return;
            var completedSessionOwnsLock = sessionHost != null &&
                sessionHost.Session.IsVehicleComplete;
            inputLock?.SetInputLocked(inputWasLocked || completedSessionOwnsLock);
            inputStateCaptured = false;
        }

        private static void AppendNewEntries(
            ICollection<KnowledgeEntry> target,
            KnowledgeUnlockResult result)
        {
            if (result == null)
                return;
            foreach (var entry in result.NewlyUnlockedEntries)
                target.Add(entry);
        }

        private void EnsureProgress()
        {
            gameCatalog ??= WhiteboxGameCatalog.CreateDefault();
            catalog ??= EngineeringKnowledgeCatalog.CreateDefault(gameCatalog);
            knowledgeProgress ??= new EngineeringKnowledgeProgress(catalog);
        }

        private void AddButtonListeners()
        {
            if (catalogButton != null)
            {
                catalogButton.onClick.RemoveListener(OpenCatalog);
                catalogButton.onClick.AddListener(OpenCatalog);
            }
            if (popupCloseButton != null)
            {
                popupCloseButton.onClick.RemoveListener(CloseKnowledgePopup);
                popupCloseButton.onClick.AddListener(CloseKnowledgePopup);
            }
            if (catalogCloseButton != null)
            {
                catalogCloseButton.onClick.RemoveListener(CloseCatalog);
                catalogCloseButton.onClick.AddListener(CloseCatalog);
            }
            if (secondaryCatalogButton != null)
            {
                secondaryCatalogButton.onClick.RemoveListener(OpenCatalog);
                secondaryCatalogButton.onClick.AddListener(OpenCatalog);
            }
        }

        private void RemoveButtonListeners()
        {
            catalogButton?.onClick.RemoveListener(OpenCatalog);
            popupCloseButton?.onClick.RemoveListener(CloseKnowledgePopup);
            catalogCloseButton?.onClick.RemoveListener(CloseCatalog);
            secondaryCatalogButton?.onClick.RemoveListener(OpenCatalog);
        }

        private void Subscribe()
        {
            if (subscribed || sessionHost == null || !isActiveAndEnabled)
                return;
            sessionHost.AnswerEvaluated += OnAnswerEvaluated;
            sessionHost.MilestoneReached += OnMilestoneReached;
            sessionHost.VehicleCompleted += OnVehicleCompleted;
            sessionHost.SessionReset += OnSessionReset;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || sessionHost == null)
                return;
            sessionHost.AnswerEvaluated -= OnAnswerEvaluated;
            sessionHost.MilestoneReached -= OnMilestoneReached;
            sessionHost.VehicleCompleted -= OnVehicleCompleted;
            sessionHost.SessionReset -= OnSessionReset;
            subscribed = false;
        }
    }
}
