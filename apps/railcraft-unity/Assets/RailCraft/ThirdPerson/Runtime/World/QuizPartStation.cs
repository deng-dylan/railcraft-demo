using System;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    [DisallowMultipleComponent]
    public sealed class QuizPartStation : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private ThirdPersonInputLock inputLock;
        [SerializeField] private MonoBehaviour quizDialogBehaviour;
        [SerializeField] private QuizQuestionPresentation[] questions = Array.Empty<QuizQuestionPresentation>();
        [SerializeField] private PartId rewardPart;
        [SerializeField] private string stationDisplayName = "零件工位";
        [SerializeField] private GameObject rewardVisual;
        [SerializeField, TextArea] private string afterPickupObjective = "前往模块装配台安装零件";

        private IQuizDialog quizDialogOverride;
        private WhiteboxGameSessionHost subscribedHost;
        private bool rewardUnlocked;
        private bool collected;
        private bool quizOpen;
        private bool questionCycleInitialized;
        private int currentQuestionIndex;

        public string InteractionPrompt
        {
            get
            {
                if (collected)
                    return string.Empty;
                return rewardUnlocked
                    ? $"按 E 拾取{WhiteboxDisplayNames.Part(rewardPart)}"
                    : $"按 E 在{stationDisplayName}答题";
            }
        }

        public bool RewardUnlocked => rewardUnlocked;
        public bool IsCollected => collected;
        public bool IsQuizOpen => quizOpen;
        public PartId RewardPart => rewardPart;
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
                configuredRewardPart,
                configuredStationDisplayName,
                configuredRewardVisual,
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
            Unsubscribe();
            sessionHost = configuredSessionHost;
            inputLock = configuredInputLock;
            quizDialogBehaviour = configuredQuizDialog;
            quizDialogOverride = null;
            questions = configuredQuestions == null
                ? Array.Empty<QuizQuestionPresentation>()
                : (QuizQuestionPresentation[])configuredQuestions.Clone();
            rewardPart = configuredRewardPart;
            stationDisplayName = string.IsNullOrWhiteSpace(configuredStationDisplayName)
                ? "零件工位"
                : configuredStationDisplayName;
            rewardVisual = configuredRewardVisual;
            afterPickupObjective = configuredAfterPickupObjective ?? string.Empty;
            questionCycleInitialized = false;
            currentQuestionIndex = 0;
            Subscribe();
            ResetLocalState();
        }

        public void SetQuizDialogForTests(IQuizDialog configuredQuizDialog)
        {
            quizDialogOverride = configuredQuizDialog;
        }

        public bool CanInteract(InteractionContext context)
        {
            return sessionHost != null && !collected && !quizOpen;
        }

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
                return;

            if (rewardUnlocked)
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

            if (result.RewardPart.HasValue && result.RewardPart.Value != rewardPart)
            {
                ResolveDialog()?.SetFeedback("题目奖励与工位零件配置不一致，请检查白盒配置。");
                return;
            }

            rewardUnlocked = true;
            ApplyRewardVisualState();
            CloseQuiz();
            sessionHost.NotifyFeedback($"回答正确，{WhiteboxDisplayNames.Part(rewardPart)}已解锁");
            sessionHost.SetObjective($"拾取{WhiteboxDisplayNames.Part(rewardPart)}");
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
            var result = sessionHost.CollectPart(rewardPart);
            if (!result.Accepted)
            {
                sessionHost.NotifyFeedback("零件尚未解锁");
                return;
            }

            collected = true;
            ApplyRewardVisualState();
            sessionHost.NotifyFeedback($"已拾取{WhiteboxDisplayNames.Part(rewardPart)}");
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
            rewardUnlocked = snapshot != null && Contains(snapshot.UnlockedParts, rewardPart);
            collected = snapshot != null && Contains(snapshot.CollectedParts, rewardPart);
            ApplyRewardVisualState();
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

            currentQuestionIndex = (currentQuestionIndex + 1) % questions.Length;
        }

        private QuizQuestionPresentation ResolveCurrentQuestion()
        {
            if (questions == null || questions.Length == 0)
                return null;
            if (currentQuestionIndex < 0 || currentQuestionIndex >= questions.Length)
                currentQuestionIndex = 0;
            return questions[currentQuestionIndex];
        }

        private IQuizDialog ResolveDialog()
        {
            return quizDialogOverride ?? quizDialogBehaviour as IQuizDialog;
        }

        private void ApplyRewardVisualState()
        {
            if (rewardVisual != null)
                rewardVisual.SetActive(rewardUnlocked && !collected);
        }
    }
}
