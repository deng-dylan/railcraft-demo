using System;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    public enum InteractionFeedbackOutcome
    {
        None,
        Success,
        Failure
    }

    /// <summary>
    /// Routes gameplay outcomes to the visual feedback component on the active
    /// interactable. It remembers the last selected target so quiz answers can still
    /// color their station while the modal dialog has locked the interaction scanner.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiteboxInteractionFeedbackRouter : MonoBehaviour
    {
        private static readonly string[] FailureTokens =
        {
            "失败", "错误", "异常", "无法", "没有", "缺失", "损坏", "未通过", "未完成",
            "尚未", "不可", "不能", "拒绝", "无效", "failed", "failure",
            "incorrect", "missing", "invalid", "locked", "rejected"
        };

        private static readonly string[] SuccessTokens =
        {
            "成功", "正确", "完成", "已安装", "已拾取", "已解锁", "通过",
            "投入使用", "合格", "accepted", "success", "correct", "installed",
            "collected", "complete", "passed", "unlocked"
        };

        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private PlayerInteractionScanner interactionScanner;

        private WhiteboxGameSessionHost subscribedHost;
        private PlayerInteractionScanner subscribedScanner;
        private IPlayerInteractable lastSelectedTarget;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            PlayerInteractionScanner configuredScanner)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost;
            interactionScanner = configuredScanner;
            lastSelectedTarget = null;
            RememberTarget(interactionScanner == null ? null : interactionScanner.CurrentTarget);
            Subscribe();
        }

        public void RouteFeedbackMessage(string message)
        {
            var outcome = ClassifyFeedback(message);
            if (outcome == InteractionFeedbackOutcome.Success)
                ShowSuccessForCurrentTarget();
            else if (outcome == InteractionFeedbackOutcome.Failure)
                ShowFailureForCurrentTarget();
        }

        public bool ShowSuccessForCurrentTarget()
        {
            return ShowForCurrentTarget(true);
        }

        public bool ShowFailureForCurrentTarget()
        {
            return ShowForCurrentTarget(false);
        }

        public static InteractionFeedbackOutcome ClassifyFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return InteractionFeedbackOutcome.None;

            if (ContainsAny(message, FailureTokens))
                return InteractionFeedbackOutcome.Failure;
            if (ContainsAny(message, SuccessTokens))
                return InteractionFeedbackOutcome.Success;
            return InteractionFeedbackOutcome.None;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled)
                return;

            if (sessionHost != null && subscribedHost != sessionHost)
            {
                subscribedHost = sessionHost;
                subscribedHost.AnswerEvaluated += HandleAnswerEvaluated;
                subscribedHost.FeedbackRequested += RouteFeedbackMessage;
            }

            if (interactionScanner != null && subscribedScanner != interactionScanner)
            {
                subscribedScanner = interactionScanner;
                subscribedScanner.TargetChanged += HandleTargetChanged;
                RememberTarget(subscribedScanner.CurrentTarget);
            }
        }

        private void Unsubscribe()
        {
            if (subscribedHost != null)
            {
                subscribedHost.AnswerEvaluated -= HandleAnswerEvaluated;
                subscribedHost.FeedbackRequested -= RouteFeedbackMessage;
                subscribedHost = null;
            }

            if (subscribedScanner != null)
            {
                subscribedScanner.TargetChanged -= HandleTargetChanged;
                subscribedScanner = null;
            }
        }

        private void HandleAnswerEvaluated(WhiteboxAnswerEvaluatedEvent answerEvent)
        {
            if (answerEvent.Result.IsCorrect)
                ShowSuccessForCurrentTarget();
            else
                ShowFailureForCurrentTarget();
        }

        private void HandleTargetChanged(IPlayerInteractable target)
        {
            RememberTarget(target);
        }

        private void RememberTarget(IPlayerInteractable target)
        {
            if (IsAlive(target))
                lastSelectedTarget = target;
        }

        private bool ShowForCurrentTarget(bool success)
        {
            var currentTarget = interactionScanner == null
                ? null
                : interactionScanner.CurrentTarget;
            var target = IsAlive(currentTarget) ? currentTarget : lastSelectedTarget;
            var visual = FindVisual(target);
            if (visual == null)
                return false;

            if (success)
                visual.ShowSuccess();
            else
                visual.ShowFailure();
            return true;
        }

        private static InteractableVisualFeedback FindVisual(IPlayerInteractable target)
        {
            if (!IsAlive(target) || !(target is Component component))
                return null;

            var visual = component.GetComponent<InteractableVisualFeedback>();
            return visual != null
                ? visual
                : component.GetComponentInParent<InteractableVisualFeedback>();
        }

        private static bool IsAlive(IPlayerInteractable target)
        {
            if (target == null)
                return false;
            return !(target is UnityEngine.Object unityObject) || unityObject != null;
        }

        private static bool ContainsAny(string message, string[] tokens)
        {
            for (var index = 0; index < tokens.Length; index++)
            {
                if (message.IndexOf(tokens[index], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
