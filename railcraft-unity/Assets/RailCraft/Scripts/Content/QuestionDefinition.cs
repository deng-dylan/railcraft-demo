namespace RailCraft.Content
{
    [System.Serializable]
    public sealed class QuestionDefinition
    {
        public string id;
        public string type;
        public string prompt;
        public string[] options;
        public int correctOptionIndex;
    }
}
