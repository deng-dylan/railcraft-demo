using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class QuizQuestionDefinition
    {
        private readonly ReadOnlyCollection<string> options;

        public QuizQuestionDefinition(
            string id,
            string prompt,
            IEnumerable<string> options,
            int correctOptionIndex,
            PartId rewardPart)
            : this(id, prompt, options, correctOptionIndex, rewardPart, string.Empty)
        {
        }

        public QuizQuestionDefinition(
            string id,
            string prompt,
            IEnumerable<string> options,
            int correctOptionIndex,
            PartId rewardPart,
            string explanation)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A question id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("A question prompt is required.", nameof(prompt));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var copiedOptions = new List<string>(options);
            if (copiedOptions.Count < 2)
                throw new ArgumentException("A question needs at least two options.", nameof(options));
            if (copiedOptions.Exists(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Question options cannot be blank.", nameof(options));
            if (correctOptionIndex < 0 || correctOptionIndex >= copiedOptions.Count)
                throw new ArgumentOutOfRangeException(nameof(correctOptionIndex));

            Id = id;
            Prompt = prompt;
            this.options = copiedOptions.AsReadOnly();
            CorrectOptionIndex = correctOptionIndex;
            RewardPart = rewardPart;
            Explanation = explanation ?? string.Empty;
        }

        public string Id { get; }
        public string Prompt { get; }
        public IReadOnlyList<string> Options => options;
        public int CorrectOptionIndex { get; }
        public PartId RewardPart { get; }
        public string Explanation { get; }

        public bool IsValidOption(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < options.Count;
        }

        public bool IsCorrectOption(int optionIndex)
        {
            return IsValidOption(optionIndex) && optionIndex == CorrectOptionIndex;
        }
    }
}
