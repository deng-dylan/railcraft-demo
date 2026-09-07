using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Deterministic first-art-pass dressing built from the curated third-party
    /// source directory. All instances are visual-only and can be regenerated.
    /// </summary>
    public static class ArtAlphaEnvironmentVisualFactory
    {
        public const string RootName = "ArtAlphaEnvironment";
        public const string CraneName = "OverheadCrane_MainBay";
        public const string FreightDisplayName = "MaintenanceFlatbed_WestBay";
        public const string Container40Name = "Container40F_WestStorage";
        public const string Container20Name = "Container20F_WestStorage";
        public const string SubstationName = "ElectricalSubstation_NorthEast";

        public const string CraneModelPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/OverheadCrane/Models/Overhead_Crane.fbx";
        public const string CraneBaseMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/OverheadCrane/Textures/Crane_Diff.jpg";
        public const string CraneNormalMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/OverheadCrane/Textures/Crane_NRM.jpg";
        public const string KenneyTrackPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/KenneyTrainKit/Models/track-detailed.fbx";
        public const string KenneyFlatbedPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/KenneyTrainKit/Models/train-carriage-flatbed.fbx";
        public const string KenneyColorMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/KenneyTrainKit/Textures/colormap.png";
        public const string FloorLargePath =
            "Assets/RailCraft/ThirdPerson/Art/Models/FactoryKit/floor-large.fbx";
        public const string Container20ModelPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ShippingContainers/Models/Container20F.fbx";
        public const string Container40ModelPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ShippingContainers/Models/Container40F.fbx";
        public const string ContainerBlueBaseMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ShippingContainers/Textures/ContainerBlue2K_basecolor.tga";
        public const string ContainerBeigeBaseMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ShippingContainers/Textures/ContainerBeige2K_basecolor.tga";
        public const string ContainerNormalMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ShippingContainers/Textures/Container2K_Clean_normal.tga";
        public const string ContainerMaskMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ShippingContainers/Textures/Container2K_Clean_mask.tga";
        public const string SubstationModelPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ElectricalSubstation/Meshes/SM_Electrical_Substation.fbx";
        public const string SubstationBaseMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ElectricalSubstation/Textures/T_Electrical_Substation_BaseColor.tga";
        public const string SubstationNormalMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ElectricalSubstation/Textures/T_Electrical_Substation_Normal.tga";
        public const string SubstationOcclusionMapPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ElectricalSubstation/Textures/T_Electrical_Substation_Occlusion.tga";

        public const string MaterialRootPath =
            "Assets/RailCraft/ThirdPerson/Art/Materials/ArtAlpha";
        public const string CraneMaterialPath = MaterialRootPath + "/AA_OverheadCrane.mat";
        public const string KenneyMaterialPath = MaterialRootPath + "/AA_KenneyTrainKit.mat";
        public const string ContainerBlueMaterialPath = MaterialRootPath + "/AA_ContainerBlue.mat";
        public const string ContainerBeigeMaterialPath = MaterialRootPath + "/AA_ContainerBeige.mat";
        public const string SubstationMaterialPath = MaterialRootPath + "/AA_ElectricalSubstation.mat";

        private const int TrackSectionCount = 5;
        private const float CraneTargetSpan = 43f;

        private static readonly StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        public static bool IsCraneAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(CraneModelPath) != null;

        public static bool IsRailKitAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(KenneyTrackPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(KenneyFlatbedPath) != null;

        public static bool IsFloorKitAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(FloorLargePath) != null;

        public static bool AreContainersAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(Container20ModelPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(Container40ModelPath) != null;

        public static bool IsSubstationAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(SubstationModelPath) != null;

        public static int BuildDefaultVisuals(
            Transform parent,
            Material fallbackSteel = null,
            Material fallbackFloor = null)
        {
            if (parent == null ||
                (!IsCraneAvailable && !IsRailKitAvailable &&
                 !IsFloorKitAvailable && !AreContainersAvailable && !IsSubstationAvailable))
                return 0;

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            MarkStatic(root);

            var created = 0;
            if (IsCraneAvailable)
            {
                var material = EnsureCraneMaterial() ?? fallbackSteel;
                if (TryCreateCrane(root.transform, material, out _))
                    created++;
            }

            if (IsRailKitAvailable)
            {
                var material = EnsureKenneyMaterial() ?? fallbackFloor ?? fallbackSteel;
                created += BuildWestMaintenanceDisplay(root.transform, material);
            }

            if (IsFloorKitAvailable)
                created += BuildFactoryFloorFinish(root.transform, fallbackFloor);

            if (AreContainersAvailable)
                created += BuildWestContainerStorage(root.transform);

            if (IsSubstationAvailable && BuildNorthEastSubstation(root.transform))
                created++;

            if (created == 0)
                UnityEngine.Object.DestroyImmediate(root);
            return created;
        }

        private static int BuildFactoryFloorFinish(Transform parent, Material fallbackFloor)
        {
            // floor-large uses the Factory Kit atlas UVs. The atlas is not a
            // floor texture, so use the project's authored floor material for
            // the hall surface and keep the mesh only for panel seams.
            var material = fallbackFloor ?? EnsureKenneyMaterial();
            var floorRoot = new GameObject("FactoryFloorFinish");
            floorRoot.transform.SetParent(parent, false);
            floorRoot.transform.localPosition = Vector3.zero;
            floorRoot.transform.localRotation = Quaternion.identity;
            floorRoot.transform.localScale = Vector3.one;
            MarkStatic(floorRoot);

            var created = 0;
            // Four large panels keep the Factory Kit UVs readable while covering
            // the complete 56 m x 42 m hall floor with a single visual layer.
            foreach (var placement in new[]
            {
                (new Vector3(-14f, 0.006f, -10.5f), new Vector3(14f, 1f, 10.5f)),
                (new Vector3(14f, 0.006f, -10.5f), new Vector3(14f, 1f, 10.5f)),
                (new Vector3(-14f, 0.006f, 10.5f), new Vector3(14f, 1f, 10.5f)),
                (new Vector3(14f, 0.006f, 10.5f), new Vector3(14f, 1f, 10.5f))
            })
            {
                if (FactoryKitEnvironmentVisualFactory.TryCreateAsset(
                        floorRoot.transform,
                        $"FloorPanel_{created + 1}",
                        FloorLargePath,
                        placement.Item1,
                        Quaternion.identity,
                        placement.Item2,
                        material,
                        out _))
                {
                    created++;
                }
            }

            if (created == 0)
            {
                UnityEngine.Object.DestroyImmediate(floorRoot);
                return 0;
            }

            return created;
        }

        public static bool TryCreateCrane(
            Transform parent,
            Material material,
            out GameObject crane)
        {
            crane = InstantiateVisual(CraneModelPath, parent, CraneName, material);
            if (crane == null)
                return false;

            crane.transform.localPosition = Vector3.zero;
            crane.transform.localRotation = Quaternion.identity;
            crane.transform.localScale = Vector3.one;

            if (!TryGetBounds(crane, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(crane);
                crane = null;
                return false;
            }

            // The source FBX stores its bridge span on source Y. Unity therefore
            // imports that dimension vertically. Detect the longest dimension
            // instead of assuming an authoring axis. The source's Z dimension
            // is physical up, so remap source Y -> factory X, source Z ->
            // factory Y, and source X -> factory Z before fitting the span.
            if (bounds.size.y >= bounds.size.x && bounds.size.y >= bounds.size.z)
                crane.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.right);
            else if (bounds.size.z > bounds.size.x)
                crane.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            if (!TryGetBounds(crane, out bounds) || bounds.size.x <= 0.001f)
            {
                UnityEngine.Object.DestroyImmediate(crane);
                crane = null;
                return false;
            }

            var uniformScale = CraneTargetSpan / bounds.size.x;
            crane.transform.localScale = Vector3.one * uniformScale;
            if (!TryGetBounds(crane, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(crane);
                crane = null;
                return false;
            }

            // Park the trolley over the north inspection bay. This keeps its
            // bridge and hanging cable behind the bogie/landing hero views while
            // retaining the crane in the player's forward traversal sightline.
            var desiredCenter = parent.position + new Vector3(0f, 0f, 12.5f);
            crane.transform.position += new Vector3(
                desiredCenter.x - bounds.center.x,
                parent.position.y + 0.02f - bounds.min.y,
                desiredCenter.z - bounds.center.z);
            MarkStatic(crane);
            return true;
        }

        private static int BuildWestMaintenanceDisplay(Transform parent, Material material)
        {
            var created = 0;
            var displayRoot = new GameObject("WestMaintenanceRailDisplay");
            displayRoot.transform.SetParent(parent, false);
            displayRoot.transform.localPosition = Vector3.zero;
            displayRoot.transform.localRotation = Quaternion.identity;
            displayRoot.transform.localScale = Vector3.one;
            MarkStatic(displayRoot);

            for (var index = 0; index < TrackSectionCount; index++)
            {
                var track = InstantiateVisual(
                    KenneyTrackPath,
                    displayRoot.transform,
                    $"ServiceTrack_{index + 1:00}",
                    material);
                if (track == null)
                    continue;

                track.transform.localPosition = new Vector3(-23f, 0.04f, 5f + index * 2.2f);
                track.transform.localRotation = Quaternion.identity;
                track.transform.localScale = Vector3.one * 2.2f;
                AlignBottom(track, 0.04f);
                MarkStatic(track);
                created++;
            }

            var flatbed = InstantiateVisual(
                KenneyFlatbedPath,
                displayRoot.transform,
                FreightDisplayName,
                material);
            if (flatbed != null)
            {
                flatbed.transform.localPosition = new Vector3(-23f, 0.08f, 9.4f);
                flatbed.transform.localRotation = Quaternion.identity;
                flatbed.transform.localScale = Vector3.one * 2.2f;
                AlignBottom(flatbed, 0.16f);
                MarkStatic(flatbed);
                created++;
            }

            if (created == 0)
                UnityEngine.Object.DestroyImmediate(displayRoot);
            return created;
        }

        private static int BuildWestContainerStorage(Transform parent)
        {
            var blueMaterial = EnsureContainerMaterial(
                ContainerBlueMaterialPath,
                ContainerBlueBaseMapPath);
            var beigeMaterial = EnsureContainerMaterial(
                ContainerBeigeMaterialPath,
                ContainerBeigeBaseMapPath);
            var created = 0;

            if (TryCreateContainer(
                    parent,
                    Container40Name,
                    Container40ModelPath,
                    new Vector3(2.44f, 2.59f, 12.19f),
                    new Vector3(-25f, 0f, -9.5f),
                    blueMaterial,
                    out _))
            {
                created++;
            }

            if (TryCreateContainer(
                    parent,
                    Container20Name,
                    Container20ModelPath,
                    new Vector3(2.44f, 2.59f, 6.06f),
                    new Vector3(-25f, 0f, 0f),
                    beigeMaterial,
                    out _))
            {
                created++;
            }

            return created;
        }

        private static bool TryCreateContainer(
            Transform parent,
            string name,
            string modelPath,
            Vector3 targetSize,
            Vector3 localPosition,
            Material material,
            out GameObject anchor)
        {
            anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = localPosition;
            anchor.transform.localRotation = Quaternion.identity;
            anchor.transform.localScale = Vector3.one;

            var visual = InstantiateVisual(modelPath, anchor.transform, name + "_Visual", material);
            if (visual == null || !TryGetBounds(visual, out var sourceBounds))
            {
                UnityEngine.Object.DestroyImmediate(anchor);
                anchor = null;
                return false;
            }

            var sourceSize = sourceBounds.size;
            if (sourceSize.y >= sourceSize.x && sourceSize.y >= sourceSize.z)
            {
                // Source Y is container length and source Z is height.
                visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.transform.localScale = new Vector3(
                    targetSize.x / sourceSize.x,
                    targetSize.z / sourceSize.y,
                    targetSize.y / sourceSize.z);
            }
            else if (sourceSize.x >= sourceSize.y && sourceSize.x >= sourceSize.z)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                visual.transform.localScale = new Vector3(
                    targetSize.z / sourceSize.x,
                    targetSize.y / sourceSize.y,
                    targetSize.x / sourceSize.z);
            }
            else
            {
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = new Vector3(
                    targetSize.x / sourceSize.x,
                    targetSize.y / sourceSize.y,
                    targetSize.z / sourceSize.z);
            }

            CenterOnAnchorAndFloor(visual, anchor.transform.position.y);
            var collider = anchor.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, targetSize.y * 0.5f, 0f);
            collider.size = targetSize;
            MarkStatic(anchor);
            return true;
        }

        private static bool BuildNorthEastSubstation(Transform parent)
        {
            var material = EnsureSubstationMaterial();
            var anchor = new GameObject(SubstationName);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(22.5f, 0f, 15f);
            anchor.transform.localRotation = Quaternion.identity;
            anchor.transform.localScale = Vector3.one;

            var visual = InstantiateVisual(
                SubstationModelPath,
                anchor.transform,
                SubstationName + "_Visual",
                material);
            if (visual == null || !TryGetBounds(visual, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(anchor);
                return false;
            }

            const float targetLongestSide = 9.8f;
            var largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest <= 0.001f)
            {
                UnityEngine.Object.DestroyImmediate(anchor);
                return false;
            }
            visual.transform.localScale = Vector3.one * (targetLongestSide / largest);
            CenterOnAnchorAndFloor(visual, anchor.transform.position.y);
            if (!TryGetBounds(visual, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(anchor);
                return false;
            }

            var collider = anchor.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, bounds.size.y * 0.5f, 0f);
            collider.size = bounds.size;
            MarkStatic(anchor);
            return true;
        }

        private static GameObject InstantiateVisual(
            string assetPath,
            Transform parent,
            string name,
            Material material)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null || parent == null)
                return null;

            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            StripNonVisualComponents(instance);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return null;
            }

            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                if (material != null)
                {
                    var slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    renderer.sharedMaterials = Enumerable.Repeat(material, slotCount).ToArray();
                }
            }

            return instance;
        }

        private static Material EnsureCraneMaterial()
        {
            EnsureMaterialFolder();
            ConfigureTexture(CraneBaseMapPath, TextureImporterType.Default, 2048);
            ConfigureTexture(CraneNormalMapPath, TextureImporterType.NormalMap, 2048);

            var material = EnsureUrpLitMaterial(CraneMaterialPath);
            if (material == null)
                return null;

            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(CraneBaseMapPath);
            var normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>(CraneNormalMapPath);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", baseMap);
            material.SetFloat("_Metallic", 0.62f);
            material.SetFloat("_Smoothness", 0.32f);
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureKenneyMaterial()
        {
            EnsureMaterialFolder();
            ConfigureTexture(KenneyColorMapPath, TextureImporterType.Default, 1024);
            var material = EnsureUrpLitMaterial(KenneyMaterialPath);
            if (material == null)
                return null;

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture(
                "_BaseMap",
                AssetDatabase.LoadAssetAtPath<Texture2D>(KenneyColorMapPath));
            material.SetFloat("_Metallic", 0.12f);
            material.SetFloat("_Smoothness", 0.24f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureContainerMaterial(string materialPath, string baseMapPath)
        {
            EnsureMaterialFolder();
            ConfigureTexture(baseMapPath, TextureImporterType.Default, 2048);
            ConfigureTexture(ContainerNormalMapPath, TextureImporterType.NormalMap, 2048);
            ConfigureTexture(ContainerMaskMapPath, TextureImporterType.Default, 2048, false);
            var material = EnsureUrpLitMaterial(materialPath);
            if (material == null)
                return null;

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath));
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(ContainerNormalMapPath);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetFloat("_Metallic", 0.62f);
            material.SetFloat("_Smoothness", 0.28f);
            if (normal != null)
                material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureSubstationMaterial()
        {
            EnsureMaterialFolder();
            ConfigureTexture(SubstationBaseMapPath, TextureImporterType.Default, 2048);
            ConfigureTexture(SubstationNormalMapPath, TextureImporterType.NormalMap, 2048);
            ConfigureTexture(SubstationOcclusionMapPath, TextureImporterType.Default, 2048, false);
            var material = EnsureUrpLitMaterial(SubstationMaterialPath);
            if (material == null)
                return null;

            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SubstationBaseMapPath));
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(SubstationNormalMapPath);
            var occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(SubstationOcclusionMapPath);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_OcclusionMap", occlusion);
            material.SetFloat("_BumpScale", 1f);
            material.SetFloat("_OcclusionStrength", 0.85f);
            material.SetFloat("_Metallic", 0.48f);
            material.SetFloat("_Smoothness", 0.26f);
            if (normal != null)
                material.EnableKeyword("_NORMALMAP");
            if (occlusion != null)
                material.EnableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureUrpLitMaterial(string assetPath)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                return null;

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(assetPath)
            };
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void EnsureMaterialFolder()
        {
            const string materialParent = "Assets/RailCraft/ThirdPerson/Art/Materials";
            if (!AssetDatabase.IsValidFolder(MaterialRootPath))
                AssetDatabase.CreateFolder(materialParent, "ArtAlpha");
        }

        private static void ConfigureTexture(
            string assetPath,
            TextureImporterType textureType,
            int maximumSize,
            bool? sRgb = null)
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
            if (sRgb.HasValue && importer.sRGBTexture != sRgb.Value)
            {
                importer.sRGBTexture = sRgb.Value;
                dirty = true;
            }
            if (dirty)
                importer.SaveAndReimport();
        }

        private static void CenterOnAnchorAndFloor(GameObject visual, float floorY)
        {
            if (!TryGetBounds(visual, out var bounds))
                return;
            var anchor = visual.transform.parent;
            visual.transform.position += new Vector3(
                anchor.position.x - bounds.center.x,
                floorY - bounds.min.y,
                anchor.position.z - bounds.center.z);
        }

        private static void AlignBottom(GameObject instance, float worldY)
        {
            if (TryGetBounds(instance, out var bounds))
                instance.transform.position += Vector3.up * (worldY - bounds.min.y);
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
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
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
            foreach (var audioSource in root.GetComponentsInChildren<AudioSource>(true))
                UnityEngine.Object.DestroyImmediate(audioSource);
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
