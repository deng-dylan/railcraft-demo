using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace RailCraft.ThirdPerson.FinalShowcase
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class FinalShowcaseRuntimeController : MonoBehaviour
    {
        public const int ExpectedCarCount = 8;

        private const string TrainDisplayName = "TrainDisplay";
        private const string CarSegmentsName = "CarSegments";
        private const string CarSegmentPrefix = "CarSegment_";
        private const string VisualRootName = "VisualRoot";
        private const string CameraCompositionName = "CameraComposition";
        private const string HeroCameraName = "HeroCamera";

        private static readonly CameraShotDefinition[] CameraShots =
        {
            new CameraShotDefinition(
                FinalShowcaseCameraPreset.Overview,
                "OverviewFocus",
                new Vector3(0f, 2.4f, 0f),
                new Vector3(-38f, 14.1f, -118f),
                46f),
            new CameraShotDefinition(
                FinalShowcaseCameraPreset.Head,
                "HeadCarFocus",
                new Vector3(0f, 2.3f, -91f),
                new Vector3(-10.5f, 4.2f, -18f),
                37f),
            new CameraShotDefinition(
                FinalShowcaseCameraPreset.Side,
                "SideDetailFocus",
                new Vector3(0f, 1.7f, -18f),
                new Vector3(-17f, 4.6f, 0f),
                35f),
            new CameraShotDefinition(
                FinalShowcaseCameraPreset.Departure,
                "DepartureFocus",
                new Vector3(0f, 2.2f, 88f),
                new Vector3(18f, 6.2f, 31f),
                42f)
        };

        [Header("Scene References")]
        [SerializeField] private UnityEngine.Camera controlledCamera;
        [SerializeField] private Transform trainDisplayRoot;
        [SerializeField] private Transform cameraCompositionRoot;
        [SerializeField] private bool autoBindOnEnable = true;

        [Header("Camera")]
        [SerializeField, Min(0.01f)] private float cameraPositionSharpness = 4.8f;
        [SerializeField, Min(0.01f)] private float cameraRotationSharpness = 6.5f;
        [SerializeField, Min(0.01f)] private float cameraFieldOfViewSharpness = 5.5f;
        [SerializeField] private bool sideCameraTracksSelectedCar = true;

        [Header("Exploded View")]
        [SerializeField, Min(0f)] private float explodeDuration = 1.35f;
        [SerializeField, Min(0f)] private float explodedLongitudinalGap = 3.2f;
        [SerializeField, Min(0f)] private float explodedLateralOffset = 0.55f;
        [SerializeField, Min(0f)] private float explodedVerticalLift = 0.18f;

        [Header("Selection")]
        [SerializeField] private bool showSelectionMarker = true;
        [SerializeField] private Color selectionColor = new Color(0.12f, 0.83f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float selectionLineWidth = 0.075f;
        [SerializeField, Min(0f)] private float selectionPadding = 0.2f;

        [Header("Input")]
        [SerializeField] private bool enableKeyboardShortcuts = true;

        private readonly FinalShowcasePresentationState state =
            new FinalShowcasePresentationState();
        private readonly List<CarSegmentBinding> carSegments =
            new List<CarSegmentBinding>(ExpectedCarCount);
        private readonly Vector3[] selectionBoxPositions = new Vector3[16];

        private GameObject selectionMarkerObject;
        private LineRenderer selectionLine;
        private Material selectionMaterial;
        private string lastLoggedBindingMessage = string.Empty;

        public event Action<FinalShowcaseHudState> HudStateChanged;
        public event Action<FinalShowcaseCameraPreset> CameraPresetChanged;
        public event Action<int> SelectedCarChanged;
        public event Action<bool> ExplodedChanged;

        public UnityEngine.Camera ControlledCamera => controlledCamera;
        public Transform TrainDisplayRoot => trainDisplayRoot;
        public Transform CameraCompositionRoot => cameraCompositionRoot;
        public FinalShowcaseCameraPreset CurrentCameraPreset => state.CameraPreset;
        public int SelectedCarIndex => state.SelectedCarIndex;
        public int SelectedCarNumber => state.SelectedCarIndex < 0 ? 0 : state.SelectedCarIndex + 1;
        public int CarCount => state.CarCount;
        public int ExplodableCarCount => carSegments.Count(item => item.MotionRoot != null);
        public bool IsExploded => state.ExplodedTarget;
        public bool IsExplosionAnimating => state.IsExplosionAnimating;
        public float ExplodeProgress => state.ExplodeProgress;
        public bool CanExplode => ExplodableCarCount > 0;
        public bool IsReady => controlledCamera != null && state.CarCount > 0;
        public string BindingMessage { get; private set; } = "尚未绑定展示场景";
        public string StatusText => HudState.StatusText;
        public string ShortcutHelp => FinalShowcaseHudState.ShortcutHelp;
        public FinalShowcaseHudState HudState => CreateHudState();

        public Transform SelectedCarTransform
        {
            get
            {
                var index = state.SelectedCarIndex;
                return index >= 0 && index < carSegments.Count
                    ? carSegments[index].MotionRoot ?? carSegments[index].Segment
                    : null;
            }
        }

        public void Configure(
            UnityEngine.Camera camera,
            Transform requestedTrainDisplayRoot,
            Transform requestedCameraCompositionRoot)
        {
            controlledCamera = camera;
            trainDisplayRoot = requestedTrainDisplayRoot;
            cameraCompositionRoot = requestedCameraCompositionRoot;
            RefreshBindings(true);
        }

        /// <summary>
        /// Resolves the stable FinalShowcase hierarchy by name. Missing branches
        /// are reported through BindingMessage and do not disable other controls.
        /// </summary>
        public bool TryBindScene()
        {
            var searchRoot = transform.root;
            if (trainDisplayRoot == null)
                trainDisplayRoot = FindInHierarchyOrScene(searchRoot, TrainDisplayName);
            if (cameraCompositionRoot == null)
                cameraCompositionRoot = FindInHierarchyOrScene(searchRoot, CameraCompositionName);
            if (controlledCamera == null && cameraCompositionRoot != null)
            {
                var hero = FindDescendantByName(cameraCompositionRoot, HeroCameraName);
                if (hero != null)
                    controlledCamera = hero.GetComponent<UnityEngine.Camera>();
            }
            if (controlledCamera == null)
                controlledCamera = UnityEngine.Camera.main;

            RefreshBindings(true);
            return IsReady;
        }

        public void RefreshBindings(bool snapToCurrentState = false)
        {
            RestoreBoundVisualRoots();
            carSegments.Clear();

            var notices = new List<string>();
            if (controlledCamera == null)
                notices.Add("未找到 HeroCamera，镜头切换已停用");
            if (cameraCompositionRoot == null)
                notices.Add("未找到 CameraComposition，将使用内置焦点坐标");

            var carSegmentsRoot = trainDisplayRoot == null
                ? null
                : FindDirectChild(trainDisplayRoot, CarSegmentsName);
            if (trainDisplayRoot == null)
            {
                notices.Add("未找到 TrainDisplay，车厢控制已停用");
            }
            else if (carSegmentsRoot == null)
            {
                notices.Add("未找到 TrainDisplay/CarSegments，车厢控制已停用");
            }
            else
            {
                var segmentTransforms = Enumerable.Range(0, carSegmentsRoot.childCount)
                    .Select(carSegmentsRoot.GetChild)
                    .Where(item => item.name.StartsWith(CarSegmentPrefix, StringComparison.Ordinal))
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .ToArray();
                foreach (var segment in segmentTransforms)
                {
                    var visualRoot = FindDirectChild(segment, VisualRootName) ??
                        FindDirectChildByPrefix(segment, VisualRootName);
                    var renderableVisualRoot = visualRoot != null &&
                        visualRoot.GetComponentsInChildren<Renderer>(true).Length > 0
                            ? visualRoot
                            : null;
                    carSegments.Add(new CarSegmentBinding(segment, renderableVisualRoot));
                }

                if (carSegments.Count != ExpectedCarCount)
                {
                    notices.Add(
                        $"检测到 {carSegments.Count} 节车，完整编组预期为 {ExpectedCarCount} 节");
                }

                var missingVisualRoots = carSegments.Count(item => item.MotionRoot == null);
                if (missingVisualRoots > 0)
                {
                    notices.Add(
                        $"{missingVisualRoots} 节车缺少可渲染 VisualRoot，对应分解动画将跳过");
                }
            }

            state.ConfigureCounts(CameraShots.Length, carSegments.Count);
            BindingMessage = notices.Count == 0
                ? "展示控制已就绪"
                : string.Join("；", notices);
            ApplyExplodeProgress();
            if (snapToCurrentState)
                SnapCameraToCurrentPreset();
            UpdateSelectionMarker();
            PublishHudState();

            if (Application.isPlaying && notices.Count > 0 &&
                !string.Equals(lastLoggedBindingMessage, BindingMessage, StringComparison.Ordinal))
            {
                Debug.LogWarning($"FinalShowcase：{BindingMessage}", this);
                lastLoggedBindingMessage = BindingMessage;
            }
        }

        public bool SetCameraPreset(
            FinalShowcaseCameraPreset preset,
            bool immediate = false)
        {
            var index = (int)preset;
            if (index < 0 || index >= CameraShots.Length)
                return false;

            var changed = state.SelectCamera(index);
            if (immediate)
                SnapCameraToCurrentPreset();
            if (changed)
                CameraPresetChanged?.Invoke(preset);
            PublishHudState();
            return controlledCamera != null;
        }

        public bool NextCamera()
        {
            return StepCamera(1);
        }

        public bool PreviousCamera()
        {
            return StepCamera(-1);
        }

        public void ShowOverview()
        {
            SetCameraPreset(FinalShowcaseCameraPreset.Overview);
        }

        public void ShowHead()
        {
            SetCameraPreset(FinalShowcaseCameraPreset.Head);
        }

        public void ShowSide()
        {
            SetCameraPreset(FinalShowcaseCameraPreset.Side);
        }

        public void ShowDeparture()
        {
            SetCameraPreset(FinalShowcaseCameraPreset.Departure);
        }

        public bool SelectCar(int zeroBasedIndex)
        {
            var changed = state.SelectCar(zeroBasedIndex);
            if (!changed && state.SelectedCarIndex != zeroBasedIndex)
                return false;

            UpdateSelectionMarker();
            if (changed)
                SelectedCarChanged?.Invoke(state.SelectedCarIndex);
            PublishHudState();
            return true;
        }

        public bool SelectCarNumber(int oneBasedNumber)
        {
            return SelectCar(oneBasedNumber - 1);
        }

        public bool NextCar()
        {
            return StepCar(1);
        }

        public bool PreviousCar()
        {
            return StepCar(-1);
        }

        public bool FocusSelectedCar()
        {
            return SetCameraPreset(FinalShowcaseCameraPreset.Side);
        }

        public bool SetExploded(bool exploded, bool immediate = false)
        {
            if (!CanExplode)
                return false;

            var changed = state.SetExploded(exploded);
            if (immediate)
            {
                state.AdvanceExplosion(float.MaxValue, 0f);
                ApplyExplodeProgress();
            }
            if (changed)
                ExplodedChanged?.Invoke(exploded);
            PublishHudState();
            return true;
        }

        public bool ToggleExploded()
        {
            if (!CanExplode)
                return false;

            state.ToggleExploded();
            ExplodedChanged?.Invoke(state.ExplodedTarget);
            PublishHudState();
            return true;
        }

        public void Explode()
        {
            SetExploded(true);
        }

        public void Restore()
        {
            SetExploded(false);
        }

        public void ResetPresentation(bool immediate = false)
        {
            var previousCamera = state.CameraPreset;
            var previousCar = state.SelectedCarIndex;
            var previousExploded = state.ExplodedTarget;
            state.Reset(immediate);
            if (immediate)
            {
                ApplyExplodeProgress();
                SnapCameraToCurrentPreset();
            }

            if (previousCamera != state.CameraPreset)
                CameraPresetChanged?.Invoke(state.CameraPreset);
            if (previousCar != state.SelectedCarIndex)
                SelectedCarChanged?.Invoke(state.SelectedCarIndex);
            if (previousExploded != state.ExplodedTarget)
                ExplodedChanged?.Invoke(false);
            UpdateSelectionMarker();
            PublishHudState();
        }

        public void SnapToCurrentState()
        {
            state.AdvanceExplosion(float.MaxValue, 0f);
            ApplyExplodeProgress();
            SnapCameraToCurrentPreset();
            UpdateSelectionMarker();
            PublishHudState();
        }

        private void Awake()
        {
            state.ConfigureCounts(CameraShots.Length, 0);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            if (autoBindOnEnable)
                TryBindScene();
            else
                RefreshBindings(true);
            SetSelectionMarkerActive(showSelectionMarker);
        }

        private void OnDisable()
        {
            SetSelectionMarkerActive(false);
        }

        private void OnDestroy()
        {
            DestroyRuntimeObject(selectionMaterial);
            selectionMaterial = null;
            selectionLine = null;
            selectionMarkerObject = null;
        }

        private void OnValidate()
        {
            cameraPositionSharpness = Mathf.Max(0.01f, cameraPositionSharpness);
            cameraRotationSharpness = Mathf.Max(0.01f, cameraRotationSharpness);
            cameraFieldOfViewSharpness = Mathf.Max(0.01f, cameraFieldOfViewSharpness);
            explodeDuration = Mathf.Max(0f, explodeDuration);
            explodedLongitudinalGap = Mathf.Max(0f, explodedLongitudinalGap);
            explodedLateralOffset = Mathf.Max(0f, explodedLateralOffset);
            explodedVerticalLift = Mathf.Max(0f, explodedVerticalLift);
            selectionLineWidth = Mathf.Max(0.01f, selectionLineWidth);
            selectionPadding = Mathf.Max(0f, selectionPadding);
            if (selectionLine != null)
            {
                selectionLine.startWidth = selectionLineWidth;
                selectionLine.endWidth = selectionLineWidth;
                selectionLine.startColor = selectionColor;
                selectionLine.endColor = selectionColor;
            }
        }

        private void Update()
        {
            if (enableKeyboardShortcuts)
                HandleKeyboard();

            if (state.AdvanceExplosion(Time.deltaTime, explodeDuration))
            {
                ApplyExplodeProgress();
                PublishHudState();
            }

            UpdateCamera(Time.deltaTime);
        }

        private void LateUpdate()
        {
            UpdateSelectionMarker();
        }

        private bool StepCamera(int delta)
        {
            if (!state.StepCamera(delta))
                return false;

            CameraPresetChanged?.Invoke(state.CameraPreset);
            PublishHudState();
            return true;
        }

        private bool StepCar(int delta)
        {
            if (!state.StepCar(delta))
                return false;

            UpdateSelectionMarker();
            SelectedCarChanged?.Invoke(state.SelectedCarIndex);
            PublishHudState();
            return true;
        }

        private void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f1Key.wasPressedThisFrame)
                SetCameraPreset(FinalShowcaseCameraPreset.Overview);
            else if (keyboard.f2Key.wasPressedThisFrame)
                SetCameraPreset(FinalShowcaseCameraPreset.Head);
            else if (keyboard.f3Key.wasPressedThisFrame)
                SetCameraPreset(FinalShowcaseCameraPreset.Side);
            else if (keyboard.f4Key.wasPressedThisFrame)
                SetCameraPreset(FinalShowcaseCameraPreset.Departure);
            else if (keyboard.tabKey.wasPressedThisFrame)
                NextCamera();

            if (keyboard.digit1Key.wasPressedThisFrame)
                SelectCarNumber(1);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                SelectCarNumber(2);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                SelectCarNumber(3);
            else if (keyboard.digit4Key.wasPressedThisFrame)
                SelectCarNumber(4);
            else if (keyboard.digit5Key.wasPressedThisFrame)
                SelectCarNumber(5);
            else if (keyboard.digit6Key.wasPressedThisFrame)
                SelectCarNumber(6);
            else if (keyboard.digit7Key.wasPressedThisFrame)
                SelectCarNumber(7);
            else if (keyboard.digit8Key.wasPressedThisFrame)
                SelectCarNumber(8);

            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)
                PreviousCar();
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
                NextCar();

            if (keyboard.xKey.wasPressedThisFrame)
                ToggleExploded();
            if (keyboard.rKey.wasPressedThisFrame)
                ResetPresentation();
        }

        private void UpdateCamera(float deltaTime)
        {
            if (controlledCamera == null || state.CameraIndex < 0)
                return;

            var pose = ResolveCameraPose(CameraShots[state.CameraIndex]);
            var positionBlend = FinalShowcasePresentationState.CalculateExponentialBlend(
                cameraPositionSharpness,
                deltaTime);
            var rotationBlend = FinalShowcasePresentationState.CalculateExponentialBlend(
                cameraRotationSharpness,
                deltaTime);
            var fieldOfViewBlend = FinalShowcasePresentationState.CalculateExponentialBlend(
                cameraFieldOfViewSharpness,
                deltaTime);
            controlledCamera.transform.position = Vector3.Lerp(
                controlledCamera.transform.position,
                pose.Position,
                positionBlend);
            controlledCamera.transform.rotation = Quaternion.Slerp(
                controlledCamera.transform.rotation,
                pose.Rotation,
                rotationBlend);
            controlledCamera.fieldOfView = Mathf.Lerp(
                controlledCamera.fieldOfView,
                pose.FieldOfView,
                fieldOfViewBlend);
        }

        private void SnapCameraToCurrentPreset()
        {
            if (controlledCamera == null || state.CameraIndex < 0)
                return;

            var pose = ResolveCameraPose(CameraShots[state.CameraIndex]);
            controlledCamera.transform.SetPositionAndRotation(pose.Position, pose.Rotation);
            controlledCamera.fieldOfView = pose.FieldOfView;
        }

        private CameraPose ResolveCameraPose(CameraShotDefinition shot)
        {
            var focus = ResolveCameraFocus(shot);
            var position = focus + shot.CameraOffset;
            var lookDirection = focus - position;
            var rotation = lookDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
                : Quaternion.identity;
            return new CameraPose(position, rotation, shot.FieldOfView);
        }

        private Vector3 ResolveCameraFocus(CameraShotDefinition shot)
        {
            if (shot.Preset == FinalShowcaseCameraPreset.Side &&
                sideCameraTracksSelectedCar && SelectedCarTransform != null)
            {
                return SelectedCarTransform.position + Vector3.up * 1.7f;
            }

            if (cameraCompositionRoot != null)
            {
                var anchor = FindDescendantByName(cameraCompositionRoot, shot.FocusName);
                if (anchor != null)
                    return anchor.position;
                return cameraCompositionRoot.TransformPoint(shot.FallbackFocus);
            }

            return shot.FallbackFocus;
        }

        private void ApplyExplodeProgress()
        {
            var smoothProgress = FinalShowcasePresentationState.SmoothProgress(
                state.ExplodeProgress);
            for (var index = 0; index < carSegments.Count; index++)
            {
                var binding = carSegments[index];
                if (binding.MotionRoot == null)
                    continue;

                var offset = FinalShowcasePresentationState.CalculateExplodedOffset(
                    index,
                    carSegments.Count,
                    explodedLongitudinalGap,
                    explodedLateralOffset,
                    explodedVerticalLift);
                binding.MotionRoot.localPosition =
                    binding.BaseLocalPosition + offset * smoothProgress;
            }
        }

        private void RestoreBoundVisualRoots()
        {
            foreach (var binding in carSegments)
            {
                if (binding.MotionRoot != null)
                    binding.MotionRoot.localPosition = binding.BaseLocalPosition;
            }
        }

        private void UpdateSelectionMarker()
        {
            if (!Application.isPlaying || !showSelectionMarker ||
                state.SelectedCarIndex < 0 || state.SelectedCarIndex >= carSegments.Count)
            {
                SetSelectionMarkerActive(false);
                return;
            }

            EnsureSelectionMarker();
            if (selectionLine == null)
                return;

            var binding = carSegments[state.SelectedCarIndex];
            var bounds = CalculateSelectionBounds(binding);
            bounds.Expand(selectionPadding * 2f);
            SetBoxPositions(selectionLine, bounds, selectionBoxPositions);
            SetSelectionMarkerActive(true);
        }

        private Bounds CalculateSelectionBounds(CarSegmentBinding binding)
        {
            var hasBounds = false;
            var bounds = default(Bounds);
            foreach (var renderer in binding.Renderers)
            {
                if (renderer == null || renderer.bounds.size.sqrMagnitude < 0.000001f)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (hasBounds)
                return bounds;

            var segmentLength = CalculateFallbackSegmentLength(binding.Segment);
            var fallbackRoot = binding.MotionRoot == null ? binding.Segment : binding.MotionRoot;
            var fallbackPosition = fallbackRoot == null ? transform.position : fallbackRoot.position;
            return new Bounds(
                fallbackPosition + Vector3.up * 2f,
                new Vector3(3.8f, 4.2f, segmentLength));
        }

        private static float CalculateFallbackSegmentLength(Transform segment)
        {
            if (segment == null)
                return 22f;

            var negativeEnd = FindDirectChild(segment, "SourceMinusX_End");
            var positiveEnd = FindDirectChild(segment, "SourcePlusX_End");
            if (negativeEnd == null || positiveEnd == null)
                return 22f;

            return Mathf.Max(1f, Vector3.Distance(negativeEnd.position, positiveEnd.position));
        }

        private void EnsureSelectionMarker()
        {
            if (selectionLine != null)
                return;

            selectionMarkerObject = new GameObject("FinalShowcase_SelectedCarMarker");
            selectionMarkerObject.hideFlags = HideFlags.DontSave;
            selectionMarkerObject.transform.SetParent(transform, false);
            selectionLine = selectionMarkerObject.AddComponent<LineRenderer>();
            selectionLine.useWorldSpace = true;
            selectionLine.loop = false;
            selectionLine.positionCount = 16;
            selectionLine.startWidth = selectionLineWidth;
            selectionLine.endWidth = selectionLineWidth;
            selectionLine.startColor = selectionColor;
            selectionLine.endColor = selectionColor;
            selectionLine.alignment = LineAlignment.View;
            selectionLine.textureMode = LineTextureMode.Stretch;
            selectionLine.shadowCastingMode = ShadowCastingMode.Off;
            selectionLine.receiveShadows = false;
            selectionLine.sortingOrder = 100;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Unlit/Color");
            if (shader == null)
                return;

            selectionMaterial = new Material(shader)
            {
                name = "FinalShowcaseSelection_Runtime",
                hideFlags = HideFlags.DontSave
            };
            if (selectionMaterial.HasProperty("_BaseColor"))
                selectionMaterial.SetColor("_BaseColor", selectionColor);
            if (selectionMaterial.HasProperty("_Color"))
                selectionMaterial.SetColor("_Color", selectionColor);
            selectionLine.sharedMaterial = selectionMaterial;
        }

        private void SetSelectionMarkerActive(bool active)
        {
            if (selectionMarkerObject != null && selectionMarkerObject.activeSelf != active)
                selectionMarkerObject.SetActive(active);
        }

        private static void SetBoxPositions(
            LineRenderer line,
            Bounds bounds,
            Vector3[] positions)
        {
            var min = bounds.min;
            var max = bounds.max;
            var p000 = new Vector3(min.x, min.y, min.z);
            var p100 = new Vector3(max.x, min.y, min.z);
            var p101 = new Vector3(max.x, min.y, max.z);
            var p001 = new Vector3(min.x, min.y, max.z);
            var p010 = new Vector3(min.x, max.y, min.z);
            var p110 = new Vector3(max.x, max.y, min.z);
            var p111 = new Vector3(max.x, max.y, max.z);
            var p011 = new Vector3(min.x, max.y, max.z);
            positions[0] = p000;
            positions[1] = p100;
            positions[2] = p101;
            positions[3] = p001;
            positions[4] = p000;
            positions[5] = p010;
            positions[6] = p110;
            positions[7] = p100;
            positions[8] = p110;
            positions[9] = p111;
            positions[10] = p101;
            positions[11] = p111;
            positions[12] = p011;
            positions[13] = p001;
            positions[14] = p011;
            positions[15] = p010;
            line.SetPositions(positions);
        }

        private FinalShowcaseHudState CreateHudState()
        {
            return new FinalShowcaseHudState(
                state.CameraPreset,
                state.SelectedCarIndex,
                state.CarCount,
                ExplodableCarCount,
                state.ExplodedTarget,
                state.ExplodeProgress,
                IsReady,
                BindingMessage);
        }

        private void PublishHudState()
        {
            HudStateChanged?.Invoke(CreateHudState());
        }

        private static Transform FindInHierarchyOrScene(Transform preferredRoot, string name)
        {
            var localMatch = FindDescendantByName(preferredRoot, name);
            if (localMatch != null)
                return localMatch;

            var scene = preferredRoot == null ? default : preferredRoot.gameObject.scene;
            if (!scene.IsValid())
                return null;

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                var match = FindDescendantByName(rootObject.transform, name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null)
                return null;
            if (string.Equals(root.name, name, StringComparison.Ordinal))
                return root;

            for (var index = 0; index < root.childCount; index++)
            {
                var match = FindDescendantByName(root.GetChild(index), name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
                return null;

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                    return child;
            }

            return null;
        }

        private static Transform FindDirectChildByPrefix(Transform parent, string prefix)
        {
            if (parent == null)
                return null;

            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                    return child;
            }

            return null;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private sealed class CarSegmentBinding
        {
            public CarSegmentBinding(Transform segment, Transform motionRoot)
            {
                Segment = segment;
                MotionRoot = motionRoot;
                BaseLocalPosition = motionRoot == null ? Vector3.zero : motionRoot.localPosition;
                var renderRoot = motionRoot == null ? segment : motionRoot;
                Renderers = renderRoot == null
                    ? Array.Empty<Renderer>()
                    : renderRoot.GetComponentsInChildren<Renderer>(true);
            }

            public Transform Segment { get; }
            public Transform MotionRoot { get; }
            public Vector3 BaseLocalPosition { get; }
            public Renderer[] Renderers { get; }
        }

        private readonly struct CameraShotDefinition
        {
            public CameraShotDefinition(
                FinalShowcaseCameraPreset preset,
                string focusName,
                Vector3 fallbackFocus,
                Vector3 cameraOffset,
                float fieldOfView)
            {
                Preset = preset;
                FocusName = focusName;
                FallbackFocus = fallbackFocus;
                CameraOffset = cameraOffset;
                FieldOfView = fieldOfView;
            }

            public FinalShowcaseCameraPreset Preset { get; }
            public string FocusName { get; }
            public Vector3 FallbackFocus { get; }
            public Vector3 CameraOffset { get; }
            public float FieldOfView { get; }
        }

        private readonly struct CameraPose
        {
            public CameraPose(Vector3 position, Quaternion rotation, float fieldOfView)
            {
                Position = position;
                Rotation = rotation;
                FieldOfView = fieldOfView;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public float FieldOfView { get; }
        }
    }
}
