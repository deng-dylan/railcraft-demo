using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class FactoryKitWorkbenchVisualTests
    {
        [OneTimeSetUp]
        public void PrepareOperationTableMaterials()
        {
            OperationTableAssetPreparation.Prepare();
        }

        [Test]
        public void FactoryKitWorkbenchSourceAssetsAreAvailable()
        {
            Assert.That(FactoryKitWorkbenchVisualFactory.IsAvailable, Is.True);
        }

        [Test]
        public void QuizWorkbenchBuildsIndustrialWorkbenchWithSolidColliders()
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
                Assert.That(root.transform.Find("OperationTable"), Is.Not.Null);
                Assert.That(root.transform.Find("WorkbenchCollision"), Is.Not.Null);
                AssertWorkbenchStructure(root);
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
                Assert.That(table.transform.Find("OperationTable"), Is.Not.Null);
                Assert.That(console.transform.Find("OperationTable"), Is.Not.Null);
                Assert.That(table.transform.Find("WorkbenchCollision"), Is.Not.Null);
                Assert.That(console.transform.Find("WorkbenchCollision"), Is.Not.Null);
                AssertWorkbenchStructure(table);
                AssertWorkbenchStructure(console);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static void AssertWorkbenchStructure(GameObject root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            Assert.That(colliders, Is.Not.Empty);
            Assert.That(colliders.Any(collider => !collider.isTrigger), Is.True);
            Assert.That(root.GetComponentsInChildren<BoxCollider>(true).Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(root.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
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
            Assert.That(renderers.SelectMany(renderer => renderer.sharedMaterials)
                .Select(material => material.name).Distinct().Count(), Is.EqualTo(2));
            foreach (var material in renderers.SelectMany(renderer => renderer.sharedMaterials))
            {
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
                    Assert.That(material.GetTexture(property), Is.Not.Null, property);
            }
        }

        [TestCase(0.82f, 0.8f)]
        [TestCase(1.22f, 0.67f)]
        public void ART004_AssemblyBoundsFitExistingSlotsAndFloor(float span, float surfaceHeight)
        {
            var parent = new GameObject("OperationTableScaleTest");
            try
            {
                parent.transform.SetPositionAndRotation(new Vector3(17f, 0f, -5f), Quaternion.Euler(0f, 90f, 0f));
                Assert.That(FactoryKitWorkbenchVisualFactory.TryBuildAssemblyTable(
                    parent.transform, null, null, null, span, out var root, surfaceHeight), Is.True);
                var bounds = FactoryKitWorkbenchVisualFactory.CalculateLocalMeshBounds(
                    root.transform.Find("OperationTable").gameObject, root.transform);
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(bounds.max.y, Is.EqualTo(surfaceHeight).Within(0.001f));
                Assert.That(bounds.size.x, Is.EqualTo(5.4f * span).Within(0.001f));
                Assert.That(bounds.size.z, Is.EqualTo(3.3f * span).Within(0.001f));
                var top = root.transform.Find("WorkbenchCollision/Top").GetComponent<BoxCollider>();
                Assert.That(top.center.y + top.size.y / 2f, Is.EqualTo(surfaceHeight).Within(0.001f));
            }
            finally { Object.DestroyImmediate(parent); }
        }

        [Test]
        public void ART004_CharacterCapsuleIsBlockedFromEverySideAndTriggerIsPreserved()
        {
            var parent = new GameObject("OperationTablePhysicsTest");
            try
            {
                parent.transform.position = new Vector3(250f, 0f, 250f);
                var trigger = parent.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 1.1f, 0f);
                trigger.size = new Vector3(4.6f, 2.2f, 3.2f);
                Assert.That(FactoryKitWorkbenchVisualFactory.TryBuildQuizWorkbench(
                    parent.transform, null, null, null, out var root), Is.True);
                Physics.SyncTransforms();
                foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    var start = parent.transform.position + direction * 3f;
                    Assert.That(Physics.CapsuleCast(start + Vector3.up * 0.35f,
                        start + Vector3.up * 1.45f, 0.3f, -direction, out var hit, 3f,
                        ~0, QueryTriggerInteraction.Ignore), Is.True, direction.ToString());
                    Assert.That(hit.collider.transform.IsChildOf(root.transform), Is.True);
                    Assert.That(hit.distance, Is.GreaterThan(0.8f));
                }
                Assert.That(trigger.isTrigger && trigger.enabled, Is.True);
                Assert.That(root.GetComponentsInChildren<Collider>().All(c => !c.isTrigger), Is.True);
            }
            finally { Object.DestroyImmediate(parent); }
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
