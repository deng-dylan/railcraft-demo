using System;
using System.Collections.Generic;
using RailCraft.Interaction;
using UnityEngine;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SnapEffectController : MonoBehaviour
    {
        [SerializeField] private HighlightController highlightController;
        [SerializeField] private DragDropController dragDropController;
        [SerializeField, ColorUsage(false, true)]
        private Color successColor = new Color(0.12f, 1.35f, 0.28f, 1f);
        [SerializeField, ColorUsage(false, true)]
        private Color rejectedColor = new Color(1.4f, 0.08f, 0.05f, 1f);
        [SerializeField, Min(0f)] private float successDuration = 0.6f;
        [SerializeField, Min(0f)] private float rejectedDuration = 0.35f;

        private readonly List<Renderer> currentModuleRenderers = new List<Renderer>();
        private readonly List<Renderer> currentTargetRenderers = new List<Renderer>();
        private readonly List<Renderer> lockedFutureRenderers = new List<Renderer>();
        private readonly Dictionary<string, Renderer[]> moduleRenderersByStepId =
            new Dictionary<string, Renderer[]>(StringComparer.Ordinal);
        private readonly Dictionary<string, Renderer[]> targetRenderersByStepId =
            new Dictionary<string, Renderer[]>(StringComparer.Ordinal);

        private bool subscribed;
        private float successUntil = float.NegativeInfinity;
        private float rejectedUntil = float.NegativeInfinity;

        public float SuccessDuration => successDuration;
        public float RejectedDuration => rejectedDuration;
        public bool IsSuccessEffectActive => Time.unscaledTime < successUntil;
        public bool IsRejectedEffectActive => Time.unscaledTime < rejectedUntil;
        public int LockedFutureCount => CountLive(lockedFutureRenderers);

        private void OnEnable()
        {
            Subscribe();
            highlightController?.SetLockedFuture(lockedFutureRenderers);
        }

        private void OnDisable()
        {
            Unsubscribe();
            ResetRuntimeState();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ResetRuntimeState();
        }

        private void OnValidate()
        {
            successDuration = Mathf.Max(0f, successDuration);
            rejectedDuration = Mathf.Max(0f, rejectedDuration);
        }

        public void Configure(HighlightController configuredHighlightController,
            DragDropController configuredDragDropController)
        {
            Unsubscribe();
            highlightController = configuredHighlightController;
            dragDropController = configuredDragDropController;
            Subscribe();
            highlightController?.SetLockedFuture(lockedFutureRenderers);
        }

        public void ConfigureTimings(float configuredSuccessDuration, float configuredRejectedDuration)
        {
            successDuration = Mathf.Max(0f, configuredSuccessDuration);
            rejectedDuration = Mathf.Max(0f, configuredRejectedDuration);
        }

        public void ConfigurePalette(Color configuredSuccessColor, Color configuredRejectedColor)
        {
            successColor = configuredSuccessColor;
            rejectedColor = configuredRejectedColor;
        }

        public void SetCurrentStep(DraggableModule module, DropTarget target)
        {
            var stepId = module == null ? null : module.StepId;
            var moduleRoot = module == null ? null : module.VisualRoot;
            SetCurrentRenderers(stepId, ResolveRenderers(moduleRoot), ResolveRenderers(target?.transform));
        }

        public void SetLockedFuture(Transform root)
        {
            SetLockedFuture(ResolveRenderers(root));
        }

        public void SetLockedFuture(IEnumerable<Renderer> renderers)
        {
            ReplaceList(lockedFutureRenderers, renderers);
            highlightController?.SetLockedFuture(lockedFutureRenderers);
        }

        public void ClearLockedFuture()
        {
            lockedFutureRenderers.Clear();
            highlightController?.ClearLockedFuture();
        }

        public void SetCurrentRenderers(IEnumerable<Renderer> moduleRenderers,
            IEnumerable<Renderer> targetRenderers)
        {
            SetCurrentRenderers(null, moduleRenderers, targetRenderers);
        }

        public void SetCurrentRenderers(string stepId, IEnumerable<Renderer> moduleRenderers,
            IEnumerable<Renderer> targetRenderers)
        {
            ReplaceList(currentModuleRenderers, moduleRenderers);
            ReplaceList(currentTargetRenderers, targetRenderers);

            if (!string.IsNullOrWhiteSpace(stepId))
            {
                moduleRenderersByStepId[stepId] = currentModuleRenderers.ToArray();
                targetRenderersByStepId[stepId] = currentTargetRenderers.ToArray();
            }

            highlightController?.SetCurrentModule(currentModuleRenderers);
            highlightController?.SetCurrentTarget(currentTargetRenderers);
        }

        public void ClearCurrentStep()
        {
            currentModuleRenderers.Clear();
            currentTargetRenderers.Clear();
            highlightController?.ClearCurrentModule();
            highlightController?.ClearCurrentTarget();
        }

        public void PlaySuccess()
        {
            PlaySuccess(currentModuleRenderers);
            PlaySuccess(currentTargetRenderers);
        }

        public void PlaySuccess(IEnumerable<Renderer> renderers)
        {
            rejectedUntil = float.NegativeInfinity;
            successUntil = Mathf.Max(successUntil, Time.unscaledTime + successDuration);
            highlightController?.Flash(renderers, successColor, successDuration);
        }

        public void PlayRejected()
        {
            PlayRejected(currentModuleRenderers);
            PlayRejected(currentTargetRenderers);
        }

        public void PlayRejected(IEnumerable<Renderer> renderers)
        {
            successUntil = float.NegativeInfinity;
            rejectedUntil = Mathf.Max(rejectedUntil, Time.unscaledTime + rejectedDuration);
            highlightController?.Flash(renderers, rejectedColor, rejectedDuration);
        }

        public void Clear()
        {
            ClearCurrentStep();
            moduleRenderersByStepId.Clear();
            targetRenderersByStepId.Clear();
            successUntil = float.NegativeInfinity;
            rejectedUntil = float.NegativeInfinity;
            highlightController?.ClearTransientEffects();
            highlightController?.SetLockedFuture(lockedFutureRenderers);
        }

        private void ResetRuntimeState()
        {
            currentModuleRenderers.Clear();
            currentTargetRenderers.Clear();
            moduleRenderersByStepId.Clear();
            targetRenderersByStepId.Clear();
            successUntil = float.NegativeInfinity;
            rejectedUntil = float.NegativeInfinity;
            highlightController?.Clear();
        }

        private void HandleDropCompleted(string stepId)
        {
            if (!string.IsNullOrWhiteSpace(stepId)
                && moduleRenderersByStepId.TryGetValue(stepId, out var modules))
            {
                PlaySuccess(modules);
                if (targetRenderersByStepId.TryGetValue(stepId, out var targets))
                    PlaySuccess(targets);
                return;
            }

            PlaySuccess();
        }

        private void HandleDropRejected(DragDropResult result)
        {
            if (result != null && !string.IsNullOrWhiteSpace(result.StepId)
                && moduleRenderersByStepId.TryGetValue(result.StepId, out var modules))
                PlayRejected(modules);
            else
                PlayRejected(currentModuleRenderers);

            if (result?.Target != null)
                PlayRejected(ResolveRenderers(result.Target.transform));
            else
                PlayRejected(currentTargetRenderers);
        }

        private void Subscribe()
        {
            if (subscribed || dragDropController == null || !isActiveAndEnabled)
                return;
            dragDropController.DropCompleted += HandleDropCompleted;
            dragDropController.DropRejected += HandleDropRejected;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;
            if (dragDropController != null)
            {
                dragDropController.DropCompleted -= HandleDropCompleted;
                dragDropController.DropRejected -= HandleDropRejected;
            }
            subscribed = false;
        }

        private static Renderer[] ResolveRenderers(Transform root)
        {
            return root == null ? Array.Empty<Renderer>() : root.GetComponentsInChildren<Renderer>(true);
        }

        private static void ReplaceList(List<Renderer> destination, IEnumerable<Renderer> source)
        {
            destination.Clear();
            if (source == null)
                return;
            foreach (var renderer in source)
            {
                if (renderer != null && !destination.Contains(renderer))
                    destination.Add(renderer);
            }
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
    }
}
