using System;
using System.Collections.Generic;
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

        // Some restricted Windows player environments do not expose a writable
        // registry hive to Unity's PlayerPrefs implementation. Keep a process-
        // local mirror so the whitebox can still be demonstrated and verified
        // during that run; persistent PlayerPrefs remains the first choice when
        // it is available.
        private static readonly Dictionary<string, string> volatileSaves =
            new Dictionary<string, string>(StringComparer.Ordinal);
        // A failed delete must hide the older persistent value for the rest of
        // the process.  Writes still retry PlayerPrefs so a transient failure
        // can recover on the next autosave.
        private static readonly HashSet<string> volatileDeletedKeys =
            new HashSet<string>(StringComparer.Ordinal);
        private static bool fallbackWarningLogged;

        public event Action<bool> SaveAvailabilityChanged;

        public bool HasSave => HasStoredSave(EffectiveSaveKey);
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
            StartNewGame(sessionHost == null
                ? AssemblyVariantId.FuxingDemo
                : sessionHost.SelectedAssemblyVariant);
        }

        public void StartNewGame(AssemblyVariantId variant)
        {
            activeSession = true;
            DeleteStoredSave();
            sessionHost?.SelectAssemblyVariant(variant);
            sessionHost?.ResetSession();
            SaveCurrentSession();
        }

        public bool TryContinueGame()
        {
            if (sessionHost == null || !HasSave)
                return false;

            try
            {
                var json = ReadStoredSave(EffectiveSaveKey);
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
            catch (SaveStorageUnavailableException exception)
            {
                Debug.LogWarning($"RAILCRAFT_SAVE_READ_UNAVAILABLE {exception.Message}");
                activeSession = false;
                sessionHost.NotifyFeedback("存档暂时无法读取，请稍后重试");
                return false;
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

            var snapshot = sessionHost.ExportSnapshot();
            var json = JsonUtility.ToJson(snapshot);
            WriteStoredSave(EffectiveSaveKey, json);
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
            if (!activeSession)
                RetryPendingDelete(EffectiveSaveKey);
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
            var key = EffectiveSaveKey;
            volatileSaves.Remove(key);
            volatileDeletedKeys.Add(key);
            RetryPendingDelete(key);
            SaveAvailabilityChanged?.Invoke(false);
        }

        private static bool HasStoredSave(string key)
        {
            // The volatile value is the newest value when a previous write failed.
            // Check it first so an older readable PlayerPrefs value cannot win.
            if (volatileSaves.ContainsKey(key))
                return true;
            if (volatileDeletedKeys.Contains(key))
                return false;

            try
            {
                if (PlayerPrefs.HasKey(key))
                    return true;
            }
            catch (Exception exception)
            {
                MarkPlayerPrefsFallback(key, "read", exception);
            }

            return false;
        }

        private static string ReadStoredSave(string key)
        {
            if (volatileSaves.TryGetValue(key, out var volatileJson))
                return volatileJson;
            if (volatileDeletedKeys.Contains(key))
                return string.Empty;

            try
            {
                if (PlayerPrefs.HasKey(key))
                    return PlayerPrefs.GetString(key, string.Empty);
            }
            catch (Exception exception)
            {
                MarkPlayerPrefsFallback(key, "read", exception);
                throw new SaveStorageUnavailableException(
                    $"PlayerPrefs could not read key '{key}'.",
                    exception);
            }

            return string.Empty;
        }

        private static void WriteStoredSave(string key, string json)
        {
            // A new session supersedes any process-local delete tombstone.
            // Always retry PlayerPrefs: the failure may have been temporary.
            volatileDeletedKeys.Remove(key);

            try
            {
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
                // Keep no stale in-memory copy once the persistent path works.
                volatileSaves.Remove(key);
                return;
            }
            catch (Exception exception)
            {
                volatileSaves[key] = json;
                MarkPlayerPrefsFallback(key, "write", exception);
            }
        }

        private static void RetryPendingDelete(string key)
        {
            if (!volatileDeletedKeys.Contains(key))
                return;

            try
            {
                if (PlayerPrefs.HasKey(key))
                    PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                volatileDeletedKeys.Remove(key);
            }
            catch (Exception exception)
            {
                MarkPlayerPrefsFallback(key, "delete", exception);
            }
        }

        private static void MarkPlayerPrefsFallback(string key, string operation, Exception exception)
        {
            if (fallbackWarningLogged)
                return;

            fallbackWarningLogged = true;
            Debug.LogWarning(
                $"RAILCRAFT_SAVE_PLAYERPREFS_FALLBACK key={key} operation={operation}; " +
                $"using resilient process-local fallback state. {exception.Message}");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetVolatileStorage()
        {
            volatileSaves.Clear();
            volatileDeletedKeys.Clear();
            fallbackWarningLogged = false;
        }

        private sealed class SaveStorageUnavailableException : Exception
        {
            public SaveStorageUnavailableException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }
    }
}
