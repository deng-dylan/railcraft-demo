using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Knowledge/material station. A station can expose one legacy part or a
    /// small module package. The domain still records individual PartId values,
    /// while the player sees one coherent engineering work package.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuizPartStation : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private ThirdPersonInputLock inputLock;
        [SerializeField] private MonoBehaviour quizDialogBehaviour;
        [SerializeField] private QuizQuestionPresentation[] questions = Array.Empty<QuizQuestionPresentation>();
        [SerializeField] private PartId rewardPart;
        [SerializeField] private PartId[] rewardParts = Array.Empty<PartId>();
        [SerializeField] private string stationDisplayName = "零件工位";
        [SerializeField] private GameObject rewardVisual;
        [SerializeField] private GameObject[] rewardVisuals = Array.Empty<GameObject>();
        [SerializeField, TextArea] private string afterPickupObjective = "前往模块装配台安装零件";
        [SerializeField] private bool usesWorkPackage;
        [SerializeField] private WorkPackageId workPackageId;

        private IQuizDialog quizDialogOverride;
        private WhiteboxGameSessionHost subscribedHost;
        private bool rewardUnlocked;
        private bool collected;
        private bool quizOpen;
        private bool questionCycleInitialized;
        private int currentQuestionIndex;
        private int currentRewardIndex;
        private int questionCycleOffset;

        public string InteractionPrompt
        {
            get
            {
                if (IsCollected)
                    return string.Empty;

                if (usesWorkPackage)
                {
                    return RewardUnlocked
                        ? $"按 E 领取{WhiteboxDisplayNames.WorkPackage(workPackageId)}"
                        : $"按 E 在{stationDisplayName}完成知识确认";
                }

                return RewardUnlocked
                    ? $"按 E 拾取{WhiteboxDisplayNames.Part(CurrentRewardPart)}"
                    : $"按 E 在{stationDisplayName}答题";
            }
        }

        /// <summary>All sub-parts represented by this station.</summary>
        public IReadOnlyList<PartId> RewardParts => rewardParts ?? Array.Empty<PartId>();

        /// <summary>
        /// Number of player-facing rewards. A package is one reward even when
        /// its domain recipe contains several child PartId values.
        /// </summary>
        public int RewardPartCount => usesWorkPackage ? 1 : RewardParts.Count;
        public int SubPartCount => RewardParts.Count;
        public int CollectedRewardCount => usesWorkPackage
            ? (IsCollected ? 1 : 0)
            : CountParts(IsCollectedInSession);
        public int PendingRewardCount => Math.Max(0, RewardPartCount - CollectedRewardCount);
        public bool AllRewardsUnlocked => usesWorkPackage
            ? IsWorkPackageUnlockedInSession()
            : RewardPartCount > 0 && CountParts(IsUnlockedInSession) == RewardPartCount;
        public bool RewardUnlocked => usesWorkPackage
            ? IsWorkPackageUnlockedInSession()
            : IsUnlockedInSession(CurrentRewardPart);
        public bool IsCollected => usesWorkPackage
            ? IsWorkPackageCollectedInSession()
            : RewardPartCount > 0 && CountParts(IsCollectedInSession) == RewardPartCount;
        public bool IsQuizOpen => quizOpen;
        public bool UsesWorkPackage => usesWorkPackage;
        public WorkPackageId WorkPackageId => workPackageId;
        public int KnowledgeQuestionCount => usesWorkPackage
            ? WhiteboxWorkPackageCatalog.Get(workPackageId).RequiredQuestionCount
            : 0;
        public int AnsweredKnowledgeQuestionCount => usesWorkPackage
            ? ResolvePackageSession()?.GetWorkPackageAnsweredQuestionCount(workPackageId) ?? 0
            : 0;
        public bool KnowledgeComplete => usesWorkPackage &&
            (ResolvePackageSession()?.IsWorkPackageKnowledgeComplete(workPackageId) ?? false);

        /// <summary>
        /// Legacy property retained for existing save adapters and tests. For
        /// a package station it returns the current sub-part being prepared.
        /// </summary>
        public PartId RewardPart => CurrentRewardPart;

        public PartId CurrentRewardPart
        {
            get
            {
                if (RewardParts.Count == 0)
                    return rewardPart;
                var index = Mathf.Clamp(currentRewardIndex, 0, RewardParts.Count - 1);
                return RewardParts[index];
            }
        }

        public QuizQuestionPresentation CurrentQuestion => ResolveCurrentQuestion();

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            ThirdPersonInputLock configuredInputLock,
            MonoBehaviour configuredQuizDialog,
            QuizQuestionPresentation configuredQuestion,
            PartId configuredRewardPart,
            string configuredStationDisplayName,
            GameObject configuredRewardVisual,
            string configuredAfterPickupObjective)
        {
            Configure(
                configuredSessionHost,
                configuredInputLock,
                configuredQuizDialog,
                configuredQuestion == null
                    ? Array.Empty<QuizQuestionPresentation>()
                    : new[] { configuredQuestion },
                new[] { configuredRewardPart },
                configuredStationDisplayName,
                configuredRewardVisual == null
                    ? Array.Empty<GameObject>()
                    : new[] { configuredRewardVisual },
                configuredAfterPickupObjective);
        }

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            ThirdPersonInputLock configuredInputLock,
            MonoBehaviour configuredQuizDialog,
            QuizQuestionPresentation[] configuredQuestions,
            PartId configuredRewardPart,
            string configuredStationDisplayName,
            GameObject configuredRewardVisual,
            string configuredAfterPickupObjective)
        {
            Configure(
                configuredSessionHost,
                configuredInputLock,
                configuredQuizDialog,
                configuredQuestions,
                new[] { configuredRewardPart },
                configuredStationDisplayName,
                configuredRewardVisual == null
                    ? Array.Empty<GameObject>()
                    : new[] { configuredRewardVisual },
                configuredAfterPickupObjective);
        }

        /// <summary>
        /// Configures a module-level station. Questions may cover any of the
        /// configured sub-parts; one correct answer unlocks the matching part,
        /// then the station advances to the next missing part.
        /// </summary>
        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            ThirdPersonInputLock configuredInputLock,
            MonoBehaviour configuredQuizDialog,
            QuizQuestionPresentation[] configuredQuestions,
            PartId[] configuredRewardParts,
            string configuredStationDisplayName,
            GameObject[] configuredRewardVisuals,
            string configuredAfterPickupObjective)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost;
            inputLock = configuredInputLock;
            quizDialogBehaviour = configuredQuizDialog;
            quizDialogOverride = null;
            questions = configuredQuestions == null
                ? Array.Empty<QuizQuestionPresentation>()
                : (QuizQuestionPresentation[])configuredQuestions.Clone();

            var normalizedParts = (configuredRewardParts ?? Array.Empty<PartId>())
                .Distinct()
                .ToArray();
            rewardParts = normalizedParts.Length == 0
                ? new[] { rewardPart }
                : normalizedParts;
            rewardPart = rewardParts[0];
            stationDisplayName = string.IsNullOrWhiteSpace(configuredStationDisplayName)
                ? "零件工位"
                : configuredStationDisplayName;
            rewardVisuals = configuredRewardVisuals == null
                ? Array.Empty<GameObject>()
                : (GameObject[])configuredRewardVisuals.Clone();
            rewardVisual = rewardVisuals.FirstOrDefault(visual => visual != null);
            afterPickupObjective = configuredAfterPickupObjective ?? string.Empty;
            usesWorkPackage = false;
            workPackageId = default;
            questionCycleInitialized = false;
            currentQuestionIndex = 0;
            currentRewardIndex = 0;
            questionCycleOffset = 0;
            Subscribe();
            ResetLocalState();
        }

        /// <summary>
        /// Configures the compact player-facing flow. The domain still keeps
        /// every required PartId for assembly and save compatibility, while
        /// this station exposes one material package and its gate questions.
        /// </summary>
        public void ConfigureWorkPackage(
            WhiteboxGameSessionHost configuredSessionHost,
            ThirdPersonInputLock configuredInputLock,
            MonoBehaviour configuredQuizDialog,
            WorkPackageId configuredWorkPackageId,
            QuizQuestionPresentation[] configuredQuestions,
            GameObject configuredPackageVisual,
            string configuredStationDisplayName,
            string configuredAfterPickupObjective)
        {
            var package = WhiteboxWorkPackageCatalog.Get(configuredWorkPackageId);
            Configure(
                configuredSessionHost,
                configuredInputLock,
                configuredQuizDialog,
                configuredQuestions,
                package.RequiredParts.ToArray(),
                configuredStationDisplayName,
                configuredPackageVisual == null
                    ? Array.Empty<GameObject>()
                    : new[] { configuredPackageVisual },
                configuredAfterPickupObjective);
            usesWorkPackage = true;
            workPackageId = configuredWorkPackageId;
            // Configure() initialized the legacy question cycle. Start the
            // package cycle once from zero so initial setup cannot advance it
            // twice; subsequent session resets rotate the first gate question.
            questionCycleInitialized = false;
            questionCycleOffset = 0;
            currentQuestionIndex = 0;
            ResetLocalState();
        }

        public void SetQuizDialogForTests(IQuizDialog configuredQuizDialog)
        {
            quizDialogOverride = configuredQuizDialog;
        }

        public bool CanInteract(InteractionContext context)
        {
            return sessionHost != null && !IsCollected && !quizOpen;
        }

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
                return;

            if (RewardUnlocked)
                CollectReward();
            else
                OpenQuiz();
        }

        private void OnEnable()
        {
            Subscribe();
            ApplyRewardVisualState();
        }

        private void OnDisable()
        {
            if (quizOpen)
                CloseQuiz();
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || sessionHost == null || subscribedHost == sessionHost)
                return;

            subscribedHost = sessionHost;
            subscribedHost.SessionReset += ResetLocalState;
        }

        private void Unsubscribe()
        {
            if (subscribedHost == null)
                return;

            subscribedHost.SessionReset -= ResetLocalState;
            subscribedHost = null;
        }

        private void OpenQuiz()
        {
            var dialog = ResolveDialog();
            var question = ResolveCurrentQuestion();
            if (dialog == null || question == null || !question.IsValid)
            {
                sessionHost.NotifyFeedback("答题界面或题目配置缺失");
                return;
            }

            quizOpen = true;
            inputLock?.SetInputLocked(true);
            try
            {
                dialog.Present(question, HandleOptionSelected, HandleQuizCancelled);
            }
            catch (Exception exception)
            {
                quizOpen = false;
                inputLock?.SetInputLocked(false);
                dialog.Dismiss();
                sessionHost.NotifyFeedback("答题界面打开失败，请检查题目与按钮配置");
                Debug.LogException(exception, this);
            }
        }

        private void HandleOptionSelected(int selectedOptionIndex)
        {
            if (!quizOpen || sessionHost == null)
                return;

            var question = ResolveCurrentQuestion();
            if (question == null)
            {
                CloseQuiz();
                sessionHost.NotifyFeedback("当前工位没有可用题目");
                return;
            }

            if (usesWorkPackage)
            {
                HandleWorkPackageOptionSelected(question, selectedOptionIndex);
                return;
            }

            var submittedOptionIndex = question.MapSubmittedOptionIndex(selectedOptionIndex);
            var result = sessionHost.SubmitAnswer(question.QuestionId, submittedOptionIndex);
            if (!result.IsCorrect)
            {
                var explanation = string.IsNullOrWhiteSpace(question.Explanation)
                    ? "回答错误，请再想一想。"
                    : $"回答错误。{question.Explanation}";
                ResolveDialog()?.SetFeedback(explanation);
                return;
            }

            var target = CurrentRewardPart;
            if (result.RewardPart.HasValue && !RewardParts.Contains(result.RewardPart.Value))
            {
                ResolveDialog()?.SetFeedback("题目主题与当前模块配置不一致，请检查题库路由。");
                return;
            }

            if (!result.RewardPart.HasValue)
            {
                ResolveDialog()?.SetFeedback("知识已记录；当前题目不发放额外零件，请继续完成本模块确认。");
                CloseQuiz();
                return;
            }

            if (result.RewardPart.HasValue && result.RewardPart.Value != target &&
                IsUnlockedInSession(target) == false)
            {
                // A package may contain several sub-parts. The question's
                // reward remains authoritative, so advance to that part before
                // showing the collection prompt.
                var matchingIndex = Array.IndexOf(rewardParts, result.RewardPart.Value);
                if (matchingIndex >= 0)
                    currentRewardIndex = matchingIndex;
                target = CurrentRewardPart;
            }

            rewardUnlocked = IsUnlockedInSession(target);
            ApplyRewardVisualState();
            CloseQuiz();
            sessionHost.NotifyFeedback($"回答正确，{WhiteboxDisplayNames.Part(target)}已解锁");
            sessionHost.SetObjective($"拾取{WhiteboxDisplayNames.Part(target)}");
        }

        private void HandleWorkPackageOptionSelected(
            QuizQuestionPresentation question,
            int selectedOptionIndex)
        {
            var packageSession = ResolvePackageSession();
            if (packageSession == null)
            {
                CloseQuiz();
                sessionHost.NotifyFeedback("当前会话不支持材料包流程");
                return;
            }

            var submittedOptionIndex = question.MapSubmittedOptionIndex(selectedOptionIndex);
            var result = sessionHost.SubmitWorkPackageAnswer(
                workPackageId,
                question.QuestionId,
                submittedOptionIndex);
            if (!result.IsCorrect)
            {
                var explanation = string.IsNullOrWhiteSpace(question.Explanation)
                    ? "回答错误，请再想一想。"
                    : $"回答错误。{question.Explanation}";
                ResolveDialog()?.SetFeedback(explanation);
                return;
            }

            CloseQuiz();
            ApplyRewardVisualState();
            if (result.PackageUnlocked)
            {
                sessionHost.NotifyFeedback(
                    $"知识确认完成，{WhiteboxDisplayNames.WorkPackage(workPackageId)}已解锁");
                sessionHost.SetObjective($"领取{WhiteboxDisplayNames.WorkPackage(workPackageId)}");
            }
            else
            {
                sessionHost.NotifyFeedback(
                    $"已确认{WhiteboxDisplayNames.WorkPackage(workPackageId)}知识（" +
                    $"{result.AnsweredQuestionCount}/{result.RequiredQuestionCount}）");
                sessionHost.SetObjective(
                    $"继续完成{WhiteboxDisplayNames.WorkPackage(workPackageId)}知识确认");
            }
        }

        private void HandleQuizCancelled()
        {
            CloseQuiz();
        }

        private void CloseQuiz()
        {
            ResolveDialog()?.Dismiss();
            quizOpen = false;
            inputLock?.SetInputLocked(false);
        }

        private void CollectReward()
        {
            if (usesWorkPackage)
            {
                CollectWorkPackage();
                return;
            }

            var target = CurrentRewardPart;
            var result = sessionHost.CollectPart(target);
            if (!result.Accepted)
            {
                sessionHost.NotifyFeedback("零件尚未解锁");
                return;
            }

            collected = true;
            currentRewardIndex = ResolveFirstPendingIndex(sessionHost.Session.ExportSnapshot());
            ApplyRewardVisualState();
            sessionHost.NotifyFeedback($"已拾取{WhiteboxDisplayNames.Part(target)}");
            sessionHost.SetObjective(IsCollected
                ? afterPickupObjective
                : $"继续准备{WhiteboxDisplayNames.Part(CurrentRewardPart)}");
        }

        private void CollectWorkPackage()
        {
            var packageSession = ResolvePackageSession();
            if (packageSession == null)
            {
                sessionHost.NotifyFeedback("当前会话不支持材料包领取");
                return;
            }

            var result = sessionHost.CollectWorkPackage(workPackageId);
            if (!result.Accepted)
            {
                sessionHost.NotifyFeedback("材料包尚未解锁");
                return;
            }

            ApplyRewardVisualState();
            sessionHost.NotifyFeedback($"已领取{WhiteboxDisplayNames.WorkPackage(workPackageId)}");
            sessionHost.SetObjective(afterPickupObjective);
        }

        private void ResetLocalState()
        {
            if (quizOpen)
                CloseQuiz();

            var snapshot = sessionHost == null
                ? null
                : sessionHost.Session.ExportSnapshot();
            var isFreshSession = snapshot == null ||
                (snapshot.FlowStatus == AssemblyFlowStatus.Pending &&
                 (snapshot.UnlockedParts == null || snapshot.UnlockedParts.Length == 0) &&
                 (snapshot.CollectedParts == null || snapshot.CollectedParts.Length == 0));
            if (isFreshSession)
                AdvanceQuestionForReset();

            currentRewardIndex = ResolveFirstPendingIndex(snapshot);
            rewardUnlocked = RewardUnlocked;
            collected = IsCollected;
            ApplyRewardVisualState();
        }

        private int ResolveFirstPendingIndex(WhiteboxGameSessionSnapshot snapshot)
        {
            if (RewardParts.Count == 0)
                return 0;

            // Prefer an unlocked-but-not-collected item so the next prompt is
            // always actionable.
            for (var index = 0; index < RewardParts.Count; index++)
            {
                var part = RewardParts[index];
                if (Contains(snapshot?.UnlockedParts, part) && !Contains(snapshot?.CollectedParts, part))
                    return index;
            }

            for (var index = 0; index < RewardParts.Count; index++)
            {
                if (!Contains(snapshot?.CollectedParts, RewardParts[index]))
                    return index;
            }

            return RewardParts.Count - 1;
        }

        private static bool Contains(PartId[] parts, PartId partId)
        {
            if (parts == null)
                return false;
            for (var index = 0; index < parts.Length; index++)
            {
                if (parts[index] == partId)
                    return true;
            }
            return false;
        }

        private bool IsUnlockedInSession(PartId partId)
        {
            return sessionHost != null && Contains(sessionHost.Session.ExportSnapshot().UnlockedParts, partId);
        }

        private bool IsCollectedInSession(PartId partId)
        {
            return sessionHost != null && Contains(sessionHost.Session.ExportSnapshot().CollectedParts, partId);
        }

        private IWorkPackageGameSession ResolvePackageSession()
        {
            return sessionHost?.Session as IWorkPackageGameSession;
        }

        private bool IsWorkPackageUnlockedInSession()
        {
            return usesWorkPackage &&
                (ResolvePackageSession()?.IsWorkPackageUnlocked(workPackageId) ?? false);
        }

        private bool IsWorkPackageCollectedInSession()
        {
            return usesWorkPackage &&
                (ResolvePackageSession()?.IsWorkPackageCollected(workPackageId) ?? false);
        }

        private int CountParts(Func<PartId, bool> predicate)
        {
            var count = 0;
            foreach (var part in RewardParts)
            {
                if (predicate(part))
                    count++;
            }
            return count;
        }

        private void AdvanceQuestionForReset()
        {
            if (questions == null || questions.Length == 0)
            {
                currentQuestionIndex = 0;
                questionCycleInitialized = true;
                return;
            }

            if (!questionCycleInitialized)
            {
                currentQuestionIndex = 0;
                questionCycleInitialized = true;
                return;
            }

            questionCycleOffset = (questionCycleOffset + 1) % questions.Length;
            currentQuestionIndex = questionCycleOffset;
        }

        private QuizQuestionPresentation ResolveCurrentQuestion()
        {
            if (questions == null || questions.Length == 0)
                return null;

            if (usesWorkPackage)
            {
                var snapshot = sessionHost?.Session.ExportSnapshot();
                var answered = new HashSet<string>(
                    snapshot?.CorrectQuestionIds ?? Array.Empty<string>(),
                    StringComparer.Ordinal);
                for (var step = 0; step < questions.Length; step++)
                {
                    var packageQuestionIndex =
                        (questionCycleOffset + step) % questions.Length;
                    if (packageQuestionIndex < 0)
                        packageQuestionIndex += questions.Length;
                    var candidate = questions[packageQuestionIndex];
                    if (candidate != null && !answered.Contains(candidate.QuestionId))
                        return candidate;
                }

                var fallbackIndex = questionCycleOffset % questions.Length;
                if (fallbackIndex < 0)
                    fallbackIndex += questions.Length;
                return questions[fallbackIndex];
            }

            var candidates = questions
                .Select((question, index) => new { question, index })
                .Where(item => RewardParts.Count <= 1 || item.question.RewardPart == CurrentRewardPart)
                .Select(item => item.question)
                .ToArray();
            if (candidates.Length == 0)
                candidates = questions;

            var index = candidates.Length == 0
                ? 0
                : questionCycleOffset % candidates.Length;
            if (index < 0)
                index += candidates.Length;
            return candidates[index];
        }

        private IQuizDialog ResolveDialog()
        {
            return quizDialogOverride ?? quizDialogBehaviour as IQuizDialog;
        }

        private void ApplyRewardVisualState()
        {
            var visualSet = new HashSet<GameObject>();
            if (rewardVisuals != null)
            {
                foreach (var visual in rewardVisuals)
                {
                    if (visual != null)
                        visualSet.Add(visual);
                }
            }
            if (rewardVisual != null)
                visualSet.Add(rewardVisual);

            foreach (var visual in visualSet)
            {
                if (usesWorkPackage)
                {
                    visual.SetActive(IsWorkPackageUnlockedInSession() &&
                                     !IsWorkPackageCollectedInSession());
                    continue;
                }

                var index = Array.IndexOf(rewardVisuals ?? Array.Empty<GameObject>(), visual);
                var part = index >= 0 && index < RewardParts.Count
                    ? RewardParts[index]
                    : CurrentRewardPart;
                visual.SetActive(IsUnlockedInSession(part) && !IsCollectedInSession(part));
            }
        }
    }
}
