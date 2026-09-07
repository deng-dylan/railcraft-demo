using System.Collections.Generic;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Central non-spatial sound layer for the generated training UI. Imported
    /// clips stay optional so a source-only checkout can still rebuild and run.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class WhiteboxAudioPresenter : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private AudioClip repairClip;

        private readonly List<Button> wiredButtons = new List<Button>();
        private WhiteboxGameSessionHost subscribedHost;
        private AudioClip lastPlayedClip;
        private float lastPlayedAt = float.NegativeInfinity;

        public AudioClip ButtonClip => buttonClip;
        public AudioClip SuccessClip => successClip;
        public AudioClip FailureClip => failureClip;
        public AudioClip RepairClip => repairClip;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            Transform configuredButtonRoot,
            AudioClip configuredButtonClip,
            AudioClip configuredSuccessClip,
            AudioClip configuredFailureClip,
            AudioClip configuredRepairClip)
        {
            Unsubscribe();
            UnwireButtons();

            sessionHost = configuredSessionHost;
            buttonRoot = configuredButtonRoot;
            buttonClip = configuredButtonClip;
            successClip = configuredSuccessClip;
            failureClip = configuredFailureClip;
            repairClip = configuredRepairClip;

            EnsureAudioSource();
            Subscribe();
            WireButtons();
        }

        public void PlayButton()
        {
            Play(buttonClip);
        }

        public void PlaySuccess()
        {
            Play(successClip);
        }

        public void PlayFailure()
        {
            Play(failureClip);
        }

        public void PlayRepair()
        {
            Play(repairClip != null ? repairClip : successClip);
        }

        private void Awake()
        {
            EnsureAudioSource();
        }

        private void OnEnable()
        {
            Subscribe();
            WireButtons();
        }

        private void OnDisable()
        {
            Unsubscribe();
            UnwireButtons();
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || sessionHost == null || subscribedHost == sessionHost)
                return;

            subscribedHost = sessionHost;
            subscribedHost.AnswerEvaluated += HandleAnswerEvaluated;
            subscribedHost.FeedbackRequested += HandleFeedbackRequested;
            subscribedHost.MilestoneReached += HandleMilestoneReached;
            subscribedHost.VehicleCompleted += PlaySuccess;
        }

        private void Unsubscribe()
        {
            if (subscribedHost == null)
                return;

            subscribedHost.AnswerEvaluated -= HandleAnswerEvaluated;
            subscribedHost.FeedbackRequested -= HandleFeedbackRequested;
            subscribedHost.MilestoneReached -= HandleMilestoneReached;
            subscribedHost.VehicleCompleted -= PlaySuccess;
            subscribedHost = null;
        }

        private void WireButtons()
        {
            if (!isActiveAndEnabled || buttonRoot == null || wiredButtons.Count > 0)
                return;

            var buttons = buttonRoot.GetComponentsInChildren<Button>(true);
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                if (button == null)
                    continue;
                button.onClick.AddListener(PlayButton);
                wiredButtons.Add(button);
            }
        }

        private void UnwireButtons()
        {
            for (var index = 0; index < wiredButtons.Count; index++)
            {
                if (wiredButtons[index] != null)
                    wiredButtons[index].onClick.RemoveListener(PlayButton);
            }
            wiredButtons.Clear();
        }

        private void HandleAnswerEvaluated(WhiteboxAnswerEvaluatedEvent answerEvent)
        {
            if (answerEvent.Result.IsCorrect)
                PlaySuccess();
            else
                PlayFailure();
        }

        private void HandleFeedbackRequested(string message)
        {
            var outcome = WhiteboxInteractionFeedbackRouter.ClassifyFeedback(message);
            if (outcome == InteractionFeedbackOutcome.Success)
                PlaySuccess();
            else if (outcome == InteractionFeedbackOutcome.Failure)
                PlayFailure();
        }

        private void HandleMilestoneReached(WhiteboxMilestoneEvent milestone)
        {
            if (milestone.Kind != WhiteboxMilestoneKind.Commissioning)
            {
                PlaySuccess();
                return;
            }

            switch (milestone.CommissioningPhase)
            {
                case CommissioningPhase.NeedsRetuning:
                    PlayFailure();
                    break;
                case CommissioningPhase.ReadyForInspection:
                    PlayRepair();
                    break;
                default:
                    PlaySuccess();
                    break;
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip == null)
                return;
            EnsureAudioSource();

            var now = Time.unscaledTime;
            if (lastPlayedClip == clip && now - lastPlayedAt < 0.04f)
                return;

            lastPlayedClip = clip;
            lastPlayedAt = now;
            audioSource.PlayOneShot(clip);
        }
    }
}
