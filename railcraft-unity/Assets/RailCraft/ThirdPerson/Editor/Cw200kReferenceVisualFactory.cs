using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Creates visual-only completed-bogie references from the inspected
    /// CW-200K passenger-bogie CAD conversion. Gameplay still uses the
    /// established whitebox domain and semantic assembly steps.
    /// </summary>
    public static class Cw200kReferenceVisualFactory
    {
        public const string ModelAssetPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/CW200KReference.fbx";

        public const string ModelRootName = "CW200KReference";
        public const string RailContactAnchorName = "RailContactPlane";
        public const float ReferenceWidthMetres = 2.89814f;
        public const float ReferenceLengthMetres = 3.518641f;
        public const float ReferenceHeightMetres = 1.006507f;
        public const string DemonstrationNotice =
            "CW-200K 客车转向架参考件｜用于完成态视觉提升，不代表 CR400AF 工程模型";
        public const string TractionRodGroupToken = "QIANYINLAGAN";

        private const string ReferencePartContentName = "CW200KReferencePartContent";
        private const float ReferencePartDisplaySize = 1.05f;

        public static bool IsModelAvailable =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) != null;

        public static bool TryCreateCompletedVisual(
            Transform parent,
            string name,
            out GameObject root)
        {
            root = null;
            if (parent == null)
                return false;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (model == null)
                return false;

            var rootObject = new GameObject(name);
            rootObject.transform.SetParent(parent, false);
            ResetTransform(rootObject.transform);

            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "CW200KReference_SourceInstance";
            instance.transform.SetParent(rootObject.transform, false);
            // Keep the FBX importer root rotation and scale. Clearing that
            // wrapper would turn the model's longitudinal axis into Unity Y.
            instance.transform.localPosition = Vector3.zero;
            StripNonVisualComponents(instance);

            var renderers = instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .ToArray();
            if (renderers.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                return false;
            }

            // Unity already imports this FBX as X=lateral, Y=up and
            // Z=vehicle-forward. The empty-anchor transform carries the FBX
            // axis-conversion wrapper, so renderer bounds are the reliable
            // final datum for rail contact and centring.
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            var localMinimum = rootObject.transform.InverseTransformPoint(bounds.min);
            var localCenter = rootObject.transform.InverseTransformPoint(bounds.center);
            instance.transform.localPosition += new Vector3(
                -localCenter.x,
                -localMinimum.y,
                -localCenter.z);

            root = rootObject;
            return true;
        }

        /// <summary>
        /// Extracts only component groups whose identity is explicit in the
        /// hierarchy-preserving CW-200K conversion. Ambiguous parts continue to
        /// use the existing semantic fallback until an engineering mapping is
        /// supplied.
        /// </summary>
        public static bool TryCreatePartVisual(
            Transform parent,
            string name,
            PartId partId,
            Material material,
            out GameObject root)
        {
            root = null;
            if (partId != PartId.TractionRod)
                return false;

            return TryCreateSelection(
                parent,
                name,
                TractionRodGroupToken,
                material,
                ReferencePartDisplaySize,
                out root);
        }

        private static bool TryCreateSelection(
            Transform parent,
            string name,
            string hierarchyToken,
            Material material,
            float fitSize,
            out GameObject root)
        {
            root = null;
            if (parent == null || string.IsNullOrWhiteSpace(hierarchyToken))
                return false;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath);
            if (model == null)
                return false;

            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = "CW200KReference_SourceSelection";
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;

            var selected = instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => IsInHierarchy(renderer.transform, hierarchyToken))
                .Select(renderer => renderer.transform)
                .Distinct()
                .OrderBy(item => HierarchyDepth(item))
                .ToArray();
            if (selected.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            var rootObject = new GameObject(name);
            rootObject.transform.SetParent(parent, false);
            ResetTransform(rootObject.transform);
            var content = new GameObject(ReferencePartContentName);
            content.transform.SetParent(rootObject.transform, false);
            ResetTransform(content.transform);

            foreach (var item in selected)
                item.SetParent(content.transform, true);

            UnityEngine.Object.DestroyImmediate(instance);
            StripNonVisualComponents(rootObject);
            ApplyMaterial(rootObject, material);

            if (!TryCalculateLocalBounds(rootObject.transform, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                return false;
            }

            var largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            var scale = largestDimension > Mathf.Epsilon && fitSize > Mathf.Epsilon
                ? Mathf.Clamp(fitSize / largestDimension, 0.05f, 5f)
                : 1f;
            content.transform.localScale = Vector3.one * scale;
            content.transform.localPosition = new Vector3(
                -bounds.center.x * scale,
                -bounds.min.y * scale,
                -bounds.center.z * scale);

            root = rootObject;
            return true;
        }

        private static bool IsInHierarchy(Transform transform, string token)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                if (current.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static int HierarchyDepth(Transform transform)
        {
            var depth = 0;
            for (var current = transform; current != null; current = current.parent)
                depth++;
            return depth;
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

        private static bool TryCalculateLocalBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return false;

            var hasPoint = false;
            foreach (var renderer in renderers)
            {
                var world = renderer.bounds;
                foreach (var corner in BoundsCorners(world))
                {
                    var point = root.InverseTransformPoint(corner);
                    if (!hasPoint)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasPoint = true;
                    }
                    else
                    {
                        bounds.Encapsulate(point);
                    }
                }
            }
            return hasPoint;
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            yield return new Vector3(min.x, min.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, max.y, max.z);
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

        private static void ResetTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
