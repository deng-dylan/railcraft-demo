using System;
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
                "探索知识工位，答题解锁并拾取 14 类装配零件");

            var playerRig = BuildPlayer(CreateChild(root.transform, "PlayerRig"), palette);
            var uiRig = BuildUi(CreateChild(root.transform, "Interface"), sessionHost, playerRig);
            var gameplay = CreateChild(root.transform, "GameplayStations");
            BuildPartStations(gameplay.transform, sessionHost, playerRig.InputLock, uiRig.QuizPanel, palette);
            BuildModuleStations(gameplay.transform, sessionHost, palette);
            BuildCompositeAssembly(gameplay.transform, sessionHost, palette);
            BuildFinalAssembly(gameplay.transform, sessionHost, palette);
            BuildCommissioningStations(gameplay.transform, sessionHost, palette);
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

            BuildZonePad(environment.transform, "WestKnowledgeLane", new Vector3(-23.2f, 0.015f, 0f),
                new Vector3(7.2f, 0.03f, 39f), palette.Running);
            BuildZonePad(environment.transform, "EastKnowledgeLane", new Vector3(23.2f, 0.015f, 0f),
                new Vector3(7.2f, 0.03f, 39f), palette.Electrical);
            BuildZonePad(environment.transform, "AssemblyFlowLane", new Vector3(0f, 0.02f, 0f),
                new Vector3(34f, 0.04f, 27f), palette.Station);
            BuildZonePad(environment.transform, "CommissioningLoop", new Vector3(0f, 0.025f, 16.5f),
                new Vector3(30f, 0.05f, 6.5f), palette.Safety);

            BuildSafetyRailings(environment.transform, palette);
            CreateWorldLabel(environment.transform, "WestLaneLabel", "零件知识工位 A",
                new Vector3(-22.5f, 4.2f, 20.65f), Quaternion.identity, palette.Running.color, 1.05f);
            CreateWorldLabel(environment.transform, "FlowLabel", "部件 → 子总成 → 转向架构体 → 落车",
                new Vector3(0f, 4.2f, 20.65f), Quaternion.identity, palette.Safety.color, 0.82f);
            CreateWorldLabel(environment.transform, "EastLaneLabel", "零件知识工位 B",
                new Vector3(22.5f, 4.2f, 20.65f), Quaternion.identity, palette.Electrical.color, 1.05f);
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

        private static void BuildSafetyRailings(Transform parent, Palette palette)
        {
            var railings = CreateChild(parent, "SafetyRailings");
            foreach (var x in new[] { -19.2f, 19.2f })
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
            orbit.ConfigureLimits(2.2f, 7f, 8f, 68f);
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
                "RAILCRAFT · 第三人称玩法白盒",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            SetAnchoredRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(620f, 46f));

            var taskPanel = CreatePanel(
                canvasObject.transform,
                "TaskPanel",
                new Color(0.025f, 0.055f, 0.075f, 0.88f));
            SetAnchoredRect((RectTransform)taskPanel.transform, new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(700f, 116f), new Vector2(0f, 1f));
            var taskText = CreateText(taskPanel.transform, "TaskText", "当前任务：探索工位", 23,
                FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.76f, 0.94f, 1f));
            Stretch(taskText.rectTransform, new Vector2(24f, 18f), new Vector2(-24f, -18f));

            var statePanel = CreatePanel(
                canvasObject.transform,
                "StatePanel",
                new Color(0.025f, 0.055f, 0.075f, 0.88f));
            SetAnchoredRect((RectTransform)statePanel.transform, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(780f, 178f), new Vector2(1f, 1f));
            var progressText = CreateText(statePanel.transform, "ProgressText", "装配节点 0/6 · 落车未完成 · 调试锁定", 22,
                FontStyle.Bold, TextAnchor.UpperLeft, new Color(1f, 0.78f, 0.26f));
            SetTopRect(progressText.rectTransform, new Vector2(24f, -18f), new Vector2(732f, 50f));
            var inventoryText = CreateText(statePanel.transform, "InventoryText", "库存：空", 19,
                FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            SetTopRect(inventoryText.rectTransform, new Vector2(24f, -72f), new Vector2(732f, 92f));

            var controlsPanel = CreatePanel(
                canvasObject.transform,
                "ControlsPanel",
                new Color(0.025f, 0.055f, 0.075f, 0.78f));
            SetAnchoredRect((RectTransform)controlsPanel.transform, new Vector2(0f, 0f), new Vector2(28f, 28f), new Vector2(570f, 74f), new Vector2(0f, 0f));
            var controls = CreateText(controlsPanel.transform, "ControlsText",
                "WASD 移动  ·  Shift 奔跑  ·  鼠标视角  ·  滚轮缩放  ·  E 交互",
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
            var completionPanel = BuildCompletionPanel(canvasObject.transform, sessionHost, out var completionText);

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
                completionPanel,
                completionText);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(interfaceRoot.transform, false);
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            return new UiRig
            {
                QuizPanel = quizPanelComponent,
                Hud = hud
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

        private static GameObject BuildCompletionPanel(
            Transform canvas,
            WhiteboxGameSessionHost sessionHost,
            out Text completionText)
        {
            var panel = CreatePanel(canvas, "CompletionPanel", new Color(0.018f, 0.05f, 0.055f, 0.98f));
            SetAnchoredRect((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 380f));
            completionText = CreateText(panel.transform, "CompletionText", "调试通过，车辆投入使用！", 38,
                FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.2f, 1f, 0.48f));
            SetTopRect(completionText.rectTransform, new Vector2(48f, -58f), new Vector2(664f, 90f));
            var detail = CreateText(panel.transform, "CompletionDetail",
                "14 类基础零件 → 5 个子总成 → 落车 → 调试回路\n流程闭环验证完成，可进入 Blender 资产替换阶段。",
                23, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            SetTopRect(detail.rectTransform, new Vector2(58f, -158f), new Vector2(644f, 96f));
            var reset = CreateButton(panel.transform, "ResetWhiteboxButton", "重新开始", 23);
            SetTopRect((RectTransform)reset.transform, new Vector2(248f, -292f), new Vector2(264f, 62f));
            var resetController = panel.AddComponent<WhiteboxResetButton>();
            resetController.Configure(sessionHost, reset);
            panel.SetActive(false);
            return panel;
        }

        private static void BuildPartStations(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            ThirdPersonInputLock inputLock,
            WhiteboxQuizPanel quizPanel,
            Palette palette)
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var specs = new[]
            {
                new PartStationSpec(PartId.Axle, new Vector3(-23.2f, 0f, -15f), -90f),
                new PartStationSpec(PartId.Wheel, new Vector3(-23.2f, 0f, -10f), -90f),
                new PartStationSpec(PartId.Bearing, new Vector3(-23.2f, 0f, -5f), -90f),
                new PartStationSpec(PartId.BrakeDevice, new Vector3(-23.2f, 0f, 0f), -90f),
                new PartStationSpec(PartId.TractionRod, new Vector3(-23.2f, 0f, 5f), -90f),
                new PartStationSpec(PartId.SensorBracket, new Vector3(-23.2f, 0f, 10f), -90f),
                new PartStationSpec(PartId.Carbody, new Vector3(-23.2f, 0f, 15f), -90f),
                new PartStationSpec(PartId.PrimaryElasticElement, new Vector3(23.2f, 0f, -15f), 90f),
                new PartStationSpec(PartId.PrimaryPositioningElement, new Vector3(23.2f, 0f, -10f), 90f),
                new PartStationSpec(PartId.PrimaryDamper, new Vector3(23.2f, 0f, -5f), 90f),
                new PartStationSpec(PartId.SecondaryElasticElement, new Vector3(23.2f, 0f, 0f), 90f),
                new PartStationSpec(PartId.HeightControlElement, new Vector3(23.2f, 0f, 5f), 90f),
                new PartStationSpec(PartId.SecondaryDamper, new Vector3(23.2f, 0f, 10f), 90f),
                new PartStationSpec(PartId.CentralTractionDevice, new Vector3(23.2f, 0f, 15f), 90f)
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
            }
        }

        private static void BuildModuleStations(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            Palette palette)
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var definitions = new[]
            {
                (Id: ModuleId.WheelsetAxlebox, Position: new Vector3(-13.5f, 0f, -10f), Yaw: 0f),
                (Id: ModuleId.Frame, Position: new Vector3(-4.5f, 0f, -10f), Yaw: 0f),
                (Id: ModuleId.PrimarySuspension, Position: new Vector3(4.5f, 0f, -10f), Yaw: 0f),
                (Id: ModuleId.SecondarySuspension, Position: new Vector3(13.5f, 0f, -10f), Yaw: 0f)
            };

            var stations = CreateChild(parent, "ModuleAssemblyStations");
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
                CreateWorldLabel(station.transform, "StationLabel", module.DisplayName + "装配台",
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
            Palette palette)
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var module = catalog.GetModule(ModuleId.BogieStructure);
            var station = new GameObject("CompositeStation_BogieStructure");
            station.transform.SetParent(parent, false);
            station.transform.localPosition = new Vector3(-7f, 0f, 1.5f);
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
                slot.transform.localPosition = new Vector3(-2f + index * 2f, 1.05f, 0f);
                slots[index] = slot.transform;
                visuals[index] = BuildFinalModuleVisual(
                    station.transform,
                    $"Installed_{children[index]}",
                    children[index],
                    ModuleMaterial(children[index], palette),
                    palette);
            }

            var completeBeacon = CreatePrimitive(PrimitiveType.Cylinder, station.transform,
                "BogieStructureCompleteBeacon", new Vector3(0f, 2.4f, 1.7f),
                new Vector3(0.18f, 0.58f, 0.18f), palette.Success);
            RemoveCollider(completeBeacon);
            completeBeacon.SetActive(false);
            CreateWorldLabel(station.transform, "StationLabel", "转向架构体装配台",
                new Vector3(0f, 3.15f, -2.1f), Quaternion.identity, palette.Running.color, 0.58f);

            var behaviour = station.AddComponent<CompositeAssemblyStation>();
            behaviour.Configure(
                sessionHost,
                ModuleId.BogieStructure,
                "转向架构体装配台",
                children,
                slots,
                visuals,
                completeBeacon,
                "转向架构体完成；准备二系悬挂、车体和中央牵引装置后进行落车");
        }

        private static void BuildFinalAssembly(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            Palette palette)
        {
            var station = new GameObject("LandingAssemblyStation");
            station.transform.SetParent(parent, false);
            station.transform.localPosition = new Vector3(7f, 0f, 1.5f);
            var collider = station.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.25f, 0f);
            collider.size = new Vector3(9f, 2.5f, 7f);
            collider.isTrigger = true;

            var lane = CreatePrimitive(PrimitiveType.Cube, station.transform, "AssemblyPlatform",
                new Vector3(0f, 0.18f, 0f), new Vector3(8.4f, 0.36f, 6.4f), palette.Steel);
            RemoveCollider(lane);
            foreach (var x in new[] { -1.45f, 1.45f })
            {
                CreatePrimitive(PrimitiveType.Cube, station.transform, $"Rail_{x}",
                    new Vector3(x, 0.42f, 0f), new Vector3(0.14f, 0.14f, 5.9f), palette.White);
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
                new Vector3(-2f, 1.55f, -1.2f),
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

            var completedLandingVisual = BuildLandingVehicleVisual(
                station.transform, "DroppedVehicle", palette);
            completedLandingVisual.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            completedLandingVisual.SetActive(false);
            CreateWorldLabel(station.transform, "FinalAssemblyLabel", "落车工位",
                new Vector3(0f, 3.8f, -3f), Quaternion.identity, palette.Safety.color, 0.62f);

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
        }

        private static void BuildCommissioningStations(
            Transform parent,
            WhiteboxGameSessionHost sessionHost,
            Palette palette)
        {
            var root = CreateChild(parent, "CommissioningLoopStations");
            var definitions = new[]
            {
                (Action: CommissioningAction.Test, Name: "调试判定", Position: new Vector3(-9f, 0f, 16f)),
                (Action: CommissioningAction.Retune, Name: "重新调试", Position: new Vector3(0f, 0f, 16f)),
                (Action: CommissioningAction.Inspect, Name: "检验", Position: new Vector3(9f, 0f, 16f))
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
            }

            CreateWorldLabel(root.transform, "LoopGuide", "失败 → 重新调试 → 检验 → 返回调试判定    成功 → 投入使用",
                new Vector3(0f, 4.7f, 19.9f), Quaternion.identity, palette.Safety.color, 0.58f);
        }

        private static GameObject BuildPartVisual(
            Transform parent,
            string name,
            PartId partId,
            Material material)
        {
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
            Palette palette)
        {
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
            CreateVisualCube(root.transform, "Carbody", new Vector3(0f, 1.35f, 0f),
                new Vector3(3.6f, 1.45f, 5.6f), palette.Carbody);
            var nose = CreatePrimitive(PrimitiveType.Sphere, root.transform, "CabNose",
                new Vector3(0f, 1.25f, -2.65f), new Vector3(3.1f, 1.2f, 1.35f), palette.Carbody);
            RemoveCollider(nose);
            foreach (var z in new[] { -1.65f, 1.65f })
            {
                CreateVisualCube(root.transform, $"Bogie_{z}", new Vector3(0f, 0.45f, z),
                    new Vector3(2.7f, 0.3f, 1.35f), palette.Running);
                foreach (var x in new[] { -1.3f, 1.3f })
                    CreateCylinder(root.transform, $"Wheel_{x}_{z}", new Vector3(x, 0.2f, z),
                        new Vector3(0.46f, 0.14f, 0.46f), Quaternion.Euler(0f, 0f, 90f), palette.Steel);
            }
            CreateVisualCube(root.transform, "WindowBand", new Vector3(0f, 1.55f, -2.88f),
                new Vector3(2.3f, 0.32f, 0.08f), palette.Steel);
            return root;
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
