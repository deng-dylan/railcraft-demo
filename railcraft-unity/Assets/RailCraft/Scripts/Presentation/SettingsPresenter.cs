using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SettingsPresenter : MonoBehaviour
    {
        private static readonly List<string> QualityOptions = new List<string>
        {
            "低", "中", "高"
        };

        private static readonly List<string> WindowModeOptions = new List<string>
        {
            "窗口化", "全屏窗口"
        };

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Dropdown windowModeDropdown;
        [SerializeField] private Button backButton;
        [SerializeField] private MainMenuPresenter mainMenuPresenter;
        [SerializeField, Range(0, 2)] private int selectedQuality = 1;
        [SerializeField, Range(0, 1)] private int selectedWindowMode;

        private bool subscribed;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
        public int SelectedQuality => selectedQuality;
        public int SelectedWindowMode => selectedWindowMode;
        public int QualityOptionCount => qualityDropdown?.options.Count ?? 0;
        public int WindowModeOptionCount => windowModeDropdown?.options.Count ?? 0;

        public void ConfigureView(GameObject configuredPanelRoot, Dropdown configuredQuality,
            Dropdown configuredWindowMode, Button configuredBack)
        {
            Unsubscribe();
            panelRoot = configuredPanelRoot;
            qualityDropdown = configuredQuality;
            windowModeDropdown = configuredWindowMode;
            backButton = configuredBack;
            PopulateOptions();
            RefreshSelections();
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Bind(MainMenuPresenter configuredMainMenu)
        {
            mainMenuPresenter = configuredMainMenu;
        }

        public void Show()
        {
            panelRoot?.SetActive(true);
            RefreshSelections();
        }

        public void Hide()
        {
            panelRoot?.SetActive(false);
        }

        public void SelectQuality(int optionIndex)
        {
            selectedQuality = Mathf.Clamp(optionIndex, 0, QualityOptions.Count - 1);
            qualityDropdown?.SetValueWithoutNotify(selectedQuality);
            QualitySettings.SetQualityLevel(MapQualityLevel(selectedQuality), true);
        }

        public void SelectWindowMode(int optionIndex)
        {
            selectedWindowMode = Mathf.Clamp(optionIndex, 0, WindowModeOptions.Count - 1);
            windowModeDropdown?.SetValueWithoutNotify(selectedWindowMode);
            Screen.fullScreenMode = selectedWindowMode == 0
                ? FullScreenMode.Windowed
                : FullScreenMode.FullScreenWindow;
        }

        public static int MapQualityLevel(int presetIndex)
        {
            var highest = Mathf.Max(0, QualitySettings.names.Length - 1);
            switch (Mathf.Clamp(presetIndex, 0, 2))
            {
                case 0:
                    return 0;
                case 1:
                    return highest / 2;
                default:
                    return highest;
            }
        }

        private void Awake()
        {
            var currentQuality = QualitySettings.GetQualityLevel();
            var bestPreset = 0;
            var bestDistance = int.MaxValue;
            for (var preset = 0; preset < QualityOptions.Count; preset++)
            {
                var distance = Mathf.Abs(MapQualityLevel(preset) - currentQuality);
                if (distance >= bestDistance)
                    continue;
                bestPreset = preset;
                bestDistance = distance;
            }
            selectedQuality = bestPreset;
            selectedWindowMode = Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : 1;
        }

        private void PopulateOptions()
        {
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(QualityOptions);
            }
            if (windowModeDropdown != null)
            {
                windowModeDropdown.ClearOptions();
                windowModeDropdown.AddOptions(WindowModeOptions);
            }
        }

        private void RefreshSelections()
        {
            qualityDropdown?.SetValueWithoutNotify(selectedQuality);
            windowModeDropdown?.SetValueWithoutNotify(selectedWindowMode);
        }

        private void HandleBack()
        {
            Hide();
            mainMenuPresenter?.Show();
        }

        private void Subscribe()
        {
            if (subscribed)
                return;
            qualityDropdown?.onValueChanged.AddListener(SelectQuality);
            windowModeDropdown?.onValueChanged.AddListener(SelectWindowMode);
            backButton?.onClick.AddListener(HandleBack);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;
            qualityDropdown?.onValueChanged.RemoveListener(SelectQuality);
            windowModeDropdown?.onValueChanged.RemoveListener(SelectWindowMode);
            backButton?.onClick.RemoveListener(HandleBack);
            subscribed = false;
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
    }
}
