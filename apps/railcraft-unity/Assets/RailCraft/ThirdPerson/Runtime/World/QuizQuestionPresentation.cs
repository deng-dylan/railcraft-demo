using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    [Serializable]
    public sealed class QuizQuestionPresentation
    {
        [SerializeField] private string questionId;
        [SerializeField, TextArea(2, 5)] private string prompt;
        [SerializeField] private string[] options = Array.Empty<string>();
        [SerializeField] private int[] submittedOptionIndices = Array.Empty<int>();
        [SerializeField, TextArea(2, 5)] private string explanation;

        public QuizQuestionPresentation(
            string configuredQuestionId,
            string configuredPrompt,
            IReadOnlyList<string> configuredOptions,
            string configuredExplanation)
        {
            questionId = configuredQuestionId ?? string.Empty;
            prompt = configuredPrompt ?? string.Empty;
            options = CopyOptions(configuredOptions);
            submittedOptionIndices = CreateIdentityMap(options.Length);
            explanation = configuredExplanation ?? string.Empty;
        }

        public QuizQuestionPresentation(
            string configuredQuestionId,
            string configuredPrompt,
            IReadOnlyList<string> configuredOptions,
            IReadOnlyList<int> configuredSubmittedOptionIndices,
            string configuredExplanation)
        {
            questionId = configuredQuestionId ?? string.Empty;
            prompt = configuredPrompt ?? string.Empty;
            options = CopyOptions(configuredOptions);
            submittedOptionIndices = CopyOptionMap(configuredSubmittedOptionIndices);
            explanation = configuredExplanation ?? string.Empty;
        }

        public string QuestionId => questionId ?? string.Empty;
        public string Prompt => prompt ?? string.Empty;
        public IReadOnlyList<string> Options => options ?? Array.Empty<string>();
        public string Explanation => explanation ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(QuestionId)
            && !string.IsNullOrWhiteSpace(Prompt)
            && Options.Count >= 2
            && submittedOptionIndices != null
            && submittedOptionIndices.Length == Options.Count;

        public int MapSubmittedOptionIndex(int displayedOptionIndex)
        {
            if (displayedOptionIndex < 0 || displayedOptionIndex >= Options.Count)
                return -1;
            if (submittedOptionIndices == null || submittedOptionIndices.Length != Options.Count)
                return displayedOptionIndex;
            return submittedOptionIndices[displayedOptionIndex];
        }

        private static string[] CopyOptions(IReadOnlyList<string> source)
        {
            if (source == null)
                return Array.Empty<string>();

            var copy = new string[source.Count];
            for (var index = 0; index < source.Count; index++)
                copy[index] = source[index] ?? string.Empty;
            return copy;
        }

        private static int[] CopyOptionMap(IReadOnlyList<int> source)
        {
            if (source == null)
                return Array.Empty<int>();

            var copy = new int[source.Count];
            for (var index = 0; index < source.Count; index++)
                copy[index] = source[index];
            return copy;
        }

        private static int[] CreateIdentityMap(int count)
        {
            var map = new int[count];
            for (var index = 0; index < count; index++)
                map[index] = index;
            return map;
        }
    }

    public interface IQuizDialog
    {
        bool IsOpen { get; }
        void Present(QuizQuestionPresentation question, Action<int> optionSelected, Action cancelled);
        void SetFeedback(string message);
        void Dismiss();
    }
}
