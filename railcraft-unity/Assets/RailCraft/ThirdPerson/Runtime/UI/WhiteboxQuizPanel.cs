using System;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    [DisallowMultipleComponent]
    public sealed class WhiteboxQuizPanel : MonoBehaviour, IQuizDialog
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Button[] optionButtons = Array.Empty<Button>();
        [SerializeField] private Button cancelButton;

        private Action<int> optionSelected;
        private Action cancelled;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public string PromptText => promptText == null ? string.Empty : promptText.text;
        public string FeedbackText => feedbackText == null ? string.Empty : feedbackText.text;

        public void Configure(
            GameObject configuredPanelRoot,
            Text configuredPromptText,
            Text configuredFeedbackText,
            Button[] configuredOptionButtons,
            Button configuredCancelButton)
        {
            panelRoot = configuredPanelRoot;
            promptText = configuredPromptText;
            feedbackText = configuredFeedbackText;
            optionButtons = configuredOptionButtons == null
                ? Array.Empty<Button>()
                : (Button[])configuredOptionButtons.Clone();
            cancelButton = configuredCancelButton;
            WireCancelButton();
            Dismiss();
        }

        public void Present(
            QuizQuestionPresentation question,
            Action<int> configuredOptionSelected,
            Action configuredCancelled)
        {
            if (question == null)
                throw new ArgumentNullException(nameof(question));
            if (!question.IsValid)
                throw new ArgumentException("The quiz presentation is incomplete.", nameof(question));
            if (optionButtons == null || optionButtons.Length < question.Options.Count)
                throw new InvalidOperationException("The quiz panel does not have enough option buttons.");

            optionSelected = configuredOptionSelected;
            cancelled = configuredCancelled;
            if (promptText != null)
                promptText.text = question.Prompt;
            if (feedbackText != null)
                feedbackText.text = string.Empty;

            for (var index = 0; index < optionButtons.Length; index++)
            {
                var button = optionButtons[index];
                if (button == null)
                    continue;

                var hasOption = index < question.Options.Count;
                button.gameObject.SetActive(hasOption);
                button.onClick.RemoveAllListeners();
                if (!hasOption)
                    continue;

                var capturedIndex = index;
                var label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    var prefix = question.Options.Count > 2
                        ? $"{(char)('A' + index)}. "
                        : string.Empty;
                    label.text = prefix + question.Options[index];
                }
                button.interactable = true;
                button.onClick.AddListener(() => optionSelected?.Invoke(capturedIndex));
            }

            WireCancelButton();
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message ?? string.Empty;
        }

        public void Dismiss()
        {
            optionSelected = null;
            cancelled = null;
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Awake()
        {
            WireCancelButton();
        }

        private void WireCancelButton()
        {
            if (cancelButton == null)
                return;

            cancelButton.onClick.RemoveListener(HandleCancelClicked);
            cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        private void HandleCancelClicked()
        {
            var callback = cancelled;
            Dismiss();
            callback?.Invoke();
        }
    }
}
