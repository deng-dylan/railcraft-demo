using RailCraft.Content;
using RailCraft.Flow;

namespace RailCraft.Tests.EditMode.Fixtures
{
    internal static class FlowFixture
    {
        public static GuidedFlowMachine StartAtFirstKnowledgeGate()
        {
            var machine = new GuidedFlowMachine(ContentFixture.CreateValid());
            machine.StartNewRun();
            machine.ConfirmGuidance();
            return machine;
        }

        public static GuidedFlowMachine UnlockFirstStep()
        {
            var machine = StartAtFirstKnowledgeGate();
            AnswerCurrentStepCorrectly(machine);
            return machine;
        }

        public static GuidedFlowMachine ReachInitialCommissioning()
        {
            var machine = StartAtFirstKnowledgeGate();
            while (machine.Snapshot.CurrentStepId != "commissioning")
            {
                AnswerCurrentStepCorrectly(machine);
                machine.ConfirmDrop(machine.Snapshot.CurrentStepId);
                machine.ConfirmSnapAnimation();
            }

            AnswerCurrentStepCorrectly(machine);
            return machine;
        }

        public static int WrongOption(GuidedFlowMachine machine)
        {
            return 1;
        }

        public static void AnswerCurrentStepCorrectly(GuidedFlowMachine machine)
        {
            while (machine.Snapshot.Phase == FlowPhase.KnowledgeGate)
                machine.SubmitAnswer(0);
        }
    }
}
