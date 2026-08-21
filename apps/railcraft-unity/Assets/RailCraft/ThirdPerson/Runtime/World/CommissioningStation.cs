using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    public enum CommissioningAction
    {
        Test,
        Retune,
        Inspect
    }

    [DisallowMultipleComponent]
    public sealed class CommissioningStation : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private CommissioningAction action;
        [SerializeField] private string stationDisplayName = "调试工位";
        [SerializeField] private GameObject readyVisual;
        [SerializeField] private GameObject completedVisual;

        private WhiteboxGameSessionHost subscribedHost;
        private const string InjectedFaultMessage =
            "教学故障注入：传感器信号一致性异常（占位）";

        public CommissioningAction Action => action;
        public bool IsReady => sessionHost != null && IsReadyFor(sessionHost.Session.CommissioningPhase);

        public string InteractionPrompt
        {
            get
            {
                if (sessionHost == null || sessionHost.Session.IsVehicleComplete)
                    return string.Empty;
                if (IsReady)
                    return $"按 E 执行{stationDisplayName}";

                switch (sessionHost.Session.CommissioningPhase)
                {
                    case CommissioningPhase.Locked:
                        return "请先完成落车集成";
                    case CommissioningPhase.NeedsRetuning:
                        return action == CommissioningAction.Retune ? string.Empty : "请前往重新调试工位";
                    case CommissioningPhase.ReadyForInspection:
                        return action == CommissioningAction.Inspect ? string.Empty : "请前往检验工位";
                    case CommissioningPhase.ReadyForInitialTest:
                    case CommissioningPhase.ReadyForRetest:
                        return action == CommissioningAction.Test ? string.Empty : "请前往调试判定工位";
                    default:
                        return string.Empty;
                }
            }
        }

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            CommissioningAction configuredAction,
            string configuredStationDisplayName,
            GameObject configuredReadyVisual,
            GameObject configuredCompletedVisual)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost;
            action = configuredAction;
            stationDisplayName = string.IsNullOrWhiteSpace(configuredStationDisplayName)
                ? "调试工位"
                : configuredStationDisplayName;
            readyVisual = configuredReadyVisual;
            completedVisual = configuredCompletedVisual;
            Subscribe();
            RefreshVisuals();
        }

        public bool CanInteract(InteractionContext context)
        {
            return sessionHost != null && !sessionHost.Session.IsVehicleComplete;
        }

        public void Interact(InteractionContext context)
        {
            if (!CanInteract(context))
                return;
            if (!IsReady)
            {
                sessionHost.NotifyFeedback(InteractionPrompt);
                return;
            }

            WorldCommissioningResult result;
            switch (action)
            {
                case CommissioningAction.Retune:
                    result = sessionHost.PerformRetuning();
                    break;
                case CommissioningAction.Inspect:
                    result = sessionHost.PerformInspection();
                    break;
                default:
                    result = sessionHost.RunCommissioning();
                    break;
            }

            if (!result.Accepted)
            {
                sessionHost.NotifyFeedback($"当前阶段无法执行{stationDisplayName}（{result.Status}）");
                return;
            }

            RefreshVisuals();
            switch (action)
            {
                case CommissioningAction.Retune:
                    sessionHost.NotifyFeedback("教学故障处置完成，等待检验确认");
                    sessionHost.SetObjective("前往检验工位确认处置结果");
                    break;
                case CommissioningAction.Inspect:
                    sessionHost.NotifyFeedback("检验完成，返回调试判定进行复测");
                    sessionHost.SetObjective("返回调试判定工位进行复测");
                    break;
                default:
                    if (result.Passed)
                    {
                        sessionHost.NotifyFeedback("复测通过，车辆通过调试检验");
                    }
                    else
                    {
                        sessionHost.NotifyFeedback($"{InjectedFaultMessage}，请前往重新调试工位");
                        sessionHost.SetObjective("前往重新调试工位处理教学故障");
                    }
                    break;
            }
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshVisuals();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || sessionHost == null || subscribedHost == sessionHost)
                return;
            subscribedHost = sessionHost;
            subscribedHost.StateChanged += RefreshVisuals;
            subscribedHost.SessionReset += RefreshVisuals;
        }

        private void Unsubscribe()
        {
            if (subscribedHost == null)
                return;
            subscribedHost.StateChanged -= RefreshVisuals;
            subscribedHost.SessionReset -= RefreshVisuals;
            subscribedHost = null;
        }

        private bool IsReadyFor(CommissioningPhase phase)
        {
            switch (action)
            {
                case CommissioningAction.Retune:
                    return phase == CommissioningPhase.NeedsRetuning;
                case CommissioningAction.Inspect:
                    return phase == CommissioningPhase.ReadyForInspection;
                default:
                    return phase == CommissioningPhase.ReadyForInitialTest
                        || phase == CommissioningPhase.ReadyForRetest;
            }
        }

        private void RefreshVisuals()
        {
            var phase = sessionHost == null
                ? CommissioningPhase.Locked
                : sessionHost.Session.CommissioningPhase;
            if (readyVisual != null)
                readyVisual.SetActive(IsReadyFor(phase));
            if (completedVisual != null)
                completedVisual.SetActive(IsCompletedAt(phase));
        }

        private bool IsCompletedAt(CommissioningPhase phase)
        {
            switch (action)
            {
                case CommissioningAction.Retune:
                    return phase == CommissioningPhase.ReadyForInspection
                        || phase == CommissioningPhase.ReadyForRetest
                        || phase == CommissioningPhase.InService;
                case CommissioningAction.Inspect:
                    return phase == CommissioningPhase.ReadyForRetest
                        || phase == CommissioningPhase.InService;
                default:
                    return phase == CommissioningPhase.NeedsRetuning
                        || phase == CommissioningPhase.InService;
            }
        }
    }
}
