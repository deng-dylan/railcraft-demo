using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class CadModelPreviewGalleryTests
    {
        [TestCase(CadModelPreviewGalleryBuilder.Y25ModelPath, 500000)]
        [TestCase(CadModelPreviewGalleryBuilder.BettendorfModelPath, 100000)]
        public void ConvertedCadPreviewHasDetailedRenderableGeometry(
            string assetPath,
            int minimumTriangleCount)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.That(model, Is.Not.Null, assetPath);

            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters.Length, Is.GreaterThan(0), assetPath);
            var triangleCount = filters
                .Where(filter => filter.sharedMesh != null)
                .Sum(filter => filter.sharedMesh.triangles.Length / 3);
            Assert.That(triangleCount, Is.GreaterThanOrEqualTo(minimumTriangleCount), assetPath);

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), assetPath);

            var instance = Object.Instantiate(model);
            try
            {
                var instanceRenderers = instance.GetComponentsInChildren<Renderer>(true);
                var bounds = instanceRenderers[0].bounds;
                foreach (var renderer in instanceRenderers.Skip(1))
                    bounds.Encapsulate(renderer.bounds);
                Assert.That(bounds.size.y, Is.LessThan(bounds.size.x), assetPath);
                Assert.That(bounds.size.y, Is.LessThan(bounds.size.z), assetPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PreviewGalleryStaysOutsidePlayerBuildSettings()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                CadModelPreviewGalleryBuilder.ScenePath);
            Assert.That(scene, Is.Not.Null);
            Assert.That(
                EditorBuildSettings.scenes.Select(item => item.path),
                Has.None.EqualTo(CadModelPreviewGalleryBuilder.ScenePath));
        }
    }
}
