using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Builds the visual layer for knowledge, assembly and commissioning
    /// stations from small CC0 Factory Kit modules. Gameplay triggers remain on
    /// the station roots; these objects are presentation-only.
    /// </summary>
    public static class FactoryKitWorkbenchVisualFactory
    {
        public const string AssetRootPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/FactoryKit";
        public const string MachineBedAssetPath = AssetRootPath + "/machine-bed.fbx";
        public const string ScreenPanelWideAssetPath = AssetRootPath + "/screen-panel-wide.fbx";
        public const string ScreenFlatAssetPath = AssetRootPath + "/screen-flat.fbx";
        public const string LeverAssetPath = AssetRootPath + "/lever-single.fbx";
        public const string ButtonAssetPath = AssetRootPath + "/button-floor-square.fbx";

        public const string QuizWorkbenchRootName = "QuizWorkbenchVisual";
        public const string AssemblyTableRootName = "AssemblyTableVisual";
        public const string CommissioningConsoleRootName = "CommissioningConsoleVisual";

        public static bool IsAvailable =>
            HasAsset(MachineBedAssetPath) &&
            HasAsset(ScreenPanelWideAssetPath) &&
            HasAsset(ScreenFlatAssetPath) &&
            HasAsset(LeverAssetPath) &&
            HasAsset(ButtonAssetPath);

        public static bool TryBuildQuizWorkbench(
            Transform parent,
            Material bodyMaterial,
            Material screenMaterial,
            Material accentMaterial,
            out GameObject root)
        {
            root = CreateRoot(parent, QuizWorkbenchRootName);
            if (root == null || !IsAvailable)
                return false;

            var created = 0;
            created += CreatePart(
                root.transform,
                "MachineBed",
                MachineBedAssetPath,
                new Vector3(0f, 0.42f, 0f),
                new Vector3(1f, 0.36f, 0.70f),
                bodyMaterial);
            created += CreatePart(
                root.transform,
                "HmiScreen",
                ScreenPanelWideAssetPath,
                new Vector3(0f, 1.30f, -0.51f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(1.38f, 1.10f, 0.13f),
                screenMaterial ?? bodyMaterial);
            created += CreatePart(
                root.transform,
                "ConfirmButton",
                ButtonAssetPath,
                new Vector3(-0.46f, 0.89f, -0.70f),
                Quaternion.identity,
                new Vector3(0.26f, 0.18f, 0.26f),
                accentMaterial ?? bodyMaterial);
            created += CreatePart(
                root.transform,
                "ModeLever",
                LeverAssetPath,
                new Vector3(0.47f, 0.92f, -0.66f),
                Quaternion.identity,
                new Vector3(0.34f, 0.34f, 0.34f),
                accentMaterial ?? bodyMaterial);

            if (created == 0)
            {
                Object.DestroyImmediate(root);
                root = null;
                return false;
            }

            MarkStatic(root);
            return true;
        }

        public static bool TryBuildAssemblyTable(
            Transform parent,
            Material bodyMaterial,
            Material screenMaterial,
            Material accentMaterial,
            float spanMultiplier,
            out GameObject root)
        {
            root = CreateRoot(parent, AssemblyTableRootName);
            if (root == null || !IsAvailable)
                return false;

            var span = Mathf.Clamp(spanMultiplier, 0.75f, 1.6f);
            var created = 0;
            created += CreatePart(
                root.transform,
                "MachineBed_Left",
                MachineBedAssetPath,
                new Vector3(-1.65f * span, 0.46f, 0f),
                Quaternion.identity,
                new Vector3(1.42f * span, 0.39f, 0.76f),
                bodyMaterial);
            created += CreatePart(
                root.transform,
                "MachineBed_Right",
                MachineBedAssetPath,
                new Vector3(1.65f * span, 0.46f, 0f),
                Quaternion.identity,
                new Vector3(1.42f * span, 0.39f, 0.76f),
                bodyMaterial);
            created += CreatePart(
                root.transform,
                "AssemblyScreen",
                ScreenFlatAssetPath,
                new Vector3(0f, 1.12f, -1.18f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(0.80f * span, 0.92f, 0.13f),
                screenMaterial ?? bodyMaterial);
            created += CreatePart(
                root.transform,
                "AssemblyButton",
                ButtonAssetPath,
                new Vector3(-0.44f * span, 0.94f, -1.30f),
                Quaternion.identity,
                new Vector3(0.24f, 0.16f, 0.24f),
                accentMaterial ?? bodyMaterial);
            created += CreatePart(
                root.transform,
                "AssemblyLever",
                LeverAssetPath,
                new Vector3(0.44f * span, 0.94f, -1.27f),
                Quaternion.identity,
                new Vector3(0.30f, 0.30f, 0.30f),
                accentMaterial ?? bodyMaterial);

            if (created == 0)
            {
                Object.DestroyImmediate(root);
                root = null;
                return false;
            }

            MarkStatic(root);
            return true;
        }

        public static bool TryBuildCommissioningConsole(
            Transform parent,
            Material bodyMaterial,
            Material screenMaterial,
            Material accentMaterial,
            out GameObject root)
        {
            root = CreateRoot(parent, CommissioningConsoleRootName);
            if (root == null || !IsAvailable)
                return false;

            var created = 0;
            created += CreatePart(
                root.transform,
                "MachineBed",
                MachineBedAssetPath,
                new Vector3(0f, 0.42f, 0f),
                Quaternion.identity,
                new Vector3(1.56f, 0.36f, 0.72f),
                bodyMaterial);
            created += CreatePart(
                root.transform,
                "StatusScreen",
                ScreenFlatAssetPath,
                new Vector3(0f, 1.20f, -0.56f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(1.05f, 0.95f, 0.14f),
                screenMaterial ?? bodyMaterial);
            created += CreatePart(
                root.transform,
                "ConfirmButton",
                ButtonAssetPath,
                new Vector3(-0.46f, 0.88f, -0.73f),
                Quaternion.identity,
                new Vector3(0.25f, 0.17f, 0.25f),
                accentMaterial ?? bodyMaterial);
            created += CreatePart(
                root.transform,
                "ServiceLever",
                LeverAssetPath,
                new Vector3(0.46f, 0.90f, -0.70f),
                Quaternion.identity,
                new Vector3(0.32f, 0.32f, 0.32f),
                accentMaterial ?? bodyMaterial);

            if (created == 0)
            {
                Object.DestroyImmediate(root);
                root = null;
                return false;
            }

            MarkStatic(root);
            return true;
        }

        private static bool HasAsset(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
        }

        private static GameObject CreateRoot(Transform parent, string name)
        {
            if (parent == null || !IsAvailable)
                return null;

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static int CreatePart(
            Transform parent,
            string name,
            string assetPath,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            return CreatePart(
                parent,
                name,
                assetPath,
                localPosition,
                Quaternion.identity,
                localScale,
                material);
        }

        private static int CreatePart(
            Transform parent,
            string name,
            string assetPath,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            return FactoryKitEnvironmentVisualFactory.TryCreateAsset(
                       parent,
                       name,
                       assetPath,
                       localPosition,
                       localRotation,
                       localScale,
                       material,
                       out _) ? 1 : 0;
        }

        private static void MarkStatic(GameObject root)
        {
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
