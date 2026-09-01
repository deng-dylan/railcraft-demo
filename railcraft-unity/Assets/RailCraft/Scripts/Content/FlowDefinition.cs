namespace RailCraft.Content
{
    [System.Serializable]
    public sealed class FlowDefinition
    {
        public int schemaVersion;
        public string contentVersion;
        public bool failFirstCommissioning;
        public StepDefinition[] steps;
    }
}
