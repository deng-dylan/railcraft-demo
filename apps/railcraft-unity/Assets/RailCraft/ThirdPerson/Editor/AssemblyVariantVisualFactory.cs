using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Optional model slots for playable assembly plans. CAD files are kept
    /// outside the runtime import path until they are exported to FBX/GLB; once
    /// a converted asset is placed at the declared path, rebuilding the scene
    /// makes the same gameplay route use that geometry automatically.
    /// </summary>
    public static class AssemblyVariantVisualFactory
    {
        private const string VariantRoot =
            "Assets/RailCraft/ThirdPerson/Art/Models/VariantModels";

        private static readonly IReadOnlyDictionary<AssemblyVariantId, string[]> ModelPaths =
            new Dictionary<AssemblyVariantId, string[]>
            {
                {
                    AssemblyVariantId.MetroSimplified,
                    new[]
                    {
                        VariantRoot + "/MetroSimplified/MetroSimplifiedBogie.fbx",
                        VariantRoot + "/MetroSimplified/地铁转向架（简化）（上色版）.fbx",
                        VariantRoot + "/MetroSimplified/地铁转向架（上色版）.fbx",
                        "Assets/RailCraft/ThirdPerson/Art/Models/AssemblyVariants/MetroBogie.fbx",
                        "Assets/RailCraft/ThirdPerson/Art/Models/Candidates/Metro/MetroBogieAssemblyDemo.fbx"
                    }
                },
                {
                    AssemblyVariantId.Y25Freight,
                    new[]
                    {
                        VariantRoot + "/Y25Freight/Y25FreightBogie.fbx",
                        VariantRoot + "/Y25Freight/Y25转向架 欧洲货运火车.fbx",
                        "Assets/RailCraft/ThirdPerson/Art/Models/AssemblyVariants/Y25Bogie.fbx",
                        "Assets/RailCraft/ThirdPerson/Art/Models/Candidates/Y25/Y25BogieAssemblyDemo.fbx"
                    }
                },
                {
                    AssemblyVariantId.TeachingConcept,
                    new[]
                    {
                        VariantRoot + "/TeachingConcept/TeachingConceptBogie.fbx",
                        VariantRoot + "/TeachingConcept/简化铁路转向架（现实无对应）.fbx",
                        "Assets/RailCraft/ThirdPerson/Art/Models/AssemblyVariants/ConceptTeachingBogie.fbx",
                        "Assets/RailCraft/ThirdPerson/Art/Models/Candidates/Teaching/TeachingBogieAssemblyDemo.fbx"
                    }
                }
            };

        public static string ExpectedModelPath(AssemblyVariantId variant)
        {
            return ModelPaths.TryGetValue(variant, out var paths)
                ? ModelCandidateRegistry.ResolveFirstAvailable(paths) ?? paths[0]
                : BogieAssemblyDemoVisualFactory.ModelAssetPath;
        }

        public static bool IsModelAvailable(AssemblyVariantId variant)
        {
            return variant == AssemblyVariantId.FuxingDemo
                ? BogieAssemblyDemoVisualFactory.IsModelAvailable
                : ModelPaths.TryGetValue(variant, out var paths) &&
                  ModelCandidateRegistry.ResolveFirstAvailable(paths) != null;
        }

        public static bool TryCreateReferenceVisual(
            Transform parent,
            string name,
            AssemblyVariantId variant,
            Material material,
            out GameObject root)
        {
            root = null;
            if (parent == null)
                return false;

            if (variant == AssemblyVariantId.FuxingDemo || !IsModelAvailable(variant))
            {
                return BogieAssemblyDemoVisualFactory.TryCreateCompletedBogieVisual(
                    parent,
                    name,
                    material,
                    out root);
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ExpectedModelPath(variant));
            if (model == null)
                return false;

            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = $"{variant}_ImportedSourceInstance";
            instance.transform.SetParent(parent, false);
            ResetTransform(instance.transform);
            StripNonVisualComponents(instance);
            // Keep teammate-authored colors for the colored metro and Y25
            // deliveries. The concept teaching part is intentionally restyled
            // in Unity because its SolidWorks teaching source cannot be edited.
            if (variant == AssemblyVariantId.TeachingConcept)
                ApplyMaterial(instance, material);

            var rootObject = new GameObject(name);
            rootObject.transform.SetParent(parent, false);
            ResetTransform(rootObject.transform);
            instance.transform.SetParent(rootObject.transform, false);
            if (!TryCalculateLocalBounds(rootObject.transform, out var bounds))
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                return false;
            }

            var largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            var scale = largest > Mathf.Epsilon ? 3.2f / largest : 1f;
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.localPosition = new Vector3(
                -bounds.center.x * scale,
                -bounds.min.y * scale,
                -bounds.center.z * scale);
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
            foreach (var audio in root.GetComponentsInChildren<AudioSource>(true))
                UnityEngine.Object.DestroyImmediate(audio);
        }

        private static bool TryCalculateLocalBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var initialized = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var worldBounds = renderer.bounds;
                for (var x = 0; x <= 1; x++)
                for (var y = 0; y <= 1; y++)
                for (var z = 0; z <= 1; z++)
                {
                    var point = new Vector3(
                        x == 0 ? worldBounds.min.x : worldBounds.max.x,
                        y == 0 ? worldBounds.min.y : worldBounds.max.y,
                        z == 0 ? worldBounds.min.z : worldBounds.max.z);
                    var localPoint = root.InverseTransformPoint(point);
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

        private static void ResetTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
