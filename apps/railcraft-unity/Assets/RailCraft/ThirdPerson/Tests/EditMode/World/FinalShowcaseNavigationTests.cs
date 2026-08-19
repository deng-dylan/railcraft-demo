using NUnit.Framework;
using RailCraft.ThirdPerson.Editor;
using RailCraft.ThirdPerson.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class FinalShowcaseNavigationTests
    {
        private GameObject root;
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("FinalShowcaseNavigationTests");
            originalTimeScale = Time.timeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void MissingShowcaseSceneHidesActionAndRejectsTransition()
        {
            var completion = Child("Completion");
            var button = Child("ShowcaseButton", completion).AddComponent<Button>();
            var loadCount = 0;
            var controller = root.AddComponent<FinalShowcaseEntryController>();

            controller.Configure(
                null,
                null,
                completion,
                button,
                configuredSceneAvailability: _ => false,
                configuredSceneLoader: _ => loadCount++);

            Assert.That(controller.IsSceneAvailable, Is.False);
            Assert.That(controller.CanEnterShowcase, Is.False);
            Assert.That(button.gameObject.activeSelf, Is.False);
            Assert.That(controller.TryEnterShowcase(), Is.False);
            Assert.That(loadCount, Is.Zero);
        }

        [Test]
        public void CompletedSettlementButtonLoadsAvailableShowcase()
        {
            var completion = Child("Completion");
            var button = Child("ShowcaseButton", completion).AddComponent<Button>();
            string loadedScene = null;
            var controller = root.AddComponent<FinalShowcaseEntryController>();

            controller.Configure(
                null,
                null,
                completion,
                button,
                configuredSceneAvailability: scene =>
                    scene == FinalShowcaseEntryController.DefaultShowcaseSceneName,
                configuredSceneLoader: scene => loadedScene = scene);

            Assert.That(controller.CanEnterShowcase, Is.True);
            Assert.That(button.gameObject.activeSelf, Is.True);
            Assert.That(button.interactable, Is.True);

            button.onClick.Invoke();

            Assert.That(loadedScene, Is.EqualTo("FinalShowcase"));
        }

        [Test]
        public void AvailableShowcaseWaitsForCompletionPanelBeforeEnabling()
        {
            var completion = Child("Completion");
            var button = Child("ShowcaseButton", completion).AddComponent<Button>();
            completion.SetActive(false);
            var controller = root.AddComponent<FinalShowcaseEntryController>();

            controller.Configure(
                null,
                null,
                completion,
                button,
                configuredSceneAvailability: _ => true,
                configuredSceneLoader: _ => Assert.Fail("Incomplete session must not load."));

            Assert.That(controller.IsSceneAvailable, Is.True);
            Assert.That(controller.CanEnterShowcase, Is.False);
            Assert.That(button.interactable, Is.False);

            completion.SetActive(true);
            controller.RefreshAvailability();

            Assert.That(controller.CanEnterShowcase, Is.True);
            Assert.That(button.interactable, Is.True);
        }

        [Test]
        public void ShowcaseReturnUsesExistingFactorySceneContract()
        {
            var button = Child("ReturnButton").AddComponent<Button>();
            string loadedScene = null;
            var controller = root.AddComponent<FinalShowcaseReturnController>();

            controller.Configure(
                button,
                configuredSceneAvailability: scene =>
                    scene == FinalShowcaseReturnController.DefaultFactorySceneName,
                configuredSceneLoader: scene => loadedScene = scene);

            Assert.That(controller.FactorySceneName, Is.EqualTo("ThirdPersonWhitebox"));
            Assert.That(controller.CanReturnToFactory, Is.True);
            button.onClick.Invoke();
            Assert.That(loadedScene, Is.EqualTo("ThirdPersonWhitebox"));
        }

        [Test]
        public void WindowsBuildKeepsFactoryFirstAndOnlyAppendsExistingShowcase()
        {
            Assert.That(
                WhiteboxWindowsBuild.ResolveBuildScenePaths(false),
                Is.EqualTo(new[] { WhiteboxWindowsBuild.ScenePath }));
            Assert.That(
                WhiteboxWindowsBuild.ResolveBuildScenePaths(true),
                Is.EqualTo(new[]
                {
                    WhiteboxWindowsBuild.ScenePath,
                    WhiteboxWindowsBuild.FinalShowcaseScenePath
                }));
        }

        private GameObject Child(string name, GameObject parent = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent((parent ?? root).transform, false);
            return child;
        }
    }
}
