using System.Collections;
using System.Linq;
using NUnit.Framework;
using RailCraft.Flow;
using RailCraft.Process;
using RailCraft.Tests.PlayMode.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace RailCraft.Tests.PlayMode
{
    public sealed class FullFlowTests
    {
        [UnityTest]
        public IEnumerator FullGuidedRunReachesCompletedAndUsesAllProductionIds()
        {
            using var fixture = FullFlowFixture.Create();
            fixture.StartNewRun();

            yield return CompleteRun(fixture);

            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Completed));
            Assert.That(fixture.AnsweredQuestionIds, Is.EquivalentTo(fixture.ExpectedQuestionIds));
            Assert.That(fixture.CompletedStepIds, Is.EquivalentTo(fixture.ExpectedStepIds));
            Assert.That(fixture.QuestionAnswerEventCount, Is.EqualTo(48));
            Assert.That(fixture.StepDropEventCount, Is.EqualTo(15));
            Assert.That(fixture.Controller.QuestionsAnswered, Is.EqualTo(48));
            Assert.That(fixture.Controller.CompletedUniqueSteps, Is.EqualTo(15));
            Assert.That(fixture.Controller.Snapshot.CommissioningAttempt, Is.EqualTo(2));
            Assert.That(fixture.ScoreUiCount, Is.EqualTo(0));
            Assert.That(fixture.Completion.Message, Is.EqualTo("流程完成：已投入使用"));
        }

        [UnityTest]
        public IEnumerator CommissioningUsesTeachingFailureInspectionAndQuestionFreeRetry()
        {
            using var fixture = FullFlowFixture.Create();
            fixture.StartNewRun();

            yield return AdvanceUntil(fixture, FlowPhase.Rework, 180);
            Assert.That(fixture.Process.Message,
                Is.EqualTo(TeachingOutcomeProvider.TeachingAnomalyMessage));
            Assert.That(fixture.Process.InspectionMarkerVisible, Is.True);
            Assert.That(fixture.Assembly.IsInstalledHighlighted("sensor_module"), Is.True);
            Assert.That(fixture.QuestionsAnswered, Is.EqualTo(41));
            Assert.That(fixture.CompletedStepIds, Does.Contain("commissioning"));

            var firstPulseScale = fixture.Process.InspectionMarkerScale;
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(firstPulseScale, fixture.Process.InspectionMarkerScale),
                Is.GreaterThan(0.0001f));

            fixture.Process.RequestReworkAcknowledgement();
            Assert.That(fixture.Assembly.IsInstalledHighlighted("sensor_module"), Is.False);
            var inspectionSafety = 0;
            while (fixture.Controller.Snapshot.Phase == FlowPhase.KnowledgeGate
                && inspectionSafety++ < 12)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                yield return null;
            }
            Assert.That(inspectionSafety, Is.LessThan(12),
                "The inspection questions did not unlock the inspection card.");
            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(FlowPhase.StepReady));
            Assert.That(fixture.Controller.Snapshot.CurrentStepId, Is.EqualTo("inspection"));
            Assert.That(fixture.QuestionsAnswered, Is.EqualTo(45));
            fixture.DropCurrentItemWhenUnlocked();
            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(FlowPhase.SecondCommissioning));
            Assert.That(fixture.Process.Message,
                Is.EqualTo(TeachingOutcomeProvider.InspectionCompleteMessage));
            Assert.That(fixture.Process.InspectionMarkerVisible, Is.False);
            Assert.That(fixture.IsQuizVisible, Is.False);

            var beforeRetry = fixture.QuestionsAnswered;
            fixture.Process.RequestSecondCommissioningCompletion();
            yield return null;
            Assert.That(fixture.QuestionsAnswered, Is.EqualTo(beforeRetry));
            Assert.That(fixture.Controller.Snapshot.CurrentStepId, Is.EqualTo("release"));
            Assert.That(fixture.Controller.Snapshot.CommissioningAttempt, Is.EqualTo(2));
            Assert.That(fixture.Process.PassIndicatorVisible, Is.True);
            Assert.That(fixture.AnsweredQuestionIds.Intersect(
                new[] { "q038", "q039", "q040", "q041" }).Count(), Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator ResetClearsProgressAndSecondRunHasNoDuplicateEvents()
        {
            using var fixture = FullFlowFixture.Create();
            fixture.StartNewRun();
            for (var index = 0; index < 10; index++)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                fixture.DropCurrentItemWhenUnlocked();
                yield return null;
            }

            Assert.That(fixture.Assembly.InstalledVisualCount, Is.GreaterThan(0));
            Assert.That(fixture.QuestionsAnswered, Is.GreaterThan(0));
            fixture.Controller.ResetRun();
            yield return null;

            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(fixture.Assembly.InstalledVisualCount, Is.EqualTo(0));
            Assert.That(fixture.Assembly.CurrentModule, Is.Null);
            Assert.That(fixture.Controller.QuestionsAnswered, Is.EqualTo(0));
            Assert.That(fixture.Controller.CompletedUniqueSteps, Is.EqualTo(0));
            Assert.That(fixture.Controller.Snapshot.CommissioningAttempt, Is.EqualTo(0));
            Assert.That(fixture.Process.Message, Is.Empty);
            Assert.That(fixture.Completion.IsVisible, Is.False);
            Assert.That(fixture.IsQuizVisible, Is.False);

            fixture.ResetObservedEvents();
            fixture.StartNewRun();
            yield return CompleteRun(fixture);
            Assert.That(fixture.QuestionAnswerEventCount, Is.EqualTo(48));
            Assert.That(fixture.StepDropEventCount, Is.EqualTo(15));
            Assert.That(fixture.AnsweredQuestionIds, Is.EquivalentTo(fixture.ExpectedQuestionIds));
            Assert.That(fixture.CompletedStepIds, Is.EquivalentTo(fixture.ExpectedStepIds));
        }

        [UnityTest]
        public IEnumerator CarbodyLoweringUsesVerticalMotionConstraint()
        {
            using var fixture = FullFlowFixture.Create();
            fixture.StartNewRun();
            var safety = 0;
            while (fixture.Controller.Snapshot.CurrentStepId != "carbody_lowering" && safety++ < 160)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                fixture.DropCurrentItemWhenUnlocked();
                yield return null;
            }

            Assert.That(safety, Is.LessThan(160));
            Assert.That(fixture.Assembly.CurrentModule.MotionConstraint,
                Is.EqualTo(RailCraft.Interaction.DragMotionConstraint.Vertical));
            var start = fixture.Assembly.CurrentModule.transform.position;
            Assert.That(start.x, Is.EqualTo(fixture.Assembly.CurrentTarget.SnapAnchor.position.x)
                .Within(0.001f));
            Assert.That(start.z, Is.EqualTo(fixture.Assembly.CurrentTarget.SnapAnchor.position.z)
                .Within(0.001f));
            Assert.That(start.y, Is.GreaterThan(fixture.Assembly.CurrentTarget.SnapAnchor.position.y));
            fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
            while (fixture.Controller.Snapshot.Phase == FlowPhase.KnowledgeGate)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                yield return null;
            }
            Assert.That(fixture.Assembly.CurrentModule.InteractionCollider.enabled, Is.True);
            Assert.That(fixture.Controller.CanDrag("carbody_lowering"), Is.True);
            fixture.DropCurrentItemWhenUnlocked();
            Assert.That(fixture.Controller.Snapshot.CurrentStepId, Is.EqualTo("commissioning"));
        }

        [UnityTest]
        public IEnumerator ResetCancelsActiveDragAndPendingSnapWithoutGhostCompletion()
        {
            using var fixture = FullFlowFixture.Create(0.25f);
            fixture.StartNewRun();
            while (fixture.Controller.Snapshot.Phase == FlowPhase.KnowledgeGate)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                yield return null;
            }

            fixture.BeginCurrentDrag();
            Assert.That(fixture.IsPartDragActive, Is.True);
            fixture.Controller.ResetRun();
            Assert.That(fixture.IsPartDragActive, Is.False);
            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(fixture.StepDropEventCount, Is.EqualTo(0));

            fixture.ResetObservedEvents();
            fixture.StartNewRun();
            while (fixture.Controller.Snapshot.Phase == FlowPhase.KnowledgeGate)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                yield return null;
            }
            fixture.BeginCurrentDrag();
            Assert.That(fixture.ReleaseCurrentDragAtTarget().Accepted, Is.True);
            Assert.That(fixture.Assembly.CurrentModule.IsSnapping, Is.True);
            fixture.Controller.ResetRun();
            yield return new WaitForSecondsRealtime(0.35f);

            Assert.That(fixture.Controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Guidance));
            Assert.That(fixture.StepDropEventCount, Is.EqualTo(0));
            Assert.That(fixture.Assembly.InstalledVisualCount, Is.EqualTo(0));
            fixture.StartNewRun();
            Assert.That(fixture.Controller.Snapshot.CurrentStepId, Is.EqualTo("frame_module"));
            Assert.That(fixture.Assembly.CurrentModule, Is.Not.Null);
        }

        private static IEnumerator CompleteRun(FullFlowFixture fixture)
        {
            var safety = 0;
            while (fixture.Controller.Snapshot.Phase != FlowPhase.Completed && safety++ < 220)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                fixture.DropCurrentItemWhenUnlocked();
                yield return null;
            }
            Assert.That(safety, Is.LessThan(220), "The guided run did not reach completion.");
        }

        private static IEnumerator AdvanceUntil(FullFlowFixture fixture,
            FlowPhase targetPhase, int maximumFrames)
        {
            var safety = 0;
            while (fixture.Controller.Snapshot.Phase != targetPhase && safety++ < maximumFrames)
            {
                fixture.AnswerCurrentQuestionCorrectlyWhenVisible();
                fixture.DropCurrentItemWhenUnlocked();
                yield return null;
            }
            Assert.That(safety, Is.LessThan(maximumFrames),
                $"The flow did not reach {targetPhase}.");
        }
    }
}
