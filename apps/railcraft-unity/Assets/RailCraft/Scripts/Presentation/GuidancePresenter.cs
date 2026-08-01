using RailCraft.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GuidancePresenter : MonoBehaviour
    {
        public const string RequiredCopy =
            "目标：完成 SWM-400E1 动力转向架的子系统级教学装配，并体验落车、调试、整改、检验和放行流程。\n" +
            "操作：回答当前知识准备题；全部答对后，按住鼠标左键拖动高亮模块到发光接口。\n" +
            "镜头：鼠标右键旋转视角，中键平移，滚轮缩放，WASD或方向键移动观察中心。\n" +
            "范围：流程和占位模型用于内部学习与方案演示，后续由团队工艺和模型成果替换。";

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text copyText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private GuidedFlowController flowController;
        [SerializeField] private MainMenuPresenter mainMenuPresenter;

        private bool informationOnly;
        private bool subscribed;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
        public string Copy => copyText == null ? string.Empty : copyText.text;

        public void ConfigureView(GameObject configuredPanelRoot, Text configuredCopy,
            Button configuredPrimary)
        {
            Unsubscribe();
            panelRoot = configuredPanelRoot;
            copyText = configuredCopy;
            primaryButton = configuredPrimary;
            if (copyText != null)
                copyText.text = RequiredCopy;
            if (isActiveAndEnabled)
                Subscribe();
        }

        public void Bind(GuidedFlowController configuredController,
            MainMenuPresenter configuredMainMenu)
        {
            flowController = configuredController;
            mainMenuPresenter = configuredMainMenu;
        }

        public void ShowForRun()
        {
            informationOnly = false;
            SetPrimaryLabel("开始装配");
            if (copyText != null)
                copyText.text = RequiredCopy;
            panelRoot?.SetActive(true);
        }

        public void ShowForInformation()
        {
            informationOnly = true;
            SetPrimaryLabel("返回主菜单");
            if (copyText != null)
                copyText.text = RequiredCopy;
            panelRoot?.SetActive(true);
        }

        public void Hide()
        {
            panelRoot?.SetActive(false);
        }

        private void HandlePrimaryAction()
        {
            if (informationOnly)
            {
                Hide();
                mainMenuPresenter?.Show();
                return;
            }

            if (flowController == null || flowController.Snapshot.Phase != FlowPhase.Guidance)
                return;
            flowController.ConfirmGuidance();
            if (flowController.Snapshot.Phase != FlowPhase.Guidance)
                Hide();
        }

        private void SetPrimaryLabel(string label)
        {
            if (primaryButton == null)
                return;
            var text = primaryButton.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = label;
        }

        private void Subscribe()
        {
            if (subscribed)
                return;
            primaryButton?.onClick.AddListener(HandlePrimaryAction);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;
            primaryButton?.onClick.RemoveListener(HandlePrimaryAction);
            subscribed = false;
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
    }
}
