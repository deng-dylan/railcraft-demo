using System;
using RailCraft.ThirdPerson.Domain;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Applies the selected plan to every assembly-stage visual currently in
    /// the scene. This keeps the existing station interaction graph intact and
    /// gives a plan a visible identity at answer, pickup, module, landing and
    /// commissioning stages while converted CAD meshes are being prepared.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AssemblyVariantGameplayPresentation : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private Transform gameplayRoot;

        private WhiteboxGameSessionHost subscribedHost;
        private MaterialPropertyBlock propertyBlock;

        public AssemblyVariantId ActiveVariant { get; private set; } = AssemblyVariantId.FuxingDemo;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            Transform configuredGameplayRoot)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost;
            gameplayRoot = configuredGameplayRoot;
            propertyBlock = new MaterialPropertyBlock();
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            ActiveVariant = sessionHost == null
                ? AssemblyVariantId.FuxingDemo
                : AssemblyVariantCatalog.Clamp(sessionHost.SelectedAssemblyVariant);
            if (gameplayRoot == null)
                return;

            var tint = VariantTint(ActiveVariant);
            foreach (var renderer in gameplayRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !IsAssemblyVisual(renderer.transform))
                    continue;

                if (ActiveVariant == AssemblyVariantId.FuxingDemo ||
                    PreservesImportedColors(renderer.transform, ActiveVariant))
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", tint);
                propertyBlock.SetColor("_BaseColor", tint);
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_EmissionColor"))
                    propertyBlock.SetColor("_EmissionColor", tint * 0.12f);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private static bool PreservesImportedColors(
            Transform transform,
            AssemblyVariantId variant)
        {
            if (variant != AssemblyVariantId.MetroSimplified &&
                variant != AssemblyVariantId.Y25Freight)
                return false;

            for (var current = transform; current != null; current = current.parent)
            {
                if (current.name.EndsWith("_ImportedSourceInstance", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void OnEnable()
        {
            Subscribe();
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
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

        private static bool IsAssemblyVisual(Transform transform)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                var name = current.name;
                if (name.IndexOf("Reward_", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Installed_", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("LandingInput_", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("DroppedVehicle", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("CompletedBogie", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("CarbodyReference", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("ReferenceModel", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Fallback", StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }

        private static Color VariantTint(AssemblyVariantId variant)
        {
            switch (variant)
            {
                case AssemblyVariantId.MetroSimplified:
                    return new Color(0.58f, 0.28f, 0.92f, 1f);
                case AssemblyVariantId.Y25Freight:
                    return new Color(0.92f, 0.58f, 0.12f, 1f);
                case AssemblyVariantId.TeachingConcept:
                    return new Color(0.18f, 0.82f, 0.48f, 1f);
                default:
                    return new Color(0.08f, 0.67f, 0.85f, 1f);
            }
        }
    }
}
