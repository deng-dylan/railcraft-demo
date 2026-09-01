using System.Linq;
using NUnit.Framework;
using RailCraft.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Tests.EditMode
{
    public sealed class UiPrefabContractTests
    {
        [Test]
        public void QuizPanelContainsRequiredFieldsAndNoScoreSurface()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/UI/QuizPanel.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<QuizView>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("StageNameText"), Is.Not.Null);
            Assert.That(prefab.transform.Find("QuestionCounterText"), Is.Not.Null);
            Assert.That(prefab.transform.Find("PromptText"), Is.Not.Null);
            Assert.That(prefab.transform.Find("OptionButtonContainer"), Is.Not.Null);
            Assert.That(prefab.transform.Find("FeedbackText"), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Text>(true)
                .Select(text => text.text), Has.None.Contains("分数"));
            Assert.That(prefab.GetComponentsInChildren<Text>(true)
                .Select(text => text.text), Has.None.Contains("正确率"));
            Assert.That(prefab.GetComponentsInChildren<Text>(true)
                .Select(text => text.text), Has.None.Contains("来源"));
            Assert.That(prefab.GetComponentInChildren<AudioSource>(true), Is.Null);
        }

        [Test]
        public void QuizPresenterPrefabUsesPointTwoSecondTransition()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/UI/QuizPanel.prefab");
            var presenter = prefab.GetComponent<QuizPresenter>();
            var data = new SerializedObject(presenter);

            Assert.That(presenter, Is.Not.Null);
            Assert.That(data.FindProperty("serializedView").objectReferenceValue,
                Is.SameAs(prefab.GetComponent<QuizView>()));
            Assert.That(data.FindProperty("transitionDuration").floatValue,
                Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(prefab.transform.Find("OptionButtonContainer/OptionButtonTemplate")
                .gameObject.activeSelf, Is.False);
        }

        [Test]
        public void StepHudAndFeedbackPrefabsExposeRequiredViews()
        {
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/UI/StepHud.prefab");
            var feedback = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/UI/FeedbackToast.prefab");

            Assert.That(hud, Is.Not.Null);
            var hudView = hud.GetComponent<StepHudView>();
            Assert.That(hudView, Is.Not.Null);
            Assert.That(hud.transform.Find("StageNameText"), Is.Not.Null);
            Assert.That(hud.transform.Find("ProgressText"), Is.Not.Null);
            Assert.That(hud.transform.Find("KnowledgeText"), Is.Not.Null);
            Assert.That(hud.transform.Find("HintText"), Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            var feedbackView = feedback.GetComponent<FeedbackView>();
            Assert.That(feedbackView, Is.Not.Null);
            Assert.That(feedback.transform.Find("MessageText"), Is.Not.Null);

            var hudData = new SerializedObject(hudView);
            Assert.That(hudData.FindProperty("stageNameText").objectReferenceValue, Is.Not.Null);
            Assert.That(hudData.FindProperty("progressText").objectReferenceValue, Is.Not.Null);
            Assert.That(hudData.FindProperty("knowledgeText").objectReferenceValue, Is.Not.Null);
            Assert.That(hudData.FindProperty("hintText").objectReferenceValue, Is.Not.Null);
            var feedbackData = new SerializedObject(feedbackView);
            Assert.That(feedbackData.FindProperty("panelRoot").objectReferenceValue, Is.SameAs(feedback));
            Assert.That(feedbackData.FindProperty("messageText").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void NavigationPrefabsExposeExactV01Surface()
        {
            var mainMenu = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/UI/MainMenu.prefab");
            var guidance = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/UI/GuidancePanel.prefab");
            var settings = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RailCraft/Art/Prefabs/UI/SettingsPanel.prefab");

            Assert.That(mainMenu, Is.Not.Null);
            Assert.That(guidance, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);
            Assert.That(mainMenu.GetComponent<MainMenuPresenter>(), Is.Not.Null);
            Assert.That(guidance.GetComponent<GuidancePresenter>(), Is.Not.Null);
            Assert.That(settings.GetComponent<SettingsPresenter>(), Is.Not.Null);

            var menuLabels = mainMenu.GetComponentsInChildren<Button>(true)
                .Select(button => button.GetComponentInChildren<Text>(true)?.text).ToArray();
            Assert.That(menuLabels, Is.EquivalentTo(new[] { "开始体验", "操作说明", "设置", "退出" }));
            Assert.That(menuLabels, Does.Not.Contain("继续游戏"));
            Assert.That(guidance.GetComponentsInChildren<Text>(true)
                .Select(text => text.text), Has.Some.EqualTo(GuidancePresenter.RequiredCopy));

            var settingsLabels = settings.GetComponentsInChildren<Text>(true)
                .Select(text => text.text).ToArray();
            Assert.That(settingsLabels, Does.Contain("画质"));
            Assert.That(settingsLabels, Does.Contain("窗口模式"));
            Assert.That(settingsLabels, Does.Not.Contain("音乐"));
            Assert.That(settingsLabels, Does.Not.Contain("音效"));
            var dropdowns = settings.GetComponentsInChildren<Dropdown>(true);
            Assert.That(dropdowns, Has.Length.EqualTo(2));
            Assert.That(dropdowns.Single(item => item.name == "QualityDropdown").options,
                Has.Count.EqualTo(3));
            Assert.That(dropdowns.Single(item => item.name == "WindowModeDropdown").options,
                Has.Count.EqualTo(2));
            Assert.That(settings.GetComponentInChildren<AudioSource>(true), Is.Null);
        }
    }
}
