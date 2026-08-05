using System;

namespace RailCraft.ThirdPerson.Domain
{
    public enum KnowledgeEntryCategory
    {
        QuestionExplanation,
        Part,
        Assembly,
        Commissioning,
        VehicleOverview
    }

    public enum KnowledgeUnlockKind
    {
        CorrectAnswer,
        PartKnowledgeFromCorrectAnswer,
        ModuleCompleted,
        CommissioningPhaseReached,
        VehicleCompleted
    }

    public sealed class KnowledgeEntry
    {
        public KnowledgeEntry(
            string id,
            string title,
            string body,
            KnowledgeEntryCategory category,
            int unlockOrder,
            KnowledgeUnlockKind unlockKind,
            string sourceQuestionId = null,
            PartId? relatedPart = null,
            ModuleId? relatedModule = null,
            CommissioningPhase? relatedCommissioningPhase = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A knowledge entry id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("A knowledge entry title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Knowledge entry body text is required.", nameof(body));
            if (unlockOrder <= 0)
                throw new ArgumentOutOfRangeException(nameof(unlockOrder));
            if (relatedPart.HasValue && !Enum.IsDefined(typeof(PartId), relatedPart.Value))
                throw new ArgumentOutOfRangeException(nameof(relatedPart));
            if (relatedModule.HasValue && !Enum.IsDefined(typeof(ModuleId), relatedModule.Value))
                throw new ArgumentOutOfRangeException(nameof(relatedModule));
            if (relatedCommissioningPhase.HasValue &&
                !Enum.IsDefined(typeof(CommissioningPhase), relatedCommissioningPhase.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(relatedCommissioningPhase));
            }

            ValidateTrigger(
                category,
                unlockKind,
                sourceQuestionId,
                relatedPart,
                relatedModule,
                relatedCommissioningPhase);

            Id = id;
            Title = title;
            Body = body;
            Category = category;
            UnlockOrder = unlockOrder;
            UnlockKind = unlockKind;
            SourceQuestionId = sourceQuestionId;
            RelatedPart = relatedPart;
            RelatedModule = relatedModule;
            RelatedCommissioningPhase = relatedCommissioningPhase;
        }

        public string Id { get; }
        public string Title { get; }
        public string Body { get; }
        public KnowledgeEntryCategory Category { get; }
        public int UnlockOrder { get; }
        public KnowledgeUnlockKind UnlockKind { get; }
        public string SourceQuestionId { get; }
        public PartId? RelatedPart { get; }
        public ModuleId? RelatedModule { get; }
        public CommissioningPhase? RelatedCommissioningPhase { get; }

        private static void ValidateTrigger(
            KnowledgeEntryCategory category,
            KnowledgeUnlockKind unlockKind,
            string sourceQuestionId,
            PartId? relatedPart,
            ModuleId? relatedModule,
            CommissioningPhase? relatedCommissioningPhase)
        {
            switch (unlockKind)
            {
                case KnowledgeUnlockKind.CorrectAnswer:
                    if (category != KnowledgeEntryCategory.QuestionExplanation ||
                        string.IsNullOrWhiteSpace(sourceQuestionId) ||
                        !relatedPart.HasValue)
                    {
                        throw new ArgumentException(
                            "Correct-answer entries require a source question and related part.");
                    }
                    break;

                case KnowledgeUnlockKind.PartKnowledgeFromCorrectAnswer:
                    if (category != KnowledgeEntryCategory.Part || !relatedPart.HasValue)
                    {
                        throw new ArgumentException(
                            "Part entries require a related part.");
                    }
                    break;

                case KnowledgeUnlockKind.ModuleCompleted:
                    if (category != KnowledgeEntryCategory.Assembly || !relatedModule.HasValue)
                    {
                        throw new ArgumentException(
                            "Assembly entries require a related module.");
                    }
                    break;

                case KnowledgeUnlockKind.CommissioningPhaseReached:
                    if (category != KnowledgeEntryCategory.Commissioning ||
                        !relatedCommissioningPhase.HasValue ||
                        relatedCommissioningPhase == CommissioningPhase.Locked ||
                        relatedCommissioningPhase == CommissioningPhase.ReadyForInitialTest)
                    {
                        throw new ArgumentException(
                            "Commissioning entries require a completed commissioning phase.");
                    }
                    break;

                case KnowledgeUnlockKind.VehicleCompleted:
                    if (category != KnowledgeEntryCategory.VehicleOverview)
                    {
                        throw new ArgumentException(
                            "Vehicle-completion entries must be vehicle overviews.");
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(unlockKind));
            }

            if (unlockKind != KnowledgeUnlockKind.CorrectAnswer &&
                !string.IsNullOrWhiteSpace(sourceQuestionId))
            {
                throw new ArgumentException(
                    "Only correct-answer entries can reference a source question.",
                    nameof(sourceQuestionId));
            }

            if (unlockKind != KnowledgeUnlockKind.ModuleCompleted && relatedModule.HasValue)
            {
                throw new ArgumentException(
                    "Only assembly entries can reference a module.",
                    nameof(relatedModule));
            }

            if (unlockKind != KnowledgeUnlockKind.CommissioningPhaseReached &&
                relatedCommissioningPhase.HasValue)
            {
                throw new ArgumentException(
                    "Only commissioning entries can reference a commissioning phase.",
                    nameof(relatedCommissioningPhase));
            }
        }
    }
}
