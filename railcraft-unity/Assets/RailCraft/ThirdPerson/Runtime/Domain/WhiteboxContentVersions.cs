namespace RailCraft.ThirdPerson.Domain
{
    /// <summary>
    /// Independent content identities stored beside the structural snapshot
    /// schema. Change only the affected value when content is revised, then
    /// add an explicit migration before accepting the previous value.
    /// </summary>
    public static class WhiteboxContentVersions
    {
        public const string QuestionBank = "question-bank-user-doc-58-v1";
        public const string CoreQuestionSet = "core-packages-10-v1";
        public const string WorkPackageRecipe = "work-packages-5-v1";
        public const string RewardRouting = "legacy-reward-routing-v2";
    }
}
