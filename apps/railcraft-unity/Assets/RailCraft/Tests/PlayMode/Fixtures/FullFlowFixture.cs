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

namespace RailCraft.Tests.PlayMode.Fixtures
{
    internal sealed class FullFlowFixture : IDisposable
    {
        private readonly GameObject root;
        private readonly PartPrefabCatalog catalog;
        private readonly HashSet<string> answeredQuestionIds = new HashSet<string>();
        private readonly HashSet<string> completedStepIds = new HashSet<string>();
        private readonly MemoryQuizView quizView;
        private readonly QuizPresenter quiz;
        private readonly DragDropController drag;

        public GuidedFlowController Controller { get; }
        public AssemblyPresenter Assembly { get; }
        public ProcessStagePresenter Process { get; }
        public CompletionPresenter Completion { get; }
        public IReadOnlyCollection<string> ExpectedQuestionIds { get; }
        public IReadOnlyCollection<string> ExpectedStepIds { get; }
        public IReadOnlyCollection<string> AnsweredQuestionIds => answeredQuestionIds;
        public IReadOnlyCollection<string> CompletedStepIds => completedStepIds;
        public int QuestionsAnswered => answeredQuestionIds.Count;
        public int CompletedUniqueSteps => completedStepIds.Count;
        public int QuestionAnswerEventCount { get; private set; }
        public int StepDropEventCount { get; private set; }
        public bool IsQuizVisible => quizView.IsVisible;
        public bool IsPartDragActive => drag.IsPartDragActive;
        public int ScoreUiCount => CountScoreUi(root);

        private FullFlowFixture(GameObject root, PartPrefabCatalog catalog,
            GuidedFlowController controller, AssemblyPresenter assembly,
            ProcessStagePresenter process, CompletionPresenter completion,
            QuizPresenter quiz, MemoryQuizView quizView, DragDropController drag,
            ContentBundle content)
        {
            this.root = root;
            this.catalog = catalog;
            Controller = controller;
            Assembly = assembly;
            Process = process;
            Completion = completion;
            this.quiz = quiz;
            this.quizView = quizView;
            this.drag = drag;
            ExpectedQuestionIds = content.Questions.Select(question => question.id).ToArray();
            ExpectedStepIds = content.Flow.steps.Select(step => step.id).ToArray();
            quiz.AnswerEvaluated += ObserveAnswer;
            drag.DropCompleted += ObserveDrop;
        }

        public static FullFlowFixture Create(float snapDuration = 0f)
        {
            var content = LoadProductionContent();
            var root = new GameObject("full.flow.fixture");
            var staging = CreateChild(root.transform, "staging");
            var installed = CreateChild(root.transform, "installed");
            var templates = new List<GameObject>();
            var entries = new List<PartPrefabEntry>();
            var targets = new List<DropTarget>();

            foreach (var step in content.Flow.steps)
            {
                var template = CreateTemplate(root.transform, step.id);
                templates.Add(template);
                entries.Add(new PartPrefabEntry(step.assetKey, template));
                targets.Add(CreateTarget(root.transform, step, snapDuration));
            }

            var catalog = ScriptableObject.CreateInstance<PartPrefabCatalog>();
            catalog.Configure(entries.ToArray());
            var quiz = root.AddComponent<QuizPresenter>();
            var quizView = new MemoryQuizView();
            var drag = root.AddComponent<DragDropController>();
            var assembly = root.AddComponent<AssemblyPresenter>();
            assembly.Configure(catalog, staging, installed, drag);
            assembly.ConfigureTargets(targets);

            var inspectionMarker = CreateChild(root.transform, "inspection.marker").gameObject;
            var passIndicator = CreateChild(root.transform, "commissioning.pass").gameObject;
            var process = root.AddComponent<ProcessStagePresenter>();
            process.Configure(null, null, inspectionMarker, passIndicator, null);
            var completion = root.AddComponent<CompletionPresenter>();
            var controller = root.AddComponent<GuidedFlowController>();
            controller.Configure(content, catalog, quiz, quizView, assembly,
                process, completion, null, null, null, drag, 0f);
            drag.Configure(controller, targets.ToArray(), null);

            return new FullFlowFixture(root, catalog, controller, assembly,
                process, completion, quiz, quizView, drag, content);
        }

        public void StartNewRun()
        {
            Controller.StartNewRun();
            Controller.ConfirmGuidance();
        }

