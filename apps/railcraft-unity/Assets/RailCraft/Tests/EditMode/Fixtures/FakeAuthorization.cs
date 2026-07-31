using RailCraft.Interaction;

namespace RailCraft.Tests.EditMode.Fixtures
{
    internal sealed class FakeAuthorization : IDragAuthorization
    {
        private readonly string unlockedStepId;
        private readonly bool isUnlocked;

        public FakeAuthorization(string unlockedStepId, bool isUnlocked)
        {
            this.unlockedStepId = unlockedStepId;
            this.isUnlocked = isUnlocked;
        }

        public bool CanDrag(string stepId)
        {
            return isUnlocked && stepId == unlockedStepId;
        }
    }
}
