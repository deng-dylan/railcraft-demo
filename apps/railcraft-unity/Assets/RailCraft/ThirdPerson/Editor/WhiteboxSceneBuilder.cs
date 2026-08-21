using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.Editor
{
    public static class WhiteboxSceneBuilder
    {
        private const string RootPath = "Assets/RailCraft/ThirdPerson";
        private const string ScenePath = RootPath + "/Scenes/ThirdPersonWhitebox.unity";
        private const string MaterialPath = RootPath + "/Art/Materials";

        private sealed class Palette
        {
            public Material Floor;
            public Material Wall;
            public Material Steel;
            public Material Safety;
            public Material Running;
            public Material Carbody;
            public Material Electrical;
            public Material Station;
            public Material White;
            public Material Locked;
            public Material Success;
            public Material Warning;
        }

        private sealed class PlayerRig
        {
            public GameObject Player;
            public ThirdPersonInputLock InputLock;
            public PlayerInteractionScanner Scanner;
            public ThirdPersonOrbitCamera OrbitCamera;
        }

        private sealed class UiRig
        {
            public WhiteboxQuizPanel QuizPanel;
            public WhiteboxHudPresenter Hud;
            public WhiteboxMainMenuController MainMenu;
        }

        private sealed class MainMenuUi
        {
            public GameObject Root;
            public GameObject SettingsRoot;
            public Text Title;
            public Text Subtitle;
            public Text Footnote;
            public Button Start;
            public Button Continue;
            public Button Settings;
            public Button Quit;
            public Button SettingsBack;
            public Slider Volume;
            public Text VolumeValue;
            public Dropdown Quality;
        }

        private sealed class KnowledgeUi
        {
            public Button OpenButton;
            public GameObject PopupRoot;
            public Text PopupTitle;
            public Text PopupBody;
            public Button PopupClose;
            public GameObject CompendiumRoot;
            public Text CompendiumBody;
            public Button CompendiumClose;
        }

        private sealed class CompletionUi
        {
            public GameObject Root;
            public Text Title;
            public Text Detail;
            public Button Replay;
            public Button OpenCompendium;
            public Button OpenShowcase;
        }

        private readonly struct PartStationSpec
        {
            public PartStationSpec(PartId partId, Vector3 position, float facingYaw)
            {
                PartId = partId;
                Position = position;
                FacingYaw = facingYaw;
            }

            public PartId PartId { get; }
            public Vector3 Position { get; }
            public float FacingYaw { get; }
        }

        [MenuItem("RailCraft/Third Person Whitebox/Rebuild Scene")]
        public static void RebuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void Build()
        {
            EnsureFolders();
            var palette = BuildPalette();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ThirdPersonWhitebox";

            var root = new GameObject("ThirdPersonWhiteboxRoot");
            BuildEnvironment(CreateChild(root.transform, "Environment"), palette);
            BuildLighting(CreateChild(root.transform, "Lighting"), palette);

            var hostObject = CreateChild(root.transform, "WhiteboxGameSession");
            var sessionHost = hostObject.AddComponent<WhiteboxGameSessionHost>();
            sessionHost.Configure(
                new DomainWorldGameSession(),
                "执行标准工单 RC-EMU-01：前往知识工位确认零件与装配要求");
            var saveController = hostObject.AddComponent<WhiteboxSaveController>();
            saveController.Configure(sessionHost);

            var playerRig = BuildPlayer(CreateChild(root.transform, "PlayerRig"), palette);
            var feedbackRouter = hostObject.AddComponent<WhiteboxInteractionFeedbackRouter>();
            feedbackRouter.Configure(sessionHost, playerRig.Scanner);
            var uiRig = BuildUi(
                CreateChild(root.transform, "Interface"),
                sessionHost,
                saveController,
                playerRig);
            var gameplay = CreateChild(root.transform, "GameplayStations");
            var focusBindings = new List<AssemblyFocusBinding>();
            BuildPartStations(
                gameplay.transform,
                sessionHost,
                playerRig.InputLock,
                playerRig.Scanner,
                uiRig.QuizPanel,
                palette);
            BuildModuleStations(
                gameplay.transform,
                sessionHost,
                playerRig.Scanner,
                focusBindings,
                palette);
            BuildCompositeAssembly(
                gameplay.transform,
                sessionHost,
                playerRig.Scanner,
                focusBindings,
                palette);
            BuildFinalAssembly(
                gameplay.transform,
                sessionHost,
                playerRig.Scanner,
                focusBindings,
                palette);
            BuildCommissioningStations(
                gameplay.transform,
                sessionHost,
                playerRig.Scanner,
                palette);

            var focusDirector = playerRig.OrbitCamera.gameObject.AddComponent<AssemblyCameraFocusDirector>();
            focusDirector.Configure(
                sessionHost,
                playerRig.OrbitCamera.GetComponent<UnityEngine.Camera>(),
                playerRig.OrbitCamera,
                focusBindings);
            root.AddComponent<WhiteboxAutomatedSmokeRunner>();

            ConfigureRenderSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"Failed to save whitebox scene: {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"RAILCRAFT_WHITEBOX_SCENE_BUILT path={ScenePath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/RailCraft", "ThirdPerson");
            EnsureFolder(RootPath, "Scenes");
            EnsureFolder(RootPath, "Art");
            EnsureFolder(RootPath + "/Art", "Materials");
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Palette BuildPalette()
        {
            return new Palette
            {
                Floor = CreateOrUpdateMaterial("WB_Floor", new Color(0.10f, 0.13f, 0.16f)),
                Wall = CreateOrUpdateMaterial("WB_Wall", new Color(0.34f, 0.39f, 0.43f)),
                Steel = CreateOrUpdateMaterial("WB_Steel", new Color(0.12f, 0.16f, 0.20f), false, 0.45f),
                Safety = CreateOrUpdateMaterial("WB_Safety", new Color(0.98f, 0.62f, 0.06f)),
                Running = CreateOrUpdateMaterial("WB_Running", new Color(0.08f, 0.67f, 0.85f)),
                Carbody = CreateOrUpdateMaterial("WB_Carbody", new Color(0.95f, 0.32f, 0.15f)),
                Electrical = CreateOrUpdateMaterial("WB_Electrical", new Color(0.56f, 0.28f, 0.86f)),
                Station = CreateOrUpdateMaterial("WB_Station", new Color(0.08f, 0.30f, 0.48f)),
                White = CreateOrUpdateMaterial("WB_White", new Color(0.88f, 0.92f, 0.94f)),
                Locked = CreateOrUpdateMaterial("WB_Locked", new Color(0.24f, 0.27f, 0.30f)),
                Success = CreateOrUpdateMaterial("WB_Success", new Color(0.08f, 0.90f, 0.36f), true),
                Warning = CreateOrUpdateMaterial("WB_Warning", new Color(1f, 0.34f, 0.08f), true)
            };
        }

        private static Material CreateOrUpdateMaterial(
            string name,
            Color color,
            bool emission = false,
            float smoothness = 0.25f)
        {
            var path = $"{MaterialPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    throw new InvalidOperationException("A Universal Render Pipeline material shader is required.");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission ? color * 2.4f : Color.black);
                if (emission)
                    material.EnableKeyword("_EMISSION");
                else
                    material.DisableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildEnvironment(GameObject environment, Palette palette)
        {
            var architecture = CreateChild(environment.transform, "Architecture");
            MarkStatic(CreatePrimitive(
                PrimitiveType.Cube,
                architecture.transform,
                "FactoryFloor",
                new Vector3(0f, -0.12f, 0f),
                new Vector3(56f, 0.24f, 42f),
                palette.Floor));

            MarkStatic(CreatePrimitive(
                PrimitiveType.Cube,
                architecture.transform,
                "BackWall",
                new Vector3(0f, 3.2f, 20.9f),
                new Vector3(56f, 6.4f, 0.25f),
                palette.Wall));
            MarkStatic(CreatePrimitive(
                PrimitiveType.Cube,
                architecture.transform,
                "LeftWall",
                new Vector3(-27.9f, 3.2f, 0f),
                new Vector3(0.25f, 6.4f, 42f),
                palette.Wall));
            MarkStatic(CreatePrimitive(
                PrimitiveType.Cube,
                architecture.transform,
                "RightWall",
                new Vector3(27.9f, 3.2f, 0f),
                new Vector3(0.25f, 6.4f, 42f),
                palette.Wall));

            var beams = CreateChild(environment.transform, "RoofBeams");
            for (var z = -19; z <= 19; z += 4)
            {
                MarkStatic(CreatePrimitive(
                    PrimitiveType.Cube,
                    beams.transform,
                    $"RoofBeam_{z}",
                    new Vector3(0f, 6.1f, z),
                    new Vector3(55.5f, 0.22f, 0.28f),
                    palette.Steel));
            }

            var columns = CreateChild(environment.transform, "Columns");
            for (var x = -24; x <= 24; x += 8)
            {
                MarkStatic(CreatePrimitive(
                    PrimitiveType.Cube,
                    columns.transform,
                    $"BackColumn_{x}",
                    new Vector3(x, 3f, 20.35f),
                    new Vector3(0.38f, 6f, 0.38f),
                    palette.Steel));
            }

            BuildZonePad(environment.transform, "KnowledgeAndMaterialZone", new Vector3(-1.75f, 0.015f, -16f),
                new Vector3(43f, 0.03f, 5.2f), palette.Running);
            BuildZonePad(environment.transform, "SubassemblyZone", new Vector3(-6f, 0.02f, -10f),
                new Vector3(32f, 0.04f, 5.2f), palette.Electrical);
            BuildZonePad(environment.transform, "BogieAssemblyZone", new Vector3(0f, 0.022f, 1.5f),
                new Vector3(9f, 0.044f, 7f), palette.Station);
            BuildZonePad(environment.transform, "LandingZone", new Vector3(15f, 0.024f, 0f),
                new Vector3(10f, 0.048f, 27f), palette.Station);
            BuildZonePad(environment.transform, "CommissioningLoop", new Vector3(0f, 0.025f, 16.5f),
                new Vector3(30f, 0.05f, 6.5f), palette.Safety);

            BuildSafetyRailings(environment.transform, palette);
            CreateWorldLabel(environment.transform, "MaterialZoneLabel", "01  知识确认与材料准备",
                new Vector3(-18f, 4.2f, 20.65f), Quaternion.identity, palette.Running.color, 0.78f);
            CreateWorldLabel(environment.transform, "AssemblyZoneLabel", "02  子总成与转向架装配",
                new Vector3(0f, 4.2f, 20.65f), Quaternion.identity, palette.Electrical.color, 0.78f);
            CreateWorldLabel(environment.transform, "LandingZoneLabel", "03  落车 · 教学调试 · 检验",
                new Vector3(18f, 4.2f, 20.65f), Quaternion.identity, palette.Safety.color, 0.78f);
            BuildStandardWorkOrderBoard(environment.transform, palette);

            FactoryKitEnvironmentVisualFactory.BuildDefaultDecorations(
                environment.transform,
                palette.Steel,
                palette.Safety);
        }

        private static void BuildZonePad(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var pad = CreatePrimitive(PrimitiveType.Cube, parent, name, position, scale, material);
            RemoveCollider(pad);
            MarkStatic(pad);
        }

        private static void BuildStandardWorkOrderBoard(Transform parent, Palette palette)
        {
            var board = CreateChild(parent, "StandardWorkOrderBoard");
            // Keep the board away from the player's initial chase-camera ray.
            board.transform.localPosition = new Vector3(-13.5f, 0f, -19.55f);
            var panel = CreatePrimitive(
                PrimitiveType.Cube,
                board.transform,
                "WorkOrderPanel",
                new Vector3(0f, 2.05f, 0f),
                new Vector3(10.8f, 3.3f, 0.18f),
                palette.Station);
            RemoveCollider(panel);
            CreateWorldLabel(
                board.transform,
                "WorkOrderTitle",
                "标准工单  RC-EMU-01\n动力中间车转向架装配 → 落车 → 教学故障调试\n精装 1 套代表性转向架 · 同型第 2 套由配套生产线提供",
                new Vector3(0f, 2.05f, 0.12f),
                Quaternion.Euler(0f, 180f, 0f),
                palette.White.color,
                0.38f);
        }

        private static void BuildSafetyRailings(Transform parent, Palette palette)
        {
            var railings = CreateChild(parent, "SafetyRailings");
            foreach (var x in new[] { -25.5f, 25.5f })
            {
                for (var z = -17f; z <= 17f; z += 4f)
                {
                    var post = CreatePrimitive(
                        PrimitiveType.Cylinder,
                        railings.transform,
                        $"Post_{x}_{z}",
                        new Vector3(x, 0.55f, z),
                        new Vector3(0.08f, 0.55f, 0.08f),
                        palette.Safety);
                    MarkStatic(post);
                }
            }
        }

        private static void BuildLighting(GameObject lighting, Palette palette)
        {
            var sunObject = CreateChild(lighting.transform, "MainDirectionalLight");
            sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;

            var fixtures = CreateChild(lighting.transform, "WorkLights");
            foreach (var position in new[]
            {
                new Vector3(-18f, 5.5f, -12f),
                new Vector3(0f, 5.5f, -12f),
                new Vector3(18f, 5.5f, -12f),
                new Vector3(-18f, 5.5f, 8f),
                new Vector3(0f, 5.5f, 8f),
                new Vector3(18f, 5.5f, 8f),
                new Vector3(0f, 5.5f, 18f)
            })
            {
                var fixture = CreatePrimitive(
                    PrimitiveType.Cube,
                    fixtures.transform,
                    $"Fixture_{position.x}_{position.z}",
                    position,
                    new Vector3(2.2f, 0.08f, 0.42f),
                    palette.White);
                RemoveCollider(fixture);
                var light = fixture.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 10f;
                light.intensity = 2.1f;
                light.color = new Color(1f, 0.86f, 0.68f);
                light.shadows = LightShadows.None;
            }
        }

        private static PlayerRig BuildPlayer(GameObject rig, Palette palette)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(rig.transform, false);
            player.transform.localPosition = new Vector3(0f, 0f, -18.2f);
            player.transform.localRotation = Quaternion.identity;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.32f;
            controller.slopeLimit = 48f;

            var inputLock = player.AddComponent<ThirdPersonInputLock>();
            var body = CreatePrimitive(
                PrimitiveType.Capsule,
                player.transform,
                "Body",
                new Vector3(0f, 0.88f, 0f),
                new Vector3(0.42f, 0.78f, 0.34f),
                palette.Station);
            RemoveCollider(body);
            var head = CreatePrimitive(
                PrimitiveType.Sphere,
                player.transform,
                "Head",
                new Vector3(0f, 1.66f, 0f),
                new Vector3(0.38f, 0.38f, 0.38f),
                palette.White);
            RemoveCollider(head);
            var directionMarker = CreatePrimitive(
                PrimitiveType.Cube,
                player.transform,
                "ForwardMarker",
                new Vector3(0f, 1.1f, 0.34f),
                new Vector3(0.18f, 0.16f, 0.08f),
                palette.Safety);
            RemoveCollider(directionMarker);

            var interactionOrigin = CreateChild(player.transform, "InteractionOrigin");
            interactionOrigin.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            var scanner = player.AddComponent<PlayerInteractionScanner>();
            scanner.ConfigurePlayer(player);
            scanner.Configure(interactionOrigin.transform, inputLock);
            scanner.ConfigureScan(2.8f, 180f, ~0);

            var cameraObject = new GameObject("ThirdPersonCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rig.transform, false);
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 120f;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.095f);
            cameraObject.AddComponent<AudioListener>();
            var orbit = cameraObject.AddComponent<ThirdPersonOrbitCamera>();
            orbit.Configure(camera, player.transform, inputLock);
            orbit.SetPivotOffset(new Vector3(0f, 1.45f, 0f));
            // The landing demonstration now uses a full intermediate coach;
            // allow a wide orbit distance so the player can inspect the whole
            // 25 m body after the automatic focus shot.
            orbit.ConfigureLimits(2.2f, 24f, 8f, 68f);
            orbit.ConfigureCollision(~0, 0.22f, 0.1f, 0.45f);
            orbit.SetView(0f, 20f, 5.2f);

            var motor = player.AddComponent<ThirdPersonMotor>();
            motor.Configure(controller, cameraObject.transform, inputLock);
            motor.ConfigureMovement(4.2f, 7f, 720f, -24f);

            return new PlayerRig
            {
                Player = player,
                InputLock = inputLock,
                Scanner = scanner,
                OrbitCamera = orbit
            };
        }

        private static UiRig BuildUi(
            GameObject interfaceRoot,
            WhiteboxGameSessionHost sessionHost,
            WhiteboxSaveController saveController,
            PlayerRig playerRig)
        {
            var canvasObject = new GameObject(
                "WhiteboxCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(interfaceRoot.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var header = CreateText(
                canvasObject.transform,
                "WhiteboxHeader",
                "RAILCRAFT · 高速动车组装配实训白盒 v0.3",
                20,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            SetAnchoredRect(
                header.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -18f),
                new Vector2(650f, 34f));

            var workOrderText = CreateText(
                canvasObject.transform,
                "WorkOrderHudText",
                "标准工单 RC-EMU-01 · 动力中间车转向架装配、落车与调试实训",
                16,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.78f, 0.26f));
            SetAnchoredRect(
                workOrderText.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -47f),
                new Vector2(980f, 26f));

            var progressPanel = CreatePanel(
                canvasObject.transform,
                "AssemblyProgressPanel",
                new Color(0.018f, 0.06f, 0.085f, 0.94f));
            SetAnchoredRect(
                (RectTransform)progressPanel.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -74f),
                new Vector2(720f, 78f));
            var stepText = CreateText(progressPanel.transform, "AssemblyStepText", "第1步/共23步", 22,
                FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.42f, 0.92f, 1f));
            SetAnchoredRect(stepText.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 14f),
                new Vector2(230f, 34f), new Vector2(0f, 0.5f));
            var percentText = CreateText(progressPanel.transform, "AssemblyPercentText", "完成度 0%", 20,
                FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.76f, 0.22f));
            SetAnchoredRect(percentText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-22f, 14f),
                new Vector2(190f, 34f), new Vector2(1f, 0.5f));
            var flowStatusText = CreateText(progressPanel.transform, "AssemblyFlowStatusText", "状态：待装配", 18,
                FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            SetAnchoredRect(flowStatusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f),
                new Vector2(230f, 32f));
            var progressSlider = CreateSlider(progressPanel.transform, "AssemblyProgressSlider", false);
            SetAnchoredRect((RectTransform)progressSlider.transform, new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(672f, 14f), new Vector2(0.5f, 0f));
            progressSlider.interactable = false;

            var taskPanel = CreatePanel(
                canvasObject.transform,
                "TaskPanel",
                new Color(0.025f, 0.055f, 0.075f, 0.88f));
            SetAnchoredRect((RectTransform)taskPanel.transform, new Vector2(0f, 1f), new Vector2(28f, -126f), new Vector2(650f, 116f), new Vector2(0f, 1f));
            var taskText = CreateText(taskPanel.transform, "TaskText", "当前任务：探索工位", 23,
                FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.76f, 0.94f, 1f));
            Stretch(taskText.rectTransform, new Vector2(24f, 18f), new Vector2(-24f, -18f));

            var statePanel = CreatePanel(
                canvasObject.transform,
                "StatePanel",
                new Color(0.025f, 0.055f, 0.075f, 0.88f));
            SetAnchoredRect((RectTransform)statePanel.transform, new Vector2(1f, 1f), new Vector2(-28f, -126f), new Vector2(650f, 178f), new Vector2(1f, 1f));
            var progressText = CreateText(statePanel.transform, "ProgressText", "阶段：知识确认\n总成：0/6 · 调试：未解锁", 22,
                FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.78f, 0.26f));
            SetTopRect(progressText.rectTransform, new Vector2(24f, -18f), new Vector2(602f, 50f));
            var inventoryText = CreateText(statePanel.transform, "InventoryText", "待装配输入：空", 19,
                FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            SetTopRect(inventoryText.rectTransform, new Vector2(24f, -72f), new Vector2(602f, 92f));

            var quickActions = CreatePanel(
                canvasObject.transform,
                "QuickActionsPanel",
                new Color(0.018f, 0.05f, 0.07f, 0.9f));
            SetAnchoredRect((RectTransform)quickActions.transform, new Vector2(1f, 0f),
                new Vector2(-28f, 28f), new Vector2(500f, 64f), new Vector2(1f, 0f));
            var menuButton = CreateButton(quickActions.transform, "MenuButton", "暂停菜单", 18);
            SetAnchoredRect((RectTransform)menuButton.transform, new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(144f, 48f), new Vector2(0f, 0.5f));
            var replayButton = CreateButton(quickActions.transform, "ReplayButton", "一键重玩", 18);
            SetAnchoredRect((RectTransform)replayButton.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(156f, 48f));
            var compendiumButton = CreateButton(quickActions.transform, "CompendiumButton", "工程知识图鉴", 18);
            SetAnchoredRect((RectTransform)compendiumButton.transform, new Vector2(1f, 0.5f),
                new Vector2(-8f, 0f), new Vector2(174f, 48f), new Vector2(1f, 0.5f));
            var replayController = quickActions.AddComponent<WhiteboxResetButton>();
            replayController.Configure(sessionHost, replayButton);

            var controlsPanel = CreatePanel(
                canvasObject.transform,
                "ControlsPanel",
                new Color(0.025f, 0.055f, 0.075f, 0.78f));
            SetAnchoredRect((RectTransform)controlsPanel.transform, new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(760f, 74f), new Vector2(0f, 0f));
            var controls = CreateText(controlsPanel.transform, "ControlsText",
                "WASD 移动  ·  Shift 奔跑  ·  鼠标视角  ·  滚轮缩放  ·  E 交互  ·  ESC 暂停/返回",
                19, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.84f, 0.88f, 0.91f));
            Stretch(controls.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -8f));

            var promptPanel = CreatePanel(
                canvasObject.transform,
                "InteractionPromptPanel",
                new Color(0.02f, 0.07f, 0.10f, 0.92f));
            SetAnchoredRect((RectTransform)promptPanel.transform, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(780f, 72f), new Vector2(0.5f, 0f));
            var promptText = CreateText(promptPanel.transform, "InteractionPromptText", string.Empty, 24,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.36f, 0.92f, 1f));
            Stretch(promptText.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f));

            var feedbackText = CreateText(canvasObject.transform, "FeedbackText", string.Empty, 25,
                FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetAnchoredRect(feedbackText.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(920f, 70f));
            var feedbackBackground = feedbackText.gameObject.AddComponent<Outline>();
            feedbackBackground.effectColor = new Color(0f, 0f, 0f, 0.9f);
            feedbackBackground.effectDistance = new Vector2(2f, -2f);
            feedbackText.gameObject.SetActive(false);

            var crosshair = CreateText(canvasObject.transform, "Crosshair", "+", 25,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f));
            SetAnchoredRect(crosshair.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f));

            var quizPanelComponent = BuildQuizPanel(canvasObject.transform);
            var completionUi = BuildCompletionPanel(canvasObject.transform, sessionHost);
            var knowledgeUi = BuildKnowledgeUi(canvasObject.transform, compendiumButton);
            var mainMenuUi = BuildMainMenu(canvasObject.transform);

            var hud = canvasObject.AddComponent<WhiteboxHudPresenter>();
            hud.Configure(
                sessionHost,
                playerRig.Scanner,
                playerRig.InputLock,
                promptText,
                taskText,
                inventoryText,
                progressText,
                feedbackText,
                completionUi.Root,
                completionUi.Title,
                completionUi.Detail);

            var progressPresenter = canvasObject.AddComponent<WhiteboxAssemblyProgressPresenter>();
            progressPresenter.Configure(
                sessionHost,
                progressSlider,
                stepText,
                percentText,
                flowStatusText);

            var knowledgePresenter = canvasObject.AddComponent<WhiteboxKnowledgePresenter>();
            knowledgePresenter.Configure(
                sessionHost,
                playerRig.InputLock,
                knowledgeUi.OpenButton,
                knowledgeUi.PopupRoot,
                knowledgeUi.PopupTitle,
                knowledgeUi.PopupBody,
                knowledgeUi.PopupClose,
                knowledgeUi.CompendiumRoot,
                knowledgeUi.CompendiumBody,
                knowledgeUi.CompendiumClose,
                completionUi.OpenCompendium);

            var showcaseEntry = canvasObject.AddComponent<FinalShowcaseEntryController>();
            showcaseEntry.Configure(
                sessionHost,
                saveController,
                completionUi.Root,
                completionUi.OpenShowcase);

            var mainMenu = canvasObject.AddComponent<WhiteboxMainMenuController>();
            mainMenu.Configure(
                sessionHost,
                saveController,
                playerRig.InputLock,
                mainMenuUi.Root,
                mainMenuUi.SettingsRoot,
                mainMenuUi.Start,
                mainMenuUi.Continue,
                mainMenuUi.Settings,
                mainMenuUi.Quit,
                mainMenuUi.SettingsBack,
                mainMenuUi.Volume,
                mainMenuUi.VolumeValue,
                mainMenuUi.Quality,
                menuButton,
                mainMenuUi.Title,
                mainMenuUi.Subtitle,
                mainMenuUi.Footnote,
                knowledgePresenter);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(interfaceRoot.transform, false);
            // OnEnable assigns the default action asset. A second assignment
            // throws in Input System 1.17 when rebuilding scenes in the Editor.

            return new UiRig
            {
                QuizPanel = quizPanelComponent,
                Hud = hud,
                MainMenu = mainMenu
            };
        }

        private static WhiteboxQuizPanel BuildQuizPanel(Transform canvas)
        {
            var panel = CreatePanel(canvas, "QuizPanel", new Color(0.025f, 0.055f, 0.075f, 0.98f));
            SetAnchoredRect((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 760f));
            var title = CreateText(panel.transform, "QuizTitle", "工位知识确认", 31,
                FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.34f, 0.9f, 1f));
            SetTopRect(title.rectTransform, new Vector2(48f, -34f), new Vector2(944f, 54f));
            var prompt = CreateText(panel.transform, "QuizPromptText", string.Empty, 24,
                FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            prompt.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopRect(prompt.rectTransform, new Vector2(48f, -104f), new Vector2(944f, 126f));

            var buttons = new Button[4];
            for (var index = 0; index < buttons.Length; index++)
            {
                buttons[index] = CreateButton(panel.transform, $"QuizOption{index + 1}", string.Empty, 21);
                SetTopRect((RectTransform)buttons[index].transform,
                    new Vector2(72f, -244f - index * 84f), new Vector2(896f, 66f));
            }

            var feedback = CreateText(panel.transform, "QuizFeedbackText", string.Empty, 20,
                FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.68f, 0.24f));
            SetTopRect(feedback.rectTransform, new Vector2(72f, -592f), new Vector2(690f, 102f));
            var cancel = CreateButton(panel.transform, "QuizCancelButton", "暂时离开", 20);
            SetTopRect((RectTransform)cancel.transform, new Vector2(800f, -660f), new Vector2(168f, 54f));

            var presenter = panel.AddComponent<WhiteboxQuizPanel>();
            presenter.Configure(panel, prompt, feedback, buttons, cancel);
            return presenter;
        }

        private static CompletionUi BuildCompletionPanel(
            Transform canvas,
            WhiteboxGameSessionHost sessionHost)
        {
            var panel = CreatePanel(canvas, "CompletionPanel", new Color(0.018f, 0.05f, 0.055f, 0.98f));
            SetAnchoredRect((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 620f));
            var completionText = CreateText(panel.transform, "CompletionText", "标准实训完成，车辆通过调试检验", 38,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.2f, 1f, 0.48f));
            SetTopRect(completionText.rectTransform, new Vector2(48f, -42f), new Vector2(744f, 84f));
            var detail = CreateText(panel.transform, "CompletionDetail",
                "装配用时  00:00\n答题正确  0/0  ·  正确率 0%\n得分  0  ·  等级：初级工程师",
                24, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            detail.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopRect(detail.rectTransform, new Vector2(70f, -142f), new Vector2(700f, 150f));
            var unlockHint = CreateText(panel.transform, "CompendiumUnlockHint",
                "工程知识图鉴已解锁；八编组整车展示可在完成后单独查看。",
                20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.42f, 0.9f, 1f));
            SetTopRect(unlockHint.rectTransform, new Vector2(70f, -302f), new Vector2(700f, 44f));
            var showcase = CreateButton(panel.transform, "FinalShowcaseButton", "查看八编组出厂展示  [V]", 22);
            SetTopRect((RectTransform)showcase.transform, new Vector2(120f, -378f), new Vector2(600f, 62f));
            var reset = CreateButton(panel.transform, "ResetWhiteboxButton", "一键重玩", 22);
            SetTopRect((RectTransform)reset.transform, new Vector2(120f, -466f), new Vector2(270f, 62f));
            var compendium = CreateButton(panel.transform, "CompletionCompendiumButton", "打开工程知识图鉴", 22);
            SetTopRect((RectTransform)compendium.transform, new Vector2(450f, -466f), new Vector2(270f, 62f));
            var resetController = panel.AddComponent<WhiteboxResetButton>();
            resetController.Configure(sessionHost, reset);
            panel.SetActive(false);
            return new CompletionUi
            {
                Root = panel,
                Title = completionText,
                Detail = detail,
                Replay = reset,
                OpenCompendium = compendium,
                OpenShowcase = showcase
            };
        }

        private static KnowledgeUi BuildKnowledgeUi(Transform canvas, Button openButton)
        {
            var popupMask = CreatePanel(canvas, "EngineeringKnowledgePopupMask",
                new Color(0.005f, 0.015f, 0.022f, 0.78f));
            Stretch((RectTransform)popupMask.transform, Vector2.zero, Vector2.zero);
            popupMask.GetComponent<Image>().raycastTarget = true;
            var popup = CreatePanel(popupMask.transform, "EngineeringKnowledgePopup",
                new Color(0.02f, 0.055f, 0.075f, 0.99f));
            SetAnchoredRect((RectTransform)popup.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(920f, 520f));
            var popupEyebrow = CreateText(popup.transform, "PopupEyebrow", "装配步骤完成 · 工程知识", 18,
                FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.72f, 0.2f));
            SetTopRect(popupEyebrow.rectTransform, new Vector2(48f, -28f), new Vector2(824f, 34f));
            var popupTitle = CreateText(popup.transform, "PopupTitle", "知识标题", 31,
                FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.34f, 0.92f, 1f));
            SetTopRect(popupTitle.rectTransform, new Vector2(48f, -72f), new Vector2(824f, 54f));
            var popupBody = CreateText(popup.transform, "PopupBody", string.Empty, 21,
                FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            popupBody.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopRect(popupBody.rectTransform, new Vector2(48f, -140f), new Vector2(824f, 260f));
            var popupClose = CreateButton(popup.transform, "PopupCloseButton", "我知道了，继续装配", 21);
            SetTopRect((RectTransform)popupClose.transform, new Vector2(300f, -438f), new Vector2(320f, 58f));
            popupMask.SetActive(false);

            var compendiumMask = CreatePanel(canvas, "EngineeringKnowledgeCompendiumMask",
                new Color(0.003f, 0.012f, 0.018f, 0.82f));
            Stretch((RectTransform)compendiumMask.transform, Vector2.zero, Vector2.zero);
            compendiumMask.GetComponent<Image>().raycastTarget = true;
            var compendium = CreatePanel(compendiumMask.transform, "EngineeringKnowledgeCompendium",
                new Color(0.012f, 0.035f, 0.05f, 0.995f));
            SetAnchoredRect((RectTransform)compendium.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1320f, 900f));
            var compendiumTitle = CreateText(compendium.transform, "CompendiumTitle",
                "工程知识图鉴", 36, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Color(0.32f, 0.92f, 1f));
            SetTopRect(compendiumTitle.rectTransform, new Vector2(56f, -36f), new Vector2(900f, 58f));
            var compendiumSubtitle = CreateText(compendium.transform, "CompendiumSubtitle",
                "本次训练已解锁的题目解析、零件、装配节点与调试知识",
                19, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.88f));
            SetTopRect(compendiumSubtitle.rectTransform, new Vector2(56f, -92f), new Vector2(1050f, 38f));
            var scrollText = CreateScrollableText(
                compendium.transform,
                "CompendiumScroll",
                20,
                new Color(0.95f, 0.97f, 1f));
            SetTopRect((RectTransform)scrollText.transform.parent.parent,
                new Vector2(56f, -148f), new Vector2(1208f, 640f));
            var compendiumClose = CreateButton(compendium.transform, "CompendiumCloseButton", "返回装配", 21);
            SetTopRect((RectTransform)compendiumClose.transform, new Vector2(520f, -814f), new Vector2(280f, 60f));
            compendiumMask.SetActive(false);

            openButton.interactable = false;
            return new KnowledgeUi
            {
                OpenButton = openButton,
                PopupRoot = popupMask,
                PopupTitle = popupTitle,
                PopupBody = popupBody,
                PopupClose = popupClose,
                CompendiumRoot = compendiumMask,
                CompendiumBody = scrollText,
                CompendiumClose = compendiumClose
            };
        }

        private static MainMenuUi BuildMainMenu(Transform canvas)
        {
            var root = CreatePanel(canvas, "MainMenuRoot", new Color(0.008f, 0.025f, 0.038f, 0.985f));
            Stretch((RectTransform)root.transform, Vector2.zero, Vector2.zero);
            var card = CreatePanel(root.transform, "MainMenuCard", new Color(0.025f, 0.085f, 0.12f, 0.98f));
            SetAnchoredRect((RectTransform)card.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 760f));
            var title = CreateText(card.transform, "MainMenuTitle", "高速动车组转向架装配与调试实训", 38,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.34f, 0.94f, 1f));
            SetTopRect(title.rectTransform, new Vector2(44f, -74f), new Vector2(632f, 70f));
            var subtitle = CreateText(card.transform, "MainMenuSubtitle",
                "标准工单 RC-EMU-01 · 知识确认 · 分级装配 · 落车 · 调试检验",
                20, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.8f, 0.88f, 0.92f));
            SetTopRect(subtitle.rectTransform, new Vector2(44f, -150f), new Vector2(632f, 54f));

            var start = CreateButton(card.transform, "StartGameButton", "开始标准实训", 25);
            var resume = CreateButton(card.transform, "ContinueGameButton", "继续实训", 25);
            var settings = CreateButton(card.transform, "SettingsButton", "设置", 25);
            var quit = CreateButton(card.transform, "QuitButton", "退出", 25);
            SetTopRect((RectTransform)start.transform, new Vector2(150f, -260f), new Vector2(420f, 68f));
            SetTopRect((RectTransform)resume.transform, new Vector2(150f, -350f), new Vector2(420f, 68f));
            SetTopRect((RectTransform)settings.transform, new Vector2(150f, -440f), new Vector2(420f, 68f));
            SetTopRect((RectTransform)quit.transform, new Vector2(150f, -530f), new Vector2(420f, 68f));
            var footnote = CreateText(card.transform, "MainMenuFootnote",
                "本轮详细装配一套代表性转向架；落车时按整车工况显示，八编组展示在结算后单独查看。",
                18, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.62f, 0.72f, 0.78f));
            SetTopRect(footnote.rectTransform, new Vector2(60f, -650f), new Vector2(600f, 52f));

            var settingsRoot = CreatePanel(root.transform, "SettingsRoot",
                new Color(0.012f, 0.04f, 0.058f, 0.995f));
            SetAnchoredRect((RectTransform)settingsRoot.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760f, 600f));
            var settingsTitle = CreateText(settingsRoot.transform, "SettingsTitle", "设置", 38,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.34f, 0.94f, 1f));
            SetTopRect(settingsTitle.rectTransform, new Vector2(60f, -42f), new Vector2(640f, 64f));
            var volumeLabel = CreateText(settingsRoot.transform, "VolumeLabel", "音效音量", 23,
                FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            SetTopRect(volumeLabel.rectTransform, new Vector2(86f, -152f), new Vector2(240f, 46f));
            var volume = CreateSlider(settingsRoot.transform, "VolumeSlider", true);
            SetTopRect((RectTransform)volume.transform, new Vector2(86f, -212f), new Vector2(500f, 28f));
            var volumeValue = CreateText(settingsRoot.transform, "VolumeValue", "80%", 21,
                FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.76f, 0.24f));
            SetTopRect(volumeValue.rectTransform, new Vector2(600f, -202f), new Vector2(76f, 44f));
            var qualityLabel = CreateText(settingsRoot.transform, "QualityLabel", "画质", 23,
                FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            SetTopRect(qualityLabel.rectTransform, new Vector2(86f, -294f), new Vector2(240f, 46f));
            var quality = CreateDropdown(settingsRoot.transform, "QualityDropdown", "画质等级");
            SetTopRect((RectTransform)quality.transform, new Vector2(86f, -356f), new Vector2(590f, 58f));
            var settingsHint = CreateText(settingsRoot.transform, "SettingsHint",
                "设置会自动保存，并在下次启动时恢复。",
                18, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.72f, 0.8f, 0.84f));
            SetTopRect(settingsHint.rectTransform, new Vector2(80f, -442f), new Vector2(600f, 44f));
            var back = CreateButton(settingsRoot.transform, "SettingsBackButton", "保存并返回", 22);
            SetTopRect((RectTransform)back.transform, new Vector2(230f, -506f), new Vector2(300f, 62f));
            settingsRoot.SetActive(false);

            return new MainMenuUi
            {
                Root = root,
                SettingsRoot = settingsRoot,
                Title = title,
                Subtitle = subtitle,
                Footnote = footnote,
                Start = start,
                Continue = resume,
                Settings = settings,
                Quit = quit,
                SettingsBack = back,
                Volume = volume,
                VolumeValue = volumeValue,
                Quality = quality
            };
        }

        private static void BuildPartStations(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            ThirdPersonInputLock inputLock,
            PlayerInteractionScanner scanner,
            WhiteboxQuizPanel quizPanel,
            Palette palette)
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var specs = new[]
            {
                // Materials are grouped by the subassembly they feed. This
                // restores a short learn -> collect -> assemble loop and keeps
                // the long landing track clear on the east side of the hall.
                new PartStationSpec(PartId.Axle, new Vector3(-21f, 0f, -16f), 0f),
                new PartStationSpec(PartId.Wheel, new Vector3(-17.5f, 0f, -16f), 0f),
                new PartStationSpec(PartId.Bearing, new Vector3(-14f, 0f, -16f), 0f),
                new PartStationSpec(PartId.BrakeDevice, new Vector3(-10.5f, 0f, -16f), 0f),
                new PartStationSpec(PartId.TractionRod, new Vector3(-7f, 0f, -16f), 0f),
                new PartStationSpec(PartId.SensorBracket, new Vector3(-3.5f, 0f, -16f), 0f),
                new PartStationSpec(PartId.PrimaryElasticElement, new Vector3(0f, 0f, -16f), 0f),
                new PartStationSpec(PartId.PrimaryPositioningElement, new Vector3(3.5f, 0f, -16f), 0f),
                new PartStationSpec(PartId.PrimaryDamper, new Vector3(7f, 0f, -16f), 0f),
                new PartStationSpec(PartId.SecondaryElasticElement, new Vector3(10.5f, 0f, -16f), 0f),
                new PartStationSpec(PartId.HeightControlElement, new Vector3(14f, 0f, -16f), 0f),
                new PartStationSpec(PartId.SecondaryDamper, new Vector3(17.5f, 0f, -16f), 0f),
                new PartStationSpec(PartId.Carbody, new Vector3(23f, 0f, -7f), 90f),
                new PartStationSpec(PartId.CentralTractionDevice, new Vector3(23f, 0f, -2f), 90f)
            };

            var stations = CreateChild(parent, "QuizPartStations");
            foreach (var spec in specs)
            {
                var part = catalog.GetPart(spec.PartId);
                var questions = catalog.Questions
                    .Where(item => item.RewardPart == spec.PartId)
                    .Select((item, index) => CreateQuestionPresentation(item, (int)spec.PartId + index))
                    .ToArray();
                if (questions.Length == 0)
                    throw new InvalidOperationException($"Part {spec.PartId} has no questions.");
                var station = new GameObject($"QuizStation_{part.Key}");
                station.transform.SetParent(stations.transform, false);
                station.transform.localPosition = spec.Position;
                station.transform.localRotation = Quaternion.Euler(0f, spec.FacingYaw, 0f);
                var collider = station.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 1.1f, 0f);
                collider.size = new Vector3(2.8f, 2.2f, 2.6f);
                collider.isTrigger = true;

                var material = PartMaterial(spec.PartId, palette);
                CreatePrimitive(PrimitiveType.Cube, station.transform, "Pedestal",
                    new Vector3(0f, 0.42f, 0f), new Vector3(2.4f, 0.84f, 1.65f), palette.Steel);
                CreatePrimitive(PrimitiveType.Cube, station.transform, "ControlScreen",
                    new Vector3(0f, 1.28f, -0.46f), new Vector3(1.65f, 0.72f, 0.12f), palette.Station);
                CreatePrimitive(PrimitiveType.Cube, station.transform, "ColorBand",
                    new Vector3(0f, 0.9f, -0.57f), new Vector3(2.15f, 0.12f, 0.08f), material);
                var rewardVisual = BuildPartVisual(
                    station.transform,
                    $"Reward_{part.Key}",
                    spec.PartId,
                    material);
                rewardVisual.transform.localPosition = new Vector3(0f, 1.85f, 0.15f);

                CreateWorldLabel(station.transform, "StationLabel", part.DisplayName + "工位",
                    new Vector3(0f, 2.85f, 0f), Quaternion.Euler(0f, 180f, 0f), material.color, 0.7f);

                var stationBehaviour = station.AddComponent<QuizPartStation>();
                stationBehaviour.Configure(
                    sessionHost,
                    inputLock,
                    quizPanel,
                    questions,
                    spec.PartId,
                    part.DisplayName + "知识工位",
                    rewardVisual,
                    "继续收集零件，或前往流程图对应的装配工位");
                AddInteractionVisual(station, scanner, stationBehaviour);
            }
        }

        private static void BuildModuleStations(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            PlayerInteractionScanner scanner,
            ICollection<AssemblyFocusBinding> focusBindings,
            Palette palette)
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var definitions = new[]
            {
                (Id: ModuleId.WheelsetAxlebox, Position: new Vector3(-18f, 0f, -10f), Yaw: 0f),
                (Id: ModuleId.Frame, Position: new Vector3(-10f, 0f, -10f), Yaw: 0f),
                (Id: ModuleId.PrimarySuspension, Position: new Vector3(-2f, 0f, -10f), Yaw: 0f),
                (Id: ModuleId.SecondarySuspension, Position: new Vector3(6f, 0f, -10f), Yaw: 0f)
            };

            var stations = CreateChild(parent, "ModuleAssemblyStations");
            CreateWorldLabel(
                stations.transform,
                "AssemblyDemonstrationNotice",
                "结构示范件｜本轮精装 1 套代表性转向架；同型第 2 套由配套生产线提供",
                new Vector3(-6f, 3.9f, -7.2f),
                Quaternion.Euler(0f, 180f, 0f),
                palette.Warning.color,
                0.42f);
            foreach (var definition in definitions)
            {
                var module = catalog.GetModule(definition.Id);
                var station = new GameObject($"ModuleStation_{module.Key}");
                station.transform.SetParent(stations.transform, false);
                station.transform.localPosition = definition.Position;
                station.transform.localRotation = Quaternion.Euler(0f, definition.Yaw, 0f);
                var collider = station.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 1f, 0f);
                collider.size = new Vector3(4.8f, 2f, 3.4f);
                collider.isTrigger = true;

                var material = ModuleMaterial(definition.Id, palette);
                CreatePrimitive(PrimitiveType.Cube, station.transform, "AssemblyTable",
                    new Vector3(0f, 0.55f, 0f), new Vector3(4.5f, 0.32f, 2.7f), palette.Steel);
                CreatePrimitive(PrimitiveType.Cube, station.transform, "ModuleColorBand",
                    new Vector3(0f, 0.76f, -1.25f), new Vector3(4.2f, 0.12f, 0.12f), material);

                var parts = module.RequiredParts.ToArray();
                var slots = new Transform[parts.Length];
                var partVisuals = new GameObject[parts.Length];
                for (var index = 0; index < parts.Length; index++)
                {
                    var slot = CreateChild(station.transform, $"SnapSlot_{parts[index]}");
                    slot.transform.localPosition = new Vector3(-1.35f + index * 1.35f, 1.04f, 0f);
                    slots[index] = slot.transform;
                    var pad = CreatePrimitive(PrimitiveType.Cube, station.transform, $"SlotPad_{index + 1}",
                        new Vector3(-1.35f + index * 1.35f, 0.8f, 0f),
                        new Vector3(1.05f, 0.08f, 1.35f), palette.Locked);
                    RemoveCollider(pad);
                    partVisuals[index] = BuildPartVisual(
                        station.transform,
                        $"Installed_{parts[index]}",
                        parts[index],
                        material);
                }

                var completeBeacon = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    station.transform,
                    "ModuleCompleteBeacon",
                    new Vector3(0f, 2.25f, 1.12f),
                    new Vector3(0.16f, 0.5f, 0.16f),
                    palette.Success);
                RemoveCollider(completeBeacon);
                completeBeacon.SetActive(false);
                CreateWorldLabel(station.transform, "StationLabel", module.DisplayName + "装配台（结构示范）",
                    new Vector3(0f, 2.85f, 0f), Quaternion.Euler(0f, 180f, 0f), material.color, 0.74f);

                var stationBehaviour = station.AddComponent<ModuleAssemblyStation>();
                stationBehaviour.Configure(
                    sessionHost,
                    definition.Id,
                    module.DisplayName + "装配台",
                    parts,
                    slots,
                    partVisuals,
                    completeBeacon,
                    definition.Id == ModuleId.SecondarySuspension
                        ? "二系悬挂装置完成；继续准备落车所需输入"
                        : "继续完成子总成；轮对轴箱、构架和一系悬挂将组成转向架构体");
                AddInteractionVisual(station, scanner, stationBehaviour);
                focusBindings?.Add(new AssemblyFocusBinding(
                    definition.Id,
                    completeBeacon.transform,
                    configuredFallbackDistance: 5.2f,
                    configuredFocusOffset: new Vector3(0f, -0.25f, 0f)));
            }
        }

        private static QuizQuestionPresentation CreateQuestionPresentation(
            QuizQuestionDefinition question,
            int rotationSeed)
        {
            var optionCount = question.Options.Count;
            var displayedOptions = new string[optionCount];
            var submittedIndices = new int[optionCount];
            var shift = optionCount == 0 ? 0 : ((rotationSeed % optionCount) + optionCount) % optionCount;
            for (var displayedIndex = 0; displayedIndex < optionCount; displayedIndex++)
            {
                var sourceIndex = (displayedIndex + shift) % optionCount;
                displayedOptions[displayedIndex] = question.Options[sourceIndex];
                submittedIndices[displayedIndex] = sourceIndex;
            }

            return new QuizQuestionPresentation(
                question.Id,
                question.Prompt,
                displayedOptions,
                submittedIndices,
                question.Explanation);
        }

        private static void BuildCompositeAssembly(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            PlayerInteractionScanner scanner,
            ICollection<AssemblyFocusBinding> focusBindings,
            Palette palette)
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var module = catalog.GetModule(ModuleId.BogieStructure);
            var station = new GameObject("CompositeStation_BogieStructure");
            station.transform.SetParent(parent, false);
            station.transform.localPosition = new Vector3(0f, 0f, 1.5f);
            var collider = station.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.size = new Vector3(7f, 2.2f, 5.5f);
            collider.isTrigger = true;

            CreatePrimitive(PrimitiveType.Cube, station.transform, "AssemblyTable",
                new Vector3(0f, 0.5f, 0f), new Vector3(6.4f, 0.34f, 4.6f), palette.Steel);
            CreatePrimitive(PrimitiveType.Cube, station.transform, "FlowBand",
                new Vector3(0f, 0.75f, -2.05f), new Vector3(5.9f, 0.12f, 0.12f), palette.Running);

            var children = module.RequiredModules.ToArray();
            var slots = new Transform[children.Length];
            var visuals = new GameObject[children.Length];
            for (var index = 0; index < children.Length; index++)
            {
                var slot = CreateChild(station.transform, $"ModuleSnapSlot_{children[index]}");
                // Demonstration modules retain the source FBX coordinate space so
                // they converge into one correctly aligned bogie instead of three
                // separated proxy shapes.
                // The table top is y=0.67. The imported RailContactPlane is
                // the wheel-bottom datum, so keep each demonstration layer on
                // that same surface instead of making the finished bogie float.
                slot.transform.localPosition = new Vector3(0f, 0.69f, 0f);
                slots[index] = slot.transform;
                visuals[index] = BuildFinalModuleVisual(
                    station.transform,
                    $"Installed_{children[index]}",
                    children[index],
                    ModuleMaterial(children[index], palette),
                    palette,
                    preserveAssemblyCoordinates: true);
            }

            var completedVisual = CreateChild(station.transform, "BogieStructureCompletionVisual");
            completedVisual.transform.localPosition = new Vector3(0f, 0.69f, 0f);
            BogieAssemblyDemoVisualFactory.TryCreateFixedDriveVisual(
                completedVisual.transform,
                "Installed_FixedDrivePackage",
                palette.Electrical,
                out _);

            var completeBeacon = CreatePrimitive(PrimitiveType.Cylinder, completedVisual.transform,
                "BogieStructureCompleteBeacon", new Vector3(0f, 1.35f, 1.7f),
                new Vector3(0.18f, 0.58f, 0.18f), palette.Success);
            RemoveCollider(completeBeacon);
            completedVisual.SetActive(false);
            CreateWorldLabel(station.transform, "StationLabel", "代表性转向架总成装配台（结构示范）",
                new Vector3(0f, 3.15f, -2.1f), Quaternion.identity, palette.Running.color, 0.58f);

            var behaviour = station.AddComponent<CompositeAssemblyStation>();
            behaviour.Configure(
                sessionHost,
                ModuleId.BogieStructure,
                "转向架构体装配台",
                children,
                slots,
                visuals,
                completedVisual,
                "代表性转向架完成；同型第 2 套由配套生产线提供，继续准备落车输入");
            AddInteractionVisual(station, scanner, behaviour);
            focusBindings?.Add(new AssemblyFocusBinding(
                ModuleId.BogieStructure,
                completeBeacon.transform,
                configuredFallbackDistance: 7f,
                configuredFocusOffset: new Vector3(0f, -0.4f, 0f)));
        }

        private static void BuildFinalAssembly(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            PlayerInteractionScanner scanner,
            ICollection<AssemblyFocusBinding> focusBindings,
            Palette palette)
        {
            var station = new GameObject("LandingAssemblyStation");
            station.transform.SetParent(parent, false);
            station.transform.localPosition = new Vector3(15f, 0f, 0f);
            var collider = station.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.25f, 0f);
            // The reference intermediate coach is approximately 25.7 m long,
            // while interaction should remain a compact console-sized target.
            // Keep the long display lane separate from the trigger footprint so
            // it cannot steal nearby module or commissioning interactions.
            collider.size = new Vector3(9f, 5.5f, 7f);
            collider.isTrigger = true;

            var lane = CreatePrimitive(PrimitiveType.Cube, station.transform, "AssemblyPlatform",
                new Vector3(0f, 0.18f, 0f), new Vector3(8.4f, 0.36f, 27f), palette.Steel);
            RemoveCollider(lane);
            foreach (var x in new[] { -0.7175f, 0.7175f })
            {
                CreatePrimitive(PrimitiveType.Cube, station.transform, $"Rail_{x}",
                    new Vector3(x, 0.42f, 0f), new Vector3(0.14f, 0.14f, 26.5f), palette.White);
            }

            var moduleOrder = new[] { ModuleId.BogieStructure, ModuleId.SecondarySuspension };
            var partOrder = new[] { PartId.Carbody, PartId.CentralTractionDevice };
            var moduleSlots = new Transform[moduleOrder.Length];
            var moduleVisuals = new GameObject[moduleOrder.Length];
            var moduleSlotPositions = new[]
            {
                new Vector3(-2f, 0.86f, 0.4f),
                new Vector3(2f, 0.86f, 0.4f)
            };
            for (var index = 0; index < moduleOrder.Length; index++)
            {
                var slot = CreateChild(station.transform, $"ModuleSnapSlot_{moduleOrder[index]}");
                slot.transform.localPosition = moduleSlotPositions[index];
                moduleSlots[index] = slot.transform;
                moduleVisuals[index] = BuildFinalModuleVisual(
                    station.transform,
                    $"LandingInput_{moduleOrder[index]}",
                    moduleOrder[index],
                    ModuleMaterial(moduleOrder[index], palette),
                    palette);
            }

            var partSlots = new Transform[partOrder.Length];
            var partVisuals = new GameObject[partOrder.Length];
            var partSlotPositions = new[]
            {
                // Keep the long carbody preview at the end of the expanded
                // lane so it cannot visually overlap the compact input
                // modules while the player is staging the landing sequence.
                new Vector3(-2f, 1.55f, -6f),
                new Vector3(2f, 1.55f, -1.2f)
            };
            for (var index = 0; index < partOrder.Length; index++)
            {
                var slot = CreateChild(station.transform, $"PartSnapSlot_{partOrder[index]}");
                slot.transform.localPosition = partSlotPositions[index];
                partSlots[index] = slot.transform;
                partVisuals[index] = BuildPartVisual(
                    station.transform,
                    $"LandingInput_{partOrder[index]}",
                    partOrder[index],
                    PartMaterial(partOrder[index], palette));
            }

            // The process inputs disappear when landing is complete and one
            // coherent product visual takes their place. Internal parts do not
            // need to remain exposed for the product to read as completed.
            var completedLandingVisual = BuildLandingVehicleVisual(
                station.transform,
                "DroppedVehicle",
                palette);

            // Rail heads finish at Y=0.49. The imported bogie visuals use their
            // RailContactPlane anchor as local zero, so both wheelsets sit on top.
            completedLandingVisual.transform.localPosition = new Vector3(0f, 0.49f, 0f);
            completedLandingVisual.SetActive(false);
            CreateWorldLabel(station.transform, "FinalAssemblyLabel", "落车工位 · 整车落位工况演示",
                new Vector3(0f, 5.35f, -13.1f), Quaternion.identity, palette.Safety.color, 0.62f);
            CreateWorldLabel(
                station.transform,
                "SecondBogieSupplyNotice",
                "本轮精装的代表性转向架已用于整车落位演示\n另一转向架由配套生产线提供，成品按完整落车工况呈现",
                new Vector3(0f, 3.75f, 13.1f),
                Quaternion.identity,
                palette.Warning.color,
                0.42f);

            var behaviour = station.AddComponent<FinalAssemblyStation>();
            behaviour.Configure(
                sessionHost,
                ModuleId.Landing,
                "落车工位",
                moduleOrder,
                partOrder,
                moduleSlots,
                partSlots,
                moduleVisuals,
                partVisuals,
                completedLandingVisual);
            AddInteractionVisual(station, scanner, behaviour);
            focusBindings?.Add(new AssemblyFocusBinding(
                ModuleId.Landing,
                completedLandingVisual.transform,
                configuredFallbackDistance: 17f,
                configuredFocusOffset: new Vector3(0f, 1.6f, 0f)));
        }

        private static void BuildCommissioningStations(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            PlayerInteractionScanner scanner,
            Palette palette)
        {
            var root = CreateChild(parent, "CommissioningLoopStations");
            var definitions = new[]
            {
                (Action: CommissioningAction.Test, Name: "调试判定", Position: new Vector3(-9f, 0f, 16f)),
                (Action: CommissioningAction.Retune, Name: "重新调试", Position: new Vector3(0f, 0f, 16f)),
                (Action: CommissioningAction.Inspect, Name: "检验", Position: new Vector3(8f, 0f, 16f))
            };

            foreach (var definition in definitions)
            {
                var station = new GameObject($"Commissioning_{definition.Action}");
                station.transform.SetParent(root.transform, false);
                station.transform.localPosition = definition.Position;
                var collider = station.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 1f, 0f);
                collider.size = new Vector3(5.8f, 2.2f, 4.5f);
                collider.isTrigger = true;

                CreatePrimitive(PrimitiveType.Cube, station.transform, "ConsoleBase",
                    new Vector3(0f, 0.55f, 0f), new Vector3(5.2f, 0.7f, 3.4f), palette.Steel);
                CreatePrimitive(PrimitiveType.Cube, station.transform, "ConsoleFace",
                    new Vector3(0f, 1.25f, -0.8f), new Vector3(3.8f, 0.9f, 0.16f), palette.Station);
                var ready = CreatePrimitive(PrimitiveType.Cylinder, station.transform, "ReadyBeacon",
                    new Vector3(-0.55f, 2f, 0f), new Vector3(0.18f, 0.5f, 0.18f), palette.Warning);
                var completed = CreatePrimitive(PrimitiveType.Cylinder, station.transform, "CompletedBeacon",
                    new Vector3(0.55f, 2f, 0f), new Vector3(0.18f, 0.5f, 0.18f), palette.Success);
                RemoveCollider(ready);
                RemoveCollider(completed);
                completed.SetActive(false);
                CreateWorldLabel(station.transform, "StationLabel", definition.Name,
                    new Vector3(0f, 3f, -1.25f), Quaternion.identity, palette.Safety.color, 0.72f);

                var behaviour = station.AddComponent<CommissioningStation>();
                behaviour.Configure(sessionHost, definition.Action, definition.Name, ready, completed);
                AddInteractionVisual(station, scanner, behaviour);
            }

            CreateWorldLabel(root.transform, "LoopGuide",
                "首次调试判定（教学故障注入） → 重新调试 → 检验 → 复测判定 → 投入使用",
                new Vector3(0f, 4.7f, 19.9f), Quaternion.identity, palette.Safety.color, 0.58f);
        }

        private static GameObject BuildPartVisual(
            Transform parent,
            string name,
            PartId partId,
            Material material)
        {
            if (BogieAssemblyDemoVisualFactory.TryCreatePartVisual(
                    parent,
                    name,
                    partId,
                    material,
                    out var demonstrationVisual))
                return demonstrationVisual;

            var root = CreateChild(parent, name);
            switch (partId)
            {
                case PartId.Axle:
                    CreateCylinder(root.transform, "Axle", Vector3.zero, new Vector3(0.12f, 0.72f, 0.12f),
                        Quaternion.Euler(0f, 0f, 90f), material);
                    break;
                case PartId.Wheel:
                    CreateCylinder(root.transform, "Wheel", Vector3.zero, new Vector3(0.5f, 0.14f, 0.5f),
                        Quaternion.Euler(0f, 0f, 90f), material);
                    break;
                case PartId.Bearing:
                    CreateCylinder(root.transform, "BearingOuter", Vector3.zero, new Vector3(0.42f, 0.28f, 0.42f),
                        Quaternion.Euler(0f, 0f, 90f), material);
                    CreateCylinder(root.transform, "BearingHub", Vector3.zero, new Vector3(0.18f, 0.32f, 0.18f),
                        Quaternion.Euler(0f, 0f, 90f), material);
                    break;
                case PartId.BrakeDevice:
                    CreateVisualCube(root.transform, "Caliper", Vector3.zero, new Vector3(0.75f, 0.45f, 0.5f), material);
                    CreateCylinder(root.transform, "BrakeDisc", new Vector3(0.5f, 0f, 0f),
                        new Vector3(0.38f, 0.08f, 0.38f), Quaternion.Euler(0f, 0f, 90f), material);
                    break;
                case PartId.TractionRod:
                    CreateCylinderBetween(root.transform, "TractionRod", new Vector3(-0.75f, 0f, 0f),
                        new Vector3(0.75f, 0f, 0f), 0.11f, material);
                    CreateCylinder(root.transform, "JointA", new Vector3(-0.78f, 0f, 0f),
                        new Vector3(0.2f, 0.1f, 0.2f), Quaternion.Euler(90f, 0f, 0f), material);
                    CreateCylinder(root.transform, "JointB", new Vector3(0.78f, 0f, 0f),
                        new Vector3(0.2f, 0.1f, 0.2f), Quaternion.Euler(90f, 0f, 0f), material);
                    break;
                case PartId.SensorBracket:
                    CreateVisualCube(root.transform, "Base", new Vector3(0f, -0.2f, 0f),
                        new Vector3(0.9f, 0.18f, 0.65f), material);
                    CreateVisualCube(root.transform, "PostA", new Vector3(-0.34f, 0.2f, 0f),
                        new Vector3(0.16f, 0.8f, 0.16f), material);
                    CreateVisualCube(root.transform, "PostB", new Vector3(0.34f, 0.2f, 0f),
                        new Vector3(0.16f, 0.8f, 0.16f), material);
                    break;
                case PartId.PrimaryElasticElement:
                    CreateCylinder(root.transform, "SpringA", new Vector3(-0.32f, 0f, 0f),
                        new Vector3(0.22f, 0.48f, 0.22f), Quaternion.identity, material);
                    CreateCylinder(root.transform, "SpringB", new Vector3(0.32f, 0f, 0f),
                        new Vector3(0.22f, 0.48f, 0.22f), Quaternion.identity, material);
                    break;
                case PartId.PrimaryPositioningElement:
                    CreateVisualCube(root.transform, "PositioningArm", Vector3.zero,
                        new Vector3(1.4f, 0.18f, 0.34f), material);
                    CreateCylinder(root.transform, "Bushing", new Vector3(0.62f, 0f, 0f),
                        new Vector3(0.24f, 0.14f, 0.24f), Quaternion.Euler(90f, 0f, 0f), material);
                    break;
                case PartId.PrimaryDamper:
                    CreateCylinderBetween(root.transform, "PrimaryDamper", new Vector3(-0.55f, -0.35f, 0f),
                        new Vector3(0.55f, 0.35f, 0f), 0.15f, material);
                    break;
                case PartId.SecondaryElasticElement:
                    CreateCylinder(root.transform, "AirSpringA", new Vector3(-0.38f, 0f, 0f),
                        new Vector3(0.38f, 0.32f, 0.38f), Quaternion.identity, material);
                    CreateCylinder(root.transform, "AirSpringB", new Vector3(0.38f, 0f, 0f),
                        new Vector3(0.38f, 0.32f, 0.38f), Quaternion.identity, material);
                    break;
                case PartId.HeightControlElement:
                    CreateVisualCube(root.transform, "Valve", Vector3.zero, new Vector3(0.55f, 0.65f, 0.5f), material);
                    CreateCylinderBetween(root.transform, "Link", new Vector3(0.25f, -0.45f, 0f),
                        new Vector3(0.75f, 0.45f, 0f), 0.08f, material);
                    break;
                case PartId.SecondaryDamper:
                    CreateCylinderBetween(root.transform, "SecondaryDamper", new Vector3(-0.7f, 0f, 0f),
                        new Vector3(0.7f, 0f, 0f), 0.17f, material);
                    break;
                case PartId.Carbody:
                    CreateVisualCube(root.transform, "Carbody", new Vector3(0f, 0.28f, 0f),
                        new Vector3(1.75f, 0.72f, 0.9f), material);
                    var nose = CreatePrimitive(PrimitiveType.Sphere, root.transform, "CabNose",
                        new Vector3(0f, 0.2f, -0.42f), new Vector3(1.5f, 0.58f, 0.68f), material);
                    RemoveCollider(nose);
                    break;
                case PartId.CentralTractionDevice:
                    CreateVisualCube(root.transform, "CenterBlock", Vector3.zero,
                        new Vector3(0.7f, 0.5f, 0.65f), material);
                    CreateCylinderBetween(root.transform, "TractionLinkA", new Vector3(-0.85f, 0f, 0f),
                        new Vector3(-0.25f, 0f, 0f), 0.11f, material);
                    CreateCylinderBetween(root.transform, "TractionLinkB", new Vector3(0.25f, 0f, 0f),
                        new Vector3(0.85f, 0f, 0f), 0.11f, material);
                    break;
            }
            return root;
        }

        private static GameObject BuildFinalModuleVisual(
            Transform parent,
            string name,
            ModuleId moduleId,
            Material material,
            Palette palette,
            bool preserveAssemblyCoordinates = false)
        {
            var demonstrationMaterial = moduleId == ModuleId.WheelsetAxlebox
                ? palette.Steel
                : material;
            if (BogieAssemblyDemoVisualFactory.TryCreateModuleVisual(
                    parent,
                    name,
                    moduleId,
                    demonstrationMaterial,
                    preserveAssemblyCoordinates,
                    out var demonstrationVisual))
                return demonstrationVisual;

            var root = CreateChild(parent, name);
            switch (moduleId)
            {
                case ModuleId.WheelsetAxlebox:
                    CreateCylinder(root.transform, "Axle", Vector3.zero, new Vector3(0.1f, 0.65f, 0.1f),
                        Quaternion.Euler(0f, 0f, 90f), palette.Steel);
                    CreateCylinder(root.transform, "WheelA", new Vector3(-0.55f, 0f, 0f),
                        new Vector3(0.34f, 0.1f, 0.34f), Quaternion.Euler(0f, 0f, 90f), material);
                    CreateCylinder(root.transform, "WheelB", new Vector3(0.55f, 0f, 0f),
                        new Vector3(0.34f, 0.1f, 0.34f), Quaternion.Euler(0f, 0f, 90f), material);
                    break;
                case ModuleId.Frame:
                    CreateVisualCube(root.transform, "Frame", Vector3.zero, new Vector3(1.55f, 0.18f, 1.05f), material);
                    CreateVisualCube(root.transform, "CrossA", new Vector3(-0.58f, 0.18f, 0f),
                        new Vector3(0.16f, 0.36f, 1.2f), material);
                    CreateVisualCube(root.transform, "CrossB", new Vector3(0.58f, 0.18f, 0f),
                        new Vector3(0.16f, 0.36f, 1.2f), material);
                    break;
                case ModuleId.PrimarySuspension:
                    CreateCylinder(root.transform, "PrimarySpringA", new Vector3(-0.42f, 0f, 0f),
                        new Vector3(0.25f, 0.45f, 0.25f), Quaternion.identity, material);
                    CreateCylinder(root.transform, "PrimarySpringB", new Vector3(0.42f, 0f, 0f),
                        new Vector3(0.25f, 0.45f, 0.25f), Quaternion.identity, material);
                    break;
                case ModuleId.BogieStructure:
                    CreateVisualCube(root.transform, "BogieFrame", new Vector3(0f, 0.25f, 0f),
                        new Vector3(2.6f, 0.28f, 1.5f), material);
                    foreach (var x in new[] { -1.15f, 1.15f })
                    foreach (var z in new[] { -0.55f, 0.55f })
                        CreateCylinder(root.transform, $"Wheel_{x}_{z}", new Vector3(x, 0f, z),
                            new Vector3(0.42f, 0.13f, 0.42f), Quaternion.Euler(0f, 0f, 90f), palette.Steel);
                    break;
                case ModuleId.SecondarySuspension:
                    CreateCylinder(root.transform, "AirSpringA", new Vector3(-0.5f, 0f, 0f),
                        new Vector3(0.42f, 0.34f, 0.42f), Quaternion.identity, material);
                    CreateCylinder(root.transform, "AirSpringB", new Vector3(0.5f, 0f, 0f),
                        new Vector3(0.42f, 0.34f, 0.42f), Quaternion.identity, material);
                    break;
            }
            return root;
        }

        private static GameObject BuildLandingVehicleVisual(Transform parent, string name, Palette palette)
        {
            var root = CreateChild(parent, name);
            // The extracted intermediate coach keeps its normalized 1:1
            // dimensions (about 25.7 m long). Its visual bottom is placed
            // 4.5 cm above the completed bogie mount to avoid interpenetration.
            if (BogieAssemblyDemoVisualFactory.TryCreateProductCarbodyVisual(
                    root.transform,
                    "FuxingCarbodyReference",
                    displayLength: 0f,
                    out var carbodyReference))
            {
                carbodyReference.transform.localPosition = new Vector3(0f, 1.575f, 0f);
                BuildProductCarbodyLivery(carbodyReference.transform, palette);
            }
            else
            {
                CreateVisualCube(root.transform, "CarbodyFallback", new Vector3(0f, 2.3f, 0f),
                    new Vector3(3.6f, 1.45f, 9.2f), palette.Carbody);
                var nose = CreatePrimitive(PrimitiveType.Sphere, root.transform, "CabNoseFallback",
                    new Vector3(0f, 2.2f, -4.45f), new Vector3(3.1f, 1.2f, 1.35f), palette.Carbody);
                RemoveCollider(nose);
            }

            foreach (var placement in new[]
            {
                (Name: "Front", Z: -8f),
                (Name: "Rear", Z: 8f)
            })
            {
                var bogiePlacement = CreateChild(root.transform, $"LandingBogie_{placement.Name}");
                bogiePlacement.transform.localPosition = new Vector3(0f, 0f, placement.Z);
                if (!BogieAssemblyDemoVisualFactory.TryCreateCompletedBogieVisual(
                        bogiePlacement.transform,
                        "CompletedBogieVisual",
                        palette.Running,
                        out _))
                {
                    CreateVisualCube(bogiePlacement.transform, "BogieFallback", new Vector3(0f, 0.45f, 0f),
                        new Vector3(2.7f, 0.3f, 1.35f), palette.Running);
                    foreach (var x in new[] { -1.3f, 1.3f })
                        CreateCylinder(bogiePlacement.transform, $"WheelFallback_{x}", new Vector3(x, 0.2f, 0f),
                            new Vector3(0.46f, 0.14f, 0.46f), Quaternion.Euler(0f, 0f, 90f), palette.Steel);
                }
            }

            return root;
        }

        private static void BuildProductCarbodyLivery(Transform parent, Palette palette)
        {
            var glass = AssetDatabase.LoadAssetAtPath<Material>(
                BogieAssemblyDemoVisualFactory.ProductGlassMaterialPath) ?? palette.Steel;
            var blue = AssetDatabase.LoadAssetAtPath<Material>(
                BogieAssemblyDemoVisualFactory.ProductBlueMaterialPath) ?? palette.Station;

            foreach (var side in new[] { -1f, 1f })
            {
                var sideName = side < 0f ? "Left" : "Right";
                CreateVisualCube(
                    parent,
                    $"ProductLiveryStripe_{sideName}",
                    new Vector3(side * 1.746f, 1.24f, 0f),
                    new Vector3(0.028f, 0.13f, 23.6f),
                    blue);

                for (var windowIndex = 0; windowIndex < 12; windowIndex++)
                {
                    var z = -10.45f + windowIndex * 1.9f;
                    CreateVisualCube(
                        parent,
                        $"ProductWindow_{sideName}_{windowIndex + 1:00}",
                        new Vector3(side * 1.75f, 1.91f, z),
                        new Vector3(0.032f, 0.52f, 1.28f),
                        glass);
                }

                foreach (var z in new[] { -11.85f, 11.85f })
                {
                    CreateVisualCube(
                        parent,
                        $"ProductDoor_{sideName}_{(z < 0f ? "Front" : "Rear")}",
                        new Vector3(side * 1.752f, 1.2f, z),
                        new Vector3(0.034f, 1.72f, 0.72f),
                        blue);
                }
            }
        }

        private static Material PartMaterial(PartId partId, Palette palette)
        {
            switch (partId)
            {
                case PartId.Axle:
                case PartId.Wheel:
                case PartId.Bearing:
                case PartId.BrakeDevice:
                case PartId.TractionRod:
                case PartId.SensorBracket:
                case PartId.PrimaryElasticElement:
                case PartId.PrimaryPositioningElement:
                case PartId.PrimaryDamper:
                    return palette.Running;
                case PartId.Carbody:
                    return palette.Carbody;
                case PartId.CentralTractionDevice:
                    return palette.Safety;
                default:
                    return palette.Electrical;
            }
        }

        private static Material ModuleMaterial(ModuleId moduleId, Palette palette)
        {
            switch (moduleId)
            {
                case ModuleId.WheelsetAxlebox:
                case ModuleId.Frame:
                case ModuleId.PrimarySuspension:
                case ModuleId.BogieStructure:
                    return palette.Running;
                case ModuleId.Landing:
                    return palette.Carbody;
                default:
                    return palette.Electrical;
            }
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.36f, 0.43f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.19f, 0.23f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.055f, 0.065f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.19f, 0.23f, 0.27f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 64f;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType type,
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = localPosition;
            item.transform.localScale = localScale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }

        private static GameObject CreateVisualCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var cube = CreatePrimitive(PrimitiveType.Cube, parent, name, localPosition, localScale, material);
            RemoveCollider(cube);
            return cube;
        }

        private static GameObject CreateCylinder(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var cylinder = CreatePrimitive(
                PrimitiveType.Cylinder, parent, name, localPosition, localScale, material);
            cylinder.transform.localRotation = localRotation;
            RemoveCollider(cylinder);
            return cylinder;
        }

        private static GameObject CreateCylinderBetween(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float radius,
            Material material)
        {
            var direction = end - start;
            var cylinder = CreatePrimitive(
                PrimitiveType.Cylinder,
                parent,
                name,
                (start + end) * 0.5f,
                new Vector3(radius, direction.magnitude * 0.5f, radius),
                material);
            cylinder.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            RemoveCollider(cylinder);
            return cylinder;
        }

        private static void RemoveCollider(GameObject item)
        {
            var collider = item.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
        }

        private static InteractableVisualFeedback AddInteractionVisual(
            GameObject station,
            PlayerInteractionScanner scanner,
            MonoBehaviour interactable)
        {
            var feedback = station.AddComponent<InteractableVisualFeedback>();
            feedback.Configure(
                scanner,
                interactable,
                station.GetComponentsInChildren<Renderer>(true));
            return feedback;
        }

        private static void MarkStatic(GameObject item)
        {
            GameObjectUtility.SetStaticEditorFlags(
                item,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.ReflectionProbeStatic);
        }

        private static GameObject CreateWorldLabel(
            Transform parent,
            string name,
            string value,
            Vector3 localPosition,
            Quaternion localRotation,
            Color color,
            float characterSize)
        {
            var label = new GameObject(name, typeof(TextMesh));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = localRotation;
            var text = label.GetComponent<TextMesh>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.characterSize = characterSize * 0.055f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            var renderer = label.GetComponent<MeshRenderer>();
            if (text.font != null)
                renderer.sharedMaterial = text.font.material;
            return label;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            var text = item.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string value,
            int fontSize)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            var image = item.GetComponent<Image>();
            image.color = new Color(0.08f, 0.28f, 0.42f, 1f);
            var button = item.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.72f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.4f, 0.78f, 0.92f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.7f);
            button.colors = colors;
            var label = CreateText(item.transform, "Label", value, fontSize, FontStyle.Normal,
                TextAnchor.MiddleCenter, Color.white);
            Stretch(label.rectTransform, new Vector2(18f, 6f), new Vector2(-18f, -6f));
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, bool interactable)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            var slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = interactable ? 0.8f : 0f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;
            slider.interactable = interactable;

            var background = CreatePanel(root.transform, "Background", new Color(0.12f, 0.18f, 0.22f, 1f));
            Stretch((RectTransform)background.transform, new Vector2(0f, 5f), new Vector2(0f, -5f));

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch((RectTransform)fillArea.transform, new Vector2(4f, 5f), new Vector2(-4f, -5f));
            var fill = CreatePanel(fillArea.transform, "Fill", new Color(0.14f, 0.78f, 0.94f, 1f));
            Stretch((RectTransform)fill.transform, Vector2.zero, Vector2.zero);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            Stretch((RectTransform)handleArea.transform, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            var handle = CreatePanel(handleArea.transform, "Handle", new Color(1f, 0.75f, 0.22f, 1f));
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(22f, 22f);

            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        private static Dropdown CreateDropdown(Transform parent, string name, string captionValue)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
            root.transform.SetParent(parent, false);
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0.075f, 0.22f, 0.3f, 1f);

            var caption = CreateText(root.transform, "Label", captionValue, 21,
                FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            Stretch(caption.rectTransform, new Vector2(20f, 6f), new Vector2(-60f, -6f));
            var arrow = CreateText(root.transform, "Arrow", "▼", 18,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.4f, 0.92f, 1f));
            SetAnchoredRect(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(-28f, 0f),
                new Vector2(32f, 32f));

            var template = CreatePanel(root.transform, "Template", new Color(0.025f, 0.075f, 0.105f, 1f));
            var templateRect = (RectTransform)template.transform;
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -4f);
            templateRect.sizeDelta = new Vector2(0f, 250f);
            var scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreatePanel(template.transform, "Viewport", new Color(0.02f, 0.055f, 0.075f, 1f));
            Stretch((RectTransform)viewport.transform, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 48f);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRect = (RectTransform)item.transform;
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 48f);
            var itemBackground = CreatePanel(item.transform, "Item Background", new Color(0.06f, 0.17f, 0.23f, 1f));
            Stretch((RectTransform)itemBackground.transform, Vector2.zero, Vector2.zero);
            var checkmark = CreateText(item.transform, "Item Checkmark", "✓", 20,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.22f));
            SetAnchoredRect(checkmark.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f),
                new Vector2(32f, 40f));
            var itemLabel = CreateText(item.transform, "Item Label", captionValue, 20,
                FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
            Stretch(itemLabel.rectTransform, new Vector2(54f, 3f), new Vector2(-12f, -3f));
            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemBackground.GetComponent<Image>();
            toggle.graphic = checkmark;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;
            var dropdown = root.GetComponent<Dropdown>();
            dropdown.targetGraphic = rootImage;
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            template.SetActive(false);
            return dropdown;
        }

        private static Text CreateScrollableText(
            Transform parent,
            string name,
            int fontSize,
            Color color)
        {
            var root = CreatePanel(parent, name, new Color(0.02f, 0.065f, 0.09f, 0.9f));
            var scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 42f;

            var viewport = CreatePanel(root.transform, "Viewport", Color.clear);
            Stretch((RectTransform)viewport.transform, new Vector2(20f, 16f), new Vector2(-20f, -16f));
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var text = CreateText(viewport.transform, "Content", string.Empty, fontSize,
                FontStyle.Normal, TextAnchor.UpperLeft, color);
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = Vector2.zero;
            var textFitter = text.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = text.rectTransform;
            return text;
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

        private static void SetAnchoredRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
