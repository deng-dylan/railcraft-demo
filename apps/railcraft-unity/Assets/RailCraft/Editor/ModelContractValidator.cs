using System.Collections.Generic;
using RailCraft.Assets;
using RailCraft.Interaction;
using UnityEditor;
using UnityEngine;

namespace RailCraft.Editor
{
    public static class ModelContractValidator
    {
        private const string CatalogPath = "Assets/RailCraft/Art/PartPrefabCatalog.asset";

        [MenuItem("RailCraft/Validate Model Contracts")]
        public static void ValidateFromMenu()
        {
            var issues = ValidateProductionCatalog();
            if (issues.Count == 0)
            {
                Debug.Log("RailCraft model contracts are valid.");
                return;
            }

            foreach (var issue in issues)
                Debug.LogError(issue);
        }

        public static IReadOnlyList<string> ValidateProductionCatalog()
        {
            var issues = new List<string>();
            var catalog = AssetDatabase.LoadAssetAtPath<PartPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                issues.Add($"catalog_missing:{CatalogPath}");
                return issues;
            }

            var seenKeys = new HashSet<string>();
            foreach (var entry in catalog.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.assetKey))
                {
                    issues.Add("catalog_asset_key_blank");
                    continue;
                }

                if (!seenKeys.Add(entry.assetKey))
                    issues.Add($"catalog_asset_key_duplicate:{entry.assetKey}");
                if (entry.prefab == null)
                {
                    issues.Add($"prefab_missing:{entry.assetKey}");
                    continue;
                }

                ValidatePrefab(entry.assetKey, entry.prefab, issues);
            }

            return issues;
        }

        private static void ValidatePrefab(string catalogKey, GameObject prefab, ICollection<string> issues)
        {
            var prefix = $"{catalogKey}:";
            var transform = prefab.transform;
            var contract = prefab.GetComponent<ModelContract>();
            var draggable = prefab.GetComponent<DraggableModule>();
            var collider = prefab.GetComponent<Collider>();
            var visualRoot = transform.Find("VisualRoot");
            var highlight = transform.Find("Highlight");

            if (contract == null)
                issues.Add(prefix + "model_contract_missing");
            else
            {
                if (contract.AssetKey != catalogKey)
                    issues.Add(prefix + "contract_asset_key_mismatch");
                if (string.IsNullOrWhiteSpace(contract.SourceVersion))
                    issues.Add(prefix + "source_version_blank");
                if (!contract.AuthoredAtMeterScale)
                    issues.Add(prefix + "meter_scale_not_authored");
                if (contract.LocalAxleDirection.sqrMagnitude < 0.99f)
                    issues.Add(prefix + "axle_direction_invalid");
                if (contract.LocalUpDirection.sqrMagnitude < 0.99f)
                    issues.Add(prefix + "up_direction_invalid");
            }

            if (draggable == null)
                issues.Add(prefix + "draggable_module_missing");
            if (collider == null)
                issues.Add(prefix + "root_collider_missing");
            if (visualRoot == null)
                issues.Add(prefix + "visual_root_missing");
            if (highlight == null)
                issues.Add(prefix + "highlight_missing");
            if (transform.localPosition.sqrMagnitude > 0.000001f)
                issues.Add(prefix + "root_position_not_zero");
            if (Quaternion.Angle(transform.localRotation, Quaternion.identity) >= 0.01f)
                issues.Add(prefix + "root_rotation_not_identity");
            if ((transform.localScale - Vector3.one).sqrMagnitude > 0.000001f)
                issues.Add(prefix + "root_scale_not_one");

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                issues.Add(prefix + "renderer_missing");
                return;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled)
                    issues.Add(prefix + $"renderer_disabled:{renderer.name}");
                bounds.Encapsulate(renderer.bounds);
            }

            if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
                issues.Add(prefix + "bounds_empty");
        }
    }
}
