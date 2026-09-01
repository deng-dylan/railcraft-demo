using System;
using System.Collections.Generic;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.Tests.EditMode.Domain
{
    public sealed class WhiteboxGameCatalogTests
    {
        [Test]
        public void DefaultCatalogDefinesFourteenPartsAndSixAssemblyNodes()
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var recipeParts = new HashSet<PartId>();

            Assert.That(catalog.Parts, Has.Count.EqualTo(14));
            Assert.That(catalog.Modules, Has.Count.EqualTo(6));
            foreach (var module in catalog.Modules)
            {
                Assert.That(module.RequiredInputCount, Is.GreaterThan(0), module.Key);
                foreach (var partId in module.RequiredParts)
                    Assert.That(recipeParts.Add(partId), Is.True, $"Duplicate recipe part: {partId}");
            }

            Assert.That(recipeParts,
                Is.EquivalentTo((PartId[])Enum.GetValues(typeof(PartId))));
        }

        [Test]
        public void DefaultAssemblyGraphMatchesTheProductionFlow()
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();

            AssertRecipe(
                catalog.GetModule(ModuleId.WheelsetAxlebox),
                new[] { PartId.Axle, PartId.Wheel, PartId.Bearing },
                Array.Empty<ModuleId>());
            AssertRecipe(
                catalog.GetModule(ModuleId.Frame),
                new[] { PartId.BrakeDevice, PartId.TractionRod, PartId.SensorBracket },
                Array.Empty<ModuleId>());
            AssertRecipe(
                catalog.GetModule(ModuleId.PrimarySuspension),
                new[]
                {
                    PartId.PrimaryElasticElement,
                    PartId.PrimaryPositioningElement,
                    PartId.PrimaryDamper
                },
                Array.Empty<ModuleId>());
            AssertRecipe(
                catalog.GetModule(ModuleId.BogieStructure),
                Array.Empty<PartId>(),
                new[]
                {
                    ModuleId.WheelsetAxlebox,
                    ModuleId.Frame,
                    ModuleId.PrimarySuspension
                });
            AssertRecipe(
                catalog.GetModule(ModuleId.SecondarySuspension),
                new[]
                {
                    PartId.SecondaryElasticElement,
                    PartId.HeightControlElement,
                    PartId.SecondaryDamper
                },
                Array.Empty<ModuleId>());
            AssertRecipe(
                catalog.GetModule(ModuleId.Landing),
                new[] { PartId.Carbody, PartId.CentralTractionDevice },
                new[] { ModuleId.BogieStructure, ModuleId.SecondarySuspension });
        }

        [Test]
        public void DefaultQuestionBankContainsFiftyChoiceAndEightJudgmentQuestions()
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();
            var rewardsPerPart = new Dictionary<PartId, int>();
            var fourOptionCount = 0;
            var twoOptionCount = 0;

            Assert.That(catalog.Questions, Has.Count.EqualTo(58));
            foreach (var question in catalog.Questions)
            {
                Assert.That(question.IsValidOption(question.CorrectOptionIndex), Is.True, question.Id);
                Assert.That(question.Explanation, Is.Not.Empty, question.Id);
                Assert.That(question.Prompt, Is.Not.Empty, question.Id);

                if (question.Options.Count == 4)
                    fourOptionCount++;
                else if (question.Options.Count == 2)
                    twoOptionCount++;
                else
                    Assert.Fail($"Unexpected option count for {question.Id}: {question.Options.Count}");

                rewardsPerPart.TryGetValue(question.RewardPart, out var rewardCount);
                rewardsPerPart[question.RewardPart] = rewardCount + 1;
            }

            Assert.That(fourOptionCount, Is.EqualTo(50));
            Assert.That(twoOptionCount, Is.EqualTo(8));
            Assert.That(rewardsPerPart.Keys,
                Is.EquivalentTo((PartId[])Enum.GetValues(typeof(PartId))));
            foreach (var rewardCount in rewardsPerPart.Values)
                Assert.That(rewardCount, Is.InRange(4, 5));
        }

        [Test]
        public void QuestionDefinitionSupportsLegacyAndExplanationConstructors()
        {
            var legacy = new QuizQuestionDefinition(
                "legacy",
                "题干",
                new[] { "A", "B" },
                0,
                PartId.Axle);
            var explained = new QuizQuestionDefinition(
                "explained",
                "题干",
                new[] { "正确", "错误" },
                1,
                PartId.Wheel,
                "解析内容");

            Assert.That(legacy.Explanation, Is.Empty);
            Assert.That(explained.Explanation, Is.EqualTo("解析内容"));
        }

        [Test]
        public void StableKeysResolveForMonoBehaviourAdapters()
        {
            var catalog = WhiteboxGameCatalog.CreateDefault();

            Assert.That(catalog.TryGetPart("part_axle", out var part), Is.True);
            Assert.That(part.Id, Is.EqualTo(PartId.Axle));
            Assert.That(catalog.TryGetModule("module_bogie_structure", out var module), Is.True);
            Assert.That(module.Id, Is.EqualTo(ModuleId.BogieStructure));
            Assert.That(catalog.TryGetQuestion("bank_mc01", out var question), Is.True);
            Assert.That(question.Options, Has.Count.EqualTo(4));
        }

        private static void AssertRecipe(
            ModuleDefinition definition,
            IEnumerable<PartId> expectedParts,
            IEnumerable<ModuleId> expectedModules)
        {
            Assert.That(definition.RequiredParts, Is.EqualTo(expectedParts), definition.Key);
            Assert.That(definition.RequiredModules, Is.EqualTo(expectedModules), definition.Key);
            Assert.That(
                definition.RequiredInputCount,
                Is.EqualTo(definition.RequiredParts.Count + definition.RequiredModules.Count));
        }
    }
}
