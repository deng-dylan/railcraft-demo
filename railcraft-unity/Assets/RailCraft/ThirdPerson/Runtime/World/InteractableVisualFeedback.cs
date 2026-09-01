using System;
using System.Collections.Generic;
using RailCraft.ThirdPerson.Player;
using UnityEngine;

namespace RailCraft.ThirdPerson.World
{
    public enum InteractionVisualState
    {
        Idle,
        Highlighted,
        Success,
        Failure
    }

    /// <summary>
    /// Drives one interactable's selection pulse and transient outcome feedback without
    /// instantiating materials. Original material property blocks are restored exactly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractableVisualFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        [Header("Bindings")]
        [SerializeField] private PlayerInteractionScanner scanner;
        [SerializeField] private MonoBehaviour targetInteractableBehaviour;
        [SerializeField] private Renderer[] targetRenderers = Array.Empty<Renderer>();

        [Header("Palette")]
        [SerializeField, ColorUsage(false, true)]
        private Color highlightColor = new Color(1.25f, 0.82f, 0.15f, 1f);
        [SerializeField, ColorUsage(false, true)]
        private Color successColor = new Color(0.16f, 1.15f, 0.38f, 1f);
        [SerializeField, ColorUsage(false, true)]
        private Color failureColor = new Color(1.25f, 0.18f, 0.12f, 1f);

        [Header("Pulse")]
        [SerializeField, Min(0f)] private float pulseFrequency = 1.35f;
        [SerializeField, Range(0f, 1f)] private float pulseMinimumBlend = 0.2f;
        [SerializeField, Range(0f, 1f)] private float pulseMaximumBlend = 0.65f;

        [Header("Outcome Feedback")]
        [SerializeField, Min(0f)] private float feedbackHoldDuration = 0.35f;
        [SerializeField, Min(0f)] private float feedbackFadeDuration = 0.35f;

        private readonly List<RendererState> rendererStates = new List<RendererState>();
        private bool subscribed;
        private bool highlighted;
        private InteractionVisualState transientState;
        private float pulseElapsed;
        private float feedbackElapsed;

        public InteractionVisualState State => transientState == InteractionVisualState.Success ||
            transientState == InteractionVisualState.Failure
                ? transientState
                : highlighted
                    ? InteractionVisualState.Highlighted
                    : InteractionVisualState.Idle;
        public bool IsHighlighted => highlighted;
        public bool IsShowingFeedback => transientState == InteractionVisualState.Success ||
            transientState == InteractionVisualState.Failure;

        /// <summary>
        /// Binds this visual driver to a scanner and one behaviour implementing
        /// <see cref="IPlayerInteractable"/>. Pass a null scanner/interactable to drive
        /// the component manually through <see cref="SetHighlighted"/>.
        /// </summary>
        public void Configure(
            PlayerInteractionScanner configuredScanner,
            MonoBehaviour configuredInteractable,
            IEnumerable<Renderer> configuredRenderers)
        {
            if (configuredInteractable != null &&
                !(configuredInteractable is IPlayerInteractable))
            {
                throw new ArgumentException(
                    "The configured behaviour must implement IPlayerInteractable.",
                    nameof(configuredInteractable));
            }

            Unsubscribe();
            RestoreRenderers();
            rendererStates.Clear();

            scanner = configuredScanner;
            targetInteractableBehaviour = configuredInteractable;
            targetRenderers = CopyRenderers(configuredRenderers);
            CaptureRenderers(targetRenderers);

            transientState = InteractionVisualState.Idle;
            feedbackElapsed = 0f;
            pulseElapsed = 0f;
            Subscribe();
            SetHighlighted(IsScannerTarget(scanner == null ? null : scanner.CurrentTarget));
        }

        public void ConfigurePalette(
            Color configuredHighlightColor,
            Color configuredSuccessColor,
            Color configuredFailureColor)
        {
            highlightColor = configuredHighlightColor;
            successColor = configuredSuccessColor;
            failureColor = configuredFailureColor;
            ApplyCurrentVisual();
        }

