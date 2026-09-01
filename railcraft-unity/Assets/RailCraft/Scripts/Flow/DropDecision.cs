namespace RailCraft.Flow
{
    public sealed class DropDecision
    {
        public bool Accepted { get; }
        public string Code { get; }

        public DropDecision(bool accepted, string code)
        {
            Accepted = accepted;
            Code = code;
        }
    }
}
