using System;
using System.Linq;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    [DisallowMultipleComponent]
    public sealed class WhiteboxMainMenuController : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private WhiteboxSaveController saveController;
        [SerializeField] private ThirdPersonInputLock inputLock;
        [SerializeField] private GameObject mainMenuRoot;
        [SerializeField] private GameObject settingsRoot;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeValueText;
        [SerializeField] private Dropdown qualityDropdown;

        private bool wired;

        public bool IsMenuVisible => mainMenuRoot != null && mainMenuRoot.activeSelf;
        public bool IsSettingsVisible => settingsRoot != null && settingsRoot.activeSelf;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            WhiteboxSaveController configuredSaveController,
            ThirdPersonInputLock configuredInputLock,
            GameObject configuredMainMenuRoot,
            GameObject configuredSettingsRoot,
            Button configuredStartButton,
            Button configuredContinueButton,
            Button configuredSettingsButton,
            Button configuredQuitButton,
            Button configuredSettingsBackButton,
            Slider configuredVolumeSlider,
            Text configuredVolumeValueText,
            Dropdown configuredQualityDropdown,
            Button configuredMenuButton)
        {
            Unwire();
            sessionHost = configuredSessionHost;
            saveController = configuredSaveController;
            inputLock = configuredInputLock;
            mainMenuRoot = configuredMainMenuRoot;
            settingsRoot = configuredSettingsRoot;
            startButton = configuredStartButton;
            continueButton = configuredContinueButton;
            settingsButton = configuredSettingsButton;
            quitButton = configuredQuitButton;
            settingsBackButton = configuredSettingsBackButton;
            volumeSlider = configuredVolumeSlider;
            volumeValueText = configuredVolumeValueText;
            qualityDropdown = configuredQualityDropdown;
            menuButton = configuredMenuButton;
            PopulateQualityOptions();
            LoadSettingsControls();
            Wire();
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            sessionHost?.Session.PauseTiming();
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(true);
            if (settingsRoot != null)
                settingsRoot.SetActive(false);
            inputLock?.SetInputLocked(true);
            RefreshContinueButton();
            saveController?.SaveCurrentSession();
        }

        public void StartNewGame()
        {
            if (saveController != null)
                saveController.StartNewGame();
            else
                sessionHost?.ResetSession();
            CloseMenuForPlay();
        }

        public bool ContinueGame()
        {
            if (saveController == null || !saveController.TryContinueGame())
            {
                RefreshContinueButton();
                return false;
            }

            CloseMenuForPlay();
            return true;
        }

        private void Start()
        {
            WhiteboxRuntimeSettings.Apply(WhiteboxRuntimeSettings.Load());
            if (Environment.GetCommandLineArgs().Any(argument =>
                string.Equals(argument, WhiteboxAutomatedSmokeRunner.SmokeArgument,
                    StringComparison.OrdinalIgnoreCase)))
            {
                StartNewGame();
                return;
            }

            ShowMainMenu();
        }

        private void OnEnable()
        {
            Wire();
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void Wire()
        {
            if (wired || !isActiveAndEnabled)
                return;

            startButton?.onClick.AddListener(StartNewGame);
            continueButton?.onClick.AddListener(HandleContinueClicked);
            settingsButton?.onClick.AddListener(ShowSettings);
            quitButton?.onClick.AddListener(QuitGame);
            settingsBackButton?.onClick.AddListener(ApplySettingsAndReturn);
            menuButton?.onClick.AddListener(ShowMainMenu);
            volumeSlider?.onValueChanged.AddListener(HandleVolumeChanged);
            qualityDropdown?.onValueChanged.AddListener(HandleQualityChanged);
            wired = true;
        }

        private void Unwire()
        {
            if (!wired)
                return;

            startButton?.onClick.RemoveListener(StartNewGame);
            continueButton?.onClick.RemoveListener(HandleContinueClicked);
            settingsButton?.onClick.RemoveListener(ShowSettings);
            quitButton?.onClick.RemoveListener(QuitGame);
            settingsBackButton?.onClick.RemoveListener(ApplySettingsAndReturn);
            menuButton?.onClick.RemoveListener(ShowMainMenu);
            volumeSlider?.onValueChanged.RemoveListener(HandleVolumeChanged);
            qualityDropdown?.onValueChanged.RemoveListener(HandleQualityChanged);
            wired = false;
        }

        private void CloseMenuForPlay()
        {
            sessionHost?.Session.ResumeTiming();
            if (settingsRoot != null)
                settingsRoot.SetActive(false);
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);
            inputLock?.SetInputLocked(sessionHost != null && sessionHost.Session.IsVehicleComplete);
            saveController?.SaveCurrentSession();
        }

        private void HandleContinueClicked()
        {
            ContinueGame();
        }

        private void ShowSettings()
        {
            LoadSettingsControls();
            if (settingsRoot != null)
                settingsRoot.SetActive(true);
        }

        private void ApplySettingsAndReturn()
        {
            SaveSettingsControls();
            if (settingsRoot != null)
                settingsRoot.SetActive(false);
        }

        private void HandleVolumeChanged(float value)
        {
            if (volumeValueText != null)
                volumeValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            AudioListener.volume = Mathf.Clamp01(value);
        }

        private void HandleQualityChanged(int value)
        {
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(Mathf.Clamp(value, 0, QualitySettings.names.Length - 1), true);
        }

        private void LoadSettingsControls()
        {
            var state = WhiteboxRuntimeSettings.Load();
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(state.MasterVolume);
                HandleVolumeChanged(state.MasterVolume);
            }
            if (qualityDropdown != null && QualitySettings.names.Length > 0)
                qualityDropdown.SetValueWithoutNotify(state.QualityLevel);
        }

        private void SaveSettingsControls()
        {
            var volume = volumeSlider == null ? AudioListener.volume : volumeSlider.value;
            var quality = qualityDropdown == null
                ? QualitySettings.GetQualityLevel()
                : qualityDropdown.value;
            WhiteboxRuntimeSettings.Save(volume, quality);
        }

        private void PopulateQualityOptions()
        {
            if (qualityDropdown == null)
                return;

            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(QualitySettings.names.ToList());
            qualityDropdown.interactable = QualitySettings.names.Length > 1;
        }

        private void RefreshContinueButton()
        {
            if (continueButton != null)
                continueButton.interactable = saveController != null && saveController.HasSave;
        }

        private static void QuitGame()
        {
            Application.Quit();
        }
    }
}
