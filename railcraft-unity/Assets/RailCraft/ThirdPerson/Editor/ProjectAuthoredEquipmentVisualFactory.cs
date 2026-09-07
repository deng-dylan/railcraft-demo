using System;
using System.Linq;
using RailCraft.ThirdPerson.Domain;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>Project-owned, dimension-neutral teaching geometry for unresolved asset gaps.</summary>
    public static class ProjectAuthoredEquipmentVisualFactory
    {
        public const string RootName = "ProjectAuthoredEquipment";
        public const string LiftTableName = "TeachingLiftTable";
        public const string SpreaderName = "TeachingLiftingSpreader";
        public const string HmiCabinetName = "TeachingHmiCabinet";
        public const string CableRunName = "TeachingCableRun";
        public const string AirSpringRootName = "TeachingSecondaryAirSpring";

        private static readonly StaticEditorFlags StaticFlags = StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        public static bool TryCreatePartVisual(Transform parent, string name, PartId partId,
            Material material, out GameObject visual)
        {
            visual = null;
            if (parent == null || partId != PartId.SecondaryElasticElement) return false;
            var root = CreateRoot(parent, string.IsNullOrWhiteSpace(name) ? AirSpringRootName : name, false);
            foreach (var x in new[] { -0.42f, 0.42f })
            {
                var side = x < 0f ? "L" : "R";
                CreateCylinder(root.transform, $"AirSpring_{side}_BottomPlate", new Vector3(x, -0.25f, 0f), new Vector3(0.66f, 0.08f, 0.66f), material, false);
                CreateCylinder(root.transform, $"AirSpring_{side}_AuxiliarySpring", new Vector3(x, -0.13f, 0f), new Vector3(0.42f, 0.18f, 0.42f), material, false);
                CreateCylinder(root.transform, $"AirSpring_{side}_BellowsLower", new Vector3(x, 0.01f, 0f), new Vector3(0.60f, 0.12f, 0.60f), material, false);
                CreateCylinder(root.transform, $"AirSpring_{side}_BellowsWaist", new Vector3(x, 0.13f, 0f), new Vector3(0.50f, 0.10f, 0.50f), material, false);
                CreateCylinder(root.transform, $"AirSpring_{side}_BellowsUpper", new Vector3(x, 0.25f, 0f), new Vector3(0.60f, 0.12f, 0.60f), material, false);
                CreateCylinder(root.transform, $"AirSpring_{side}_TopPlate", new Vector3(x, 0.37f, 0f), new Vector3(0.68f, 0.08f, 0.68f), material, false);
            }
            visual = root;
            return true;
        }

        public static int BuildDefaultVisuals(Transform parent, Material steel, Material safety,
            Material electrical, Material white, Material running)
        {
            if (parent == null) return 0;
            var root = CreateRoot(parent, RootName, true);
            BuildLiftTable(root.transform, steel, safety);
            BuildSpreader(root.transform, steel, safety);
            BuildHmiCabinet(root.transform, steel, electrical, white, running);
            BuildCableRun(root.transform, steel, electrical);
            return 4;
        }

        private static void BuildLiftTable(Transform parent, Material steel, Material safety)
        {
            var root = CreateRoot(parent, LiftTableName, true);
            root.transform.localPosition = new Vector3(-20.5f, 0f, 9.8f);
            CreateCube(root.transform, "Base", new Vector3(0f, 0.12f, 0f), new Vector3(3.4f, 0.24f, 2.1f), steel, true);
            CreateCube(root.transform, "Platform", new Vector3(0f, 1.42f, 0f), new Vector3(3.5f, 0.20f, 2.2f), safety, true);
            foreach (var z in new[] { -0.62f, 0.62f })
            {
                CreateBeam(root.transform, $"ScissorA_{z}", new Vector3(-0.75f, 0.22f, z), new Vector3(0.75f, 1.32f, z), 0.14f, steel, true);
                CreateBeam(root.transform, $"ScissorB_{z}", new Vector3(0.75f, 0.22f, z), new Vector3(-0.75f, 1.32f, z), 0.14f, steel, true);
            }
        }

        private static void BuildSpreader(Transform parent, Material steel, Material safety)
        {
            var root = CreateRoot(parent, SpreaderName, true);
            root.transform.localPosition = new Vector3(10.5f, 6.8f, 6f);
            CreateCube(root.transform, "MainBeam", Vector3.zero, new Vector3(5.4f, 0.26f, 0.34f), safety, true);
            CreateCube(root.transform, "CrossBeamL", new Vector3(-2.2f, -0.18f, 0f), new Vector3(0.28f, 0.28f, 2.2f), steel, true);
            CreateCube(root.transform, "CrossBeamR", new Vector3(2.2f, -0.18f, 0f), new Vector3(0.28f, 0.28f, 2.2f), steel, true);
            foreach (var x in new[] { -2.2f, 2.2f }) foreach (var z in new[] { -0.9f, 0.9f })
            {
                CreateBeam(root.transform, $"Sling_{x}_{z}", new Vector3(0f, 1.8f, 0f), new Vector3(x, 0f, z), 0.055f, steel, true);
                CreateCylinder(root.transform, $"Hook_{x}_{z}", new Vector3(x, -0.42f, z), new Vector3(0.18f, 0.42f, 0.18f), safety, true);
            }
            CreateCylinder(root.transform, "TopEye", new Vector3(0f, 1.95f, 0f), new Vector3(0.34f, 0.20f, 0.34f), steel, true);
        }

        private static void BuildHmiCabinet(Transform parent, Material steel, Material electrical, Material white, Material running)
        {
            var root = CreateRoot(parent, HmiCabinetName, true);
            root.transform.localPosition = new Vector3(23.8f, 0f, 12.8f);
            CreateCube(root.transform, "Cabinet", new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 2.5f, 0.72f), steel, true);
            CreateCube(root.transform, "Screen", new Vector3(0f, 1.65f, -0.39f), new Vector3(0.92f, 0.58f, 0.05f), electrical, true);
            CreateCube(root.transform, "ScreenGlow", new Vector3(0f, 1.65f, -0.425f), new Vector3(0.78f, 0.42f, 0.025f), running, true);
            for (var index = 0; index < 3; index++)
                CreateCylinder(root.transform, $"Indicator_{index}", new Vector3(-0.36f + index * 0.36f, 1.04f, -0.43f), new Vector3(0.11f, 0.035f, 0.11f), index == 1 ? running : white, true, Quaternion.Euler(90f, 0f, 0f));
            CreateCylinder(root.transform, "EmergencyStop", new Vector3(0f, 0.72f, -0.48f), new Vector3(0.24f, 0.10f, 0.24f), electrical, true, Quaternion.Euler(90f, 0f, 0f));
        }

        private static void BuildCableRun(Transform parent, Material steel, Material electrical)
        {
            var root = CreateRoot(parent, CableRunName, true);
            root.transform.localPosition = new Vector3(23.8f, 0f, 12.8f);
            CreateBeam(root.transform, "CableTray", new Vector3(0f, 0.24f, 0.2f), new Vector3(-4.4f, 0.24f, 0.2f), 0.10f, steel, true);
            CreateBeam(root.transform, "PowerCable", new Vector3(-0.42f, 0.54f, 0.32f), new Vector3(-3.9f, 0.30f, 0.32f), 0.045f, electrical, true);
            CreateBeam(root.transform, "SignalCable", new Vector3(0.42f, 0.54f, 0.08f), new Vector3(-3.6f, 0.18f, 0.08f), 0.035f, electrical, true);
        }

        private static GameObject CreateRoot(Transform parent, string name, bool isStatic)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            if (isStatic) MarkStatic(root);
            return root;
        }

        private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool isStatic)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            Configure(item, material, isStatic);
            return item;
        }

        private static void CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool isStatic, Quaternion? rotation = null)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localRotation = rotation ?? Quaternion.identity;
            item.transform.localScale = new Vector3(scale.x, scale.y * 0.5f, scale.z);
            Configure(item, material, isStatic);
        }

        private static void CreateBeam(Transform parent, string name, Vector3 start, Vector3 end, float width, Material material, bool isStatic)
        {
            var delta = end - start;
            var item = CreateCube(parent, name, (start + end) * 0.5f, new Vector3(width, delta.magnitude, width), material, isStatic);
            item.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        }

        private static void Configure(GameObject item, Material material, bool isStatic)
        {
            foreach (var collider in item.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            var renderer = item.GetComponent<Renderer>();
            if (renderer != null) { renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true; }
            if (isStatic) MarkStatic(item);
        }

        private static void MarkStatic(GameObject root)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(item.gameObject, StaticFlags);
        }
    }
}
