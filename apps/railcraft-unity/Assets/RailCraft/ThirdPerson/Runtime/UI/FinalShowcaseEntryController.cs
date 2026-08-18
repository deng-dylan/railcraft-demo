using System;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Guards the optional transition from the completed factory session to the
    /// full-train showcase. The action stays unavailable until both the session
    /// is complete and Unity reports that the showcase scene can be loaded.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FinalShowcaseEntryController : MonoBehaviour
    {
        public const string DefaultShowcaseSceneName = "FinalShowcase";

        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private WhiteboxSaveController saveController;
        [SerializeField] private GameObject completionRoot;
        [SerializeField] private Button showcaseButton;
        [SerializeField] private string showcaseSceneName = DefaultShowcaseSceneName;
        [SerializeField] private Key shortcutKey = Key.V;

        private Func<string, bool> sceneAvailability;
        private Action<string> sceneLoader;
        private WhiteboxGameSessionHost subscribedHost;
        private bool wired;

        public bool IsSceneAvailable { get; private set; }
        public bool CanEnterShowcase => HasCompletedSession && IsSceneAvailable;
        public string ShowcaseSceneName => EffectiveShowcaseSceneName;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            WhiteboxSaveController configuredSaveController,
            GameObject configuredCompletionRoot,
            Button configuredShowcaseButton,
            string configuredShowcaseSceneName = DefaultShowcaseSceneName,
            Func<string, bool> configuredSceneAvailability = null,
            Action<string> configuredSceneLoader = null)
        {
            Unwire();
            Unsubscribe();
            sessionHost = configuredSessionHost;
            saveController = configuredSaveController;
            completionRoot = configuredCompletionRoot;
            showcaseButton = configuredShowcaseButton;
            showcaseSceneName = string.IsNullOrWhiteSpace(configuredShowcaseSceneName)
                ? DefaultShowcaseSceneName
                : configuredShowcaseSceneName;
            sceneAvailability = configuredSceneAvailability;
            sceneLoader = configuredSceneLoader;
            Subscribe();
            Wire();
            RefreshAvailability();
        }

        public void RefreshAvailability()
        {
            IsSceneAvailable = CanLoadScene(EffectiveShowcaseSceneName);
            if (showcaseButton == null)
                return;

            // Hiding the optional action gives a clean settlement panel in a
            // checkout where the generated showcase has not been committed yet.
            showcaseButton.gameObject.SetActive(IsSceneAvailable);
            showcaseButton.interactable = CanEnterShowcase;
        }

        public bool TryEnterShowcase()
        {
            RefreshAvailability();
            if (!CanEnterShowcase)
                return false;

            try
            {
                saveController?.SaveCurrentSession();
                Time.timeScale = 1f;
                LoadScene(EffectiveShowcaseSceneName);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"RAILCRAFT_SHOWCASE_LOAD_REJECTED scene={EffectiveShowcaseSceneName};" +
                    $"reason={exception.Message}");
                RefreshAvailability();
                return false;
            }
        }

        private string EffectiveShowcaseSceneName => string.IsNullOrWhiteSpace(showcaseSceneName)
            ? DefaultShowcaseSceneName
            : showcaseSceneName;

        private bool HasCompletedSession => sessionHost != null
            ? sessionHost.Session.IsVehicleComplete
            : completionRoot != null && completionRoot.activeInHierarchy;

        private void OnEnable()
        {
            Subscribe();
            Wire();
            RefreshAvailability();
        }

        private void OnDisable()
        {
            Unwire();
            Unsubscribe();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[shortcutKey].wasPressedThisFrame)
                return;

            TryEnterShowcase();
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || sessionHost == null || subscribedHost == sessionHost)
                return;

            subscribedHost = sessionHost;
            subscribedHost.VehicleCompleted += RefreshAvailability;
            subscribedHost.SessionReset += RefreshAvailability;
        }

        private void Unsubscribe()
        {
            if (subscribedHost == null)
                return;

            subscribedHost.VehicleCompleted -= RefreshAvailability;
            subscribedHost.SessionReset -= RefreshAvailability;
            subscribedHost = null;
        }

        private void Wire()
        {
            if (!isActiveAndEnabled || showcaseButton == null || wired)
                return;

            showcaseButton.onClick.AddListener(HandleShowcaseClicked);
            wired = true;
        }

        private void Unwire()
        {
            if (showcaseButton != null && wired)
                showcaseButton.onClick.RemoveListener(HandleShowcaseClicked);
            wired = false;
        }

        private bool CanLoadScene(string sceneName)
        {
            return sceneAvailability != null
                ? sceneAvailability(sceneName)
                : Application.CanStreamedLevelBeLoaded(sceneName);
        }

        private void LoadScene(string sceneName)
        {
            if (sceneLoader != null)
            {
                sceneLoader(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private void HandleShowcaseClicked()
        {
            TryEnterShowcase();
        }
    }
}
