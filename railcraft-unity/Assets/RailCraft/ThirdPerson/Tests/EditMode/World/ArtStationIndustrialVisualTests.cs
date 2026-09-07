using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class ArtStationIndustrialVisualTests
    {
        private const int ExpectedPropCount = 9;

        [Test]
        public void FreeIndustrialDressingBuildsCuratedProps()
        {
            var parent = new GameObject("ArtStationIndustrialTestRoot");
            try
            {
                Assert.That(ArtStationIndustrialVisualFactory.IsForkliftAvailable, Is.True);
                Assert.That(ArtStationIndustrialVisualFactory.IsEquipmentAvailable, Is.True);
                Assert.That(ArtStationIndustrialVisualFactory.IsShelvingAvailable, Is.True);

                var created = ArtStationIndustrialVisualFactory.BuildDefaultVisuals(
                    parent.transform,
                    AssetDatabase.LoadAssetAtPath<Material>(
                        "Assets/RailCraft/ThirdPerson/Art/Materials/WB_Steel.mat"));
                Assert.That(created, Is.EqualTo(ExpectedPropCount));

                var root = parent.transform.Find(ArtStationIndustrialVisualFactory.RootName);
                Assert.That(root, Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.ForkliftName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.BenchGrinderName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.CableReelName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.CementMixerOneName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.CementMixerTwoName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.ConcreteBarrierName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.ShelfAisleName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.ShelfRedName), Is.Not.Null);
                Assert.That(root.Find(ArtStationIndustrialVisualFactory.ShelfWhiteName), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void FreeIndustrialDressingIsVisualOnlyAndUsesProjectMaterial()
        {
            var parent = new GameObject("ArtStationIndustrialContractRoot");
            try
            {
                ArtStationIndustrialVisualFactory.BuildDefaultVisuals(parent.transform);
                var root = parent.transform.Find(ArtStationIndustrialVisualFactory.RootName);
                Assert.That(root, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Animator>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Light>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<AudioSource>(true), Is.Empty);

                var expected = AssetDatabase.LoadAssetAtPath<Material>(
                    ArtStationIndustrialVisualFactory.MaterialPath);
                Assert.That(expected, Is.Not.Null);
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.That(renderer.sharedMaterials, Is.Not.Empty, renderer.name);
                    Assert.That(renderer.sharedMaterials.All(item => item == expected), Is.True,
                        renderer.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void FreeIndustrialDressingStaysInsideFactoryEnvelope()
        {
            var parent = new GameObject("ArtStationIndustrialBoundsRoot");
            try
            {
                ArtStationIndustrialVisualFactory.BuildDefaultVisuals(parent.transform);
                var root = parent.transform.Find(ArtStationIndustrialVisualFactory.RootName);
                Assert.That(root, Is.Not.Null);
                foreach (var child in root.Cast<Transform>())
                {
                    var renderers = child.GetComponentsInChildren<Renderer>(true);
                    Assert.That(renderers, Is.Not.Empty, child.name);
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers.Skip(1))
                        bounds.Encapsulate(renderer.bounds);

                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-27.7f), child.name);
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(27.7f), child.name);
                    Assert.That(bounds.min.z, Is.GreaterThanOrEqualTo(-20.8f), child.name);
                    Assert.That(bounds.max.z, Is.LessThanOrEqualTo(20.8f), child.name);
                    Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(-0.05f), child.name);
                    Assert.That(bounds.max.y, Is.LessThan(10f), child.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
