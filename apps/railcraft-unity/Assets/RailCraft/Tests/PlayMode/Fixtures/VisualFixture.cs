using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailCraft.Assets;
using RailCraft.Content;
using RailCraft.Flow;
using RailCraft.Interaction;
using RailCraft.Presentation;
using RailCraft.Process;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class VisualFixture : IDisposable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color TestMaterialBaseColor = new Color(0.18f, 0.22f, 0.26f, 1f);

        private readonly GameObject root;
        private readonly PartPrefabCatalog catalog;
        private readonly Material testMaterial;
        private readonly GuidedFlowController flow;
        private readonly AssemblyPresenter assembly;
        private readonly DragDropController drag;
        private readonly FeedbackView feedback;
        private readonly HighlightController highlight;
        private readonly SnapEffectController snapEffects;
        private readonly Renderer currentModuleRenderer;
        private readonly Renderer currentTargetRenderer;
        private readonly Renderer[] lockedFutureRenderers;
        private readonly Renderer[] unrelatedRenderers;
        private readonly DropTarget wrongTarget;

        private Renderer lastEffectModuleRenderer;
        private Renderer lastEffectTargetRenderer;

        private VisualFixture(GameObject root, PartPrefabCatalog catalog, Material testMaterial,
            GuidedFlowController flow, AssemblyPresenter assembly,
            DragDropController drag, FeedbackView feedback,
            HighlightController highlight, SnapEffectController snapEffects,
            Renderer currentModuleRenderer, Renderer currentTargetRenderer,
            Renderer[] lockedFutureRenderers, Renderer[] unrelatedRenderers,
            DropTarget wrongTarget)
        {
            this.root = root;
            this.catalog = catalog;
            this.testMaterial = testMaterial;
            this.flow = flow;
            this.assembly = assembly;
            this.drag = drag;
            this.feedback = feedback;
            this.highlight = highlight;
            this.snapEffects = snapEffects;
            this.currentModuleRenderer = currentModuleRenderer;
            this.currentTargetRenderer = currentTargetRenderer;
            this.lockedFutureRenderers = lockedFutureRenderers;
            this.unrelatedRenderers = unrelatedRenderers;
            this.wrongTarget = wrongTarget;
        }

        public bool CurrentModuleHighlight => highlight.IsHighlighted(currentModuleRenderer);
        public bool CurrentTargetHighlight => highlight.IsHighlighted(currentTargetRenderer);
        public int LockedFutureRendererCount => lockedFutureRenderers.Length;
        public int LockedFutureHighlightCount => lockedFutureRenderers.Count(highlight.IsHighlighted);
        public int UnrelatedHighlightCount => unrelatedRenderers.Count(highlight.IsHighlighted);
        public string FeedbackText => feedback.MessageText;
        public int CompletedStepCount => flow.CompletedUniqueSteps;

        public Color CurrentModuleBaseColor => ReadPropertyColor(currentModuleRenderer, BaseColorId);
        public Color CurrentModuleEmissionColor => ReadPropertyColor(
            currentModuleRenderer, EmissionColorId);
        public Color CurrentTargetBaseColor => ReadPropertyColor(currentTargetRenderer, BaseColorId);
        public Color CurrentTargetEmissionColor => ReadPropertyColor(
            currentTargetRenderer, EmissionColorId);
        public Color[] LockedFutureBaseColors => lockedFutureRenderers
            .Select(renderer => ReadPropertyColor(renderer, BaseColorId)).ToArray();
        public Color[] LockedFutureEmissionColors => lockedFutureRenderers
            .Select(renderer => ReadPropertyColor(renderer, EmissionColorId)).ToArray();

        public bool IsSuccessEffectActive => snapEffects.IsSuccessEffectActive;
        public bool IsRejectedEffectActive => snapEffects.IsRejectedEffectActive;
        public float SuccessDuration => snapEffects.SuccessDuration;
        public float RejectedDuration => snapEffects.RejectedDuration;
        public bool LastEffectModuleIsFlashing => highlight.IsFlashing(lastEffectModuleRenderer);
        public Color LastEffectModuleEmissionColor => ReadPropertyColor(
            lastEffectModuleRenderer, EmissionColorId);
        public Color LastEffectTargetEmissionColor => ReadPropertyColor(
            lastEffectTargetRenderer, EmissionColorId);
        public int ActiveFlashCount => highlight.ActiveFlashCount;
        public int PersistentHighlightCount => highlight.CurrentModuleHighlightCount
            + highlight.CurrentTargetHighlightCount + highlight.LockedFutureHighlightCount;
        public bool AllObservedPropertyBlocksCleared => ObservedRenderers()
            .All(renderer => renderer == null || !renderer.HasPropertyBlock());

        public static VisualFixture CreateAtStep(string stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
                throw new ArgumentException("A visual fixture requires a step ID.", nameof(stepId));

            var content = LoadProductionContent();
            if (!content.Flow.steps.Any(step => string.Equals(step.id, stepId,
                    StringComparison.Ordinal)))
                throw new ArgumentException($"Unknown production step: {stepId}", nameof(stepId));

            var testMaterial = CreateTestMaterial();
            var root = new GameObject("visual.feedback.fixture");
            var staging = CreateChild(root.transform, "staging");
            var installed = CreateChild(root.transform, "installed");
            var entries = new List<PartPrefabEntry>();
            var targets = new List<DropTarget>();

            foreach (var step in content.Flow.steps)
            {
                var template = CreateTemplate(root.transform, step.id, testMaterial);
                entries.Add(new PartPrefabEntry(step.assetKey, template));
                targets.Add(CreateTarget(root.transform, step, testMaterial));
            }

            var unrelatedModule = CreateStandaloneRenderer(root.transform,
                "unrelated.module", new Vector3(-12f, 0f, 0f), testMaterial);
            var unrelatedTarget = CreateStandaloneRenderer(root.transform,
                "unrelated.target", new Vector3(-12f, 0f, 3f), testMaterial);
            var lockedFuture = new Renderer[3];
            for (var index = 0; index < lockedFuture.Length; index++)
            {
                lockedFuture[index] = CreateStandaloneRenderer(root.transform,
                    $"locked.future.{index}", new Vector3(-8f + index * 2f, 0f, -4f),
                    testMaterial);
            }

            var catalog = ScriptableObject.CreateInstance<PartPrefabCatalog>();
            catalog.Configure(entries.ToArray());

            var quiz = root.AddComponent<QuizPresenter>();
            var quizView = new MemoryQuizView();
            var drag = root.AddComponent<DragDropController>();
            var assembly = root.AddComponent<AssemblyPresenter>();
            assembly.Configure(catalog, staging, installed, drag);
            assembly.ConfigureTargets(targets);

            var feedbackPanel = CreateChild(root.transform, "feedback.panel").gameObject;
            var feedbackTextObject = new GameObject("feedback.text", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Text));
            feedbackTextObject.transform.SetParent(feedbackPanel.transform, false);
            var feedback = root.AddComponent<FeedbackView>();
            feedback.Configure(feedbackPanel, feedbackTextObject.GetComponent<Text>());

            var highlight = root.AddComponent<HighlightController>();
            var snapEffects = root.AddComponent<SnapEffectController>();
            snapEffects.Configure(highlight, drag);
            assembly.ConfigureEffects(snapEffects);

            var process = root.AddComponent<ProcessStagePresenter>();
            var completion = root.AddComponent<CompletionPresenter>();
            var flow = root.AddComponent<GuidedFlowController>();
            flow.Configure(content, catalog, quiz, quizView, assembly, process,
                completion, null, feedback, null, drag, 0f);
            drag.Configure(flow, targets.ToArray(), null);

            flow.StartNewRun();
            flow.ConfirmGuidance();
            AdvanceToUnlockedStep(flow, stepId);
            snapEffects.SetLockedFuture(lockedFuture);

            var currentModuleRenderer = assembly.CurrentModule.VisualRoot
                .GetComponentsInChildren<Renderer>(true).First();
            var currentTargetRenderer = assembly.CurrentTarget
                .GetComponentsInChildren<Renderer>(true).First();
            var wrongTarget = targets.First(target => !string.Equals(
                target.AcceptedStepId, stepId, StringComparison.Ordinal));

            return new VisualFixture(root, catalog, testMaterial, flow, assembly, drag,
                feedback, highlight, snapEffects, currentModuleRenderer,
                currentTargetRenderer, lockedFuture,
                new[] { unrelatedModule, unrelatedTarget }, wrongTarget);
        }

        public void DropOnWrongTarget()
        {
            DropOnTarget(wrongTarget, false);
        }

        public void DropOnCurrentTarget()
        {
            DropOnTarget(assembly.CurrentTarget, true);
        }

        public void DisableSnapEffects()
        {
            snapEffects.enabled = false;
        }

        public void Dispose()
        {
            snapEffects.ClearLockedFuture();
            snapEffects.Clear();
            highlight.Clear();
            root.SetActive(false);
            UnityEngine.Object.Destroy(catalog);
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(testMaterial);
        }

        private void DropOnTarget(DropTarget target, bool expectedAccepted)
        {
            var module = assembly.CurrentModule;
            if (module == null || target?.SnapAnchor == null)
                throw new InvalidOperationException("The visual fixture has no active module or target.");

            lastEffectModuleRenderer = module.VisualRoot
                .GetComponentsInChildren<Renderer>(true).First();
            lastEffectTargetRenderer = target.GetComponentsInChildren<Renderer>(true).First();
            if (!drag.TryBeginDrag(module))
                throw new InvalidOperationException("The unlocked module could not begin dragging.");

            drag.DragTo(target.SnapAnchor.position);
            var result = drag.ReleaseAt(module.transform.position);
            if (result.Accepted != expectedAccepted)
                throw new InvalidOperationException(
                    $"The visual fixture expected accepted={expectedAccepted} but received {result.Code}.");
            if (!expectedAccepted && result.Code != "wrong_target")
                throw new InvalidOperationException(
                    $"The visual fixture expected wrong_target but received {result.Code}.");
        }

        private IEnumerable<Renderer> ObservedRenderers()
        {
            yield return currentModuleRenderer;
            yield return currentTargetRenderer;
            foreach (var renderer in lockedFutureRenderers)
                yield return renderer;
            foreach (var renderer in unrelatedRenderers)
                yield return renderer;
            yield return lastEffectModuleRenderer;
            yield return lastEffectTargetRenderer;
        }

        private static void AdvanceToUnlockedStep(GuidedFlowController flow, string stepId)
        {
            var safety = 0;
            while (!string.Equals(flow.Snapshot.CurrentStepId, stepId, StringComparison.Ordinal)
                && safety++ < 64)
            {
                AnswerCurrentGate(flow);
                if (flow.Snapshot.Phase != FlowPhase.StepReady)
                    throw new InvalidOperationException("A fixture step did not unlock after its questions.");
                flow.CompleteCurrentStep();
            }

            if (safety >= 64 || !string.Equals(flow.Snapshot.CurrentStepId, stepId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException($"The visual fixture did not reach {stepId}.");

            AnswerCurrentGate(flow);
            if (flow.Snapshot.Phase != FlowPhase.StepReady)
                throw new InvalidOperationException($"The visual fixture did not unlock {stepId}.");
        }

        private static void AnswerCurrentGate(GuidedFlowController flow)
        {
            var safety = 0;
            while (flow.Snapshot.Phase == FlowPhase.KnowledgeGate && safety++ < 8)
            {
                var question = flow.CurrentQuestion;
                if (question == null)
                    throw new InvalidOperationException("The visual fixture has no current question.");
                flow.SubmitAnswer(question.correctOptionIndex);
            }

            if (safety >= 8)
                throw new InvalidOperationException("The visual fixture question gate did not complete.");
        }

        private static Material CreateTestMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The URP Lit shader is required by VisualFixture.");

            var material = new Material(shader) { name = "visual.fixture.urp-lit" };
            material.SetColor(BaseColorId, TestMaterialBaseColor);
            material.SetColor(EmissionColorId, Color.black);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return material;
        }

        private static GameObject CreateTemplate(Transform parent, string stepId, Material material)
        {
            var template = new GameObject("template." + stepId);
            template.transform.SetParent(parent, false);
            var visualRoot = CreateChild(template.transform, "VisualRoot");
            CreatePrimitiveRenderer(visualRoot, "Body", Vector3.zero, material);
            CreatePrimitiveRenderer(template.transform, "Highlight",
                new Vector3(0f, 0.4f, 0f), material);

            var collider = template.AddComponent<BoxCollider>();
            var module = template.AddComponent<DraggableModule>();
            module.Configure(stepId, collider, visualRoot);
            template.SetActive(false);
            return template;
        }

        private static DropTarget CreateTarget(Transform parent, StepDefinition step,
            Material material)
        {
            var targetObject = CreateChild(parent, step.dropTargetId).gameObject;
            targetObject.transform.localPosition = new Vector3(step.order * 4f, 0f, 2f);
            var anchor = CreateChild(targetObject.transform, "SnapAnchor");
            var marker = CreatePrimitiveRenderer(targetObject.transform, "TargetMarker",
                Vector3.zero, material);
            marker.enabled = false;

            var target = targetObject.AddComponent<DropTarget>();
            target.Configure(step.dropTargetId, step.id, anchor, 0f, 0.75f);
            return target;
        }

        private static Renderer CreateStandaloneRenderer(Transform parent, string name,
            Vector3 position, Material material)
        {
            var item = CreateChild(parent, name);
            item.localPosition = position;
            return CreatePrimitiveRenderer(item, "Body", Vector3.zero, material);
        }

        private static Renderer CreatePrimitiveRenderer(Transform parent, string name,
            Vector3 localPosition, Material material)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);
            var renderer = primitive.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Color ReadPropertyColor(Renderer renderer, int propertyId)
        {
            if (renderer == null)
                return Color.clear;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, 0);
            return block.GetColor(propertyId);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static ContentBundle LoadProductionContent()
        {
            var contentRoot = Path.Combine(Application.dataPath, "RailCraft", "Content", "V1");
            return JsonContentRepository.Load(
                File.ReadAllText(Path.Combine(contentRoot, "questions.v1.json")),
                File.ReadAllText(Path.Combine(contentRoot, "flow.v1.json")));
        }
    }
}