        public void ConfigureTimings(
            float configuredPulseFrequency,
            float configuredPulseMinimumBlend,
            float configuredPulseMaximumBlend,
            float configuredFeedbackHoldDuration,
            float configuredFeedbackFadeDuration)
        {
            pulseFrequency = Mathf.Max(0f, configuredPulseFrequency);
            pulseMinimumBlend = Mathf.Clamp01(configuredPulseMinimumBlend);
            pulseMaximumBlend = Mathf.Clamp(
                configuredPulseMaximumBlend,
                pulseMinimumBlend,
                1f);
            feedbackHoldDuration = Mathf.Max(0f, configuredFeedbackHoldDuration);
            feedbackFadeDuration = Mathf.Max(0f, configuredFeedbackFadeDuration);
            ApplyCurrentVisual();
        }

        public void SetHighlighted(bool shouldHighlight)
        {
            if (highlighted == shouldHighlight)
            {
                ApplyCurrentVisual();
                return;
            }

            highlighted = shouldHighlight;
            pulseElapsed = 0f;
            ApplyCurrentVisual();
        }

        public void ShowSuccess()
        {
            ShowFeedback(InteractionVisualState.Success);
        }

        public void ShowFailure()
        {
            ShowFeedback(InteractionVisualState.Failure);
        }

        public void ClearFeedback()
        {
            transientState = InteractionVisualState.Idle;
            feedbackElapsed = 0f;
            ApplyCurrentVisual();
        }

        /// <summary>
        /// Deterministic time step used by Update and EditMode tests.
        /// </summary>
        public void Advance(float unscaledDeltaTime)
        {
            var deltaTime = Mathf.Max(0f, unscaledDeltaTime);
            pulseElapsed += deltaTime;

            if (!IsShowingFeedback)
            {
                ApplyCurrentVisual();
                return;
            }

            feedbackElapsed += deltaTime;
            if (feedbackElapsed <= feedbackHoldDuration)
            {
                ApplyOverlay(GetFeedbackColor(), 1f);
                return;
            }

            if (feedbackFadeDuration <= 0f)
            {
                ClearFeedback();
                return;
            }

            var fadeProgress = (feedbackElapsed - feedbackHoldDuration) / feedbackFadeDuration;
            if (fadeProgress >= 1f)
            {
                ClearFeedback();
                return;
            }

            ApplyOverlay(GetFeedbackColor(), 1f - Mathf.Clamp01(fadeProgress));
        }

        private void Awake()
        {
            CaptureRenderers(targetRenderers);
        }

        private void OnEnable()
        {
            Subscribe();
            SetHighlighted(IsScannerTarget(scanner == null ? null : scanner.CurrentTarget));
        }

        private void OnDisable()
        {
            Unsubscribe();
            highlighted = false;
            transientState = InteractionVisualState.Idle;
            feedbackElapsed = 0f;
            RestoreRenderers();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RestoreRenderers();
        }

        private void OnValidate()
        {
            pulseFrequency = Mathf.Max(0f, pulseFrequency);
            pulseMinimumBlend = Mathf.Clamp01(pulseMinimumBlend);
            pulseMaximumBlend = Mathf.Clamp(pulseMaximumBlend, pulseMinimumBlend, 1f);
            feedbackHoldDuration = Mathf.Max(0f, feedbackHoldDuration);
            feedbackFadeDuration = Mathf.Max(0f, feedbackFadeDuration);
        }

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void ShowFeedback(InteractionVisualState requestedState)
        {
            transientState = requestedState;
            feedbackElapsed = 0f;
            ApplyOverlay(GetFeedbackColor(), 1f);
        }

        private void ApplyCurrentVisual()
        {
            if (IsShowingFeedback)
            {
                var blend = feedbackElapsed <= feedbackHoldDuration || feedbackFadeDuration <= 0f
                    ? 1f
                    : 1f - Mathf.Clamp01(
                        (feedbackElapsed - feedbackHoldDuration) / feedbackFadeDuration);
                ApplyOverlay(GetFeedbackColor(), blend);
                return;
            }

            if (!highlighted)
            {
                RestoreRenderers();
                return;
            }

            var wave = (Mathf.Sin(pulseElapsed * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            var blendAmount = Mathf.Lerp(pulseMinimumBlend, pulseMaximumBlend, wave);
            ApplyOverlay(highlightColor, blendAmount);
        }

        private Color GetFeedbackColor()
        {
            return transientState == InteractionVisualState.Success
                ? successColor
                : failureColor;
        }

        private void ApplyOverlay(Color overlay, float blend)
        {
            var clampedBlend = Mathf.Clamp01(blend);
            for (var index = 0; index < rendererStates.Count; index++)
                rendererStates[index].Apply(overlay, clampedBlend);
        }

        private void CaptureRenderers(IEnumerable<Renderer> renderers)
        {
            if (renderers == null || rendererStates.Count > 0)
                return;

            var uniqueRenderers = new HashSet<Renderer>();
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer == null || !uniqueRenderers.Add(targetRenderer))
                    continue;

                rendererStates.Add(new RendererState(targetRenderer));
            }
        }

