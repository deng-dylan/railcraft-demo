using System;
using System.Collections.Generic;
using UnityEngine;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class HighlightController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Palette")]
        [SerializeField, ColorUsage(false, true)]
        private Color currentModuleColor = new Color(0.05f, 1.15f, 1.35f, 1f);
        [SerializeField, ColorUsage(false, true)]
        private Color currentTargetColor = new Color(1.35f, 0.68f, 0.08f, 1f);
        [SerializeField, ColorUsage(false, true)]
        private Color lockedFutureColor = new Color(0.42f, 0.45f, 0.48f, 1f);

        [Header("Pulse")]
        [SerializeField, Range(0.05f, 1.5f)] private float pulseFrequency = 0.7f;
        [SerializeField, Range(0f, 1f)] private float pulseMinimum = 0.55f;
        [SerializeField, Range(0f, 1f)] private float pulseMaximum = 1f;
        [SerializeField, Range(0f, 1f)] private float baseColorTint = 0.72f;
        [SerializeField, Min(0f)] private float emissionIntensity = 1.35f;

        private readonly HashSet<Renderer> currentModuleRenderers = new HashSet<Renderer>();
        private readonly HashSet<Renderer> currentTargetRenderers = new HashSet<Renderer>();
        private readonly HashSet<Renderer> lockedFutureRenderers = new HashSet<Renderer>();
        private readonly Dictionary<Renderer, RendererState> states =
            new Dictionary<Renderer, RendererState>();
        private readonly List<Renderer> cleanup = new List<Renderer>();

        public Color CurrentModuleColor => currentModuleColor;
        public Color CurrentTargetColor => currentTargetColor;
        public Color LockedFutureColor => lockedFutureColor;
        public int CurrentModuleHighlightCount => CountLive(currentModuleRenderers);
        public int CurrentTargetHighlightCount => CountLive(currentTargetRenderers);
        public int LockedFutureHighlightCount => CountLive(lockedFutureRenderers);
        public int ActiveFlashCount
        {
            get
            {
                var count = 0;
                var now = Time.unscaledTime;
                foreach (var state in states.Values)
                {
                    if (state.Renderer != null && state.FlashUntil > now)
                        count++;
                }
                return count;
            }
        }

        private void Update()
        {
            ApplyAll(Time.unscaledTime);
        }

        private void OnDisable()
        {
            ClearTransientEffects(false);
            RestoreAll(false);
        }

        private void OnDestroy()
        {
            RestoreAll(true);
            currentModuleRenderers.Clear();
            currentTargetRenderers.Clear();
            lockedFutureRenderers.Clear();
        }

        private void OnValidate()
        {
            pulseFrequency = Mathf.Clamp(pulseFrequency, 0.05f, 1.5f);
            pulseMinimum = Mathf.Clamp01(pulseMinimum);
            pulseMaximum = Mathf.Clamp(pulseMaximum, pulseMinimum, 1f);
            baseColorTint = Mathf.Clamp01(baseColorTint);
            emissionIntensity = Mathf.Max(0f, emissionIntensity);
        }

        public void Configure(IEnumerable<Renderer> configuredCurrentModule,
            IEnumerable<Renderer> configuredCurrentTarget,
            IEnumerable<Renderer> configuredLockedFuture = null)
        {
            Clear();
            SetCurrentModule(configuredCurrentModule);
            SetCurrentTarget(configuredCurrentTarget);
            SetLockedFuture(configuredLockedFuture);
        }

        public void ConfigurePalette(Color moduleColor, Color targetColor, Color futureColor)
        {
            currentModuleColor = moduleColor;
            currentTargetColor = targetColor;
            lockedFutureColor = futureColor;
            ApplyAll(Time.unscaledTime);
        }

        public void SetCurrentModule(IEnumerable<Renderer> renderers)
        {
            ReplaceGroup(currentModuleRenderers, renderers);
        }

        public void SetCurrentModule(Transform root)
        {
            SetCurrentModule(ResolveRenderers(root));
        }

        public void SetCurrentTarget(IEnumerable<Renderer> renderers)
        {
            ReplaceGroup(currentTargetRenderers, renderers);
        }

        public void SetCurrentTarget(Transform root)
        {
            SetCurrentTarget(ResolveRenderers(root));
        }

        public void SetLockedFuture(IEnumerable<Renderer> renderers)
        {
            ReplaceGroup(lockedFutureRenderers, renderers);
        }

        public void SetLockedFuture(Transform root)
        {
            SetLockedFuture(ResolveRenderers(root));
        }

        public void ClearCurrentModule()
        {
            ReplaceGroup(currentModuleRenderers, null);
        }

        public void ClearCurrentTarget()
        {
            ReplaceGroup(currentTargetRenderers, null);
        }

        public void ClearLockedFuture()
        {
            ReplaceGroup(lockedFutureRenderers, null);
        }

        public bool IsHighlighted(Renderer renderer)
        {
            return renderer != null && (currentModuleRenderers.Contains(renderer)
                || currentTargetRenderers.Contains(renderer)
                || lockedFutureRenderers.Contains(renderer));
        }

        public bool IsFlashing(Renderer renderer)
        {
            return renderer != null && states.TryGetValue(renderer, out var state)
                && state.FlashUntil > Time.unscaledTime;
        }

        public void Flash(IEnumerable<Renderer> renderers, Color color, float duration)
        {
            if (renderers == null || duration <= 0f || !isActiveAndEnabled)
                return;

            var until = Time.unscaledTime + duration;
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                var state = EnsureState(renderer);
                state.FlashColor = color;
                state.FlashUntil = until;
                ApplyState(state, color, 1f);
            }
        }

        public void ClearTransientEffects()
        {
            ClearTransientEffects(true);
        }

        public void Clear()
        {
            RestoreAll(true);
            currentModuleRenderers.Clear();
            currentTargetRenderers.Clear();
            lockedFutureRenderers.Clear();
        }

        private void ApplyAll(float now)
        {
            if (!isActiveAndEnabled)
                return;

            cleanup.Clear();
            var pulse = EvaluatePulse(now);
            foreach (var entry in states)
            {
                var renderer = entry.Key;
                var state = entry.Value;
                if (renderer == null)
                {
                    cleanup.Add(renderer);
                    continue;
                }

                if (state.FlashUntil > now)
                {
                    ApplyState(state, state.FlashColor, 1f);
                    continue;
                }

                state.FlashUntil = float.NegativeInfinity;
                if (currentTargetRenderers.Contains(renderer))
                    ApplyState(state, currentTargetColor, pulse);
                else if (currentModuleRenderers.Contains(renderer))
                    ApplyState(state, currentModuleColor, pulse);
                else if (lockedFutureRenderers.Contains(renderer))
                    ApplyState(state, lockedFutureColor, 1f, false);
                else
                {
                    state.Restore();
                    cleanup.Add(renderer);
                }
            }

            foreach (var renderer in cleanup)
                states.Remove(renderer);
        }

        private float EvaluatePulse(float now)
        {
            var wave = 0.5f + 0.5f * Mathf.Sin(now * pulseFrequency * Mathf.PI * 2f);
            return Mathf.Lerp(pulseMinimum, pulseMaximum, wave);
        }

        private void ReplaceGroup(HashSet<Renderer> group, IEnumerable<Renderer> replacements)
        {
            var removed = new List<Renderer>(group);
            group.Clear();
            if (replacements != null)
            {
                foreach (var renderer in replacements)
                {
                    if (renderer == null || !group.Add(renderer))
                        continue;
                    EnsureState(renderer);
                }
            }

            foreach (var renderer in removed)
                ReleaseIfUnused(renderer);
            ApplyAll(Time.unscaledTime);
        }

        private RendererState EnsureState(Renderer renderer)
        {
            if (!states.TryGetValue(renderer, out var state))
            {
                state = new RendererState(renderer);
                states.Add(renderer, state);
            }
            return state;
        }

        private void ReleaseIfUnused(Renderer renderer)
        {
            if (renderer == null || currentModuleRenderers.Contains(renderer)
                || currentTargetRenderers.Contains(renderer) || lockedFutureRenderers.Contains(renderer))
                return;
            if (!states.TryGetValue(renderer, out var state)
                || state.FlashUntil > Time.unscaledTime)
                return;

            state.Restore();
            states.Remove(renderer);
        }

        private void ClearTransientEffects(bool reapplyPersistentState)
        {
            foreach (var state in states.Values)
                state.FlashUntil = float.NegativeInfinity;
            if (reapplyPersistentState)
                ApplyAll(Time.unscaledTime);
        }

        private void RestoreAll(bool clearStates)
        {
            foreach (var state in states.Values)
                state.Restore();
            if (clearStates)
                states.Clear();
        }

        private void ApplyState(RendererState state, Color color, float strength,
            bool emissionEnabled = true)
        {
            if (state.Renderer == null)
                return;

            var clampedStrength = Mathf.Clamp01(strength);
            foreach (var slot in state.Slots)
            {
                var tint = Color.Lerp(slot.BaseColor, color, baseColorTint * clampedStrength);
                tint.a = slot.BaseColor.a;
                var emission = emissionEnabled
                    ? color * (emissionIntensity * clampedStrength)
                    : Color.black;
                emission.a = color.a;
                slot.Apply(tint, emission);
            }
        }

        private static Renderer[] ResolveRenderers(Transform root)
        {
            return root == null ? Array.Empty<Renderer>() : root.GetComponentsInChildren<Renderer>(true);
        }

        private static int CountLive(IEnumerable<Renderer> renderers)
        {
            var count = 0;
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                    count++;
            }
            return count;
        }

        private sealed class RendererState
        {
            public Renderer Renderer { get; }
            public MaterialSlotState[] Slots { get; }
            public Color FlashColor { get; set; }
            public float FlashUntil { get; set; } = float.NegativeInfinity;

            public RendererState(Renderer renderer)
            {
                Renderer = renderer;
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    Slots = new[] { new MaterialSlotState(renderer, null, -1) };
                    return;
                }

                Slots = new MaterialSlotState[materials.Length];
                for (var index = 0; index < materials.Length; index++)
                    Slots[index] = new MaterialSlotState(renderer, materials[index], index);
            }

            public void Restore()
            {
                foreach (var slot in Slots)
                    slot.Restore();
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
            private readonly bool supportsEmission;

            public Color BaseColor { get; }

            public MaterialSlotState(Renderer renderer, Material material, int materialIndex)
            {
                this.renderer = renderer;
                this.materialIndex = materialIndex;
                original = new MaterialPropertyBlock();
                working = new MaterialPropertyBlock();
                GetPropertyBlock(original);
                hadOriginal = !original.isEmpty;
                if (hadOriginal || materialIndex < 0)
                    GetPropertyBlock(working);
                else
                    renderer.GetPropertyBlock(working);

                supportsBaseColor = material != null && material.HasProperty(BaseColorId);
                supportsLegacyColor = material != null && material.HasProperty(LegacyColorId);
                supportsEmission = material != null && material.HasProperty(EmissionColorId);
                BaseColor = ResolveBaseColor(material, hadOriginal ? original : working);
            }

            public void Apply(Color tint, Color emission)
            {
                if (renderer == null)
                    return;
                if (supportsBaseColor)
                    working.SetColor(BaseColorId, tint);
                if (supportsLegacyColor)
                    working.SetColor(LegacyColorId, tint);
                if (supportsEmission)
                    working.SetColor(EmissionColorId, emission);
                SetPropertyBlock(working);
            }

            public void Restore()
            {
                if (renderer == null)
                    return;
                SetPropertyBlock(hadOriginal ? original : null);
            }

            private void GetPropertyBlock(MaterialPropertyBlock block)
            {
                if (materialIndex < 0)
                    renderer.GetPropertyBlock(block);
                else
                    renderer.GetPropertyBlock(block, materialIndex);
            }

            private void SetPropertyBlock(MaterialPropertyBlock block)
            {
                if (materialIndex < 0)
                    renderer.SetPropertyBlock(block);
                else
                    renderer.SetPropertyBlock(block, materialIndex);
            }

            private static Color ResolveBaseColor(Material material, MaterialPropertyBlock block)
            {
                if (block.HasColor(BaseColorId))
                    return block.GetColor(BaseColorId);
                if (block.HasColor(LegacyColorId))
                    return block.GetColor(LegacyColorId);
                if (material != null && material.HasProperty(BaseColorId))
                    return material.GetColor(BaseColorId);
                if (material != null && material.HasProperty(LegacyColorId))
                    return material.GetColor(LegacyColorId);
                return Color.white;
            }
        }
    }
}
