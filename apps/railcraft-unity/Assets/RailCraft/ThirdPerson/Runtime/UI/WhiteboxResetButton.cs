using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    [DisallowMultipleComponent]
    public sealed class WhiteboxResetButton : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private Button resetButton;

        public void Configure(WhiteboxGameSessionHost configuredSessionHost, Button configuredResetButton)
        {
            Unwire();
            sessionHost = configuredSessionHost;
            resetButton = configuredResetButton;
            Wire();
        }

        private void OnEnable()
        {
            Wire();
        }

        private void OnDisable()
        {
            Unwire();
        }

        private void Wire()
        {
            if (!isActiveAndEnabled || resetButton == null)
                return;
            resetButton.onClick.RemoveListener(HandleResetClicked);
            resetButton.onClick.AddListener(HandleResetClicked);
        }

        private void Unwire()
        {
            if (resetButton != null)
                resetButton.onClick.RemoveListener(HandleResetClicked);
        }

        private void HandleResetClicked()
        {
            sessionHost?.ResetSession();
        }
    }
}
