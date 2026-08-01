using System;
using System.Collections;
using RailCraft.CameraSystem;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CompletionPresenter : MonoBehaviour
    {
        public const string CompletedMessage = "流程完成：已投入使用";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text messageText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private float releaseTravelDuration = 2f;

        private Transform releasedVehicle;
        private Transform releaseDestination;
        private CameraShotDirector cameraDirector;
        private Coroutine releaseMotion;

        public event Action RestartRequested;
        public event Action ExitRequested;

        public string Message { get; private set; } = string.Empty;
        public string FatalIssueCode { get; private set; }
        public bool IsVisible => panelRoot == null ? !string.IsNullOrEmpty(Message) : panelRoot.activeSelf;
        public bool IsFatal => !string.IsNullOrEmpty(FatalIssueCode);

        public void Configure(GameObject configuredPanelRoot, Text configuredMessageText,
            Button configuredRestartButton, Button configuredExitButton)
        {
            RemoveButtonListeners();
            panelRoot = configuredPanelRoot;
            messageText = configuredMessageText;
            restartButton = configuredRestartButton;
            exitButton = configuredExitButton;
            AddButtonListeners();
            Hide();
        }

        public void ConfigureReleaseScene(Transform configuredVehicle,
            Transform configuredDestination, CameraShotDirector configuredCameraDirector,
            float configuredTravelDuration = -1f)
        {
            releasedVehicle = configuredVehicle;
            releaseDestination = configuredDestination;
            cameraDirector = configuredCameraDirector;
            if (configuredTravelDuration >= 0f)
                releaseTravelDuration = configuredTravelDuration;
        }

        public void ShowCompleted()
        {
            ShowMessage(CompletedMessage, null);
            if (cameraDirector != null)
                cameraDirector.Focus("hero");
            if (releaseMotion != null)
                StopCoroutine(releaseMotion);
            if (releasedVehicle != null && releaseDestination != null)
                releaseMotion = StartCoroutine(MoveReleasedVehicle());
        }

        public void ShowFatal(string issueCode)
        {
            var code = string.IsNullOrWhiteSpace(issueCode) ? "unknown" : issueCode;
            ShowMessage($"内容加载失败（{code}）", code);
            if (restartButton != null)
                restartButton.gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (releaseMotion != null)
                StopCoroutine(releaseMotion);
            releaseMotion = null;
            Message = string.Empty;
            FatalIssueCode = null;
            if (messageText != null)
                messageText.text = string.Empty;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (restartButton != null)
                restartButton.gameObject.SetActive(true);
        }

        private void ShowMessage(string message, string fatalIssueCode)
        {
            Message = message ?? string.Empty;
            FatalIssueCode = fatalIssueCode;
            if (messageText != null)
                messageText.text = Message;
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private IEnumerator MoveReleasedVehicle()
        {
            var start = releasedVehicle.position;
            var target = releaseDestination.position;
            var elapsed = 0f;
            var duration = Mathf.Max(0f, releaseTravelDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = duration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, elapsed / duration);
                releasedVehicle.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
            releasedVehicle.position = target;
            releaseMotion = null;
        }

        private void AddButtonListeners()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(HandleRestart);
            if (exitButton != null)
                exitButton.onClick.AddListener(HandleExit);
        }

        private void RemoveButtonListeners()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(HandleRestart);
            if (exitButton != null)
                exitButton.onClick.RemoveListener(HandleExit);
        }

        private void HandleRestart() => RestartRequested?.Invoke();
        private void HandleExit() => ExitRequested?.Invoke();

        private void Awake()
        {
            RemoveButtonListeners();
            AddButtonListeners();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }
    }
}
