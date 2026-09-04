using System;
using System.Collections.Generic;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.Tests.EditMode.Domain
{
    public sealed class WhiteboxWorkPackageSessionTests
    {
        [Test]
        public void MaterialPackagesExposeStableAssemblyTargetsAndGateQuestions()
        {
            var expectedModules = new Dictionary<WorkPackageId, ModuleId>
            {
                { WorkPackageId.WheelsetAxlebox, ModuleId.WheelsetAxlebox },
                { WorkPackageId.FrameAndBrakeTraction, ModuleId.Frame },
                { WorkPackageId.PrimarySuspension, ModuleId.PrimarySuspension },
                { WorkPackageId.SecondarySuspension, ModuleId.SecondarySuspension },
                { WorkPackageId.CarbodyAndLanding, ModuleId.Landing }
            };

            foreach (var pair in expectedModules)
            {
                var package = WhiteboxWorkPackageCatalog.Get(pair.Key);
                Assert.That(package.IsMaterialPackage, Is.True, package.Key);
                Assert.That(package.AssemblyModule, Is.EqualTo(pair.Value), package.Key);
                Assert.That(package.CoreQuestionIds, Is.Not.Empty, package.Key);
                Assert.That(package.RequiredQuestionCount,
                    Is.EqualTo(package.CoreQuestionIds.Count), package.Key);

                foreach (var questionId in package.CoreQuestionIds)
                {
                    Assert.That(package.ContainsCoreQuestion(questionId), Is.True, questionId);
                    Assert.That(
                        WhiteboxWorkPackageCatalog.TryGetForCoreQuestion(questionId, out var owner),
                        Is.True,
                        questionId);
                    Assert.That(owner.Id, Is.EqualTo(pair.Key), questionId);
                }
            }

            var commissioning = WhiteboxWorkPackageCatalog.Get(WorkPackageId.Commissioning);
            Assert.That(commissioning.IsMaterialPackage, Is.False);
            Assert.That(commissioning.AssemblyModule, Is.Null);
            Assert.That(commissioning.CoreQuestionIds, Is.Empty);
        }

        [Test]
        public void PackageAnswersUnlockAllPartsOnlyAfterEveryGateQuestion()
        {
            var session = new WhiteboxGameSession();
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.WheelsetAxlebox);

            for (var index = 0; index < package.CoreQuestionIds.Count; index++)
            {
                var questionId = package.CoreQuestionIds[index];
                var question = session.Catalog.GetQuestion(questionId);
                var result = session.SubmitWorkPackageAnswer(
                    package.Id,
                    questionId,
                    question.CorrectOptionIndex);

                Assert.That(result.Status, Is.EqualTo(WorkPackageAnswerStatus.Correct));
                Assert.That(result.IsCorrect, Is.True);
                Assert.That(result.QuestionId, Is.EqualTo(questionId));
                Assert.That(result.AnsweredQuestionCount, Is.EqualTo(index + 1));
                Assert.That(result.RequiredQuestionCount,
                    Is.EqualTo(package.RequiredQuestionCount));

                var expectedComplete = index == package.CoreQuestionIds.Count - 1;
                Assert.That(result.KnowledgeComplete, Is.EqualTo(expectedComplete));
                Assert.That(result.PackageUnlocked, Is.EqualTo(expectedComplete));
                Assert.That(session.IsWorkPackageKnowledgeComplete(package.Id),
                    Is.EqualTo(expectedComplete));
                Assert.That(session.IsWorkPackageUnlocked(package.Id),
                    Is.EqualTo(expectedComplete));
            }

            Assert.That(session.UnlockedParts,
                Is.EquivalentTo(package.RequiredParts));
            Assert.That(session.Inventory.Parts, Is.Empty);

            var duplicate = session.SubmitWorkPackageAnswer(
                package.Id,
                package.CoreQuestionIds[0],
                session.Catalog.GetQuestion(package.CoreQuestionIds[0]).CorrectOptionIndex);
            Assert.That(duplicate.Status, Is.EqualTo(WorkPackageAnswerStatus.Correct));
            Assert.That(duplicate.PackageUnlocked, Is.False);
            Assert.That(session.UnlockedParts,
                Is.EquivalentTo(package.RequiredParts));
        }

        [Test]
        public void WrongOrForeignPackageAnswersDoNotUnlockMaterial()
        {
            var session = new WhiteboxGameSession();
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.PrimarySuspension);
            var questionId = package.CoreQuestionIds[0];
            var question = session.Catalog.GetQuestion(questionId);
            var wrongOption = (question.CorrectOptionIndex + 1) % question.Options.Count;

            var wrong = session.SubmitWorkPackageAnswer(package.Id, questionId, wrongOption);
            Assert.That(wrong.Status, Is.EqualTo(WorkPackageAnswerStatus.Incorrect));
            Assert.That(wrong.KnowledgeComplete, Is.False);
            Assert.That(session.IsWorkPackageUnlocked(package.Id), Is.False);
            Assert.That(session.UnlockedParts, Is.Empty);

            var foreign = session.SubmitWorkPackageAnswer(
                package.Id,
                WhiteboxWorkPackageCatalog.Get(WorkPackageId.WheelsetAxlebox).CoreQuestionIds[0],
                0);
            Assert.That(foreign.Status,
                Is.EqualTo(WorkPackageAnswerStatus.QuestionNotInPackage));
            Assert.That(session.GetWorkPackageAnsweredQuestionCount(package.Id), Is.Zero);
            Assert.That(session.UnlockedParts, Is.Empty);
        }

        [Test]
        public void CollectWorkPackageIsAtomicAndIdempotent()
        {
            var session = new WhiteboxGameSession();
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.SecondarySuspension);

            Assert.That(
                session.CollectWorkPackage(package.Id).Status,
                Is.EqualTo(WorkPackageCollectionStatus.Locked));

            AnswerAllPackageQuestions(session, package);
            var collected = session.CollectWorkPackage(package.Id);

            Assert.That(collected.Status, Is.EqualTo(WorkPackageCollectionStatus.Collected));
            Assert.That(collected.Accepted, Is.True);
            Assert.That(collected.Changed, Is.True);
            Assert.That(collected.CollectedPartCount,
                Is.EqualTo(package.RequiredParts.Count));
            Assert.That(collected.CollectedParts,
                Is.EquivalentTo(package.RequiredParts));
            Assert.That(session.IsWorkPackageCollected(package.Id), Is.True);
            Assert.That(session.Inventory.Parts,
                Is.EquivalentTo(package.RequiredParts));

            var duplicate = session.CollectWorkPackage(package.Id);
            Assert.That(duplicate.Status,
                Is.EqualTo(WorkPackageCollectionStatus.AlreadyCollected));
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(session.Inventory.Parts,
                Is.EquivalentTo(package.RequiredParts));
        }

        [Test]
        public void InstallWorkPackageConsumesAllInputsInOneOperation()
        {
            var session = new WhiteboxGameSession();
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.FrameAndBrakeTraction);
            var moduleId = package.AssemblyModule.Value;

            AnswerAllPackageQuestions(session, package);
            Assert.That(session.CollectWorkPackage(package.Id).Accepted, Is.True);

            var wrongModule = session.InstallWorkPackage(ModuleId.WheelsetAxlebox, package.Id);
            Assert.That(wrongModule.Status,
                Is.EqualTo(WorkPackageInstallationStatus.PackageNotInModule));
            Assert.That(session.Inventory.Parts,
                Is.EquivalentTo(package.RequiredParts));
            Assert.That(session.GetModuleState(moduleId).InstalledParts, Is.Empty);

            var installed = session.InstallWorkPackage(moduleId, package.Id);
            Assert.That(installed.Status,
                Is.EqualTo(WorkPackageInstallationStatus.Installed));
            Assert.That(installed.Accepted, Is.True);
            Assert.That(installed.Changed, Is.True);
            Assert.That(installed.InstalledPartCount,
                Is.EqualTo(package.RequiredParts.Count));
            Assert.That(installed.InstalledParts,
                Is.EquivalentTo(package.RequiredParts));
            Assert.That(session.GetModuleState(moduleId).InstalledParts,
                Is.EquivalentTo(package.RequiredParts));
            Assert.That(session.Inventory.Parts, Is.Empty);

            var duplicate = session.InstallWorkPackage(moduleId, package.Id);
            Assert.That(duplicate.Status,
                Is.EqualTo(WorkPackageInstallationStatus.AlreadyInstalled));
            Assert.That(duplicate.Changed, Is.False);
        }

        [Test]
        public void InstallWorkPackageDoesNotPartiallyConsumeMissingInputs()
        {
            var session = new WhiteboxGameSession();
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.WheelsetAxlebox);
            var moduleId = package.AssemblyModule.Value;
            AnswerAllPackageQuestions(session, package);

            var result = session.InstallWorkPackage(moduleId, package.Id);

            Assert.That(result.Status,
                Is.EqualTo(WorkPackageInstallationStatus.MissingFromInventory));
            Assert.That(result.Changed, Is.False);
            Assert.That(session.Inventory.Parts, Is.Empty);
            Assert.That(session.GetModuleState(moduleId).InstalledParts, Is.Empty);
        }

        [Test]
        public void PackageStateRoundTripsThroughTheLegacySnapshotSchema()
        {
            var source = new WhiteboxGameSession();
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.WheelsetAxlebox);
            AnswerAllPackageQuestions(source, package);
            Assert.That(source.CollectWorkPackage(package.Id).Accepted, Is.True);
            Assert.That(source.InstallWorkPackage(
                    package.AssemblyModule.Value,
                    package.Id).Accepted,
                Is.True);

            var restored = new WhiteboxGameSession();
            restored.RestoreSnapshot(source.ExportSnapshot());

            Assert.That(restored.IsWorkPackageKnowledgeComplete(package.Id), Is.True);
            Assert.That(restored.IsWorkPackageUnlocked(package.Id), Is.True);
            Assert.That(restored.IsWorkPackageCollected(package.Id), Is.True);
            Assert.That(restored.GetModuleState(package.AssemblyModule.Value).IsComplete, Is.True);
            Assert.That(restored.Inventory.Parts, Is.Empty);
        }

        [Test]
        public void FirstPackageGateAnswerCanBeSavedAndRestoredBeforeUnlock()
        {
            var source = new WhiteboxGameSession();
            var package = WhiteboxWorkPackageCatalog.Get(WorkPackageId.PrimarySuspension);
            var question = source.Catalog.GetQuestion(package.CoreQuestionIds[0]);

            var answer = source.SubmitWorkPackageAnswer(
                package.Id,
                question.Id,
                question.CorrectOptionIndex);
            Assert.That(answer.IsCorrect, Is.True);
            Assert.That(answer.KnowledgeComplete, Is.False);
            Assert.That(source.UnlockedParts, Is.Empty);

            var restored = new WhiteboxGameSession();
            restored.RestoreSnapshot(source.ExportSnapshot());

            Assert.That(restored.GetWorkPackageAnsweredQuestionCount(package.Id), Is.EqualTo(1));
            Assert.That(restored.IsWorkPackageKnowledgeComplete(package.Id), Is.False);
            Assert.That(restored.IsWorkPackageUnlocked(package.Id), Is.False);
            Assert.That(restored.CorrectQuestionIds, Does.Contain(question.Id));

            var secondQuestion = restored.Catalog.GetQuestion(package.CoreQuestionIds[1]);
            var resumedAnswer = restored.SubmitWorkPackageAnswer(
                package.Id,
                secondQuestion.Id,
                secondQuestion.CorrectOptionIndex);
            Assert.That(resumedAnswer.IsCorrect, Is.True);
            Assert.That(resumedAnswer.PackageNewlyUnlocked, Is.True);
            Assert.That(resumedAnswer.IsPackageUnlocked, Is.True);
            Assert.That(restored.IsWorkPackageKnowledgeComplete(package.Id), Is.True);
            Assert.That(restored.UnlockedParts, Is.EquivalentTo(package.RequiredParts));
        }

        [Test]
        public void SnapshotKeepsQuestionHistoryWhenRewardRoutingChanges()
        {
            var source = new WhiteboxGameSession();
            var bearingQuestion = source.Catalog.GetQuestion("bank_mc05");
            Assert.That(bearingQuestion.RewardPart, Is.EqualTo(PartId.Bearing));
            Assert.That(
                source.SubmitAnswer(
                    bearingQuestion.Id,
                    bearingQuestion.CorrectOptionIndex).IsCorrect,
                Is.True);

            var legacySnapshot = source.ExportSnapshot();
            // In the previous content route bank_mc03 rewarded Bearing. Keep
            // that old material state while restoring its exact answer id.
            legacySnapshot.CorrectQuestionIds = new[] { "bank_mc03" };

            var restored = new WhiteboxGameSession();
            restored.RestoreSnapshot(legacySnapshot);

            Assert.That(restored.UnlockedParts, Is.EqualTo(new[] { PartId.Bearing }));
            Assert.That(restored.CorrectQuestionIds, Is.EqualTo(new[] { "bank_mc03" }));
        }

        [Test]
        public void PackageOperationsRejectUnknownAndNonMaterialPackages()
        {
            var session = new WhiteboxGameSession();
            var unknownPackage = (WorkPackageId)999;

            Assert.That(
                session.IsWorkPackageKnowledgeComplete(unknownPackage),
                Is.False);
            Assert.That(
                session.SubmitWorkPackageAnswer(unknownPackage, "missing", 0).Status,
                Is.EqualTo(WorkPackageAnswerStatus.UnknownWorkPackage));
            Assert.That(
                session.CollectWorkPackage(unknownPackage).Status,
                Is.EqualTo(WorkPackageCollectionStatus.UnknownWorkPackage));
            Assert.That(
                session.InstallWorkPackage(ModuleId.Landing, unknownPackage).Status,
                Is.EqualTo(WorkPackageInstallationStatus.UnknownWorkPackage));

            var commissioning = WorkPackageId.Commissioning;
            Assert.That(
                session.SubmitWorkPackageAnswer(commissioning, "missing", 0).Status,
                Is.EqualTo(WorkPackageAnswerStatus.NotMaterialPackage));
            Assert.That(
                session.CollectWorkPackage(commissioning).Status,
                Is.EqualTo(WorkPackageCollectionStatus.NotMaterialPackage));
            Assert.That(
                session.InstallWorkPackage(ModuleId.Landing, commissioning).Status,
                Is.EqualTo(WorkPackageInstallationStatus.NotMaterialPackage));
        }

        private static void AnswerAllPackageQuestions(
            WhiteboxGameSession session,
            WorkPackageDefinition package)
        {
            foreach (var questionId in package.CoreQuestionIds)
            {
                var question = session.Catalog.GetQuestion(questionId);
                var answer = session.SubmitWorkPackageAnswer(
                    package.Id,
                    questionId,
                    question.CorrectOptionIndex);
                Assert.That(answer.Status, Is.EqualTo(WorkPackageAnswerStatus.Correct));
            }

            Assert.That(session.IsWorkPackageKnowledgeComplete(package.Id), Is.True);
            Assert.That(session.IsWorkPackageUnlocked(package.Id), Is.True);
        }
    }
}
