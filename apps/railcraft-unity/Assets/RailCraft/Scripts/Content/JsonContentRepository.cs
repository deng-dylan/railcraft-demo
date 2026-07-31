using UnityEngine;

namespace RailCraft.Content
{
    public static class JsonContentRepository
    {
        public static ContentBundle Load(string questionJson, string flowJson)
        {
            if (string.IsNullOrWhiteSpace(questionJson))
                throw new ContentLoadException("question_json_blank");
            if (string.IsNullOrWhiteSpace(flowJson))
                throw new ContentLoadException("flow_json_blank");

            var questionFile = JsonUtility.FromJson<QuestionFile>(questionJson);
            if (questionFile == null || questionFile.questions == null)
                throw new ContentLoadException("question_json_invalid");

            var flow = JsonUtility.FromJson<FlowDefinition>(flowJson);
            if (flow == null || flow.steps == null)
                throw new ContentLoadException("flow_json_invalid");

            return new ContentBundle(questionFile.questions, flow);
        }

        [System.Serializable]
        private sealed class QuestionFile
        {
            public QuestionDefinition[] questions;
        }
    }
}
