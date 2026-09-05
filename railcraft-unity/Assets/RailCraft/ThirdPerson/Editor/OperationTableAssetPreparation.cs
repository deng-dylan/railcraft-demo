using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailCraft.ThirdPerson.Editor
{
    /// <summary>
    /// Prepares the imported operation table as a presentation-style (展示风格)
    /// asset. Its materials do not establish engineering specifications.
    /// Call Prepare before rebuilding the scene; no automatic import hook runs.
    /// Source images remain intact, and generated assets keep stable paths/GUIDs.
    /// </summary>
    public static class OperationTableAssetPreparation
    {
        public const string AssetRootPath =
            "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Components/RebelHideoutOperationTable";
        public const string ModelAssetPath = AssetRootPath + "/Models/RebelHideoutOperationTable.dae";
        public const string MaterialRootPath = AssetRootPath + "/Materials";
        public const string GeneratedTextureRootPath = AssetRootPath + "/Textures/Generated";

        private static readonly string[] MaterialIds = { "1001", "1002" };
        private static readonly string[] SourceSuffixes =
            { "albedo.jpg", "metallic.jpg", "roughness.jpg", "normal.png", "AO.jpg" };

        public static void Prepare()
        {
            RequireFile(ModelAssetPath);
            foreach (var id in MaterialIds)
                foreach (var suffix in SourceSuffixes)
                    RequireFile(SourcePath(id, suffix));

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("Operation table requires the URP/Lit shader.");

            EnsureFolder(MaterialRootPath);
            EnsureFolder(GeneratedTextureRootPath);
            var materials = new Material[MaterialIds.Length];
            for (var index = 0; index < MaterialIds.Length; index++)
            {
                var id = MaterialIds[index];
                foreach (var suffix in SourceSuffixes)
                    ConfigureTexture(SourcePath(id, suffix), suffix == "albedo.jpg",
                        suffix == "normal.png" ? TextureImporterType.NormalMap : TextureImporterType.Default);

                var packedPath = GeneratedTextureRootPath + "/" + id + "_MetallicGloss.png";
                GenerateMetallicGloss(id, packedPath);
                ConfigureTexture(packedPath, false, TextureImporterType.Default, true);
                materials[index] = PrepareMaterial(id, packedPath, shader);
            }

            var importer = RequireImporter<ModelImporter>(ModelAssetPath);
            var dirty = false;
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                dirty = true;
            }

            var mappings = importer.GetExternalObjectMap();
            for (var index = 0; index < MaterialIds.Length; index++)
            {
                var source = new AssetImporter.SourceAssetIdentifier(typeof(Material), MaterialIds[index]);
                if (mappings.TryGetValue(source, out var current) && current == materials[index])
                    continue;
                importer.AddRemap(source, materials[index]);
                dirty = true;
            }
            if (dirty)
                importer.SaveAndReimport();
        }

        private static void ConfigureTexture(
            string path, bool sRgb, TextureImporterType type, bool packed = false)
        {
            var importer = RequireImporter<TextureImporter>(path);
            var dirty = false;
            if (importer.textureType != type)
            {
                importer.textureType = type;
                dirty = true;
            }
            if (importer.sRGBTexture != sRgb)
            {
                importer.sRGBTexture = sRgb;
                dirty = true;
            }
            if (packed && importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                dirty = true;
            }
            if (packed && importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = false;
                dirty = true;
            }
            if (type == TextureImporterType.NormalMap && importer.convertToNormalmap)
            {
                importer.convertToNormalmap = false;
                dirty = true;
            }
            if (dirty)
                importer.SaveAndReimport();
        }

        private static void GenerateMetallicGloss(string id, string outputPath)
        {
            // Decode source bytes into temporary linear textures. This avoids
            // enabling Read/Write on imported runtime textures or packing sRGB data.
            Texture2D metallic = null;
            Texture2D roughness = null;
            Texture2D packed = null;
            try
            {
                metallic = LoadLinearSource(SourcePath(id, "metallic.jpg"));
                roughness = LoadLinearSource(SourcePath(id, "roughness.jpg"));
                if (metallic.width != roughness.width || metallic.height != roughness.height)
                    throw new InvalidOperationException("Operation table " + id +
                        " metallic and roughness images must have matching dimensions.");

                var metalPixels = metallic.GetPixels32();
                var roughPixels = roughness.GetPixels32();
                for (var index = 0; index < metalPixels.Length; index++)
                    metalPixels[index] = new Color32(metalPixels[index].r, 0, 0,
                        (byte)(255 - roughPixels[index].r));

                packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
                packed.SetPixels32(metalPixels);
                packed.Apply(false, false);
                var bytes = packed.EncodeToPNG();
                var fullPath = FullPath(outputPath);
                if (File.Exists(fullPath) && File.ReadAllBytes(fullPath).SequenceEqual(bytes))
                    return;
                File.WriteAllBytes(fullPath, bytes);
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                if (metallic != null) Object.DestroyImmediate(metallic);
                if (roughness != null) Object.DestroyImmediate(roughness);
                if (packed != null) Object.DestroyImmediate(packed);
            }
        }

        private static Texture2D LoadLinearSource(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (ImageConversion.LoadImage(texture, File.ReadAllBytes(FullPath(path)), false))
                return texture;
            Object.DestroyImmediate(texture);
            throw new InvalidOperationException("Cannot decode operation table texture: " + path);
        }

        private static Material PrepareMaterial(string id, string packedPath, Shader shader)
        {
            var path = MaterialRootPath + "/OperationTable_" + id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "OperationTable_" + id };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }

            SetTexture(material, "_BaseMap", SourcePath(id, "albedo.jpg"));
            SetTexture(material, "_MetallicGlossMap", packedPath);
            SetTexture(material, "_BumpMap", SourcePath(id, "normal.png"));
            SetTexture(material, "_OcclusionMap", SourcePath(id, "AO.jpg"));
            if (material.GetColor("_BaseColor") != Color.white)
            {
                material.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(material);
            }
            SetFloat(material, "_WorkflowMode", 1f);
            SetFloat(material, "_Metallic", 1f);
            SetFloat(material, "_Smoothness", 1f);
            SetFloat(material, "_SmoothnessTextureChannel", 0f);
            SetFloat(material, "_BumpScale", 1f);
            SetFloat(material, "_OcclusionStrength", 1f);
            SetKeyword(material, "_METALLICSPECGLOSSMAP", true);
            SetKeyword(material, "_NORMALMAP", true);
            SetKeyword(material, "_OCCLUSIONMAP", true);
            SetKeyword(material, "_SPECULAR_SETUP", false);
            SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", false);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void SetTexture(Material material, string property, string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                throw new InvalidOperationException("Operation table texture did not import: " + path);
            if (material.GetTexture(property) != texture)
            {
                material.SetTexture(property, texture);
                EditorUtility.SetDirty(material);
            }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.GetFloat(property) != value)
            {
                material.SetFloat(property, value);
                EditorUtility.SetDirty(material);
            }
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (material.IsKeywordEnabled(keyword) == enabled)
                return;
            if (enabled) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
            EditorUtility.SetDirty(material);
        }

        private static T RequireImporter<T>(string path) where T : AssetImporter
        {
            var importer = AssetImporter.GetAtPath(path) as T;
            if (importer != null)
                return importer;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as T;
            if (importer == null)
                throw new InvalidOperationException("Operation table importer is unavailable: " + path);
            return importer;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var separator = path.LastIndexOf('/');
            var parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }

        private static string SourcePath(string id, string suffix) =>
            AssetRootPath + "/Textures/" + id + "_" + suffix;

        private static string FullPath(string path) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));

        private static void RequireFile(string path)
        {
            if (!File.Exists(FullPath(path)))
                throw new FileNotFoundException("Operation table source asset is missing.", path);
        }
    }
}
