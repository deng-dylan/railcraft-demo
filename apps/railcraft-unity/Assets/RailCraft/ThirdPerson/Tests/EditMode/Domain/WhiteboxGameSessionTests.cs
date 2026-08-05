using System;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.Tests.EditMode.Domain
{
    public sealed class WhiteboxGameSessionTests
    {
        [Test]
        public void WrongInvalidAndUnknownAnswersDoNotUnlockTheReward()
        {
            var session = new WhiteboxGameSession();
            var question = QuestionFor(session, PartId.Axle);
            var wrongOption = (question.CorrectOptionIndex + 1) % question.Options.Count;

            var wrong = session.SubmitAnswer(question.Id, wrongOption);
            var invalid = session.SubmitAnswer(question.Id, question.Options.Count);
            var unknown = session.SubmitAnswer("missing_question", 0);

            Assert.That(wrong.Status, Is.EqualTo(QuizSubmissionStatus.Incorrect));
            Assert.That(wrong.CorrectOptionIndex, Is.EqualTo(question.CorrectOptionIndex));
            Assert.That(invalid.Status, Is.EqualTo(QuizSubmissionStatus.InvalidOption));
            Assert.That(unknown.Status, Is.EqualTo(QuizSubmissionStatus.UnknownQuestion));
            Assert.That(session.IsPartUnlocked(PartId.Axle), Is.False);
            Assert.That(session.Inventory.Count, Is.Zero);
        }

        [Test]
        public void CorrectAnswerUnlocksPickupAndDuplicateOperationsAreIdempotent()
        {
            var session = new WhiteboxGameSession();
            var question = QuestionFor(session, PartId.Axle);

            var firstAnswer = session.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            var duplicateAnswer = session.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            var firstPickup = session.CollectPart(PartId.Axle);
            var duplicatePickup = session.CollectPart(PartId.Axle);

            Assert.That(firstAnswer.RewardUnlocked, Is.True);
            Assert.That(duplicateAnswer.RewardUnlocked, Is.False);
            Assert.That(firstPickup.Status, Is.EqualTo(PartCollectionStatus.Collected));
            Assert.That(duplicatePickup.Status, Is.EqualTo(PartCollectionStatus.AlreadyCollected));
            Assert.That(session.Inventory.Parts, Is.EqualTo(new[] { PartId.Axle }));
        }

        [Test]
        public void PartInstallationRequiresTheMatchingRecipeAndInventoryItem()
        {
            var session = new WhiteboxGameSession();
            UnlockAndCollect(session, PartId.Carbody);

            var wrongRecipe = session.InstallPart(ModuleId.WheelsetAxlebox, PartId.Carbody);
            var missingItem = session.InstallPart(ModuleId.WheelsetAxlebox, PartId.Axle);

            Assert.That(wrongRecipe.Status, Is.EqualTo(PartInstallationStatus.PartNotInRecipe));
            Assert.That(session.Inventory.Contains(PartId.Carbody), Is.True);
            Assert.That(missingItem.Status, Is.EqualTo(PartInstallationStatus.MissingFromInventory));
            Assert.That(session.GetModuleState(ModuleId.WheelsetAxlebox).InstalledInputCount, Is.Zero);
        }

        [Test]
        public void LeafAssemblyCompletesAfterAllVariableRecipePartsAreInstalled()
        {
            var session = new WhiteboxGameSession();
            var definition = session.Catalog.GetModule(ModuleId.WheelsetAxlebox);

            for (var index = 0; index < definition.RequiredParts.Count; index++)
            {
                var partId = definition.RequiredParts[index];
                UnlockAndCollect(session, partId);
                var result = session.InstallPart(definition.Id, partId);

                Assert.That(result.Status, Is.EqualTo(PartInstallationStatus.Installed));
                Assert.That(result.IsModuleComplete,
                    Is.EqualTo(index == definition.RequiredParts.Count - 1));
            }

            var state = session.GetModuleState(ModuleId.WheelsetAxlebox);
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.InstalledParts, Is.EqualTo(definition.RequiredParts));
            Assert.That(state.RequiredModuleCount, Is.Zero);
        }

        [Test]
        public void ChildAssemblyInstallationEnforcesRecipeCompletionAndIdempotence()
        {
            var session = new WhiteboxGameSession();

            var incomplete = session.InstallModule(
                ModuleId.BogieStructure,
                ModuleId.WheelsetAxlebox);
            CompletePartAssembly(session, ModuleId.WheelsetAxlebox);
            var wrongRecipe = session.InstallModule(
                ModuleId.Landing,
                ModuleId.WheelsetAxlebox);
            var first = session.InstallModule(
                ModuleId.BogieStructure,
                ModuleId.WheelsetAxlebox);
            var duplicate = session.InstallModule(
                ModuleId.BogieStructure,
                ModuleId.WheelsetAxlebox);

            Assert.That(incomplete.Status, Is.EqualTo(ModuleInstallationStatus.ChildModuleIncomplete));
            Assert.That(wrongRecipe.Status, Is.EqualTo(ModuleInstallationStatus.ModuleNotInRecipe));
            Assert.That(first.Status, Is.EqualTo(ModuleInstallationStatus.Installed));
            Assert.That(first.TargetModuleId, Is.EqualTo(ModuleId.BogieStructure));
            Assert.That(first.ChildModuleId, Is.EqualTo(ModuleId.WheelsetAxlebox));
            Assert.That(first.IsTargetModuleComplete, Is.False);
            Assert.That(duplicate.Status, Is.EqualTo(ModuleInstallationStatus.AlreadyInstalled));
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(
                session.IsModuleInstalled(ModuleId.BogieStructure, ModuleId.WheelsetAxlebox),
                Is.True);
        }

        [Test]
        public void BogieStructureRequiresAllThreeChildAssemblies()
        {
            var session = new WhiteboxGameSession();
            CompletePartAssembly(session, ModuleId.WheelsetAxlebox);
            CompletePartAssembly(session, ModuleId.Frame);
            CompletePartAssembly(session, ModuleId.PrimarySuspension);

            var wheelset = session.InstallModule(
                ModuleId.BogieStructure,
                ModuleId.WheelsetAxlebox);
            var frame = session.InstallModule(
                ModuleId.BogieStructure,
                ModuleId.Frame);
            var primary = session.InstallModule(
                ModuleId.BogieStructure,
                ModuleId.PrimarySuspension);

            Assert.That(wheelset.IsTargetModuleComplete, Is.False);
            Assert.That(frame.IsTargetModuleComplete, Is.False);
            Assert.That(primary.IsTargetModuleComplete, Is.True);
            Assert.That(session.IsModuleComplete(ModuleId.BogieStructure), Is.True);
            Assert.That(
                session.GetModuleState(ModuleId.BogieStructure).InstalledModules,
                Is.EqualTo(new[]
                {
                    ModuleId.WheelsetAxlebox,
                    ModuleId.Frame,
                    ModuleId.PrimarySuspension
                }));
        }

        [Test]
        public void LandingCombinesTwoPartsAndTwoCompletedChildAssemblies()
        {
            var session = new WhiteboxGameSession();
            CompletePreLandingAssemblies(session);

            Assert.That(session.CanBeginFinalAssembly, Is.True);
            Assert.That(session.IsLandingComplete, Is.False);

            InstallDirectLandingParts(session);
            var bogie = session.InstallModule(ModuleId.Landing, ModuleId.BogieStructure);
            var secondary = session.InstallModule(ModuleId.Landing, ModuleId.SecondarySuspension);

            Assert.That(bogie.IsTargetModuleComplete, Is.False);
            Assert.That(secondary.IsTargetModuleComplete, Is.True);
            Assert.That(session.IsLandingComplete, Is.True);
            Assert.That(session.AreAllModulesComplete, Is.True);
            Assert.That(session.IsVehicleComplete, Is.False);
            Assert.That(session.CommissioningPhase,
                Is.EqualTo(CommissioningPhase.ReadyForInitialTest));
            Assert.That(session.InstalledVehicleModules,
                Is.EqualTo(new[] { ModuleId.BogieStructure, ModuleId.SecondarySuspension }));
        }

        [Test]
        public void CommissioningIsLockedUntilLandingIsComplete()
        {
            var session = new WhiteboxGameSession();

            var test = session.RunCommissioning();
            var retune = session.PerformRetuning();
            var inspection = session.PerformInspection();

            Assert.That(test.Status, Is.EqualTo(CommissioningStatus.AssemblyIncomplete));
            Assert.That(retune.Status, Is.EqualTo(CommissioningStatus.AssemblyIncomplete));
            Assert.That(inspection.Status, Is.EqualTo(CommissioningStatus.AssemblyIncomplete));
            Assert.That(session.CommissioningPhase, Is.EqualTo(CommissioningPhase.Locked));
        }

        [Test]
        public void CommissioningRunsFailureRetuningInspectionRetestAndServiceFlow()
        {
            var session = new WhiteboxGameSession();
            CompleteLanding(session);

            var initialTest = session.RunCommissioning();
            var retuning = session.PerformRetuning();
            var inspection = session.PerformInspection();
            var retest = session.RunCommissioning();

            AssertResult(
                initialTest,
                CommissioningStatus.Failed,
                CommissioningPhase.NeedsRetuning,
                true,
                false);
            Assert.That(session.CommissioningState.InitialTestAttempted, Is.True);
            AssertResult(
                retuning,
                CommissioningStatus.Retuned,
                CommissioningPhase.ReadyForInspection,
                true,
                false);
            AssertResult(
                inspection,
                CommissioningStatus.Inspected,
                CommissioningPhase.ReadyForRetest,
                true,
                false);
            AssertResult(
                retest,
                CommissioningStatus.Passed,
                CommissioningPhase.InService,
                true,
                true);
            Assert.That(session.IsVehicleComplete, Is.True);

            var repeated = session.RunCommissioning();
            AssertResult(
                repeated,
                CommissioningStatus.Passed,
                CommissioningPhase.InService,
                false,
                true);
        }

        [Test]
        public void CommissioningRejectsStepsPerformedOutOfOrder()
        {
            var session = new WhiteboxGameSession();
            CompleteLanding(session);

            var earlyRetune = session.PerformRetuning();
            session.RunCommissioning();
            var earlyInspection = session.RunCommissioning();
            session.PerformRetuning();
            var duplicateRetune = session.PerformRetuning();

            Assert.That(earlyRetune.Status, Is.EqualTo(CommissioningStatus.InvalidPhase));
            Assert.That(earlyRetune.Changed, Is.False);
            Assert.That(earlyInspection.Status, Is.EqualTo(CommissioningStatus.InvalidPhase));
            Assert.That(duplicateRetune.Status, Is.EqualTo(CommissioningStatus.InvalidPhase));
            Assert.That(session.CommissioningPhase,
                Is.EqualTo(CommissioningPhase.ReadyForInspection));
            Assert.That(session.IsVehicleComplete, Is.False);
        }

        [Test]
        public void SingleArgumentModuleInstallationFindsTheUniqueParentForCompatibility()
        {
            var session = new WhiteboxGameSession();
            CompletePartAssembly(session, ModuleId.WheelsetAxlebox);

            var result = session.InstallModule(ModuleId.WheelsetAxlebox);
            var root = session.InstallModule(ModuleId.Landing);

            Assert.That(result.Status, Is.EqualTo(ModuleInstallationStatus.Installed));
            Assert.That(result.TargetModuleId, Is.EqualTo(ModuleId.BogieStructure));
            Assert.That(result.ModuleId, Is.EqualTo(ModuleId.WheelsetAxlebox));
            Assert.That(root.Status, Is.EqualTo(ModuleInstallationStatus.ModuleNotInRecipe));
        }

        [Test]
        public void ResetClearsInventoryAssemblyHierarchyAndCommissioning()
        {
            var session = new WhiteboxGameSession();
            CompleteLanding(session);
            session.RunCommissioning();
            session.PerformRetuning();
            session.PerformInspection();
            session.RunCommissioning();

            session.Reset();

            Assert.That(session.UnlockedParts, Is.Empty);
            Assert.That(session.CollectedParts, Is.Empty);
            Assert.That(session.Inventory.Count, Is.Zero);
            Assert.That(session.AreAllModulesComplete, Is.False);
            Assert.That(session.CanBeginFinalAssembly, Is.False);
            Assert.That(session.InstalledVehicleModules, Is.Empty);
            Assert.That(session.IsLandingComplete, Is.False);
            Assert.That(session.IsVehicleComplete, Is.False);
            Assert.That(session.CommissioningPhase, Is.EqualTo(CommissioningPhase.Locked));
            Assert.That(session.CommissioningState.InitialTestAttempted, Is.False);
            foreach (var module in session.Catalog.Modules)
            {
                var state = session.GetModuleState(module.Id);
                Assert.That(state.InstalledPartCount, Is.Zero);
                Assert.That(state.InstalledModuleCount, Is.Zero);
                Assert.That(state.IsComplete, Is.False);
            }
        }

        [Test]
        public void UndefinedIdentifiersReturnStableFailures()
        {
            var session = new WhiteboxGameSession();
            var unknownPart = (PartId)999;
            var unknownModule = (ModuleId)999;

            Assert.That(session.CollectPart(unknownPart).Status,
                Is.EqualTo(PartCollectionStatus.UnknownPart));
            Assert.That(session.InstallPart(ModuleId.WheelsetAxlebox, unknownPart).Status,
                Is.EqualTo(PartInstallationStatus.UnknownPart));
            Assert.That(session.InstallPart(unknownModule, PartId.Axle).Status,
                Is.EqualTo(PartInstallationStatus.UnknownModule));
            Assert.That(session.InstallModule(unknownModule).Status,
                Is.EqualTo(ModuleInstallationStatus.UnknownModule));
            Assert.That(
                session.InstallModule(unknownModule, ModuleId.WheelsetAxlebox).Status,
                Is.EqualTo(ModuleInstallationStatus.UnknownTargetModule));
            Assert.That(
                session.InstallModule(ModuleId.BogieStructure, unknownModule).Status,
                Is.EqualTo(ModuleInstallationStatus.UnknownChildModule));
            Assert.That(session.TryGetModuleState(unknownModule, out _), Is.False);
            Assert.That(session.IsModuleComplete(unknownModule), Is.False);
        }

        private static void CompleteLanding(WhiteboxGameSession session)
        {
            CompletePreLandingAssemblies(session);
            InstallDirectLandingParts(session);
            Assert.That(
                session.InstallModule(ModuleId.Landing, ModuleId.BogieStructure).Status,
                Is.EqualTo(ModuleInstallationStatus.Installed));
            Assert.That(
                session.InstallModule(ModuleId.Landing, ModuleId.SecondarySuspension).Status,
                Is.EqualTo(ModuleInstallationStatus.Installed));
            Assert.That(session.IsLandingComplete, Is.True);
        }

        private static void CompletePreLandingAssemblies(WhiteboxGameSession session)
        {
            CompletePartAssembly(session, ModuleId.WheelsetAxlebox);
            CompletePartAssembly(session, ModuleId.Frame);
            CompletePartAssembly(session, ModuleId.PrimarySuspension);
            Assert.That(
                session.InstallModule(ModuleId.BogieStructure, ModuleId.WheelsetAxlebox).Status,
                Is.EqualTo(ModuleInstallationStatus.Installed));
            Assert.That(
                session.InstallModule(ModuleId.BogieStructure, ModuleId.Frame).Status,
                Is.EqualTo(ModuleInstallationStatus.Installed));
            Assert.That(
                session.InstallModule(ModuleId.BogieStructure, ModuleId.PrimarySuspension).Status,
                Is.EqualTo(ModuleInstallationStatus.Installed));
            CompletePartAssembly(session, ModuleId.SecondarySuspension);
        }

        private static void InstallDirectLandingParts(WhiteboxGameSession session)
        {
            foreach (var partId in session.Catalog.GetModule(ModuleId.Landing).RequiredParts)
            {
                UnlockAndCollect(session, partId);
                Assert.That(
                    session.InstallPart(ModuleId.Landing, partId).Status,
                    Is.EqualTo(PartInstallationStatus.Installed));
            }
        }

        private static void CompletePartAssembly(
            WhiteboxGameSession session,
            ModuleId moduleId)
        {
            var module = session.Catalog.GetModule(moduleId);
            Assert.That(module.RequiredParts, Is.Not.Empty, moduleId.ToString());
            foreach (var partId in module.RequiredParts)
            {
                UnlockAndCollect(session, partId);
                var installation = session.InstallPart(moduleId, partId);
                Assert.That(installation.Status, Is.EqualTo(PartInstallationStatus.Installed));
            }
            Assert.That(session.IsModuleComplete(moduleId), Is.True);
        }

        private static void UnlockAndCollect(WhiteboxGameSession session, PartId partId)
        {
            var question = QuestionFor(session, partId);
            var answer = session.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            Assert.That(answer.Status, Is.EqualTo(QuizSubmissionStatus.Correct));
            var collection = session.CollectPart(partId);
            Assert.That(collection.Status, Is.EqualTo(PartCollectionStatus.Collected));
        }

        private static QuizQuestionDefinition QuestionFor(
            WhiteboxGameSession session,
            PartId partId)
        {
            foreach (var question in session.Catalog.Questions)
            {
                if (question.RewardPart == partId)
                    return question;
            }

            throw new InvalidOperationException($"Missing test question for {partId}.");
        }

        private static void AssertResult(
            CommissioningResult result,
            CommissioningStatus expectedStatus,
            CommissioningPhase expectedPhase,
            bool expectedChanged,
            bool expectedPassed)
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Phase, Is.EqualTo(expectedPhase));
            Assert.That(result.Changed, Is.EqualTo(expectedChanged));
            Assert.That(result.Passed, Is.EqualTo(expectedPassed));
        }
    }
}
