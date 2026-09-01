using System.IO;
using System.Linq;
using NUnit.Framework;
using RailCraft.Assets;
using RailCraft.Editor;
using RailCraft.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace RailCraft.Tests.EditMode
{
    public sealed class FactoryPresentationContractTests
    {
        private const string CatalogPath = "Assets/RailCraft/Art/PartPrefabCatalog.asset";

        [Test]
        public void FactoryPresentationAssetsUseConservativeUrpSettings()
        {
            var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(
                FactoryPresentationBuilder.LightingSettingsPath);
            Assert.That(PlayerSettings.enableFrameTimingStats, Is.True);
            Assert.That(lighting, Is.Not.Null);
            Assert.That(lighting.bakedGI, Is.True);
            Assert.That(lighting.realtimeGI, Is.False);
            Assert.That(lighting.autoGenerate, Is.False);
            Assert.That(lighting.lightmapMaxSize, Is.LessThanOrEqualTo(1024));
            Assert.That(lighting.maxBounces, Is.LessThanOrEqualTo(2));

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                FactoryPresentationBuilder.VolumeProfilePath);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.TryGet(out Tonemapping tonemapping), Is.True);
            Assert.That(tonemapping.mode.value, Is.EqualTo(TonemappingMode.ACES));
            Assert.That(profile.TryGet(out Bloom bloom), Is.True);
            Assert.That(bloom.intensity.value, Is.InRange(0.01f, 0.2f));
            Assert.That(bloom.highQualityFiltering.value, Is.False);
            Assert.That(profile.TryGet(out ColorAdjustments _), Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<Object>(
                FactoryPresentationBuilder.OcclusionDataPath), Is.Not.Null);
        }


        [Test]
        public void HighlightMaterialsEnableUrpEmissionForPropertyBlocks()
        {
            foreach (var path in FactoryPresentationBuilder.HighlightMaterialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material, Is.Not.Null, path);
                Assert.That(material.HasProperty("_EmissionColor"), Is.True, path);
                var emissionKeyword = new LocalKeyword(material.shader, "_EMISSION");
                Assert.That(emissionKeyword.isValid, Is.True, path);
                Assert.That(material.IsKeywordEnabled(emissionKeyword), Is.True, path);
                var emission = material.GetColor("_EmissionColor");
                Assert.That(Mathf.Max(emission.r, Mathf.Max(emission.g, emission.b)),
                    Is.EqualTo(0f).Within(0.000001f), path);
                Assert.That(material.globalIlluminationFlags &
                    MaterialGlobalIlluminationFlags.RealtimeEmissive,
                    Is.EqualTo(MaterialGlobalIlluminationFlags.RealtimeEmissive), path);
            }
        }

        [Test]
        public void FactorySceneReferencesBakesAndKeepsInteractiveModulesDynamic()
        {
            var scene = EditorSceneManager.OpenScene(
                FactoryPresentationBuilder.FactoryScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().Single(item => item.name == "FactoryRoot");
            var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(
                FactoryPresentationBuilder.LightingSettingsPath);
            Assert.That(Lightmapping.GetLightingSettingsForScene(scene), Is.SameAs(lighting));

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                FactoryPresentationBuilder.VolumeProfilePath);
            var volume = root.GetComponentInChildren<Volume>(true);
            Assert.That(volume, Is.Not.Null);
            Assert.That(volume.isGlobal, Is.True);
            Assert.That(volume.sharedProfile, Is.SameAs(profile));

            var camera = root.GetComponentInChildren<Camera>(true);
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.useOcclusionCulling, Is.True);
            var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            Assert.That(cameraData, Is.Not.Null);
            Assert.That(cameraData.renderPostProcessing, Is.True);

            var lights = root.GetComponentsInChildren<Light>(true);
            Assert.That(lights.Count(light => light.shadows != LightShadows.None
                && light.lightmapBakeType != LightmapBakeType.Baked), Is.LessThanOrEqualTo(2));
            Assert.That(lights.Count(light => light.lightmapBakeType == LightmapBakeType.Baked),
                Is.GreaterThan(0));
            var lightProbes = root.GetComponentInChildren<LightProbeGroup>(true);
            Assert.That(lightProbes, Is.Not.Null);
            Assert.That(lightProbes.probePositions, Is.Not.Empty);
            var reflectionProbes = root.GetComponentsInChildren<ReflectionProbe>(true);
            Assert.That(reflectionProbes, Has.Length.GreaterThanOrEqualTo(2));

            var lockedFuture = root.transform.Find("LockedFutureModules");
            Assert.That(lockedFuture, Is.Not.Null);
            var lockedRenderers = lockedFuture.GetComponentsInChildren<Renderer>(true);
            Assert.That(lockedRenderers, Has.Length.EqualTo(3));
            foreach (var renderer in lockedRenderers)
                Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject),
                    Is.EqualTo((StaticEditorFlags)0));

            var data = FactoryPresentationBuilder.GetOcclusionDataForActiveScene();
            Assert.That(data, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(data),
                Is.EqualTo(FactoryPresentationBuilder.OcclusionDataPath));
            Assert.That(AssetFileSize(FactoryPresentationBuilder.OcclusionDataPath),
                Is.GreaterThan(1024));
            Assert.That(AssetFileSize("Assets/RailCraft/Scenes/Factory/LightingData.asset"),
                Is.GreaterThan(1024));

            var bakeFolder = ProjectPath("Assets/RailCraft/Scenes/Factory");
            var lightmaps = Directory.GetFiles(bakeFolder, "Lightmap-*_comp_light.exr");
            Assert.That(lightmaps, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(lightmaps.All(path => new FileInfo(path).Length > 1024), Is.True);
            var reflections = Directory.GetFiles(bakeFolder, "ReflectionProbe-*.exr");
            Assert.That(reflections, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(reflections.All(path => new FileInfo(path).Length > 1024), Is.True);

            var catalog = AssetDatabase.LoadAssetAtPath<PartPrefabCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            foreach (var entry in catalog.Entries)
            {
                var module = entry.prefab.GetComponentInChildren<DraggableModule>(true);
                Assert.That(module, Is.Not.Null, entry.assetKey);
                var flags = GameObjectUtility.GetStaticEditorFlags(module.gameObject);
                Assert.That(flags & (StaticEditorFlags.ContributeGI
                    | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic),
                    Is.EqualTo((StaticEditorFlags)0), entry.assetKey);
            }
        }

        [Test]
        public void ProductionAssetBudgetsAreExecutableContracts()
        {
            Assert.That(ProductionAssetBudgetPostprocessor.RequiredLodLevels, Is.EqualTo(3));
            Assert.That(ProductionAssetBudgetPostprocessor.ResolveTextureLimit(
                "Assets/RailCraft/Art/Textures/Hero/carbody.png"), Is.EqualTo(2048));
            Assert.That(ProductionAssetBudgetPostprocessor.ResolveTextureLimit(
                "Assets/RailCraft/Art/Textures/Props/toolbox.png"), Is.EqualTo(1024));
            Assert.That(ProductionAssetBudgetPostprocessor.FindProductionLodViolations(), Is.Empty);
            Assert.That(ProductionAssetBudgetPostprocessor.FindHighlightMaterialViolations(),
                Is.Empty);
            Assert.That(FactoryPresentationBuilder.GetCatalogHighlightMaterials(), Is.Not.Empty);
        }

        [Test]
        public void ProductionMaterialImportGateIsScopedAndEnablesPropertyBlockEmission()
        {
            Assert.That(ProductionAssetBudgetPostprocessor.IsProductionModelPath(
                "Assets/RailCraft/Art/Models/Production/Bogie/Wheel/wheel.fbx"), Is.True);
            Assert.That(ProductionAssetBudgetPostprocessor.IsProductionModelPath(
                "Assets/RailCraft/Art/Models/Factory/production_sign.fbx"), Is.False);
            Assert.That(ProductionAssetBudgetPostprocessor.IsProductionModelPath(
                "Assets/ThirdParty/Models/Production/wheel.fbx"), Is.False);
            Assert.That(ProductionAssetBudgetPostprocessor.IsProductionModelPath(
                "Assets/RailCraft/Art/Materials/Production/wheel.mat"), Is.False);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                var originalEmission = new Color(0.25f, 0.1f, 0.05f, 1f);
                material.SetColor("_EmissionColor", originalEmission);
                var keyword = new LocalKeyword(shader, "_EMISSION");
                material.DisableKeyword(keyword);
                material.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                var configured = ProductionAssetBudgetPostprocessor
                    .TryEnsurePropertyBlockEmission(material, out var issue);

                Assert.That(configured, Is.True, issue);
                Assert.That(material.GetColor("_EmissionColor"), Is.EqualTo(originalEmission));
                Assert.That(material.IsKeywordEnabled(keyword), Is.True);
                Assert.That(material.globalIlluminationFlags,
                    Is.EqualTo(MaterialGlobalIlluminationFlags.RealtimeEmissive));
                Assert.That(ProductionAssetBudgetPostprocessor
                    .GetPropertyBlockEmissionIssue(material), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void PerformanceBudgetRecordsEveryRequiredCaptureState()
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                "Documentation", "PerformanceBudget.md"));
            Assert.That(File.Exists(path), Is.True);
            var document = File.ReadAllText(path);
            foreach (var required in new[]
            {
                "2,000,000", "500", "2048", "1024", "60 FPS", "45 FPS", "10 秒",
                "factory idle", "all bogie modules installed", "carbody lowering",
                "commissioning feedback", "final hero view"
            })
            {
                Assert.That(document, Does.Contain(required), required);
            }
        }

        private static long AssetFileSize(string assetPath)
        {
            return new FileInfo(ProjectPath(assetPath)).Length;
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }
    }
}