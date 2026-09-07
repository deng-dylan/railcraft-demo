using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Deterministic visual-only dressing sourced from the free ArtStation
    /// industrial packs.  The imported meshes are deliberately kept outside
    /// the gameplay catalog: they dress the factory and never claim to be
    /// engineering parts or interactive physics assets.
    /// </summary>
    public static class ArtStationIndustrialVisualFactory
    {
        public const string RootName = "ArtStationIndustrialDressing";

        public const string AssetRootPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ArtStationIndustrial";
        public const string ForkliftModelPath =
            AssetRootPath + "/Forklift/SM_Ind_Forklift_Detailed.fbx";
        public const string BenchGrinderModelPath =
            AssetRootPath + "/Equipment/SM_Industrial_Bench_Grinder_01.fbx";
        public const string CableReelModelPath =
            AssetRootPath + "/Equipment/SM_Industrial_Cable_Reel_01_Background.fbx";
        public const string CementMixerOneModelPath =
            AssetRootPath + "/Equipment/SM_Industrial_Cement_Mixer_001.fbx";
        public const string CementMixerTwoModelPath =
            AssetRootPath + "/Equipment/SM_Industrial_Cement_Mixer_002.fbx";
        public const string ConcreteBarrierModelPath =
            AssetRootPath + "/Equipment/SM_Industrial_Concrete_Barrier_Background.fbx";
        public const string ShelfAisleModelPath =
            AssetRootPath + "/Shelving/SM_Aisle_Red_A.fbx";
        public const string ShelfRedBackgroundModelPath =
            AssetRootPath + "/Shelving/SM_ShelfEmptyx3_Red_B.fbx";
        public const string ShelfWhiteBackgroundModelPath =
            AssetRootPath + "/Shelving/SM_ShelfEmptyx3_White_B.fbx";
        public const string AtlasTexturePath =
            AssetRootPath + "/Textures/Stylized_Industrial_TextureMap_V02.png";

        public const string MaterialRootPath =
            "Assets/RailCraft/ThirdPerson/Art/Materials/ArtAlpha";
        public const string MaterialPath = MaterialRootPath + "/AA_ArtStationIndustrial.mat";

        public const string ForkliftName = "ArtStationForklift_WestLogistics";
        public const string BenchGrinderName = "ArtStationBenchGrinder_EastBay";
        public const string CableReelName = "ArtStationCableReel_EastBay";
        public const string CementMixerOneName = "ArtStationCementMixer_NorthA";
        public const string CementMixerTwoName = "ArtStationCementMixer_NorthB";
        public const string ConcreteBarrierName = "ArtStationConcreteBarrier_South";
        public const string ShelfAisleName = "ArtStationShelfAisle_West";
        public const string ShelfRedName = "ArtStationShelfRed_NorthWest";
        public const string ShelfWhiteName = "ArtStationShelfWhite_North";

        private const float FloorOffset = 0.045f;

        private readonly struct Placement
        {
            public Placement(
                string name,
                string assetPath,
                Vector3 anchor,
                Vector3 eulerAngles,
                float longestSide)
            {
                Name = name;
                AssetPath = assetPath;
                Anchor = anchor;
                EulerAngles = eulerAngles;
                LongestSide = longestSide;
            }

            public string Name { get; }
            public string AssetPath { get; }
            public Vector3 Anchor { get; }
            public Vector3 EulerAngles { get; }
            public float LongestSide { get; }
        }

        private static readonly Placement[] Placements =
        {
            new Placement(
                ForkliftName,
                ForkliftModelPath,
                new Vector3(-18.2f, 0f, -2.4f),
                new Vector3(0f, 90f, 0f),
                5.2f),
            new Placement(
                BenchGrinderName,
                BenchGrinderModelPath,
                new Vector3(23.0f, 0f, -6.4f),
                new Vector3(0f, 180f, 0f),
                1.8f),
            new Placement(
                CableReelName,
                CableReelModelPath,
                new Vector3(22.8f, 0f, -2.2f),
                new Vector3(0f, 0f, 0f),
                2.0f),
            new Placement(
                CementMixerOneName,
                CementMixerOneModelPath,
                new Vector3(7.6f, 0f, 18.0f),
                new Vector3(0f, 90f, 0f),
                2.2f),
            new Placement(
                CementMixerTwoName,
                CementMixerTwoModelPath,
                new Vector3(12.8f, 0f, 18.0f),
                new Vector3(0f, 90f, 0f),
                2.2f),
            new Placement(
                ConcreteBarrierName,
                ConcreteBarrierModelPath,
                new Vector3(8.2f, 0f, -10.8f),
                Vector3.zero,
                2.0f),
            new Placement(
                ShelfAisleName,
                ShelfAisleModelPath,
                new Vector3(-18.2f, 0f, 17.0f),
                new Vector3(0f, 180f, 0f),
                6.4f),
            new Placement(
                ShelfRedName,
                ShelfRedBackgroundModelPath,
                new Vector3(-10.0f, 0f, 17.0f),
                new Vector3(0f, 180f, 0f),
                6.0f),
            new Placement(
                ShelfWhiteName,
                ShelfWhiteBackgroundModelPath,
                new Vector3(-1.8f, 0f, 17.0f),
                new Vector3(0f, 180f, 0f),
                6.0f)
        };

        private static readonly StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        public static bool IsForkliftAvailable => HasAsset(ForkliftModelPath);
        public static bool IsEquipmentAvailable =>
            HasAsset(BenchGrinderModelPath) &&
            HasAsset(CableReelModelPath) &&
            HasAsset(CementMixerOneModelPath) &&
            HasAsset(CementMixerTwoModelPath) &&
            HasAsset(ConcreteBarrierModelPath);
        public static bool IsShelvingAvailable =>
            HasAsset(ShelfAisleModelPath) &&
            HasAsset(ShelfRedBackgroundModelPath) &&
            HasAsset(ShelfWhiteBackgroundModelPath);
        public static bool IsAvailable =>
            IsForkliftAvailable || IsEquipmentAvailable || IsShelvingAvailable;

        public static int BuildDefaultVisuals(
            Transform parent,
            Material fallbackMaterial = null)
        {
            if (parent == null || !IsAvailable)
                return 0;

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            MarkStatic(root);

            var material = EnsureIndustrialMaterial() ?? fallbackMaterial;
            var created = 0;
            foreach (var placement in Placements)
            {
                if (!HasAsset(placement.AssetPath))
                    continue;

                if (TryCreateNormalizedAsset(
                        root.transform,
                        placement,
                        material,
                        out _))
                {
                    created++;
                }
            }

            if (created == 0)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return 0;
            }

            return created;
        }

        private static bool TryCreateNormalizedAsset(
            Transform parent,
            Placement placement,
            Material material,
            out GameObject instance)
        {
            instance = null;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(placement.AssetPath);
            if (model == null || parent == null)
                return false;

            var visual = UnityEngine.Object.Instantiate(model);
            visual.name = placement.Name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(placement.EulerAngles);
            visual.transform.localScale = Vector3.one;

            StripNonVisualComponents(visual);
            ApplyProjectMaterial(visual, material);
            ConfigureRenderers(visual);

            if (!TryGetBounds(visual, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(visual);
                return false;
            }

            var largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest <= Mathf.Epsilon || placement.LongestSide <= Mathf.Epsilon)
            {
                UnityEngine.Object.DestroyImmediate(visual);
                return false;
            }

            visual.transform.localScale = Vector3.one * (placement.LongestSide / largest);
            if (!TryGetBounds(visual, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(visual);
                return false;
            }

            var target = parent.TransformPoint(placement.Anchor);
            visual.transform.position += new Vector3(
                target.x - bounds.center.x,
                parent.position.y + FloorOffset - bounds.min.y,
                target.z - bounds.center.z);
            MarkStatic(visual);
            instance = visual;
            return true;
        }

        private static Material EnsureIndustrialMaterial()
        {
            EnsureMaterialFolder();
            ConfigureTexture(AtlasTexturePath, TextureImporterType.Default, 1024, true);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "AA_ArtStationIndustrial"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture(
                "_BaseMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasTexturePath));
            material.SetFloat("_Metallic", 0.38f);
            material.SetFloat("_Smoothness", 0.30f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureMaterialFolder()
        {
            const string materialParent =
                "Assets/RailCraft/ThirdPerson/Art/Materials";
            if (!AssetDatabase.IsValidFolder(MaterialRootPath))
                AssetDatabase.CreateFolder(materialParent, "ArtAlpha");
        }

        private static void ConfigureTexture(
            string assetPath,
            TextureImporterType textureType,
            int maximumSize,
            bool sRgb)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
                return;

            var dirty = false;
            if (importer.textureType != textureType)
            {
                importer.textureType = textureType;
                dirty = true;
            }
            if (importer.maxTextureSize != maximumSize)
            {
                importer.maxTextureSize = maximumSize;
                dirty = true;
            }
            if (importer.sRGBTexture != sRgb)
            {
                importer.sRGBTexture = sRgb;
                dirty = true;
            }
            if (dirty)
                importer.SaveAndReimport();
        }

        private static bool HasAsset(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
        }

        private static void ApplyProjectMaterial(GameObject root, Material material)
        {
            if (root == null || material == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var count = Mathf.Max(1, renderer.sharedMaterials?.Length ?? 0);
                renderer.sharedMaterials = Enumerable.Repeat(material, count).ToArray();
            }
        }

        private static void ConfigureRenderers(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
                return false;

            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return true;
        }

        private static void StripNonVisualComponents(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(rigidbody);
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(animator);
            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
                UnityEngine.Object.DestroyImmediate(animation);
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                UnityEngine.Object.DestroyImmediate(camera);
            foreach (var light in root.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(light);
            foreach (var audio in root.GetComponentsInChildren<AudioSource>(true))
                UnityEngine.Object.DestroyImmediate(audio);
        }

        private static void MarkStatic(GameObject root)
        {
            if (root == null)
                return;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(item.gameObject, StaticFlags);
        }
    }
}
