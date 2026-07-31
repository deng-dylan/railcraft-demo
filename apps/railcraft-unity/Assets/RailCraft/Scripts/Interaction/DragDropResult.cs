namespace RailCraft.Interaction
{
    public sealed class DragDropResult
    {
        public bool Accepted { get; }
        public string Code { get; }
        public string StepId { get; }
        public DropTarget Target { get; }

        public DragDropResult(bool accepted, string code, string stepId, DropTarget target)
        {
            Accepted = accepted;
            Code = code;
            StepId = stepId;
            Target = target;
        }
    }
}
