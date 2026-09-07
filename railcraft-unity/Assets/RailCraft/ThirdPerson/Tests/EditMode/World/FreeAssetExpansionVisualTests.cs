using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class FreeAssetExpansionVisualTests
    {
        private GameObject root;
        private Material material;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("FreeAssetExpansionTests");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Assert.That(shader, Is.Not.Null);
            material = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
            if (material != null)
                UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void CuratedCc0AssetsAndSourceRecordsAreImported()
        {
            Assert.That(FreeAssetExpansionVisualFactory.AreComponentAssetsAvailable, Is.True);
            Assert.That(FreeAssetExpansionVisualFactory.AreEnvironmentAssetsAvailable, Is.True);

            foreach (var path in new[]
            {
                FreeAssetExpansionVisualFactory.SpringPath,
                FreeAssetExpansionVisualFactory.PipesPath,
                FreeAssetExpansionVisualFactory.MetalSupportPath,
                FreeAssetExpansionVisualFactory.StopValvePath,
                FreeAssetExpansionVisualFactory.DrillPath,
                FreeAssetExpansionVisualFactory.ConePath,
                FreeAssetExpansionVisualFactory.BuildingCPath,
                FreeAssetExpansionVisualFactory.HighSpeedFrontPath
            })
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path), Is.Not.Null, path);
            }

            foreach (var path in new[]
            {
                "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Components/QuaterniusUtilityParts/SOURCE.md",
                "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Components/BlendSwapStopValve/SOURCE.md",
                "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/LowPolyConstruction/SOURCE.md",
                "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/CC0Safety/SOURCE.md",
                "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/KenneyCityIndustrial/SOURCE.md",
                "Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/QuaterniusModularTrain/SOURCE.md"
            })
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(path), Is.Not.Null, path);
            }
        }

        [TestCase(PartId.PrimaryElasticElement, "CC0_Spring_Left")]
        [TestCase(PartId.SensorBracket, "CC0_SensorMount")]
        [TestCase(PartId.PrimaryPositioningElement, "CC0_PositioningSupport_A")]
        [TestCase(PartId.HeightControlElement, "CC0_StopValve")]
        public void ComponentFactoryCreatesMovableVisualsWithoutImportedCollision(
            PartId partId,
            string expectedChild)
        {
            Assert.That(
                FreeAssetExpansionVisualFactory.TryCreatePartVisual(
                    root.transform,
                    "Component",
                    partId,
                    material,
                    out var visual),
                Is.True);

            Assert.That(visual, Is.Not.Null);
            Assert.That(visual.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
            Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(visual.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name == expectedChild), Is.True, expectedChild);
            Assert.That(visual.GetComponentsInChildren<Transform>(true)
                .All(item => GameObjectUtility.GetStaticEditorFlags(item.gameObject) == 0), Is.True);

            var bounds = BoundsOf(visual);
            Assert.That(bounds.size.magnitude, Is.GreaterThan(0.1f));
            Assert.That(bounds.size.magnitude, Is.LessThan(4f));
        }

        [Test]
        public void EnvironmentFactoryBuildsToolsSafetyExteriorAndDistantTrainsAsVisualOnly()
        {
            var created = FreeAssetExpansionVisualFactory.BuildDefaultVisuals(
                root.transform,
                material,
                material,
                material,
                material,
                material,
                material);

            Assert.That(created, Is.EqualTo(26));
            var expansion = root.transform.Find(FreeAssetExpansionVisualFactory.RootName);
            Assert.That(expansion, Is.Not.Null);
            foreach (var groupName in new[]
            {
                FreeAssetExpansionVisualFactory.ToolDisplayName,
                FreeAssetExpansionVisualFactory.SafetyDisplayName,
                FreeAssetExpansionVisualFactory.ExteriorName,
                FreeAssetExpansionVisualFactory.DistantTrainName
            })
            {
                Assert.That(expansion.Find(groupName), Is.Not.Null, groupName);
            }

            Assert.That(expansion.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(expansion.GetComponentsInChildren<Renderer>(true).Length,
                Is.GreaterThanOrEqualTo(26));
            Assert.That(expansion.GetComponentsInChildren<Transform>(true)
                .All(item => GameObjectUtility.GetStaticEditorFlags(item.gameObject) != 0), Is.True);
        }

        private static Bounds BoundsOf(GameObject owner)
        {
            var renderers = owner.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Visual has no renderer bounds.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
