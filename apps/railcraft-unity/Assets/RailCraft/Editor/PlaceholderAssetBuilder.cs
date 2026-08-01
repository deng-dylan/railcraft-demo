using System;
using System.Collections.Generic;
using System.IO;
using RailCraft.Assets;
using RailCraft.Content;
using RailCraft.Interaction;
using UnityEditor;
using UnityEngine;

namespace RailCraft.Editor
{
    /// <summary>Creates the replaceable v0.1 art set from primitives; safe to run repeatedly.</summary>
    public static class PlaceholderAssetBuilder
    {
        private const string ArtRoot = "Assets/RailCraft/Art";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string PrefabRoot = ArtRoot + "/Prefabs";
        private const string SourceVersion = "placeholder-v1";

        private sealed class MaterialSet
        {
            public Material Steel;
            public Material Yellow;
            public Material Blue;
            public Material Orange;
            public Material Red;
            public Material Green;
            public Material Cyan;
            public Material White;
            public Material Violet;
        }

        [MenuItem("RailCraft/Build Placeholder Assets")]
        public static void BuildFromMenu() => Build(false);

        public static void BuildFromCommandLine()
        {
            Build(true);
        }

        public static void Build(bool exitBatchMode)
        {
            EnsureFolders();
            var materials = CreateMaterials();
            var flow = LoadFlow();
            var entries = new List<PartPrefabEntry>();
            foreach (var step in flow.steps)
            {
                var prefabPath = PrefabPath(step.assetKey);
                var prefab = BuildDraggablePrefab(step.assetKey, step.id, prefabPath, materials);
                entries.Add(new PartPrefabEntry(step.assetKey, prefab));
            }

            BuildPoweredIntermediateCar(entries, materials);
            BuildHeadDisplay(materials);
            var catalog = AssetDatabase.LoadAssetAtPath<PartPrefabCatalog>(ArtRoot + "/PartPrefabCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PartPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, ArtRoot + "/PartPrefabCatalog.asset");
            }

            catalog.Configure(entries.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var issues = ModelContractValidator.ValidateProductionCatalog();
            if (issues.Count != 0)
                throw new InvalidOperationException("Generated placeholder assets failed validation: " + string.Join(", ", issues));

            if (exitBatchMode && Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        private static FlowDefinition LoadFlow()
        {
            var path = Path.Combine(Application.dataPath, "RailCraft/Content/V1/flow.v1.json");
            return JsonUtility.FromJson<FlowDefinition>(File.ReadAllText(path));
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/RailCraft", "Art");
            EnsureFolder(ArtRoot, "Materials");
            EnsureFolder(ArtRoot, "Prefabs");
            EnsureFolder(PrefabRoot, "Modules");
            EnsureFolder(PrefabRoot, "Process");
            EnsureFolder(PrefabRoot, "Vehicles");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static MaterialSet CreateMaterials()
        {
            return new MaterialSet
            {
                Steel = CreateMaterial("Steel", new Color(0.28f, 0.34f, 0.4f)),
                Yellow = CreateMaterial("SafetyYellow", new Color(0.95f, 0.62f, 0.05f)),
                Blue = CreateMaterial("RailBlue", new Color(0.05f, 0.27f, 0.68f)),
                Orange = CreateMaterial("SignalOrange", new Color(0.95f, 0.25f, 0.05f)),
                Red = CreateMaterial("BrakeRed", new Color(0.72f, 0.05f, 0.04f)),
                Green = CreateMaterial("InspectionGreen", new Color(0.05f, 0.55f, 0.24f)),
                Cyan = CreateMaterial("SensorCyan", new Color(0.03f, 0.7f, 0.82f)),
                White = CreateMaterial("CardWhite", new Color(0.9f, 0.92f, 0.92f)),
                Violet = CreateMaterial("TractionViolet", new Color(0.38f, 0.13f, 0.62f))
            };
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var path = MaterialRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string PrefabPath(string assetKey)
        {
            var file = assetKey.Replace('.', '_');
            if (assetKey.StartsWith("module.", StringComparison.Ordinal))
                return PrefabRoot + "/Modules/" + file + ".prefab";
            if (assetKey.StartsWith("process.", StringComparison.Ordinal))
                return PrefabRoot + "/Process/" + file + ".prefab";
            return PrefabRoot + "/Vehicles/PoweredIntermediateCar.prefab";
        }

        private static GameObject BuildDraggablePrefab(string key, string stepId, string path, MaterialSet materials)
        {
            var root = new GameObject(key.Replace('.', '_'));
            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            BuildSilhouette(key, visualRoot.transform, materials);
            AddPrimitive(root.transform, PrimitiveType.Cube, "Highlight", new Vector3(0f, 0.34f, 0f), new Vector3(0.62f, 0.035f, 0.12f), materials.Yellow);

            var contract = root.AddComponent<ModelContract>();
            contract.Configure(key, SourceVersion, Vector3.right, Vector3.up, true);
            var draggable = root.AddComponent<DraggableModule>();
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.12f, 0f);
            collider.size = new Vector3(1.25f, 0.55f, 0.85f);
            draggable.Configure(stepId, collider, visualRoot.transform);
            return SavePrefab(root, path);
        }

        private static void BuildPoweredIntermediateCar(List<PartPrefabEntry> entries, MaterialSet materials)
        {
            var path = PrefabRoot + "/Vehicles/PoweredIntermediateCar.prefab";
            var prefab = BuildDraggablePrefab("vehicle.powered_intermediate_car", "carbody_lowering", path, materials);
            ReplaceEntry(entries, "vehicle.powered_intermediate_car", prefab);
        }

        private static void BuildHeadDisplay(MaterialSet materials)
        {
            var root = new GameObject("CR400AF 展示背景");
            var visual = new GameObject("CR400AF 展示背景");
            visual.transform.SetParent(root.transform, false);
            AddPrimitive(visual.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 1.1f, 0f), new Vector3(7f, 1.8f, 1.5f), materials.White);
            AddPrimitive(visual.transform, PrimitiveType.Cube, "BlueBand", new Vector3(0f, 1.2f, -0.78f), new Vector3(7.05f, 0.35f, 0.06f), materials.Blue);
            AddPrimitive(visual.transform, PrimitiveType.Sphere, "Nose", new Vector3(3.65f, 1.05f, 0f), new Vector3(1.7f, 1.7f, 1.5f), materials.White);
            AddPrimitive(visual.transform, PrimitiveType.Cube, "Window", new Vector3(1.3f, 1.62f, -0.8f), new Vector3(1.5f, 0.38f, 0.05f), materials.Steel);
            AddPrimitive(visual.transform, PrimitiveType.Cube, "Window2", new Vector3(-0.7f, 1.62f, -0.8f), new Vector3(1.5f, 0.38f, 0.05f), materials.Steel);
            SavePrefab(root, PrefabRoot + "/Vehicles/CR400AFHeadDisplay.prefab");
        }

        private static void ReplaceEntry(List<PartPrefabEntry> entries, string key, GameObject prefab)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].assetKey == key)
                {
                    entries[i] = new PartPrefabEntry(key, prefab);
                    return;
                }
            }
            entries.Add(new PartPrefabEntry(key, prefab));
        }

