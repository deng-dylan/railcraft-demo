using System;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    [DisallowMultipleComponent]
    public sealed class WhiteboxMainMenuController : MonoBehaviour
    {
        private const AssemblyVariantId StandardAssemblyVariant = AssemblyVariantId.FuxingDemo;

        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private WhiteboxSaveController saveController;
        [SerializeField] private ThirdPersonInputLock inputLock;
        [SerializeField] private WhiteboxKnowledgePresenter knowledgePresenter;
        [SerializeField] private GameObject mainMenuRoot;
        [SerializeField] private GameObject settingsRoot;
        [SerializeField] private Text menuTitleText;
        [SerializeField] private Text menuSubtitleText;
        [SerializeField] private Text menuFootnoteText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Text volumeValueText;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Dropdown assemblyVariantDropdown;

        private bool wired;
        private bool menuOwnsTimingPause;
        private bool menuOwnsInputLock;
        private bool useSessionVariantForNextStart;

        public bool IsMenuVisible => mainMenuRoot != null && mainMenuRoot.activeSelf;
        public bool IsSettingsVisible => settingsRoot != null && settingsRoot.activeSelf;
        public bool HasActiveGame => saveController != null && saveController.HasActiveSession;
        public AssemblyVariantId SelectedAssemblyVariant => ResolveSelectedVariant();

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
            Button configuredMenuButton,
            Text configuredMenuTitleText = null,
            Text configuredMenuSubtitleText = null,
            Text configuredMenuFootnoteText = null,
            WhiteboxKnowledgePresenter configuredKnowledgePresenter = null,
            Dropdown configuredAssemblyVariantDropdown = null)
        {
            Unwire();
            sessionHost = configuredSessionHost;
            saveController = configuredSaveController;
            inputLock = configuredInputLock;
            knowledgePresenter = configuredKnowledgePresenter;
            mainMenuRoot = configuredMainMenuRoot;
            settingsRoot = configuredSettingsRoot;
            menuTitleText = configuredMenuTitleText;
            menuSubtitleText = configuredMenuSubtitleText;
            menuFootnoteText = configuredMenuFootnoteText;
            assemblyVariantDropdown = configuredAssemblyVariantDropdown;
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
            PopulateAssemblyVariantOptions();
            LoadSettingsControls();
            Wire();
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            var session = sessionHost?.Session;
            if (!menuOwnsTimingPause && session != null && !session.IsTimingPaused)
            {
                session.PauseTiming();
                menuOwnsTimingPause = session.IsTimingPaused;
            }
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(true);
            if (settingsRoot != null)
                settingsRoot.SetActive(false);
            if (!menuOwnsInputLock && inputLock != null && !inputLock.InputLocked)
            {
                inputLock.SetInputLocked(true);
                menuOwnsInputLock = true;
            }
            RefreshMenuPresentation();
            saveController?.SaveCurrentSession();
        }

        public bool HandleEscapePressed()
        {
            if (IsSettingsVisible)
            {
                ApplySettingsAndReturn();
                return true;
            }

            if (IsMenuVisible)
            {
                if (IsKnowledgeViewBlockingMenu())
                    return false;
                return ResumeCurrentGame();
            }

            if (!HasActiveGame || IsKnowledgeViewBlockingMenu() || inputLock != null && inputLock.InputLocked)
                return false;

            ShowMainMenu();
            return true;
        }

        public bool ResumeCurrentGame()
        {
            if (!HasActiveGame)
                return false;

            CloseMenuForPlay();
            return true;
        }

        public void StartNewGame()
        {
            var variant = ResolveSelectedVariant();
            useSessionVariantForNextStart = false;
            if (saveController != null)
                saveController.StartNewGame(variant);
            else
            {
                sessionHost?.SelectAssemblyVariant(variant);
                sessionHost?.ResetSession();
            }
            CloseMenuForPlay();
        }

        public bool ContinueGame()
        {
            if (HasActiveGame)
                return ResumeCurrentGame();

            if (saveController == null || !saveController.TryContinueGame())
            {
                RefreshMenuPresentation();
                return false;
            }

            CloseMenuForPlay();
            return true;
        }

        private void Awake()
        {
            // Scene generation serializes the initial menu and its shared input lock as active.
            // Reclaim that lock after loading so Start/Continue can release only the menu's lock.
            if (IsMenuVisible && inputLock != null && inputLock.InputLocked)
                menuOwnsInputLock = true;
        }

        private void Start()
        {
            WhiteboxRuntimeSettings.Apply(WhiteboxRuntimeSettings.Load());
            var arguments = Environment.GetCommandLineArgs();
            if (arguments.Any(argument =>
                string.Equals(argument, WhiteboxAutomatedSmokeRunner.SmokeArgument,
                    StringComparison.OrdinalIgnoreCase)))
            {
                if (WhiteboxAutomatedSmokeRunner.TryGetRequestedVariant(
                        arguments,
                        out var requestedVariant))
                {
                    sessionHost?.SelectAssemblyVariant(requestedVariant);
                    useSessionVariantForNextStart = true;
                    if (assemblyVariantDropdown != null)
                        assemblyVariantDropdown.SetValueWithoutNotify((int)requestedVariant);
                }
                StartNewGame();
                return;
            }

            ShowMainMenu();
        }

        private void LateUpdate()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                HandleEscapePressed();
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
            menuButton?.onClick.AddListener(HandleMenuButtonClicked);
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
            menuButton?.onClick.RemoveListener(HandleMenuButtonClicked);
            volumeSlider?.onValueChanged.RemoveListener(HandleVolumeChanged);
            qualityDropdown?.onValueChanged.RemoveListener(HandleQualityChanged);
            wired = false;
        }

        private void CloseMenuForPlay()
        {
            if (menuOwnsTimingPause)
                sessionHost?.Session.ResumeTiming();
            menuOwnsTimingPause = false;
            if (settingsRoot != null)
                settingsRoot.SetActive(false);
            if (mainMenuRoot != null)
                mainMenuRoot.SetActive(false);
            if (menuOwnsInputLock)
            {
                var anotherViewOwnsInput = sessionHost != null && sessionHost.Session.IsVehicleComplete ||
                    IsKnowledgeViewBlockingMenu();
                inputLock?.SetInputLocked(anotherViewOwnsInput);
            }
            menuOwnsInputLock = false;
            saveController?.SaveCurrentSession();
        }

        private void HandleContinueClicked()
        {
            ContinueGame();
        }

        private void HandleMenuButtonClicked()
        {
            HandleEscapePressed();
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
            RefreshMenuPresentation();
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

        private void RefreshMenuPresentation()
        {
            var hasActiveGame = HasActiveGame;
            if (menuTitleText != null)
                menuTitleText.text = hasActiveGame ? "实训已暂停" : "高速动车组转向架装配与调试实训";
            if (menuSubtitleText != null)
            {
                menuSubtitleText.text = hasActiveGame
                    ? "当前工位与计时已暂停 · 按 ESC 或“继续实训”返回现场"
                    : "标准工单 RC-EMU-01 · 知识确认 · 子总成装配 · 落车 · 调试检验";
            }
            if (menuFootnoteText != null)
            {
                menuFootnoteText.text = hasActiveGame
                    ? "当前进度已自动保存；重新开始会清空本轮标准实训进度。"
                    : "本轮详细装配一套代表性转向架；扩展示范方案仅用于模型接入验证。";
            }
            SetButtonLabel(startButton, hasActiveGame ? "重新开始标准实训" : "开始标准实训");
            SetButtonLabel(continueButton, hasActiveGame ? "继续实训" : "继续实训");
            if (continueButton != null)
                continueButton.interactable = HasActiveGame || saveController != null && saveController.HasSave;
            if (assemblyVariantDropdown != null && sessionHost != null)
            {
                var menuVariant = hasActiveGame
                    ? sessionHost.SelectedAssemblyVariant
                    : StandardAssemblyVariant;
                assemblyVariantDropdown.SetValueWithoutNotify((int)menuVariant);
                assemblyVariantDropdown.interactable = false;
                assemblyVariantDropdown.gameObject.SetActive(false);
            }
        }

        private void PopulateAssemblyVariantOptions()
        {
            if (assemblyVariantDropdown == null)
                return;

            assemblyVariantDropdown.ClearOptions();
            var labels = AssemblyVariantCatalog.Definitions
                .Select(definition => definition.MenuLabel)
                .ToList();
            assemblyVariantDropdown.AddOptions(labels);
            assemblyVariantDropdown.SetValueWithoutNotify((int)StandardAssemblyVariant);
            assemblyVariantDropdown.interactable = false;
            assemblyVariantDropdown.gameObject.SetActive(false);
        }

        private AssemblyVariantId ResolveSelectedVariant()
        {
            if (useSessionVariantForNextStart && sessionHost != null)
                return sessionHost.SelectedAssemblyVariant;

            return StandardAssemblyVariant;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button == null ? null : button.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label;
        }

        private bool IsKnowledgeViewBlockingMenu()
        {
            return knowledgePresenter != null &&
                (knowledgePresenter.IsAnyViewOpen || knowledgePresenter.PendingPopupCount > 0);
        }

        private static void QuitGame()
        {
            Application.Quit();
        }
    }
}
