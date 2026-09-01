using RailCraft.Content;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class StepHudView : MonoBehaviour
    {
        [SerializeField] private Text stageNameText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text knowledgeText;
        [SerializeField] private Text hintText;

        public string StageNameText => stageNameText == null ? string.Empty : stageNameText.text;
        public string ProgressText => progressText == null ? string.Empty : progressText.text;
        public string KnowledgeText => knowledgeText == null ? string.Empty : knowledgeText.text;
        public string HintText => hintText == null ? string.Empty : hintText.text;

        public void Configure(Text stage, Text progress, Text knowledge, Text hint)
        {
            stageNameText = stage;
            progressText = progress;
            knowledgeText = knowledge;
            hintText = hint;
        }

        public void Show(StepDefinition step, int completedStepCount, int answeredInStep,
            int questionCountForStep, bool secondCommissioning = false)
        {
            var stage = secondCommissioning ? "再次调试" : step?.displayName ?? "未开始";
            if (stageNameText != null)
                stageNameText.text = $"当前阶段：{stage}";
            if (progressText != null)
                progressText.text = $"装配进度：{Mathf.Clamp(completedStepCount, 0, 15)}/15";
            if (knowledgeText != null)
                knowledgeText.text = $"知识准备：{Mathf.Max(0, answeredInStep)}/{Mathf.Max(0, questionCountForStep)}";
            if (hintText != null)
                hintText.text = "操作提示：拖动高亮模块到发光接口";
            gameObject.SetActive(true);
        }
    }
}
