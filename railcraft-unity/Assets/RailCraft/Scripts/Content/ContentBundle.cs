namespace RailCraft.Content
{
    public sealed class ContentBundle
    {
        public QuestionDefinition[] Questions { get; }
        public FlowDefinition Flow { get; }

        public ContentBundle(QuestionDefinition[] questions, FlowDefinition flow)
        {
            Questions = questions;
            Flow = flow;
        }
    }
}
