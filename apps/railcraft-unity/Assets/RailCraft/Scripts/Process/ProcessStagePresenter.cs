using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Process
{
    [DisallowMultipleComponent]
    public sealed class ProcessStagePresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject inspectionMarker;
        [SerializeField] private GameObject passIndicator;
        [SerializeField] private Button primaryActionButton;
        [SerializeField] private float inspectionPulseAmplitude = 0.1f;
        [SerializeField] private float inspectionPulseCyclesPerSecond = 1.8f;

        private Coroutine inspectionPulse;
        private Vector3 inspectionMarkerBaseScale = Vector3.one;

        public event Action ReworkAcknowledged;
        public event Action SecondCommissioningCompleted;

        public string Message { get; private set; } = string.Empty;
        public bool InspectionMarkerVisible => inspectionMarker != null && inspectionMarker.activeSelf;
        public bool PassIndicatorVisible => passIndicator != null && passIndicator.activeSelf;
        public Vector3 InspectionMarkerScale => inspectionMarker == null
            ? Vector3.zero
            : inspectionMarker.transform.localScale;

        public void Configure(GameObject configuredPanelRoot, Text configuredMessageText,
            GameObject configuredInspectionMarker, GameObject configuredPassIndicator,
            Button configuredPrimaryActionButton)
        {
            if (primaryActionButton != null)
                primaryActionButton.onClick.RemoveListener(HandlePrimaryAction);

            panelRoot = configuredPanelRoot;
            messageText = configuredMessageText;
            inspectionMarker = configuredInspectionMarker;
            inspectionMarkerBaseScale = inspectionMarker == null
                ? Vector3.one
                : inspectionMarker.transform.localScale;
            passIndicator = configuredPassIndicator;
            primaryActionButton = configuredPrimaryActionButton;
            if (primaryActionButton != null)
                primaryActionButton.onClick.AddListener(HandlePrimaryAction);
            ResetView();
        }

        public void ShowTeachingAnomaly()
        {
            SetMessage(TeachingOutcomeProvider.TeachingAnomalyMessage);
            SetInspectionMarker(true);
            SetPassIndicator(false);
            SetActionVisible(true, TeachingOutcomeProvider.EnterReworkMessage);
        }

        public void ShowInspection()
        {
            SetMessage(TeachingOutcomeProvider.EnterReworkMessage);
            SetInspectionMarker(true);
            SetPassIndicator(false);
            SetActionVisible(false, string.Empty);
        }

        public void ShowInspectionCompleted()
        {
            SetMessage(TeachingOutcomeProvider.InspectionCompleteMessage);
            SetInspectionMarker(false);
            SetPassIndicator(false);
            SetActionVisible(true, "再次调试");
        }

        public void ShowSecondCommissioningPassed()
        {
            SetMessage(TeachingOutcomeProvider.SecondCommissioningPassedMessage);
            SetInspectionMarker(false);
            SetPassIndicator(true);
            SetActionVisible(false, string.Empty);
        }

        public void RequestReworkAcknowledgement()
        {
            ReworkAcknowledged?.Invoke();
        }

        public void RequestSecondCommissioningCompletion()
        {
            SecondCommissioningCompleted?.Invoke();
        }

        public void ResetView()
        {
            Message = string.Empty;
            if (messageText != null)
                messageText.text = string.Empty;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            SetInspectionMarker(false);
            SetPassIndicator(false);
            SetActionVisible(false, string.Empty);
        }

        private void SetMessage(string message)
        {
            Message = message ?? string.Empty;
            if (messageText != null)
                messageText.text = Message;
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private void SetInspectionMarker(bool visible)
        {
            if (inspectionMarker == null)
                return;
            inspectionMarker.SetActive(visible);
            if (!visible)
            {
                StopInspectionPulse();
                return;
            }
            if (inspectionPulse == null && isActiveAndEnabled)
                inspectionPulse = StartCoroutine(PulseInspectionMarker());
        }

        private void SetPassIndicator(bool visible)
        {
            if (passIndicator != null)
                passIndicator.SetActive(visible);
        }

        private void SetActionVisible(bool visible, string label)
        {
            if (primaryActionButton != null)
            {
                primaryActionButton.gameObject.SetActive(visible);
                var text = primaryActionButton.GetComponentInChildren<Text>(true);
                if (text != null && !string.IsNullOrEmpty(label))
                    text.text = label;
            }
        }

        private void Awake()
        {
            if (inspectionMarker != null)
                inspectionMarkerBaseScale = inspectionMarker.transform.localScale;
            if (primaryActionButton != null)
            {
                primaryActionButton.onClick.RemoveListener(HandlePrimaryAction);
                primaryActionButton.onClick.AddListener(HandlePrimaryAction);
            }
        }

        private void OnEnable()
        {
            if (inspectionMarker != null && inspectionMarker.activeSelf && inspectionPulse == null)
                inspectionPulse = StartCoroutine(PulseInspectionMarker());
        }

        private void OnDisable()
        {
            StopInspectionPulse();
        }

        private IEnumerator PulseInspectionMarker()
        {
            var elapsed = 0f;
            while (inspectionMarker != null && inspectionMarker.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                var wave = Mathf.Sin(elapsed * Mathf.PI * 2f
                    * Mathf.Max(0.1f, inspectionPulseCyclesPerSecond));
                var factor = 1f + wave * Mathf.Clamp(inspectionPulseAmplitude, 0f, 0.3f);
                inspectionMarker.transform.localScale = inspectionMarkerBaseScale * factor;
                yield return null;
            }
            inspectionPulse = null;
        }

        private void StopInspectionPulse()
        {
            if (inspectionPulse != null)
                StopCoroutine(inspectionPulse);
            inspectionPulse = null;
            if (inspectionMarker != null)
                inspectionMarker.transform.localScale = inspectionMarkerBaseScale;
        }

        private void HandlePrimaryAction()
        {
            if (string.Equals(Message, TeachingOutcomeProvider.TeachingAnomalyMessage,
                    StringComparison.Ordinal))
            {
                RequestReworkAcknowledgement();
                return;
            }

            if (string.Equals(Message, TeachingOutcomeProvider.InspectionCompleteMessage,
                    StringComparison.Ordinal))
                RequestSecondCommissioningCompletion();
        }

        private void OnDestroy()
        {
            StopInspectionPulse();
            if (primaryActionButton != null)
                primaryActionButton.onClick.RemoveListener(HandlePrimaryAction);
        }
    }
}
