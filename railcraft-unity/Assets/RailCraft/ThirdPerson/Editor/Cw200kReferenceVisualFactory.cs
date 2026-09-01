using System.Linq;
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
