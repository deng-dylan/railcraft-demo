using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RailCraft.ThirdPerson.Domain
{
    public enum QuestionReviewStatus
    {
        PendingReview,
        ApprovedForCore,
        BackupOnly
    }

    public sealed class QuizQuestionDefinition
    {
        private readonly ReadOnlyCollection<string> options;

        public QuizQuestionDefinition(
            string id,
            string prompt,
            IEnumerable<string> options,
            int correctOptionIndex,
            PartId rewardPart)
            : this(
                id,
                prompt,
                options,
                correctOptionIndex,
                rewardPart,
                string.Empty)
        {
        }

        public QuizQuestionDefinition(
            string id,
            string prompt,
            IEnumerable<string> options,
            int correctOptionIndex,
            PartId rewardPart,
            string explanation)
            : this(
                id,
                prompt,
                options,
                correctOptionIndex,
                rewardPart,
                explanation,
                null,
                null,
                false,
                QuestionReviewStatus.BackupOnly,
                true)
        {
        }

        /// <summary>
        /// Full metadata constructor. RewardPart remains for old save and UI
        /// adapters; GrantsPart allows knowledge-only questions to live in the
        /// same source bank without inventing a new physical part.
        /// </summary>
        public QuizQuestionDefinition(
            string id,
            string prompt,
            IEnumerable<string> options,
            int correctOptionIndex,
            PartId rewardPart,
            string explanation,
            ModuleId? relatedModule,
            bool isCore,
            QuestionReviewStatus reviewStatus,
            bool grantsPart)
            : this(
                id,
                prompt,
                options,
                correctOptionIndex,
                rewardPart,
                explanation,
                relatedModule,
                null,
                isCore,
                reviewStatus,
                grantsPart)
        {
        }

        /// <summary>
        /// Full metadata constructor with an optional player-facing material package.
        /// </summary>
        public QuizQuestionDefinition(
            string id,
            string prompt,
            IEnumerable<string> options,
            int correctOptionIndex,
            PartId rewardPart,
            string explanation,
            ModuleId? relatedModule,
            WorkPackageId? relatedWorkPackage,
            bool isCore,
            QuestionReviewStatus reviewStatus,
            bool grantsPart)
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
            if (!Enum.IsDefined(typeof(QuestionReviewStatus), reviewStatus))
                throw new ArgumentOutOfRangeException(nameof(reviewStatus));
            if (relatedWorkPackage.HasValue &&
                !Enum.IsDefined(typeof(WorkPackageId), relatedWorkPackage.Value))
                throw new ArgumentOutOfRangeException(nameof(relatedWorkPackage));

            Id = id;
            Prompt = prompt;
            this.options = copiedOptions.AsReadOnly();
            CorrectOptionIndex = correctOptionIndex;
            RewardPart = rewardPart;
            Explanation = explanation ?? string.Empty;
            RelatedModule = relatedModule;
            RelatedWorkPackage = relatedWorkPackage;
            IsCore = isCore;
            ReviewStatus = reviewStatus;
            GrantsPart = grantsPart;
        }

        public string Id { get; }
        public string Prompt { get; }
        public IReadOnlyList<string> Options => options;
        public int CorrectOptionIndex { get; }
        public PartId RewardPart { get; }
        public string Explanation { get; }
        public ModuleId? RelatedModule { get; }
        public WorkPackageId? RelatedWorkPackage { get; }
        public bool IsCore { get; }
        public QuestionReviewStatus ReviewStatus { get; }
        public bool GrantsPart { get; }

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
