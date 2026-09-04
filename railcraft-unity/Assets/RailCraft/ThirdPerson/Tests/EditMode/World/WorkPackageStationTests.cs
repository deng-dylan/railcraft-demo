using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class WorkPackageStationTests
    {
        private GameObject root;
        private WhiteboxGameSessionHost host;
        private DomainWorldGameSession worldSession;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("WorkPackageStationTests");
            host = root.AddComponent<WhiteboxGameSessionHost>();
            worldSession = new DomainWorldGameSession();
            host.Configure(worldSession, "测试材料包");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void PackageStationUsesTwoKnowledgeQuestionsAndOnePickup()
        {
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.WheelsetAxlebox);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var dialog = new FakeQuizDialog();
            var reward = Child("WheelsetPackage");
            var station = root.AddComponent<QuizPartStation>();
            station.ConfigureWorkPackage(
                host,
                inputLock,
                null,
                package.Id,
                package.CoreQuestionIds
                    .Select(id => Presentation(id))
                    .ToArray(),
                reward,
                "轮对轴箱知识工位",
                "前往轮对轴箱装配台");
            station.SetQuizDialogForTests(dialog);

            Assert.That(station.UsesWorkPackage, Is.True);
            Assert.That(station.RewardPartCount, Is.EqualTo(1));
            Assert.That(station.SubPartCount, Is.EqualTo(package.RequiredParts.Count));
            Assert.That(station.KnowledgeQuestionCount, Is.EqualTo(2));
            Assert.That(station.RewardUnlocked, Is.False);

            var context = new InteractionContext(root);
            station.Interact(context);
            Assert.That(dialog.IsOpen, Is.True);
            dialog.Select(dialog.LastQuestion.Options.Count - 1);
            // The presentation uses identity order, so choose the actual
            // correct option explicitly if the first click was not correct.
            if (station.IsQuizOpen)
                dialog.Select(worldSession.DomainSession.Catalog
                    .GetQuestion(dialog.LastQuestion.QuestionId).CorrectOptionIndex);
            Assert.That(station.AnsweredKnowledgeQuestionCount, Is.EqualTo(1));
            Assert.That(station.RewardUnlocked, Is.False);

            station.Interact(context);
            var secondQuestion = worldSession.DomainSession.Catalog
                .GetQuestion(dialog.LastQuestion.QuestionId);
            dialog.Select(secondQuestion.CorrectOptionIndex);
            Assert.That(station.KnowledgeComplete, Is.True);
            Assert.That(station.RewardUnlocked, Is.True);
            Assert.That(reward.activeSelf, Is.True);

            station.Interact(context);
            Assert.That(station.IsCollected, Is.True);
            Assert.That(worldSession.InventoryParts.Count, Is.EqualTo(package.RequiredParts.Count));
            Assert.That(reward.activeSelf, Is.False);

            station.Interact(context);
            Assert.That(worldSession.InventoryParts.Count, Is.EqualTo(package.RequiredParts.Count));
        }

        [Test]
        public void PackageStationRotatesItsFirstGateQuestionOnFreshSessionReset()
        {
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.PrimarySuspension);
            var station = root.AddComponent<QuizPartStation>();
            station.ConfigureWorkPackage(
                host,
                root.AddComponent<ThirdPersonInputLock>(),
                null,
                package.Id,
                package.CoreQuestionIds.Select(Presentation).ToArray(),
                Child("PrimaryPackage"),
                "一系悬挂知识工位",
                "前往装配台");

            Assert.That(station.CurrentQuestion.QuestionId,
                Is.EqualTo(package.CoreQuestionIds[0]));

            host.ResetSession();
            Assert.That(station.CurrentQuestion.QuestionId,
                Is.EqualTo(package.CoreQuestionIds[1]));

            host.ResetSession();
            Assert.That(station.CurrentQuestion.QuestionId,
                Is.EqualTo(package.CoreQuestionIds[0]));
        }

        [Test]
        public void PackageAssemblyStationInstallsAllChildPartsInOneAction()
        {
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.FrameAndBrakeTraction);
            AnswerPackage(package);

            var parts = package.RequiredParts.ToArray();
            var slots = new Transform[parts.Length];
            var visuals = new GameObject[parts.Length];
            for (var index = 0; index < parts.Length; index++)
            {
                slots[index] = Child($"Slot_{index}").transform;
                visuals[index] = Child($"Visual_{index}");
            }

            var station = root.AddComponent<ModuleAssemblyStation>();
            station.ConfigureWorkPackage(
                host,
                package.AssemblyModule.Value,
                package.Id,
                "构架装配台",
                parts,
                slots,
                visuals,
                Child("Complete"),
                "继续下一工序");

            Assert.That(station.RequiredPartCount, Is.EqualTo(1));
            Assert.That(station.SubPartCount, Is.EqualTo(parts.Length));
            station.Interact(new InteractionContext(root));

            Assert.That(station.IsComplete, Is.True);
            Assert.That(station.InstalledPartCount, Is.EqualTo(1));
            Assert.That(station.RequiredPartCount, Is.EqualTo(1));
            Assert.That(worldSession.InventoryParts, Is.Empty);
            foreach (var part in parts)
                Assert.That(worldSession.IsPartInstalled(package.AssemblyModule.Value, part), Is.True);
        }

        [Test]
        public void PackageCompletionDoesNotRepeatMilestonesForLegacyInstalledParts()
        {
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.WheelsetAxlebox);
            AnswerPackage(package);
            var previouslyInstalled = package.RequiredParts[0];
            Assert.That(
                host.InstallPart(package.AssemblyModule.Value, previouslyInstalled).Accepted,
                Is.True);

            var observedParts = new System.Collections.Generic.List<PartId>();
            host.MilestoneReached += milestone =>
            {
                if (milestone.Kind == WhiteboxMilestoneKind.PartInstalled)
                    observedParts.Add(milestone.PartId);
            };

            var result = host.InstallWorkPackage(package.AssemblyModule.Value, package.Id);

            Assert.That(result.Accepted, Is.True);
            Assert.That(observedParts, Has.Count.EqualTo(package.RequiredParts.Count - 1));
            Assert.That(observedParts, Has.None.EqualTo(previouslyInstalled));
        }

        [Test]
        public void LandingTreatsCarbodyPackageAsOnePlayerFacingInput()
        {
            foreach (var packageId in new[]
            {
                WorkPackageId.WheelsetAxlebox,
                WorkPackageId.FrameAndBrakeTraction,
                WorkPackageId.PrimarySuspension,
                WorkPackageId.SecondarySuspension
            })
            {
                var package = WhiteboxWorkPackageCatalog.Get(packageId);
                AnswerPackage(package);
                Assert.That(
                    host.InstallWorkPackage(package.AssemblyModule.Value, package.Id).Accepted,
                    Is.True,
                    package.Key);
            }

            foreach (var child in new[]
            {
                ModuleId.WheelsetAxlebox,
                ModuleId.Frame,
                ModuleId.PrimarySuspension
            })
            {
                Assert.That(
                    host.InstallModule(ModuleId.BogieStructure, child).Accepted,
                    Is.True,
                    child.ToString());
            }

            var carbodyPackage = WhiteboxWorkPackageCatalog.Get(
                WorkPackageId.CarbodyAndLanding);
            AnswerPackage(carbodyPackage);
            var modules = new[] { ModuleId.BogieStructure, ModuleId.SecondarySuspension };
            var parts = carbodyPackage.RequiredParts.ToArray();
            var moduleSlots = modules.Select((_, index) => Child($"ModuleSlot_{index}").transform).ToArray();
            var partSlots = parts.Select((_, index) => Child($"PartSlot_{index}").transform).ToArray();
            var moduleVisuals = modules.Select((_, index) => Child($"ModuleVisual_{index}")).ToArray();
            var partVisuals = parts.Select((_, index) => Child($"PartVisual_{index}")).ToArray();
            var completed = Child("LandingCompleted");
            var station = root.AddComponent<FinalAssemblyStation>();
            station.ConfigureWorkPackage(
                host,
                ModuleId.Landing,
                carbodyPackage.Id,
                "落车工位",
                modules,
                parts,
                moduleSlots,
                partSlots,
                moduleVisuals,
                partVisuals,
                completed);

            Assert.That(station.UsesWorkPackage, Is.True);
            Assert.That(station.RequiredInputCount, Is.EqualTo(3));
            Assert.That(station.SubPartInputCount, Is.EqualTo(2));

            var context = new InteractionContext(root);
            station.Interact(context);
            station.Interact(context);
            station.Interact(context);

            Assert.That(station.InstalledInputCount, Is.EqualTo(3));
            Assert.That(station.IsLandingComplete, Is.True);
            Assert.That(completed.activeSelf, Is.True);
            Assert.That(partVisuals.Any(visual => visual.activeSelf), Is.False);
        }

        private void AnswerPackage(WorkPackageDefinition package)
        {
            foreach (var questionId in package.CoreQuestionIds)
            {
                var question = worldSession.DomainSession.Catalog.GetQuestion(questionId);
                var answer = worldSession.DomainSession.SubmitWorkPackageAnswer(
                    package.Id,
                    questionId,
                    question.CorrectOptionIndex);
                Assert.That(answer.IsCorrect, Is.True, questionId);
            }

            Assert.That(worldSession.DomainSession.CollectWorkPackage(package.Id).Accepted, Is.True);
        }

        private QuizQuestionPresentation Presentation(string questionId)
        {
            var question = worldSession.DomainSession.Catalog.GetQuestion(questionId);
            return new QuizQuestionPresentation(
                question.Id,
                question.Prompt,
                question.Options,
                question.Explanation,
                question.RewardPart);
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child;
        }

        private sealed class FakeQuizDialog : IQuizDialog
        {
            private Action<int> selection;
            private Action cancelled;

            public bool IsOpen { get; private set; }
            public QuizQuestionPresentation LastQuestion { get; private set; }

            public void Present(
                QuizQuestionPresentation question,
                Action<int> optionSelected,
                Action cancelledCallback)
            {
                LastQuestion = question;
                selection = optionSelected;
                cancelled = cancelledCallback;
                IsOpen = true;
            }

            public void SetFeedback(string message)
            {
            }

            public void Dismiss()
            {
                IsOpen = false;
                selection = null;
                cancelled = null;
            }

            public void Select(int index)
            {
                selection?.Invoke(index);
            }
        }
    }
}
