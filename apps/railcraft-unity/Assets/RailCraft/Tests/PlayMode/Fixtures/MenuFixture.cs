using System;
using System.Linq;
using RailCraft.Flow;
using RailCraft.Presentation;
using RailCraft.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class MenuFixture : IDisposable
    {
        private readonly GameObject root;
        private readonly FullFlowFixture flow;
        private readonly int originalQualityLevel;
        private readonly FullScreenMode originalWindowMode;
        private readonly GameObject settingsPanel;

        public MainMenuPresenter MainMenu { get; }
        public GuidancePresenter Guidance { get; }
        public SettingsPresenter Settings { get; }
        public ResetPresenter Reset { get; }
        public FlowPhase Phase => flow.Controller.Snapshot.Phase;
        public int InstalledVisualCount => flow.Assembly.InstalledVisualCount;
        public GameObject FirstInstalledVisual => flow.Assembly.GetInstalledVisual("frame_module");
        public DraggableModule CurrentModule => flow.Assembly.CurrentModule;
        public bool IsPartDragActive => flow.IsPartDragActive;
        public int QuestionsAnswered => flow.Controller.QuestionsAnswered;
        public int CompletedUniqueSteps => flow.Controller.CompletedUniqueSteps;
        public int CommissioningAttempt => flow.Controller.Snapshot.CommissioningAttempt;
        public string GuidanceCopy => Guidance.Copy;
        public string ResetConfirmationCopy => Reset.ConfirmationCopy;

        private MenuFixture(GameObject root, FullFlowFixture flow,
            MainMenuPresenter mainMenu, GuidancePresenter guidance,
            SettingsPresenter settings, ResetPresenter reset, GameObject settingsPanel)
        {
            this.root = root;
            this.flow = flow;
            this.settingsPanel = settingsPanel;
            MainMenu = mainMenu;
            Guidance = guidance;
            Settings = settings;
            Reset = reset;
            originalQualityLevel = QualitySettings.GetQualityLevel();
            originalWindowMode = Screen.fullScreenMode;
        }

        public static MenuFixture Create()
        {
            var flow = FullFlowFixture.Create();
            var root = new GameObject("menu.fixture");

            var mainPanel = CreatePanel(root.transform, "MainMenu");
            var start = CreateButton(mainPanel.transform, "StartButton", "开始体验");
            var guidanceButton = CreateButton(mainPanel.transform, "GuidanceButton", "操作说明");
            var settingsButton = CreateButton(mainPanel.transform, "SettingsButton", "设置");
            var exit = CreateButton(mainPanel.transform, "ExitButton", "退出");
            var mainMenu = mainPanel.AddComponent<MainMenuPresenter>();
            mainMenu.ConfigureView(mainPanel, start, guidanceButton, settingsButton, exit);

            var guidancePanel = CreatePanel(root.transform, "GuidancePanel");
            var guidanceCopy = CreateText(guidancePanel.transform, "GuidanceCopy", string.Empty);
            var guidancePrimary = CreateButton(guidancePanel.transform, "GuidancePrimaryButton", "继续");
            var guidance = guidancePanel.AddComponent<GuidancePresenter>();
            guidance.ConfigureView(guidancePanel, guidanceCopy, guidancePrimary);

            var settingsPanel = CreatePanel(root.transform, "SettingsPanel");
            CreateText(settingsPanel.transform, "QualityLabel", "画质");
            var quality = CreateDropdown(settingsPanel.transform, "QualityDropdown");
            CreateText(settingsPanel.transform, "WindowModeLabel", "窗口模式");
            var windowMode = CreateDropdown(settingsPanel.transform, "WindowModeDropdown");
            var settingsBack = CreateButton(settingsPanel.transform, "SettingsBackButton", "返回主菜单");
            var settings = settingsPanel.AddComponent<SettingsPresenter>();
            settings.ConfigureView(settingsPanel, quality, windowMode, settingsBack);

            var resetHost = new GameObject("ResetHost");
            resetHost.transform.SetParent(root.transform, false);
            var resetRequest = CreateButton(resetHost.transform, "ResetRequestButton", "重置流程");
            var resetPanel = CreatePanel(resetHost.transform, "ResetConfirmationPanel");
            var resetCopy = CreateText(resetPanel.transform, "ResetConfirmationCopy", string.Empty);
            var resetConfirm = CreateButton(resetPanel.transform, "ResetConfirmButton", "确认重置");
            var resetCancel = CreateButton(resetPanel.transform, "ResetCancelButton", "取消");
            var reset = resetHost.AddComponent<ResetPresenter>();
            reset.ConfigureView(resetPanel, resetCopy, resetRequest, resetConfirm, resetCancel);

            guidance.Bind(flow.Controller, mainMenu);
            settings.Bind(mainMenu);
            mainMenu.Bind(flow.Controller, guidance, settings);
            reset.Bind(flow.Controller, guidance, mainMenu, settings);

            mainMenu.Show();
            guidance.Hide();
            settings.Hide();
            reset.HideConfirmation();

            return new MenuFixture(root, flow, mainMenu, guidance, settings, reset, settingsPanel);
        }

        public static MenuFixture CreateWithProgress()
        {
            var fixture = Create();
            fixture.ClickButton("开始体验");
            fixture.ClickButton("开始装配");
            while (fixture.flow.Controller.Snapshot.Phase == FlowPhase.KnowledgeGate)
                fixture.flow.AnswerCurrentQuestionCorrectlyWhenVisible();
            fixture.flow.DropCurrentItemWhenUnlocked();
            if (fixture.InstalledVisualCount == 0)
                throw new InvalidOperationException("Failed to seed installed progress for the reset test.");
            return fixture;
        }

        public Button FindButton(string label)
        {
            return root.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
                button.GetComponentsInChildren<Text>(true).Any(text => text.text == label));
        }

        public bool HasControl(string label)
        {
            return settingsPanel.GetComponentsInChildren<Text>(true)
                .Any(text => text.text == label);
        }

        public void ClickButton(string label)
        {
            var button = FindButton(label);
            if (button == null)
                throw new InvalidOperationException($"Button '{label}' was not found.");
            button.onClick.Invoke();
        }

        public void OpenSettings() => ClickButton("设置");

        public void SelectQuality(int optionIndex) => Settings.SelectQuality(optionIndex);

        public void SelectWindowMode(int optionIndex) => Settings.SelectWindowMode(optionIndex);

        public void ConfirmReset()
        {
            ClickButton("重置流程");
            ClickButton("确认重置");
        }

        public void ResetControllerDirectly() => flow.Controller.ResetRun();

        public void UnlockCurrentStepAndBeginDrag()
        {
            while (flow.Controller.Snapshot.Phase == FlowPhase.KnowledgeGate)
                flow.AnswerCurrentQuestionCorrectlyWhenVisible();
            flow.BeginCurrentDrag();
        }

        public void Dispose()
        {
            QualitySettings.SetQualityLevel(originalQualityLevel, false);
            Screen.fullScreenMode = originalWindowMode;
            UnityEngine.Object.Destroy(root);
            flow.Dispose();
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string value)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            var text = item.GetComponent<Text>();
            text.text = value;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            CreateText(item.transform, "Label", label);
            return item.GetComponent<Button>();
        }

        private static Dropdown CreateDropdown(Transform parent, string name)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Dropdown));
            item.transform.SetParent(parent, false);
            return item.GetComponent<Dropdown>();
        }
    }
}
