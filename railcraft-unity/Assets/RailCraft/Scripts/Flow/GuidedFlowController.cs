using System;
using System.Collections;
using System.Collections.Generic;
using RailCraft.Assets;
using RailCraft.CameraSystem;
using RailCraft.Content;
using RailCraft.Interaction;
using RailCraft.Presentation;
using RailCraft.Process;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RailCraft.Flow
{
    [DisallowMultipleComponent]
    public sealed class GuidedFlowController : MonoBehaviour, IDragAuthorization
    {
        [Header("Production content")]
        [SerializeField] private TextAsset questionsJson;
        [SerializeField] private TextAsset flowJson;
        [SerializeField] private PartPrefabCatalog prefabCatalog;
        [SerializeField] private string factorySceneName = "Factory";
        [SerializeField] private bool initializeOnStart = true;

        [Header("Bootstrap presenters")]
        [SerializeField] private QuizPresenter quizPresenter;
        [SerializeField] private AssemblyPresenter assemblyPresenter;
        [SerializeField] private ProcessStagePresenter processPresenter;
        [SerializeField] private CompletionPresenter completionPresenter;
        [SerializeField] private StepHudView stepHud;
        [SerializeField] private FeedbackView feedbackView;
        [SerializeField] private DragDropController dragDropController;
        [SerializeField] private MainMenuPresenter mainMenuPresenter;
        [SerializeField] private GuidancePresenter guidancePresenter;
        [SerializeField] private SettingsPresenter settingsPresenter;
        [SerializeField] private ResetPresenter resetPresenter;

        private readonly Dictionary<string, QuestionDefinition> questionsById =
            new Dictionary<string, QuestionDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, StepDefinition> stepsById =
            new Dictionary<string, StepDefinition>(StringComparer.Ordinal);
        private readonly HashSet<string> answeredQuestionIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> completedStepIds =
            new HashSet<string>(StringComparer.Ordinal);

        private GuidedFlowMachine machine;
        private ContentBundle content;
        private CameraShotDirector cameraDirector;
        private bool configured;
        private Transform runtimeHeroAnchor;
        private Transform runtimeReleaseDestination;

        public event Action<AnswerResult> AnswerEvaluated;
        public event Action<string> StepCompleted;
        public event Action<FlowSnapshot> StateChanged;

        public FlowSnapshot Snapshot => machine?.Snapshot
            ?? new FlowSnapshot(FlowPhase.MainMenu, 0, 0, null, 0);
        public QuestionDefinition CurrentQuestion => ResolveCurrentQuestion();
        public StepDefinition CurrentStep => ResolveCurrentStep();
        public int QuestionsAnswered => answeredQuestionIds.Count;
        public int CompletedUniqueSteps => completedStepIds.Count;
        public IReadOnlyCollection<string> AnsweredQuestionIds => answeredQuestionIds;
        public IReadOnlyCollection<string> CompletedStepIds => completedStepIds;
        public string FatalErrorCode { get; private set; }
        public bool IsConfigured => configured;

        public void ConfigureStartup(TextAsset configuredQuestionsJson, TextAsset configuredFlowJson,
            PartPrefabCatalog configuredCatalog, QuizPresenter configuredQuiz,
            AssemblyPresenter configuredAssembly, ProcessStagePresenter configuredProcess,
            CompletionPresenter configuredCompletion, StepHudView configuredHud,
            FeedbackView configuredFeedback, DragDropController configuredDragController,
            string configuredFactorySceneName = "Factory")
        {
            questionsJson = configuredQuestionsJson;
            flowJson = configuredFlowJson;
            prefabCatalog = configuredCatalog;
            quizPresenter = configuredQuiz;
            assemblyPresenter = configuredAssembly;
            processPresenter = configuredProcess;
            completionPresenter = configuredCompletion;
            stepHud = configuredHud;
            feedbackView = configuredFeedback;
            dragDropController = configuredDragController;
            factorySceneName = string.IsNullOrWhiteSpace(configuredFactorySceneName)
                ? configuredFactorySceneName
                : configuredFactorySceneName.Trim();
            initializeOnStart = true;
        }

        public void ConfigureNavigation(MainMenuPresenter configuredMainMenu,
            GuidancePresenter configuredGuidance, SettingsPresenter configuredSettings,
            ResetPresenter configuredReset)
        {
            mainMenuPresenter = configuredMainMenu;
            guidancePresenter = configuredGuidance;
            settingsPresenter = configuredSettings;
            resetPresenter = configuredReset;
        }

        public void Configure(ContentBundle configuredContent, PartPrefabCatalog configuredCatalog,
            QuizPresenter configuredQuiz, IQuizView configuredQuizView,
            AssemblyPresenter configuredAssembly, ProcessStagePresenter configuredProcess,
            CompletionPresenter configuredCompletion, StepHudView configuredHud,
            FeedbackView configuredFeedback, CameraShotDirector configuredCameraDirector,
            DragDropController configuredDragController, float quizTransitionDuration)
        {
            ValidateConfiguration(configuredContent, configuredCatalog, configuredQuiz,
                configuredAssembly, configuredProcess, configuredCompletion);
            UnsubscribeDependencies();

            content = configuredContent;
            prefabCatalog = configuredCatalog;
            quizPresenter = configuredQuiz;
            assemblyPresenter = configuredAssembly;
            processPresenter = configuredProcess;
            completionPresenter = configuredCompletion;
            stepHud = configuredHud;
            feedbackView = configuredFeedback;
            cameraDirector = configuredCameraDirector;
            dragDropController = configuredDragController;
            machine = new GuidedFlowMachine(content);
            BuildLookups();

            quizPresenter.Configure(machine, content, configuredQuizView, quizTransitionDuration);
            quizPresenter.AnswerEvaluated += HandleAnswerEvaluated;
            quizPresenter.StepUnlocked += HandleStepUnlocked;
            if (dragDropController != null)
            {
                dragDropController.DropCompleted += HandleDropCompleted;
                dragDropController.DropRejected += HandleDropRejected;
            }
            processPresenter.ReworkAcknowledged += AcknowledgeRework;
            processPresenter.SecondCommissioningCompleted += CompleteSecondCommissioning;
            completionPresenter.RestartRequested += ResetRun;
            completionPresenter.ExitRequested += ExitApplication;

            answeredQuestionIds.Clear();
            completedStepIds.Clear();
            FatalErrorCode = null;
            configured = true;
            quizPresenter.CancelAndHide();
            processPresenter.ResetView();
            completionPresenter.Hide();
            feedbackView?.Hide();
            if (stepHud != null)
                stepHud.gameObject.SetActive(false);
            RaiseStateChanged();
        }

        public void StartNewRun()
        {
            if (!configured)
                return;
            machine.StartNewRun();
            completionPresenter.Hide();
            processPresenter.ResetView();
            cameraDirector?.Focus("overview");
            RaiseStateChanged();
        }

        public void ConfirmGuidance()
        {
            if (!configured || Snapshot.Phase != FlowPhase.Guidance)
                return;
            machine.ConfirmGuidance();
            PresentKnowledgeGate();
        }

        public void SubmitAnswer(int optionIndex)
        {
            if (!configured)
                return;
            quizPresenter.SubmitAnswer(optionIndex);
        }

        public void CompleteCurrentStep()
        {
            if (!configured || Snapshot.Phase != FlowPhase.StepReady)
                return;
            HandleAcceptedDrop(Snapshot.CurrentStepId);
        }

        public void AcknowledgeRework()
        {
            if (!configured || Snapshot.Phase != FlowPhase.Rework)
                return;
            assemblyPresenter.SetInstalledHighlight("sensor_module", false);
            machine.ConfirmReworkAcknowledged();
            processPresenter.ShowInspection();
            PresentKnowledgeGate();
        }

        public void CompleteSecondCommissioning()
        {
            if (!configured || Snapshot.Phase != FlowPhase.SecondCommissioning)
                return;
            machine.CompleteSecondCommissioning();
            processPresenter.ShowSecondCommissioningPassed();
            PresentKnowledgeGate();
        }

        public void ResetRun()
        {
            if (!configured)
                return;
            machine.Reset();
            answeredQuestionIds.Clear();
            completedStepIds.Clear();
            dragDropController?.CancelAllInteractions();
            quizPresenter.CancelAndHide();
            assemblyPresenter.Clear();
            processPresenter.ResetView();
            completionPresenter.Hide();
            feedbackView?.Hide();
            if (stepHud != null)
                stepHud.gameObject.SetActive(false);
            cameraDirector?.Focus("overview");
            RaiseStateChanged();
        }

        public void ExitApplication()
        {
            Application.Quit();
        }

        public bool CanDrag(string stepId)
        {
            return configured
                && Snapshot.Phase == FlowPhase.StepReady
                && string.Equals(Snapshot.CurrentStepId, stepId, StringComparison.Ordinal);
        }

        private void Start()
        {
            if (initializeOnStart && !configured)
                StartCoroutine(InitializeProductionComposition());
        }

        private IEnumerator InitializeProductionComposition()
        {
            ContentBundle loadedContent;
            try
            {
                if (questionsJson == null)
                    throw new ContentLoadException("bootstrap_questions_missing");
                if (flowJson == null)
                    throw new ContentLoadException("bootstrap_flow_missing");
                if (prefabCatalog == null)
                    throw new ContentLoadException("bootstrap_catalog_missing");

                loadedContent = JsonContentRepository.Load(questionsJson.text, flowJson.text);
                var issues = ContentValidator.Validate(loadedContent);
                if (issues.Count > 0)
                    throw new ContentLoadException(issues[0]);
                ValidateCatalogBindings(loadedContent, prefabCatalog);
            }
            catch (Exception exception)
            {
                ShowFatal(ExtractIssueCode(exception));
                yield break;
            }

            if (!SceneManager.GetSceneByName(factorySceneName).isLoaded)
            {
                AsyncOperation load;
                try
                {
                    load = SceneManager.LoadSceneAsync(factorySceneName, LoadSceneMode.Additive);
                }
                catch (Exception)
                {
                    ShowFatal("factory_scene_load_failed");
                    yield break;
                }
                if (load == null)
                {
                    ShowFatal("factory_scene_load_failed");
                    yield break;
                }
                yield return load;
            }

            try
            {
                BindFactoryComposition(loadedContent);
            }
            catch (Exception exception)
            {
                ShowFatal(ExtractIssueCode(exception));
            }
        }

        private void BindFactoryComposition(ContentBundle loadedContent)
        {
            if (quizPresenter == null || assemblyPresenter == null || processPresenter == null
                || completionPresenter == null || dragDropController == null)
                throw new InvalidOperationException("bootstrap_presenter_missing");

            var staging = FindTransform("PartsStagingArea");
            if (staging == null)
                throw new InvalidOperationException("factory_staging_missing");
            var lockedFuture = FindTransform("LockedFutureModules");
            if (lockedFuture == null)
                throw new InvalidOperationException("factory_locked_future_missing");
            var installedObject = new GameObject("InstalledModules");
            installedObject.transform.SetParent(staging.parent, false);

            var targets = FindObjectsByType<DropTarget>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (targets.Length != loadedContent.Flow.steps.Length)
                throw new InvalidOperationException("factory_drop_target_count");

            cameraDirector = FindFirstObjectByType<CameraShotDirector>();
            var orbitController = FindFirstObjectByType<FactoryCameraController>();
            assemblyPresenter.Configure(prefabCatalog, staging, installedObject.transform,
                dragDropController);
            assemblyPresenter.ConfigureTargets(targets);
            assemblyPresenter.ConfigureLockedFuture(lockedFuture);
            dragDropController.SetInteractionCamera(UnityEngine.Camera.main);
            dragDropController.Configure(this, targets, orbitController);
            Configure(loadedContent, prefabCatalog, quizPresenter, null, assemblyPresenter,
                processPresenter, completionPresenter, stepHud, feedbackView, cameraDirector,
                dragDropController, 0.2f);
        }

        private void PresentKnowledgeGate()
        {
            if (Snapshot.Phase != FlowPhase.KnowledgeGate)
                return;
            var step = ResolveCurrentStep();
            if (step == null)
                throw new InvalidOperationException("Active flow step is missing.");

            assemblyPresenter.PrepareStep(step);
            quizPresenter.ShowStep(step);
            cameraDirector?.Focus(step.id);
            UpdateHud();
            RaiseStateChanged();
        }

        private void HandleAnswerEvaluated(AnswerResult result)
        {
            if (result == null)
                return;
            if (result.IsCorrect && !string.IsNullOrWhiteSpace(result.QuestionId))
                answeredQuestionIds.Add(result.QuestionId);
            AnswerEvaluated?.Invoke(result);
            UpdateHud();
            RaiseStateChanged();
        }

        private void HandleStepUnlocked(string stepId)
        {
            if (Snapshot.Phase != FlowPhase.StepReady
                || !string.Equals(Snapshot.CurrentStepId, stepId, StringComparison.Ordinal))
                return;
            assemblyPresenter.UnlockStep(stepId);
            UpdateHud();
            RaiseStateChanged();
        }

        private void HandleDropCompleted(string stepId)
        {
            HandleAcceptedDrop(stepId);
        }

        private void HandleAcceptedDrop(string stepId)
        {
            var decision = machine.ConfirmDrop(stepId);
            if (!decision.Accepted)
            {
                feedbackView?.Show("当前模块尚未解锁。", 1.5f);
                return;
            }

            assemblyPresenter.MarkCurrentInstalled(stepId);
            if (completedStepIds.Add(stepId))
                StepCompleted?.Invoke(stepId);
            machine.ConfirmSnapAnimation();
            HandlePostSnapPhase();
        }

        private void HandlePostSnapPhase()
        {
            switch (Snapshot.Phase)
            {
                case FlowPhase.KnowledgeGate:
                    PresentKnowledgeGate();
                    return;
                case FlowPhase.Rework:
                    processPresenter.ShowTeachingAnomaly();
                    assemblyPresenter.SetInstalledHighlight("sensor_module", true);
                    cameraDirector?.Focus("sensor_module");
                    break;
                case FlowPhase.SecondCommissioning:
                    processPresenter.ShowInspectionCompleted();
                    cameraDirector?.Focus("commissioning");
                    break;
                case FlowPhase.Completed:
                    PrepareHeroPresentation();
                    completionPresenter.ShowCompleted();
                    break;
            }
            UpdateHud();
            RaiseStateChanged();
        }

        private void HandleDropRejected(DragDropResult result)
        {
            if (result == null)
                return;
            var message = result.Code == "wrong_target"
                ? "安装位置不匹配，请拖到当前发光接口。"
                : "模块尚未放入有效接口。";
            feedbackView?.Show(message, 1.5f);
        }

        private void UpdateHud()
        {
            if (stepHud == null)
                return;
            var step = ResolveCurrentStep();
            if (step == null)
                return;
            var answeredInStep = Snapshot.Phase == FlowPhase.KnowledgeGate
                ? Mathf.Clamp(Snapshot.QuestionIndex, 0, step.questionIds.Length)
                : step.questionIds.Length;
            stepHud.Show(step, completedStepIds.Count, answeredInStep,
                step.questionIds.Length, Snapshot.Phase == FlowPhase.SecondCommissioning);
        }

        private void PrepareHeroPresentation()
        {
            var vehicle = assemblyPresenter.GetInstalledVisual("carbody_lowering");
            if (vehicle == null)
                return;

            if (runtimeReleaseDestination == null)
            {
                var destination = new GameObject("ReleasedVehicleDestination");
                destination.transform.SetParent(transform, false);
                runtimeReleaseDestination = destination.transform;
            }
            runtimeReleaseDestination.position = vehicle.transform.position + Vector3.forward * 8f;

            if (runtimeHeroAnchor == null)
            {
                var heroAnchor = new GameObject("HeroViewAnchor");
                heroAnchor.transform.SetParent(transform, false);
                runtimeHeroAnchor = heroAnchor.transform;
            }
            var headDisplay = GameObject.Find("CR400AF 展示背景");
            runtimeHeroAnchor.position = headDisplay == null
                ? runtimeReleaseDestination.position
                : Vector3.Lerp(headDisplay.transform.position, runtimeReleaseDestination.position, 0.5f);
            cameraDirector?.AddOrReplaceShot(new FactoryCameraShot
            {
                shotId = "hero",
                focusAnchor = runtimeHeroAnchor,
                distance = 14f,
                yaw = 28f,
                pitch = 26f
            });
            completionPresenter.ConfigureReleaseScene(vehicle.transform,
                runtimeReleaseDestination, cameraDirector);
        }

        private StepDefinition ResolveCurrentStep()
        {
            var stepId = Snapshot.CurrentStepId;
            return !string.IsNullOrWhiteSpace(stepId)
                && stepsById.TryGetValue(stepId, out var step)
                ? step
                : null;
        }

        private QuestionDefinition ResolveCurrentQuestion()
        {
            if (Snapshot.Phase != FlowPhase.KnowledgeGate)
                return null;
            var step = ResolveCurrentStep();
            var index = Snapshot.QuestionIndex;
            if (step?.questionIds == null || index < 0 || index >= step.questionIds.Length)
                return null;
            return questionsById.TryGetValue(step.questionIds[index], out var question)
                ? question
                : null;
        }

        private void BuildLookups()
        {
            questionsById.Clear();
            foreach (var question in content.Questions)
                questionsById.Add(question.id, question);
            stepsById.Clear();
            foreach (var step in content.Flow.steps)
                stepsById.Add(step.id, step);
        }

        private static void ValidateConfiguration(ContentBundle configuredContent,
            PartPrefabCatalog configuredCatalog, QuizPresenter configuredQuiz,
            AssemblyPresenter configuredAssembly, ProcessStagePresenter configuredProcess,
            CompletionPresenter configuredCompletion)
        {
            if (configuredContent == null)
                throw new ArgumentNullException(nameof(configuredContent));
            var issues = ContentValidator.Validate(configuredContent);
            if (issues.Count > 0)
                throw new ContentLoadException(issues[0]);
            if (configuredCatalog == null)
                throw new ArgumentNullException(nameof(configuredCatalog));
            if (configuredQuiz == null)
                throw new ArgumentNullException(nameof(configuredQuiz));
            if (configuredAssembly == null)
                throw new ArgumentNullException(nameof(configuredAssembly));
            if (configuredProcess == null)
                throw new ArgumentNullException(nameof(configuredProcess));
            if (configuredCompletion == null)
                throw new ArgumentNullException(nameof(configuredCompletion));
            ValidateCatalogBindings(configuredContent, configuredCatalog);
        }

        private static void ValidateCatalogBindings(ContentBundle configuredContent,
            PartPrefabCatalog configuredCatalog)
        {
            foreach (var step in configuredContent.Flow.steps)
            {
                if (configuredCatalog.Resolve(step.assetKey) == null)
                    throw new ContentLoadException($"catalog_asset_missing:{step.assetKey}");
            }
        }

        private void ShowFatal(string issueCode)
        {
            FatalErrorCode = string.IsNullOrWhiteSpace(issueCode) ? "unknown" : issueCode;
            configured = false;
            dragDropController?.CancelAllInteractions();
            quizPresenter?.CancelAndHide();
            processPresenter?.ResetView();
            mainMenuPresenter?.Hide();
            guidancePresenter?.Hide();
            settingsPresenter?.Hide();
            resetPresenter?.HideConfirmation();
            completionPresenter?.ShowFatal(FatalErrorCode);
            RaiseStateChanged();
        }

        private static string ExtractIssueCode(Exception exception)
        {
            if (exception == null || string.IsNullOrWhiteSpace(exception.Message))
                return "unknown";
            return exception.Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
        }

        private static Transform FindTransform(string objectName)
        {
            var found = GameObject.Find(objectName);
            return found == null ? null : found.transform;
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(Snapshot);
        }

        private void UnsubscribeDependencies()
        {
            if (quizPresenter != null)
            {
                quizPresenter.AnswerEvaluated -= HandleAnswerEvaluated;
                quizPresenter.StepUnlocked -= HandleStepUnlocked;
            }
            if (dragDropController != null)
            {
                dragDropController.DropCompleted -= HandleDropCompleted;
                dragDropController.DropRejected -= HandleDropRejected;
            }
            if (processPresenter != null)
            {
                processPresenter.ReworkAcknowledged -= AcknowledgeRework;
                processPresenter.SecondCommissioningCompleted -= CompleteSecondCommissioning;
            }
            if (completionPresenter != null)
            {
                completionPresenter.RestartRequested -= ResetRun;
                completionPresenter.ExitRequested -= ExitApplication;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeDependencies();
        }
    }
}
