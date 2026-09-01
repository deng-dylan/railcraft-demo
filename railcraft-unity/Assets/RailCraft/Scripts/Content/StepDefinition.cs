namespace RailCraft.Content
{
    [System.Serializable]
    public sealed class StepDefinition
    {
        public string id;
        public int order;
        public string displayName;
        public string phase;
        public string assetKey;
        public string dropTargetId;
        public string[] questionIds;
    }
}
