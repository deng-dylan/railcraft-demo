using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Builds the visual layer for knowledge, assembly and commissioning
    /// stations from the imported operation-table model. Gameplay triggers
    /// remain on station roots; solid workbench colliders are generated here.
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
        public const string OperationTableAssetPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Components/RebelHideoutOperationTable/Models/RebelHideoutOperationTable.dae";

        public const string QuizWorkbenchRootName = "QuizWorkbenchVisual";
        public const string AssemblyTableRootName = "AssemblyTableVisual";
        public const string CommissioningConsoleRootName = "CommissioningConsoleVisual";

        public static bool IsAvailable => HasAsset(OperationTableAssetPath);

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

            if (!CreateOperationTable(root.transform, new Vector3(2.7f, 1.13f, 1.356f)))
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
            out GameObject root,
            float surfaceHeight = 0.8f)
        {
            root = CreateRoot(parent, AssemblyTableRootName);
            if (root == null || !IsAvailable)
                return false;

            var span = Mathf.Clamp(spanMultiplier, 0.75f, 1.6f);
            if (!CreateOperationTable(root.transform,
                    new Vector3(5.4f * span, Mathf.Clamp(surfaceHeight, 0.5f, 1.2f), 3.3f * span)))
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

            if (!CreateOperationTable(root.transform, new Vector3(2.7f, 1.13f, 1.356f)))
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

        private static bool CreateOperationTable(
            Transform parent,
            Vector3 targetSize)
        {
            if (!FactoryKitEnvironmentVisualFactory.TryCreateAsset(
                    parent,
                    "OperationTable",
                    OperationTableAssetPath,
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    null,
                    out var visual))
            {
                return false;
            }

            // The source declares meter=0.01 despite a roughly 2-unit wide mesh.
            // Fit the imported bounds to display dimensions, independently of
            // importer units. These dimensions are layout choices, not engineering data.
            var bounds = CalculateLocalMeshBounds(visual, parent);
            if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
            {
                Object.DestroyImmediate(visual);
                return false;
            }
            var scale = new Vector3(targetSize.x / bounds.size.x,
                targetSize.y / bounds.size.y, targetSize.z / bounds.size.z);
            visual.transform.localScale = scale;
            visual.transform.localPosition = Vector3.Scale(
                new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z), scale);
            AddSolidWorkbenchColliders(parent, targetSize);
            return true;
        }

        public static Bounds CalculateLocalMeshBounds(GameObject visual, Transform relativeTo)
        {
            var bounds = new Bounds();
            var initialized = false;
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                var meshBounds = filter.sharedMesh.bounds;
                var matrix = relativeTo.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (var corner = 0; corner < 8; corner++)
                {
                    var point = matrix.MultiplyPoint3x4(meshBounds.center + Vector3.Scale(
                        meshBounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1,
                            (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                    if (!initialized)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                    else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }

        private static void AddSolidWorkbenchColliders(Transform parent, Vector3 size)
        {
            var collisionRoot = new GameObject("WorkbenchCollision");
            collisionRoot.transform.SetParent(parent, false);

            // Tabletop and two pedestal supports follow the supplied mesh silhouette.
            // A character capsule hits the top from every side; the under-table
            // gap stays open. No mesh physics or oversized invisible rear wall.
            AddBoxCollider(collisionRoot.transform, "Top", Vector3.Scale(size, new Vector3(0f, 0.91f, 0f)),
                Vector3.Scale(size, new Vector3(0.98f, 0.18f, 0.98f)));
            AddBoxCollider(collisionRoot.transform, "LeftSupport", Vector3.Scale(size, new Vector3(-0.357f, 0.41f, 0f)),
                Vector3.Scale(size, new Vector3(0.25f, 0.82f, 0.70f)));
            AddBoxCollider(collisionRoot.transform, "RightSupport", Vector3.Scale(size, new Vector3(0.357f, 0.41f, 0f)),
                Vector3.Scale(size, new Vector3(0.25f, 0.82f, 0.70f)));
        }

        private static void AddBoxCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            var collider = item.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            collider.isTrigger = false;
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
