using System;
using RailCraft.ThirdPerson.Domain;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    /// <summary>
    /// Routes the selected playable plan to the visual reference placed in the
    /// live gameplay scene. Each root may contain an imported FBX/GLB or the
    /// generated fallback geometry. The interaction stations remain shared so
    /// every plan follows the same verified answer and assembly rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AssemblyVariantVisualRouter : MonoBehaviour
    {
        [SerializeField] private WhiteboxGameSessionHost sessionHost;
        [SerializeField] private GameObject[] variantRoots = Array.Empty<GameObject>();
        [SerializeField] private TextMesh worldLabel;
        [SerializeField] private Color fallbackTint = new Color(0.08f, 0.67f, 0.85f, 1f);

        private WhiteboxGameSessionHost subscribedHost;
        private MaterialPropertyBlock propertyBlock;

        public AssemblyVariantId ActiveVariant { get; private set; } = AssemblyVariantId.FuxingDemo;
        public GameObject ActiveVariantRoot { get; private set; }
        public bool HasVariantRoot => ActiveVariantRoot != null;

        public void Configure(
            WhiteboxGameSessionHost configuredSessionHost,
            GameObject[] configuredVariantRoots,
            TextMesh configuredWorldLabel = null)
        {
            Unsubscribe();
            sessionHost = configuredSessionHost;
            variantRoots = configuredVariantRoots == null
                ? Array.Empty<GameObject>()
                : (GameObject[])configuredVariantRoots.Clone();
            worldLabel = configuredWorldLabel;
            propertyBlock = new MaterialPropertyBlock();
            Subscribe();
            Refresh();
        }

        public void Refresh()
        {
            var selected = sessionHost == null
                ? AssemblyVariantId.FuxingDemo
                : AssemblyVariantCatalog.Clamp(sessionHost.SelectedAssemblyVariant);
            ActiveVariant = selected;

            var selectedIndex = (int)selected;
            ActiveVariantRoot = selectedIndex >= 0 && selectedIndex < variantRoots.Length
                ? variantRoots[selectedIndex]
                : FindFirstRoot();

            for (var index = 0; index < variantRoots.Length; index++)
            {
                var root = variantRoots[index];
                if (root != null)
                    root.SetActive(root == ActiveVariantRoot);
            }

            ApplyTint(ActiveVariantRoot, VariantTint(selected));
            if (worldLabel != null)
            {
                var definition = AssemblyVariantCatalog.Get(selected);
                worldLabel.text = $"当前玩法方案：{definition.DisplayName}\n{definition.AssetStatus}";
                worldLabel.color = VariantTint(selected);
            }
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

        private GameObject FindFirstRoot()
        {
            for (var index = 0; index < variantRoots.Length; index++)
            {
                if (variantRoots[index] != null)
                    return variantRoots[index];
            }

            return null;
        }

        private void ApplyTint(GameObject root, Color tint)
        {
            if (root == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (PreservesImportedColors(renderer.transform, ActiveVariant))
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

        private Color VariantTint(AssemblyVariantId variant)
        {
            switch (variant)
            {
                case AssemblyVariantId.MetroSimplified:
                    return new Color(0.58f, 0.28f, 0.92f, 1f);
                case AssemblyVariantId.Y25Freight:
                    return new Color(0.92f, 0.58f, 0.12f, 1f);
                case AssemblyVariantId.TeachingConcept:
                    return new Color(0.18f, 0.82f, 0.48f, 1f);
                case AssemblyVariantId.FuxingDemo:
                    return fallbackTint;
                default:
                    return fallbackTint;
            }
        }
    }
}
