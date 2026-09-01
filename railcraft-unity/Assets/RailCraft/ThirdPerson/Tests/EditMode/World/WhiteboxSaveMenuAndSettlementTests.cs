using System;
using System.Linq;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;
using RailCraft.ThirdPerson.Player;
using RailCraft.ThirdPerson.UI;
using RailCraft.ThirdPerson.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RailCraft.ThirdPerson.Tests.EditMode.World
{
    public sealed class WhiteboxSaveMenuAndSettlementTests
    {
        private const string VolumeKey = "railcraft.whitebox.settings.master-volume";
        private const string QualityKey = "railcraft.whitebox.settings.quality-level";

        private GameObject root;
        private string saveKey;
        private float originalVolume;
        private int originalQuality;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("WhiteboxSaveMenuAndSettlementTests");
            saveKey = $"railcraft.tests.save.{Guid.NewGuid():N}";
            originalVolume = AudioListener.volume;
            originalQuality = QualitySettings.GetQualityLevel();
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.DeleteKey(VolumeKey);
            PlayerPrefs.DeleteKey(QualityKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.DeleteKey(VolumeKey);
            PlayerPrefs.DeleteKey(QualityKey);
            PlayerPrefs.Save();
            AudioListener.volume = originalVolume;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(originalQuality, false);
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void NewGameResetsSessionCreatesSaveAndAutoSavesLaterChanges()
        {
            var host = CreateHost(root, out var worldSession);
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);

            var axleQuestion = QuestionRewarding(worldSession.DomainSession, PartId.Axle);
            Assert.That(
                host.SubmitAnswer(axleQuestion.Id, axleQuestion.CorrectOptionIndex).IsCorrect,
                Is.True);
            Assert.That(worldSession.DomainSession.AnswerAttemptCount, Is.EqualTo(1));

            save.StartNewGame();

            Assert.That(save.HasActiveSession, Is.True);
            Assert.That(save.HasSave, Is.True);
            Assert.That(worldSession.DomainSession.AnswerAttemptCount, Is.Zero);
            Assert.That(ReadStoredSnapshot().AnswerAttemptCount, Is.Zero);

            Assert.That(
                host.SubmitAnswer(axleQuestion.Id, axleQuestion.CorrectOptionIndex).IsCorrect,
                Is.True);
            var autoSaved = ReadStoredSnapshot();
            Assert.That(autoSaved.AnswerAttemptCount, Is.EqualTo(1));
            Assert.That(autoSaved.CorrectAnswerCount, Is.EqualTo(1));
            Assert.That(autoSaved.UnlockedParts, Does.Contain(PartId.Axle));
        }

        [Test]
        public void ContinueRestoresStoredSnapshotIntoAnotherHost()
        {
            var sourceObject = Child("Source");
            var sourceHost = CreateHost(sourceObject, out var sourceSession);
            var sourceSave = sourceObject.AddComponent<WhiteboxSaveController>();
            sourceSave.Configure(sourceHost, saveKey, true);
            sourceSave.StartNewGame();
            var question = QuestionRewarding(sourceSession.DomainSession, PartId.Axle);
            Assert.That(sourceHost.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
            Assert.That(sourceHost.CollectPart(PartId.Axle).Accepted, Is.True);
            Assert.That(PlayerPrefs.HasKey(saveKey), Is.True);

            var targetObject = Child("Target");
            var targetHost = CreateHost(targetObject, out _);
            var targetSave = targetObject.AddComponent<WhiteboxSaveController>();
            targetSave.Configure(targetHost, saveKey, true);

            Assert.That(targetSave.TryContinueGame(), Is.True);
            Assert.That(targetSave.HasActiveSession, Is.True);
            Assert.That(targetHost.Session.InventoryContains(PartId.Axle), Is.True);
            Assert.That(targetHost.Session.Progress.AnswerAttemptCount, Is.EqualTo(1));
            Assert.That(targetHost.Session.Progress.CorrectAnswerCount, Is.EqualTo(1));
        }

        [Test]
        public void ContinueRestoresTheSelectedAssemblyVariant()
        {
            var sourceObject = Child("VariantSource");
            var sourceHost = CreateHost(sourceObject, out _);
            var sourceSave = sourceObject.AddComponent<WhiteboxSaveController>();
            sourceSave.Configure(sourceHost, saveKey, true);
            sourceSave.StartNewGame(AssemblyVariantId.Y25Freight);

            var targetObject = Child("VariantTarget");
            var targetHost = CreateHost(targetObject, out _);
            var targetSave = targetObject.AddComponent<WhiteboxSaveController>();
            targetSave.Configure(targetHost, saveKey, true);

            Assert.That(targetSave.TryContinueGame(), Is.True);
            Assert.That(
                targetHost.SelectedAssemblyVariant,
                Is.EqualTo(AssemblyVariantId.Y25Freight));
        }

        [Test]
        public void CorruptedSaveIsRejectedAndRemoved()
        {
            var host = CreateHost(root, out _);
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            PlayerPrefs.SetString(saveKey, "{ definitely-not-valid-json }");
            PlayerPrefs.Save();

            Assert.That(save.TryContinueGame(), Is.False);
            Assert.That(save.HasActiveSession, Is.False);
            Assert.That(PlayerPrefs.HasKey(saveKey), Is.False);
        }

        [Test]
        public void RuntimeSettingsClampPersistAndLoadValues()
        {
            var highestQuality = Math.Max(0, QualitySettings.names.Length - 1);

            WhiteboxRuntimeSettings.Save(1.7f, highestQuality + 20);
            var high = WhiteboxRuntimeSettings.Load();

            Assert.That(high.MasterVolume, Is.EqualTo(1f));
            Assert.That(high.QualityLevel, Is.EqualTo(highestQuality));
            AssertAudioVolumeWhenBackendIsAvailable(1f);

            WhiteboxRuntimeSettings.Save(-0.4f, -5);
            var low = WhiteboxRuntimeSettings.Load();

            Assert.That(low.MasterVolume, Is.Zero);
            Assert.That(low.QualityLevel, Is.Zero);
            AssertAudioVolumeWhenBackendIsAvailable(0f);
        }

        [Test]
        public void MainMenuStartButtonBeginsFreshGameHidesMenuAndReleasesInput()
        {
            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var view = CreateMenu(host, save, inputLock);

            Assert.That(view.Controller.IsMenuVisible, Is.True);
            Assert.That(inputLock.InputLocked, Is.True);
            Assert.That(view.ContinueButton.interactable, Is.False);

            view.StartButton.onClick.Invoke();

            Assert.That(save.HasActiveSession, Is.True);
            Assert.That(save.HasSave, Is.True);
            Assert.That(host.SelectedAssemblyVariant, Is.EqualTo(AssemblyVariantId.FuxingDemo));
            Assert.That(view.Controller.IsMenuVisible, Is.False);
            Assert.That(inputLock.InputLocked, Is.False);
        }

        [Test]
        public void MainMenuStartIgnoresVariantDropdownAndUsesStandardPlan()
        {
            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var view = CreateMenu(host, save, inputLock, includeAssemblyVariantDropdown: true);

            view.AssemblyVariantDropdown.value = (int)AssemblyVariantId.TeachingConcept;
            view.StartButton.onClick.Invoke();

            Assert.That(
                host.SelectedAssemblyVariant,
                Is.EqualTo(AssemblyVariantId.FuxingDemo));
            Assert.That(ReadStoredSnapshot().AssemblyVariant,
                Is.EqualTo(AssemblyVariantId.FuxingDemo));
        }

        [Test]
        public void MainMenuOmitsVariantDropdownInTheStandardTrainingLayout()
        {
            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);

            var view = CreateMenu(host, save, inputLock);

            Assert.That(view.AssemblyVariantDropdown, Is.Null);
        }

        [Test]
        public void MainMenuContinueButtonRestoresSaveAndReleasesInput()
        {
            var snapshotSession = new WhiteboxGameSession();
            var question = QuestionRewarding(snapshotSession, PartId.Axle);
            Assert.That(snapshotSession.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
            Assert.That(snapshotSession.CollectPart(PartId.Axle).Status, Is.EqualTo(PartCollectionStatus.Collected));
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(snapshotSession.ExportSnapshot()));
            PlayerPrefs.Save();

            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var view = CreateMenu(host, save, inputLock);

            Assert.That(view.ContinueButton.interactable, Is.True);
            view.ContinueButton.onClick.Invoke();

            Assert.That(host.Session.InventoryContains(PartId.Axle), Is.True);
            Assert.That(view.Controller.IsMenuVisible, Is.False);
            Assert.That(inputLock.InputLocked, Is.False);
        }

        [Test]
        public void LoadedInitialMenuReclaimsSerializedInputLockBeforeContinue()
        {
            var snapshotSession = new WhiteboxGameSession();
            var question = QuestionRewarding(snapshotSession, PartId.Axle);
            Assert.That(snapshotSession.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(snapshotSession.ExportSnapshot()));
            PlayerPrefs.Save();

            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            inputLock.SetInputLocked(true);
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var view = CreateMenu(host, save, inputLock);
            var awake = typeof(WhiteboxMainMenuController).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null);
            awake.Invoke(view.Controller, null);

            view.ContinueButton.onClick.Invoke();

            Assert.That(host.Session.InventoryContains(PartId.Axle), Is.False);
            Assert.That(host.Session.Progress.CorrectAnswerCount, Is.EqualTo(1));
            Assert.That(view.Controller.IsMenuVisible, Is.False);
            Assert.That(inputLock.InputLocked, Is.False);
        }

        [Test]
        public void EscapeTogglesActiveGamePauseMenuAndReturnsFromSettingsFirst()
        {
            var host = CreateHost(root, out var worldSession);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var view = CreateMenu(host, save, inputLock);

            view.StartButton.onClick.Invoke();
            var question = QuestionRewarding(worldSession.DomainSession, PartId.Axle);
            Assert.That(host.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);

            Assert.That(view.Controller.HandleEscapePressed(), Is.True);
            Assert.That(view.Controller.IsMenuVisible, Is.True);
            Assert.That(inputLock.InputLocked, Is.True);
            Assert.That(host.Session.IsTimingPaused, Is.True);

            view.SettingsButton.onClick.Invoke();
            Assert.That(view.Controller.IsSettingsVisible, Is.True);
            Assert.That(view.Controller.HandleEscapePressed(), Is.True);
            Assert.That(view.Controller.IsSettingsVisible, Is.False);
            Assert.That(view.Controller.IsMenuVisible, Is.True);
            Assert.That(inputLock.InputLocked, Is.True);

            Assert.That(view.Controller.HandleEscapePressed(), Is.True);
            Assert.That(view.Controller.IsMenuVisible, Is.False);
            Assert.That(inputLock.InputLocked, Is.False);
            Assert.That(host.Session.IsTimingPaused, Is.False);
        }

        [Test]
        public void EscapeCannotDismissInitialMenuOrOpenOverAnotherInputOwner()
        {
            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var view = CreateMenu(host, save, inputLock);

            Assert.That(view.Controller.HandleEscapePressed(), Is.False);
            Assert.That(view.Controller.IsMenuVisible, Is.True);

            view.StartButton.onClick.Invoke();
            inputLock.SetInputLocked(true);

            Assert.That(view.Controller.HandleEscapePressed(), Is.False);
            Assert.That(view.Controller.IsMenuVisible, Is.False);
            Assert.That(inputLock.InputLocked, Is.True);
        }

        [Test]
        public void InputSystemEscapeFrameOpensAndClosesPauseMenu()
        {
            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var view = CreateMenu(host, save, inputLock);
            view.StartButton.onClick.Invoke();
            var lateUpdate = typeof(WhiteboxMainMenuController).GetMethod(
                "LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(lateUpdate, Is.Not.Null);

            var inputFixture = new InputTestFixture();
            inputFixture.Setup();
            try
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                keyboard.MakeCurrent();
                inputFixture.Press(keyboard.escapeKey);
                Assert.That(Keyboard.current, Is.SameAs(keyboard));
                Assert.That(keyboard.escapeKey.wasPressedThisFrame, Is.True);
                lateUpdate.Invoke(view.Controller, null);
                Assert.That(view.Controller.IsMenuVisible, Is.True);
                Assert.That(inputLock.InputLocked, Is.True);

                inputFixture.Release(keyboard.escapeKey);
                inputFixture.Press(keyboard.escapeKey);
                lateUpdate.Invoke(view.Controller, null);
                Assert.That(view.Controller.IsMenuVisible, Is.False);
                Assert.That(inputLock.InputLocked, Is.False);
            }
            finally
            {
                inputFixture.TearDown();
            }
        }

        [Test]
        public void PendingKnowledgePopupBlocksEscapeMenuUntilKnowledgeIsClosed()
        {
            var host = CreateHost(root, out var worldSession);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            var popupRoot = Child("KnowledgePopup");
            var knowledge = root.AddComponent<WhiteboxKnowledgePresenter>();
            knowledge.Configure(
                host,
                inputLock,
                null,
                popupRoot,
                null,
                null,
                null,
                null,
                null,
                null);
            var view = CreateMenu(host, save, inputLock, knowledge);
            view.StartButton.onClick.Invoke();

            var question = QuestionRewarding(worldSession.DomainSession, PartId.Axle);
            Assert.That(host.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
            Assert.That(knowledge.PendingPopupCount, Is.GreaterThan(0));
            Assert.That(view.Controller.HandleEscapePressed(), Is.False);
            Assert.That(view.Controller.IsMenuVisible, Is.False);

            knowledge.FlushPendingKnowledgePopup();
            Assert.That(knowledge.IsAnyViewOpen, Is.True);
            Assert.That(inputLock.InputLocked, Is.True);
            Assert.That(view.Controller.HandleEscapePressed(), Is.False);

            knowledge.CloseKnowledgePopup();
            Assert.That(inputLock.InputLocked, Is.False);
            Assert.That(view.Controller.HandleEscapePressed(), Is.True);
            Assert.That(view.Controller.IsMenuVisible, Is.True);
        }

        [Test]
        public void MainMenuAndContinueExcludePausedTimeFromSettlementClock()
        {
            var start = new DateTimeOffset(2026, 8, 6, 7, 0, 0, TimeSpan.Zero);
            var clock = new ManualClock(start);
            var domain = new WhiteboxGameSession(
                WhiteboxGameCatalog.CreateDefault(),
                clock.UtcNow);
            var world = new DomainWorldGameSession(domain);
            var host = root.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(world, "测试目标");
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            save.StartNewGame();

            var question = QuestionRewarding(domain, PartId.Axle);
            host.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            clock.Now = start.AddMinutes(5);

            var view = CreateMenu(host, save, inputLock);
            Assert.That(domain.PausedAtUtc, Is.EqualTo(clock.Now));
            Assert.That(domain.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));

            clock.Now = start.AddHours(1).AddMinutes(5);
            InvokeApplicationPause(save, true);
            InvokeApplicationPause(save, false);

            Assert.That(domain.PausedAtUtc, Is.EqualTo(start.AddMinutes(5)));
            Assert.That(domain.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));

            view.ContinueButton.onClick.Invoke();

            Assert.That(view.Controller.IsMenuVisible, Is.False);
            Assert.That(domain.PausedAtUtc, Is.Null);
            Assert.That(domain.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));

            clock.Now = clock.Now.AddMinutes(3);
            Assert.That(domain.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(8)));
        }

        [Test]
        public void ApplicationPauseOwnsAndResumesOnlyItsOwnTimingPause()
        {
            var start = new DateTimeOffset(2026, 8, 6, 7, 30, 0, TimeSpan.Zero);
            var clock = new ManualClock(start);
            var domain = new WhiteboxGameSession(
                WhiteboxGameCatalog.CreateDefault(),
                clock.UtcNow);
            var world = new DomainWorldGameSession(domain);
            var host = root.AddComponent<WhiteboxGameSessionHost>();
            host.Configure(world, "测试目标");
            var save = root.AddComponent<WhiteboxSaveController>();
            save.Configure(host, saveKey, true);
            save.StartNewGame();

            var question = QuestionRewarding(domain, PartId.Axle);
            host.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            clock.Now = start.AddMinutes(5);
            InvokeApplicationPause(save, true);

            Assert.That(domain.IsTimingPaused, Is.True);
            Assert.That(ReadStoredSnapshot().PausedAtUnixMilliseconds,
                Is.EqualTo(clock.Now.ToUnixTimeMilliseconds()));

            clock.Now = start.AddHours(1).AddMinutes(5);
            InvokeApplicationPause(save, false);

            Assert.That(domain.IsTimingPaused, Is.False);
            Assert.That(domain.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(ReadStoredSnapshot().PausedAtUnixMilliseconds,
                Is.EqualTo(WhiteboxGameSessionSnapshot.MissingTimestamp));

            clock.Now = clock.Now.AddMinutes(3);
            Assert.That(domain.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(8)));
        }

        [Test]
        public void SettlementFormatsElapsedTimeAccuracyScoreAndEngineerGrade()
        {
            var now = new DateTimeOffset(2026, 8, 6, 8, 0, 0, TimeSpan.Zero);
            var session = new WhiteboxGameSession(WhiteboxGameCatalog.CreateDefault(), () => now);
            var question = session.Catalog.Questions[0];
            Assert.That(session.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
            now = now.AddHours(1).AddMinutes(2).AddSeconds(3);

            var formatted = WhiteboxHudPresenter.FormatSettlement(session.Progress);

            Assert.That(formatted, Does.Contain("01:02:03"));
            Assert.That(formatted, Does.Contain("1/1"));
            Assert.That(formatted, Does.Contain("100%"));
            Assert.That(formatted, Does.Contain("100"));
            Assert.That(formatted, Does.Contain(session.EngineerGradeDisplayName));
        }

        [Test]
        public void QuizStationRebuildsUnlockedAndCollectedLocalStateAfterHostRestore()
        {
            var host = CreateHost(root, out _);
            var inputLock = root.AddComponent<ThirdPersonInputLock>();
            var rewardVisual = Child("RewardVisual");
            var station = root.AddComponent<QuizPartStation>();
            station.Configure(
                host,
                inputLock,
                null,
                Array.Empty<QuizQuestionPresentation>(),
                PartId.Axle,
                "车轴答题工位",
                rewardVisual,
                "继续装配");

            var restored = new WhiteboxGameSession();
            var question = QuestionRewarding(restored, PartId.Axle);
            Assert.That(restored.SubmitAnswer(question.Id, question.CorrectOptionIndex).IsCorrect, Is.True);
            host.RestoreSession(restored.ExportSnapshot());

            Assert.That(station.RewardUnlocked, Is.True);
            Assert.That(station.IsCollected, Is.False);
            Assert.That(rewardVisual.activeSelf, Is.True);

            Assert.That(restored.CollectPart(PartId.Axle).Status, Is.EqualTo(PartCollectionStatus.Collected));
            host.RestoreSession(restored.ExportSnapshot());

            Assert.That(station.RewardUnlocked, Is.True);
            Assert.That(station.IsCollected, Is.True);
            Assert.That(rewardVisual.activeSelf, Is.False);
        }

        private WhiteboxGameSessionSnapshot ReadStoredSnapshot()
        {
            return JsonUtility.FromJson<WhiteboxGameSessionSnapshot>(PlayerPrefs.GetString(saveKey));
        }

        private static QuizQuestionDefinition QuestionRewarding(
            WhiteboxGameSession session,
            PartId rewardPart)
        {
            return session.Catalog.Questions.First(question => question.RewardPart == rewardPart);
        }

        private static void InvokeApplicationPause(
            WhiteboxSaveController save,
            bool paused)
        {
            // Unity 6's EditMode runner rejects SendMessage for lifecycle
            // callbacks with a ShouldRunBehaviour assertion. Invoke the exact
            // callback directly so this test covers the production path while
            // remaining independent of Unity's play-mode message dispatcher.
            var callback = typeof(WhiteboxSaveController).GetMethod(
                "OnApplicationPause",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(callback, Is.Not.Null);
            callback.Invoke(save, new object[] { paused });
        }

        private static void AssertAudioVolumeWhenBackendIsAvailable(float expected)
        {
            // Unity's -batchmode -nographics test runner has no audio backend;
            // AudioListener.volume remains at its native default even though
            // persistence and the requested runtime state are applied. Player
            // and GUI-editor runs still verify the native property directly.
            if (AudioSettings.GetConfiguration().sampleRate <= 0)
                return;

            Assert.That(AudioListener.volume, Is.EqualTo(expected));
        }

        private static WhiteboxGameSessionHost CreateHost(
            GameObject owner,
            out DomainWorldGameSession session)
        {
            var host = owner.AddComponent<WhiteboxGameSessionHost>();
            session = new DomainWorldGameSession();
            host.Configure(session, "测试目标");
            return host;
        }

        private MenuView CreateMenu(
            WhiteboxGameSessionHost host,
            WhiteboxSaveController save,
            ThirdPersonInputLock inputLock,
            WhiteboxKnowledgePresenter knowledgePresenter = null,
            bool includeAssemblyVariantDropdown = false)
        {
            var menuRoot = Child("MainMenu");
            var settingsRoot = Child("Settings");
            var start = Child("Start", menuRoot).AddComponent<Button>();
            var continueButton = Child("Continue", menuRoot).AddComponent<Button>();
            var settings = Child("SettingsButton", menuRoot).AddComponent<Button>();
            var quit = Child("Quit", menuRoot).AddComponent<Button>();
            var back = Child("Back", settingsRoot).AddComponent<Button>();
            var menu = Child("MenuButton").AddComponent<Button>();
            var slider = Child("Volume", settingsRoot).AddComponent<Slider>();
            var volumeText = Child("VolumeText", settingsRoot).AddComponent<Text>();
            var quality = Child("Quality", settingsRoot).AddComponent<Dropdown>();
            var assemblyVariant = includeAssemblyVariantDropdown
                ? Child("AssemblyVariant", menuRoot).AddComponent<Dropdown>()
                : null;
            var controller = root.AddComponent<WhiteboxMainMenuController>();
            controller.Configure(
                host,
                save,
                inputLock,
                menuRoot,
                settingsRoot,
                start,
                continueButton,
                settings,
                quit,
                back,
                slider,
                volumeText,
                quality,
                menu,
                configuredKnowledgePresenter: knowledgePresenter,
                configuredAssemblyVariantDropdown: assemblyVariant);
            return new MenuView(controller, start, continueButton, settings, assemblyVariant);
        }

        private GameObject Child(string name, GameObject parent = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent((parent ?? root).transform, false);
            return child;
        }

        private readonly struct MenuView
        {
            public MenuView(
                WhiteboxMainMenuController controller,
                Button startButton,
                Button continueButton,
                Button settingsButton,
                Dropdown assemblyVariantDropdown)
            {
                Controller = controller;
                StartButton = startButton;
                ContinueButton = continueButton;
                SettingsButton = settingsButton;
                AssemblyVariantDropdown = assemblyVariantDropdown;
            }

            public WhiteboxMainMenuController Controller { get; }
            public Button StartButton { get; }
            public Button ContinueButton { get; }
            public Button SettingsButton { get; }
            public Dropdown AssemblyVariantDropdown { get; }
        }

        private sealed class ManualClock
        {
            internal ManualClock(DateTimeOffset now)
            {
                Now = now;
            }

            internal DateTimeOffset Now { get; set; }

            internal DateTimeOffset UtcNow()
            {
                return Now;
            }
        }
    }
}
