using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    [DisallowMultipleComponent]
    public sealed class WhiteboxInternalDebugController : MonoBehaviour
    {
        public const string LaunchArgument = "-railcraft-internal-debug";
        public const string UnlockLogMarker = "RAILCRAFT_INTERNAL_DEBUG_UNLOCKED";
        public const string CollisionBoxLogMarker = "RAILCRAFT_INTERNAL_DEBUG_COLLISION_BOXES";

        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private WhiteboxSaveController saveController;
        [SerializeField] private ThirdPersonInputLock inputLock;

        private bool armed;
        private bool unlocked;
        private bool panelOwnsInputLock;
        private GameObject panel;
        private Text watermark;
        private Text statusText;
        private bool collisionBoxesVisible;
        private GameObject collisionLines;
        private Mesh collisionMesh;
        private Material collisionMaterial;
        private readonly List<Vector3> collisionVertices = new List<Vector3>();
        private readonly List<Color> collisionColors = new List<Color>();
        private readonly List<int> collisionIndices = new List<int>();
        private static readonly int[] BoxEdgeIndices =
        {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        };

        public bool IsArmed => armed;
        public bool IsUnlocked => unlocked;
        public bool IsPanelVisible => panel != null && panel.activeSelf;
        public bool AreCollisionBoxesVisible => collisionBoxesVisible;
        public int VisibleCollisionBoxCount { get; private set; }

        public void Configure(WhiteboxGameSessionHost host, WhiteboxSaveController save,
            ThirdPersonInputLock configuredInputLock)
        {
            sessionHost = host;
            saveController = save;
            inputLock = configuredInputLock;
        }

        public static bool IsLaunchAuthorized(string[] arguments)
        {
            return arguments != null && arguments.Any(argument =>
                string.Equals(argument, LaunchArgument, StringComparison.OrdinalIgnoreCase));
        }

        private void Awake()
        {
            armed = IsLaunchAuthorized(Environment.GetCommandLineArgs());
            enabled = armed;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var chordHeld = (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) &&
                (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed) &&
                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            if (!unlocked && chordHeld && keyboard.f10Key.wasPressedThisFrame)
            {
                Unlock();
                return;
            }

            if (unlocked && keyboard.f10Key.wasPressedThisFrame)
                SetPanelVisible(!IsPanelVisible);
        }

        private void LateUpdate()
        {
            if (collisionBoxesVisible)
                RefreshCollisionBoxes();
        }

        private void OnDisable()
        {
            collisionBoxesVisible = false;
            VisibleCollisionBoxCount = 0;
            if (collisionLines != null) collisionLines.SetActive(false);
        }

        private void OnDestroy()
        {
            DestroyDebugResource(collisionLines);
            DestroyDebugResource(collisionMesh);
            DestroyDebugResource(collisionMaterial);
        }

        public void UnlockForTests()
        {
            if (armed) Unlock();
        }

        public void ArmForTests()
        {
            armed = true;
            enabled = true;
        }

        public void PrepareAllMaterials()
        {
            RequireUnlocked();
            EnsureFreshSession();
            var catalog = WhiteboxGameCatalog.CreateDefault();
            foreach (var package in WhiteboxWorkPackageCatalog.Definitions.Where(item => item.IsMaterialPackage))
            {
                foreach (var questionId in package.CoreQuestionIds)
                {
                    var question = catalog.GetQuestion(questionId);
                    sessionHost.SubmitWorkPackageAnswer(package.Id, questionId, question.CorrectOptionIndex);
                }
                sessionHost.CollectWorkPackage(package.Id);
            }
            FinishAction("已解锁并领取全部 5 个材料包");
        }

        public void AssembleToLanding()
        {
            RequireUnlocked();
            PrepareAllMaterials();
            foreach (var package in WhiteboxWorkPackageCatalog.Definitions.Where(item =>
                item.IsMaterialPackage && item.Id != WorkPackageId.CarbodyAndLanding))
                sessionHost.InstallWorkPackage(package.AssemblyModule.Value, package.Id);

            sessionHost.InstallModule(ModuleId.BogieStructure, ModuleId.WheelsetAxlebox);
            sessionHost.InstallModule(ModuleId.BogieStructure, ModuleId.Frame);
            sessionHost.InstallModule(ModuleId.BogieStructure, ModuleId.PrimarySuspension);
            sessionHost.InstallModule(ModuleId.Landing, ModuleId.BogieStructure);
            sessionHost.InstallModule(ModuleId.Landing, ModuleId.SecondarySuspension);
            sessionHost.InstallWorkPackage(ModuleId.Landing, WorkPackageId.CarbodyAndLanding);
            FinishAction("已推进至落车完成，等待调试检验");
        }

        public void CompleteTraining()
        {
            RequireUnlocked();
            AssembleToLanding();
            sessionHost.RunCommissioning();
            sessionHost.PerformRetuning();
            sessionHost.PerformInspection();
            sessionHost.RunCommissioning();
            FinishAction("已完成装配、教学故障处理、检验和复测");
        }

        public void ResetTraining()
        {
            RequireUnlocked();
            sessionHost.ResetSession();
            saveController?.SaveCurrentSession();
            FinishAction("已重置当前内测进度");
        }

        public void ToggleCollisionBoxes()
        {
            RequireUnlocked();
            collisionBoxesVisible = !collisionBoxesVisible;
            if (collisionBoxesVisible)
                RefreshCollisionBoxes();
            else
            {
                VisibleCollisionBoxCount = 0;
                if (collisionLines != null) collisionLines.SetActive(false);
            }
            if (statusText != null)
                statusText.text = collisionBoxesVisible ? "碰撞箱已显示" : "碰撞箱已隐藏";
            Debug.Log(CollisionBoxLogMarker + " " + (collisionBoxesVisible ? "ON" : "OFF"));
        }

        private void Unlock()
        {
            unlocked = true;
            BuildUiIfNeeded();
            watermark.gameObject.SetActive(true);
            SetPanelVisible(true);
            Debug.Log(UnlockLogMarker);
        }

        private void BuildUiIfNeeded()
        {
            if (panel != null) return;
            var canvas = GetComponent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Internal debug controller requires the main Canvas.");

            watermark = CreateText(transform, "InternalDebugWatermark", "内部调试模式 · F10",
                18, TextAnchor.MiddleLeft, new Color(1f, 0.55f, 0.12f));
            SetRect(watermark.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(300f, 34f), new Vector2(0f, 1f));

            panel = new GameObject("InternalDebugPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(transform, false);
            var image = panel.GetComponent<Image>();
            image.color = new Color(0.03f, 0.045f, 0.06f, 0.97f);
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            SetRect((RectTransform)panel.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(380f, 530f), new Vector2(1f, 0.5f));

            CreateText(panel.transform, "Title", "内部调试工具", 26, TextAnchor.MiddleCenter, Color.white).gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
            CreateButton(panel.transform, "PrepareMaterials", "解锁并领取全部材料包", PrepareAllMaterials);
            CreateButton(panel.transform, "AssembleLanding", "推进至落车完成", AssembleToLanding);
            CreateButton(panel.transform, "CompleteTraining", "完成调试检验", CompleteTraining);
            CreateButton(panel.transform, "ResetTraining", "重置当前进度", ResetTraining);
            CreateButton(panel.transform, "ToggleCollisionBoxes", "显示/隐藏碰撞箱", ToggleCollisionBoxes);
            statusText = CreateText(panel.transform, "Status", "已授权。所有调试操作都会写入日志。", 16, TextAnchor.UpperLeft, new Color(0.75f, 0.86f, 0.92f));
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
        }

        private void EnsureFreshSession()
        {
            if (sessionHost == null) throw new InvalidOperationException("Session host is unavailable.");
            if (sessionHost.Session.IsVehicleComplete) sessionHost.ResetSession();
        }

        private void FinishAction(string message)
        {
            if (statusText != null) statusText.text = message;
            sessionHost.SetObjective("[内部调试] " + message);
            saveController?.SaveCurrentSession();
            Debug.Log("RAILCRAFT_INTERNAL_DEBUG_ACTION " + message);
        }

        private void SetPanelVisible(bool visible)
        {
            if (panel != null) panel.SetActive(visible);
            if (visible && inputLock != null && !inputLock.InputLocked)
            {
                inputLock.SetInputLocked(true);
                panelOwnsInputLock = true;
            }
            else if (!visible && panelOwnsInputLock)
            {
                inputLock?.SetInputLocked(false);
                panelOwnsInputLock = false;
            }
        }

        private void RequireUnlocked()
        {
            if (!unlocked) throw new InvalidOperationException("Internal debug mode has not been unlocked.");
        }

        public void RefreshCollisionBoxes()
        {
            if (!unlocked || !collisionBoxesVisible || !isActiveAndEnabled)
                return;

            BuildCollisionRendererIfNeeded();
            collisionVertices.Clear();
            collisionColors.Clear();
            collisionIndices.Clear();
            VisibleCollisionBoxCount = 0;
            foreach (var collider in FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (collider == null || !collider.enabled || collider.isTrigger ||
                    !collider.gameObject.activeInHierarchy)
                    continue;

                var bounds = collider.bounds;
                var min = bounds.min;
                var max = bounds.max;
                var first = collisionVertices.Count;
                collisionVertices.Add(new Vector3(min.x, min.y, min.z));
                collisionVertices.Add(new Vector3(max.x, min.y, min.z));
                collisionVertices.Add(new Vector3(max.x, min.y, max.z));
                collisionVertices.Add(new Vector3(min.x, min.y, max.z));
                collisionVertices.Add(new Vector3(min.x, max.y, min.z));
                collisionVertices.Add(new Vector3(max.x, max.y, min.z));
                collisionVertices.Add(new Vector3(max.x, max.y, max.z));
                collisionVertices.Add(new Vector3(min.x, max.y, max.z));
                var color = collider.attachedRigidbody != null ? Color.yellow : Color.cyan;
                for (var corner = 0; corner < 8; corner++) collisionColors.Add(color);
                foreach (var index in BoxEdgeIndices) collisionIndices.Add(first + index);
                VisibleCollisionBoxCount++;
            }

            collisionMesh.Clear();
            collisionMesh.SetVertices(collisionVertices);
            collisionMesh.SetColors(collisionColors);
            collisionMesh.SetIndices(collisionIndices, MeshTopology.Lines, 0, true);
            collisionLines.SetActive(VisibleCollisionBoxCount > 0);
        }

        private void BuildCollisionRendererIfNeeded()
        {
            if (collisionLines != null) return;

            // The Canvas already references this unlit shader in every Player build.
            // Cloning it avoids an editor-only Gizmo path or a stripped Shader.Find variant.
            collisionMaterial = new Material(Graphic.defaultGraphicMaterial)
            {
                name = "InternalDebugCollisionLines_Runtime",
                hideFlags = HideFlags.DontSave,
                renderQueue = (int)RenderQueue.Overlay
            };
            collisionMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
            collisionMesh = new Mesh
            {
                name = "InternalDebugCollisionBounds_Runtime",
                hideFlags = HideFlags.DontSave,
                indexFormat = IndexFormat.UInt32
            };
            collisionMesh.MarkDynamic();
            // Vertices are world coordinates. Keep this object at the scene root so the
            // screen-space Canvas transform cannot scale or offset the collision bounds.
            collisionLines = new GameObject("InternalDebugCollisionBoxes", typeof(MeshFilter), typeof(MeshRenderer))
            {
                hideFlags = HideFlags.DontSave
            };
            SceneManager.MoveGameObjectToScene(collisionLines, gameObject.scene);
            collisionLines.GetComponent<MeshFilter>().sharedMesh = collisionMesh;
            var renderer = collisionLines.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = collisionMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void DestroyDebugResource(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment, Color color)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(Text));
            owner.transform.SetParent(parent, false);
            var text = owner.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static void CreateButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction action)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            owner.transform.SetParent(parent, false);
            owner.GetComponent<Image>().color = new Color(0.08f, 0.34f, 0.5f, 1f);
            owner.GetComponent<Button>().onClick.AddListener(action);
            owner.GetComponent<LayoutElement>().preferredHeight = 54f;
            var text = CreateText(owner.transform, "Label", label, 18, TextAnchor.MiddleCenter, Color.white);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