        private void RestoreRenderers()
        {
            for (var index = 0; index < rendererStates.Count; index++)
                rendererStates[index].Restore();
        }

        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled || scanner == null)
                return;

            scanner.TargetChanged += HandleTargetChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (scanner != null)
                scanner.TargetChanged -= HandleTargetChanged;
            subscribed = false;
        }

        private void HandleTargetChanged(IPlayerInteractable target)
        {
            SetHighlighted(IsScannerTarget(target));
        }

        private bool IsScannerTarget(IPlayerInteractable target)
        {
            return targetInteractableBehaviour != null &&
                ReferenceEquals(target, targetInteractableBehaviour as IPlayerInteractable);
        }

        private static Renderer[] CopyRenderers(IEnumerable<Renderer> renderers)
        {
            if (renderers == null)
                return Array.Empty<Renderer>();

            var copy = new List<Renderer>();
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer != null && !copy.Contains(targetRenderer))
                    copy.Add(targetRenderer);
            }

            return copy.ToArray();
        }

        private sealed class RendererState
        {
            private readonly Renderer renderer;
            private readonly MaterialSlotState[] slots;

            public RendererState(Renderer configuredRenderer)
            {
                renderer = configuredRenderer;
                var materials = renderer.sharedMaterials;
                slots = new MaterialSlotState[materials.Length];
                for (var index = 0; index < materials.Length; index++)
                    slots[index] = new MaterialSlotState(renderer, materials[index], index);
            }

            public void Apply(Color overlay, float blend)
            {
                for (var index = 0; index < slots.Length; index++)
                    slots[index].Apply(overlay, blend);
            }

            public void Restore()
            {
                for (var index = 0; index < slots.Length; index++)
                    slots[index].Restore();
            }
        }

        private sealed class MaterialSlotState
        {
            private readonly Renderer renderer;
            private readonly int materialIndex;
            private readonly MaterialPropertyBlock original;
            private readonly MaterialPropertyBlock working;
            private readonly bool hadOriginal;
            private readonly bool supportsBaseColor;
            private readonly bool supportsLegacyColor;
            private readonly Color baseColor;

            public MaterialSlotState(Renderer configuredRenderer, Material material, int index)
            {
                renderer = configuredRenderer;
                materialIndex = index;
                original = new MaterialPropertyBlock();
                working = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original, materialIndex);
                renderer.GetPropertyBlock(working, materialIndex);
                hadOriginal = !original.isEmpty;
                supportsBaseColor = material != null && material.HasProperty(BaseColorId);
                supportsLegacyColor = material != null && material.HasProperty(LegacyColorId);
                baseColor = ResolveBaseColor(material, original);
            }

            public void Apply(Color overlay, float blend)
            {
                if (renderer == null)
                    return;

                var color = Color.Lerp(baseColor, overlay, blend);
                if (supportsBaseColor)
                    working.SetColor(BaseColorId, color);
                if (supportsLegacyColor)
                    working.SetColor(LegacyColorId, color);
                renderer.SetPropertyBlock(working, materialIndex);
            }

            public void Restore()
            {
                if (renderer != null)
                    renderer.SetPropertyBlock(hadOriginal ? original : null, materialIndex);
            }

            private static Color ResolveBaseColor(
                Material material,
                MaterialPropertyBlock propertyBlock)
            {
                if (propertyBlock.HasColor(BaseColorId))
                    return propertyBlock.GetColor(BaseColorId);
                if (propertyBlock.HasColor(LegacyColorId))
                    return propertyBlock.GetColor(LegacyColorId);
                if (material != null && material.HasProperty(BaseColorId))
                    return material.GetColor(BaseColorId);
                if (material != null && material.HasProperty(LegacyColorId))
                    return material.GetColor(LegacyColorId);
                return Color.white;
            }
        }
    }
}
