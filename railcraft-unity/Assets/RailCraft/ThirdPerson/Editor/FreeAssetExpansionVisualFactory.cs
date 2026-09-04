using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Curated CC0/free-asset layer for the first post-whitebox version.
    /// Component meshes express generic teaching semantics; environment meshes
    /// remain visual-only and never change interaction or collision rules.
    /// </summary>
    public static class FreeAssetExpansionVisualFactory
    {
        public const string RootName = "FreeAssetExpansion";
        public const string ComponentRootName = "CC0ComponentVisual";
        public const string ToolDisplayName = "CC0IndustrialToolDisplay";
        public const string SafetyDisplayName = "CC0SafetyDisplay";
        public const string ExteriorName = "CC0FactoryExterior";
        public const string DistantTrainName = "CC0DistantTrainYard";

        private const string ComponentRoot =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Components";
        private const string EnvironmentRoot =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment";

        public const string SpringPath = ComponentRoot +
            "/QuaterniusUtilityParts/Models/Spring_Bouncer.fbx";
        public const string PipesPath = ComponentRoot +
            "/QuaterniusUtilityParts/Models/Pipes.fbx";
        public const string MetalSupportPath = ComponentRoot +
            "/QuaterniusUtilityParts/Models/MetalSupport.fbx";
        public const string StopValvePath = ComponentRoot +
            "/BlendSwapStopValve/Models/StopValve.fbx";

        public const string DrillPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Drill.fbx";
        public const string LevelPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Level.fbx";
        public const string SpannerAPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Spanner_A.fbx";
        public const string SpannerBPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Spanner_B.fbx";
        public const string TapeMeasurePath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Tape_Measure.fbx";
        public const string ToolboxPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Toolbox.fbx";
        public const string ClipboardPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Clipboard.fbx";
        public const string WorkLightAPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Work_Light_A.fbx";
        public const string WorkLightBPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Work_Light_B.fbx";
        public const string WindowPath = EnvironmentRoot +
            "/LowPolyConstruction/Models/Window.fbx";

        public const string ConePath = EnvironmentRoot +
            "/CC0Safety/Models/ConstructionCone.fbx";
        public const string BarrierPath = EnvironmentRoot +
            "/CC0Safety/Models/Type2Barrier.fbx";
        public const string BollardPath = EnvironmentRoot +
            "/CC0Safety/Models/SecurityBollard.fbx";

        public const string BuildingCPath = EnvironmentRoot +
            "/KenneyCityIndustrial/Models/building-c.fbx";
        public const string BuildingMPath = EnvironmentRoot +
            "/KenneyCityIndustrial/Models/building-m.fbx";
        public const string ChimneyPath = EnvironmentRoot +
            "/KenneyCityIndustrial/Models/chimney-large.fbx";
        public const string TankPath = EnvironmentRoot +
            "/KenneyCityIndustrial/Models/detail-tank.fbx";
        public const string LargeTankPath = EnvironmentRoot +
            "/KenneyCityIndustrial/Models/detail-tank-large.fbx";

        public const string HighSpeedFrontPath = EnvironmentRoot +
            "/QuaterniusModularTrain/Models/HighSpeed_Front.fbx";
        public const string HighSpeedWagonPath = EnvironmentRoot +
            "/QuaterniusModularTrain/Models/HighSpeed_Wagon.fbx";
        public const string CargoFrontPath = EnvironmentRoot +
            "/QuaterniusModularTrain/Models/CargoTrain_Front.fbx";
        public const string CargoWagonPath = EnvironmentRoot +
            "/QuaterniusModularTrain/Models/CargoTrain_Wagon.fbx";

        private static readonly StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        public static bool AreComponentAssetsAvailable =>
            HasModel(SpringPath) && HasModel(PipesPath) &&
            HasModel(MetalSupportPath) && HasModel(StopValvePath);

        public static bool AreEnvironmentAssetsAvailable =>
            HasModel(DrillPath) && HasModel(ConePath) &&
            HasModel(BuildingCPath) && HasModel(HighSpeedFrontPath);

        public static bool TryCreatePartVisual(
            Transform parent,
            string name,
            PartId partId,
            Material material,
            out GameObject visual)
        {
            visual = null;
            if (parent == null || !SupportsPart(partId))
                return false;

            var root = new GameObject(string.IsNullOrWhiteSpace(name) ? ComponentRootName : name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var created = 0;
            switch (partId)
            {
                case PartId.PrimaryElasticElement:
                    created += CreateFittedAsset(root.transform, "CC0_Spring_Left", SpringPath,
                        new Vector3(-0.34f, 0f, 0f), new Vector3(0.48f, 0.72f, 0.48f),
                        Quaternion.identity, material) ? 1 : 0;
                    created += CreateFittedAsset(root.transform, "CC0_Spring_Right", SpringPath,
                        new Vector3(0.34f, 0f, 0f), new Vector3(0.48f, 0.72f, 0.48f),
                        Quaternion.identity, material) ? 1 : 0;
                    break;

                case PartId.SensorBracket:
                    created += CreateFittedAsset(root.transform, "CC0_SensorMount", MetalSupportPath,
                        Vector3.zero, new Vector3(1.05f, 0.72f, 0.48f),
                        Quaternion.Euler(0f, 0f, 90f), material) ? 1 : 0;
                    break;

                case PartId.PrimaryPositioningElement:
                    created += CreateFittedAsset(root.transform, "CC0_PositioningSupport_A", MetalSupportPath,
                        new Vector3(-0.36f, 0f, 0f), new Vector3(0.72f, 0.54f, 0.35f),
                        Quaternion.Euler(0f, 0f, 90f), material) ? 1 : 0;
                    created += CreateFittedAsset(root.transform, "CC0_PositioningSupport_B", MetalSupportPath,
                        new Vector3(0.36f, 0f, 0f), new Vector3(0.72f, 0.54f, 0.35f),
                        Quaternion.Euler(0f, 180f, -90f), material) ? 1 : 0;
                    break;

                case PartId.HeightControlElement:
                    created += CreateFittedAsset(root.transform, "CC0_StopValve", StopValvePath,
                        new Vector3(-0.38f, 0.08f, 0f), new Vector3(0.48f, 0.72f, 0.48f),
                        Quaternion.identity, material) ? 1 : 0;
                    created += CreateFittedAsset(root.transform, "CC0_PipeAndLink", PipesPath,
                        new Vector3(0.42f, 0f, 0f), new Vector3(0.92f, 0.72f, 0.55f),
                        Quaternion.Euler(0f, 0f, 90f), material) ? 1 : 0;
                    break;
            }

            if (created == 0)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return false;
            }

            ClearStaticFlags(root);
            visual = root;
            return true;
        }

        public static int BuildDefaultVisuals(
            Transform parent,
            Material steel,
            Material safety,
            Material white,
            Material running,
            Material floor,
            Material wall)
        {
            if (parent == null || !AreEnvironmentAssetsAvailable)
                return 0;

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            MarkStatic(root);

            var created = 0;
            created += BuildToolDisplay(root.transform, steel, safety, white);
            created += BuildSafetyDisplay(root.transform, safety, white);
            created += BuildExterior(root.transform, floor, wall, steel);
            created += BuildDistantTrainYard(root.transform, white, running, steel);
            return created;
        }

        private static bool SupportsPart(PartId partId)
        {
            return partId == PartId.PrimaryElasticElement ||
                   partId == PartId.SensorBracket ||
                   partId == PartId.PrimaryPositioningElement ||
                   partId == PartId.HeightControlElement;
        }

        private static int BuildToolDisplay(
            Transform parent,
            Material steel,
            Material safety,
            Material white)
        {
            var root = CreateRoot(parent, ToolDisplayName);
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "ToolBench";
            table.transform.SetParent(root.transform, false);
            table.transform.localPosition = new Vector3(-21.5f, 0.52f, 14.1f);
            table.transform.localScale = new Vector3(4.6f, 0.18f, 1.3f);
            ApplyMaterial(table, steel);
            RemoveColliders(table);
            MarkStatic(table);

            var items = new[]
            {
                ("Toolbox", ToolboxPath, new Vector3(-23.0f, 0.9f, 14.0f), new Vector3(0.8f, 0.55f, 0.55f), Quaternion.Euler(90f, 0f, 0f), safety),
                ("Drill", DrillPath, new Vector3(-21.9f, 0.92f, 14.0f), new Vector3(0.45f, 0.65f, 0.45f), Quaternion.Euler(90f, 0f, 90f), steel),
                ("SpannerA", SpannerAPath, new Vector3(-21.1f, 0.84f, 13.9f), new Vector3(0.58f, 0.12f, 0.18f), Quaternion.Euler(0f, 90f, 90f), white),
                ("SpannerB", SpannerBPath, new Vector3(-20.5f, 0.84f, 14.2f), new Vector3(0.58f, 0.12f, 0.18f), Quaternion.Euler(0f, -90f, 90f), white),
                ("TapeMeasure", TapeMeasurePath, new Vector3(-19.9f, 0.88f, 13.9f), new Vector3(0.32f, 0.24f, 0.28f), Quaternion.Euler(90f, 0f, 0f), safety),
                ("Level", LevelPath, new Vector3(-21.1f, 0.84f, 14.35f), new Vector3(1.0f, 0.12f, 0.18f), Quaternion.Euler(90f, 0f, 90f), safety),
                ("Clipboard", ClipboardPath, new Vector3(-23.0f, 1.15f, 14.45f), new Vector3(0.42f, 0.55f, 0.12f), Quaternion.Euler(75f, 0f, 0f), white),
                ("WorkLightA", WorkLightAPath, new Vector3(-23.2f, 1.25f, 13.45f), new Vector3(0.55f, 0.8f, 0.55f), Quaternion.identity, safety),
                ("WorkLightB", WorkLightBPath, new Vector3(-19.8f, 1.25f, 13.45f), new Vector3(0.55f, 0.8f, 0.55f), Quaternion.identity, safety)
            };

            var created = 0;
            foreach (var item in items)
            {
                if (CreateFittedAsset(root.transform, item.Item1, item.Item2, item.Item3,
                        item.Item4, item.Item5, item.Item6))
                    created++;
            }

            CreateFittedAsset(root.transform, "SideWindow_01", WindowPath,
                new Vector3(27.72f, 4.4f, -10f), new Vector3(0.14f, 2.6f, 3.2f),
                Quaternion.Euler(0f, 90f, 0f), white);
            CreateFittedAsset(root.transform, "SideWindow_02", WindowPath,
                new Vector3(27.72f, 4.4f, -4f), new Vector3(0.14f, 2.6f, 3.2f),
                Quaternion.Euler(0f, 90f, 0f), white);
            return created + 2;
        }

        private static int BuildSafetyDisplay(Transform parent, Material safety, Material white)
        {
            var root = CreateRoot(parent, SafetyDisplayName);
            var created = 0;
            foreach (var x in new[] { 21.5f, 23.3f, 25.1f })
            {
                if (CreateFittedAsset(root.transform, $"ConstructionCone_{created + 1}", ConePath,
                        new Vector3(x, 0.38f, -18.7f), new Vector3(0.55f, 0.76f, 0.55f),
                        Quaternion.Euler(-90f, 0f, 0f), safety))
                    created++;
            }

            if (CreateFittedAsset(root.transform, "Type2Barrier", BarrierPath,
                    new Vector3(24.0f, 0.65f, -16.8f), new Vector3(2.2f, 1.25f, 0.65f),
                    Quaternion.Euler(-90f, 0f, 90f), white))
                created++;
            foreach (var x in new[] { 22.2f, 25.8f })
            {
                if (CreateFittedAsset(root.transform, $"SecurityBollard_{created}", BollardPath,
                        new Vector3(x, 0.55f, -16.8f), new Vector3(0.32f, 1.1f, 0.32f),
                        Quaternion.Euler(-90f, 0f, 0f), safety))
                    created++;
            }
            return created;
        }

        private static int BuildExterior(
            Transform parent,
            Material floor,
            Material wall,
            Material steel)
        {
            var root = CreateRoot(parent, ExteriorName);
            var apron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            apron.name = "ExteriorApron";
            apron.transform.SetParent(root.transform, false);
            apron.transform.localPosition = new Vector3(0f, -0.1f, -39f);
            apron.transform.localScale = new Vector3(56f, 0.18f, 34f);
            ApplyMaterial(apron, floor);
            RemoveColliders(apron);
            MarkStatic(apron);

            var items = new[]
            {
                ("FactoryBuilding_West", BuildingCPath, new Vector3(-18f, 6.3f, -49f), new Vector3(16f, 12f, 14f), Quaternion.identity, wall),
                ("FactoryBuilding_East", BuildingMPath, new Vector3(18f, 6.5f, -48f), new Vector3(15f, 13f, 14f), Quaternion.identity, wall),
                ("Chimney_SouthWest", ChimneyPath, new Vector3(-27f, 8.5f, -53f), new Vector3(4f, 17f, 4f), Quaternion.identity, steel),
                ("TankFarm_A", TankPath, new Vector3(24f, 1.5f, -37f), new Vector3(4.2f, 3f, 3.2f), Quaternion.identity, steel),
                ("TankFarm_B", LargeTankPath, new Vector3(20f, 2.0f, -39f), new Vector3(5.5f, 4f, 4.2f), Quaternion.identity, steel)
            };

            var created = 0;
            foreach (var item in items)
            {
                if (CreateFittedAsset(root.transform, item.Item1, item.Item2, item.Item3,
                        item.Item4, item.Item5, item.Item6))
                    created++;
            }
            return created;
        }

        private static int BuildDistantTrainYard(
            Transform parent,
            Material white,
            Material running,
            Material steel)
        {
            var root = CreateRoot(parent, DistantTrainName);
            var created = 0;
            var items = new[]
            {
                ("HighSpeed_Front", HighSpeedFrontPath, new Vector3(-5.7f, 1.05f, -28.5f), new Vector3(12.2f, 2.1f, 2.2f), Quaternion.identity, white),
                ("HighSpeed_Wagon", HighSpeedWagonPath, new Vector3(6.0f, 1.05f, -28.5f), new Vector3(11.2f, 2.1f, 2.2f), Quaternion.identity, running),
                ("CargoTrain_Front", CargoFrontPath, new Vector3(-5.2f, 1.4f, -34.8f), new Vector3(10.4f, 2.8f, 2.3f), Quaternion.identity, steel),
                ("CargoTrain_Wagon", CargoWagonPath, new Vector3(5.2f, 1.4f, -34.8f), new Vector3(10.3f, 2.8f, 2.3f), Quaternion.identity, steel)
            };

            foreach (var item in items)
            {
                if (CreateFittedAsset(root.transform, item.Item1, item.Item2, item.Item3,
                        item.Item4, item.Item5, item.Item6))
                    created++;
            }
            return created;
        }

        private static GameObject CreateRoot(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            MarkStatic(root);
            return root;
        }

        private static bool CreateFittedAsset(
            Transform parent,
            string name,
            string assetPath,
            Vector3 desiredLocalCenter,
            Vector3 targetSize,
            Quaternion rotation,
            Material material)
        {
            if (!FactoryKitEnvironmentVisualFactory.TryCreateAsset(
                    parent,
                    name,
                    assetPath,
                    Vector3.zero,
                    rotation,
                    Vector3.one,
                    material,
                    out var instance) ||
                !TryGetBounds(instance, out var bounds))
                return false;

            var sourceSize = bounds.size;
            var factors = new List<float>(3);
            if (sourceSize.x > 0.0001f && targetSize.x > 0f)
                factors.Add(targetSize.x / sourceSize.x);
            if (sourceSize.y > 0.0001f && targetSize.y > 0f)
                factors.Add(targetSize.y / sourceSize.y);
            if (sourceSize.z > 0.0001f && targetSize.z > 0f)
                factors.Add(targetSize.z / sourceSize.z);
            if (factors.Count == 0)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            var uniformScale = factors.Min();
            instance.transform.localScale *= uniformScale;
            if (!TryGetBounds(instance, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            var desiredWorldCenter = parent.TransformPoint(desiredLocalCenter);
            instance.transform.position += desiredWorldCenter - bounds.center;
            MarkStatic(instance);
            return true;
        }

        private static bool HasModel(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root == null
                ? Array.Empty<Renderer>()
                : root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return true;
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            if (root == null || material == null)
                return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }

        private static void RemoveColliders(GameObject root)
        {
            if (root == null)
                return;
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
        }

        private static void MarkStatic(GameObject root)
        {
            if (root == null)
                return;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(item.gameObject, StaticFlags);
        }

        private static void ClearStaticFlags(GameObject root)
        {
            if (root == null)
                return;
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(item.gameObject, 0);
        }
    }
}
