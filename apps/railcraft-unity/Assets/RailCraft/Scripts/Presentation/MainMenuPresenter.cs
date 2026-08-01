using RailCraft.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button startButton;
        [SerializeField] private Button guidanceButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GuidedFlowController flowController;
        [SerializeField] private GuidancePresenter guidancePresenter;
        [SerializeField] private SettingsPresenter settingsPresenter;

        private bool interactionsSubscribed;
        private bool controllerSubscribed;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        public void ConfigureView(GameObject configuredPanelRoot, Button configuredStart,
            Button configuredGuidance, Button configuredSettings, Button configuredExit)
        {
            UnsubscribeInteractions();
            panelRoot = configuredPanelRoot;
            startButton = configuredStart;
            guidanceButton = configuredGuidance;
            settingsButton = configuredSettings;
            exitButton = configuredExit;
            if (isActiveAndEnabled)
                SubscribeInteractions();
            RefreshAvailability();
        }

        public void Bind(GuidedFlowController configuredController,
            GuidancePresenter configuredGuidance, SettingsPresenter configuredSettings)
        {
            UnsubscribeController();
            flowController = configuredController;
            guidancePresenter = configuredGuidance;
            settingsPresenter = configuredSettings;
            if (isActiveAndEnabled)
                SubscribeController();
            RefreshAvailability();
        }

        public void Show()
        {
            if (panelRoot == null)
                return;
            panelRoot.SetActive(true);
            RefreshAvailability();
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void StartExperience()
        {
            if (flowController == null)
                return;
            flowController.StartNewRun();
            if (flowController.Snapshot.Phase != FlowPhase.Guidance)
                return;
            Hide();
            guidancePresenter?.ShowForRun();
        }

        private void ShowGuidance()
        {
            Hide();
            guidancePresenter?.ShowForInformation();
        }

        private void ShowSettings()
        {
            Hide();
            settingsPresenter?.Show();
        }

        private void ExitApplication()
        {
            flowController?.ExitApplication();
        }

        private void HandleStateChanged(FlowSnapshot snapshot)
        {
            RefreshAvailability();
        }

        private void RefreshAvailability()
        {
            if (startButton != null)
            {
                startButton.interactable = flowController != null
                    && flowController.IsConfigured
                    && flowController.Snapshot.Phase == FlowPhase.MainMenu;
            }
        }

        private void SubscribeInteractions()
        {
            if (interactionsSubscribed)
                return;
            startButton?.onClick.AddListener(StartExperience);
            guidanceButton?.onClick.AddListener(ShowGuidance);
            settingsButton?.onClick.AddListener(ShowSettings);
            exitButton?.onClick.AddListener(ExitApplication);
            interactionsSubscribed = true;
        }

        private void UnsubscribeInteractions()
        {
            if (!interactionsSubscribed)
                return;
            startButton?.onClick.RemoveListener(StartExperience);
            guidanceButton?.onClick.RemoveListener(ShowGuidance);
            settingsButton?.onClick.RemoveListener(ShowSettings);
            exitButton?.onClick.RemoveListener(ExitApplication);
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
            RefreshAvailability();
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
