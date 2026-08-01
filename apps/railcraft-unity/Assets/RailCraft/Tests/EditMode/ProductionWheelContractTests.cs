using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RailCraft.Assets;
using RailCraft.Editor;
using RailCraft.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RailCraft.Tests.EditMode
{
    public sealed class ProductionWheelContractTests
    {
        private const string RuntimePrefabPath =
            "Assets/RailCraft/Art/Prefabs/Modules/WheelRuntime.prefab";
        private const string RuntimeModelPath =
            "Assets/RailCraft/Art/Models/Production/Bogie/Wheel/wheel.fbx";
        private const string FactoryScenePath = "Assets/RailCraft/Scenes/Factory.unity";

        private static readonly Regex MeshNamePattern = new Regex(
            "^swm400e1_wheel_[a-z0-9]+_lod[0-2]$", RegexOptions.CultureInvariant);
        private static readonly Regex MaterialNamePattern = new Regex(
            "^mat_[a-z0-9]+(?:_[a-z0-9]+)+$", RegexOptions.CultureInvariant);

        [Test]
        public void ProductionWheelUsesStableRuntimeContract()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuntimePrefabPath);
            if (prefab == null)
            {
                var repositoryRoot = Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", "..", ".."));
                var manifestPath = Path.Combine(repositoryRoot, "deliveries", "models",
                    "swm-400e1-wheel-v1", "README.md");
                Assert.That(File.Exists(manifestPath), Is.True,
                    "The pending production-wheel gate must have a tracked delivery manifest.");
                var manifest = File.ReadAllText(manifestPath);
                var gateRow = manifest.Replace("\r", string.Empty).Split('\n')
                    .SingleOrDefault(line => line.Contains("STEP AP242 / Parasolid x_t"));
                Assert.That(gateRow, Is.Not.Null);
                Assert.That(gateRow, Does.Contain("未提供").And.Contain("阻断"));
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeModelPath), Is.Null);
                Assert.Pass(
                    "Production wheel is honestly blocked on the documented neutral-format delivery.");
            }

            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(RuntimeModelPath), Is.Not.Null);
            var contract = prefab.GetComponent<ModelContract>();
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.assetKey, Is.EqualTo("mesh.wheel.production"));
            Assert.That(contract.SourceVersion, Is.Not.Empty);
            Assert.That(contract.AuthoredAtMeterScale, Is.True);
            Assert.That(contract.LocalAxleDirection, Is.EqualTo(Vector3.right));
            Assert.That(contract.LocalUpDirection, Is.EqualTo(Vector3.up));
            Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(Quaternion.Angle(prefab.transform.localRotation, Quaternion.identity),
                Is.LessThan(0.01f));
            Assert.That(prefab.GetComponent<DraggableModule>(), Is.Null,
                "The production wheel is a visual child of a wheelset module, not a flow step.");

            var lodGroup = prefab.GetComponentInChildren<LODGroup>(true);
            Assert.That(lodGroup, Is.Not.Null);
            var lods = lodGroup.GetLODs();
            Assert.That(lods, Has.Length.EqualTo(3));
            var visualMeshes = new HashSet<Mesh>();
            for (var lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                var lodRenderers = lods[lodIndex].renderers;
                Assert.That(lodRenderers, Is.Not.Empty, "LOD" + lodIndex);
                foreach (var renderer in lodRenderers)
                {
                    Assert.That(renderer, Is.Not.Null, "LOD" + lodIndex);
                    var mesh = GetSharedMesh(renderer);
                    Assert.That(mesh, Is.Not.Null, renderer.name);
                    Assert.That(MeshNamePattern.IsMatch(mesh.name), Is.True, mesh.name);
                    Assert.That(mesh.name.EndsWith("_lod" + lodIndex,
                        StringComparison.Ordinal), Is.True, mesh.name);
                    visualMeshes.Add(mesh);

                    var materials = renderer.sharedMaterials;
                    Assert.That(materials, Is.Not.Empty, renderer.name);
                    Assert.That(materials.All(material => material != null
                        && MaterialNamePattern.IsMatch(material.name)), Is.True, renderer.name);
                    foreach (var material in materials)
                    {
                        Assert.That(ProductionAssetBudgetPostprocessor
                            .GetPropertyBlockEmissionIssue(material), Is.Null,
                            $"{renderer.name}/{material?.name}");
                    }
                }
            }

            var colliders = prefab.GetComponentsInChildren<Collider>(true);
            Assert.That(colliders, Is.Not.Empty);
            var lod0IndexCount = lods[0].renderers
                .Select(GetSharedMesh)
                .Where(mesh => mesh != null)
                .Select(IndexCount)
                .DefaultIfEmpty(0UL)
                .Max();
            foreach (var meshCollider in colliders.OfType<MeshCollider>())
            {
                Assert.That(meshCollider.convex, Is.True, meshCollider.name);
                Assert.That(meshCollider.sharedMesh, Is.Not.Null, meshCollider.name);
                Assert.That(visualMeshes.Contains(meshCollider.sharedMesh), Is.False,
                    "Collision geometry must be separate from visual LOD meshes.");
                Assert.That(IndexCount(meshCollider.sharedMesh), Is.LessThan(lod0IndexCount),
                    "A collision mesh must be simpler than the LOD0 visual mesh.");
            }

            ValidateWheelsetModule(
                "Assets/RailCraft/Art/Prefabs/Modules/module_wheelset_axlebox_a.prefab",
                "module.wheelset_axlebox_a", "wheelset_axlebox_a");
            ValidateWheelsetModule(
                "Assets/RailCraft/Art/Prefabs/Modules/module_wheelset_axlebox_b.prefab",
                "module.wheelset_axlebox_b", "wheelset_axlebox_b");
            ValidateStableFlowContracts();
        }

        private static void ValidateWheelsetModule(string path, string assetKey, string stepId)
        {
            var modulePrefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Assert.That(modulePrefab, Is.Not.Null, path);
                Assert.That(modulePrefab.transform.localPosition, Is.EqualTo(Vector3.zero), path);
                Assert.That(modulePrefab.transform.localScale, Is.EqualTo(Vector3.one), path);
                Assert.That(Quaternion.Angle(modulePrefab.transform.localRotation, Quaternion.identity),
                    Is.LessThan(0.01f), path);

                var contract = modulePrefab.GetComponent<ModelContract>();
                Assert.That(contract, Is.Not.Null, path);
                Assert.That(contract.AssetKey, Is.EqualTo(assetKey), path);
                var draggable = modulePrefab.GetComponent<DraggableModule>();
                Assert.That(draggable, Is.Not.Null, path);
                Assert.That(draggable.StepId, Is.EqualTo(stepId), path);
                Assert.That(draggable.InteractionCollider,
                    Is.SameAs(modulePrefab.GetComponent<Collider>()), path);
                Assert.That(draggable.InteractionCollider.transform,
                    Is.SameAs(modulePrefab.transform), path);
                Assert.That(draggable.VisualRoot, Is.Not.SameAs(modulePrefab.transform), path);
                Assert.That(draggable.VisualRoot.name, Is.EqualTo("VisualRoot"), path);
                Assert.That(draggable.VisualRoot.IsChildOf(modulePrefab.transform), Is.True, path);

                var wheelsInVisualRoot = draggable.VisualRoot
                    .GetComponentsInChildren<ModelContract>(true)
                    .Where(item => item.AssetKey == "mesh.wheel.production")
                    .ToArray();
                Assert.That(wheelsInVisualRoot, Has.Length.EqualTo(2), path);
                Assert.That(modulePrefab.GetComponentsInChildren<ModelContract>(true)
                    .Count(item => item.AssetKey == "mesh.wheel.production"), Is.EqualTo(2), path);
                foreach (var wheel in wheelsInVisualRoot)
                {
                    Assert.That(PrefabUtility.IsAnyPrefabInstanceRoot(wheel.gameObject), Is.True,
                        wheel.name);
                    Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(wheel.gameObject),
                        Is.EqualTo(RuntimePrefabPath), wheel.name);
                    Assert.That(wheel.transform.IsChildOf(draggable.VisualRoot), Is.True, wheel.name);
                }
            }
            finally
            {
                if (modulePrefab != null)
                    PrefabUtility.UnloadPrefabContents(modulePrefab);
            }
        }

        private static void ValidateStableFlowContracts()
        {
            var bundle = Fixtures.ContentFixture.LoadProduction();
            var expected = new[]
            {
                new { StepId = "wheelset_axlebox_a", AssetKey = "module.wheelset_axlebox_a",
                    TargetId = "target.wheelset_axlebox_a" },
                new { StepId = "wheelset_axlebox_b", AssetKey = "module.wheelset_axlebox_b",
                    TargetId = "target.wheelset_axlebox_b" }
            };

            var scene = EditorSceneManager.OpenScene(FactoryScenePath, OpenSceneMode.Additive);
            try
            {
                var targets = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<DropTarget>(true))
                    .ToArray();
                foreach (var expectedContract in expected)
                {
                    var step = bundle.Flow.steps.Single(item => item.id == expectedContract.StepId);
                    Assert.That(step.assetKey, Is.EqualTo(expectedContract.AssetKey),
                        expectedContract.StepId);
                    Assert.That(step.dropTargetId, Is.EqualTo(expectedContract.TargetId),
                        expectedContract.StepId);
                    var target = targets.Single(item => item.TargetId == expectedContract.TargetId);
                    Assert.That(target.AcceptedStepId, Is.EqualTo(expectedContract.StepId),
                        expectedContract.TargetId);
                    Assert.That(target.SnapAnchor, Is.Not.Null, expectedContract.TargetId);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Mesh GetSharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            return renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static ulong IndexCount(Mesh mesh)
        {
            ulong count = 0;
            for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                count += mesh.GetIndexCount(subMeshIndex);
            return count;
        }
    }
}