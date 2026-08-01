using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RailCraft.Assets;
using RailCraft.CameraSystem;
using RailCraft.Content;
using RailCraft.Flow;
using RailCraft.Interaction;
using RailCraft.Presentation;
using RailCraft.Process;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RailCraft.Tests.PlayMode
{
    public sealed class BootstrapStartupTests
    {
        [UnityTest]
        public IEnumerator ProductionBootstrapLoadsFactoryAndWiresFirstInteractiveStep()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Additive);
            var bootstrap = SceneManager.GetSceneByName("Bootstrap");
            Assert.That(bootstrap.isLoaded, Is.True);
            var controller = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GuidedFlowController>(true))
                .Single();

            var deadline = Time.realtimeSinceStartup + 5f;
            while (!controller.IsConfigured && controller.FatalErrorCode == null
                && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            Assert.That(controller.FatalErrorCode, Is.Null);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(FlowPhase.MainMenu));
            Assert.That(SceneManager.GetSceneByName("Factory").isLoaded, Is.True);
            Assert.That(Object.FindObjectsByType<DropTarget>(FindObjectsSortMode.None), Has.Length.EqualTo(15));

            var assembly = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<AssemblyPresenter>(true)).Single();
            var drag = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DragDropController>(true)).Single();
            Assert.That(assembly.LockedFutureCount, Is.EqualTo(3));
            var quizView = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<QuizView>(true)).Single();
            var cameraDirector = Object.FindFirstObjectByType<CameraShotDirector>();
            var mainMenu = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MainMenuPresenter>(true)).Single();
            var guidance = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GuidancePresenter>(true)).Single();
            var buttons = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Button>(true)).ToArray();
            Assert.That(mainMenu.IsVisible, Is.True);
            Assert.That(buttons.Select(ButtonLabel), Does.Not.Contain("继续游戏"));
            Click(buttons.Single(button => ButtonLabel(button) == "设置"));
            yield return null;
            var settings = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SettingsPresenter>(true)).Single();
            Assert.That(settings.IsVisible, Is.True);
            var qualityDropdown = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Dropdown>(true))
                .Single(dropdown => dropdown.name == "QualityDropdown");
            Click(qualityDropdown);
            yield return null;
            Assert.That(bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Any(item => item.name == "Dropdown List"), Is.True);
            qualityDropdown.Hide();
            yield return null;
            Click(buttons.Single(button => ButtonLabel(button) == "返回主菜单"));
            yield return null;
            Assert.That(mainMenu.IsVisible, Is.True);
            Click(buttons.Single(button => ButtonLabel(button) == "开始体验"));
            yield return null;
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(mainMenu.IsVisible, Is.False);
            Assert.That(guidance.IsVisible, Is.True);
            Assert.That(guidance.Copy, Is.EqualTo(GuidancePresenter.RequiredCopy));
            Click(buttons.Single(button => ButtonLabel(button) == "开始装配"));
            yield return null;
            Assert.That(controller.Snapshot.CurrentStepId, Is.EqualTo("frame_module"));
            Assert.That(quizView.IsVisible, Is.True);
            Assert.That(assembly.CurrentModule, Is.Not.Null);
            Assert.That(assembly.IsTargetHighlighted, Is.True);
            Assert.That(cameraDirector.CurrentShotId, Is.EqualTo("frame_module"));

            for (var index = 0; index < 4; index++)
            {
                controller.SubmitAnswer(controller.CurrentQuestion.correctOptionIndex);
                yield return new WaitForSecondsRealtime(0.22f);
            }
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(FlowPhase.StepReady));
            Assert.That(assembly.CurrentModule.InteractionCollider.enabled, Is.True);
            Assert.That(drag.TryBeginDrag(assembly.CurrentModule), Is.True);
            drag.DragTo(assembly.CurrentTarget.SnapAnchor.position);
            Assert.That(drag.ReleaseAt(assembly.CurrentModule.transform.position).Accepted, Is.True);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(assembly.InstalledVisualCount, Is.EqualTo(1));
            Assert.That(controller.Snapshot.CurrentStepId, Is.EqualTo("wheelset_axlebox_a"));
            Assert.That(cameraDirector.CurrentShotId, Is.EqualTo("wheelset_axlebox_a"));

            Click(buttons.Single(button => ButtonLabel(button) == "重置流程"));
            yield return null;
            Assert.That(bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Text>(true))
                .Select(text => text.text), Has.Some.EqualTo(ResetPresenter.RequiredConfirmationCopy));
            Click(buttons.Single(button => ButtonLabel(button) == "确认重置"));
            yield return null;
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(assembly.InstalledVisualCount, Is.EqualTo(0));
            Assert.That(guidance.IsVisible, Is.True);
            Assert.That(cameraDirector.CurrentShotId, Is.EqualTo("overview"));

            yield return SceneManager.UnloadSceneAsync("Factory");
            yield return SceneManager.UnloadSceneAsync("Bootstrap");
        }

        [UnityTest]
        public IEnumerator InvalidProductionContentShowsIssueCodeOnFatalSurface()
        {
            var root = new GameObject("invalid.bootstrap.fixture");
            var invalidQuestions = new TextAsset("{\"questions\":[]}");
            var flow = new TextAsset(System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath, "RailCraft", "Content", "V1", "flow.v1.json")));
            var catalog = ScriptableObject.CreateInstance<PartPrefabCatalog>();
            var quiz = root.AddComponent<QuizPresenter>();
            var assembly = root.AddComponent<AssemblyPresenter>();
            var process = root.AddComponent<ProcessStagePresenter>();
            var completion = root.AddComponent<CompletionPresenter>();
            var drag = root.AddComponent<DragDropController>();
            var controller = root.AddComponent<GuidedFlowController>();
            var mainPanel = CreateChild(root.transform, "fatal.main");
            var mainMenu = mainPanel.AddComponent<MainMenuPresenter>();
            mainMenu.ConfigureView(mainPanel, null, null, null, null);
            var guidancePanel = CreateChild(root.transform, "fatal.guidance");
            var guidance = guidancePanel.AddComponent<GuidancePresenter>();
            guidance.ConfigureView(guidancePanel, null, null);
            var settingsPanel = CreateChild(root.transform, "fatal.settings");
            var settings = settingsPanel.AddComponent<SettingsPresenter>();
            settings.ConfigureView(settingsPanel, null, null, null);
            var resetPanel = CreateChild(root.transform, "fatal.reset");
            var reset = root.AddComponent<ResetPresenter>();
            reset.ConfigureView(resetPanel, null, null, null, null);
            guidance.Bind(controller, mainMenu);
            settings.Bind(mainMenu);
            mainMenu.Bind(controller, guidance, settings);
            reset.Bind(controller, guidance, mainMenu, settings);
            controller.ConfigureNavigation(mainMenu, guidance, settings, reset);
            mainMenu.Show();
            guidance.ShowForInformation();
            settings.Show();
            controller.ConfigureStartup(invalidQuestions, flow, catalog, quiz, assembly,
                process, completion, null, null, drag);

            yield return null;
            yield return null;

            Assert.That(controller.IsConfigured, Is.False);
            Assert.That(controller.FatalErrorCode, Is.EqualTo("question_count"));
            Assert.That(completion.IsFatal, Is.True);
            Assert.That(completion.Message, Does.Contain("question_count"));
            Assert.That(mainMenu.IsVisible, Is.False);
            Assert.That(guidance.IsVisible, Is.False);
            Assert.That(settings.IsVisible, Is.False);
            Assert.That(reset.IsConfirmationVisible, Is.False);

            Object.Destroy(root);
            Object.Destroy(catalog);
            Object.Destroy(invalidQuestions);
            Object.Destroy(flow);
        }

        [UnityTest]
        public IEnumerator MissingFactorySceneShowsStableFatalCode()
        {
            var root = new GameObject("missing.factory.fixture");
            var questions = new TextAsset(System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath, "RailCraft", "Content", "V1", "questions.v1.json")));
            var flow = new TextAsset(System.IO.File.ReadAllText(System.IO.Path.Combine(
                Application.dataPath, "RailCraft", "Content", "V1", "flow.v1.json")));
            var content = JsonContentRepository.Load(questions.text, flow.text);
            var entries = content.Flow.steps.Select(step =>
            {
                var template = new GameObject("template." + step.id);
                template.transform.SetParent(root.transform, false);
                template.SetActive(false);
                return new PartPrefabEntry(step.assetKey, template);
            }).ToArray();
            var catalog = ScriptableObject.CreateInstance<PartPrefabCatalog>();
            catalog.Configure(entries);
            var quiz = root.AddComponent<QuizPresenter>();
            var assembly = root.AddComponent<AssemblyPresenter>();
            var process = root.AddComponent<ProcessStagePresenter>();
            var completion = root.AddComponent<CompletionPresenter>();
            var drag = root.AddComponent<DragDropController>();
            var controller = root.AddComponent<GuidedFlowController>();
            LogAssert.Expect(LogType.Error,
                new Regex("Scene '__missing_factory_scene__' couldn't be loaded"));
            controller.ConfigureStartup(questions, flow, catalog, quiz, assembly,
                process, completion, null, null, drag, "__missing_factory_scene__");

            yield return null;
            yield return null;

            Assert.That(controller.IsConfigured, Is.False);
            Assert.That(controller.FatalErrorCode, Is.EqualTo("factory_scene_load_failed"));
            Assert.That(completion.IsFatal, Is.True);

            Object.Destroy(root);
            Object.Destroy(catalog);
            Object.Destroy(questions);
            Object.Destroy(flow);
        }

        private static string ButtonLabel(Button button)
        {
            return button == null
                ? string.Empty
                : button.GetComponentInChildren<Text>(true)?.text ?? string.Empty;
        }

        private static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.gameObject.activeInHierarchy, Is.True);
            Assert.That(button.interactable, Is.True);
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(eventSystem),
                ExecuteEvents.pointerClickHandler);
        }

        private static void Click(Dropdown dropdown)
        {
            Assert.That(dropdown, Is.Not.Null);
            Assert.That(dropdown.gameObject.activeInHierarchy, Is.True);
            Assert.That(dropdown.interactable, Is.True);
            var eventSystem = EventSystem.current;
            Assert.That(eventSystem, Is.Not.Null);
            ExecuteEvents.Execute(dropdown.gameObject, new PointerEventData(eventSystem),
                ExecuteEvents.pointerClickHandler);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
