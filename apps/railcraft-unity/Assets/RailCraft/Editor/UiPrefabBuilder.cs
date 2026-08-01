using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RailCraft.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Editor
{
    public static class UiPrefabBuilder
    {
        private const string PrefabRoot = "Assets/RailCraft/Art/Prefabs/UI";
        private static readonly Color PanelColor = new Color(0.055f, 0.075f, 0.1f, 0.96f);
        private static readonly Color TextColor = new Color(0.93f, 0.96f, 0.98f, 1f);
        private static readonly Color AccentColor = new Color(0.08f, 0.55f, 0.75f, 1f);
        private static readonly Color FeedbackColor = new Color(1f, 0.73f, 0.22f, 1f);

        [MenuItem("RailCraft/Build UI Prefabs")]
        public static void BuildFromMenu() => Build();

        public static void BuildFromCommandLine() => Build();

        public static void Build()
        {
            EnsureFolder();
            BuildQuizPanel();
            BuildStepHud();
            BuildFeedbackToast();
            BuildMainMenu();
            BuildGuidancePanel();
            BuildSettingsPanel();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsureFolder()
        {
            const string parent = "Assets/RailCraft/Art/Prefabs";
            if (!AssetDatabase.IsValidFolder(PrefabRoot))
                AssetDatabase.CreateFolder(parent, "UI");
        }

        private static void BuildQuizPanel()
        {
            var root = CreatePanel("QuizPanel", new Vector2(820f, 650f));
            var stage = CreateText(root.transform, "StageNameText", 28, FontStyle.Bold,
                new Vector2(50f, -42f), new Vector2(720f, 48f), TextAnchor.MiddleLeft, AccentColor);
            var counter = CreateText(root.transform, "QuestionCounterText", 20, FontStyle.Normal,
                new Vector2(50f, -92f), new Vector2(720f, 36f), TextAnchor.MiddleLeft, TextColor);
            var prompt = CreateText(root.transform, "PromptText", 25, FontStyle.Normal,
                new Vector2(50f, -142f), new Vector2(720f, 105f), TextAnchor.UpperLeft, TextColor);
            prompt.horizontalOverflow = HorizontalWrapMode.Wrap;
            prompt.verticalOverflow = VerticalWrapMode.Overflow;

            var container = new GameObject("OptionButtonContainer", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            container.transform.SetParent(root.transform, false);
            SetTopRect((RectTransform)container.transform, new Vector2(50f, -260f), new Vector2(720f, 260f));
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = container.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var template = CreateButton(container.transform, "OptionButtonTemplate", "选项", 22);
            template.gameObject.SetActive(false);
            var feedback = CreateText(root.transform, "FeedbackText", 21, FontStyle.Bold,
                new Vector2(50f, -575f), new Vector2(720f, 45f), TextAnchor.MiddleLeft, FeedbackColor);

            var view = root.AddComponent<QuizView>();
            view.Configure(root, stage, counter, prompt, container.transform, template, feedback);
            var presenter = root.AddComponent<QuizPresenter>();
            presenter.ConfigureView(view, 0.2f);
            SavePrefab(root, PrefabRoot + "/QuizPanel.prefab");
        }

        private static void BuildStepHud()
        {
            var root = CreatePanel("StepHud", new Vector2(560f, 190f));
            var stage = CreateText(root.transform, "StageNameText", 22, FontStyle.Bold,
                new Vector2(24f, -18f), new Vector2(512f, 34f), TextAnchor.MiddleLeft, AccentColor);
            var progress = CreateText(root.transform, "ProgressText", 18, FontStyle.Normal,
                new Vector2(24f, -58f), new Vector2(512f, 30f), TextAnchor.MiddleLeft, TextColor);
            var knowledge = CreateText(root.transform, "KnowledgeText", 18, FontStyle.Normal,
                new Vector2(24f, -94f), new Vector2(512f, 30f), TextAnchor.MiddleLeft, TextColor);
            var hint = CreateText(root.transform, "HintText", 17, FontStyle.Normal,
                new Vector2(24f, -130f), new Vector2(512f, 42f), TextAnchor.MiddleLeft, FeedbackColor);
            var view = root.AddComponent<StepHudView>();
            view.Configure(stage, progress, knowledge, hint);
            SavePrefab(root, PrefabRoot + "/StepHud.prefab");
        }

        private static void BuildFeedbackToast()
        {
            var root = CreatePanel("FeedbackToast", new Vector2(760f, 90f));
            var message = CreateText(root.transform, "MessageText", 22, FontStyle.Bold,
                new Vector2(24f, -15f), new Vector2(712f, 60f), TextAnchor.MiddleCenter, FeedbackColor);
            var view = root.AddComponent<FeedbackView>();
            view.Configure(root, message);
            SavePrefab(root, PrefabRoot + "/FeedbackToast.prefab");
        }

        private static void BuildMainMenu()
        {
            var root = CreatePanel("MainMenu", new Vector2(680f, 700f));
            var title = CreateText(root.transform, "TitleText", 38, FontStyle.Bold,
                new Vector2(48f, -44f), new Vector2(584f, 64f), TextAnchor.MiddleCenter, AccentColor);
            title.text = "轨道交通虚拟装配教学系统";
            var subtitle = CreateText(root.transform, "SubtitleText", 20, FontStyle.Normal,
                new Vector2(48f, -112f), new Vector2(584f, 54f), TextAnchor.MiddleCenter, TextColor);
            subtitle.text = "SWM-400E1 动力转向架 · v0.1 测试版";

            var container = CreateVerticalContainer(root.transform, "MenuButtonContainer",
                new Vector2(110f, -210f), new Vector2(460f, 360f), 18f);
            var start = CreateButton(container.transform, "StartButton", "开始体验", 23);
            var guidance = CreateButton(container.transform, "GuidanceButton", "操作说明", 23);
            var settings = CreateButton(container.transform, "SettingsButton", "设置", 23);
            var exit = CreateButton(container.transform, "ExitButton", "退出", 23);
            var presenter = root.AddComponent<MainMenuPresenter>();
            presenter.ConfigureView(root, start, guidance, settings, exit);
            SavePrefab(root, PrefabRoot + "/MainMenu.prefab", true);
        }

        private static void BuildGuidancePanel()
        {
            var root = CreatePanel("GuidancePanel", new Vector2(1120f, 720f));
            var title = CreateText(root.transform, "TitleText", 34, FontStyle.Bold,
                new Vector2(56f, -38f), new Vector2(1008f, 58f), TextAnchor.MiddleLeft, AccentColor);
            title.text = "操作说明";
            var copy = CreateText(root.transform, "GuidanceCopyText", 23, FontStyle.Normal,
                new Vector2(56f, -126f), new Vector2(1008f, 430f), TextAnchor.UpperLeft, TextColor);
            copy.verticalOverflow = VerticalWrapMode.Overflow;
            copy.lineSpacing = 1.35f;
            var primary = CreateButton(root.transform, "GuidancePrimaryButton", "开始装配", 22);
            SetTopRect((RectTransform)primary.transform, new Vector2(760f, -620f),
                new Vector2(304f, 60f));
            var presenter = root.AddComponent<GuidancePresenter>();
            presenter.ConfigureView(root, copy, primary);
            SavePrefab(root, PrefabRoot + "/GuidancePanel.prefab", true);
        }

        private static void BuildSettingsPanel()
        {
            var root = CreatePanel("SettingsPanel", new Vector2(760f, 620f));
            var title = CreateText(root.transform, "TitleText", 34, FontStyle.Bold,
                new Vector2(52f, -36f), new Vector2(656f, 58f), TextAnchor.MiddleLeft, AccentColor);
            title.text = "设置";
            var qualityLabel = CreateText(root.transform, "QualityLabel", 22, FontStyle.Bold,
                new Vector2(70f, -150f), new Vector2(210f, 54f), TextAnchor.MiddleLeft, TextColor);
            qualityLabel.text = "画质";
            var quality = CreateDropdown(root.transform, "QualityDropdown",
                new Vector2(300f, -150f), new Vector2(380f, 58f));
            var windowLabel = CreateText(root.transform, "WindowModeLabel", 22, FontStyle.Bold,
                new Vector2(70f, -250f), new Vector2(210f, 54f), TextAnchor.MiddleLeft, TextColor);
            windowLabel.text = "窗口模式";
            var windowMode = CreateDropdown(root.transform, "WindowModeDropdown",
                new Vector2(300f, -250f), new Vector2(380f, 58f));
            var back = CreateButton(root.transform, "SettingsBackButton", "返回主菜单", 22);
            SetTopRect((RectTransform)back.transform, new Vector2(390f, -505f),
                new Vector2(290f, 60f));
            var presenter = root.AddComponent<SettingsPresenter>();
            presenter.ConfigureView(root, quality, windowMode, back);
            SavePrefab(root, PrefabRoot + "/SettingsPanel.prefab", true);
        }

        private static GameObject CreatePanel(string name, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            var image = root.GetComponent<Image>();
            image.color = PanelColor;
            image.raycastTarget = true;
            return root;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style,
            Vector2 topLeft, Vector2 size, TextAnchor alignment, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            SetTopRect((RectTransform)item.transform, topLeft, size);
            var text = item.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string labelText, int fontSize)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button), typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            var image = item.GetComponent<Image>();
            image.color = new Color(0.1f, 0.22f, 0.3f, 1f);
            var button = item.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.8f, 0.9f, 1f);
            colors.disabledColor = new Color(0.35f, 0.4f, 0.43f, 0.75f);
            button.colors = colors;
            var layout = item.GetComponent<LayoutElement>();
            layout.preferredHeight = 52f;
            layout.minHeight = 48f;

            var label = CreateText(item.transform, "Label", fontSize, FontStyle.Normal,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, TextColor);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 6f);
            labelRect.offsetMax = new Vector2(-20f, -6f);
            label.text = labelText;
            return button;
        }

        private static GameObject CreateVerticalContainer(Transform parent, string name,
            Vector2 topLeft, Vector2 size, float spacing)
        {
            var container = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            container.transform.SetParent(parent, false);
            SetTopRect((RectTransform)container.transform, topLeft, size);
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            return container;
        }

        private static Dropdown CreateDropdown(Transform parent, string name,
            Vector2 topLeft, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Dropdown));
            root.transform.SetParent(parent, false);
            SetTopRect((RectTransform)root.transform, topLeft, size);
            var image = root.GetComponent<Image>();
            image.color = new Color(0.1f, 0.22f, 0.3f, 1f);

            var caption = CreateText(root.transform, "CaptionText", 21, FontStyle.Normal,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, TextColor);
            Stretch((RectTransform)caption.transform, new Vector2(18f, 6f), new Vector2(-54f, -6f));
            var arrow = CreateText(root.transform, "Arrow", 22, FontStyle.Bold,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, AccentColor);
            arrow.text = "▼";
            var arrowRect = (RectTransform)arrow.transform;
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = Vector2.one;
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.offsetMin = new Vector2(-48f, 4f);
            arrowRect.offsetMax = new Vector2(-8f, -4f);

            var template = new GameObject("Template", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(root.transform, false);
            var templateRect = (RectTransform)template.transform;
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 190f);
            template.GetComponent<Image>().color = PanelColor;

            var viewport = new GameObject("Viewport", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            Stretch((RectTransform)viewport.transform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            var contentFitter = content.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle),
                typeof(LayoutElement));
            item.transform.SetParent(content.transform, false);
            item.GetComponent<LayoutElement>().preferredHeight = 50f;
            var background = new GameObject("Item Background", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(item.transform, false);
            Stretch((RectTransform)background.transform, Vector2.zero, Vector2.zero);
            background.GetComponent<Image>().color = new Color(0.08f, 0.16f, 0.22f, 1f);
            var checkmark = new GameObject("Item Checkmark", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            checkmark.transform.SetParent(item.transform, false);
            var checkRect = (RectTransform)checkmark.transform;
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.sizeDelta = new Vector2(18f, 18f);
            checkRect.anchoredPosition = new Vector2(18f, 0f);
            checkmark.GetComponent<Image>().color = AccentColor;
            var itemLabel = CreateText(item.transform, "Item Label", 20, FontStyle.Normal,
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, TextColor);
            Stretch((RectTransform)itemLabel.transform, new Vector2(42f, 4f), new Vector2(-8f, -4f));

            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            var scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var dropdown = root.GetComponent<Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            template.SetActive(false);
            return dropdown;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetTopRect(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }

        private static void SavePrefab(GameObject root, string path, bool trimYaml = false)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save UI prefab: {path}");
            if (trimYaml)
            {
                TrimGeneratedYaml(path);
                TrimGeneratedYaml(path + ".meta");
            }
        }

        private static void TrimGeneratedYaml(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var absolutePath = Path.Combine(projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolutePath))
                return;
            var contents = File.ReadAllText(absolutePath);
            var normalized = Regex.Replace(contents, @"[ \t]+(?=\r?$)", string.Empty,
                RegexOptions.Multiline);
            if (!string.Equals(contents, normalized, StringComparison.Ordinal))
                File.WriteAllText(absolutePath, normalized, new UTF8Encoding(false));
        }
    }
}
