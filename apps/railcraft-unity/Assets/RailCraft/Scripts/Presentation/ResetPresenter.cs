using RailCraft.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ResetPresenter : MonoBehaviour
    {
        public const string RequiredConfirmationCopy =
            "重置后将清除本次流程进度并返回操作说明。是否继续？";

        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private Text confirmationText;
        [SerializeField] private Button requestButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GuidedFlowController flowController;
        [SerializeField] private GuidancePresenter guidancePresenter;
        [SerializeField] private MainMenuPresenter mainMenuPresenter;
        [SerializeField] private SettingsPresenter settingsPresenter;

        private bool interactionsSubscribed;
        private bool controllerSubscribed;

        public bool IsConfirmationVisible => confirmationPanel != null
            && confirmationPanel.activeSelf;
        public string ConfirmationCopy => confirmationText == null
            ? string.Empty
            : confirmationText.text;

        public void ConfigureView(GameObject configuredPanel, Text configuredText,
            Button configuredRequest, Button configuredConfirm, Button configuredCancel)
        {
            UnsubscribeInteractions();
            confirmationPanel = configuredPanel;
            confirmationText = configuredText;
            requestButton = configuredRequest;
            confirmButton = configuredConfirm;
            cancelButton = configuredCancel;
            if (confirmationText != null)
                confirmationText.text = RequiredConfirmationCopy;
            if (isActiveAndEnabled)
                SubscribeInteractions();
        }

        public void Bind(GuidedFlowController configuredController,
            GuidancePresenter configuredGuidance, MainMenuPresenter configuredMainMenu,
            SettingsPresenter configuredSettings)
        {
            UnsubscribeController();
            flowController = configuredController;
            guidancePresenter = configuredGuidance;
            mainMenuPresenter = configuredMainMenu;
            settingsPresenter = configuredSettings;
            if (isActiveAndEnabled)
                SubscribeController();
            RefreshRequestVisibility();
        }

        public void ShowConfirmation()
        {
            if (flowController == null || !CanReset(flowController.Snapshot.Phase))
                return;
            confirmationPanel?.SetActive(true);
        }

        public void HideConfirmation()
        {
            confirmationPanel?.SetActive(false);
        }

        private void ConfirmReset()
        {
            if (flowController == null || !CanReset(flowController.Snapshot.Phase))
                return;
            flowController.ResetRun();
            mainMenuPresenter?.Hide();
            settingsPresenter?.Hide();
            guidancePresenter?.ShowForRun();
            HideConfirmation();
        }

        private void HandleStateChanged(FlowSnapshot snapshot)
        {
            RefreshRequestVisibility();
            if (!CanReset(snapshot.Phase))
                HideConfirmation();
            if (snapshot.Phase == FlowPhase.Guidance)
            {
                mainMenuPresenter?.Hide();
                settingsPresenter?.Hide();
                guidancePresenter?.ShowForRun();
            }
        }

        private void RefreshRequestVisibility()
        {
            if (requestButton != null)
                requestButton.gameObject.SetActive(flowController != null
                    && CanReset(flowController.Snapshot.Phase));
        }

        private static bool CanReset(FlowPhase phase)
        {
            return phase == FlowPhase.KnowledgeGate
                || phase == FlowPhase.StepReady
                || phase == FlowPhase.Snapping
                || phase == FlowPhase.Rework
                || phase == FlowPhase.SecondCommissioning;
        }

        private void SubscribeInteractions()
        {
            if (interactionsSubscribed)
                return;
            requestButton?.onClick.AddListener(ShowConfirmation);
            confirmButton?.onClick.AddListener(ConfirmReset);
            cancelButton?.onClick.AddListener(HideConfirmation);
            interactionsSubscribed = true;
        }

        private void UnsubscribeInteractions()
        {
            if (!interactionsSubscribed)
                return;
            requestButton?.onClick.RemoveListener(ShowConfirmation);
            confirmButton?.onClick.RemoveListener(ConfirmReset);
            cancelButton?.onClick.RemoveListener(HideConfirmation);
            interactionsSubscribed = false;
        }

        private void SubscribeController()
        {
            if (controllerSubscribed || flowController == null)
                return;
            flowController.StateChanged += HandleStateChanged;
            controllerSubscribed = true;
        }

        private void UnsubscribeController()
        {
            if (!controllerSubscribed)
                return;
            if (flowController != null)
                flowController.StateChanged -= HandleStateChanged;
            controllerSubscribed = false;
        }

        private void OnEnable()
        {
            SubscribeInteractions();
            SubscribeController();
            RefreshRequestVisibility();
        }

        private void OnDisable()
        {
            UnsubscribeInteractions();
            UnsubscribeController();
        }

        private void OnDestroy()
        {
            UnsubscribeInteractions();
            UnsubscribeController();
        }
    }
}
