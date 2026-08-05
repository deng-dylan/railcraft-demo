namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Presentation event raised after every answer submission, including invalid
    /// question identifiers and option indices.
    /// </summary>
    public readonly struct WhiteboxAnswerEvaluatedEvent
    {
        public WhiteboxAnswerEvaluatedEvent(string questionId, WorldAnswerResult result)
        {
            QuestionId = questionId ?? string.Empty;
            Result = result;
        }

        public string QuestionId { get; }
        public WorldAnswerResult Result { get; }
    }
}
