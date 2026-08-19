using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Creates visual-only assembly proxies from the teammate bogie FBX.
    /// Gameplay identifiers, snap slots and completion rules remain owned by
    /// the whitebox domain; this class only replaces generated primitive art.
    /// </summary>
    public static class BogieAssemblyDemoVisualFactory
    {
        public const string ModelAssetPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/BogieAssemblyDemo.fbx";

        public const string CarbodyModelAssetPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/FuxingCarbodyAssemblyDemo.fbx";

        public const string ModelRootName = "BogieAssemblyDemoRoot";
        public const string DemonstrationNotice =
            "结构示范件｜用于组装流程演示，外形与尺寸不代表 SWM-400E1 最终工程件";

        private const string ContentName = "DemonstrationModelContent";
        private const string RailContactAnchorName = "RailContactPlane";
        private const float PartDisplaySize = 1.05f;
        // A full intermediate coach is about 25.7 m long.  Keep the
        // assembly-input preview large enough to read as a coach while
        // leaving the small input station usable; the completed landing
        // display uses the unscaled 1:1 reference geometry.
        private const float CarbodyPartDisplayLength = 7.5f;

        private static readonly string[] FixedDriveObjects =
        {
            "Motor_L", "Motor_R", "Gearbox_F", "Gearbox_R", "DriveShaft_F", "DriveShaft_R"
        };

        private static readonly IReadOnlyDictionary<PartId, string[]> PartObjects =
            new Dictionary<PartId, string[]>
            {
                { PartId.Axle, new[] { "Axle_F", "Axle_R" } },
                { PartId.Wheel, new[] { "Wheels_F", "Wheels_R" } },
                {
                    PartId.Bearing,
                    new[] { "Axlebox_FL", "Axlebox_FR", "Axlebox_RL", "Axlebox_RR" }
                },
                {
                    PartId.BrakeDevice,
                    new[]
                    {
                        "BrakeDiscs_F", "BrakeDiscs_R",
                        "Caliper_FL", "Caliper_FR", "Caliper_RL", "Caliper_RR"
                    }
                },
                {
                    PartId.PrimaryElasticElement,
                    new[] { "Spring_FL", "Spring_FR", "Spring_RL", "Spring_RR" }
                },
                {
                    PartId.PrimaryDamper,
                    new[] { "DamperV_FL", "DamperV_FR", "DamperV_RL", "DamperV_RR" }
                },
                { PartId.SecondaryElasticElement, new[] { "AirSpring_L", "AirSpring_R" } },
                {
                    PartId.SecondaryDamper,
                    new[] { "DamperY_L", "DamperY_R", "DamperT_C" }
                },
                { PartId.CentralTractionDevice, new[] { "Traction" } }
            };

        private static readonly IReadOnlyDictionary<ModuleId, string[]> ModuleObjects =
            new Dictionary<ModuleId, string[]>
            {
                {
                    ModuleId.WheelsetAxlebox,
                    new[]
                    {
                        "Axle_F", "Axle_R", "Wheels_F", "Wheels_R",
                        "Axlebox_FL", "Axlebox_FR", "Axlebox_RL", "Axlebox_RR"
                    }
                },
                {
                    ModuleId.Frame,
                    new[]
                    {
                        "Frame", "BrakeDiscs_F", "BrakeDiscs_R",
                        "Caliper_FL", "Caliper_FR", "Caliper_RL", "Caliper_RR"
                    }
                },
                {
                    ModuleId.PrimarySuspension,
                    new[]
                    {
                        "Spring_FL", "Spring_FR", "Spring_RL", "Spring_RR",
                        "DamperV_FL", "DamperV_FR", "DamperV_RL", "DamperV_RR"
                    }
                },
                {
                    ModuleId.SecondarySuspension,
                    new[] { "AirSpring_L", "AirSpring_R", "DamperY_L", "DamperY_R", "DamperT_C" }
                },
                {
                    ModuleId.BogieStructure,
                    new[]
                    {
                        "Axle_F", "Axle_R", "Wheels_F", "Wheels_R",
                        "Axlebox_FL", "Axlebox_FR", "Axlebox_RL", "Axlebox_RR",
                        "Frame", "BrakeDiscs_F", "BrakeDiscs_R",
                        "Caliper_FL", "Caliper_FR", "Caliper_RL", "Caliper_RR",
                        "Spring_FL", "Spring_FR", "Spring_RL", "Spring_RR",
                        "DamperV_FL", "DamperV_FR", "DamperV_RL", "DamperV_RR",
                        "Motor_L", "Motor_R", "Gearbox_F", "Gearbox_R",
                        "DriveShaft_F", "DriveShaft_R"
                    }
                }
            };

        public static bool IsModelAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) != null;

        public static bool IsCarbodyModelAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(CarbodyModelAssetPath) != null;

        public static bool UsesDemonstrationGeometry(PartId partId) =>
            partId == PartId.Carbody || PartObjects.ContainsKey(partId);

        public static bool UsesDemonstrationGeometry(ModuleId moduleId) =>
            ModuleObjects.ContainsKey(moduleId);

        public static bool TryCreatePartVisual(
            Transform parent,
            string name,
            PartId partId,
            Material material,
            out GameObject root)
        {
            root = null;
            if (partId == PartId.Carbody)
            {
                return TryCreateCarbodyVisual(
                    parent,
                    name,
                    material,
                    CarbodyPartDisplayLength,
                    out root);
            }

            return PartObjects.TryGetValue(partId, out var objectNames)
                && TryCreateSelection(
                    parent,
                    name,
                    objectNames,
                    material,
                    alignToRailContact: false,
                    fitSize: PartDisplaySize,
                    out root);
        }

        /// <summary>
        /// Creates the extracted intermediate Fuxing coach as a visual-only
        /// assembly input.  A positive display length applies uniform scaling;
        /// zero keeps the imported coach at its normalized 1:1 dimensions.
        /// </summary>
        public static bool TryCreateCarbodyVisual(
            Transform parent,
            string name,
            Material material,
            float displayLength,
            out GameObject root)
        {
            root = null;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CarbodyModelAssetPath);
            if (model == null || parent == null)
                return false;

            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "FuxingCarbodyAssemblyDemo_SourceInstance";
            instance.transform.SetParent(parent, false);
            ResetTransform(instance.transform);

            var renderers = instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .ToArray();
            if (renderers.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            var rootObject = new GameObject(name);
            rootObject.transform.SetParent(parent, false);
            ResetTransform(rootObject.transform);
            var content = new GameObject("CarbodyReferenceContent");
            content.transform.SetParent(rootObject.transform, false);
            ResetTransform(content.transform);

            foreach (var renderer in renderers)
                renderer.transform.SetParent(content.transform, true);

            // The extracted FBX is authored in Blender's X-lateral/Y-up/Z-
            // longitudinal frame. Unity's FBX importer adds its conventional
            // -90 degree X conversion to the imported hierarchy, which puts
            // the coach length on Unity Y. Apply the correction after the
            // world-preserving reparent so child transforms do not cancel it.
            // The assembly scene then consistently uses X=width, Y=height and
            // Z=length without mutating the source asset.
            content.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            UnityEngine.Object.DestroyImmediate(instance);
            StripNonVisualComponents(rootObject);
            ApplyMaterial(rootObject, material);

            // Measure in the identity-rotation root frame.  Measuring from the
            // corrected child would express the coach back in its imported
            // local axes (length on Y), defeating the Z-length fit below.
            if (!TryCalculateLocalBounds(rootObject.transform, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                return false;
            }

            var scale = displayLength > Mathf.Epsilon
                ? displayLength / Mathf.Max(bounds.size.z, Mathf.Epsilon)
                : 1f;
            content.transform.localScale = Vector3.one * scale;
            content.transform.localPosition = new Vector3(
                -bounds.center.x * scale,
                -bounds.min.y * scale,
                -bounds.center.z * scale);

            root = rootObject;
            return true;
        }

        public static bool TryCreateModuleVisual(
            Transform parent,
            string name,
            ModuleId moduleId,
            Material material,
            bool preserveAssemblyCoordinates,
            out GameObject root)
        {
            root = null;
            return ModuleObjects.TryGetValue(moduleId, out var objectNames)
                && TryCreateSelection(
                    parent,
                    name,
                    objectNames,
                    material,
                    alignToRailContact: preserveAssemblyCoordinates,
                    fitSize: preserveAssemblyCoordinates ? 0f : 1.8f,
                    out root);
        }

        public static bool TryCreateFixedDriveVisual(
            Transform parent,
            string name,
            Material material,
            out GameObject root)
        {
            return TryCreateSelection(
                parent,
                name,
                FixedDriveObjects,
                material,
                alignToRailContact: true,
                fitSize: 0f,
                out root);
        }

        public static bool TryCreateCompletedBogieVisual(
            Transform parent,
            string name,
            Material material,
            out GameObject root)
        {
            var objectNames = ModuleObjects[ModuleId.BogieStructure]
                .Concat(ModuleObjects[ModuleId.SecondarySuspension])
                .Concat(PartObjects[PartId.CentralTractionDevice])
                .Distinct()
                .ToArray();
            return TryCreateSelection(
                parent,
                name,
                objectNames,
                material,
                alignToRailContact: true,
                fitSize: 0f,
                out root);
        }

        private static bool TryCreateSelection(
            Transform parent,
            string name,
            IReadOnlyCollection<string> objectNames,
            Material material,
            bool alignToRailContact,
            float fitSize,
            out GameObject root)
        {
            root = null;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (model == null || parent == null || objectNames == null || objectNames.Count == 0)
                return false;

            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "BogieAssemblyDemo_SourceInstance";
            instance.transform.SetParent(parent, false);
            ResetTransform(instance.transform);

            var selected = new List<Transform>(objectNames.Count);
            foreach (var objectName in objectNames)
            {
                var match = FindDescendant(instance.transform, objectName);
                if (match == null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                    Debug.LogWarning(
                        $"Bogie assembly demonstration FBX is missing '{objectName}'. " +
                        "The generated primitive visual will be used instead.");
                    return false;
                }
                selected.Add(match);
            }

            var railAnchor = FindDescendant(instance.transform, RailContactAnchorName);
            var rootObject = new GameObject(name);
            rootObject.transform.SetParent(parent, false);
            ResetTransform(rootObject.transform);

            var content = new GameObject(ContentName);
            content.transform.SetParent(rootObject.transform, false);
            ResetTransform(content.transform);

            foreach (var item in selected.Distinct())
                item.SetParent(content.transform, true);

            Vector3? railContactLocal = null;
            if (railAnchor != null)
                railContactLocal = content.transform.InverseTransformPoint(railAnchor.position);

            UnityEngine.Object.DestroyImmediate(instance);
            StripNonVisualComponents(rootObject);
            ApplyMaterial(rootObject, material);

            if (!TryCalculateLocalBounds(content.transform, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                return false;
            }

            if (alignToRailContact && railContactLocal.HasValue)
            {
                content.transform.localPosition = -railContactLocal.Value;
            }
            else
            {
                var largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                var scale = largestDimension > Mathf.Epsilon && fitSize > Mathf.Epsilon
                    ? Mathf.Clamp(fitSize / largestDimension, 0.12f, 5f)
                    : 1f;
                content.transform.localScale = Vector3.one * scale;
                content.transform.localPosition = new Vector3(
                    -bounds.center.x * scale,
                    -bounds.min.y * scale,
                    -bounds.center.z * scale);
            }

            root = rootObject;
            return true;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            if (root == null || material == null)
                return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var count = Mathf.Max(1, renderer.sharedMaterials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(material, count).ToArray();
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
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                UnityEngine.Object.DestroyImmediate(source);
        }

        private static bool TryCalculateLocalBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var initialized = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var worldBounds = renderer.bounds;
                var minimum = worldBounds.min;
                var maximum = worldBounds.max;
                for (var x = 0; x <= 1; x++)
                for (var y = 0; y <= 1; y++)
                for (var z = 0; z <= 1; z++)
                {
                    var worldPoint = new Vector3(
                        x == 0 ? minimum.x : maximum.x,
                        y == 0 ? minimum.y : maximum.y,
                        z == 0 ? minimum.z : maximum.z);
                    var localPoint = root.InverseTransformPoint(worldPoint);
                    if (!initialized)
                    {
                        bounds = new Bounds(localPoint, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(localPoint);
                    }
                }
            }
            return initialized;
        }

        private static Transform FindDescendant(Transform root, string exactName)
        {
            if (root == null)
                return null;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(item.name, exactName, StringComparison.Ordinal))
                    return item;
            }
            return null;
        }

        private static void ResetTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
