using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.Tests.EditMode
{
    public sealed class ProjectConfigurationTests
    {
        [Test]
        public void ProjectTargetsWindowsLinearColorSpace()
        {
            Assert.That(PlayerSettings.colorSpace, Is.EqualTo(ColorSpace.Linear));
            Assert.That(EditorUserBuildSettings.activeBuildTarget,
                Is.EqualTo(BuildTarget.StandaloneWindows64));
            Assert.That(PlayerSettings.productName, Is.EqualTo("RailCraft"));
            Assert.That(PlayerSettings.companyName, Is.EqualTo("RailCraft Team"));
        }

        [Test]
        public void ProjectHasNoAnalyticsMultiplayerOrXrPackages()
        {
            var packageNames = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Select(packageInfo => packageInfo.name)
                .ToArray();

            Assert.That(packageNames, Does.Not.Contain("com.unity.analytics"));
            Assert.That(packageNames, Does.Not.Contain("com.unity.purchasing"));
            Assert.That(packageNames, Does.Not.Contain("com.unity.services.core"));
            Assert.That(packageNames, Does.Not.Contain("com.unity.multiplayer.center"));
            Assert.That(packageNames, Does.Not.Contain("com.unity.xr.legacyinputhelpers"));
            Assert.That(packageNames, Does.Not.Contain("com.unity.modules.unityanalytics"));
            Assert.That(packageNames, Does.Not.Contain("com.unity.modules.vr"));
            Assert.That(packageNames, Does.Not.Contain("com.unity.modules.xr"));
        }

        [Test]
        public void ProjectDoesNotUseTemplateInputActions()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions"), Is.Null);
            Assert.That(EditorBuildSettings.TryGetConfigObject(
                "com.unity.input.settings.actions", out InputActionAsset _), Is.False);
        }

        [Test]
        public void ProjectDisablesUnityAudio()
        {
            var audioManager = Unsupported.GetSerializedAssetInterfaceSingleton("AudioManager");
            var serializedAudioManager = new SerializedObject(audioManager);

            Assert.That(serializedAudioManager.FindProperty("m_DisableAudio").boolValue,
                Is.True);
        }
    }
}
