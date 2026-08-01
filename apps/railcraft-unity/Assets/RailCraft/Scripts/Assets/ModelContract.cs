using UnityEngine;

namespace RailCraft.Assets
{
    public sealed class ModelContract : MonoBehaviour
    {
        public string assetKey;
        public string sourceVersion;
        public Vector3 localAxleDirection = Vector3.right;
        public Vector3 localUpDirection = Vector3.up;
        public bool authoredAtMeterScale = true;

        public string AssetKey => assetKey;
        public string SourceVersion => sourceVersion;
        public Vector3 LocalAxleDirection => localAxleDirection;
        public Vector3 LocalUpDirection => localUpDirection;
        public bool AuthoredAtMeterScale => authoredAtMeterScale;

        public void Configure(
            string configuredAssetKey,
            string configuredSourceVersion,
            Vector3 configuredAxleDirection,
            Vector3 configuredUpDirection,
            bool configuredAtMeterScale)
        {
            assetKey = configuredAssetKey;
            sourceVersion = configuredSourceVersion;
            localAxleDirection = configuredAxleDirection;
            localUpDirection = configuredUpDirection;
            authoredAtMeterScale = configuredAtMeterScale;
        }
    }
}
