using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class ArtAlphaEnvironmentVisualTests
    {
        private const int ExpectedVisualCount = 14;

        [Test]
        public void CuratedEnvironmentBuildsCraneAndWestMaintenanceDisplay()
        {
            var parent = new GameObject("ArtAlphaEnvironmentTestRoot");
            try
            {
                Assert.That(ArtAlphaEnvironmentVisualFactory.IsCraneAvailable, Is.True);
                Assert.That(ArtAlphaEnvironmentVisualFactory.IsRailKitAvailable, Is.True);
                Assert.That(ArtAlphaEnvironmentVisualFactory.IsFloorKitAvailable, Is.True);
                Assert.That(ArtAlphaEnvironmentVisualFactory.AreContainersAvailable, Is.True);
                Assert.That(ArtAlphaEnvironmentVisualFactory.IsSubstationAvailable, Is.True);

                var created = ArtAlphaEnvironmentVisualFactory.BuildDefaultVisuals(parent.transform);
                Assert.That(created, Is.EqualTo(ExpectedVisualCount));

                var root = parent.transform.Find(ArtAlphaEnvironmentVisualFactory.RootName);
                Assert.That(root, Is.Not.Null);
                Assert.That(root.Find(ArtAlphaEnvironmentVisualFactory.CraneName), Is.Not.Null);
                Assert.That(
                    root.GetComponentsInChildren<Transform>(true)
                        .Any(item => item.name == ArtAlphaEnvironmentVisualFactory.FreightDisplayName),
                    Is.True);
                Assert.That(root.Find(ArtAlphaEnvironmentVisualFactory.Container40Name), Is.Not.Null);
                Assert.That(root.Find(ArtAlphaEnvironmentVisualFactory.Container20Name), Is.Not.Null);
                Assert.That(root.Find(ArtAlphaEnvironmentVisualFactory.SubstationName), Is.Not.Null);
                Assert.That(root.Find("FactoryFloorFinish"), Is.Not.Null);
                Assert.That(root.Find("FactoryFloorFinish").childCount, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void CuratedEnvironmentContainsOnlyStaticVisualComponents()
        {
            var parent = new GameObject("ArtAlphaEnvironmentContractRoot");
            try
            {
                ArtAlphaEnvironmentVisualFactory.BuildDefaultVisuals(parent.transform);
                var root = parent.transform.Find(ArtAlphaEnvironmentVisualFactory.RootName);
                Assert.That(root, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<BoxCollider>(true), Has.Length.EqualTo(3));
                Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Animator>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Light>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<AudioSource>(true), Is.Empty);

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Assert.That(renderer.sharedMaterials, Is.Not.Empty, renderer.name);
                    Assert.That(renderer.sharedMaterials.All(material => material != null), Is.True);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void SideBayPropsUseOnlySimpleCollisionAndStayInsideFactoryWalls()
        {
            var parent = new GameObject("ArtAlphaSideBayBoundsRoot");
            try
            {
                ArtAlphaEnvironmentVisualFactory.BuildDefaultVisuals(parent.transform);
                var root = parent.transform.Find(ArtAlphaEnvironmentVisualFactory.RootName);
                var anchors = new[]
                {
                    root.Find(ArtAlphaEnvironmentVisualFactory.Container40Name),
                    root.Find(ArtAlphaEnvironmentVisualFactory.Container20Name),
                    root.Find(ArtAlphaEnvironmentVisualFactory.SubstationName)
                };

                Assert.That(anchors.All(anchor => anchor != null), Is.True);
                foreach (var anchor in anchors)
                {
                    var collider = anchor.GetComponent<BoxCollider>();
                    Assert.That(collider, Is.Not.Null, anchor.name);
                    var bounds = collider.bounds;
                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-27.7f), anchor.name);
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(27.7f), anchor.name);
                    Assert.That(bounds.min.z, Is.GreaterThanOrEqualTo(-20.8f), anchor.name);
                    Assert.That(bounds.max.z, Is.LessThanOrEqualTo(20.8f), anchor.name);
                    Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.05f), anchor.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void CraneFitsTheRaisedFactoryEnvelope()
        {
            var parent = new GameObject("ArtAlphaCraneBoundsRoot");
            try
            {
                Assert.That(
                    ArtAlphaEnvironmentVisualFactory.TryCreateCrane(
                        parent.transform,
                        AssetDatabase.LoadAssetAtPath<Material>(
                            ArtAlphaEnvironmentVisualFactory.CraneMaterialPath),
                        out var crane),
                    Is.True);

                var renderers = crane.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers, Is.Not.Empty);
                var bounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++)
                    bounds.Encapsulate(renderers[index].bounds);

                Assert.That(bounds.size.x, Is.EqualTo(43f).Within(0.05f));
                Assert.That(bounds.min.y, Is.EqualTo(0.02f).Within(0.05f));
                Assert.That(bounds.max.y, Is.LessThan(10f));
                Assert.That(Mathf.Abs(bounds.center.z - 12.5f), Is.LessThan(0.05f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
