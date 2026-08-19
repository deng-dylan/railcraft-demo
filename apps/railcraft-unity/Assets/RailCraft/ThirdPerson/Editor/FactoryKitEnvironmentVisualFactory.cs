using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Places a small, visual-only factory dressing layer in the whitebox
    /// environment.  Kenney's Factory Kit assets are intentionally kept
    /// separate from gameplay stations: this class owns only imported mesh
    /// instances and removes every runtime component that could participate in
    /// physics, animation, audio, or camera behaviour.
    /// </summary>
    public static class FactoryKitEnvironmentVisualFactory
    {
        public const string AssetRootPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/FactoryKit";

        public const string DecorationRootName = "FactoryKitDecorations";
        public const string LicenseAssetPath = AssetRootPath + "/KenneyFactoryKit-License.txt";

        public const string BoxLargeAssetPath = AssetRootPath + "/box-large.fbx";
        public const string CatwalkStairsAssetPath = AssetRootPath + "/catwalk-stairs.fbx";
        public const string CatwalkStraightAssetPath = AssetRootPath + "/catwalk-straight.fbx";
        public const string ConveyorLongAssetPath = AssetRootPath + "/conveyor-long.fbx";
        public const string CraneLiftAssetPath = AssetRootPath + "/crane-lift.fbx";
        public const string CraneAssetPath = AssetRootPath + "/crane.fbx";
        public const string MachineAssetPath = AssetRootPath + "/machine-fortified.fbx";
        public const string PipeBendAssetPath = AssetRootPath + "/pipe-large-bend.fbx";
        public const string PipeJunctionAssetPath = AssetRootPath + "/pipe-large-junction.fbx";
        public const string PipeLongAssetPath = AssetRootPath + "/pipe-large-long.fbx";
        public const string StructureHighAssetPath = AssetRootPath + "/structure-high.fbx";
        public const string StructureWallAssetPath = AssetRootPath + "/structure-wall.fbx";
        public const string WarningTrafficAssetPath = AssetRootPath + "/warning-traffic.fbx";

        private const float MinimumRendererScale = 0.001f;

        private readonly struct Placement
        {
            public Placement(
                string name,
                string assetPath,
                Vector3 localPosition,
                Vector3 localScale,
                Vector3 localEulerAngles,
                Material fallbackMaterial)
            {
                Name = name;
                AssetPath = assetPath;
                LocalPosition = localPosition;
                LocalScale = localScale;
                LocalEulerAngles = localEulerAngles;
                FallbackMaterial = fallbackMaterial;
            }

            public string Name { get; }
            public string AssetPath { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 LocalScale { get; }
            public Vector3 LocalEulerAngles { get; }
            public Material FallbackMaterial { get; }
        }

        /// <summary>
        /// True when at least one Factory Kit FBX has been imported into the
        /// project.  The scene builder can therefore remain usable when a
        /// checkout intentionally omits optional art content.
        /// </summary>
        public static bool IsKitAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(CraneAssetPath) != null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(MachineAssetPath) != null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(StructureWallAssetPath) != null;

        /// <summary>
        /// Adds a restrained set of high/back-wall decorations.  Positions are
        /// kept behind the commissioning loop and above the playable floor so
        /// the imported meshes cannot cover quiz stations, assembly tables, or
        /// the long landing rails.  Returns the number of successfully created
        /// FBX instances.
        /// </summary>
        public static int BuildDefaultDecorations(
            Transform parent,
            Material fallbackMaterial = null,
            Material accentMaterial = null)
        {
            if (parent == null || !IsKitAvailable)
                return 0;

            var root = new GameObject(DecorationRootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            MarkStatic(root);

            // All positions are in the existing 56 m × 42 m whitebox.  The
            // back-wall row starts at z=18.8 m; commissioning stations end at
            // z≈18.25 m, while the landing rails end at z≈16 m.  Elevated
            // catwalk/pipe pieces sit above y=4 m and never become walkable.
            var placements = new[]
            {
                new Placement(
                    "Crane_BackWall",
                    CraneAssetPath,
                    new Vector3(4.5f, 2.2f, 18.75f),
                    Vector3.one,
                    new Vector3(0f, 180f, 0f),
                    fallbackMaterial),
                new Placement(
                    "CraneLift_BackWall",
                    CraneLiftAssetPath,
                    new Vector3(4.5f, 5.3f, 17.55f),
                    Vector3.one,
                    Vector3.zero,
                    fallbackMaterial),
                new Placement(
                    "Machine_BackWest",
                    MachineAssetPath,
                    new Vector3(-15.5f, 0f, 19.2f),
                    Vector3.one,
                    new Vector3(0f, 180f, 0f),
                    fallbackMaterial),
                new Placement(
                    "Machine_BackEast",
                    MachineAssetPath,
                    new Vector3(15.5f, 0f, 19.2f),
                    Vector3.one,
                    Vector3.zero,
                    fallbackMaterial),
                new Placement(
                    "StructureWall_BackWest",
                    StructureWallAssetPath,
                    new Vector3(-18.0f, 0f, 19.25f),
                    Vector3.one,
                    new Vector3(0f, 90f, 0f),
                    fallbackMaterial),
                new Placement(
                    "StructureWall_BackEast",
                    StructureWallAssetPath,
                    new Vector3(18.0f, 0f, 19.25f),
                    Vector3.one,
                    new Vector3(0f, -90f, 0f),
                    fallbackMaterial),
                new Placement(
                    "Catwalk_BackLeft",
                    CatwalkStraightAssetPath,
                    new Vector3(-12.0f, 4.65f, 19.1f),
                    new Vector3(4.0f, 1f, 1f),
                    new Vector3(0f, 0f, 0f),
                    fallbackMaterial),
                new Placement(
                    "Catwalk_BackRight",
                    CatwalkStraightAssetPath,
                    new Vector3(12.0f, 4.65f, 19.1f),
                    new Vector3(4.0f, 1f, 1f),
                    new Vector3(0f, 0f, 0f),
                    fallbackMaterial),
                new Placement(
                    "CatwalkStairs_BackLeft",
                    CatwalkStairsAssetPath,
                    new Vector3(-15.0f, 3.8f, 18.65f),
                    Vector3.one,
                    new Vector3(0f, 180f, 0f),
                    fallbackMaterial),
                new Placement(
                    "CatwalkStairs_BackRight",
                    CatwalkStairsAssetPath,
                    new Vector3(15.0f, 3.8f, 18.65f),
                    Vector3.one,
                    Vector3.zero,
                    fallbackMaterial),
                new Placement(
                    "PipeRun_BackWall",
                    PipeLongAssetPath,
                    new Vector3(0f, 4.95f, 19.45f),
                    new Vector3(7.5f, 1f, 1f),
                    new Vector3(0f, 0f, 0f),
                    fallbackMaterial),
                new Placement(
                    "PipeBend_BackWest",
                    PipeBendAssetPath,
                    new Vector3(-8.0f, 4.75f, 19.25f),
                    Vector3.one,
                    new Vector3(0f, 90f, 0f),
                    fallbackMaterial),
                new Placement(
                    "PipeBend_BackEast",
                    PipeBendAssetPath,
                    new Vector3(8.0f, 4.75f, 19.25f),
                    Vector3.one,
                    new Vector3(0f, -90f, 0f),
                    fallbackMaterial),
                new Placement(
                    "Box_BackWest",
                    BoxLargeAssetPath,
                    new Vector3(-14.0f, 0f, 18.75f),
                    Vector3.one,
                    new Vector3(0f, 90f, 0f),
                    fallbackMaterial),
                new Placement(
                    "Box_BackEast",
                    BoxLargeAssetPath,
                    new Vector3(14.0f, 0f, 18.75f),
                    Vector3.one,
                    new Vector3(0f, -90f, 0f),
                    fallbackMaterial),
                new Placement(
                    "Warning_BackWest",
                    WarningTrafficAssetPath,
                    new Vector3(-13.0f, 0f, 18.55f),
                    Vector3.one,
                    new Vector3(0f, 180f, 0f),
                    accentMaterial ?? fallbackMaterial),
                new Placement(
                    "Warning_BackEast",
                    WarningTrafficAssetPath,
                    new Vector3(13.0f, 0f, 18.55f),
                    Vector3.one,
                    Vector3.zero,
                    accentMaterial ?? fallbackMaterial)
            };

            var created = 0;
            foreach (var placement in placements)
            {
                if (TryCreateAsset(
                        root.transform,
                        placement.Name,
                        placement.AssetPath,
                        placement.LocalPosition,
                        Quaternion.Euler(placement.LocalEulerAngles),
                        placement.LocalScale,
                        placement.FallbackMaterial,
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

        /// <summary>
        /// Instantiates one imported FBX as a static visual-only object.
        /// </summary>
        public static bool TryCreateAsset(
            Transform parent,
            string name,
            string assetPath,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material fallbackMaterial,
            out GameObject instance)
        {
            instance = null;
            if (parent == null || string.IsNullOrWhiteSpace(assetPath))
                return false;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning($"Factory Kit asset was not imported: {assetPath}");
                return false;
            }

            var visual = UnityEngine.Object.Instantiate(model);
            visual.name = string.IsNullOrWhiteSpace(name) ? model.name : name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = SanitizeScale(localScale);

            StripNonVisualComponents(visual);
            ApplyProjectMaterial(visual, fallbackMaterial);
            ConfigureRenderers(visual);
            MarkStatic(visual);

            if (visual.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(visual);
                return false;
            }

            instance = visual;
            return true;
        }

        private static Vector3 SanitizeScale(Vector3 scale)
        {
            return new Vector3(
                Mathf.Abs(scale.x) < MinimumRendererScale ? 1f : scale.x,
                Mathf.Abs(scale.y) < MinimumRendererScale ? 1f : scale.y,
                Mathf.Abs(scale.z) < MinimumRendererScale ? 1f : scale.z);
        }

        private static void ApplyProjectMaterial(GameObject root, Material projectMaterial)
        {
            if (root == null || projectMaterial == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var count = Mathf.Max(1, materials == null ? 0 : materials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(projectMaterial, count).ToArray();
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
            {
                GameObjectUtility.SetStaticEditorFlags(
                    item.gameObject,
                    StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.ReflectionProbeStatic);
            }
        }
    }
}
