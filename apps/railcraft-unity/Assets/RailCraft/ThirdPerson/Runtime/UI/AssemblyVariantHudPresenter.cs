using System;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Keeps the selected assembly plan visible while the player moves through
    /// the existing gameplay loop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AssemblyVariantHudPresenter : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private Text targetText;

        private WhiteboxGameSessionHost subscribedHost;

        public string DisplayedText => targetText == null ? string.Empty : targetText.text;

        public void Configure(WhiteboxGameSessionHost configuredSessionHost, Text configuredTargetText)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost;
            targetText = configuredTargetText;
            Subscribe();
            Refresh();
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
            if (!isActiveAndEnabled || sessionHost == null || subscribedHost == sessionHost)
                return;
            subscribedHost = sessionHost;
            subscribedHost.AssemblyVariantChanged += HandleVariantChanged;
            subscribedHost.SessionReset += Refresh;
        }

        private void Unsubscribe()
        {
            if (subscribedHost == null)
                return;
            subscribedHost.AssemblyVariantChanged -= HandleVariantChanged;
            subscribedHost.SessionReset -= Refresh;
            subscribedHost = null;
        }

        private void HandleVariantChanged(AssemblyVariantId variant)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (targetText == null)
                return;

            var definition = sessionHost == null
                ? AssemblyVariantCatalog.Get(AssemblyVariantId.FuxingDemo)
                : sessionHost.SelectedAssemblyVariantDefinition;
            targetText.text = $"方案：{definition.DisplayName}  ·  {definition.AssetStatus}";
        }
    }
}
