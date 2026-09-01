using UnityEditor;
using UnityEngine;

namespace RailCraft.Editor
{
    public static class ProjectConfigurator
    {
        [MenuItem("RailCraft/Apply Project Configuration")]
        public static void Apply()
        {
            PlayerSettings.companyName = "RailCraft Team";
            PlayerSettings.productName = "RailCraft";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var audioManager = Unsupported.GetSerializedAssetInterfaceSingleton("AudioManager");
            var serializedAudioManager = new SerializedObject(audioManager);
            serializedAudioManager.FindProperty("m_DisableAudio").boolValue = true;
            serializedAudioManager.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
            AssetDatabase.SaveAssets();
        }
    }
}
