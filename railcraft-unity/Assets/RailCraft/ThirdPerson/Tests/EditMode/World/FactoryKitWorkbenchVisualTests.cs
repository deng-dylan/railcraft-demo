using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class FactoryKitWorkbenchVisualTests
    {
        [Test]
        public void FactoryKitWorkbenchSourceAssetsAreAvailable()
        {
            Assert.That(FactoryKitWorkbenchVisualFactory.IsAvailable, Is.True);
        }

        [Test]
        public void QuizWorkbenchBuildsFourVisualOnlyModules()
        {
            var parent = new GameObject("QuizWorkbenchTestRoot");
            try
            {
                Assert.That(
                    FactoryKitWorkbenchVisualFactory.TryBuildQuizWorkbench(
                        parent.transform,
                        RequireMaterial("WB_Steel.mat"),
                        RequireMaterial("WB_Station.mat"),
                        RequireMaterial("WB_Running.mat"),
                        out var root),
                    Is.True);
                Assert.That(root.transform.childCount, Is.EqualTo(4));
                Assert.That(root.transform.Find("MachineBed"), Is.Not.Null);
                Assert.That(root.transform.Find("HmiScreen"), Is.Not.Null);
                Assert.That(root.transform.Find("ConfirmButton"), Is.Not.Null);
                Assert.That(root.transform.Find("ModeLever"), Is.Not.Null);
                AssertVisualOnly(root);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void AssemblyAndCommissioningStationsUseReusableFactoryModules()
        {
            var parent = new GameObject("FactoryStationVisualTestRoot");
            try
            {
                var body = RequireMaterial("WB_Steel.mat");
                var screen = RequireMaterial("WB_Station.mat");
                var accent = RequireMaterial("WB_Safety.mat");
                Assert.That(
                    FactoryKitWorkbenchVisualFactory.TryBuildAssemblyTable(
                        parent.transform,
                        body,
                        screen,
                        accent,
                        1f,
                        out var table),
                    Is.True);
                Assert.That(
                    FactoryKitWorkbenchVisualFactory.TryBuildCommissioningConsole(
                        parent.transform,
                        body,
                        screen,
                        accent,
                        out var console),
                    Is.True);
                Assert.That(table.transform.childCount, Is.EqualTo(5));
                Assert.That(console.transform.childCount, Is.EqualTo(4));
                AssertVisualOnly(table);
                AssertVisualOnly(console);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static void AssertVisualOnly(GameObject root)
        {
            Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<Animator>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<AudioSource>(true), Is.Empty);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            Assert.That(
                renderers.All(renderer =>
                    renderer.sharedMaterials.Length > 0 &&
                    renderer.sharedMaterials.All(material => material != null)),
                Is.True);
        }

        private static Material RequireMaterial(string fileName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/RailCraft/ThirdPerson/Art/Materials/" + fileName);
            Assert.That(material, Is.Not.Null, fileName);
            return material;
        }
    }
}
