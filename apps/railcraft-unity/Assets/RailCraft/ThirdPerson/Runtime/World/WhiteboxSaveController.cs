using System;
using RailCraft.ThirdPerson.Domain;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    [DisallowMultipleComponent]
    public sealed class WhiteboxSaveController : MonoBehaviour
    {
        public const string DefaultSaveKey = "railcraft.whitebox.session.v2";

        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private string saveKey = DefaultSaveKey;
        [SerializeField] private bool autoSave = true;

        private WhiteboxGameSessionHost subscribedHost;
        private bool activeSession;
        private bool applicationPauseOwnsTimingPause;

        public event Action<bool> SaveAvailabilityChanged;

        public bool HasSave => PlayerPrefs.HasKey(EffectiveSaveKey);
        public bool HasActiveSession => activeSession;
        public string EffectiveSaveKey => string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            string configuredSaveKey = null,
            bool shouldAutoSave = true)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost ?? throw new ArgumentNullException(nameof(configuredSessionHost));
            if (!string.IsNullOrWhiteSpace(configuredSaveKey))
                saveKey = configuredSaveKey;
            autoSave = shouldAutoSave;
            Subscribe();
            SaveAvailabilityChanged?.Invoke(HasSave);
        }

        public void StartNewGame()
        {
            activeSession = true;
            DeleteStoredSave();
            sessionHost?.ResetSession();
            SaveCurrentSession();
        }

        public bool TryContinueGame()
        {
            if (sessionHost == null || !HasSave)
                return false;

            try
            {
                var json = PlayerPrefs.GetString(EffectiveSaveKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                    throw new InvalidOperationException("存档内容为空");

                var snapshot = JsonUtility.FromJson<WhiteboxGameSessionSnapshot>(json);
                if (snapshot == null)
                    throw new InvalidOperationException("存档无法解析");

                sessionHost.RestoreSession(snapshot);
                sessionHost.Session.ResumeTiming();
                activeSession = true;
                SaveCurrentSession();
                SaveAvailabilityChanged?.Invoke(true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"RAILCRAFT_SAVE_REJECTED {exception.Message}");
                activeSession = false;
                DeleteStoredSave();
                sessionHost.NotifyFeedback("存档已损坏，已清理，请开始新游戏");
                return false;
            }
        }

        public void SaveCurrentSession()
        {
            if (sessionHost == null || !activeSession)
                return;

            var snapshot = sessionHost.Session.ExportSnapshot();
            var json = JsonUtility.ToJson(snapshot);
            PlayerPrefs.SetString(EffectiveSaveKey, json);
            PlayerPrefs.Save();
            SaveAvailabilityChanged?.Invoke(true);
        }

        public void ClearSave()
        {
            activeSession = false;
            DeleteStoredSave();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                var session = sessionHost?.Session;
                if (!applicationPauseOwnsTimingPause &&
                    session != null &&
                    session.FlowStatus == AssemblyFlowStatus.InProgress &&
                    !session.IsTimingPaused)
                {
                    session.PauseTiming();
                    applicationPauseOwnsTimingPause = true;
                }
                SaveCurrentSession();
                return;
            }

            if (applicationPauseOwnsTimingPause)
                sessionHost?.Session.ResumeTiming();
            applicationPauseOwnsTimingPause = false;
            SaveCurrentSession();
        }

        private void OnApplicationQuit()
        {
            sessionHost?.Session.PauseTiming();
            SaveCurrentSession();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || sessionHost == null || subscribedHost == sessionHost)
                return;

            subscribedHost = sessionHost;
            subscribedHost.StateChanged += HandleStateChanged;
        }

        private void Unsubscribe()
        {
            if (subscribedHost == null)
                return;

            subscribedHost.StateChanged -= HandleStateChanged;
            subscribedHost = null;
        }

        private void HandleStateChanged()
        {
            if (autoSave)
                SaveCurrentSession();
        }

        private void DeleteStoredSave()
        {
            if (PlayerPrefs.HasKey(EffectiveSaveKey))
            {
                PlayerPrefs.DeleteKey(EffectiveSaveKey);
                PlayerPrefs.Save();
            }
            SaveAvailabilityChanged?.Invoke(false);
        }
    }
}
