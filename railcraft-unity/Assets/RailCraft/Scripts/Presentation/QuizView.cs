using System;
using System.Collections.Generic;
using RailCraft.Content;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    public interface IQuizView
    {
        event Action<int> OptionSelected;
        bool IsVisible { get; }
        bool AreOptionsInteractable { get; }
        void ShowQuestion(string stageName, int questionNumber, int questionCount,
            QuestionDefinition question);
        void SetFeedback(string message);
        void SetOptionsInteractable(bool interactable);
        void Hide();
    }

    [DisallowMultipleComponent]
    public sealed class QuizView : MonoBehaviour, IQuizView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text stageNameText;
        [SerializeField] private Text questionCounterText;
        [SerializeField] private Text promptText;
        [SerializeField] private Transform optionButtonContainer;
        [SerializeField] private Button optionButtonTemplate;
        [SerializeField] private Text feedbackText;

        private readonly List<Button> optionButtons = new List<Button>();

        public event Action<int> OptionSelected;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
        public bool AreOptionsInteractable => optionButtons.Count > 0
            && optionButtons.TrueForAll(button => button != null && button.interactable);
        public string StageNameText => stageNameText == null ? string.Empty : stageNameText.text;
        public string QuestionCounterText => questionCounterText == null ? string.Empty : questionCounterText.text;
        public string PromptText => promptText == null ? string.Empty : promptText.text;
        public string FeedbackText => feedbackText == null ? string.Empty : feedbackText.text;
        public int OptionCount => optionButtons.Count;

        public void Configure(GameObject configuredPanelRoot, Text configuredStageName,
            Text configuredQuestionCounter, Text configuredPrompt, Transform configuredContainer,
            Button configuredTemplate, Text configuredFeedback)
        {
            panelRoot = configuredPanelRoot;
            stageNameText = configuredStageName;
            questionCounterText = configuredQuestionCounter;
            promptText = configuredPrompt;
            optionButtonContainer = configuredContainer;
            optionButtonTemplate = configuredTemplate;
            feedbackText = configuredFeedback;
            if (optionButtonTemplate != null)
                optionButtonTemplate.gameObject.SetActive(false);
        }

        public void ShowQuestion(string stageName, int questionNumber, int questionCount,
            QuestionDefinition question)
        {
            if (question == null)
                throw new ArgumentNullException(nameof(question));
            if (question.options == null || (question.options.Length != 2 && question.options.Length != 4))
                throw new ArgumentException("Quiz questions must expose exactly two or four options.", nameof(question));

            if (panelRoot != null)
                panelRoot.SetActive(true);
            if (stageNameText != null)
                stageNameText.text = stageName ?? string.Empty;
            if (questionCounterText != null)
                questionCounterText.text = $"知识准备题 {questionNumber}/{questionCount}";
            if (promptText != null)
                promptText.text = question.prompt ?? string.Empty;
            if (feedbackText != null)
                feedbackText.text = string.Empty;

            RebuildOptions(question.options);
            SetOptionsInteractable(true);
        }

        public void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message ?? string.Empty;
        }

        public void SetOptionsInteractable(bool interactable)
        {
            foreach (var button in optionButtons)
            {
                if (button != null)
                    button.interactable = interactable;
            }
        }

        public void Hide()
        {
            SetOptionsInteractable(false);
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void RebuildOptions(IReadOnlyList<string> options)
        {
            foreach (var oldButton in optionButtons)
            {
                if (oldButton == null)
                    continue;
                oldButton.gameObject.SetActive(false);
                Destroy(oldButton.gameObject);
            }
            optionButtons.Clear();

            if (optionButtonTemplate == null || optionButtonContainer == null)
                return;

            for (var index = 0; index < options.Count; index++)
            {
                var capturedIndex = index;
                var button = Instantiate(optionButtonTemplate, optionButtonContainer);
                button.name = $"OptionButton_{index + 1}";
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OptionSelected?.Invoke(capturedIndex));
                var label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = options[index] ?? string.Empty;
                optionButtons.Add(button);
            }
        }
    }
}
