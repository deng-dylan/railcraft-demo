using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RailCraft.Assets;
using RailCraft.Flow;
using RailCraft.Interaction;
using RailCraft.Presentation;
using RailCraft.Process;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RailCraft.Editor
{
    public static class BootstrapSceneBuilder
    {
        private const string BootstrapPath = "Assets/RailCraft/Scenes/Bootstrap.unity";
        private const string FactoryPath = "Assets/RailCraft/Scenes/Factory.unity";
        private const string UiRoot = "Assets/RailCraft/Art/Prefabs/UI";
        private const string QuestionsPath = "Assets/RailCraft/Content/V1/questions.v1.json";
        private const string FlowPath = "Assets/RailCraft/Content/V1/flow.v1.json";
        private const string CatalogPath = "Assets/RailCraft/Art/PartPrefabCatalog.asset";

        [MenuItem("RailCraft/Build Bootstrap Scene")]
        public static void BuildFromMenu() => Build(false);

        public static void BuildFromCommandLine() => Build(false);

        [MenuItem("RailCraft/Rebuild Bootstrap Scene")]
        public static void RebuildFromMenu() => Build(true);

        public static void RebuildFromCommandLine() => Build(true);

        public static void Build(bool forceRebuild = false)
        {
            if (!forceRebuild && AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath) != null)
            {
                UpsertBuildScenes();
                AssetDatabase.SaveAssets();
                return;
            }

            var questions = RequireAsset<TextAsset>(QuestionsPath);
            var flow = RequireAsset<TextAsset>(FlowPath);
            var catalog = RequireAsset<PartPrefabCatalog>(CatalogPath);
            var quizPrefab = RequireAsset<GameObject>(UiRoot + "/QuizPanel.prefab");
            var hudPrefab = RequireAsset<GameObject>(UiRoot + "/StepHud.prefab");
            var feedbackPrefab = RequireAsset<GameObject>(UiRoot + "/FeedbackToast.prefab");
            var mainMenuPrefab = RequireAsset<GameObject>(UiRoot + "/MainMenu.prefab");
            var guidancePrefab = RequireAsset<GameObject>(UiRoot + "/GuidancePanel.prefab");
            var settingsPrefab = RequireAsset<GameObject>(UiRoot + "/SettingsPanel.prefab");
            RequireAsset<SceneAsset>(FactoryPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Bootstrap";
            var composition = new GameObject("RailCraftCompositionRoot");
            var canvas = BuildCanvas();
            BuildEventSystem();

            var quizObject = InstantiateUiPrefab(quizPrefab, canvas.transform, "QuizPanel");
            Anchor((RectTransform)quizObject.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            var hudObject = InstantiateUiPrefab(hudPrefab, canvas.transform, "StepHud");
            Anchor((RectTransform)hudObject.transform, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(32f, -32f));
            var feedbackObject = InstantiateUiPrefab(feedbackPrefab, canvas.transform, "FeedbackToast");
            Anchor((RectTransform)feedbackObject.transform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 36f));
            var mainMenuObject = InstantiateUiPrefab(mainMenuPrefab, canvas.transform, "MainMenu");
            Anchor((RectTransform)mainMenuObject.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            var guidanceObject = InstantiateUiPrefab(guidancePrefab, canvas.transform, "GuidancePanel");
            Anchor((RectTransform)guidanceObject.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            var settingsObject = InstantiateUiPrefab(settingsPrefab, canvas.transform, "SettingsPanel");
            Anchor((RectTransform)settingsObject.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);

            var processPanel = BuildProcessPanel(canvas.transform, out var processMessage,
                out var inspectionMarker, out var passIndicator, out var processButton);
            var completionPanel = BuildCompletionPanel(canvas.transform, out var completionMessage,
                out var restartButton, out var exitButton);
            var resetPanel = BuildResetSurface(canvas.transform, out var resetMessage,
                out var resetRequest, out var resetConfirm, out var resetCancel);

            var quiz = quizObject.GetComponent<QuizPresenter>();
            var hud = hudObject.GetComponent<StepHudView>();
            var feedback = feedbackObject.GetComponent<FeedbackView>();
            var mainMenu = mainMenuObject.GetComponent<MainMenuPresenter>();
            var guidance = guidanceObject.GetComponent<GuidancePresenter>();
            var settings = settingsObject.GetComponent<SettingsPresenter>();
            var assembly = composition.AddComponent<AssemblyPresenter>();
            var process = composition.AddComponent<ProcessStagePresenter>();
            process.Configure(processPanel, processMessage, inspectionMarker,
                passIndicator, processButton);
            var completion = composition.AddComponent<CompletionPresenter>();
            completion.Configure(completionPanel, completionMessage, restartButton, exitButton);
            var reset = composition.AddComponent<ResetPresenter>();
            reset.ConfigureView(resetPanel, resetMessage, resetRequest, resetConfirm, resetCancel);
            var drag = composition.AddComponent<DragDropController>();
            var highlight = composition.AddComponent<HighlightController>();
            var snapEffects = composition.AddComponent<SnapEffectController>();
            snapEffects.Configure(highlight, drag);
            assembly.ConfigureEffects(snapEffects);
            var controller = composition.AddComponent<GuidedFlowController>();
            controller.ConfigureStartup(questions, flow, catalog, quiz, assembly, process,
                completion, hud, feedback, drag);
            controller.ConfigureNavigation(mainMenu, guidance, settings, reset);
            guidance.Bind(controller, mainMenu);
            settings.Bind(mainMenu);
            mainMenu.Bind(controller, guidance, settings);
            reset.Bind(controller, guidance, mainMenu, settings);

            quizObject.SetActive(false);
            hudObject.SetActive(false);
            feedbackObject.SetActive(false);
            processPanel.SetActive(false);
            completionPanel.SetActive(false);
            mainMenu.Show();
            guidance.Hide();
            settings.Hide();
            reset.HideConfirmation();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, BootstrapPath))
                throw new InvalidOperationException("Failed to save Bootstrap scene.");
            TrimGeneratedYaml(BootstrapPath);
            TrimGeneratedYaml(BootstrapPath + ".meta");
            UpsertBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static Canvas BuildCanvas()
        {
            var root = new GameObject("RailCraftCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void BuildEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static GameObject BuildProcessPanel(Transform parent, out Text message,
            out GameObject inspectionMarker, out GameObject passIndicator, out Button action)
        {
            var panel = CreatePanel(parent, "ProcessPanel", new Vector2(780f, 220f),
                new Color(0.055f, 0.075f, 0.1f, 0.96f));
            Anchor((RectTransform)panel.transform, new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 150f));
            message = CreateText(panel.transform, "ProcessMessageText", string.Empty, 21,
                new Vector2(32f, -24f), new Vector2(716f, 105f), TextAnchor.UpperLeft);
            inspectionMarker = CreateBadge(panel.transform, "InspectionMarker", "检验标记",
                new Vector2(32f, -145f), new Color(0.05f, 0.7f, 0.82f, 1f));
            passIndicator = CreateBadge(panel.transform, "CommissioningPassIndicator", "调试通过",
                new Vector2(190f, -145f), new Color(0.05f, 0.65f, 0.25f, 1f));
            action = CreateButton(panel.transform, "ProcessActionButton", "进入整改",
                new Vector2(570f, -142f), new Vector2(178f, 54f));
            return panel;
        }

        private static GameObject BuildCompletionPanel(Transform parent, out Text message,
            out Button restart, out Button exit)
        {
            var panel = CreatePanel(parent, "CompletionPanel", new Vector2(720f, 300f),
                new Color(0.035f, 0.065f, 0.09f, 0.98f));
            Anchor((RectTransform)panel.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            message = CreateText(panel.transform, "CompletionMessageText", string.Empty, 28,
                new Vector2(42f, -45f), new Vector2(636f, 130f), TextAnchor.MiddleCenter);
            restart = CreateButton(panel.transform, "RestartButton", "重新开始",
                new Vector2(118f, -215f), new Vector2(220f, 58f));
            exit = CreateButton(panel.transform, "ExitButton", "退出",
                new Vector2(382f, -215f), new Vector2(220f, 58f));
            return panel;
        }

        private static GameObject BuildResetSurface(Transform parent, out Text message,
            out Button request, out Button confirm, out Button cancel)
        {
            request = CreateButton(parent, "ResetRequestButton", "重置流程",
                new Vector2(-210f, -30f), new Vector2(178f, 54f));
            var requestRect = (RectTransform)request.transform;
            requestRect.anchorMin = Vector2.one;
            requestRect.anchorMax = Vector2.one;
            requestRect.pivot = new Vector2(0f, 1f);

            var panel = CreatePanel(parent, "ResetConfirmationPanel", new Vector2(780f, 330f),
                new Color(0.035f, 0.065f, 0.09f, 0.99f));
            Anchor((RectTransform)panel.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero);
            message = CreateText(panel.transform, "ResetConfirmationText",
                ResetPresenter.RequiredConfirmationCopy, 25,
                new Vector2(48f, -46f), new Vector2(684f, 130f), TextAnchor.MiddleCenter);
            confirm = CreateButton(panel.transform, "ResetConfirmButton", "确认重置",
                new Vector2(126f, -225f), new Vector2(230f, 60f));
            cancel = CreateButton(panel.transform, "ResetCancelButton", "取消",
                new Vector2(424f, -225f), new Vector2(230f, 60f));
            return panel;
        }

        private static GameObject InstantiateUiPrefab(GameObject prefab, Transform parent, string name)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
                throw new InvalidOperationException($"Failed to instantiate {prefab.name}.");
            instance.name = name;
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            ((RectTransform)panel.transform).sizeDelta = size;
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize,
            Vector2 topLeft, Vector2 size, TextAnchor alignment)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            SetTopRect((RectTransform)item.transform, topLeft, size);
            var text = item.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = new Color(0.93f, 0.96f, 0.98f, 1f);
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = value;
            return text;
        }

        private static GameObject CreateBadge(Transform parent, string name, string label,
            Vector2 topLeft, Color color)
        {
            var badge = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badge.transform.SetParent(parent, false);
            SetTopRect((RectTransform)badge.transform, topLeft, new Vector2(140f, 42f));
            badge.GetComponent<Image>().color = color;
            var text = CreateText(badge.transform, "Label", label, 18, Vector2.zero,
                Vector2.zero, TextAnchor.MiddleCenter);
            Stretch((RectTransform)text.transform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
            return badge;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 topLeft, Vector2 size)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            SetTopRect((RectTransform)item.transform, topLeft, size);
            var image = item.GetComponent<Image>();
            image.color = new Color(0.08f, 0.42f, 0.62f, 1f);
            var button = item.GetComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(item.transform, "Label", label, 21, Vector2.zero,
                Vector2.zero, TextAnchor.MiddleCenter);
            Stretch((RectTransform)text.transform, new Vector2(10f, 6f), new Vector2(-10f, -6f));
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
        }

        private static void SetTopRect(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Required Bootstrap asset is missing: {path}");
            return asset;
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

        private static void UpsertBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(BootstrapPath, true),
                new EditorBuildSettingsScene(FactoryPath, true)
            };
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == BootstrapPath || scene.path == FactoryPath)
                    continue;
                scenes.Add(scene);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
