using System;
using System.Collections.Generic;
using RailCraft.Assets;
using RailCraft.Content;
using RailCraft.Interaction;
using UnityEngine;

namespace RailCraft.Presentation
{
    [DisallowMultipleComponent]
    public sealed class AssemblyPresenter : MonoBehaviour
    {
        private readonly List<GameObject> installedVisuals = new List<GameObject>();
        private readonly Dictionary<string, GameObject> installedByStepId =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, DropTarget> targetsById =
            new Dictionary<string, DropTarget>(StringComparer.Ordinal);

        private PartPrefabCatalog catalog;
        private Transform stagingRoot;
        private Transform installedRoot;
        private DragDropController dragController;
        private StepDefinition currentStep;
        private GameObject currentVisual;
        private DraggableModule currentModule;
        private DropTarget currentTarget;
        private Renderer currentTargetMarker;

        public int InstalledVisualCount => installedVisuals.Count;
        public DraggableModule CurrentModule => currentModule;
        public DropTarget CurrentTarget => currentTarget;
        public bool IsTargetHighlighted => currentTargetMarker != null && currentTargetMarker.enabled;

        public void Configure(PartPrefabCatalog configuredCatalog, Transform configuredStagingRoot,
            Transform configuredInstalledRoot, DragDropController configuredDragController)
        {
            catalog = configuredCatalog ?? throw new ArgumentNullException(nameof(configuredCatalog));
            stagingRoot = configuredStagingRoot ?? transform;
            installedRoot = configuredInstalledRoot ?? transform;
            dragController = configuredDragController;
        }

        public void ConfigureTargets(IEnumerable<DropTarget> configuredTargets)
        {
            targetsById.Clear();
            if (configuredTargets == null)
                return;

            foreach (var target in configuredTargets)
            {
                if (target == null || string.IsNullOrWhiteSpace(target.TargetId))
                    continue;
                targetsById[target.TargetId] = target;
            }
        }

        public DraggableModule PrepareStep(StepDefinition step)
        {
            if (catalog == null)
                throw new InvalidOperationException("AssemblyPresenter must be configured before use.");
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            if (currentStep != null && string.Equals(currentStep.id, step.id, StringComparison.Ordinal)
                && currentModule != null)
                return currentModule;

            DiscardCurrentVisual();
            currentTarget = targetsById.TryGetValue(step.dropTargetId, out var target) ? target : null;
            currentTargetMarker = ResolveMarker(currentTarget);
            var prefab = catalog.Resolve(step.assetKey);
            if (prefab == null)
                throw new InvalidOperationException($"Missing prefab for asset key: {step.assetKey}");

            currentStep = step;
            currentVisual = Instantiate(prefab, stagingRoot);
            currentVisual.name = $"active.{step.id}";
            currentVisual.SetActive(true);
            currentModule = currentVisual.GetComponentInChildren<DraggableModule>(true);
            if (currentModule == null)
                throw new InvalidOperationException($"Prefab has no DraggableModule: {step.assetKey}");

            currentModule.Configure(step.id, currentModule.InteractionCollider, currentModule.VisualRoot);
            currentModule.SetMotionConstraint(string.Equals(step.id, "carbody_lowering", StringComparison.Ordinal)
                ? DragMotionConstraint.Vertical
                : DragMotionConstraint.Free);
            if (currentModule.MotionConstraint == DragMotionConstraint.Vertical
                && currentTarget?.SnapAnchor != null)
            {
                currentVisual.transform.SetPositionAndRotation(
                    currentTarget.SnapAnchor.position + Vector3.up * 4f,
                    currentTarget.SnapAnchor.rotation);
            }
            SetInteractionEnabled(false);

            SetTargetHighlighted(true);
            dragController?.SetDraggableModules(new[] { currentModule });
            return currentModule;
        }

        public void UnlockStep(string stepId)
        {
            if (currentStep == null || !string.Equals(currentStep.id, stepId, StringComparison.Ordinal))
                return;
            SetInteractionEnabled(true);
        }

        public bool MarkCurrentInstalled(string stepId)
        {
            if (currentStep == null || currentVisual == null
                || !string.Equals(currentStep.id, stepId, StringComparison.Ordinal))
                return false;

            SetInteractionEnabled(false);
            SetTargetHighlighted(false);
            var moduleHighlight = ResolveNamedRenderer(currentVisual.transform, "Highlight");
            if (moduleHighlight != null)
                moduleHighlight.enabled = false;
            currentVisual.transform.SetParent(installedRoot, true);
            installedVisuals.Add(currentVisual);
            installedByStepId[stepId] = currentVisual;
            currentStep = null;
            currentVisual = null;
            currentModule = null;
            currentTarget = null;
            currentTargetMarker = null;
            dragController?.SetDraggableModules(Array.Empty<DraggableModule>());
            return true;
        }

        public GameObject GetInstalledVisual(string stepId)
        {
            return !string.IsNullOrWhiteSpace(stepId)
                && installedByStepId.TryGetValue(stepId, out var visual)
                ? visual
                : null;
        }

        public void SetInstalledHighlight(string stepId, bool highlighted)
        {
            var visual = GetInstalledVisual(stepId);
            var renderer = visual == null ? null : ResolveNamedRenderer(visual.transform, "Highlight");
            if (renderer != null)
                renderer.enabled = highlighted;
        }

        public bool IsInstalledHighlighted(string stepId)
        {
            var visual = GetInstalledVisual(stepId);
            var renderer = visual == null ? null : ResolveNamedRenderer(visual.transform, "Highlight");
            return renderer != null && renderer.enabled;
        }

        public void SetTargetHighlighted(bool highlighted)
        {
            if (currentTargetMarker != null)
                currentTargetMarker.enabled = highlighted;
        }

        public void Clear()
        {
            DiscardCurrentVisual();
            foreach (var visual in installedVisuals)
            {
                if (visual != null)
                    Destroy(visual);
            }
            installedVisuals.Clear();
            installedByStepId.Clear();
            dragController?.SetDraggableModules(Array.Empty<DraggableModule>());
        }

        private void SetInteractionEnabled(bool enabled)
        {
            if (currentModule?.InteractionCollider != null)
                currentModule.InteractionCollider.enabled = enabled;
        }

        private void DiscardCurrentVisual()
        {
            SetTargetHighlighted(false);
            if (currentVisual != null)
                Destroy(currentVisual);
            currentStep = null;
            currentVisual = null;
            currentModule = null;
            currentTarget = null;
            currentTargetMarker = null;
        }

        private static Renderer ResolveMarker(DropTarget target)
        {
            if (target == null)
                return null;
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.gameObject.name == "TargetMarker")
                    return renderer;
            }
            return null;
        }

        private static Renderer ResolveNamedRenderer(Transform root, string objectName)
        {
            if (root == null)
                return null;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.gameObject.name == objectName)
                    return renderer;
            }
            return null;
        }
    }
}
