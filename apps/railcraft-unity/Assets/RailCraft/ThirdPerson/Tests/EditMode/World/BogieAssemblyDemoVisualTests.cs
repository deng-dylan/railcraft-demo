using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Editor;
using RailCraft.ThirdPerson.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class BogieAssemblyDemoVisualTests
    {
        private static readonly PartId[] DemonstrationParts =
        {
            PartId.Axle,
            PartId.Wheel,
            PartId.Bearing,
            PartId.BrakeDevice,
            PartId.PrimaryElasticElement,
            PartId.PrimaryDamper,
            PartId.SecondaryElasticElement,
            PartId.SecondaryDamper,
            PartId.CentralTractionDevice,
            PartId.Carbody
        };

        private static readonly PartId[] PrimitiveFallbackParts =
        {
            PartId.TractionRod,
            PartId.SensorBracket,
            PartId.PrimaryPositioningElement,
            PartId.HeightControlElement
        };

        private static readonly ModuleId[] DemonstrationModules =
        {
            ModuleId.WheelsetAxlebox,
            ModuleId.Frame,
            ModuleId.PrimarySuspension,
            ModuleId.BogieStructure,
            ModuleId.SecondarySuspension
        };

        [Test]
        public void ImportedAssetKeepsSemanticHierarchyAndBudget()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                BogieAssemblyDemoVisualFactory.ModelAssetPath);

            Assert.That(model, Is.Not.Null);
            var names = model.GetComponentsInChildren<Transform>(true)
                .Select(item => item.name)
                .ToArray();
            Assert.That(names, Does.Contain(BogieAssemblyDemoVisualFactory.ModelRootName));
            Assert.That(names, Does.Contain("Demo_WheelsetAxlebox"));
            Assert.That(names, Does.Contain("Demo_Frame"));
            Assert.That(names, Does.Contain("Demo_PrimarySuspension"));
            Assert.That(names, Does.Contain("Demo_SecondarySuspension"));
            Assert.That(names, Does.Contain("Demo_Drive"));
            Assert.That(names, Does.Contain("Demo_CentralTraction"));
            Assert.That(names, Does.Contain("BogieCenter"));
            Assert.That(names, Does.Contain("RailContactPlane"));
            Assert.That(names, Does.Contain("VehicleMount"));
            Assert.That(names, Does.Contain("Axle_01"));
            Assert.That(names, Does.Contain("Axle_02"));
            Assert.That(names, Does.Not.Contain("Screw"));
            Assert.That(names, Does.Not.Contain("Rail_L"));
            Assert.That(names, Does.Not.Contain("Rail_R"));
            Assert.That(names, Does.Not.Contain("Camera"));
            Assert.That(names, Does.Not.Contain("Light"));

            var meshFilters = model.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(meshFilters, Has.Length.EqualTo(35));
            Assert.That(
                meshFilters.Sum(item => item.sharedMesh.triangles.Length / 3),
                Is.EqualTo(26860));
            Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(model.GetComponentsInChildren<Animator>(true), Is.Empty);
        }

        [Test]
        public void SemanticPartExtractionKeepsTheInspectedDefaultModel()
        {
            Assert.That(
                ModelCandidateRegistry.GetBogieModelAssetPath(),
                Is.EqualTo(BogieAssemblyDemoVisualFactory.ModelAssetPath));
        }

        [Test]
        public void ImportedCarbodyAssetHasCoachScaleAndNoRuntimeComponents()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                BogieAssemblyDemoVisualFactory.CarbodyModelAssetPath);

            Assert.That(model, Is.Not.Null,
                "The extracted intermediate coach must be imported before the assembly scene is built.");

            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                .ToArray();
            Assert.That(renderers, Is.Not.Empty);

            var bounds = CombinedRendererBounds(renderers);
            var dimensions = new[] { bounds.size.x, bounds.size.y, bounds.size.z };
            var length = dimensions.Max();
            var width = dimensions.OrderBy(value => value).ElementAt(1);
            var height = dimensions.Min();

            // The extraction manifest is 25.678848 m × 3.46964 m ×
            // 3.067708 m. Keep a small importer tolerance while still
            // catching accidental unit or axis changes.
            Assert.That(length, Is.InRange(25.45f, 25.95f));
            Assert.That(width, Is.InRange(3.25f, 3.70f));
            Assert.That(height, Is.InRange(2.90f, 3.25f));
            Assert.That(model.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(model.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(model.GetComponentsInChildren<Animator>(true), Is.Empty);
            Assert.That(model.GetComponentsInChildren<Camera>(true), Is.Empty);
            Assert.That(model.GetComponentsInChildren<Light>(true), Is.Empty);
        }

        [Test]
        public void CarbodyPartVisualUsesTheExtractedCoachAtPreviewLength()
        {
            var parent = new GameObject("CarbodyDemoPartTestRoot");
            var material = RequireMaterial();
            try
            {
                Assert.That(
                    BogieAssemblyDemoVisualFactory.UsesDemonstrationGeometry(PartId.Carbody),
                    Is.True);
                Assert.That(
                    BogieAssemblyDemoVisualFactory.TryCreatePartVisual(
                        parent.transform,
                        "Visual_Carbody",
                        PartId.Carbody,
                        material,
                        out var visual),
                    Is.True);

                AssertRootContract(visual);
                var bounds = CombinedRendererBounds(visual);
                Assert.That(bounds.size.z, Is.InRange(7.35f, 7.65f),
                    $"Preview carbody dimensions were {bounds.size}");
                Assert.That(bounds.size.x, Is.InRange(0.90f, 1.15f));
                Assert.That(bounds.size.y, Is.InRange(0.78f, 1.08f));
                Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(visual.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(visual.GetComponentsInChildren<Animator>(true), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void FullCarbodyVisualKeepsTheOneToOneCoachDimensions()
        {
            var parent = new GameObject("CarbodyDemoFullScaleTestRoot");
            var material = RequireMaterial();
            try
            {
                Assert.That(
                    BogieAssemblyDemoVisualFactory.TryCreateCarbodyVisual(
                        parent.transform,
                        "FullScaleCarbody",
                        material,
                        displayLength: 0f,
                        out var visual),
                    Is.True);

                var bounds = CombinedRendererBounds(visual);
                Assert.That(bounds.size.z, Is.InRange(25.45f, 25.95f),
                    $"Full carbody dimensions were {bounds.size}");
                Assert.That(bounds.size.x, Is.InRange(3.25f, 3.70f));
                Assert.That(bounds.size.y, Is.InRange(2.90f, 3.25f));
                Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void RepairedWheelsetsAreSeparatedAndMeetTheRailReference()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                BogieAssemblyDemoVisualFactory.ModelAssetPath);
            var instance = UnityEngine.Object.Instantiate(model);
            try
            {
                var front = RequireDescendant(instance.transform, "Wheels_F")
                    .GetComponent<Renderer>();
                var rear = RequireDescendant(instance.transform, "Wheels_R")
                    .GetComponent<Renderer>();
                var railContact = RequireDescendant(instance.transform, "RailContactPlane");

                Assert.That(front, Is.Not.Null);
                Assert.That(rear, Is.Not.Null);
                Assert.That(front.bounds.Intersects(rear.bounds), Is.False);
                Assert.That(
                    Vector3.Distance(front.bounds.center, rear.bounds.center),
                    Is.EqualTo(2.5f).Within(0.01f));

                var wheelBottom = Mathf.Min(front.bounds.min.y, rear.bounds.min.y);
                Assert.That(wheelBottom - railContact.position.y, Is.InRange(0f, 0.03f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void MappedPartsProduceFittedVisualOnlyGeometry()
        {
            var parent = new GameObject("BogieDemoPartTestRoot");
            var material = RequireMaterial();
            try
            {
                foreach (var partId in DemonstrationParts)
                {
                    Assert.That(
                        BogieAssemblyDemoVisualFactory.UsesDemonstrationGeometry(partId),
                        Is.True,
                        partId.ToString());
                    Assert.That(
                        BogieAssemblyDemoVisualFactory.TryCreatePartVisual(
                            parent.transform,
                            "Visual_" + partId,
                            partId,
                            material,
                            out var visual),
                        Is.True,
                        partId.ToString());

                    AssertRootContract(visual);
                    Assert.That(visual.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
                    Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
                    var dimensions = CombinedRendererBounds(visual);
                    if (partId == PartId.Carbody)
                    {
                        Assert.That(dimensions.size.z, Is.InRange(7.35f, 7.65f));
                        Assert.That(dimensions.size.x, Is.InRange(0.90f, 1.15f));
                        Assert.That(dimensions.size.y, Is.InRange(0.78f, 1.08f));
                    }
                    else
                    {
                        var maxDimension = LargestRendererBoundsDimension(visual);
                        Assert.That(maxDimension, Is.LessThanOrEqualTo(1.08f));
                    }
                    UnityEngine.Object.DestroyImmediate(visual);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void UnrepresentedPartsKeepTheExplicitPrimitiveFallback()
        {
            foreach (var partId in PrimitiveFallbackParts)
                Assert.That(
                    BogieAssemblyDemoVisualFactory.UsesDemonstrationGeometry(partId),
                    Is.False,
                    partId.ToString());
        }

        [Test]
        public void MappedModulesShareAssemblyCoordinatesWithoutDuplicateMeshes()
        {
            var parent = new GameObject("BogieDemoModuleTestRoot");
            var material = RequireMaterial();
            try
            {
                foreach (var moduleId in DemonstrationModules)
                {
                    Assert.That(
                        BogieAssemblyDemoVisualFactory.UsesDemonstrationGeometry(moduleId),
                        Is.True,
                        moduleId.ToString());
                }

                var installedModules = new[]
                {
                    ModuleId.WheelsetAxlebox,
                    ModuleId.Frame,
                    ModuleId.PrimarySuspension
                };
                foreach (var moduleId in installedModules)
                {
                    Assert.That(
                        BogieAssemblyDemoVisualFactory.TryCreateModuleVisual(
                            parent.transform,
                            "Installed_" + moduleId,
                            moduleId,
                            material,
                            preserveAssemblyCoordinates: true,
                            out var visual),
                        Is.True,
                        moduleId.ToString());
                    AssertRootContract(visual);
                }

                var meshNames = parent.GetComponentsInChildren<MeshFilter>(true)
                    .Select(item => item.name)
                    .ToArray();
                Assert.That(meshNames, Has.Length.EqualTo(23));
                Assert.That(meshNames.Distinct().Count(), Is.EqualTo(meshNames.Length));
                Assert.That(meshNames, Does.Not.Contain("Traction"));
                Assert.That(parent.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void CompletedBogieAddsTheFixedDriveAndKeepsCentralTractionSeparate()
        {
            var parent = new GameObject("BogieDemoCompletionTestRoot");
            var material = RequireMaterial();
            try
            {
                Assert.That(
                    BogieAssemblyDemoVisualFactory.TryCreateModuleVisual(
                        parent.transform,
                        "CompletedBogie",
                        ModuleId.BogieStructure,
                        material,
                        preserveAssemblyCoordinates: true,
                        out var visual),
                    Is.True);

                var names = visual.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name)
                    .ToArray();
                Assert.That(names, Does.Contain("Motor_L"));
                Assert.That(names, Does.Contain("Motor_R"));
                Assert.That(names, Does.Contain("Gearbox_F"));
                Assert.That(names, Does.Contain("Gearbox_R"));
                Assert.That(names, Does.Not.Contain("Traction"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void CompletedLandingBogieUsesEveryImportedMeshExactlyOnce()
        {
            var parent = new GameObject("BogieDemoLandingTestRoot");
            var material = RequireMaterial();
            try
            {
                Assert.That(
                    BogieAssemblyDemoVisualFactory.TryCreateCompletedBogieVisual(
                        parent.transform,
                        "CompletedLandingBogie",
                        material,
                        out var visual),
                    Is.True);

                AssertRootContract(visual);
                var meshNames = visual.GetComponentsInChildren<MeshFilter>(true)
                    .Select(item => item.name)
                    .ToArray();
                Assert.That(meshNames, Has.Length.EqualTo(35));
                Assert.That(meshNames.Distinct().Count(), Is.EqualTo(meshNames.Length));
                Assert.That(meshNames, Does.Contain("Traction"));
                Assert.That(meshNames, Does.Contain("AirSpring_L"));
                Assert.That(meshNames, Does.Contain("Motor_L"));
                Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void GeneratedWhiteboxSceneUsesTheDemoAndKeepsTheFinalShowcaseEntry()
        {
            const string scenePath =
                "Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity";
            var previousPath = SceneManager.GetActiveScene().path;
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var transforms = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .ToArray();
                var names = transforms.Select(item => item.name).ToArray();

                Assert.That(names, Does.Contain("AssemblyDemonstrationNotice"));
                Assert.That(names, Does.Contain("DemonstrationModelContent"));
                Assert.That(names, Does.Contain("Installed_FixedDrivePackage"));
                Assert.That(names.Count(item => item == "LandingBogie_Front"), Is.EqualTo(1));
                Assert.That(names.Count(item => item == "LandingBogie_Rear"), Is.EqualTo(1));
                Assert.That(names, Does.Contain("Axle_F"));
                Assert.That(names, Does.Contain("Wheels_F"));
                Assert.That(names, Does.Contain("BrakeDiscs_F"));
                Assert.That(names, Does.Not.Contain("Screw"));
                Assert.That(names, Does.Not.Contain("Rail_L"));
                Assert.That(names, Does.Not.Contain("Rail_R"));
                Assert.That(
                    scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<FinalShowcaseEntryController>(true)),
                    Is.Not.Empty);

                var slots = transforms
                    .Where(item => item.parent != null
                        && item.parent.name == "CompositeStation_BogieStructure"
                        && item.name.StartsWith("ModuleSnapSlot_", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(slots, Has.Length.EqualTo(3));
                Assert.That(
                    slots.All(item => (item.localPosition - new Vector3(0f, 0.69f, 0f)).sqrMagnitude
                        < 0.000001f),
                    Is.True);

                var droppedVehicle = transforms.Single(item => item.name == "DroppedVehicle");
                var frontBogie = transforms.Single(item => item.name == "LandingBogie_Front");
                var rearBogie = transforms.Single(item => item.name == "LandingBogie_Rear");
                var carbody = droppedVehicle.transform.Find("FuxingCarbodyReference");
                Assert.That(carbody, Is.Not.Null);
                var carbodyBounds = CombinedRendererBounds(carbody.gameObject);
                var frontBounds = CombinedRendererBounds(frontBogie.gameObject);
                var rearBounds = CombinedRendererBounds(rearBogie.gameObject);
                Assert.That(droppedVehicle.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(carbodyBounds.size.z, Is.InRange(25.45f, 25.95f));
                Assert.That(carbodyBounds.size.x, Is.InRange(3.25f, 3.70f));
                Assert.That(carbodyBounds.size.y, Is.InRange(2.90f, 3.25f));
                Assert.That(frontBounds.Intersects(rearBounds), Is.False);
                Assert.That(frontBogie.localPosition.z, Is.EqualTo(-8f).Within(0.01f));
                Assert.That(rearBogie.localPosition.z, Is.EqualTo(8f).Within(0.01f));
                Assert.That(rearBogie.localPosition.z - frontBogie.localPosition.z,
                    Is.EqualTo(16f).Within(0.02f));

                var bogieTop = Mathf.Max(frontBounds.max.y, rearBounds.max.y);
                var bodyToBogieGap = carbodyBounds.min.y - bogieTop;
                Assert.That(bodyToBogieGap, Is.InRange(0.005f, 0.20f));
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousPath))
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void LandingLaneAndTriggerStayClearOfAdjacentAssemblyStations()
        {
            const string scenePath =
                "Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity";
            var previousPath = SceneManager.GetActiveScene().path;
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var transforms = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .ToArray();
                var landing = transforms.Single(item => item.name == "LandingAssemblyStation");
                var landingCollider = landing.GetComponent<BoxCollider>();
                Assert.That(landingCollider, Is.Not.Null);
                Assert.That(landingCollider.size.z, Is.LessThan(10f),
                    "The long display lane must not become a long-range interaction trigger.");

                var laneRenderers = landing.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.name == "AssemblyPlatform"
                        || renderer.name.StartsWith("Rail_", StringComparison.Ordinal))
                    .ToArray();
                var laneBounds = CombinedRendererBounds(laneRenderers);

                foreach (var stationName in new[]
                {
                    "ModuleStation_module_primary_suspension",
                    "ModuleStation_module_secondary_suspension",
                    "Commissioning_Test",
                    "Commissioning_Retune",
                    "Commissioning_Inspect"
                })
                {
                    var station = transforms.Single(item => item.name == stationName);
                    var stationCollider = station.GetComponent<BoxCollider>();
                    Assert.That(stationCollider, Is.Not.Null, stationName);
                    Assert.That(
                        landingCollider.bounds.Intersects(stationCollider.bounds),
                        Is.False,
                        $"Landing trigger overlaps {stationName}.");
                    var stationBounds = CombinedRendererBounds(station.gameObject);
                    Assert.That(
                        laneBounds.Intersects(stationBounds),
                        Is.False,
                        $"Landing display lane overlaps {stationName} geometry.");
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousPath))
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static Material RequireMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/RailCraft/ThirdPerson/Art/Materials/WB_Steel.mat");
            Assert.That(material, Is.Not.Null);
            return material;
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var match = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => string.Equals(item.name, name, StringComparison.Ordinal));
            Assert.That(match, Is.Not.Null, name);
            return match;
        }

        private static void AssertRootContract(GameObject root)
        {
            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.localPosition.sqrMagnitude, Is.LessThan(0.000001f));
            Assert.That(Quaternion.Angle(root.transform.localRotation, Quaternion.identity), Is.LessThan(0.001f));
            Assert.That((root.transform.localScale - Vector3.one).sqrMagnitude, Is.LessThan(0.000001f));
        }

        private static float LargestRendererBoundsDimension(GameObject root)
        {
            var bounds = CombinedRendererBounds(root);
            return Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        }

        private static Bounds CombinedRendererBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            return CombinedRendererBounds(renderers);
        }

        private static Bounds CombinedRendererBounds(IEnumerable<Renderer> renderers)
        {
            var rendererArray = renderers.ToArray();
            Assert.That(rendererArray, Is.Not.Empty);
            var bounds = rendererArray[0].bounds;
            foreach (var renderer in rendererArray.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
