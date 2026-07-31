using RailCraft.Interaction;

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class FakeAuthorization : IDragAuthorization
    {
        private readonly string unlockedStepId;

        public FakeAuthorization(string unlockedStepId)
        {
            this.unlockedStepId = unlockedStepId;
        }

        public bool CanDrag(string stepId)
        {
            return stepId == unlockedStepId;
        }
    }
}
