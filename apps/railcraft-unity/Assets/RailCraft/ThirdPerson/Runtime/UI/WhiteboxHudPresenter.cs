using System;
using System.Collections.Generic;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    [DisallowMultipleComponent]
    public sealed class WhiteboxHudPresenter : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private PlayerInteractionScanner interactionScanner;
        [SerializeField] private ThirdPersonInputLock inputLock;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Text taskText;
        [SerializeField] private Text inventoryText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private GameObject completionRoot;
        [SerializeField] private Text completionText;
        [SerializeField] private Text completionDetailText;
        [SerializeField, Min(0.1f)] private float feedbackDuration = 2.5f;
        [SerializeField] private Color feedbackSuccessColor = new Color(0.28f, 1f, 0.52f, 1f);
        [SerializeField] private Color feedbackFailureColor = new Color(1f, 0.34f, 0.26f, 1f);
        [SerializeField] private Color feedbackNeutralColor = new Color(0.36f, 0.92f, 1f, 1f);

        private WhiteboxGameSessionHost subscribedHost;
        private PlayerInteractionScanner subscribedScanner;
        private float feedbackTimeRemaining;
        private bool completionOwnsInputLock;

        public string InteractionPrompt => interactionPromptText == null
            ? string.Empty
            : interactionPromptText.text;
        public string Task => taskText == null ? string.Empty : taskText.text;
        public string Inventory => inventoryText == null ? string.Empty : inventoryText.text;
        public string Progress => progressText == null ? string.Empty : progressText.text;
        public bool IsCompletionVisible => completionRoot != null && completionRoot.activeSelf;
        public bool? LastFeedbackWasSuccessful { get; private set; }

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            PlayerInteractionScanner configuredInteractionScanner,
            ThirdPersonInputLock configuredInputLock,
            Text configuredInteractionPromptText,
            Text configuredTaskText,
            Text configuredInventoryText,
            Text configuredProgressText,
            Text configuredFeedbackText,
            GameObject configuredCompletionRoot,
            Text configuredCompletionText)
        {
            Configure(
                configuredSessionHost,
                configuredInteractionScanner,
                configuredInputLock,
                configuredInteractionPromptText,
                configuredTaskText,
                configuredInventoryText,
                configuredProgressText,
                configuredFeedbackText,
                configuredCompletionRoot,
                configuredCompletionText,
                null);
        }

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            PlayerInteractionScanner configuredInteractionScanner,
            ThirdPersonInputLock configuredInputLock,
            Text configuredInteractionPromptText,
            Text configuredTaskText,
            Text configuredInventoryText,
            Text configuredProgressText,
            Text configuredFeedbackText,
            GameObject configuredCompletionRoot,
            Text configuredCompletionText,
            Text configuredCompletionDetailText)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost;
            interactionScanner = configuredInteractionScanner;
            inputLock = configuredInputLock;
            interactionPromptText = configuredInteractionPromptText;
            taskText = configuredTaskText;
            inventoryText = configuredInventoryText;
            progressText = configuredProgressText;
            feedbackText = configuredFeedbackText;
            completionRoot = configuredCompletionRoot;
            completionText = configuredCompletionText;
            completionDetailText = configuredCompletionDetailText;
            Subscribe();
            RefreshAll();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseCompletionInputLock();
        }

        private void Update()
        {
            RefreshInteractionPrompt();
            if (feedbackText != null && feedbackText.gameObject.activeSelf)
            {
                feedbackText.transform.localScale = Vector3.Lerp(
                    feedbackText.transform.localScale,
                    Vector3.one,
                    10f * Time.unscaledDeltaTime);
            }
            if (feedbackTimeRemaining <= 0f)
                return;

            feedbackTimeRemaining -= Time.unscaledDeltaTime;
            if (feedbackTimeRemaining <= 0f && feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled)
                return;

            if (sessionHost != null && subscribedHost != sessionHost)
            {
                subscribedHost = sessionHost;
                subscribedHost.StateChanged += RefreshState;
                subscribedHost.SessionReset += HandleSessionReset;
                subscribedHost.FeedbackRequested += ShowFeedback;
                subscribedHost.ObjectiveChanged += ShowObjective;
                subscribedHost.VehicleCompleted += ShowCompletion;
            }

            if (interactionScanner != null && subscribedScanner != interactionScanner)
            {
                subscribedScanner = interactionScanner;
                subscribedScanner.TargetChanged += HandleTargetChanged;
            }
        }

        private void Unsubscribe()
        {
            if (subscribedHost != null)
            {
                subscribedHost.StateChanged -= RefreshState;
                subscribedHost.SessionReset -= HandleSessionReset;
                subscribedHost.FeedbackRequested -= ShowFeedback;
                subscribedHost.ObjectiveChanged -= ShowObjective;
                subscribedHost.VehicleCompleted -= ShowCompletion;
                subscribedHost = null;
            }

            if (subscribedScanner != null)
            {
                subscribedScanner.TargetChanged -= HandleTargetChanged;
                subscribedScanner = null;
            }
        }

        private void RefreshAll()
        {
            RefreshInteractionPrompt();
            RefreshState();
            ShowObjective(sessionHost == null ? string.Empty : sessionHost.CurrentObjective);

            var complete = sessionHost != null && sessionHost.Session.IsVehicleComplete;
            if (complete)
            {
                ShowCompletion();
            }
            else
            {
                if (completionRoot != null)
                    completionRoot.SetActive(false);
                ReleaseCompletionInputLock();
            }
        }

        private void RefreshState()
        {
            if (sessionHost == null)
            {
                if (inventoryText != null)
                    inventoryText.text = "库存：空";
                if (progressText != null)
                    progressText.text = "装配节点 0/6 · 落车未完成 · 调试锁定";
                return;
            }

            var session = sessionHost.Session;
            if (inventoryText != null)
                inventoryText.text = FormatInventory(session.InventoryParts);

            var completedModules = 0;
            var moduleIds = (ModuleId[])Enum.GetValues(typeof(ModuleId));
            for (var index = 0; index < moduleIds.Length; index++)
            {
                if (session.IsModuleComplete(moduleIds[index]))
                    completedModules++;
            }

            if (progressText != null)
            {
                var landing = session.IsLandingComplete ? "落车完成" : "落车未完成";
                var commissioning = WhiteboxDisplayNames.Commissioning(session.CommissioningPhase);
                progressText.text = $"装配节点 {completedModules}/{moduleIds.Length} · {landing} · {commissioning}";
            }
        }

        private void HandleTargetChanged(IPlayerInteractable target)
        {
            RefreshInteractionPrompt();
        }

        private void RefreshInteractionPrompt()
        {
            if (interactionPromptText == null)
                return;

            var prompt = interactionScanner == null ? string.Empty : interactionScanner.CurrentPrompt;
            if (!string.Equals(interactionPromptText.text, prompt, StringComparison.Ordinal))
                interactionPromptText.text = prompt;
            var hasPrompt = !string.IsNullOrWhiteSpace(prompt);
            interactionPromptText.gameObject.SetActive(hasPrompt);
            var promptRoot = interactionPromptText.transform.parent;
            if (promptRoot != null && promptRoot != interactionPromptText.transform)
                promptRoot.gameObject.SetActive(hasPrompt);
        }

        private void ShowObjective(string objective)
        {
            if (taskText != null)
                taskText.text = string.IsNullOrWhiteSpace(objective)
                    ? "当前任务：自由探索"
                    : $"当前任务：{objective}";
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText == null)
                return;

            feedbackText.text = message ?? string.Empty;
            feedbackText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            LastFeedbackWasSuccessful = ClassifyFeedback(message);
            feedbackText.color = LastFeedbackWasSuccessful.HasValue
                ? (LastFeedbackWasSuccessful.Value ? feedbackSuccessColor : feedbackFailureColor)
                : feedbackNeutralColor;
            feedbackText.transform.localScale = Vector3.one * 1.12f;
            feedbackTimeRemaining = feedbackDuration;
        }

        private void ShowCompletion()
        {
            if (completionRoot != null)
                completionRoot.SetActive(true);
            if (completionText != null)
                completionText.text = "调试通过，车辆投入使用！";
            if (completionDetailText != null && sessionHost != null)
                completionDetailText.text = FormatSettlement(sessionHost.Session.Progress);
            completionOwnsInputLock = true;
            inputLock?.SetInputLocked(true);
        }

        private void HandleSessionReset()
        {
            feedbackTimeRemaining = 0f;
            LastFeedbackWasSuccessful = null;
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
                feedbackText.transform.localScale = Vector3.one;
            }
            if (completionRoot != null)
                completionRoot.SetActive(false);
            ReleaseCompletionInputLock();
            RefreshAll();
        }

        private void ReleaseCompletionInputLock()
        {
            if (!completionOwnsInputLock)
                return;

            completionOwnsInputLock = false;
            inputLock?.SetInputLocked(false);
        }

        private static string FormatInventory(IReadOnlyList<PartId> parts)
        {
            if (parts == null || parts.Count == 0)
                return "库存：空";

            var names = new string[parts.Count];
            for (var index = 0; index < parts.Count; index++)
                names[index] = WhiteboxDisplayNames.Part(parts[index]);
            return $"库存（{parts.Count}）：{string.Join("、", names)}";
        }

        public static string FormatSettlement(SessionProgressSummary progress)
        {
            if (progress == null)
                return "成绩数据暂不可用";

            var elapsed = progress.ElapsedTime;
            var totalHours = (int)elapsed.TotalHours;
            var time = totalHours > 0
                ? $"{totalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
                : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            return
                $"装配用时  {time}\n" +
                $"答题正确  {progress.CorrectAnswerCount}/{progress.AnswerAttemptCount}  ·  " +
                $"正确率 {progress.AnswerAccuracyPercent:0}%\n" +
                $"得分  {progress.Score}  ·  等级：{progress.EngineerGradeDisplayName}";
        }

        private static bool? ClassifyFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            var failureTokens = new[]
            {
                "无法", "没有", "尚未", "未解锁", "未完成", "未通过", "失败", "错误", "缺失", "锁定"
            };
            for (var index = 0; index < failureTokens.Length; index++)
            {
                if (message.IndexOf(failureTokens[index], StringComparison.Ordinal) >= 0)
                    return false;
            }

            var successTokens = new[]
            {
                "正确", "已拾取", "已安装", "完成", "通过", "投入使用", "已解锁"
            };
            for (var index = 0; index < successTokens.Length; index++)
            {
                if (message.IndexOf(successTokens[index], StringComparison.Ordinal) >= 0)
                    return true;
            }

            return null;
        }
    }
}
