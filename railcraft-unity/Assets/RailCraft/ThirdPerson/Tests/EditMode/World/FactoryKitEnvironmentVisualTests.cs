using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class FactoryKitEnvironmentVisualTests
    {
        private const int ExpectedDecorationCount = 17;
        private const float GroundWorkZoneBackEdge = 18.25f;
        private const float ElevatedDecorationHeight = 4f;

        private const StaticEditorFlags RequiredStaticFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        private static readonly string[] ExpectedDecorationNames =
        {
            "Crane_BackWall",
            "CraneLift_BackWall",
            "Machine_BackWest",
            "Machine_BackEast",
            "StructureWall_BackWest",
            "StructureWall_BackEast",
            "Catwalk_BackLeft",
            "Catwalk_BackRight",
            "CatwalkStairs_BackLeft",
            "CatwalkStairs_BackRight",
            "PipeRun_BackWall",
            "PipeBend_BackWest",
            "PipeBend_BackEast",
            "Box_BackWest",
            "Box_BackEast",
            "Warning_BackWest",
            "Warning_BackEast"
        };

        [Test]
        public void DefaultDecorationsCreateTheExpectedRootAndInstances()
        {
            var parent = new GameObject("FactoryKitEnvironmentTestRoot");
            try
            {
                Assert.That(FactoryKitEnvironmentVisualFactory.IsKitAvailable, Is.True,
                    "Factory Kit assets must be imported before the whitebox environment is built.");

                var created = FactoryKitEnvironmentVisualFactory.BuildDefaultDecorations(
                    parent.transform,
                    RequireMaterial("WB_Steel.mat"),
                    RequireMaterial("WB_Warning.mat"));

                Assert.That(created, Is.EqualTo(ExpectedDecorationCount));
                Assert.That(parent.transform.childCount, Is.EqualTo(1));

                var root = parent.transform.Find(
                    FactoryKitEnvironmentVisualFactory.DecorationRootName);
                Assert.That(root, Is.Not.Null);
                Assert.That(root.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(Quaternion.Angle(root.localRotation, Quaternion.identity),
                    Is.LessThan(0.001f));
                Assert.That(root.localScale, Is.EqualTo(Vector3.one));

                var decorations = DirectChildren(root);
                Assert.That(decorations, Has.Length.EqualTo(ExpectedDecorationCount));
                Assert.That(
                    decorations.Select(item => item.name),
                    Is.EquivalentTo(ExpectedDecorationNames));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void DefaultDecorationsAreStaticVisualsWithProjectMaterials()
        {
            var parent = new GameObject("FactoryKitVisualContractTestRoot");
            var steelMaterial = RequireMaterial("WB_Steel.mat");
            var warningMaterial = RequireMaterial("WB_Warning.mat");
            try
            {
                var root = BuildDefaultDecorations(
                    parent.transform,
                    steelMaterial,
                    warningMaterial);

                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Animator>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Animation>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Camera>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<Light>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<AudioSource>(true), Is.Empty);

                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                {
                    var flags = GameObjectUtility.GetStaticEditorFlags(item.gameObject);
                    Assert.That(flags & RequiredStaticFlags, Is.EqualTo(RequiredStaticFlags),
                        item.name);
                }

                foreach (var decoration in DirectChildren(root))
                {
                    var renderers = decoration.GetComponentsInChildren<Renderer>(true);
                    var expectedMaterial = decoration.name.StartsWith(
                        "Warning_",
                        StringComparison.Ordinal)
                        ? warningMaterial
                        : steelMaterial;

                    Assert.That(renderers, Is.Not.Empty, decoration.name);
                    foreach (var renderer in renderers)
                    {
                        Assert.That(renderer.enabled, Is.True, renderer.name);
                        Assert.That(renderer.receiveShadows, Is.True, renderer.name);
                        Assert.That(renderer.sharedMaterials, Is.Not.Empty, renderer.name);
                        Assert.That(
                            renderer.sharedMaterials.All(material => material == expectedMaterial),
                            Is.True,
                            renderer.name);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void DecorationAnchorsStayBehindOrAboveTheGroundWorkZone()
        {
            var parent = new GameObject("FactoryKitWorkZoneTestRoot");
            try
            {
                var root = BuildDefaultDecorations(
                    parent.transform,
                    RequireMaterial("WB_Steel.mat"),
                    RequireMaterial("WB_Warning.mat"));

                foreach (var decoration in DirectChildren(root))
                {
                    var isBehindWorkZone =
                        decoration.localPosition.z > GroundWorkZoneBackEdge;
                    var isElevated =
                        decoration.localPosition.y >= ElevatedDecorationHeight;

                    Assert.That(
                        isBehindWorkZone || isElevated,
                        Is.True,
                        $"{decoration.name} is anchored inside the ground-level work zone.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        private static Transform BuildDefaultDecorations(
            Transform parent,
            Material steelMaterial,
            Material warningMaterial)
        {
            var created = FactoryKitEnvironmentVisualFactory.BuildDefaultDecorations(
                parent,
                steelMaterial,
                warningMaterial);
            Assert.That(created, Is.EqualTo(ExpectedDecorationCount));

            var root = parent.Find(FactoryKitEnvironmentVisualFactory.DecorationRootName);
            Assert.That(root, Is.Not.Null);
            return root;
        }

        private static Transform[] DirectChildren(Transform root)
        {
            return Enumerable.Range(0, root.childCount)
                .Select(root.GetChild)
                .ToArray();
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
