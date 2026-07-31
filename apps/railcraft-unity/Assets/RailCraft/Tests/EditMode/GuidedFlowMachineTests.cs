using NUnit.Framework;
using RailCraft.Flow;
using RailCraft.Tests.EditMode.Fixtures;

namespace RailCraft.Tests.EditMode
{
    public sealed class GuidedFlowMachineTests
    {
        [Test]
        public void CorrectAnswersUnlockOnlyTheCurrentStep()
        {
            var machine = FlowFixture.StartAtFirstKnowledgeGate();

            FlowFixture.AnswerCurrentStepCorrectly(machine);

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.StepReady));
            Assert.That(machine.Snapshot.CurrentStepId, Is.EqualTo("frame_module"));
            Assert.That(machine.Snapshot.StepIndex, Is.EqualTo(0));
        }

        [Test]
        public void WrongAnswerDoesNotAdvanceQuestionIndex()
        {
            var machine = FlowFixture.StartAtFirstKnowledgeGate();
            var before = machine.Snapshot.QuestionIndex;

            var result = machine.SubmitAnswer(FlowFixture.WrongOption(machine));

            Assert.That(result.IsCorrect, Is.False);
            Assert.That(machine.Snapshot.QuestionIndex, Is.EqualTo(before));
            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.KnowledgeGate));
        }

        [Test]
        public void WrongDropIsRejectedWithoutChangingStep()
        {
            var machine = FlowFixture.UnlockFirstStep();

            var decision = machine.ConfirmDrop("wheelset_axlebox_a");

            Assert.That(decision.Accepted, Is.False);
            Assert.That(decision.Code, Is.EqualTo("wrong_step"));
            Assert.That(machine.Snapshot.CurrentStepId, Is.EqualTo("frame_module"));
            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.StepReady));
        }

        [Test]
        public void FirstCommissioningFailsThenLoopPasses()
        {
            var machine = FlowFixture.ReachInitialCommissioning();

            machine.ConfirmDrop("commissioning");
            machine.ConfirmSnapAnimation();

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.Rework));
            Assert.That(machine.Snapshot.CommissioningAttempt, Is.EqualTo(1));

            machine.ConfirmReworkAcknowledged();
            FlowFixture.AnswerCurrentStepCorrectly(machine);
            machine.ConfirmDrop("inspection");
            machine.ConfirmSnapAnimation();

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.SecondCommissioning));

            machine.CompleteSecondCommissioning();

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.KnowledgeGate));
            Assert.That(machine.Snapshot.CurrentStepId, Is.EqualTo("release"));
            Assert.That(machine.Snapshot.CommissioningAttempt, Is.EqualTo(2));
        }

        [Test]
        public void SuccessfulFirstCommissioningOpensReleaseKnowledgeGate()
        {
            var content = FlowFixtureContent();
            content.Flow.failFirstCommissioning = false;
            var machine = ReachInitialCommissioning(content);

            machine.ConfirmDrop("commissioning");
            machine.ConfirmSnapAnimation();

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.KnowledgeGate));
            Assert.That(machine.Snapshot.CurrentStepId, Is.EqualTo("release"));
            Assert.That(machine.Snapshot.CommissioningAttempt, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseSnapCompletesTheRun()
        {
            var machine = ReachReleaseKnowledgeGate();
            FlowFixture.AnswerCurrentStepCorrectly(machine);
            machine.ConfirmDrop("release");

            machine.ConfirmSnapAnimation();

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.Completed));
            Assert.That(machine.Snapshot.CurrentStepId, Is.Null);
        }

        [Test]
        public void InvalidPhaseCallsLeaveTheCurrentStateUntouched()
        {
            var machine = new GuidedFlowMachine(FlowFixtureContent());

            machine.ConfirmGuidance();
            machine.ConfirmSnapAnimation();
            machine.ConfirmReworkAcknowledged();
            machine.CompleteSecondCommissioning();

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.MainMenu));
            Assert.That(machine.Snapshot.StepIndex, Is.EqualTo(0));
            Assert.That(machine.Snapshot.QuestionIndex, Is.EqualTo(0));
        }

        [Test]
        public void ResetClearsProgressWithoutPersistence()
        {
            var machine = FlowFixture.UnlockFirstStep();

            machine.Reset();

            Assert.That(machine.Snapshot.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(machine.Snapshot.StepIndex, Is.EqualTo(0));
            Assert.That(machine.Snapshot.QuestionIndex, Is.EqualTo(0));
            Assert.That(machine.Snapshot.CommissioningAttempt, Is.EqualTo(0));
        }

        private static RailCraft.Content.ContentBundle FlowFixtureContent()
        {
            return ContentFixture.CreateValid();
        }

        private static GuidedFlowMachine ReachReleaseKnowledgeGate()
        {
            var machine = FlowFixture.ReachInitialCommissioning();
            machine.ConfirmDrop("commissioning");
            machine.ConfirmSnapAnimation();
            machine.ConfirmReworkAcknowledged();
            FlowFixture.AnswerCurrentStepCorrectly(machine);
            machine.ConfirmDrop("inspection");
            machine.ConfirmSnapAnimation();
            machine.CompleteSecondCommissioning();
            return machine;
        }

        private static GuidedFlowMachine ReachInitialCommissioning(
            RailCraft.Content.ContentBundle content)
        {
            var machine = new GuidedFlowMachine(content);
            machine.StartNewRun();
            machine.ConfirmGuidance();
            while (machine.Snapshot.CurrentStepId != "commissioning")
            {
                FlowFixture.AnswerCurrentStepCorrectly(machine);
                machine.ConfirmDrop(machine.Snapshot.CurrentStepId);
                machine.ConfirmSnapAnimation();
            }

            FlowFixture.AnswerCurrentStepCorrectly(machine);
            return machine;
        }
    }
}
