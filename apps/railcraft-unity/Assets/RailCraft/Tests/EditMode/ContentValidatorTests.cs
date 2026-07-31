using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.Content;
using RailCraft.Tests.EditMode.Fixtures;

namespace RailCraft.Tests.EditMode
{
    public sealed class ContentValidatorTests
    {
        [Test]
        public void ValidBundleUsesEveryQuestionExactlyOnce()
        {
            var issues = ContentValidator.Validate(ContentFixture.CreateValid());

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void DuplicateQuestionAssignmentIsRejected()
        {
            var bundle = ContentFixture.CreateValid();
            bundle.Flow.steps[1].questionIds[0] = "q001";

            var issues = ContentValidator.Validate(bundle);

            Assert.That(issues, Does.Contain("question_duplicate:q001"));
        }

        [Test]
        public void EveryStepRequiresAtLeastOneQuestion()
        {
            var bundle = ContentFixture.CreateValid();
            bundle.Flow.steps[3].questionIds = Array.Empty<string>();

            var issues = ContentValidator.Validate(bundle);

            Assert.That(issues, Does.Contain("step_without_questions:primary_suspension"));
        }

        [Test]
        public void FlowContainsExactlyFifteenOrderedSteps()
        {
            var bundle = ContentFixture.CreateValid();

            Assert.That(bundle.Flow.steps.Length, Is.EqualTo(15));
            Assert.That(bundle.Flow.steps[0].id, Is.EqualTo("frame_module"));
            Assert.That(bundle.Flow.steps[14].id, Is.EqualTo("release"));
        }

        [Test]
        public void ProductionContentPassesEveryContract()
        {
            var issues = ContentValidator.Validate(ContentFixture.LoadProduction());

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void InvalidQuestionAndStepFieldsReportStableIssueCodesInRuleOrder()
        {
            var bundle = ContentFixture.CreateValid();
            bundle.Questions[0].id = "";
            bundle.Questions[1].id = "q003";
            bundle.Questions[2].options = new[] { "A" };
            bundle.Questions[3].correctOptionIndex = 4;
            bundle.Flow.steps[0].order = 2;
            bundle.Flow.steps[1].id = "frame_module";
            bundle.Flow.steps[2].assetKey = "";
            bundle.Flow.steps[2].dropTargetId = "";

            var issues = ContentValidator.Validate(bundle);

            Assert.That(issues, Is.EqualTo(new[]
            {
                "question_id_missing:0",
                "question_id_duplicate:q003",
                "question_option_count:q003",
                "question_answer_out_of_range:q004",
                "step_order:frame_module",
                "step_id_duplicate:frame_module",
                "step_question_missing:frame_module:q001",
                "step_question_missing:frame_module:q002",
                "asset_key_missing:wheelset_axlebox_b",
                "drop_target_missing:wheelset_axlebox_b"
            }));
        }

        [Test]
        public void ProductionFlowFreezesAllStagesAndTheTeachingCommissioningLoop()
        {
            var bundle = ContentFixture.LoadProduction();

            Assert.That(bundle.Questions, Has.Length.EqualTo(48));
            Assert.That(bundle.Flow.schemaVersion, Is.EqualTo(1));
            Assert.That(bundle.Flow.contentVersion,
                Is.EqualTo("swm400e1-guided-flow-2026-08-v1"));
            Assert.That(bundle.Flow.failFirstCommissioning, Is.True);
            Assert.That(bundle.Flow.steps.Select(step => step.id), Is.EqualTo(new[]
            {
                "frame_module", "wheelset_axlebox_a", "wheelset_axlebox_b",
                "primary_suspension", "brake_module", "traction_drive_a",
                "traction_drive_b", "central_traction", "secondary_suspension",
                "height_damping", "sensor_module", "carbody_lowering",
                "commissioning", "inspection", "release"
            }));
            Assert.That(bundle.Flow.steps.Select(step => step.phase), Is.EqualTo(new[]
            {
                "bogie_assembly", "bogie_assembly", "bogie_assembly",
                "bogie_assembly", "bogie_assembly", "bogie_assembly",
                "bogie_assembly", "bogie_assembly", "bogie_assembly",
                "bogie_assembly", "bogie_assembly", "carbody_lowering",
                "commissioning", "inspection", "release"
            }));
            Assert.That(bundle.Flow.steps[14].assetKey, Is.EqualTo("module.release_card"));
            Assert.That(bundle.Flow.steps[14].dropTargetId, Is.EqualTo("target.release_board"));
        }

        [Test]
        public void TrueFalseQuestionRequiresExactlyTwoOptions()
        {
            var bundle = ContentFixture.CreateValid();
            bundle.Questions[40].options = new[] { "True", "False", "Maybe" };

            var issues = ContentValidator.Validate(bundle);

            Assert.That(issues, Does.Contain("question_option_count:q041"));
        }

        [Test]
        public void RepositoryRejectsBlankJsonWithDiagnosableException()
        {
            var exception = Assert.Throws<ContentLoadException>(() =>
                JsonContentRepository.Load(" ", "{}"));

            Assert.That(exception.Message, Is.EqualTo("question_json_blank"));
        }
    }
}
