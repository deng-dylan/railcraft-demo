using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.FinalShowcase
{
    /// <summary>
    /// Keeps the generated showcase overlay synchronized with the presentation
    /// controller while remaining safe when either text field is omitted.
    /// </summary>
    [DefaultExecutionOrder(130)]
    [DisallowMultipleComponent]
    public sealed class FinalShowcaseHudPresenter : MonoBehaviour
    {
        [SerializeField] private FinalShowcaseRuntimeController controller;
        [SerializeField] private Text statusText;
        [SerializeField] private Text shortcutText;

        private FinalShowcaseRuntimeController subscribedController;

        public void Configure(
            FinalShowcaseRuntimeController configuredController,
            Text configuredStatusText,
            Text configuredShortcutText)
        {
            Unsubscribe();
            controller = configuredController;
            statusText = configuredStatusText;
            shortcutText = configuredShortcutText;
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            if (shortcutText != null)
                shortcutText.text = FinalShowcaseHudState.ShortcutHelp;

            if (statusText == null)
                return;

            statusText.text = controller == null
                ? "展示控制尚未绑定"
                : controller.HudState.StatusText;
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || controller == null || subscribedController == controller)
                return;

            subscribedController = controller;
            subscribedController.HudStateChanged += HandleHudStateChanged;
        }

        private void Unsubscribe()
        {
            if (subscribedController == null)
                return;

            subscribedController.HudStateChanged -= HandleHudStateChanged;
            subscribedController = null;
        }

        private void HandleHudStateChanged(FinalShowcaseHudState state)
        {
            if (statusText != null)
                statusText.text = state.StatusText;
            if (shortcutText != null)
                shortcutText.text = FinalShowcaseHudState.ShortcutHelp;
        }
    }
}
