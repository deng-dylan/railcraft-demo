using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Builds an editor-only comparison scene for converted teammate CAD files.
    /// It deliberately stays outside EditorBuildSettings and the training flow.
    /// </summary>
    public static class CadModelPreviewGalleryBuilder
    {
        public const string ScenePath =
            "Assets/RailCraft/ThirdPerson/Scenes/CadModelPreviewGallery.unity";

        public const string Y25ModelPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/Y25Freight/Y25FreightBogie.fbx";

        public const string BettendorfModelPath =
            "Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/BettendorfFreight/BettendorfFreightBogie.fbx";

        public const string ScreenshotRelativePath =
            "Artifacts/Whitebox/ModelPreview/CadModelPreviewGallery.png";

        private const string AsphaltMaterialPath =
            "Assets/RailCraft/ThirdPerson/Art/Materials/FinalShowcase/FS_Asphalt.mat";

        private const string ConcreteMaterialPath =
            "Assets/RailCraft/ThirdPerson/Art/Materials/FinalShowcase/FS_Concrete.mat";

        private readonly struct ModelEntry
        {
            public ModelEntry(string key, string label, string assetPath, Vector3 position, float yaw)
            {
                Key = key;
                Label = label;
                AssetPath = assetPath;
                Position = position;
                Yaw = yaw;
            }

            public string Key { get; }
            public string Label { get; }
            public string AssetPath { get; }
            public Vector3 Position { get; }
            public float Yaw { get; }
        }

        [MenuItem("RailCraft/Models/Rebuild CAD Preview Gallery")]
        public static void RebuildFromMenu()
        {
            Build();
        }

        [MenuItem("RailCraft/Models/Open CAD Preview Gallery")]
        public static void OpenFromMenu()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
                Build();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var entries = new[]
            {
                new ModelEntry(
                    "Y25Freight",
                    "Y25 EUROPEAN FREIGHT BOGIE\nTRUE STEP / 582,877 TRIANGLES",
                    Y25ModelPath,
                    new Vector3(-3.4f, 0f, 0f),
                    17f),
                new ModelEntry(
                    "BettendorfFreight",
                    "BETTENDORF LOW-SPEED FREIGHT BOGIE\nTRUE STEP / 125,514 TRIANGLES",
                    BettendorfModelPath,
                    new Vector3(3.4f, 0f, 0f),
                    -17f)
            };

            foreach (var entry in entries)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath) == null)
                    throw new FileNotFoundException($"CAD preview model is missing: {entry.AssetPath}");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CadModelPreviewGallery";
            ConfigureRenderSettings();

            var root = new GameObject("CadModelPreviewGalleryRoot");
            var environment = CreateChild(root.transform, "Environment");
            var models = CreateChild(root.transform, "ImportedCadModels");
            var labels = CreateChild(root.transform, "Labels");
            var lighting = CreateChild(root.transform, "Lighting");

            var asphalt = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMaterialPath);
            var concrete = AssetDatabase.LoadAssetAtPath<Material>(ConcreteMaterialPath);
            BuildEnvironment(environment.transform, asphalt, concrete);
            foreach (var entry in entries)
                BuildModelStand(models.transform, labels.transform, scene, entry, concrete);

            CreateWorldLabel(
                labels.transform,
                "GalleryTitle",
                "TEAM CAD MODEL PREVIEW / REAL IMPORTED GEOMETRY",
                new Vector3(0f, 4.05f, 4.45f),
                Color.white,
                0.055f);
            CreateWorldLabel(
                labels.transform,
                "PendingStatus",
                "4 SOLIDWORKS FILES AWAIT PACK-AND-GO OR STEP/FBX EXPORT",
                new Vector3(0f, 3.55f, 4.43f),
                new Color(1f, 0.72f, 0.22f),
                0.04f);

            var camera = BuildCamera(root.transform);
            BuildLighting(lighting.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RenderScreenshot(camera);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"RAILCRAFT_CAD_PREVIEW_GALLERY_SUCCEEDED scene={ScenePath};" +
                $"models={entries.Length};screenshot={ScreenshotRelativePath}");
        }

        private static void BuildEnvironment(
            Transform parent,
            Material asphalt,
            Material concrete)
        {
            CreatePrimitive(
                parent,
                "GalleryFloor",
                PrimitiveType.Cube,
                new Vector3(0f, -0.24f, 0.6f),
                new Vector3(14f, 0.4f, 8.5f),
                asphalt);
            CreatePrimitive(
                parent,
                "RearWall",
                PrimitiveType.Cube,
                new Vector3(0f, 2.45f, 4.7f),
                new Vector3(14f, 5.4f, 0.25f),
                concrete);
        }

        private static void BuildModelStand(
            Transform modelParent,
            Transform labelParent,
            Scene scene,
            ModelEntry entry,
            Material standMaterial)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
            var instance = PrefabUtility.InstantiatePrefab(source, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate {entry.AssetPath}");

            instance.name = entry.Key + "_ImportedCadPreview";
            var authoredRotation = instance.transform.localRotation;
            var authoredScale = instance.transform.localScale;
            instance.transform.SetParent(modelParent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation =
                Quaternion.Euler(0f, entry.Yaw, 0f) * authoredRotation;
            instance.transform.localScale = authoredScale;

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException($"CAD preview has no renderers: {entry.AssetPath}");

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            instance.transform.position += entry.Position - new Vector3(
                bounds.center.x,
                bounds.min.y,
                bounds.center.z);

            CreatePrimitive(
                modelParent,
                entry.Key + "_Stand",
                PrimitiveType.Cube,
                entry.Position + new Vector3(0f, -0.12f, 0f),
                new Vector3(5.2f, 0.2f, 4.4f),
                standMaterial);
            CreateWorldLabel(
                labelParent,
                entry.Key + "_Label",
                entry.Label,
                entry.Position + new Vector3(0f, 2.05f, -2.35f),
                new Color(0.72f, 0.88f, 1f),
                0.036f);
        }

        private static Camera BuildCamera(Transform parent)
        {
            var cameraObject = CreateChild(parent, "PreviewCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 5.8f, -16.5f);
            var target = new Vector3(0f, 1.15f, 0.4f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                target - cameraObject.transform.position,
                Vector3.up);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 41f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.022f, 0.034f, 0.052f);
            camera.allowHDR = true;
            return camera;
        }

        private static void BuildLighting(Transform parent)
        {
            var keyObject = CreateChild(parent, "KeyDirectionalLight");
            keyObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.15f;
            key.color = new Color(0.88f, 0.94f, 1f);
            key.shadows = LightShadows.Soft;

            CreatePointLight(parent, "LeftFill", new Vector3(-5f, 4.2f, -3.5f), 19f);
            CreatePointLight(parent, "RightFill", new Vector3(5f, 4.2f, -3.5f), 19f);
            CreatePointLight(parent, "RearRim", new Vector3(0f, 4.8f, 3.4f), 23f);
        }

        private static void CreatePointLight(
            Transform parent,
            string name,
            Vector3 position,
            float intensity)
        {
            var item = CreateChild(parent, name);
            item.transform.position = position;
            var light = item.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = intensity;
            light.color = new Color(0.66f, 0.82f, 1f);
            light.shadows = LightShadows.None;
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var item = GameObject.CreatePrimitive(primitiveType);
            item.name = name;
            item.transform.SetParent(parent, true);
            item.transform.position = position;
            item.transform.localScale = scale;
            var collider = item.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
            var renderer = item.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
            return item;
        }

        private static GameObject CreateWorldLabel(
            Transform parent,
            string name,
            string value,
            Vector3 position,
            Color color,
            float characterSize)
        {
            var label = new GameObject(name, typeof(TextMesh));
            label.transform.SetParent(parent, true);
            label.transform.position = position;
            label.transform.rotation = Quaternion.identity;
            var text = label.GetComponent<TextMesh>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            if (text.font != null)
                label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return label;
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.34f, 0.46f);
            RenderSettings.ambientEquatorColor = new Color(0.09f, 0.14f, 0.2f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.035f, 0.05f);
            RenderSettings.fog = false;
        }

        private static void RenderScreenshot(Camera camera)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var outputPath = Path.Combine(projectRoot, ScreenshotRelativePath);
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
