using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FeedbackView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text messageText;

        private Coroutine hideRoutine;

        public string MessageText => messageText == null ? string.Empty : messageText.text;
        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        public void Configure(GameObject configuredPanelRoot, Text configuredMessageText)
        {
            panelRoot = configuredPanelRoot;
            messageText = configuredMessageText;
        }

        public void Show(string message, float duration = 0f)
        {
            if (hideRoutine != null)
                StopCoroutine(hideRoutine);
            if (messageText != null)
                messageText.text = message ?? string.Empty;
            if (panelRoot != null)
                panelRoot.SetActive(true);
            hideRoutine = duration > 0f ? StartCoroutine(HideAfter(duration)) : null;
        }

        public void Hide()
        {
            if (hideRoutine != null)
                StopCoroutine(hideRoutine);
            hideRoutine = null;
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            hideRoutine = null;
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}
