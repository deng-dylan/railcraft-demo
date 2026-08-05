using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.Tests.EditMode.Domain
{
    public sealed class EngineeringKnowledgeCatalogTests
    {
        [Test]
        public void DefaultCatalogIncludesQuestionsPartsAssembliesCommissioningAndOverview()
        {
            var catalog = EngineeringKnowledgeCatalog.CreateDefault();

            Assert.That(catalog.Entries.Count, Is.EqualTo(83));
            Assert.That(
                catalog.GetEntriesForCategory(KnowledgeEntryCategory.QuestionExplanation).Count,
                Is.EqualTo(58));
            Assert.That(
                catalog.GetEntriesForCategory(KnowledgeEntryCategory.Part).Count,
                Is.EqualTo(14));
            Assert.That(
                catalog.GetEntriesForCategory(KnowledgeEntryCategory.Assembly).Count,
                Is.EqualTo(6));
            Assert.That(
                catalog.GetEntriesForCategory(KnowledgeEntryCategory.Commissioning).Count,
                Is.EqualTo(4));
            Assert.That(
                catalog.GetEntriesForCategory(KnowledgeEntryCategory.VehicleOverview).Count,
                Is.EqualTo(1));
        }

        [Test]
        public void EntryIdsOrdersAndCopyAreStableAndComplete()
        {
            var catalog = EngineeringKnowledgeCatalog.CreateDefault();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();

            foreach (var entry in catalog.Entries)
            {
                Assert.That(ids.Add(entry.Id), Is.True, entry.Id);
                Assert.That(orders.Add(entry.UnlockOrder), Is.True, entry.Id);
                Assert.That(entry.Title, Is.Not.Null.And.Not.Empty, entry.Id);
                Assert.That(entry.Body, Is.Not.Null.And.Not.Empty, entry.Id);
                Assert.That(entry.UnlockOrder, Is.GreaterThan(0), entry.Id);
            }

            Assert.That(catalog.Entries.Select(entry => entry.UnlockOrder), Is.Ordered);
            Assert.That(catalog.GetEntry("knowledge_part_axle").RelatedPart,
                Is.EqualTo(PartId.Axle));
            Assert.That(catalog.GetEntry("knowledge_question_bank_mc01").SourceQuestionId,
                Is.EqualTo("bank_mc01"));
            Assert.That(catalog.GetEntry("knowledge_module_landing").RelatedModule,
                Is.EqualTo(ModuleId.Landing));
        }

        [Test]
        public void EveryQuestionAndPartHasDiscoverableKnowledge()
        {
            var gameCatalog = WhiteboxGameCatalog.CreateDefault();
            var knowledgeCatalog = EngineeringKnowledgeCatalog.CreateDefault(gameCatalog);

            foreach (var part in gameCatalog.Parts)
            {
                var entries = knowledgeCatalog.GetEntriesForPart(part.Id);
                Assert.That(
                    entries.Any(entry => entry.Category == KnowledgeEntryCategory.Part),
                    Is.True,
                    part.Id.ToString());
                Assert.That(
                    entries.Any(entry =>
                        entry.Category == KnowledgeEntryCategory.QuestionExplanation),
                    Is.True,
                    part.Id.ToString());
            }

            foreach (var question in gameCatalog.Questions)
            {
                var expectedId = "knowledge_question_" + question.Id;
                var entry = knowledgeCatalog.GetEntry(expectedId);
                Assert.That(entry.SourceQuestionId, Is.EqualTo(question.Id));
                Assert.That(entry.RelatedPart, Is.EqualTo(question.RewardPart));
                Assert.That(entry.Body, Is.EqualTo(question.Explanation));
            }
        }

        [Test]
        public void EveryAssemblyEntryNamesAllRecipeInputs()
        {
            var gameCatalog = WhiteboxGameCatalog.CreateDefault();
            var knowledgeCatalog = EngineeringKnowledgeCatalog.CreateDefault(gameCatalog);

            foreach (var module in gameCatalog.Modules)
            {
                var entries = knowledgeCatalog.GetEntriesForModule(module.Id);
                Assert.That(entries.Count, Is.EqualTo(1), module.Id.ToString());
                var body = entries[0].Body;
                foreach (var partId in module.RequiredParts)
                {
                    Assert.That(
                        body,
                        Does.Contain(gameCatalog.GetPart(partId).DisplayName),
                        module.Id.ToString());
                }
                foreach (var moduleId in module.RequiredModules)
                {
                    Assert.That(
                        body,
                        Does.Contain(gameCatalog.GetModule(moduleId).DisplayName),
                        module.Id.ToString());
                }
            }
        }

        [Test]
        public void CorrectAnswerUnlocksItsExplanationAndPartOnce()
        {
            var gameCatalog = WhiteboxGameCatalog.CreateDefault();
            var catalog = EngineeringKnowledgeCatalog.CreateDefault(gameCatalog);
            var progress = new EngineeringKnowledgeProgress(catalog);
            var question = gameCatalog.Questions[0];

            var first = progress.RecordCorrectAnswer(question.Id);
            var duplicate = progress.RecordCorrectAnswer(question.Id);
            var unknown = progress.RecordCorrectAnswer("missing_question");

            Assert.That(first.Changed, Is.True);
            Assert.That(first.NewlyUnlockedCount, Is.EqualTo(2));
            Assert.That(
                first.NewlyUnlockedEntries.Select(entry => entry.Category),
                Is.EquivalentTo(new[]
                {
                    KnowledgeEntryCategory.Part,
                    KnowledgeEntryCategory.QuestionExplanation
                }));
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(duplicate.NewlyUnlockedEntries, Is.Empty);
            Assert.That(unknown.Changed, Is.False);
            Assert.That(progress.UnlockedCount, Is.EqualTo(2));
        }

        [Test]
        public void CompletedModulesAndCommissioningPhasesUnlockInIdempotentSteps()
        {
            var progress = new EngineeringKnowledgeProgress(
                EngineeringKnowledgeCatalog.CreateDefault());

            var assembly = progress.RecordCompletedModule(ModuleId.WheelsetAxlebox);
            var duplicateAssembly = progress.RecordCompletedModule(ModuleId.WheelsetAxlebox);
            var locked = progress.RecordCommissioningPhase(CommissioningPhase.Locked);
            var initialFailure = progress.RecordCommissioningPhase(
                CommissioningPhase.NeedsRetuning);

            Assert.That(assembly.NewlyUnlockedCount, Is.EqualTo(1));
            Assert.That(assembly.NewlyUnlockedEntries[0].RelatedModule,
                Is.EqualTo(ModuleId.WheelsetAxlebox));
            Assert.That(duplicateAssembly.Changed, Is.False);
            Assert.That(locked.Changed, Is.False);
            Assert.That(initialFailure.NewlyUnlockedCount, Is.EqualTo(1));
            Assert.That(initialFailure.NewlyUnlockedEntries[0].RelatedCommissioningPhase,
                Is.EqualTo(CommissioningPhase.NeedsRetuning));
        }

        [Test]
        public void VehicleCompletionUnlocksCatalogEntranceAndResetClosesIt()
        {
            var catalog = EngineeringKnowledgeCatalog.CreateDefault();
            var progress = new EngineeringKnowledgeProgress(catalog);

            progress.RecordCommissioningPhase(CommissioningPhase.InService);
            Assert.That(progress.IsCatalogEntranceUnlocked, Is.False);

            var completed = progress.RecordVehicleCompleted();
            var duplicate = progress.RecordVehicleCompleted();

            Assert.That(completed.Changed, Is.True);
            Assert.That(completed.CatalogEntranceUnlocked, Is.True);
            Assert.That(completed.NewlyUnlockedCount, Is.EqualTo(1));
            Assert.That(completed.NewlyUnlockedEntries[0].Id,
                Is.EqualTo("knowledge_vehicle_complete"));
            Assert.That(progress.IsCatalogEntranceUnlocked, Is.True);
            Assert.That(duplicate.Changed, Is.False);

            progress.Reset();

            Assert.That(progress.IsCatalogEntranceUnlocked, Is.False);
            Assert.That(progress.UnlockedEntries, Is.Empty);
        }

        [Test]
        public void UnlockedEntriesAlwaysFollowCatalogUnlockOrder()
        {
            var gameCatalog = WhiteboxGameCatalog.CreateDefault();
            var progress = new EngineeringKnowledgeProgress(
                EngineeringKnowledgeCatalog.CreateDefault(gameCatalog));

            progress.RecordVehicleCompleted();
            progress.RecordCompletedModule(ModuleId.Landing);
            progress.RecordCorrectAnswer(gameCatalog.Questions[57].Id);
            progress.RecordCorrectAnswer(gameCatalog.Questions[0].Id);

            Assert.That(
                progress.UnlockedEntries.Select(entry => entry.UnlockOrder),
                Is.Ordered);
        }

        [Test]
        public void CatalogRejectsDuplicateStableIdsAndOrders()
        {
            var first = new KnowledgeEntry(
                "knowledge_test",
                "测试条目",
                "用于验证目录唯一性。",
                KnowledgeEntryCategory.VehicleOverview,
                1,
                KnowledgeUnlockKind.VehicleCompleted);
            var duplicateId = new KnowledgeEntry(
                "knowledge_test",
                "另一条目",
                "用于验证重复标识。",
                KnowledgeEntryCategory.VehicleOverview,
                2,
                KnowledgeUnlockKind.VehicleCompleted);
            var duplicateOrder = new KnowledgeEntry(
                "knowledge_other",
                "顺序冲突",
                "用于验证重复顺序。",
                KnowledgeEntryCategory.VehicleOverview,
                1,
                KnowledgeUnlockKind.VehicleCompleted);

            Assert.Throws<ArgumentException>(() =>
                new EngineeringKnowledgeCatalog(new[] { first, duplicateId }));
            Assert.Throws<ArgumentException>(() =>
                new EngineeringKnowledgeCatalog(new[] { first, duplicateOrder }));
        }
    }
}
