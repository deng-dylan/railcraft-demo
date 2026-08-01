using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailCraft.Assets
{
    [CreateAssetMenu(menuName = "RailCraft/Part Prefab Catalog")]
    public sealed class PartPrefabCatalog : ScriptableObject
    {
        [SerializeField] private PartPrefabEntry[] entries = Array.Empty<PartPrefabEntry>();

        public IReadOnlyList<PartPrefabEntry> Entries => entries;

        public GameObject Resolve(string assetKey)
        {
            if (string.IsNullOrWhiteSpace(assetKey))
                return null;

            foreach (var entry in entries)
            {
                if (entry != null && string.Equals(entry.assetKey, assetKey, StringComparison.Ordinal))
                    return entry.prefab;
            }

            return null;
        }

        public void Configure(PartPrefabEntry[] configuredEntries)
        {
            entries = configuredEntries ?? Array.Empty<PartPrefabEntry>();
        }
    }
}
