using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
    }
}
