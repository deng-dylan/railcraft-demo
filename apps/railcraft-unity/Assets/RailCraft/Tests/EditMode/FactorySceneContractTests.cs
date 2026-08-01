using System.Linq;
using NUnit.Framework;
using RailCraft.CameraSystem;
using RailCraft.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RailCraft.Tests.EditMode
{
    public sealed class FactorySceneContractTests
    {
        private const string ScenePath = "Assets/RailCraft/Scenes/Factory.unity";

        private static readonly string[] RequiredChildren =
        {
            "Environment",
            "BakedLighting",
            "AssemblyBay",
            "PartsStagingArea",
            "CarbodyLoweringBay",
            "CommissioningConsole",
            "InspectionStation",
            "ReleaseBoard",
            "BackgroundTrack",
            "CR400AFHeadDisplay",
            "ProcessAnchors",
            "DropTargets",
            "FactoryCameraRig"
        };

        [Test]
        public void FactorySceneContainsRequiredProductionHierarchy()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), Is.Not.Null);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "FactoryRoot");

            Assert.That(root, Is.Not.Null);
            foreach (var childName in RequiredChildren)
                Assert.That(root.transform.Find(childName), Is.Not.Null, childName);
        }

        [Test]
        public void FactorySceneProvidesCameraLightingAndProcessAnchors()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().Single(item => item.name == "FactoryRoot");

            Assert.That(root.GetComponentInChildren<FactoryCameraController>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<CameraShotDirector>(true), Is.Not.Null);
            Assert.That(root.GetComponentsInChildren<Light>(true)
                .Count(light => light.type == LightType.Directional), Is.EqualTo(1));
            Assert.That(root.GetComponentInChildren<ReflectionProbe>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<LightProbeGroup>(true), Is.Not.Null);
            Assert.That(root.transform.Find("ProcessAnchors").childCount, Is.EqualTo(15));
            Assert.That(root.transform.Find("DropTargets").childCount, Is.EqualTo(15));
        }

        [Test]
        public void FactoryCameraAndShotsHaveStableSerializedReferences()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().Single(item => item.name == "FactoryRoot");
            var controller = root.GetComponentInChildren<FactoryCameraController>(true);
            var director = root.GetComponentInChildren<CameraShotDirector>(true);
            var controllerData = new SerializedObject(controller);
            var directorData = new SerializedObject(director);

            var currentFocus = controllerData.FindProperty("focusTarget").objectReferenceValue;
            Assert.That(controllerData.FindProperty("controlledCamera").objectReferenceValue, Is.Not.Null);
            Assert.That(currentFocus, Is.Not.Null);
            Assert.That(controllerData.FindProperty("inputActions").objectReferenceValue, Is.Not.Null);
            Assert.That(directorData.FindProperty("cameraController").objectReferenceValue, Is.SameAs(controller));

            var shots = directorData.FindProperty("shots");
            Assert.That(shots.arraySize, Is.EqualTo(16));
            var shotIds = new string[shots.arraySize];
            Object overviewAnchor = null;
            for (var index = 0; index < shots.arraySize; index++)
            {
                var shot = shots.GetArrayElementAtIndex(index);
                shotIds[index] = shot.FindPropertyRelative("shotId").stringValue;
                var anchor = shot.FindPropertyRelative("focusAnchor").objectReferenceValue;
                Assert.That(anchor, Is.Not.Null, shotIds[index]);
                if (shotIds[index] == "overview")
                    overviewAnchor = anchor;
            }

            Assert.That(shotIds, Is.Unique);
            Assert.That(overviewAnchor, Is.Not.Null);
            Assert.That(overviewAnchor, Is.Not.SameAs(currentFocus),
                "The overview anchor must remain fixed when CurrentFocus moves.");
        }

        [Test]
        public void FactoryDropTargetsMatchEveryFlowStep()
        {
            var bundle = Fixtures.ContentFixture.LoadProduction();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = scene.GetRootGameObjects().Single(item => item.name == "FactoryRoot");
            var targets = root.transform.Find("DropTargets").GetComponentsInChildren<DropTarget>(true);

            Assert.That(targets, Has.Length.EqualTo(15));
            foreach (var step in bundle.Flow.steps)
            {
                var target = targets.SingleOrDefault(candidate => candidate.TargetId == step.dropTargetId);
                Assert.That(target, Is.Not.Null, step.dropTargetId);
                Assert.That(target.AcceptedStepId, Is.EqualTo(step.id));
                Assert.That(target.SnapAnchor, Is.Not.Null);
            }
        }

        [Test]
        public void RebuildingFactoryPreservesModelOverridesAndExistingBuildScenes()
        {
            const string prefabPath = "Assets/RailCraft/Art/Prefabs/Modules/module_frame.prefab";
            const string sentinelName = "ProductionOverrideSentinel";
            const string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            var originalScenes = EditorBuildSettings.scenes;

            AddPrefabSentinel(prefabPath, sentinelName);
            try
            {
                EditorBuildSettings.scenes = originalScenes
                    .Concat(new[] { new EditorBuildSettingsScene(sampleScenePath, true) })
                    .ToArray();

                RailCraft.Editor.FactorySceneBuilder.Build();

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab.transform.Find(sentinelName), Is.Not.Null,
                    "Factory rebuilding must preserve replaceable model content.");
                Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path == sampleScenePath), Is.True,
                    "Factory rebuilding must preserve other build scenes.");
                Assert.That(EditorBuildSettings.scenes.Count(entry => entry.path == ScenePath), Is.EqualTo(1));
            }
            finally
            {
                RemovePrefabSentinel(prefabPath, sentinelName);
                EditorBuildSettings.scenes = originalScenes;
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void FactoryInputActionsExposeDocumentedBindings()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/RailCraft/Input/RailCraftControls.inputactions");
            var map = actions.FindActionMap("Factory", true);

            Assert.That(map.FindAction("Point", true).bindings.Any(binding => binding.path == "<Pointer>/position"));
            Assert.That(map.FindAction("PrimaryPress", true).bindings.Any(binding => binding.path == "<Mouse>/leftButton"));
            Assert.That(map.FindAction("OrbitPress", true).bindings.Any(binding => binding.path == "<Mouse>/rightButton"));
            Assert.That(map.FindAction("PanPress", true).bindings.Any(binding => binding.path == "<Mouse>/middleButton"));
            Assert.That(map.FindAction("PointerDelta", true).bindings.Any(binding => binding.path == "<Pointer>/delta"));
            Assert.That(map.FindAction("Zoom", true).bindings.Any(binding => binding.path == "<Mouse>/scroll/y"));
            Assert.That(map.FindAction("Cancel", true).bindings.Any(binding => binding.path == "<Keyboard>/escape"));
            Assert.That(map.FindAction("Move", true).bindings.Count(binding => binding.isComposite), Is.EqualTo(2));
        }

        [Test]
        public void AssemblyBayPrefabExistsAndContainsFactoryDetails()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/Factory/AssemblyBay.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.Find("AssemblyPlatform"), Is.Not.Null);
            Assert.That(prefab.transform.Find("CraneRails"), Is.Not.Null);
            Assert.That(prefab.transform.Find("SafetyStripes"), Is.Not.Null);
            Assert.That(prefab.transform.Find("WorkLights"), Is.Not.Null);
        }

        private static void AddPrefabSentinel(string path, string sentinelName)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.transform.Find(sentinelName) == null)
                {
                    var sentinel = new GameObject(sentinelName);
                    sentinel.transform.SetParent(root.transform, false);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemovePrefabSentinel(string path, string sentinelName)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var sentinel = root.transform.Find(sentinelName);
                if (sentinel != null)
                {
                    Object.DestroyImmediate(sentinel.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