        public void AnswerCurrentQuestionCorrectlyWhenVisible()
        {
            if (Controller.Snapshot.Phase != FlowPhase.KnowledgeGate)
                return;
            if (!quizView.IsVisible || !quizView.AreOptionsInteractable)
                throw new InvalidOperationException("The production quiz is not interactive at a knowledge gate.");
            if (quiz.CurrentQuestion == null || Controller.CurrentQuestion == null
                || quiz.CurrentQuestion.id != Controller.CurrentQuestion.id)
                throw new InvalidOperationException("The quiz and controller expose different questions.");
            quizView.Click(Controller.CurrentQuestion.correctOptionIndex);
        }

        public void DropCurrentItemWhenUnlocked()
        {
            switch (Controller.Snapshot.Phase)
            {
                case FlowPhase.StepReady:
                    BeginCurrentDrag();
                    var result = ReleaseCurrentDragAtTarget();
                    if (!result.Accepted)
                        throw new InvalidOperationException($"The valid drop was rejected: {result.Code}");
                    break;
                case FlowPhase.Rework:
                    Process.RequestReworkAcknowledgement();
                    break;
                case FlowPhase.SecondCommissioning:
                    Process.RequestSecondCommissioningCompletion();
                    break;
            }
        }

        public void BeginCurrentDrag()
        {
            var module = Assembly.CurrentModule;
            var target = Assembly.CurrentTarget;
            if (module == null || target?.SnapAnchor == null)
                throw new InvalidOperationException("The active module or target was not presented.");
            if (!module.InteractionCollider.enabled || !Assembly.IsTargetHighlighted)
                throw new InvalidOperationException("The active module was not unlocked and highlighted.");
            if (!drag.TryBeginDrag(module))
                throw new InvalidOperationException("The drag controller rejected the unlocked module.");
        }

        public DragDropResult ReleaseCurrentDragAtTarget()
        {
            var module = Assembly.CurrentModule;
            var target = Assembly.CurrentTarget;
            drag.DragTo(target.SnapAnchor.position);
            return drag.ReleaseAt(module.transform.position);
        }

        public void ResetObservedEvents()
        {
            answeredQuestionIds.Clear();
            completedStepIds.Clear();
            QuestionAnswerEventCount = 0;
            StepDropEventCount = 0;
        }

        public void Dispose()
        {
            quiz.AnswerEvaluated -= ObserveAnswer;
            drag.DropCompleted -= ObserveDrop;
            UnityEngine.Object.Destroy(catalog);
            UnityEngine.Object.Destroy(root);
        }

        private void ObserveAnswer(AnswerResult result)
        {
            if (result == null || !result.IsCorrect)
                return;
            QuestionAnswerEventCount++;
            answeredQuestionIds.Add(result.QuestionId);
        }

        private void ObserveDrop(string stepId)
        {
            StepDropEventCount++;
            completedStepIds.Add(stepId);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static GameObject CreateTemplate(Transform parent, string stepId)
        {
            var template = new GameObject("template." + stepId);
            template.transform.SetParent(parent, false);
            var visual = CreateChild(template.transform, "VisualRoot");
            var highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlight.name = "Highlight";
            highlight.transform.SetParent(template.transform, false);
            var highlightCollider = highlight.GetComponent<Collider>();
            if (highlightCollider != null)
                UnityEngine.Object.Destroy(highlightCollider);
            var collider = template.AddComponent<BoxCollider>();
            var draggable = template.AddComponent<DraggableModule>();
            draggable.Configure(stepId, collider, visual);
            template.SetActive(false);
            return template;
        }

        private static DropTarget CreateTarget(Transform parent, StepDefinition step,
            float snapDuration)
        {
            var targetObject = CreateChild(parent, step.dropTargetId).gameObject;
            targetObject.transform.localPosition = new Vector3(step.order * 0.2f, 0f, 2f);
            var anchor = CreateChild(targetObject.transform, "SnapAnchor");
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "TargetMarker";
            marker.transform.SetParent(targetObject.transform, false);
            var markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
                UnityEngine.Object.Destroy(markerCollider);
            marker.GetComponent<Renderer>().enabled = false;
            var target = targetObject.AddComponent<DropTarget>();
            target.Configure(step.dropTargetId, step.id, anchor, snapDuration, 0.75f);
            return target;
        }

        private static int CountScoreUi(GameObject fixtureRoot)
        {
            var count = 0;
            foreach (var component in fixtureRoot.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                    continue;
                var name = component.gameObject.name ?? string.Empty;
                if (name.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.Contains("分数") || name.Contains("得分"))
                    count++;
            }
            return count;
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
