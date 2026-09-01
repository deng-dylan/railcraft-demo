using System;
using System.IO;
using RailCraft.Content;
using UnityEngine;

namespace RailCraft.Tests.EditMode.Fixtures
{
    internal static class ContentFixture
    {
        private static readonly string[] StepIds =
        {
            "frame_module",
            "wheelset_axlebox_a",
            "wheelset_axlebox_b",
            "primary_suspension",
            "brake_module",
            "traction_drive_a",
            "traction_drive_b",
            "central_traction",
            "secondary_suspension",
            "height_damping",
            "sensor_module",
            "carbody_lowering",
            "commissioning",
            "inspection",
            "release"
        };

        private static readonly int[] QuestionCounts =
        {
            4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 3
        };

        public static ContentBundle CreateValid()
        {
            var questions = new QuestionDefinition[48];
            for (var index = 0; index < questions.Length; index++)
            {
                var isTrueFalse = index >= 40;
                questions[index] = new QuestionDefinition
                {
                    id = $"q{index + 1:000}",
                    type = isTrueFalse ? "true_false" : "single_choice",
                    prompt = $"Question {index + 1}",
                    options = isTrueFalse
                        ? new[] { "True", "False" }
                        : new[] { "A", "B", "C", "D" },
                    correctOptionIndex = 0
                };
            }

            var steps = new StepDefinition[StepIds.Length];
            var questionIndex = 0;
            for (var index = 0; index < steps.Length; index++)
            {
                var questionIds = new string[QuestionCounts[index]];
                for (var question = 0; question < questionIds.Length; question++)
                    questionIds[question] = questions[questionIndex++].id;

                steps[index] = new StepDefinition
                {
                    id = StepIds[index],
                    order = index + 1,
                    displayName = $"Step {index + 1}",
                    phase = "bogie_assembly",
                    assetKey = $"module.{StepIds[index]}",
                    dropTargetId = $"target.{StepIds[index]}",
                    questionIds = questionIds
                };
            }

            return new ContentBundle(questions, new FlowDefinition
            {
                schemaVersion = 1,
                contentVersion = "fixture-v1",
                failFirstCommissioning = true,
                steps = steps
            });
        }

        public static ContentBundle LoadProduction()
        {
            var contentRoot = Path.Combine(Application.dataPath, "RailCraft/Content/V1");
            return JsonContentRepository.Load(
                File.ReadAllText(Path.Combine(contentRoot, "questions.v1.json")),
                File.ReadAllText(Path.Combine(contentRoot, "flow.v1.json")));
        }
    }
}
