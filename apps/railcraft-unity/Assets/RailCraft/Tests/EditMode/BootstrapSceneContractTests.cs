using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.Flow;
using RailCraft.Interaction;
using RailCraft.Presentation;
using RailCraft.Process;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RailCraft.Tests.EditMode
{
    public sealed class BootstrapSceneContractTests
    {
        private const string BootstrapPath = "Assets/RailCraft/Scenes/Bootstrap.unity";
        private const string FactoryPath = "Assets/RailCraft/Scenes/Factory.unity";

        [Test]
        public void BootstrapSceneIsFirstAndOwnsOneFullyReferencedCompositionRoot()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath), Is.Not.Null);
            Assert.That(EditorBuildSettings.scenes.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(BootstrapPath));
            Assert.That(EditorBuildSettings.scenes[1].path, Is.EqualTo(FactoryPath));

            var scene = EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Additive);
            try
            {
                var controllers = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<GuidedFlowController>(true))
                    .ToArray();
                Assert.That(controllers, Has.Length.EqualTo(1));
                var data = new SerializedObject(controllers[0]);
                foreach (var propertyName in new[]
                {
                    "questionsJson", "flowJson", "prefabCatalog", "quizPresenter",
                    "assemblyPresenter", "processPresenter", "completionPresenter",
                    "stepHud", "feedbackView", "dragDropController"
                })
                {
                    Assert.That(data.FindProperty(propertyName).objectReferenceValue, Is.Not.Null,
                        propertyName);
                }

                var roots = scene.GetRootGameObjects();
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<QuizView>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<StepHudView>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<FeedbackView>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<ProcessStagePresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<CompletionPresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<DragDropController>(true)).ToArray(),
                    Has.Length.EqualTo(1));

                var names = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(item => item.name).ToArray();
                Assert.That(names.Any(name => name.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.Contains("分数") || name.Contains("得分")), Is.False);
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<Text>(true))
                    .Select(text => text.text), Has.Some.EqualTo("退出"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
