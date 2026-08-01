using System;
using UnityEngine;

namespace RailCraft.Assets
{
    [Serializable]
    public sealed class PartPrefabEntry
    {
        public string assetKey;
        public GameObject prefab;

        public PartPrefabEntry(string configuredAssetKey, GameObject configuredPrefab)
        {
            assetKey = configuredAssetKey;
            prefab = configuredPrefab;
        }
    }
}
