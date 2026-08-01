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
                    "stepHud", "feedbackView", "dragDropController", "mainMenuPresenter",
                    "guidancePresenter", "settingsPresenter", "resetPresenter"
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
                var stepHudRect = roots.SelectMany(root => root.GetComponentsInChildren<StepHudView>(true))
                    .Single().GetComponent<RectTransform>();
                Assert.That(stepHudRect.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(stepHudRect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(stepHudRect.pivot, Is.EqualTo(new Vector2(0f, 1f)));
                Assert.That(stepHudRect.anchoredPosition, Is.EqualTo(new Vector2(32f, -32f)));

                var feedbackRect = roots.SelectMany(root => root.GetComponentsInChildren<FeedbackView>(true))
                    .Single().GetComponent<RectTransform>();
                Assert.That(feedbackRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(feedbackRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(feedbackRect.pivot, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(feedbackRect.anchoredPosition, Is.EqualTo(new Vector2(0f, 36f)));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<ProcessStagePresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<CompletionPresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<MainMenuPresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<GuidancePresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<SettingsPresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<ResetPresenter>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<DragDropController>(true)).ToArray(),
                    Has.Length.EqualTo(1));
                var highlights = roots.SelectMany(root => root.GetComponentsInChildren<HighlightController>(true)).ToArray();
                var snapEffects = roots.SelectMany(root => root.GetComponentsInChildren<SnapEffectController>(true)).ToArray();
                Assert.That(highlights, Has.Length.EqualTo(1));
                Assert.That(snapEffects, Has.Length.EqualTo(1));
                var snapData = new SerializedObject(snapEffects[0]);
                Assert.That(snapData.FindProperty("highlightController").objectReferenceValue,
                    Is.SameAs(highlights[0]));
                Assert.That(snapData.FindProperty("dragDropController").objectReferenceValue, Is.Not.Null);
                var assembly = roots.SelectMany(root => root.GetComponentsInChildren<AssemblyPresenter>(true)).Single();
                Assert.That(new SerializedObject(assembly).FindProperty("snapEffectController").objectReferenceValue,
                    Is.SameAs(snapEffects[0]));

                var names = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(item => item.name).ToArray();
                Assert.That(names.Any(name => name.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.Contains("分数") || name.Contains("得分")), Is.False);
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<Text>(true))
                    .Select(text => text.text), Has.Some.EqualTo("退出"));
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<Text>(true))
                    .Select(text => text.text), Has.None.EqualTo("继续游戏"));

                var reset = roots.SelectMany(root => root.GetComponentsInChildren<ResetPresenter>(true)).Single();
                var resetData = new SerializedObject(reset);
                foreach (var propertyName in new[]
                {
                    "confirmationPanel", "confirmationText", "requestButton", "confirmButton",
                    "cancelButton", "flowController", "guidancePresenter", "mainMenuPresenter",
                    "settingsPresenter"
                })
                {
                    Assert.That(resetData.FindProperty(propertyName).objectReferenceValue, Is.Not.Null,
                        "ResetPresenter." + propertyName);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