        private static void BuildSilhouette(string key, Transform visual, MaterialSet m)
        {
            var primary = m.Steel;
            var accent = m.Blue;
            if (key.Contains("wheelset")) { primary = m.Steel; accent = key.EndsWith("_a", StringComparison.Ordinal) ? m.Cyan : m.Orange; }
            else if (key.Contains("suspension")) { primary = m.Steel; accent = key.Contains("secondary") ? m.Green : m.Yellow; }
            else if (key.Contains("brake")) { primary = m.Red; accent = m.Yellow; }
            else if (key.Contains("traction")) { primary = m.Violet; accent = m.Orange; }
            else if (key.Contains("sensor")) { primary = m.Cyan; accent = m.Steel; }
            else if (key.StartsWith("process.")) { primary = m.White; accent = key.Contains("commissioning") ? m.Blue : key.Contains("inspection") ? m.Green : m.Orange; }
            else if (key.StartsWith("vehicle.")) { primary = m.White; accent = m.Blue; }

            AddPrimitive(visual, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(1.15f, 0.35f, 0.65f), primary);
            if (key.Contains("wheelset"))
            {
                AddPrimitive(visual, PrimitiveType.Cylinder, "Axle", Vector3.zero, new Vector3(0.13f, 0.6f, 0.13f), accent, Quaternion.Euler(0f, 0f, 90f));
                AddPrimitive(visual, PrimitiveType.Cylinder, "WheelA", new Vector3(-0.42f, 0f, 0f), new Vector3(0.32f, 0.13f, 0.32f), primary, Quaternion.Euler(0f, 0f, 90f));
                AddPrimitive(visual, PrimitiveType.Cylinder, "WheelB", new Vector3(0.42f, 0f, 0f), new Vector3(0.32f, 0.13f, 0.32f), primary, Quaternion.Euler(0f, 0f, 90f));
            }
            else if (key.Contains("suspension"))
            {
                for (var i = -1; i <= 1; i += 2)
                    AddPrimitive(visual, PrimitiveType.Cylinder, "Spring" + i, new Vector3(i * 0.32f, 0.28f, 0f), new Vector3(0.14f, 0.28f, 0.14f), accent);
            }
            else if (key.Contains("brake"))
            {
                AddPrimitive(visual, PrimitiveType.Cylinder, "Disc", new Vector3(0.2f, 0.08f, 0f), new Vector3(0.38f, 0.08f, 0.38f), accent, Quaternion.Euler(90f, 0f, 0f));
            }
            else if (key.Contains("traction"))
            {
                AddPrimitive(visual, PrimitiveType.Cylinder, "Motor", new Vector3(0f, 0.35f, 0f), new Vector3(0.28f, 0.38f, 0.28f), accent, Quaternion.Euler(90f, 0f, 0f));
                AddPrimitive(visual, PrimitiveType.Cube, "Fin", new Vector3(0.35f, 0.35f, 0f), new Vector3(0.12f, 0.4f, 0.58f), m.Yellow);
            }
            else if (key.Contains("height_damping"))
            {
                AddPrimitive(visual, PrimitiveType.Cylinder, "Damper", new Vector3(0f, 0.38f, 0f), new Vector3(0.18f, 0.4f, 0.18f), accent);
            }
            else if (key.Contains("sensor"))
            {
                AddPrimitive(visual, PrimitiveType.Sphere, "Lens", new Vector3(0.3f, 0.18f, -0.34f), new Vector3(0.2f, 0.2f, 0.08f), accent);
            }
            else if (key.StartsWith("process."))
            {
                AddPrimitive(visual, PrimitiveType.Cube, "Card", new Vector3(0f, 0.28f, 0f), new Vector3(0.85f, 0.06f, 0.52f), accent);
                AddPrimitive(visual, PrimitiveType.Cube, "CardLine", new Vector3(-0.12f, 0.33f, -0.02f), new Vector3(0.35f, 0.015f, 0.03f), m.Steel);
            }
            else if (key.StartsWith("vehicle."))
            {
                AddPrimitive(visual, PrimitiveType.Cube, "CarBody", new Vector3(0f, 0.42f, 0f), new Vector3(1.4f, 0.55f, 0.75f), primary);
                AddPrimitive(visual, PrimitiveType.Cube, "BlueStripe", new Vector3(0f, 0.42f, -0.4f), new Vector3(1.42f, 0.13f, 0.03f), accent);
            }
            else
            {
                AddPrimitive(visual, PrimitiveType.Cube, "Crossbar", new Vector3(0f, 0.26f, 0f), new Vector3(0.15f, 0.5f, 0.8f), accent);
            }
        }

        private static GameObject AddPrimitive(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Quaternion rotation = default)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
            item.transform.localScale = scale;
            var collider = item.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
            var renderer = item.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = true;
            return item;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
