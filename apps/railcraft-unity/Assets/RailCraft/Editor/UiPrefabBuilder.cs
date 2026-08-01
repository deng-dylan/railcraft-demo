using System;
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

        private static void SetTopRect(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
                throw new InvalidOperationException($"Failed to save UI prefab: {path}");
        }
    }
}
