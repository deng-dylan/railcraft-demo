using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using RailCraft.Assets;
using RailCraft.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace RailCraft.Editor
{
    public static class FactoryPresentationBuilder
    {
        public const string SettingsRoot = "Assets/RailCraft/Art/Settings";
        public const string LightingSettingsPath = SettingsRoot + "/FactoryLightingSettings.lighting";
        public const string VolumeProfilePath = SettingsRoot + "/FactoryVolumeProfile.asset";
        public const string OcclusionDataPath = SettingsRoot + "/FactoryOcclusion.asset";
        public const string FactoryScenePath = "Assets/RailCraft/Scenes/Factory.unity";
        public const string CatalogPath = "Assets/RailCraft/Art/PartPrefabCatalog.asset";
        private const string WorkLightMaterialPath =
            "Assets/RailCraft/Art/Materials/FactoryWorkLight.mat";

        public static readonly string[] HighlightMaterialPaths =
        {
            "Assets/RailCraft/Art/Materials/BrakeRed.mat",
            "Assets/RailCraft/Art/Materials/CardWhite.mat",
            "Assets/RailCraft/Art/Materials/InspectionGreen.mat",
            "Assets/RailCraft/Art/Materials/RailBlue.mat",
            "Assets/RailCraft/Art/Materials/SafetyYellow.mat",
            "Assets/RailCraft/Art/Materials/SensorCyan.mat",
            "Assets/RailCraft/Art/Materials/SignalOrange.mat",
            "Assets/RailCraft/Art/Materials/Steel.mat",
            "Assets/RailCraft/Art/Materials/TractionViolet.mat"
        };

        [MenuItem("RailCraft/Configure Factory Presentation")]
        public static void ConfigureFromMenu() => ConfigureAndBake(false);

        public static void ConfigureFromCommandLine() => ConfigureAndBake(false);

        [MenuItem("RailCraft/Bake Factory Presentation")]
        public static void BuildFromMenu() => ConfigureAndBake(true);

        public static void BuildFromCommandLine() => ConfigureAndBake(true);

        private static void ConfigureAndBake(bool bake)
        {
            EnsureSettingsFolder();
            PlayerSettings.enableFrameTimingStats = true;
            var scene = EditorSceneManager.OpenScene(FactoryScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "FactoryRoot");
            if (root == null)
                throw new InvalidOperationException("FactoryRoot is missing from Factory.unity.");

            ConfigureHighlightMaterials();
            ConfigureWorkLightMaterial();
            var lightingSettings = CreateOrUpdateLightingSettings();
            var volumeProfile = CreateOrUpdateVolumeProfile();
            Lightmapping.SetLightingSettingsForScene(scene, lightingSettings);
            ConfigureScene(root, volumeProfile);
            StaticOcclusionCulling.smallestOccluder = 3f;
            StaticOcclusionCulling.smallestHole = 0.25f;
            StaticOcclusionCulling.backfaceThreshold = 100f;

            EditorSceneManager.MarkSceneDirty(scene);
            Save(scene);
            if (!bake)
            {
                Debug.Log("RAILCRAFT_PRESENTATION_CONFIGURED");
                return;
            }

            var lightingTimer = Stopwatch.StartNew();
            if (!Lightmapping.Bake())
                throw new InvalidOperationException("Factory lighting bake did not complete successfully.");
            lightingTimer.Stop();

            var occlusionTimer = Stopwatch.StartNew();
            if (!StaticOcclusionCulling.Compute())
                throw new InvalidOperationException("Factory occlusion bake did not complete successfully.");
            occlusionTimer.Stop();

            Save(scene);
            MoveOcclusionDataToStablePath();
            Save(scene);
            var occlusionFile = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", OcclusionDataPath));
            var occlusionBytes = new FileInfo(occlusionFile).Length;
            Debug.Log($"RAILCRAFT_PRESENTATION_BAKED lighting_seconds={lightingTimer.Elapsed.TotalSeconds:F2};" +
                $"occlusion_seconds={occlusionTimer.Elapsed.TotalSeconds:F2};" +
                $"occlusion_bytes={occlusionBytes}");
        }

        private static void ConfigureHighlightMaterials()
        {
            var configured = new HashSet<Material>();
            foreach (var path in HighlightMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    throw new InvalidOperationException("Highlight material is missing: " + path);
                material.SetColor("_EmissionColor", Color.black);
                EnsureHighlightMaterial(material);
                configured.Add(material);
            }

            foreach (var material in GetCatalogHighlightMaterials())
            {
                if (configured.Add(material))
                    EnsureHighlightMaterial(material);
            }
        }

        public static IReadOnlyList<Material> GetCatalogHighlightMaterials()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PartPrefabCatalog>(CatalogPath);
            if (catalog == null)
                return Array.Empty<Material>();

            return catalog.Entries
                .Where(entry => entry?.prefab != null)
                .Select(entry => entry.prefab.GetComponent<DraggableModule>())
                .Where(module => module != null && module.VisualRoot != null)
                .SelectMany(module => module.VisualRoot.GetComponentsInChildren<Renderer>(true))
                .SelectMany(renderer => renderer.sharedMaterials ?? Array.Empty<Material>())
                .Where(material => material != null)
                .Distinct()
                .ToArray();
        }

        private static void EnsureHighlightMaterial(Material material)
        {
            if (ProductionAssetBudgetPostprocessor.TryEnsurePropertyBlockEmission(
                    material, out var issue))
                return;
            var path = AssetDatabase.GetAssetPath(material);
            throw new InvalidOperationException($"Highlight material {path} {issue}");
        }

        private static void ConfigureWorkLightMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkLightMaterialPath);
            if (material == null)
                throw new InvalidOperationException("Factory work-light material is missing.");
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(material);
        }

        private static LightingSettings CreateOrUpdateLightingSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = "FactoryLightingSettings" };
                AssetDatabase.CreateAsset(settings, LightingSettingsPath);
            }

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.realtimeEnvironmentLighting = false;
            settings.mixedBakeMode = MixedLightingMode.Shadowmask;
            settings.albedoBoost = 1f;
            settings.indirectScale = 1f;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            settings.lightmapMaxSize = 1024;
            settings.lightmapResolution = 10f;
            settings.lightmapPadding = 2;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.ao = true;
            settings.aoMaxDistance = 1f;
            settings.aoExponentIndirect = 1f;
            settings.aoExponentDirect = 0.5f;
            settings.directionalityMode = LightmapsMode.CombinedDirectional;
            settings.sampling = LightingSettings.Sampling.Fixed;
            settings.directSampleCount = 16;
            settings.indirectSampleCount = 64;
            settings.environmentSampleCount = 32;
            settings.maxBounces = 2;
            settings.minBounces = 1;
            settings.filteringMode = LightingSettings.FilterMode.Auto;
            settings.environmentImportanceSampling = true;
            settings.lightProbeSampleCountMultiplier = 2f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static VolumeProfile CreateOrUpdateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "FactoryVolumeProfile";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            for (var index = profile.components.Count - 1; index >= 0; index--)
            {
                var component = profile.components[index];
                profile.components.RemoveAt(index);
                if (component != null)
                    Object.DestroyImmediate(component, true);
            }

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.active = true;
            tonemapping.mode.value = TonemappingMode.ACES;
            AssetDatabase.AddObjectToAsset(tonemapping, profile);

            var bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.value = 1.1f;
            bloom.intensity.value = 0.12f;
            bloom.scatter.value = 0.5f;
            bloom.highQualityFiltering.value = false;
            bloom.maxIterations.value = 4;
            AssetDatabase.AddObjectToAsset(bloom, profile);

            var color = profile.Add<ColorAdjustments>(true);
            color.active = true;
            color.postExposure.value = -0.05f;
            color.contrast.value = 5f;
            color.saturation.value = -3f;
            AssetDatabase.AddObjectToAsset(color, profile);

            profile.Reset();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureScene(GameObject root, VolumeProfile profile)
        {
            var volumeTransform = root.transform.Find("FactoryVolume");
            var volumeObject = volumeTransform == null
                ? new GameObject("FactoryVolume")
                : volumeTransform.gameObject;
            if (volumeTransform == null)
                volumeObject.transform.SetParent(root.transform, false);
            var volume = volumeObject.GetComponent<Volume>() ?? volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            var camera = root.GetComponentInChildren<Camera>(true);
            if (camera == null)
                throw new InvalidOperationException("Factory camera is missing.");
            camera.useOcclusionCulling = true;
            camera.allowHDR = true;
            var cameraData = camera.GetComponent<UniversalAdditionalCameraData>()
                ?? camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            EditorUtility.SetDirty(volume);
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(cameraData);
        }

        public static Object GetOcclusionDataForActiveScene()
        {
            var property = typeof(StaticOcclusionCulling).GetProperty(
                "occlusionCullingSettings", BindingFlags.Static | BindingFlags.NonPublic);
            var settingsObject = property?.GetValue(null) as Object;
            if (settingsObject == null)
                return null;
            return new SerializedObject(settingsObject)
                .FindProperty("m_OcclusionCullingData")?.objectReferenceValue;
        }

        private static void MoveOcclusionDataToStablePath()
        {
            var data = GetOcclusionDataForActiveScene();
            if (data == null)
                throw new InvalidOperationException("Occlusion bake produced no data asset.");

            var currentPath = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrWhiteSpace(currentPath))
                throw new InvalidOperationException("Occlusion data has no AssetDatabase path.");
            if (string.Equals(currentPath, OcclusionDataPath, StringComparison.Ordinal))
                return;
            if (AssetDatabase.LoadAssetAtPath<Object>(OcclusionDataPath) != null
                && !AssetDatabase.DeleteAsset(OcclusionDataPath))
                throw new InvalidOperationException("Could not replace stale FactoryOcclusion.asset.");
            var error = AssetDatabase.MoveAsset(currentPath, OcclusionDataPath);
            if (!string.IsNullOrWhiteSpace(error))
                throw new InvalidOperationException("Could not move occlusion data: " + error);
            AssetDatabase.ImportAsset(OcclusionDataPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, FactoryScenePath))
                throw new InvalidOperationException("Could not save Factory.unity.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder(SettingsRoot))
                AssetDatabase.CreateFolder("Assets/RailCraft/Art", "Settings");
        }
    }
}