using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.World;
using UnityEngine;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class WhiteboxWorldInteractionTests
    {
        private GameObject root;
        private WhiteboxGameSessionHost host;
        private DomainWorldGameSession worldSession;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("WhiteboxWorldTests");
            host = root.AddComponent<WhiteboxGameSessionHost>();
            worldSession = new DomainWorldGameSession();
            host.Configure(worldSession, "开始测试");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void QuizStationCyclesItsQuestionPoolOnEverySessionReset()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var station = root.AddComponent<QuizPartStation>();
            var pool = new[]
            {
                Presentation("bank_mc01"),
                Presentation("bank_mc15"),
                Presentation("bank_tf07")
            };
            station.Configure(
                host,
                inputLock,
                null,
                pool,
                PartId.Axle,
                "车轴答题工位",
                Child("AxleReward"),
                "前往装配工位");

            Assert.That(station.CurrentQuestion.QuestionId, Is.EqualTo("bank_mc01"));

            host.ResetSession();
            Assert.That(station.CurrentQuestion.QuestionId, Is.EqualTo("bank_mc15"));

            host.ResetSession();
            Assert.That(station.CurrentQuestion.QuestionId, Is.EqualTo("bank_tf07"));

            host.ResetSession();
            Assert.That(station.CurrentQuestion.QuestionId, Is.EqualTo("bank_mc01"));
            Assert.That(station.RewardUnlocked, Is.False);
            Assert.That(station.IsCollected, Is.False);
        }

        [Test]
        public void FourOptionQuestionMapsDisplayOrderAndRequiresCorrectAnswerBeforeSinglePickup()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var rewardVisual = Child("AxleReward");
            var station = root.AddComponent<QuizPartStation>();
            var dialog = new FakeQuizDialog();
            station.Configure(
                host,
                inputLock,
                null,
                new[] { Presentation("bank_mc01", 2, 0, 3, 1) },
                PartId.Axle,
                "车轴答题工位",
                rewardVisual,
                "前往轮对轴箱装配台");
            station.SetQuizDialogForTests(dialog);

            var context = new InteractionContext(root);
            station.Interact(context);

            Assert.That(dialog.IsOpen, Is.True);
            Assert.That(dialog.LastQuestion.Options.Count, Is.EqualTo(4));
            Assert.That(dialog.LastQuestion.MapSubmittedOptionIndex(0), Is.EqualTo(2));
            Assert.That(dialog.LastQuestion.MapSubmittedOptionIndex(1), Is.EqualTo(0));
            Assert.That(inputLock.InputLocked, Is.True);

            dialog.Select(1);
            Assert.That(station.RewardUnlocked, Is.False);
            Assert.That(station.IsQuizOpen, Is.True);
            Assert.That(dialog.Feedback, Is.Not.Empty);
            Assert.That(worldSession.DomainSession.IsPartUnlocked(PartId.Axle), Is.False);

            dialog.Select(0);
            Assert.That(station.RewardUnlocked, Is.True);
            Assert.That(station.IsQuizOpen, Is.False);
            Assert.That(inputLock.InputLocked, Is.False);
            Assert.That(rewardVisual.activeSelf, Is.True);
            Assert.That(host.Session.InventoryContains(PartId.Axle), Is.False);

            station.Interact(context);
            Assert.That(station.IsCollected, Is.True);
            Assert.That(rewardVisual.activeSelf, Is.False);
            Assert.That(host.Session.InventoryContains(PartId.Axle), Is.True);

            station.Interact(context);
            Assert.That(host.Session.InventoryParts, Has.Count.EqualTo(1));

            host.ResetSession();
            Assert.That(station.RewardUnlocked, Is.False);
            Assert.That(station.IsCollected, Is.False);
            Assert.That(host.Session.InventoryParts, Is.Empty);
        }

        [Test]
        public void JudgmentQuestionMapsTwoDisplayedOptionsToDomainAnswerIndices()
        {
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var station = root.AddComponent<QuizPartStation>();
            var dialog = new FakeQuizDialog();
            station.Configure(
                host,
                inputLock,
                null,
                new[] { Presentation("bank_tf01", 1, 0) },
                PartId.PrimaryDamper,
                "一系减振元件答题工位",
                Child("PrimaryDamperReward"),
                "前往一系悬挂装配台");
            station.SetQuizDialogForTests(dialog);

            station.Interact(new InteractionContext(root));

            Assert.That(dialog.LastQuestion.Options.Count, Is.EqualTo(2));
            Assert.That(dialog.LastQuestion.Options[0], Is.EqualTo("错误"));
            Assert.That(dialog.LastQuestion.Options[1], Is.EqualTo("正确"));
            Assert.That(dialog.LastQuestion.MapSubmittedOptionIndex(0), Is.EqualTo(1));
            Assert.That(dialog.LastQuestion.MapSubmittedOptionIndex(1), Is.EqualTo(0));

            dialog.Select(1);
            Assert.That(station.RewardUnlocked, Is.False);

            dialog.Select(0);
            Assert.That(station.RewardUnlocked, Is.True);
            Assert.That(worldSession.DomainSession.IsPartUnlocked(PartId.PrimaryDamper), Is.True);
        }

        [Test]
        public void ModuleStationSupportsAFourPartRecipeAndSnapsEveryInstalledVisual()
        {
            worldSession = new DomainWorldGameSession(
                new WhiteboxGameSession(CreateVariableLengthCatalog()));
            host.Configure(worldSession, "开始变长配方测试");

            var requiredParts = new[]
            {
                PartId.Axle,
                PartId.Wheel,
                PartId.Bearing,
                PartId.BrakeDevice
            };
            foreach (var partId in requiredParts)
                UnlockAndCollect(partId);

            var slots = CreateTransforms("FourPartSlot", requiredParts.Length);
            var visuals = CreateVisuals("FourPartVisual", requiredParts.Length);
            var completedVisual = Child("FourPartComplete");
            var station = root.AddComponent<ModuleAssemblyStation>();
            station.Configure(
                host,
                ModuleId.WheelsetAxlebox,
                "四零件轮对轴箱装配台",
                requiredParts,
                slots,
                visuals,
                completedVisual,
                "前往下一工位");

            Assert.That(station.RequiredPartCount, Is.EqualTo(4));
            Assert.That(station.IsComplete, Is.False);

            var context = new InteractionContext(root);
            for (var index = 0; index < requiredParts.Length; index++)
            {
                station.Interact(context);
                Assert.That(station.InstalledPartCount, Is.EqualTo(index + 1));
                Assert.That(visuals[index].activeSelf, Is.True);
                Assert.That(visuals[index].transform.parent, Is.EqualTo(slots[index]));
                Assert.That(station.IsComplete, Is.EqualTo(index == requiredParts.Length - 1));
            }

            Assert.That(completedVisual.activeSelf, Is.True);
            Assert.That(host.Session.InventoryParts, Is.Empty);
        }

        [Test]
        public void CompositeStationRejectsIncompleteChildAndCompletesAfterThreeChildAssemblies()
        {
            var children = new[]
            {
                ModuleId.WheelsetAxlebox,
                ModuleId.Frame,
                ModuleId.PrimarySuspension
            };
            var slots = CreateTransforms("BogieChildSlot", children.Length);
            var visuals = CreateVisuals("BogieChildVisual", children.Length);
            var completedVisual = Child("BogieStructureComplete");
            var station = root.AddComponent<CompositeAssemblyStation>();
            station.Configure(
                host,
                ModuleId.BogieStructure,
                "转向架构体装配台",
                children,
                slots,
                visuals,
                completedVisual,
                "前往落车工位");

            var context = new InteractionContext(root);
            station.Interact(context);
            Assert.That(station.InstalledModuleCount, Is.Zero);
            Assert.That(
                host.Session.IsModuleInstalled(ModuleId.BogieStructure, ModuleId.WheelsetAxlebox),
                Is.False);

            for (var index = 0; index < children.Length; index++)
            {
                CompleteLeafModule(children[index]);
                station.Interact(context);
                Assert.That(station.InstalledModuleCount, Is.EqualTo(index + 1));
                Assert.That(visuals[index].activeSelf, Is.True);
                Assert.That(visuals[index].transform.parent, Is.EqualTo(slots[index]));

                if (index < children.Length - 1)
                {
                    station.Interact(context);
                    Assert.That(station.InstalledModuleCount, Is.EqualTo(index + 1));
                }
            }

            Assert.That(station.RequiredModuleCount, Is.EqualTo(3));
            Assert.That(station.IsComplete, Is.True);
            Assert.That(completedVisual.activeSelf, Is.True);
        }

        [Test]
        public void FinalAssemblyInstallsTwoModulesAndTwoPartsButOnlyUnlocksCommissioning()
        {
            CompleteBogieStructure();
            CompleteLeafModule(ModuleId.SecondarySuspension);
            UnlockAndCollect(PartId.Carbody);
            UnlockAndCollect(PartId.CentralTractionDevice);

            var modules = new[] { ModuleId.BogieStructure, ModuleId.SecondarySuspension };
            var parts = new[] { PartId.Carbody, PartId.CentralTractionDevice };
            var moduleSlots = CreateTransforms("LandingModuleSlot", modules.Length);
            var partSlots = CreateTransforms("LandingPartSlot", parts.Length);
            var moduleVisuals = CreateVisuals("LandingModuleVisual", modules.Length);
            var partVisuals = CreateVisuals("LandingPartVisual", parts.Length);
            var landingVisual = Child("LandingComplete");
            var station = root.AddComponent<FinalAssemblyStation>();
            station.Configure(
                host,
                ModuleId.Landing,
                "落车工位",
                modules,
                parts,
                moduleSlots,
                partSlots,
                moduleVisuals,
                partVisuals,
                landingVisual);

            var completionCount = 0;
            host.VehicleCompleted += () => completionCount++;
            var context = new InteractionContext(root);
            for (var index = 0; index < 4; index++)
            {
                station.Interact(context);
                Assert.That(station.InstalledInputCount, Is.EqualTo(index + 1));
            }

            Assert.That(station.RequiredInputCount, Is.EqualTo(4));
            Assert.That(station.IsLandingComplete, Is.True);
            Assert.That(station.IsVehicleComplete, Is.False);
            Assert.That(host.Session.IsVehicleComplete, Is.False);
            Assert.That(host.Session.CommissioningPhase,
                Is.EqualTo(CommissioningPhase.ReadyForInitialTest));
            Assert.That(landingVisual.activeSelf, Is.True);
            Assert.That(moduleVisuals.All(visual => !visual.activeSelf), Is.True);
            Assert.That(partVisuals.All(visual => !visual.activeSelf), Is.True);
            Assert.That(completionCount, Is.Zero);
        }

        [Test]
        public void LandingCompletionHidesInputVisualsAndLeavesOnlyTheDroppedVehicle()
        {
            CompleteBogieStructure();
            CompleteLeafModule(ModuleId.SecondarySuspension);
            UnlockAndCollect(PartId.Carbody);
            UnlockAndCollect(PartId.CentralTractionDevice);

            var modules = new[] { ModuleId.BogieStructure, ModuleId.SecondarySuspension };
            var parts = new[] { PartId.Carbody, PartId.CentralTractionDevice };
            var moduleSlots = CreateTransforms("LandingHideModuleSlot", modules.Length);
            var partSlots = CreateTransforms("LandingHidePartSlot", parts.Length);
            var moduleVisuals = CreateVisuals("LandingHideModuleVisual", modules.Length);
            var partVisuals = CreateVisuals("LandingHidePartVisual", parts.Length);
            var landingVisual = Child("LandingHideComplete");
            var station = root.AddComponent<FinalAssemblyStation>();
            station.Configure(
                host,
                ModuleId.Landing,
                "落车工位",
                modules,
                parts,
                moduleSlots,
                partSlots,
                moduleVisuals,
                partVisuals,
                landingVisual);

            var context = new InteractionContext(root);
            for (var index = 0; index < station.RequiredInputCount; index++)
                station.Interact(context);

            Assert.That(station.IsLandingComplete, Is.True);
            Assert.That(landingVisual.activeSelf, Is.True);
            Assert.That(moduleVisuals.Any(visual => visual.activeSelf), Is.False);
            Assert.That(partVisuals.Any(visual => visual.activeSelf), Is.False);
        }

        [Test]
        public void CommissioningRunsFailureRetuneInspectionRetestRaisesCompletionOnceAndResetClearsAll()
        {
            var completionCount = 0;
            host.VehicleCompleted += () => completionCount++;
            CompleteLanding();

            var testReady = Child("TestReady");
            var testCompleted = Child("TestCompleted");
            var retuneReady = Child("RetuneReady");
            var retuneCompleted = Child("RetuneCompleted");
            var inspectReady = Child("InspectReady");
            var inspectCompleted = Child("InspectCompleted");
            var testStation = Child("TestStation").AddComponent<CommissioningStation>();
            var retuneStation = Child("RetuneStation").AddComponent<CommissioningStation>();
            var inspectionStation = Child("InspectionStation").AddComponent<CommissioningStation>();
            testStation.Configure(host, CommissioningAction.Test, "调试判定", testReady, testCompleted);
            retuneStation.Configure(host, CommissioningAction.Retune, "重新调试", retuneReady, retuneCompleted);
            inspectionStation.Configure(host, CommissioningAction.Inspect, "检验", inspectReady, inspectCompleted);

            var context = new InteractionContext(root);
            Assert.That(host.Session.CommissioningPhase,
                Is.EqualTo(CommissioningPhase.ReadyForInitialTest));
            Assert.That(testStation.IsReady, Is.True);

            testStation.Interact(context);
            Assert.That(host.Session.CommissioningPhase, Is.EqualTo(CommissioningPhase.NeedsRetuning));
            Assert.That(retuneStation.IsReady, Is.True);
            Assert.That(completionCount, Is.Zero);

            testStation.Interact(context);
            Assert.That(host.Session.CommissioningPhase, Is.EqualTo(CommissioningPhase.NeedsRetuning));

            retuneStation.Interact(context);
            Assert.That(host.Session.CommissioningPhase,
                Is.EqualTo(CommissioningPhase.ReadyForInspection));
            Assert.That(inspectionStation.IsReady, Is.True);

            inspectionStation.Interact(context);
            Assert.That(host.Session.CommissioningPhase, Is.EqualTo(CommissioningPhase.ReadyForRetest));
            Assert.That(testStation.IsReady, Is.True);

            testStation.Interact(context);
            Assert.That(host.Session.CommissioningPhase, Is.EqualTo(CommissioningPhase.InService));
            Assert.That(host.Session.IsVehicleComplete, Is.True);
            Assert.That(completionCount, Is.EqualTo(1));

            var repeated = host.RunCommissioning();
            Assert.That(repeated.Passed, Is.True);
            Assert.That(repeated.Changed, Is.False);
            Assert.That(completionCount, Is.EqualTo(1));

            host.ResetSession();

            Assert.That(host.Session.InventoryParts, Is.Empty);
            Assert.That(host.Session.IsLandingComplete, Is.False);
            Assert.That(host.Session.IsVehicleComplete, Is.False);
            Assert.That(host.Session.AreAllModulesComplete, Is.False);
            Assert.That(host.Session.CommissioningPhase, Is.EqualTo(CommissioningPhase.Locked));
            Assert.That(worldSession.DomainSession.UnlockedParts, Is.Empty);
            Assert.That(worldSession.DomainSession.CollectedParts, Is.Empty);
            foreach (ModuleId moduleId in Enum.GetValues(typeof(ModuleId)))
                Assert.That(host.Session.IsModuleComplete(moduleId), Is.False, moduleId.ToString());
            foreach (PartId partId in Enum.GetValues(typeof(PartId)))
                Assert.That(host.Session.InventoryContains(partId), Is.False, partId.ToString());
            Assert.That(testReady.activeSelf, Is.False);
            Assert.That(testCompleted.activeSelf, Is.False);
            Assert.That(retuneReady.activeSelf, Is.False);
            Assert.That(retuneCompleted.activeSelf, Is.False);
            Assert.That(inspectReady.activeSelf, Is.False);
            Assert.That(inspectCompleted.activeSelf, Is.False);
            Assert.That(completionCount, Is.EqualTo(1));
        }

        private QuizQuestionPresentation Presentation(string questionId, params int[] displayToDomainMap)
        {
            var question = worldSession.DomainSession.Catalog.GetQuestion(questionId);
            if (displayToDomainMap == null || displayToDomainMap.Length == 0)
            {
                return new QuizQuestionPresentation(
                    question.Id,
                    question.Prompt,
                    question.Options,
                    question.Explanation);
            }

            var displayedOptions = displayToDomainMap
                .Select(domainIndex => question.Options[domainIndex])
                .ToArray();
            return new QuizQuestionPresentation(
                question.Id,
                question.Prompt,
                displayedOptions,
                displayToDomainMap,
                question.Explanation);
        }

        private void CompleteLanding()
        {
            CompleteBogieStructure();
            CompleteLeafModule(ModuleId.SecondarySuspension);
            Assert.That(
                host.InstallModule(ModuleId.Landing, ModuleId.BogieStructure).Accepted,
                Is.True);
            Assert.That(
                host.InstallModule(ModuleId.Landing, ModuleId.SecondarySuspension).Accepted,
                Is.True);
            UnlockAndCollect(PartId.Carbody);
            UnlockAndCollect(PartId.CentralTractionDevice);
            Assert.That(host.InstallPart(ModuleId.Landing, PartId.Carbody).Accepted, Is.True);
            Assert.That(
                host.InstallPart(ModuleId.Landing, PartId.CentralTractionDevice).Accepted,
                Is.True);
            Assert.That(host.Session.IsLandingComplete, Is.True);
            Assert.That(host.Session.IsVehicleComplete, Is.False);
        }

        private void CompleteBogieStructure()
        {
            var children = new[]
            {
                ModuleId.WheelsetAxlebox,
                ModuleId.Frame,
                ModuleId.PrimarySuspension
            };
            foreach (var child in children)
            {
                CompleteLeafModule(child);
                Assert.That(host.InstallModule(ModuleId.BogieStructure, child).Accepted, Is.True);
            }
            Assert.That(host.Session.IsModuleComplete(ModuleId.BogieStructure), Is.True);
        }

        private void CompleteLeafModule(ModuleId moduleId)
        {
            var definition = worldSession.DomainSession.Catalog.GetModule(moduleId);
            Assert.That(definition.RequiredModules, Is.Empty, $"{moduleId} must be a leaf assembly.");
            foreach (var partId in definition.RequiredParts)
            {
                UnlockAndCollect(partId);
                Assert.That(host.InstallPart(moduleId, partId).Accepted, Is.True);
            }
            Assert.That(host.Session.IsModuleComplete(moduleId), Is.True);
        }

        private void UnlockAndCollect(PartId partId)
        {
            var question = worldSession.DomainSession.Catalog.Questions
                .First(candidate => candidate.RewardPart == partId);
            Assert.That(
                host.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect,
                Is.True,
                question.Id);
            Assert.That(host.CollectPart(partId).Accepted, Is.True, partId.ToString());
        }

        private static WhiteboxGameCatalog CreateVariableLengthCatalog()
        {
            var parts = ((PartId[])Enum.GetValues(typeof(PartId)))
                .Select(partId => new PartDefinition(partId, $"part_{partId}", partId.ToString()))
                .ToArray();
            var modules = new[]
            {
                new ModuleDefinition(
                    ModuleId.WheelsetAxlebox,
                    "module_wheelset_axlebox",
                    "四输入轮对轴箱",
                    new[] { PartId.Axle, PartId.Wheel, PartId.Bearing, PartId.BrakeDevice }),
                new ModuleDefinition(
                    ModuleId.Frame,
                    "module_frame",
                    "双输入构架",
                    new[] { PartId.TractionRod, PartId.SensorBracket }),
                new ModuleDefinition(
                    ModuleId.PrimarySuspension,
                    "module_primary_suspension",
                    "一系悬挂装置",
                    new[]
                    {
                        PartId.PrimaryElasticElement,
                        PartId.PrimaryPositioningElement,
                        PartId.PrimaryDamper
                    }),
                new ModuleDefinition(
                    ModuleId.BogieStructure,
                    "module_bogie_structure",
                    "转向架构体",
                    Array.Empty<PartId>(),
                    new[]
                    {
                        ModuleId.WheelsetAxlebox,
                        ModuleId.Frame,
                        ModuleId.PrimarySuspension
                    }),
                new ModuleDefinition(
                    ModuleId.SecondarySuspension,
                    "module_secondary_suspension",
                    "二系悬挂装置",
                    new[]
                    {
                        PartId.SecondaryElasticElement,
                        PartId.HeightControlElement,
                        PartId.SecondaryDamper
                    }),
                new ModuleDefinition(
                    ModuleId.Landing,
                    "module_landing",
                    "落车",
                    new[] { PartId.Carbody, PartId.CentralTractionDevice },
                    new[] { ModuleId.BogieStructure, ModuleId.SecondarySuspension })
            };
            var questions = ((PartId[])Enum.GetValues(typeof(PartId)))
                .Select(partId => new QuizQuestionDefinition(
                    $"question_{partId}",
                    $"请选择 {partId} 的正确答案",
                    new[] { "错误", "正确" },
                    1,
                    partId,
                    "测试题解析"))
                .ToArray();
            return new WhiteboxGameCatalog(parts, modules, questions);
        }

        private Transform[] CreateTransforms(string prefix, int count)
        {
            var result = new Transform[count];
            for (var index = 0; index < count; index++)
                result[index] = Child($"{prefix}_{index}").transform;
            return result;
        }

        private GameObject[] CreateVisuals(string prefix, int count)
        {
            var result = new GameObject[count];
            for (var index = 0; index < count; index++)
                result[index] = Child($"{prefix}_{index}");
            return result;
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
            private Action cancellation;

            public bool IsOpen { get; private set; }
            public string Feedback { get; private set; } = string.Empty;
            public QuizQuestionPresentation LastQuestion { get; private set; }

            public void Present(
                QuizQuestionPresentation question,
                Action<int> optionSelected,
                Action cancelled)
            {
                LastQuestion = question;
                selection = optionSelected;
                cancellation = cancelled;
                Feedback = string.Empty;
                IsOpen = true;
            }

            public void SetFeedback(string message)
            {
                Feedback = message ?? string.Empty;
            }

            public void Dismiss()
            {
                IsOpen = false;
                selection = null;
                cancellation = null;
            }

            public void Select(int index)
            {
                selection?.Invoke(index);
            }

            public void Cancel()
            {
                cancellation?.Invoke();
            }
        }
    }
}
