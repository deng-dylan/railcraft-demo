using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Optional navigation adapter for the stand-alone showcase scene. Loading
    /// the factory scene returns to its existing main menu and saved session flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FinalShowcaseReturnController : MonoBehaviour
    {
        public const string DefaultFactorySceneName = "ThirdPersonWhitebox";

        [SerializeField] private Button returnButton;
        [SerializeField] private string factorySceneName = DefaultFactorySceneName;
        [SerializeField] private Key shortcutKey = Key.Escape;

        private Func<string, bool> sceneAvailability;
        private Action<string> sceneLoader;
        private bool wired;

        public bool CanReturnToFactory { get; private set; }
        public string FactorySceneName => EffectiveFactorySceneName;

        public void Configure(
            Button configuredReturnButton,
            string configuredFactorySceneName = DefaultFactorySceneName,
            Func<string, bool> configuredSceneAvailability = null,
            Action<string> configuredSceneLoader = null)
        {
            Unwire();
            returnButton = configuredReturnButton;
            factorySceneName = string.IsNullOrWhiteSpace(configuredFactorySceneName)
                ? DefaultFactorySceneName
                : configuredFactorySceneName;
            sceneAvailability = configuredSceneAvailability;
            sceneLoader = configuredSceneLoader;
            Wire();
            RefreshAvailability();
        }

        public void RefreshAvailability()
        {
            CanReturnToFactory = CanLoadScene(EffectiveFactorySceneName);
            if (returnButton == null)
                return;

            returnButton.gameObject.SetActive(CanReturnToFactory);
            returnButton.interactable = CanReturnToFactory;
        }

        public bool TryReturnToFactory()
        {
            RefreshAvailability();
            if (!CanReturnToFactory)
                return false;

            try
            {
                Time.timeScale = 1f;
                LoadScene(EffectiveFactorySceneName);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"RAILCRAFT_FACTORY_RETURN_REJECTED scene={EffectiveFactorySceneName};" +
                    $"reason={exception.Message}");
                RefreshAvailability();
                return false;
            }
        }

        private string EffectiveFactorySceneName => string.IsNullOrWhiteSpace(factorySceneName)
            ? DefaultFactorySceneName
            : factorySceneName;

        private void OnEnable()
        {
            Wire();
            RefreshAvailability();
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[shortcutKey].wasPressedThisFrame)
                return;

            TryReturnToFactory();
        }

        private void Wire()
        {
            if (!isActiveAndEnabled || returnButton == null || wired)
                return;

            returnButton.onClick.AddListener(HandleReturnClicked);
            wired = true;
        }

        private void Unwire()
        {
            if (returnButton != null && wired)
                returnButton.onClick.RemoveListener(HandleReturnClicked);
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

        private void HandleReturnClicked()
        {
            TryReturnToFactory();
        }
    }
}
