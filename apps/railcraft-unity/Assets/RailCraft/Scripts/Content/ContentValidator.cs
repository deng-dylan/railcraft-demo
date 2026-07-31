using System;
using System.Collections.Generic;

namespace RailCraft.Content
{
    public static class ContentValidator
    {
        public static IReadOnlyList<string> Validate(ContentBundle bundle)
        {
            var issues = new List<string>();
            var questions = bundle?.Questions ?? Array.Empty<QuestionDefinition>();
            var flow = bundle?.Flow;
            var steps = flow?.steps ?? Array.Empty<StepDefinition>();

            ValidateQuestions(questions, issues);
            ValidateSteps(steps, issues);
            ValidateAssignments(questions, steps, issues);
            ValidateStepBindings(steps, issues);
            return issues;
        }

        private static void ValidateQuestions(
            QuestionDefinition[] questions,
            ICollection<string> issues)
        {
            if (questions.Length != 48)
                issues.Add("question_count");

            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < questions.Length; index++)
            {
                var question = questions[index];
                if (question == null || string.IsNullOrWhiteSpace(question.id))
                    issues.Add($"question_id_missing:{index}");
                else if (!knownIds.Add(question.id))
                    issues.Add($"question_id_duplicate:{question.id}");
            }

            for (var index = 0; index < questions.Length; index++)
            {
                var question = questions[index];
                if (question == null)
                    continue;

                var optionCount = question.options?.Length ?? 0;
                var expectedOptionCount = question.type == "true_false" ? 2 : -1;
                if ((expectedOptionCount == 2 && optionCount != 2)
                    || (expectedOptionCount != 2 && optionCount < 2))
                    issues.Add($"question_option_count:{QuestionLabel(question, index)}");

                if (question.correctOptionIndex < 0
                    || question.correctOptionIndex >= optionCount)
                    issues.Add($"question_answer_out_of_range:{QuestionLabel(question, index)}");
            }
        }

        private static void ValidateSteps(
            StepDefinition[] steps,
            ICollection<string> issues)
        {
            if (steps.Length != 15)
                issues.Add("step_count");

            for (var index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                if (step != null && step.order != index + 1)
                    issues.Add($"step_order:{StepLabel(step, index)}");
            }

            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                if (step == null || string.IsNullOrWhiteSpace(step.id))
                    issues.Add($"step_id_missing:{index}");
                else if (!knownIds.Add(step.id))
                    issues.Add($"step_id_duplicate:{step.id}");
            }

            for (var index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                if (step == null || step.questionIds == null || step.questionIds.Length == 0)
                    issues.Add($"step_without_questions:{StepLabel(step, index)}");
            }
        }

        private static void ValidateAssignments(
            QuestionDefinition[] questions,
            StepDefinition[] steps,
            ICollection<string> issues)
        {
            var questionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var question in questions)
            {
                if (question != null && !string.IsNullOrWhiteSpace(question.id))
                    questionIds.Add(question.id);
            }

            var assignments = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var step in steps)
            {
                if (step?.questionIds == null)
                    continue;

                foreach (var questionId in step.questionIds)
                {
                    if (string.IsNullOrWhiteSpace(questionId) || !questionIds.Contains(questionId))
                    {
                        issues.Add($"step_question_missing:{StepLabel(step, 0)}:{questionId}");
                        continue;
                    }

                    assignments.TryGetValue(questionId, out var assignmentCount);
                    assignments[questionId] = assignmentCount + 1;
                }
            }

            foreach (var assignment in assignments)
            {
                if (assignment.Value > 1)
                    issues.Add($"question_duplicate:{assignment.Key}");
            }

            foreach (var questionId in questionIds)
            {
                if (!assignments.ContainsKey(questionId))
                    issues.Add($"question_unassigned:{questionId}");
            }
        }

        private static void ValidateStepBindings(
            StepDefinition[] steps,
            ICollection<string> issues)
        {
            for (var index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                if (step == null)
                    continue;
                if (string.IsNullOrWhiteSpace(step.assetKey))
                    issues.Add($"asset_key_missing:{StepLabel(step, index)}");
                if (string.IsNullOrWhiteSpace(step.dropTargetId))
                    issues.Add($"drop_target_missing:{StepLabel(step, index)}");
            }
        }

        private static string QuestionLabel(QuestionDefinition question, int index)
        {
            return string.IsNullOrWhiteSpace(question.id) ? index.ToString() : question.id;
        }

        private static string StepLabel(StepDefinition step, int index)
        {
            return step == null || string.IsNullOrWhiteSpace(step.id)
                ? index.ToString()
                : step.id;
        }
    }
}
