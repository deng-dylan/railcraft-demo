namespace RailCraft.Flow
{
    public sealed class AnswerResult
    {
        public bool IsCorrect { get; }
        public string QuestionId { get; }
        public int CorrectOptionIndex { get; }

        public AnswerResult(bool isCorrect, string questionId, int correctOptionIndex)
        {
            IsCorrect = isCorrect;
            QuestionId = questionId;
            CorrectOptionIndex = correctOptionIndex;
        }
    }
}
