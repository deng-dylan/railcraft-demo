using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class WhiteboxProgressAndKnowledgePresenterTests
    {
        private GameObject root;
        private WhiteboxGameSessionHost host;
        private DomainWorldGameSession session;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ProgressAndKnowledgeTests");
            host = root.AddComponent<WhiteboxGameSessionHost>();
            session = new DomainWorldGameSession();
            host.Configure(session, "测试目标");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void DefaultRecipeHasTwentyThreeStepsAndChineseFsmLabels()
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();

            Assert.That(
                WhiteboxAssemblyProgressPresenter.CalculateTotalSteps(catalog),
                Is.EqualTo(23));
            Assert.That(
                WhiteboxAssemblyProgressPresenter.BuildStepLabel(0, 23),
                Is.EqualTo("第1步/共23步"));
            Assert.That(
                WhiteboxAssemblyProgressPresenter.BuildStepLabel(23, 23),
                Is.EqualTo("第23步/共23步"));
            Assert.That(
                WhiteboxAssemblyProgressPresenter.GetStatusDisplayName(AssemblyFlowStatus.Pending),
                Is.EqualTo("待装配"));
            Assert.That(
                WhiteboxAssemblyProgressPresenter.GetStatusDisplayName(AssemblyFlowStatus.InProgress),
                Is.EqualTo("进行中"));
            Assert.That(
                WhiteboxAssemblyProgressPresenter.GetStatusDisplayName(AssemblyFlowStatus.Completed),
                Is.EqualTo("完成"));
        }

        [Test]
        public void SnapshotProgressCountsInstalledInputsAndCommissioningPhases()
        {
            var snapshot = new WhiteboxGameSessionSnapshot
            {
                Modules = new[]
                {
                    new ModuleAssemblySnapshot
                    {
                        ModuleId = ModuleId.WheelsetAxlebox,
                        InstalledParts = new[] { PartId.Axle, PartId.Wheel, PartId.Bearing }
                    },
                    new ModuleAssemblySnapshot
                    {
                        ModuleId = ModuleId.BogieStructure,
                        InstalledModules = new[] { ModuleId.WheelsetAxlebox, ModuleId.Frame }
                    }
                },
                CommissioningPhase = CommissioningPhase.ReadyForRetest
            };

            Assert.That(
                WhiteboxAssemblyProgressPresenter.CalculateCompletedSteps(
                    snapshot,
                    WhiteboxGameCatalog.CreateDefault()),
                Is.EqualTo(8));
        }

        [Test]
        public void PresenterRefreshesSliderStepPercentAndStatus()
        {
            var slider = Child("Slider").AddComponent<Slider>();
            var step = Child("Step").AddComponent<Text>();
            var percent = Child("Percent").AddComponent<Text>();
            var status = Child("Status").AddComponent<Text>();
            var presenter = root.AddComponent<WhiteboxAssemblyProgressPresenter>();

            presenter.Configure(host, slider, step, percent, status);

            Assert.That(presenter.TotalSteps, Is.EqualTo(23));
            Assert.That(presenter.CompletedSteps, Is.Zero);
            Assert.That(slider.value, Is.Zero);
            Assert.That(step.text, Is.EqualTo("第1步/共23步"));
            Assert.That(percent.text, Is.EqualTo("完成度 0%"));
            Assert.That(status.text, Is.EqualTo("状态：待装配"));
        }

        [Test]
        public void CorrectAnswerKnowledgePopupLocksAndRestoresPreviousInputState()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var view = CreateKnowledgeView(inputLock);
            var question = session.DomainSession.Catalog.Questions[0];

            var result = view.Presenter.RecordCorrectAnswer(question.Id);

            Assert.That(result.NewlyUnlockedCount, Is.EqualTo(2));
            Assert.That(view.PopupRoot.activeSelf, Is.True);
            Assert.That(view.PopupTitle.text, Does.Contain("工程知识"));
            Assert.That(inputLock.InputLocked, Is.True);

            view.Presenter.CloseKnowledgePopup();
            Assert.That(inputLock.InputLocked, Is.False);

            inputLock.SetInputLocked(true);
            view.Presenter.RecordCorrectAnswer(session.DomainSession.Catalog.Questions[1].Id);
            view.Presenter.CloseKnowledgePopup();
            Assert.That(inputLock.InputLocked, Is.True);
        }

        [Test]
        public void HostAnswerEventDefersPopupUntilQuizReleasesItsInputLock()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var view = CreateKnowledgeView(inputLock);
            var question = session.DomainSession.Catalog.Questions[0];
            inputLock.SetInputLocked(true);

            Assert.That(host.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
            Assert.That(view.Presenter.PendingPopupCount, Is.EqualTo(1));
            Assert.That(view.PopupRoot.activeSelf, Is.False);

            // QuizPartStation closes after SubmitAnswer returns.
            inputLock.SetInputLocked(false);
            view.Presenter.FlushPendingKnowledgePopup();
            Assert.That(view.PopupRoot.activeSelf, Is.True);
            Assert.That(inputLock.InputLocked, Is.True);

            view.Presenter.CloseKnowledgePopup();
            Assert.That(inputLock.InputLocked, Is.False);
        }

        [Test]
        public void SessionResetDiscardsTheCompletedViewInputLockCapture()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var view = CreateKnowledgeView(inputLock);
            var question = session.DomainSession.Catalog.Questions[0];
            inputLock.SetInputLocked(true);
            view.Presenter.RecordCorrectAnswer(question.Id);
            Assert.That(view.PopupRoot.activeSelf, Is.True);

            host.ResetSession();

            Assert.That(view.PopupRoot.activeSelf, Is.False);
            Assert.That(inputLock.InputLocked, Is.False);
        }

        [Test]
        public void CompletingModuleUnlocksAssemblyKnowledgeThroughMilestoneEvent()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var view = CreateKnowledgeView(inputLock);
            var definition = session.DomainSession.Catalog.GetModule(ModuleId.WheelsetAxlebox);

            foreach (var partId in definition.RequiredParts)
            {
                var question = session.DomainSession.Catalog.Questions
                    .First(candidate => candidate.RewardPart == partId);
                Assert.That(host.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
                Assert.That(host.CollectPart(partId).Accepted, Is.True);
                Assert.That(host.InstallPart(definition.Id, partId).Accepted, Is.True);
            }

            Assert.That(
                view.Presenter.UnlockedEntries.Any(
                    entry => entry.RelatedModule == ModuleId.WheelsetAxlebox),
                Is.True);
            view.Presenter.FlushPendingKnowledgePopup();
            Assert.That(view.PopupRoot.activeSelf, Is.True);
        }

        [Test]
        public void CompletedSnapshotRebuildUnlocksCatalogAndAllPriorCommissioningKnowledge()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var view = CreateKnowledgeView(inputLock);
            var snapshot = new WhiteboxGameSessionSnapshot
            {
                FlowStatus = AssemblyFlowStatus.Completed,
                CommissioningPhase = CommissioningPhase.InService,
                UnlockedParts = new[] { PartId.Axle },
                Modules = new[]
                {
                    new ModuleAssemblySnapshot
                    {
                        ModuleId = ModuleId.WheelsetAxlebox,
                        InstalledParts = new[] { PartId.Axle, PartId.Wheel, PartId.Bearing }
                    }
                }
            };

            view.Presenter.RebuildFromSnapshot(snapshot);

            Assert.That(view.Presenter.IsCatalogUnlocked, Is.True);
            Assert.That(view.CatalogButton.interactable, Is.True);
            Assert.That(
                view.Presenter.UnlockedEntries.Count(
                    entry => entry.Category == KnowledgeEntryCategory.Commissioning),
                Is.EqualTo(4));
            Assert.That(
                view.Presenter.UnlockedEntries.Any(
                    entry => entry.Category == KnowledgeEntryCategory.VehicleOverview),
                Is.True);

            view.Presenter.OpenCatalog();
            Assert.That(view.CatalogRoot.activeSelf, Is.True);
            Assert.That(view.CatalogBody.text, Does.Contain("工程知识图鉴"));
        }

        [Test]
        public void SnapshotRebuildUsesTheExactAnsweredQuestionWhenAvailable()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var view = CreateKnowledgeView(inputLock);
            var axleQuestions = session.DomainSession.Catalog.Questions
                .Where(question => question.RewardPart == PartId.Axle)
                .ToArray();
            Assert.That(axleQuestions.Length, Is.GreaterThan(1));
            var answered = axleQuestions[1];
            var snapshot = session.DomainSession.ExportSnapshot();
            snapshot.FlowStatus = AssemblyFlowStatus.InProgress;
            snapshot.AnswerAttemptCount = 1;
            snapshot.CorrectAnswerCount = 1;
            snapshot.CorrectQuestionIds = new[] { answered.Id };
            snapshot.UnlockedParts = new[] { PartId.Axle };

            view.Presenter.RebuildFromSnapshot(snapshot);

            Assert.That(
                view.Presenter.UnlockedEntries.Any(entry =>
                    entry.SourceQuestionId == answered.Id),
                Is.True);
            Assert.That(
                view.Presenter.UnlockedEntries.Any(entry =>
                    entry.SourceQuestionId == axleQuestions[0].Id),
                Is.False);
        }

        private KnowledgeView CreateKnowledgeView(ThirdPersonInputLock inputLock)
        {
            var catalogButton = Child("CatalogButton").AddComponent<Button>();
            var popupRoot = Child("PopupRoot");
            var popupTitle = Child("PopupTitle", popupRoot).AddComponent<Text>();
            var popupBody = Child("PopupBody", popupRoot).AddComponent<Text>();
            var popupClose = Child("PopupClose", popupRoot).AddComponent<Button>();
            var catalogRoot = Child("CatalogRoot");
            var catalogBody = Child("CatalogBody", catalogRoot).AddComponent<Text>();
            var catalogClose = Child("CatalogClose", catalogRoot).AddComponent<Button>();
            var presenter = root.AddComponent<WhiteboxKnowledgePresenter>();
            presenter.Configure(
                host,
                inputLock,
                catalogButton,
                popupRoot,
                popupTitle,
                popupBody,
                popupClose,
                catalogRoot,
                catalogBody,
                catalogClose);
            return new KnowledgeView(
                presenter,
                catalogButton,
                popupRoot,
                popupTitle,
                catalogRoot,
                catalogBody);
        }

        private GameObject Child(string name, GameObject parent = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent((parent ?? root).transform, false);
            return child;
        }

        private readonly struct KnowledgeView
        {
            public KnowledgeView(
                WhiteboxKnowledgePresenter presenter,
                Button catalogButton,
                GameObject popupRoot,
                Text popupTitle,
                GameObject catalogRoot,
                Text catalogBody)
            {
                Presenter = presenter;
                CatalogButton = catalogButton;
                PopupRoot = popupRoot;
                PopupTitle = popupTitle;
                CatalogRoot = catalogRoot;
                CatalogBody = catalogBody;
            }

            public WhiteboxKnowledgePresenter Presenter { get; }
            public Button CatalogButton { get; }
            public GameObject PopupRoot { get; }
            public Text PopupTitle { get; }
            public GameObject CatalogRoot { get; }
            public Text CatalogBody { get; }
        }
    }
}
