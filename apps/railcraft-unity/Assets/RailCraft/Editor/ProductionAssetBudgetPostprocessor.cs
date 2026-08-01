using System;
using System.Collections.Generic;
using System.Linq;
using RailCraft.Assets;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace RailCraft.Editor
{
    public sealed class ProductionAssetBudgetPostprocessor : AssetPostprocessor
    {
        public const int RequiredLodLevels = 3;
        public const int HeroTextureMaxSize = 2048;
        public const int FactoryPropTextureMaxSize = 1024;

        public override int GetPostprocessOrder() => 100;

        public override uint GetVersion() => 2;

        public void OnPreprocessMaterialDescription(MaterialDescription description,
            Material material, AnimationClip[] clips)
        {
            ConfigureImportedProductionMaterial(material);
        }

        public void OnPostprocessMaterial(Material material)
        {
            ConfigureImportedProductionMaterial(material);
        }

        private void ConfigureImportedProductionMaterial(Material material)
        {
            if (!IsProductionModelPath(assetPath))
                return;
            if (!TryEnsurePropertyBlockEmission(material, out var issue))
                Debug.LogError($"{assetPath}: {issue}", material);
        }

        private void OnPreprocessTexture()
        {
            var limit = ResolveTextureLimit(assetPath);
            if (limit == 0 || assetImporter is not TextureImporter importer)
                return;
            importer.maxTextureSize = limit;
        }

        public static bool IsProductionModelPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            return path.Replace('\\', '/').StartsWith(
                "Assets/RailCraft/Art/Models/Production/", StringComparison.Ordinal);
        }

        public static bool TryEnsurePropertyBlockEmission(Material material, out string issue)
        {
            issue = FindHighlightShaderSupportIssue(material);
            if (issue != null)
                return false;

            var keyword = new LocalKeyword(material.shader, "_EMISSION");
            material.globalIlluminationFlags =
                MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.EnableKeyword(keyword);
            EditorUtility.SetDirty(material);
            return true;
        }

        public static string GetPropertyBlockEmissionIssue(Material material)
        {
            var issue = FindHighlightShaderSupportIssue(material);
            if (issue != null)
                return issue;

            var keyword = new LocalKeyword(material.shader, "_EMISSION");
            if (!material.IsKeywordEnabled(keyword))
                return "has _EMISSION disabled";
            if (material.globalIlluminationFlags !=
                    MaterialGlobalIlluminationFlags.RealtimeEmissive)
                return "must use exactly RealtimeEmissive GI flags";
            return null;
        }

        private static string FindHighlightShaderSupportIssue(Material material)
        {
            if (material == null)
                return "material is null";
            if (!material.HasProperty("_BaseColor") && !material.HasProperty("_Color"))
                return "has no highlightable base color";
            if (!material.HasProperty("_EmissionColor"))
                return "has no _EmissionColor";
            var keyword = new LocalKeyword(material.shader, "_EMISSION");
            return keyword.isValid ? null : "shader has no _EMISSION keyword";
        }

        public static int ResolveTextureLimit(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return 0;
            var normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/RailCraft/Art/", StringComparison.Ordinal))
                return 0;
            if (normalized.Contains("/Textures/Props/", StringComparison.Ordinal)
                || normalized.Contains("/Textures/Factory/", StringComparison.Ordinal)
                || normalized.Contains("/Models/Factory/", StringComparison.Ordinal))
                return FactoryPropTextureMaxSize;
            if (normalized.Contains("/Textures/Hero/", StringComparison.Ordinal)
                || normalized.Contains("/Textures/Production/", StringComparison.Ordinal)
                || normalized.Contains("/Models/Production/", StringComparison.Ordinal))
                return HeroTextureMaxSize;
            return 0;
        }

        public static IReadOnlyList<string> FindProductionLodViolations()
        {
            var violations = new List<string>();
            var guids = AssetDatabase.FindAssets("t:Prefab",
                new[] { "Assets/RailCraft/Art/Prefabs" });
            foreach (var path in guids.Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;
                var contracts = prefab.GetComponentsInChildren<ModelContract>(true)
                    .Where(contract => contract != null
                        && !string.IsNullOrWhiteSpace(contract.AssetKey)
                        && contract.AssetKey.Contains("production", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (contracts.Length == 0)
                    continue;

                foreach (var contract in contracts)
                {
                    var groups = contract.GetComponentsInChildren<LODGroup>(true);
                    if (groups.Length == 0)
                    {
                        violations.Add($"{path}: {contract.AssetKey} has no LODGroup");
                        continue;
                    }
                    foreach (var group in groups)
                    {
                        if (group.GetLODs().Length != RequiredLodLevels)
                            violations.Add($"{path}: {group.name} must have exactly "
                                + $"{RequiredLodLevels} LOD levels");
                    }
                }
            }
            return violations;
        }

        public static IReadOnlyList<string> FindHighlightMaterialViolations()
        {
            var violations = new List<string>();
            var catalog = AssetDatabase.LoadAssetAtPath<PartPrefabCatalog>(
                FactoryPresentationBuilder.CatalogPath);
            if (catalog == null)
            {
                violations.Add(FactoryPresentationBuilder.CatalogPath + ": catalog is missing");
                return violations;
            }

            foreach (var entry in catalog.Entries.Where(item => item?.prefab != null))
            {
                var module = entry.prefab.GetComponent<RailCraft.Interaction.DraggableModule>();
                if (module == null || module.VisualRoot == null)
                {
                    violations.Add($"{entry.assetKey}: draggable visual root is missing");
                    continue;
                }

                var renderers = module.VisualRoot.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    violations.Add($"{entry.assetKey}: draggable visual root has no Renderer");
                    continue;
                }

                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                    {
                        violations.Add($"{entry.assetKey}: {renderer.name} has no material");
                        continue;
                    }

                    foreach (var material in materials)
                        ValidateHighlightMaterial(entry.assetKey, renderer, material, violations);
                }
            }
            return violations;
        }

        private static void ValidateHighlightMaterial(string assetKey, Renderer renderer,
            Material material, ICollection<string> violations)
        {
            var path = material == null ? null : AssetDatabase.GetAssetPath(material);
            var label = material == null
                ? renderer.name + " null material slot"
                : string.IsNullOrWhiteSpace(path) ? material.name : path;
            var issue = GetPropertyBlockEmissionIssue(material);
            if (issue != null)
                violations.Add($"{assetKey}: {label} {issue}");
        }

        [MenuItem("RailCraft/Validate Production Asset Budgets")]
        public static void ValidateFromMenu()
        {
            var violations = FindProductionLodViolations()
                .Concat(FindHighlightMaterialViolations())
                .ToArray();
            if (violations.Length > 0)
                throw new InvalidOperationException(string.Join(Environment.NewLine, violations));
            Debug.Log("RAILCRAFT_PRODUCTION_ASSET_BUDGETS_VALID");
        }
    }
}