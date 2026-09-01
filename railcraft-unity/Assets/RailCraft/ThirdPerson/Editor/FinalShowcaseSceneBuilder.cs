using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailCraft.ThirdPerson.FinalShowcase;
using RailCraft.ThirdPerson.UI;
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
    /// <summary>
    /// Generates an optional, stand-alone full-train presentation scene.
    /// This builder deliberately leaves EditorBuildSettings untouched so the
    /// third-person whitebox remains the only default build scene.
    /// </summary>
    public static class FinalShowcaseSceneBuilder
    {
        public const string ScenePath =
            "Assets/RailCraft/ThirdPerson/Scenes/FinalShowcase.unity";

        public const string ModelAssetPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/FinalShowcase/FuxingTrain.fbx";

        public const string PreferredSourceNodeName = "空白_2";
        public const float TargetTrainLengthMetres = 200f;
        public const float TrackLengthMetres = 280f;
        public const float StandardGaugeMetres = 1.435f;
        public const float RailTopY = 0.32f;
        public const int CarSegmentCount = 8;
        public const int InspectedHighDetailRendererCount = 99;
        public const int Lod1ProxyRenderersPerCar = 5;
        public const int Lod2ProxyRenderersPerCar = 1;

        // Each car owns its own LODGroup. Close head and side shots retain LOD0,
        // while cars farther along the 200 m consist can independently fall
        // back during overview and departure compositions.
        public const float Lod0ScreenRelativeTransitionHeight = 0.70f;
        public const float Lod1ScreenRelativeTransitionHeight = 0.38f;
        public const float Lod2ScreenRelativeTransitionHeight = 0.08f;

        // The inspected interface panels sit exactly on a car boundary. Unity
        // recomputes renderer bounds using floats, so retain a small world-space
        // tie tolerance and assign an interface to the source -X / positive-Z car.
        private const float CarBoundaryTieToleranceMetres = 0.05f;

        private const float InspectedSourceMinX = -1.02546479584f;
        private const float InspectedSourceMaxX = 1.27592720416f;

        private static readonly float[] InspectedSourceCarBoundariesX =
        {
            InspectedSourceMinX,
            -0.760061795841f,
            -0.465022795841f,
            -0.169913795841f,
            0.125195704159f,
            0.420305204159f,
            0.715414704159f,
            1.01052370416f,
            InspectedSourceMaxX
        };

        private const string RootPath = "Assets/RailCraft/ThirdPerson";
        private const string MaterialPath = RootPath + "/Art/Materials/FinalShowcase";

        private sealed class Palette
        {
            public Material Asphalt;
            public Material Ballast;
            public Material Concrete;
            public Material DarkSteel;
            public Material Rail;
            public Material SafetyYellow;
            public Material SignalBlue;
            public Material White;
            public Material TrainRed;
            public Material Glass;
            public Material Emissive;
        }

        private sealed class CarSegmentBuildData
        {
            public GameObject Segment;
            public Transform VisualRoot;
            public readonly List<Renderer> HighDetailRenderers = new List<Renderer>();
            public Bounds HighDetailBounds;
            public bool HasBounds;
        }

        private sealed class OverlayBindings
        {
            public Button ReturnButton;
            public Text RuntimeStatus;
            public Text ShortcutHelp;
        }

        private readonly struct RendererAssignment
        {
            public RendererAssignment(
                Renderer renderer,
                int segmentIndex,
                Matrix4x4 originalWorldMatrix)
            {
                Renderer = renderer;
                SegmentIndex = segmentIndex;
                OriginalWorldMatrix = originalWorldMatrix;
            }

            public Renderer Renderer { get; }
            public int SegmentIndex { get; }
            public Matrix4x4 OriginalWorldMatrix { get; }
        }

        [MenuItem("RailCraft/Final Showcase/Rebuild Scene")]
        public static void RebuildFromMenu()
        {
            Build();
        }

        [MenuItem("RailCraft/Final Showcase/Validate Train Model")]
        public static void ValidateTrainModelFromMenu()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (model == null)
                throw new FileNotFoundException(GetMissingModelMessage(), ModelAssetPath);

            var message =
                $"已找到复兴号模型：\n{ModelAssetPath}\n\n" +
                "重新生成场景时会优先提取节点“空白_2”，自动让最长水平轴沿 Unity Z 轴，" +
                $"并统一缩放到约 {TargetTrainLengthMetres:0} m。";
            EditorUtility.DisplayDialog("Final Showcase 模型检查", message, "确定");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        /// <summary>
        /// Rebuilds the showcase and returns true when the FBX was used. When the
        /// file is absent or contains no renderable meshes, a labelled eight-car
        /// placeholder is generated so layout and camera work can continue.
        /// </summary>
        public static bool Build()
        {
            EnsureFolders();
            var palette = BuildPalette();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "FinalShowcase";

            var root = new GameObject("FinalShowcaseRoot");
            BuildEnvironment(CreateChild(root.transform, "Environment"), palette);
            BuildStationArchitecture(CreateChild(root.transform, "DeparturePlatform"), palette);

            var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            var trainDisplay = CreateChild(root.transform, "TrainDisplay");
            var usedImportedModel = TryBuildImportedTrain(
                trainDisplay.transform,
                sourceModel,
                scene,
                palette,
                out var normalizedTrainRoot);
            if (!usedImportedModel)
            {
                BuildPlaceholderTrain(trainDisplay.transform, palette);
                BuildCarSegmentAnchors(trainDisplay.transform);
            }
            else
            {
                BuildImportedCarSegmentsAndLod(
                    trainDisplay.transform,
                    normalizedTrainRoot,
                    palette);
            }

            BuildLighting(CreateChild(root.transform, "Lighting"), palette);
            var cameraComposition = CreateChild(root.transform, "CameraComposition");
            var heroCamera = BuildCameraComposition(cameraComposition);
            var overlay = BuildOverlay(
                CreateChild(root.transform, "Interface"),
                usedImportedModel);

            var presentation = root.AddComponent<FinalShowcaseRuntimeController>();
            presentation.Configure(
                heroCamera,
                trainDisplay.transform,
                cameraComposition.transform);
            var presentationHud = root.AddComponent<FinalShowcaseHudPresenter>();
            presentationHud.Configure(
                presentation,
                overlay.RuntimeStatus,
                overlay.ShortcutHelp);

            var returnController = root.AddComponent<FinalShowcaseReturnController>();
            returnController.Configure(overlay.ReturnButton);
            CreateChild(root.transform, "Contract__InteractivePresentationConfigured");
            CreateChild(root.transform, "Contract__ReturnToFactoryConfigured");
            ConfigureRenderSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"无法保存 FinalShowcase 场景：{ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (usedImportedModel)
            {
                Debug.Log(
                    $"RAILCRAFT_FINAL_SHOWCASE_BUILT path={ScenePath} model={ModelAssetPath}");
            }
            else
            {
                Debug.LogWarning(GetMissingModelMessage() +
                    $"\n场景已用八节编组占位模型生成：{ScenePath}");
            }

            return usedImportedModel;
        }

        public static string GetMissingModelMessage()
        {
            return
                "FinalShowcase 尚未找到可用的复兴号 FBX。\n" +
                $"请将模型放到固定路径：{ModelAssetPath}\n" +
                "随后执行菜单：RailCraft > Final Showcase > Rebuild Scene。";
        }

        public static float CalculateUniformScale(float sourceLength, float targetLength)
        {
            if (sourceLength <= 0f || float.IsNaN(sourceLength) || float.IsInfinity(sourceLength))
                throw new ArgumentOutOfRangeException(nameof(sourceLength));
            if (targetLength <= 0f || float.IsNaN(targetLength) || float.IsInfinity(targetLength))
                throw new ArgumentOutOfRangeException(nameof(targetLength));
            return targetLength / sourceLength;
        }

        public static bool ShouldRotateLengthFromX(Vector3 sourceSize)
        {
            return sourceSize.x > sourceSize.z;
        }

        public static IReadOnlyList<float> CalculateSymmetricPositions(
            float halfExtent,
            float spacing)
        {
            if (halfExtent < 0f || float.IsNaN(halfExtent) || float.IsInfinity(halfExtent))
                throw new ArgumentOutOfRangeException(nameof(halfExtent));
            if (spacing <= 0f || float.IsNaN(spacing) || float.IsInfinity(spacing))
                throw new ArgumentOutOfRangeException(nameof(spacing));

            var stepCount = Mathf.FloorToInt(halfExtent / spacing);
            var values = new float[stepCount * 2 + 1];
            for (var index = -stepCount; index <= stepCount; index++)
                values[index + stepCount] = index * spacing;
            return values;
        }

        /// <summary>
        /// Converts the seven inspected FBX car interfaces to the normalized
        /// showcase Z axis. Source car 01 starts at the positive-Z end because a
        /// +90 degree Y rotation maps source +X to Unity -Z.
        /// </summary>
        public static IReadOnlyList<float> CalculateTargetCarBoundaryPositions()
        {
            var values = new float[InspectedSourceCarBoundariesX.Length];
            for (var index = 0; index < values.Length; index++)
                values[index] = ConvertInspectedSourceXToTargetZ(
                    InspectedSourceCarBoundariesX[index]);
            return values;
        }

        /// <summary>
        /// Applies the same center, scale and +90 degree Y-axis normalization as
        /// the imported train placement, without requiring an instantiated FBX.
        /// </summary>
        public static float ConvertInspectedSourceXToTargetZ(float sourceX)
        {
            if (float.IsNaN(sourceX) || float.IsInfinity(sourceX))
                throw new ArgumentOutOfRangeException(nameof(sourceX));

            var sourceCenter = (InspectedSourceMinX + InspectedSourceMaxX) * 0.5f;
            var sourceLength = InspectedSourceMaxX - InspectedSourceMinX;
            var scale = CalculateUniformScale(sourceLength, TargetTrainLengthMetres);
            return -(sourceX - sourceCenter) * scale;
        }

        /// <summary>
        /// Resolves a renderer world-bounds center into one of the eight
        /// positive-Z-to-negative-Z car intervals. Values outside the inspected
        /// train bounds are clamped to the nearest end car. At a shared interface
        /// the positive-Z segment wins, matching the inspected grouping manifest.
        /// </summary>
        public static int ResolveCarSegmentIndex(
            float worldCenterZ,
            IReadOnlyList<float> descendingBoundaries)
        {
            if (float.IsNaN(worldCenterZ) || float.IsInfinity(worldCenterZ))
                throw new ArgumentOutOfRangeException(nameof(worldCenterZ));
            ValidateCarBoundaries(descendingBoundaries);

            for (var index = 0; index < descendingBoundaries.Count - 1; index++)
            {
                if (worldCenterZ >=
                    descendingBoundaries[index + 1] - CarBoundaryTieToleranceMetres)
                {
                    return index;
                }
            }

            return descendingBoundaries.Count - 2;
        }

        public static IReadOnlyList<int> CountCarSegmentAssignments(
            IReadOnlyList<float> worldCentersZ,
            IReadOnlyList<float> descendingBoundaries)
        {
            if (worldCentersZ == null)
                throw new ArgumentNullException(nameof(worldCentersZ));
            ValidateCarBoundaries(descendingBoundaries);

            var counts = new int[descendingBoundaries.Count - 1];
            for (var index = 0; index < worldCentersZ.Count; index++)
                counts[ResolveCarSegmentIndex(worldCentersZ[index], descendingBoundaries)]++;
            return counts;
        }

        public static IReadOnlyList<float> GetPerCarLodTransitionHeights()
        {
            return new[]
            {
                Lod0ScreenRelativeTransitionHeight,
                Lod1ScreenRelativeTransitionHeight,
                Lod2ScreenRelativeTransitionHeight
            };
        }

        private static void ValidateCarBoundaries(IReadOnlyList<float> descendingBoundaries)
        {
            if (descendingBoundaries == null)
                throw new ArgumentNullException(nameof(descendingBoundaries));
            if (descendingBoundaries.Count != CarSegmentCount + 1)
            {
                throw new ArgumentException(
                    $"Expected {CarSegmentCount + 1} car boundaries.",
                    nameof(descendingBoundaries));
            }

            for (var index = 0; index < descendingBoundaries.Count; index++)
            {
                var value = descendingBoundaries[index];
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new ArgumentOutOfRangeException(nameof(descendingBoundaries));
                if (index > 0 && descendingBoundaries[index - 1] <= value)
                {
                    throw new ArgumentException(
                        "Car boundaries must be strictly descending.",
                        nameof(descendingBoundaries));
                }
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/RailCraft", "ThirdPerson");
            EnsureFolder(RootPath, "Scenes");
            EnsureFolder(RootPath, "Art");
            EnsureFolder(RootPath + "/Art", "Materials");
            EnsureFolder(RootPath + "/Art/Materials", "FinalShowcase");
            EnsureFolder(RootPath + "/Art", "Models");
            EnsureFolder(RootPath + "/Art/Models", "FinalShowcase");
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
                Asphalt = CreateOrUpdateMaterial(
                    "FS_Asphalt", new Color(0.055f, 0.065f, 0.075f), 0.16f, 0f),
                Ballast = CreateOrUpdateMaterial(
                    "FS_Ballast", new Color(0.19f, 0.20f, 0.21f), 0.12f, 0f),
                Concrete = CreateOrUpdateMaterial(
                    "FS_Concrete", new Color(0.38f, 0.42f, 0.45f), 0.25f, 0f),
                DarkSteel = CreateOrUpdateMaterial(
                    "FS_DarkSteel", new Color(0.065f, 0.085f, 0.10f), 0.52f, 0.55f),
                Rail = CreateOrUpdateMaterial(
                    "FS_Rail", new Color(0.25f, 0.28f, 0.30f), 0.78f, 0.8f),
                SafetyYellow = CreateOrUpdateMaterial(
                    "FS_SafetyYellow", new Color(1f, 0.62f, 0.04f), 0.35f, 0f),
                SignalBlue = CreateOrUpdateMaterial(
                    "FS_SignalBlue", new Color(0.025f, 0.24f, 0.43f), 0.5f, 0.15f),
                White = CreateOrUpdateMaterial(
                    "FS_TrainWhite", new Color(0.88f, 0.92f, 0.94f), 0.62f, 0.12f),
                TrainRed = CreateOrUpdateMaterial(
                    "FS_TrainRed", new Color(0.72f, 0.035f, 0.045f), 0.55f, 0.05f),
                Glass = CreateOrUpdateMaterial(
                    "FS_Glass", new Color(0.025f, 0.085f, 0.13f), 0.88f, 0.22f),
                Emissive = CreateOrUpdateMaterial(
                    "FS_Emissive", new Color(0.83f, 0.94f, 1f), 0.72f, 0f, true)
            };
        }

        private static Material CreateOrUpdateMaterial(
            string name,
            Color color,
            float smoothness,
            float metallic,
            bool emission = false)
        {
            var path = $"{MaterialPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("找不到 URP Lit 或 Standard Shader。");

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildEnvironment(GameObject environment, Palette palette)
        {
            var ground = CreatePrimitive(
                PrimitiveType.Cube,
                environment.transform,
                "Ground",
                new Vector3(0f, -0.36f, 0f),
                new Vector3(64f, 0.6f, 340f),
                palette.Asphalt,
                false);
            MarkStatic(ground);

            var track = CreateChild(environment.transform, "DepartureTrack");
            var ballast = CreatePrimitive(
                PrimitiveType.Cube,
                track.transform,
                "BallastBed",
                new Vector3(0f, -0.02f, 0f),
                new Vector3(4.8f, 0.24f, TrackLengthMetres),
                palette.Ballast,
                false);
            MarkStatic(ballast);

            foreach (var z in CalculateSymmetricPositions(TrackLengthMetres * 0.5f - 1f, 2f))
            {
                var sleeper = CreatePrimitive(
                    PrimitiveType.Cube,
                    track.transform,
                    $"Sleeper_{z:+000.0;-000.0;000.0}",
                    new Vector3(0f, 0.11f, z),
                    new Vector3(2.65f, 0.15f, 0.28f),
                    palette.Concrete,
                    false);
                MarkStatic(sleeper);
            }

            var halfGauge = StandardGaugeMetres * 0.5f;
            foreach (var x in new[] { -halfGauge, halfGauge })
            {
                var rail = CreatePrimitive(
                    PrimitiveType.Cube,
                    track.transform,
                    x < 0f ? "Rail_Left" : "Rail_Right",
                    new Vector3(x, 0.25f, 0f),
                    new Vector3(0.085f, 0.14f, TrackLengthMetres),
                    palette.Rail,
                    false);
                MarkStatic(rail);
            }

            BuildCatenary(track.transform, palette);
        }

        private static void BuildCatenary(Transform track, Palette palette)
        {
            var catenary = CreateChild(track, "Catenary");
            foreach (var z in CalculateSymmetricPositions(TrackLengthMetres * 0.5f - 10f, 25f))
            {
                foreach (var x in new[] { -4.4f, 4.4f })
                {
                    CreatePrimitive(
                        PrimitiveType.Cube,
                        catenary.transform,
                        $"Mast_{x:+0.0;-0.0}_{z:+000;-000;000}",
                        new Vector3(x, 4.4f, z),
                        new Vector3(0.18f, 8.7f, 0.18f),
                        palette.DarkSteel,
                        false);
                }

                CreatePrimitive(
                    PrimitiveType.Cube,
                    catenary.transform,
                    $"Crossbeam_{z:+000;-000;000}",
                    new Vector3(0f, 8.25f, z),
                    new Vector3(9f, 0.16f, 0.16f),
                    palette.DarkSteel,
                    false);
            }

            CreatePrimitive(
                PrimitiveType.Cube,
                catenary.transform,
                "ContactWire",
                new Vector3(0f, 6.25f, 0f),
                new Vector3(0.035f, 0.035f, TrackLengthMetres),
                palette.Rail,
                false);
        }

        private static void BuildStationArchitecture(GameObject station, Palette palette)
        {
            var platform = CreatePrimitive(
                PrimitiveType.Cube,
                station.transform,
                "PlatformDeck",
                new Vector3(5.7f, 0.38f, 0f),
                new Vector3(6.8f, 0.9f, 232f),
                palette.Concrete,
                true);
            MarkStatic(platform);

            var edge = CreatePrimitive(
                PrimitiveType.Cube,
                station.transform,
                "SafetyEdge",
                new Vector3(2.34f, 0.86f, 0f),
                new Vector3(0.22f, 0.075f, 230f),
                palette.SafetyYellow,
                false);
            MarkStatic(edge);

            var canopy = CreateChild(station.transform, "Canopy");
            CreatePrimitive(
                PrimitiveType.Cube,
                canopy.transform,
                "CanopyRoof",
                new Vector3(6.7f, 6.15f, 0f),
                new Vector3(8.8f, 0.28f, 190f),
                palette.SignalBlue,
                false);

            foreach (var z in CalculateSymmetricPositions(90f, 18f))
            {
                CreatePrimitive(
                    PrimitiveType.Cube,
                    canopy.transform,
                    $"Column_{z:+000;-000;000}",
                    new Vector3(7.1f, 3.1f, z),
                    new Vector3(0.32f, 5.7f, 0.32f),
                    palette.DarkSteel,
                    false);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    canopy.transform,
                    $"LightStrip_{z:+000;-000;000}",
                    new Vector3(4.5f, 5.96f, z),
                    new Vector3(4.8f, 0.055f, 0.18f),
                    palette.Emissive,
                    false);
            }

            var hall = CreateChild(station.transform, "DepartureHall");
            CreatePrimitive(
                PrimitiveType.Cube,
                hall.transform,
                "HallBody",
                new Vector3(14.6f, 5.2f, 0f),
                new Vector3(10.5f, 10f, 88f),
                palette.SignalBlue,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                hall.transform,
                "GlassFacade",
                new Vector3(9.30f, 5.35f, 0f),
                new Vector3(0.12f, 8.4f, 82f),
                palette.Glass,
                false);

            var sign = CreatePrimitive(
                PrimitiveType.Cube,
                station.transform,
                "ShowcaseSign",
                new Vector3(5.8f, 3.2f, -111f),
                new Vector3(6.4f, 2.3f, 0.22f),
                palette.SignalBlue,
                false);
            CreateWorldLabel(
                sign.transform,
                "Title",
                "复兴号 · 出厂展示",
                new Vector3(0f, 0f, -0.53f),
                Quaternion.Euler(0f, 180f, 0f),
                Color.white,
                0.18f);
        }

        private static bool TryBuildImportedTrain(
            Transform parent,
            GameObject sourceModel,
            Scene scene,
            Palette palette,
            out GameObject normalizedTrainRoot)
        {
            normalizedTrainRoot = null;
            if (sourceModel == null)
                return false;

            var importedInstance = PrefabUtility.InstantiatePrefab(sourceModel, scene) as GameObject;
            if (importedInstance == null)
                return false;

            importedInstance.name = "ImportedFbx_Staging";
            var preferred = FindDescendantByName(importedInstance.transform, PreferredSourceNodeName);
            GameObject visual;
            if (preferred != null)
            {
                visual = UnityEngine.Object.Instantiate(preferred.gameObject);
                visual.name = "FuxingTrain_Visual";
                UnityEngine.Object.DestroyImmediate(importedInstance);
            }
            else
            {
                visual = importedInstance;
                visual.name = "FuxingTrain_Visual";
            }

            var placementRoot = CreateChild(parent, "FuxingTrain_Normalized");
            visual.transform.SetParent(placementRoot.transform, true);
            StripNonVisualComponents(visual);
            ApplyCandidateMaterialFallbacks(visual, palette);

            if (!TryCalculateHierarchyBounds(placementRoot, out var sourceBounds))
            {
                UnityEngine.Object.DestroyImmediate(placementRoot);
                Debug.LogWarning(
                    $"FinalShowcase 在 {ModelAssetPath} 中没有找到可渲染网格，将改用占位编组。");
                return false;
            }

            if (ShouldRotateLengthFromX(sourceBounds.size))
                placementRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            if (!TryCalculateHierarchyBounds(placementRoot, out var orientedBounds))
            {
                UnityEngine.Object.DestroyImmediate(placementRoot);
                return false;
            }

            var sourceLength = Mathf.Max(orientedBounds.size.x, orientedBounds.size.z);
            var scale = CalculateUniformScale(sourceLength, TargetTrainLengthMetres);
            placementRoot.transform.localScale = Vector3.one * scale;

            if (!TryCalculateHierarchyBounds(placementRoot, out var scaledBounds))
            {
                UnityEngine.Object.DestroyImmediate(placementRoot);
                return false;
            }

            placementRoot.transform.position += new Vector3(
                -scaledBounds.center.x,
                RailTopY - scaledBounds.min.y,
                -scaledBounds.center.z);

            var contract = CreateChild(parent, "ImportedModelPlacement");
            CreateChild(contract.transform, $"SourceNode__{PreferredSourceNodeName}");
            CreateChild(contract.transform, $"TargetLengthMetres__{TargetTrainLengthMetres:0}");
            CreateChild(contract.transform, $"AppliedUniformScale__{scale:0.###}");
            normalizedTrainRoot = placementRoot;
            return true;
        }

        private static void ApplyCandidateMaterialFallbacks(GameObject root, Palette palette)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var index = 0; index < materials.Length; index++)
                {
                    var replacement = ResolveCandidateMaterial(materials[index], palette);
                    if (replacement == null || replacement == materials[index])
                        continue;

                    materials[index] = replacement;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        private static Material ResolveCandidateMaterial(Material source, Palette palette)
        {
            if (source == null)
                return palette.White;

            var name = source.name;
            if (name.IndexOf("Silver", StringComparison.OrdinalIgnoreCase) >= 0)
                return palette.White;
            if (name.IndexOf("Glas", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(name, "材质.2", StringComparison.Ordinal))
                return palette.Glass;
            if (string.Equals(name, "材质.1", StringComparison.Ordinal) ||
                string.Equals(name, "材质.4", StringComparison.Ordinal))
                return palette.SignalBlue;
            if (name.IndexOf("metal_bumpy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("dark_plastic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("futuristic_01", StringComparison.OrdinalIgnoreCase) >= 0)
                return palette.DarkSteel;

            return source;
        }

        private static Transform FindDescendantByName(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (var index = 0; index < root.childCount; index++)
            {
                var match = FindDescendantByName(root.GetChild(index), name);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static void StripNonVisualComponents(GameObject root)
        {
            foreach (var camera in root.GetComponentsInChildren<UnityEngine.Camera>(true))
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            foreach (var light in root.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            foreach (var audioSource in root.GetComponentsInChildren<AudioSource>(true))
                UnityEngine.Object.DestroyImmediate(audioSource);
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(animator);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
        }

        private static bool TryCalculateHierarchyBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .ToArray();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds.size.sqrMagnitude > 0.000001f;
        }

        private static void BuildPlaceholderTrain(Transform parent, Palette palette)
        {
            var placeholder = CreateChild(parent, "FuxingTrain_Placeholder_8Car");
            var warning = CreateChild(placeholder.transform, "MODEL_MISSING__See_Documentation");
            CreateChild(warning.transform, "ExpectedAsset__FuxingTrain_fbx");

            const int carCount = 8;
            const float carLength = 24.15f;
            const float gap = 0.8f;
            var totalLength = carCount * carLength + (carCount - 1) * gap;
            var firstCenter = -totalLength * 0.5f + carLength * 0.5f;

            for (var carIndex = 0; carIndex < carCount; carIndex++)
            {
                var z = firstCenter + carIndex * (carLength + gap);
                var headAtNegativeEnd = carIndex == 0;
                var headAtPositiveEnd = carIndex == carCount - 1;
                BuildPlaceholderCar(
                    placeholder.transform,
                    carIndex,
                    z,
                    headAtNegativeEnd,
                    headAtPositiveEnd,
                    palette);
            }
        }

        private static void BuildCarSegmentAnchors(Transform parent)
        {
            CreateCarSegmentHierarchy(
                parent,
                CalculateTargetCarBoundaryPositions(),
                "VisualRoot_ReservedForFutureSplit");
        }

        private static void BuildImportedCarSegmentsAndLod(
            Transform parent,
            GameObject normalizedTrainRoot,
            Palette palette)
        {
            if (normalizedTrainRoot == null)
                throw new ArgumentNullException(nameof(normalizedTrainRoot));

            var highDetailRenderers = normalizedTrainRoot
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .Distinct()
                .ToArray();
            if (highDetailRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "The normalized train contains no mesh renderers to segment.");
            }

            var boundaries = CalculateTargetCarBoundaryPositions();
            var segments = CreateCarSegmentHierarchy(
                parent,
                boundaries,
                "VisualRoot_LOD0_HighDetail");
            var assignments = new RendererAssignment[highDetailRenderers.Length];

            // Snapshot every world-space assignment and matrix before changing
            // any parent. This makes the result independent from FBX nesting and
            // prevents moving a renderer parent from affecting a later decision.
            for (var index = 0; index < highDetailRenderers.Length; index++)
            {
                var renderer = highDetailRenderers[index];
                var segmentIndex = ResolveCarSegmentIndex(renderer.bounds.center.z, boundaries);
                assignments[index] = new RendererAssignment(
                    renderer,
                    segmentIndex,
                    renderer.transform.localToWorldMatrix);

                var segment = segments[segmentIndex];
                segment.HighDetailRenderers.Add(renderer);
                if (!segment.HasBounds)
                {
                    segment.HighDetailBounds = renderer.bounds;
                    segment.HasBounds = true;
                }
                else
                {
                    var bounds = segment.HighDetailBounds;
                    bounds.Encapsulate(renderer.bounds);
                    segment.HighDetailBounds = bounds;
                }
            }

            foreach (var assignment in assignments)
            {
                assignment.Renderer.transform.SetParent(
                    segments[assignment.SegmentIndex].VisualRoot,
                    true);
                if (!ApproximatelyEqual(
                    assignment.OriginalWorldMatrix,
                    assignment.Renderer.transform.localToWorldMatrix,
                    0.002f))
                {
                    throw new InvalidOperationException(
                        $"Reparenting changed the world transform of renderer " +
                        $"'{assignment.Renderer.name}'.");
                }
            }

            ValidateUniqueRendererOwnership(highDetailRenderers, assignments, segments);

            var lod1Renderers = new List<Renderer>(
                CarSegmentCount * Lod1ProxyRenderersPerCar);
            var lod2Renderers = new List<Renderer>(
                CarSegmentCount * Lod2ProxyRenderersPerCar);
            for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                var segment = segments[segmentIndex];
                if (!segment.HasBounds)
                {
                    throw new InvalidOperationException(
                        $"Car segment {segmentIndex + 1} received no renderers.");
                }

                var carLod1Renderers = BuildLod1CarProxy(segment, palette);
                var carLod2Renderers = BuildLod2CarProxy(segment, palette);
                lod1Renderers.AddRange(carLod1Renderers);
                lod2Renderers.AddRange(carLod2Renderers);
                BuildCarLodGroup(
                    segment,
                    segment.HighDetailRenderers,
                    carLod1Renderers,
                    carLod2Renderers);
                CreateChild(
                    segment.Segment.transform,
                    $"Contract__LOD0_Renderers__{segment.HighDetailRenderers.Count}");
            }

            BuildImportedTrainContract(
                parent,
                highDetailRenderers.Length,
                segments,
                lod1Renderers.Count,
                lod2Renderers.Count);

            if (highDetailRenderers.Length != InspectedHighDetailRendererCount)
            {
                Debug.LogWarning(
                    $"FinalShowcase inspected {InspectedHighDetailRendererCount} source renderers, " +
                    $"but the current import contains {highDetailRenderers.Length}. " +
                    "All current renderers were still assigned exactly once.");
            }
        }

        private static List<CarSegmentBuildData> CreateCarSegmentHierarchy(
            Transform parent,
            IReadOnlyList<float> boundaries,
            string visualRootName)
        {
            ValidateCarBoundaries(boundaries);
            var root = CreateChild(parent, "CarSegments");
            var roles = new[]
            {
                "HeadA",
                "Intermediate",
                "Intermediate",
                "Intermediate",
                "Intermediate",
                "Intermediate",
                "Intermediate",
                "HeadB"
            };
            var result = new List<CarSegmentBuildData>(CarSegmentCount);

            for (var index = 0; index < roles.Length; index++)
            {
                var sourceMinusX = boundaries[index];
                var sourcePlusX = boundaries[index + 1];
                var centerZ = (sourceMinusX + sourcePlusX) * 0.5f;
                var segment = CreateChild(
                    root.transform,
                    $"CarSegment_{index + 1:00}_{roles[index]}");
                segment.transform.localPosition = new Vector3(0f, RailTopY, centerZ);

                var minusAnchor = CreateChild(segment.transform, "SourceMinusX_End");
                minusAnchor.transform.localPosition = new Vector3(0f, 0f, sourceMinusX - centerZ);
                var plusAnchor = CreateChild(segment.transform, "SourcePlusX_End");
                plusAnchor.transform.localPosition = new Vector3(0f, 0f, sourcePlusX - centerZ);
                var visualRoot = CreateChild(segment.transform, visualRootName);

                result.Add(new CarSegmentBuildData
                {
                    Segment = segment,
                    VisualRoot = visualRoot.transform
                });
            }

            return result;
        }

        private static void ValidateUniqueRendererOwnership(
            IReadOnlyList<Renderer> expectedRenderers,
            IReadOnlyList<RendererAssignment> assignments,
            IReadOnlyList<CarSegmentBuildData> segments)
        {
            var expected = new HashSet<Renderer>(expectedRenderers);
            var assigned = new HashSet<Renderer>();
            foreach (var assignment in assignments)
            {
                if (!assigned.Add(assignment.Renderer))
                {
                    throw new InvalidOperationException(
                        $"Renderer '{assignment.Renderer.name}' was assigned more than once.");
                }

                if (!assignment.Renderer.transform.IsChildOf(
                    segments[assignment.SegmentIndex].VisualRoot))
                {
                    throw new InvalidOperationException(
                        $"Renderer '{assignment.Renderer.name}' is outside its VisualRoot.");
                }
            }

            if (!expected.SetEquals(assigned))
            {
                throw new InvalidOperationException(
                    $"Renderer segmentation mismatch: expected {expected.Count}, " +
                    $"assigned {assigned.Count} unique renderers.");
            }
        }

        private static IReadOnlyList<Renderer> BuildLod1CarProxy(
            CarSegmentBuildData segment,
            Palette palette)
        {
            var root = CreateChild(segment.VisualRoot, "LOD1_Proxy");
            var bounds = ToLocalBounds(segment.VisualRoot, segment.HighDetailBounds);
            var width = Mathf.Max(0.5f, bounds.size.x);
            var height = Mathf.Max(0.5f, bounds.size.y);
            var length = Mathf.Max(0.5f, bounds.size.z);
            var minimum = bounds.min;
            var center = bounds.center;
            var sideThickness = Mathf.Max(0.025f, width * 0.012f);
            var renderers = new List<Renderer>(Lod1ProxyRenderersPerCar)
            {
                CreateDisabledProxyCube(
                    root.transform,
                    "BodyShell",
                    new Vector3(center.x, minimum.y + height * 0.52f, center.z),
                    new Vector3(width, height * 0.72f, length * 0.96f),
                    palette.White,
                    ShadowCastingMode.On),
                CreateDisabledProxyCube(
                    root.transform,
                    "Roof",
                    new Vector3(center.x, minimum.y + height * 0.91f, center.z),
                    new Vector3(width * 0.88f, height * 0.18f, length * 0.91f),
                    palette.White,
                    ShadowCastingMode.On),
                CreateDisabledProxyCube(
                    root.transform,
                    "Underframe",
                    new Vector3(center.x, minimum.y + height * 0.10f, center.z),
                    new Vector3(width * 0.86f, height * 0.20f, length * 0.84f),
                    palette.DarkSteel,
                    ShadowCastingMode.On),
                CreateDisabledProxyCube(
                    root.transform,
                    "WindowBand_Left",
                    new Vector3(
                        center.x - width * 0.505f,
                        minimum.y + height * 0.65f,
                        center.z),
                    new Vector3(sideThickness, height * 0.17f, length * 0.82f),
                    palette.Glass,
                    ShadowCastingMode.Off),
                CreateDisabledProxyCube(
                    root.transform,
                    "WindowBand_Right",
                    new Vector3(
                        center.x + width * 0.505f,
                        minimum.y + height * 0.65f,
                        center.z),
                    new Vector3(sideThickness, height * 0.17f, length * 0.82f),
                    palette.Glass,
                    ShadowCastingMode.Off)
            };
            return renderers;
        }

        private static IReadOnlyList<Renderer> BuildLod2CarProxy(
            CarSegmentBuildData segment,
            Palette palette)
        {
            var root = CreateChild(segment.VisualRoot, "LOD2_Proxy");
            var bounds = ToLocalBounds(segment.VisualRoot, segment.HighDetailBounds);
            return new[]
            {
                CreateDisabledProxyCube(
                    root.transform,
                    "Silhouette",
                    bounds.center,
                    new Vector3(
                        Mathf.Max(0.5f, bounds.size.x * 0.96f),
                        Mathf.Max(0.5f, bounds.size.y * 0.94f),
                        Mathf.Max(0.5f, bounds.size.z * 0.96f)),
                    palette.White,
                    ShadowCastingMode.Off)
            };
        }

        private static Renderer CreateDisabledProxyCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            ShadowCastingMode shadowCastingMode)
        {
            var item = CreatePrimitive(
                PrimitiveType.Cube,
                parent,
                name,
                localPosition,
                localScale,
                material,
                false);
            var renderer = item.GetComponent<Renderer>();
            renderer.shadowCastingMode = shadowCastingMode;
            renderer.receiveShadows = shadowCastingMode != ShadowCastingMode.Off;
            // Proxies cannot flash alongside LOD0 while the arrays are assembled.
            // They are enabled only after registration; native LOD culling then
            // becomes their sole visibility owner.
            renderer.enabled = false;
            return renderer;
        }

        private static Bounds ToLocalBounds(Transform parent, Bounds worldBounds)
        {
            var localCenter = parent.InverseTransformPoint(worldBounds.center);
            var scale = parent.lossyScale;
            var localSize = new Vector3(
                worldBounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                worldBounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                worldBounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
            return new Bounds(localCenter, localSize);
        }

        private static void BuildCarLodGroup(
            CarSegmentBuildData segment,
            IReadOnlyList<Renderer> highDetailRenderers,
            IReadOnlyList<Renderer> lod1Renderers,
            IReadOnlyList<Renderer> lod2Renderers)
        {
            var lodGroup = segment.VisualRoot.gameObject.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;

            var lods = new[]
            {
                new LOD(
                    Lod0ScreenRelativeTransitionHeight,
                    highDetailRenderers.ToArray())
                {
                    fadeTransitionWidth = 0.06f
                },
                new LOD(
                    Lod1ScreenRelativeTransitionHeight,
                    lod1Renderers.ToArray())
                {
                    fadeTransitionWidth = 0.08f
                },
                new LOD(
                    Lod2ScreenRelativeTransitionHeight,
                    lod2Renderers.ToArray())
                {
                    fadeTransitionWidth = 0.10f
                }
            };
            lodGroup.SetLODs(lods);

            // LODGroup expects registered renderers to remain enabled. Their
            // initial disabled state above is only a construction guard.
            foreach (var renderer in lod1Renderers.Concat(lod2Renderers))
                renderer.enabled = true;
            lodGroup.RecalculateBounds();
            lodGroup.ForceLOD(-1);

            var contract = CreateChild(segment.VisualRoot, "CarLODContract");
            CreateChild(contract.transform, "Contract__PerCarLODGroup");
            CreateChild(contract.transform, "Contract__LOD0_ImportedHighDetail");
            CreateChild(contract.transform, "Contract__ProxyConstructionStartsDisabled");
            CreateChild(contract.transform, "Contract__FinalVisibilityOwnedByLODGroup");
        }

        private static void BuildImportedTrainContract(
            Transform parent,
            int highDetailRendererCount,
            IReadOnlyList<CarSegmentBuildData> segments,
            int lod1RendererCount,
            int lod2RendererCount)
        {
            var contract = CreateChild(parent, "ImportedTrainSegmentationContract");
            CreateChild(contract.transform, $"CarSegments__{segments.Count}");
            CreateChild(contract.transform, $"CapturedRenderers__{highDetailRendererCount}");
            CreateChild(contract.transform, $"UniqueAssignments__{highDetailRendererCount}");
            CreateChild(contract.transform, "AssignmentAxis__WorldBoundsCenterZ");
            CreateChild(contract.transform, "InterfaceTieBreak__PositiveZSegment");
            CreateChild(contract.transform, "WorldTransformsPreserved__True");
            CreateChild(contract.transform, $"LOD0_Renderers__{highDetailRendererCount}");
            CreateChild(contract.transform, $"LOD1_Renderers__{lod1RendererCount}");
            CreateChild(contract.transform, $"LOD2_Renderers__{lod2RendererCount}");
            CreateChild(contract.transform, $"PerCarLODGroups__{segments.Count}");
        }

        private static bool ApproximatelyEqual(
            Matrix4x4 left,
            Matrix4x4 right,
            float tolerance)
        {
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    if (Mathf.Abs(left[row, column] - right[row, column]) > tolerance)
                        return false;
                }
            }

            return true;
        }

        private static void BuildPlaceholderCar(
            Transform parent,
            int index,
            float z,
            bool headAtNegativeEnd,
            bool headAtPositiveEnd,
            Palette palette)
        {
            var car = CreateChild(parent, headAtNegativeEnd || headAtPositiveEnd
                ? $"HeadCar_{index + 1:00}"
                : $"IntermediateCar_{index + 1:00}");
            car.transform.localPosition = new Vector3(0f, 0f, z);

            var bodyLength = headAtNegativeEnd || headAtPositiveEnd ? 21.2f : 23.65f;
            var bodyOffset = headAtNegativeEnd ? 1.25f : headAtPositiveEnd ? -1.25f : 0f;
            CreatePrimitive(
                PrimitiveType.Cube,
                car.transform,
                "Body",
                new Vector3(0f, 2.28f, bodyOffset),
                new Vector3(3.36f, 3.42f, bodyLength),
                palette.White,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                car.transform,
                "LowerSkirt",
                new Vector3(0f, 0.88f, bodyOffset),
                new Vector3(3.18f, 0.58f, bodyLength - 0.4f),
                palette.DarkSteel,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                car.transform,
                "RedWaistBand_Left",
                new Vector3(-1.695f, 1.76f, bodyOffset),
                new Vector3(0.045f, 0.22f, bodyLength - 0.2f),
                palette.TrainRed,
                false);
            CreatePrimitive(
                PrimitiveType.Cube,
                car.transform,
                "RedWaistBand_Right",
                new Vector3(1.695f, 1.76f, bodyOffset),
                new Vector3(0.045f, 0.22f, bodyLength - 0.2f),
                palette.TrainRed,
                false);

            if (headAtNegativeEnd || headAtPositiveEnd)
            {
                var direction = headAtNegativeEnd ? -1f : 1f;
                var nose = CreatePrimitive(
                    PrimitiveType.Sphere,
                    car.transform,
                    "StreamlinedCabNose",
                    new Vector3(0f, 2.08f, direction * 10.45f),
                    new Vector3(3.25f, 3.15f, 5.5f),
                    palette.White,
                    false);
                nose.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    car.transform,
                    "CabGlass",
                    new Vector3(0f, 2.85f, direction * 13.05f),
                    new Vector3(2.25f, 0.62f, 0.08f),
                    palette.Glass,
                    false);
            }

            for (var windowIndex = -4; windowIndex <= 4; windowIndex++)
            {
                var windowZ = windowIndex * 2.15f + bodyOffset;
                foreach (var x in new[] { -1.705f, 1.705f })
                {
                    CreatePrimitive(
                        PrimitiveType.Cube,
                        car.transform,
                        $"Window_{(x < 0f ? "L" : "R")}_{windowIndex + 5:00}",
                        new Vector3(x, 2.72f, windowZ),
                        new Vector3(0.055f, 0.67f, 1.36f),
                        palette.Glass,
                        false);
                }
            }

            foreach (var bogieZ in new[] { -7.3f, 7.3f })
            {
                var bogie = CreateChild(car.transform, bogieZ < 0f ? "Bogie_A" : "Bogie_B");
                bogie.transform.localPosition = new Vector3(0f, 0f, bogieZ);
                CreatePrimitive(
                    PrimitiveType.Cube,
                    bogie.transform,
                    "Frame",
                    new Vector3(0f, 0.68f, 0f),
                    new Vector3(2.7f, 0.34f, 3.1f),
                    palette.DarkSteel,
                    false);
                foreach (var axleZ in new[] { -0.9f, 0.9f })
                {
                    var wheelset = CreatePrimitive(
                        PrimitiveType.Cylinder,
                        bogie.transform,
                        axleZ < 0f ? "Wheelset_A" : "Wheelset_B",
                        new Vector3(0f, 0.69f, axleZ),
                        new Vector3(0.43f, 1.58f, 0.43f),
                        palette.DarkSteel,
                        false);
                    wheelset.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
        }

        private static void BuildLighting(GameObject lighting, Palette palette)
        {
            var sunObject = CreateChild(lighting.transform, "LateAfternoonSun");
            sunObject.transform.rotation = Quaternion.Euler(31f, -38f, 0f);
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.91f, 0.78f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;

            var fillObject = CreateChild(lighting.transform, "CoolFill");
            fillObject.transform.position = new Vector3(-18f, 15f, -24f);
            fillObject.transform.LookAt(new Vector3(0f, 2f, 0f));
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Spot;
            fill.color = new Color(0.43f, 0.68f, 1f);
            fill.intensity = 850f;
            fill.range = 95f;
            fill.spotAngle = 92f;
            fill.shadows = LightShadows.None;

            foreach (var z in new[] { -84f, -42f, 0f, 42f, 84f })
            {
                var lamp = CreatePrimitive(
                    PrimitiveType.Cube,
                    lighting.transform,
                    $"PlatformLamp_{z:+000;-000;000}",
                    new Vector3(4.4f, 5.75f, z),
                    new Vector3(0.55f, 0.08f, 0.26f),
                    palette.Emissive,
                    false);
                var point = lamp.AddComponent<Light>();
                point.type = LightType.Point;
                point.color = new Color(0.70f, 0.86f, 1f);
                point.intensity = 18f;
                point.range = 15f;
                point.shadows = LightShadows.None;
            }
        }

        private static UnityEngine.Camera BuildCameraComposition(GameObject composition)
        {
            CreateAnchor(composition.transform, "OverviewFocus", new Vector3(0f, 2.4f, 0f));
            CreateAnchor(composition.transform, "HeadCarFocus", new Vector3(0f, 2.3f, -91f));
            CreateAnchor(composition.transform, "SideDetailFocus", new Vector3(0f, 1.7f, -18f));
            CreateAnchor(composition.transform, "DepartureFocus", new Vector3(0f, 2.2f, 88f));

            var cameraObject = CreateChild(composition.transform, "HeroCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-38f, 16.5f, -118f);
            cameraObject.transform.LookAt(new Vector3(0f, 2.4f, -4f));
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 46f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 650f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.085f, 0.12f);
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static OverlayBindings BuildOverlay(GameObject root, bool usingImportedModel)
        {
            var canvasObject = new GameObject(
                "ShowcaseCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(root.transform, false);
            // OnEnable assigns the default action asset. A second assignment
            // throws in Input System 1.17 when rebuilding scenes in the Editor.

            var panel = new GameObject("TitlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(38f, -38f);
            panelRect.sizeDelta = new Vector2(650f, 174f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.085f, 0.88f);

            CreateUiText(
                panel.transform,
                "Title",
                "复兴号整列出厂展示",
                new Vector2(26f, -20f),
                new Vector2(598f, 54f),
                34,
                FontStyle.Bold,
                Color.white);
            CreateUiText(
                panel.transform,
                "Subtitle",
                "全列构图 · 出厂线 · 站台灯光",
                new Vector2(28f, -76f),
                new Vector2(590f, 34f),
                21,
                FontStyle.Normal,
                new Color(0.65f, 0.82f, 0.95f));
            CreateUiText(
                panel.transform,
                "ModelStatus",
                usingImportedModel
                    ? "已加载 FuxingTrain.fbx · 自动校准约 200 m"
                    : "布局预览：当前使用八节编组占位模型",
                new Vector2(28f, -116f),
                new Vector2(590f, 34f),
                18,
                FontStyle.Normal,
                usingImportedModel ? new Color(0.45f, 0.95f, 0.72f) : new Color(1f, 0.69f, 0.25f));

            var controlPanel = new GameObject(
                "PresentationControlPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            controlPanel.transform.SetParent(canvasObject.transform, false);
            var controlRect = controlPanel.GetComponent<RectTransform>();
            controlRect.anchorMin = new Vector2(0.5f, 0f);
            controlRect.anchorMax = new Vector2(0.5f, 0f);
            controlRect.pivot = new Vector2(0.5f, 0f);
            controlRect.anchoredPosition = new Vector2(0f, 28f);
            controlRect.sizeDelta = new Vector2(1120f, 82f);
            controlPanel.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.085f, 0.88f);

            var runtimeStatus = CreateUiText(
                controlPanel.transform,
                "RuntimeStatus",
                "全景机位 · 第 01/08 节 · 整列视图",
                new Vector2(24f, -10f),
                new Vector2(1072f, 28f),
                20,
                FontStyle.Bold,
                new Color(0.45f, 0.95f, 0.82f));
            runtimeStatus.alignment = TextAnchor.MiddleCenter;
            var shortcutHelp = CreateUiText(
                controlPanel.transform,
                "ShortcutHelp",
                FinalShowcaseHudState.ShortcutHelp,
                new Vector2(24f, -44f),
                new Vector2(1072f, 24f),
                16,
                FontStyle.Normal,
                new Color(0.72f, 0.84f, 0.94f));
            shortcutHelp.alignment = TextAnchor.MiddleCenter;

            var returnButtonObject = new GameObject(
                "ReturnToFactoryButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            returnButtonObject.transform.SetParent(canvasObject.transform, false);
            var returnRect = returnButtonObject.GetComponent<RectTransform>();
            returnRect.anchorMin = new Vector2(1f, 1f);
            returnRect.anchorMax = new Vector2(1f, 1f);
            returnRect.pivot = new Vector2(1f, 1f);
            returnRect.anchoredPosition = new Vector2(-38f, -38f);
            returnRect.sizeDelta = new Vector2(246f, 58f);

            var returnImage = returnButtonObject.GetComponent<Image>();
            returnImage.color = new Color(0.025f, 0.24f, 0.43f, 0.94f);
            var returnButton = returnButtonObject.GetComponent<Button>();
            returnButton.targetGraphic = returnImage;
            var colors = returnButton.colors;
            colors.highlightedColor = new Color(0.08f, 0.38f, 0.62f, 1f);
            colors.pressedColor = new Color(0.02f, 0.16f, 0.29f, 1f);
            colors.disabledColor = new Color(0.16f, 0.19f, 0.22f, 0.55f);
            returnButton.colors = colors;

            var label = CreateUiText(
                returnButtonObject.transform,
                "Label",
                "返回装配工厂  ESC",
                Vector2.zero,
                Vector2.zero,
                20,
                FontStyle.Bold,
                Color.white);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;

            return new OverlayBindings
            {
                ReturnButton = returnButton,
                RuntimeStatus = runtimeStatus,
                ShortcutHelp = shortcutHelp
            };
        }

        private static Text CreateUiText(
            Transform parent,
            string name,
            string value,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            FontStyle style,
            Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = item.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.UpperLeft;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = CreateChild(parent, name);
            anchor.transform.position = position;
            return anchor;
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
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            if (text.font != null)
                label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return label;
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.31f, 0.41f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.20f, 0.26f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.045f, 0.055f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.12f, 0.17f, 0.22f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 410f;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider)
        {
            var item = GameObject.CreatePrimitive(primitiveType);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                var collider = item.GetComponent<Collider>();
                if (collider != null)
                    UnityEngine.Object.DestroyImmediate(collider);
            }
            return item;
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
    }
}
