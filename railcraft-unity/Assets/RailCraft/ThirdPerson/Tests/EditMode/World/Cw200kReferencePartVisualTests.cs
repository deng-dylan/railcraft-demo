using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class Cw200kReferencePartVisualTests
    {
        [Test]
        public void ExplicitTractionRodGroupCanBeExtractedAsAVisualOnlyPart()
        {
            var parent = new GameObject("CW200KReferencePartTestRoot");
            try
            {
                Assert.That(
                    Cw200kReferenceVisualFactory.TryCreatePartVisual(
                        parent.transform,
                        "ReferenceTractionRod",
                        PartId.TractionRod,
                        RequireMaterial("WB_Electrical.mat"),
                        out var root),
                    Is.True);

                Assert.That(root, Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(3));
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                var bounds = BoundsOf(root);
                Assert.That(Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z),
                    Is.EqualTo(1.05f).Within(0.02f));
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.02f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void AmbiguousPartDoesNotClaimAReferenceExtraction()
        {
            var parent = new GameObject("CW200KAmbiguousPartTestRoot");
            try
            {
                Assert.That(
                    Cw200kReferenceVisualFactory.TryCreatePartVisual(
                        parent.transform,
                        "AmbiguousSensorBracket",
                        PartId.SensorBracket,
                        RequireMaterial("WB_Station.mat"),
                        out var root),
                    Is.False);
                Assert.That(root, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
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
