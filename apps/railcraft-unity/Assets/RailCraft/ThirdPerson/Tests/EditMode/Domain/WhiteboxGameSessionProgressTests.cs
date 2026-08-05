using System;
using NUnit.Framework;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.Tests.EditMode.Domain
{
    public sealed class WhiteboxGameSessionProgressTests
    {
        [Test]
        public void NewSessionStartsPendingWithEmptyStatistics()
        {
            var session = new WhiteboxGameSession();

            Assert.That(session.FlowStatus, Is.EqualTo(AssemblyFlowStatus.Pending));
            Assert.That(session.StartedAtUtc, Is.Null);
            Assert.That(session.CompletedAtUtc, Is.Null);
            Assert.That(session.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(session.AnswerAttemptCount, Is.Zero);
            Assert.That(session.CorrectAnswerCount, Is.Zero);
            Assert.That(session.AnswerAccuracy, Is.Zero);
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.Grade, Is.EqualTo(EngineerGrade.JuniorEngineer));
            Assert.That(session.EngineerGradeDisplayName, Is.EqualTo("初级工程师"));
        }

        [Test]
        public void OnlyKnownQuestionsWithValidSelectionsStartAndCountAttempts()
        {
            var clock = new ManualClock(new DateTimeOffset(2026, 8, 6, 1, 2, 3, TimeSpan.Zero));
            var session = NewSession(clock);
            var question = QuestionFor(session, PartId.Axle);
            var wrongOption = WrongOption(question);

            session.SubmitAnswer("missing", 0);
            session.SubmitAnswer(question.Id, question.Options.Count);

            Assert.That(session.FlowStatus, Is.EqualTo(AssemblyFlowStatus.Pending));
            Assert.That(session.AnswerAttemptCount, Is.Zero);

            session.SubmitAnswer(question.Id, wrongOption);

            Assert.That(session.FlowStatus, Is.EqualTo(AssemblyFlowStatus.InProgress));
            Assert.That(session.StartedAtUtc, Is.EqualTo(clock.Now));
            Assert.That(session.AnswerAttemptCount, Is.EqualTo(1));
            Assert.That(session.CorrectAnswerCount, Is.Zero);
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void AccuracyScoreAndEngineerGradeUseStableThresholds()
        {
            var junior = new WhiteboxGameSession();
            Answer(junior, 2, 1);
            Assert.That(junior.AnswerAttemptCount, Is.EqualTo(3));
            Assert.That(junior.CorrectAnswerCount, Is.EqualTo(2));
            Assert.That(junior.AnswerAccuracy, Is.EqualTo(2d / 3d).Within(0.000001d));
            Assert.That(junior.Score, Is.EqualTo(67));
            Assert.That(junior.Grade, Is.EqualTo(EngineerGrade.JuniorEngineer));

            var intermediate = new WhiteboxGameSession();
            Answer(intermediate, 3, 1);
            Assert.That(intermediate.Score, Is.EqualTo(75));
            Assert.That(intermediate.Grade, Is.EqualTo(EngineerGrade.IntermediateEngineer));
            Assert.That(intermediate.EngineerGradeDisplayName, Is.EqualTo("中级工程师"));

            var senior = new WhiteboxGameSession();
            Answer(senior, 9, 1);
            Assert.That(senior.Score, Is.EqualTo(90));
            Assert.That(senior.Grade, Is.EqualTo(EngineerGrade.SeniorEngineer));
            Assert.That(senior.EngineerGradeDisplayName, Is.EqualTo("高级工程师"));
        }

        [Test]
        public void ProgressSummaryMirrorsLiveSessionAndElapsedTime()
        {
            var start = new DateTimeOffset(2026, 8, 6, 2, 0, 0, TimeSpan.Zero);
            var clock = new ManualClock(start);
            var session = NewSession(clock);
            var question = QuestionFor(session, PartId.Axle);
            session.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            clock.Now = start.AddMinutes(12);

            var progress = session.Progress;

            Assert.That(progress.FlowStatus, Is.EqualTo(AssemblyFlowStatus.InProgress));
            Assert.That(progress.StartedAtUtc, Is.EqualTo(start));
            Assert.That(progress.CompletedAtUtc, Is.Null);
            Assert.That(progress.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(12)));
            Assert.That(progress.AnswerAttemptCount, Is.EqualTo(1));
            Assert.That(progress.CorrectAnswerCount, Is.EqualTo(1));
            Assert.That(progress.AnswerAccuracyPercent, Is.EqualTo(100d));
            Assert.That(progress.Score, Is.EqualTo(100));
            Assert.That(progress.EngineerGrade, Is.EqualTo(EngineerGrade.SeniorEngineer));
        }

        [Test]
        public void PauseAndResumeExcludeMenuAndOfflineTimeAcrossSnapshot()
        {
            var start = new DateTimeOffset(2026, 8, 6, 2, 30, 0, TimeSpan.Zero);
            var sourceClock = new ManualClock(start);
            var source = NewSession(sourceClock);
            var question = QuestionFor(source, PartId.Axle);
            source.SubmitAnswer(question.Id, question.CorrectOptionIndex);

            sourceClock.Now = start.AddMinutes(5);
            source.PauseTiming();
            var pausedSnapshot = source.ExportSnapshot();

            Assert.That(source.PausedAtUtc, Is.EqualTo(sourceClock.Now));
            Assert.That(source.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(pausedSnapshot.PausedAtUnixMilliseconds,
                Is.EqualTo(sourceClock.Now.ToUnixTimeMilliseconds()));

            sourceClock.Now = start.AddHours(1);
            Assert.That(source.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));

            var restoredClock = new ManualClock(start.AddHours(1));
            var restored = NewSession(restoredClock);
            restored.RestoreSnapshot(pausedSnapshot);

            Assert.That(restored.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));
            restored.ResumeTiming();
            Assert.That(restored.PausedAtUtc, Is.Null);
            Assert.That(restored.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(5)));

            restoredClock.Now = restoredClock.Now.AddMinutes(3);
            Assert.That(restored.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(8)));
            Assert.That(restored.ExportSnapshot().PausedAtUnixMilliseconds,
                Is.EqualTo(WhiteboxGameSessionSnapshot.MissingTimestamp));
        }

        [Test]
        public void FinalCommissioningCompletesAndFreezesTimingAndStatistics()
        {
            var start = new DateTimeOffset(2026, 8, 6, 3, 0, 0, TimeSpan.Zero);
            var clock = new ManualClock(start);
            var session = NewSession(clock);
            CompleteLanding(session);
            session.RunCommissioning();
            session.PerformRetuning();
            session.PerformInspection();
            clock.Now = start.AddMinutes(42);

            var result = session.RunCommissioning();

            Assert.That(result.Passed, Is.True);
            Assert.That(session.FlowStatus, Is.EqualTo(AssemblyFlowStatus.Completed));
            Assert.That(session.StartedAtUtc, Is.EqualTo(start));
            Assert.That(session.CompletedAtUtc, Is.EqualTo(clock.Now));
            Assert.That(session.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(42)));
            Assert.That(session.AnswerAttemptCount, Is.EqualTo(14));
            Assert.That(session.CorrectAnswerCount, Is.EqualTo(14));

            var completedAttempts = session.AnswerAttemptCount;
            var question = QuestionFor(session, PartId.Axle);
            session.SubmitAnswer(question.Id, WrongOption(question));
            clock.Now = start.AddHours(2);

            Assert.That(session.AnswerAttemptCount, Is.EqualTo(completedAttempts));
            Assert.That(session.ElapsedTime, Is.EqualTo(TimeSpan.FromMinutes(42)));
            Assert.That(session.CompletedAtUtc, Is.EqualTo(start.AddMinutes(42)));

            var invalidPausedCompletion = session.ExportSnapshot();
            invalidPausedCompletion.PausedAtUnixMilliseconds =
                invalidPausedCompletion.CompletedAtUnixMilliseconds;
            Assert.Throws<ArgumentException>(() =>
                NewSession(clock).RestoreSnapshot(invalidPausedCompletion));
        }

        [Test]
        public void SnapshotRoundTripRestoresMidCommissioningAndCanContinue()
        {
            var start = new DateTimeOffset(2026, 8, 6, 4, 0, 0, TimeSpan.Zero);
            var sourceClock = new ManualClock(start);
            var source = NewSession(sourceClock);
            CompleteLanding(source);
            source.RunCommissioning();
            var snapshot = source.ExportSnapshot();

            var restoredClock = new ManualClock(start.AddMinutes(10));
            var restored = NewSession(restoredClock);
            restored.RestoreSnapshot(snapshot);

            Assert.That(restored.FlowStatus, Is.EqualTo(AssemblyFlowStatus.InProgress));
            Assert.That(restored.StartedAtUtc, Is.EqualTo(start));
            Assert.That(restored.CommissioningPhase, Is.EqualTo(CommissioningPhase.NeedsRetuning));
            Assert.That(restored.IsLandingComplete, Is.True);
            Assert.That(restored.AreAllModulesComplete, Is.True);
            Assert.That(restored.AnswerAttemptCount, Is.EqualTo(14));
            Assert.That(restored.CorrectAnswerCount, Is.EqualTo(14));
            Assert.That(restored.CorrectQuestionIds, Is.EqualTo(source.CorrectQuestionIds));
            Assert.That(restored.Inventory.Parts, Is.Empty);
            foreach (var module in source.Catalog.Modules)
            {
                Assert.That(
                    restored.GetModuleState(module.Id).InstalledParts,
                    Is.EqualTo(source.GetModuleState(module.Id).InstalledParts));
                Assert.That(
                    restored.GetModuleState(module.Id).InstalledModules,
                    Is.EqualTo(source.GetModuleState(module.Id).InstalledModules));
            }

            restored.PerformRetuning();
            restored.PerformInspection();
            restoredClock.Now = start.AddMinutes(20);
            restored.RunCommissioning();

            Assert.That(restored.IsVehicleComplete, Is.True);
            Assert.That(restored.FlowStatus, Is.EqualTo(AssemblyFlowStatus.Completed));
            Assert.That(restored.CompletedAtUtc, Is.EqualTo(start.AddMinutes(20)));
        }

        [Test]
        public void StaticFactoryRestoresAPendingSnapshot()
        {
            var source = new WhiteboxGameSession();
            var snapshot = source.ExportSnapshot();

            var restored = WhiteboxGameSession.FromSnapshot(snapshot);

            Assert.That(restored.FlowStatus, Is.EqualTo(AssemblyFlowStatus.Pending));
            Assert.That(restored.StartedAtUtc, Is.Null);
            Assert.That(restored.UnlockedParts, Is.Empty);
            Assert.That(restored.Inventory.Parts, Is.Empty);
            Assert.That(snapshot.Modules, Has.Length.EqualTo(source.Catalog.Modules.Count));
        }

        [Test]
        public void ExportedSnapshotIsADeepCopy()
        {
            var session = new WhiteboxGameSession();
            UnlockAndCollect(session, PartId.Axle);
            session.InstallPart(ModuleId.WheelsetAxlebox, PartId.Axle);
            var snapshot = session.ExportSnapshot();

            var originalQuestionId = session.CorrectQuestionIds[0];
            snapshot.CorrectQuestionIds[0] = "mutated-question";
            snapshot.UnlockedParts[0] = (PartId)999;
            snapshot.CollectedParts[0] = (PartId)999;
            snapshot.Modules[0].InstalledParts[0] = (PartId)999;
            snapshot.Modules[0].InstalledParts = Array.Empty<PartId>();

            Assert.That(session.IsPartUnlocked(PartId.Axle), Is.True);
            Assert.That(session.IsPartCollected(PartId.Axle), Is.True);
            Assert.That(session.CorrectQuestionIds[0], Is.EqualTo(originalQuestionId));
            Assert.That(
                session.GetModuleState(ModuleId.WheelsetAxlebox).HasInstalled(PartId.Axle),
                Is.True);
        }

        [Test]
        public void InvalidSnapshotIsRejectedWithoutChangingCurrentSession()
        {
            var session = new WhiteboxGameSession();
            var question = QuestionFor(session, PartId.Axle);
            session.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            var before = session.ExportSnapshot();
            var invalid = session.ExportSnapshot();
            invalid.CorrectAnswerCount = invalid.AnswerAttemptCount + 1;

            Assert.Throws<ArgumentException>(() => session.RestoreSnapshot(invalid));

            var after = session.ExportSnapshot();
            Assert.That(after.FlowStatus, Is.EqualTo(before.FlowStatus));
            Assert.That(after.StartedAtUnixMilliseconds, Is.EqualTo(before.StartedAtUnixMilliseconds));
            Assert.That(after.AnswerAttemptCount, Is.EqualTo(before.AnswerAttemptCount));
            Assert.That(after.CorrectAnswerCount, Is.EqualTo(before.CorrectAnswerCount));
            Assert.That(after.UnlockedParts, Is.EqualTo(before.UnlockedParts));

            invalid = session.ExportSnapshot();
            invalid.CorrectQuestionIds = new[] { "unknown-question" };
            Assert.Throws<ArgumentException>(() => session.RestoreSnapshot(invalid));
            Assert.That(session.CorrectQuestionIds, Is.EqualTo(before.CorrectQuestionIds));
        }

        [Test]
        public void SnapshotRejectsUnsupportedSchemaAndBrokenMaterialAccounting()
        {
            var session = new WhiteboxGameSession();
            UnlockAndCollect(session, PartId.Axle);

            var unsupported = session.ExportSnapshot();
            unsupported.SchemaVersion++;
            Assert.Throws<ArgumentException>(() => new WhiteboxGameSession().RestoreSnapshot(unsupported));

            var broken = session.ExportSnapshot();
            broken.CollectedParts = Array.Empty<PartId>();
            Assert.Throws<ArgumentException>(() => new WhiteboxGameSession().RestoreSnapshot(broken));
            Assert.Throws<ArgumentNullException>(() => new WhiteboxGameSession().RestoreSnapshot(null));
        }

        [Test]
        public void SnapshotRejectsPauseTimestampsThatConflictWithFlowState()
        {
            var pending = new WhiteboxGameSession().ExportSnapshot();
            pending.PausedAtUnixMilliseconds = 0L;
            Assert.Throws<ArgumentException>(() => new WhiteboxGameSession().RestoreSnapshot(pending));

            var start = new DateTimeOffset(2026, 8, 6, 4, 30, 0, TimeSpan.Zero);
            var clock = new ManualClock(start);
            var inProgress = NewSession(clock);
            var question = QuestionFor(inProgress, PartId.Axle);
            inProgress.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            var invalidPause = inProgress.ExportSnapshot();
            invalidPause.PausedAtUnixMilliseconds = start.AddSeconds(-1).ToUnixTimeMilliseconds();

            Assert.Throws<ArgumentException>(() => NewSession(clock).RestoreSnapshot(invalidPause));
        }

        [Test]
        public void ResetClearsFlowTimingStatisticsAndSnapshotState()
        {
            var start = new DateTimeOffset(2026, 8, 6, 5, 0, 0, TimeSpan.Zero);
            var clock = new ManualClock(start);
            var session = NewSession(clock);
            CompleteLanding(session);
            session.RunCommissioning();
            session.PerformRetuning();
            session.PerformInspection();
            clock.Now = start.AddMinutes(30);
            session.RunCommissioning();

            session.Reset();
            var snapshot = session.ExportSnapshot();

            Assert.That(session.FlowStatus, Is.EqualTo(AssemblyFlowStatus.Pending));
            Assert.That(session.StartedAtUtc, Is.Null);
            Assert.That(session.CompletedAtUtc, Is.Null);
            Assert.That(session.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
            Assert.That(session.AnswerAttemptCount, Is.Zero);
            Assert.That(session.CorrectAnswerCount, Is.Zero);
            Assert.That(session.AnswerAccuracy, Is.Zero);
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.Grade, Is.EqualTo(EngineerGrade.JuniorEngineer));
            Assert.That(snapshot.StartedAtUnixMilliseconds,
                Is.EqualTo(WhiteboxGameSessionSnapshot.MissingTimestamp));
            Assert.That(snapshot.CompletedAtUnixMilliseconds,
                Is.EqualTo(WhiteboxGameSessionSnapshot.MissingTimestamp));
            Assert.That(snapshot.PausedAtUnixMilliseconds,
                Is.EqualTo(WhiteboxGameSessionSnapshot.MissingTimestamp));
            Assert.That(snapshot.UnlockedParts, Is.Empty);
            Assert.That(snapshot.CollectedParts, Is.Empty);
            Assert.That(snapshot.InventoryParts, Is.Empty);
            foreach (var module in snapshot.Modules)
            {
                Assert.That(module.InstalledParts, Is.Empty);
                Assert.That(module.InstalledModules, Is.Empty);
            }
        }

        private static WhiteboxGameSession NewSession(ManualClock clock)
        {
            return new WhiteboxGameSession(WhiteboxGameCatalog.CreateDefault(), clock.UtcNow);
        }

        private static void Answer(
            WhiteboxGameSession session,
            int correctCount,
            int incorrectCount)
        {
            var question = QuestionFor(session, PartId.Axle);
            for (var index = 0; index < correctCount; index++)
                session.SubmitAnswer(question.Id, question.CorrectOptionIndex);
            for (var index = 0; index < incorrectCount; index++)
                session.SubmitAnswer(question.Id, WrongOption(question));
        }

        private static int WrongOption(QuizQuestionDefinition question)
        {
            return (question.CorrectOptionIndex + 1) % question.Options.Count;
        }

        private static void CompleteLanding(WhiteboxGameSession session)
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

            foreach (var partId in session.Catalog.GetModule(ModuleId.Landing).RequiredParts)
            {
                UnlockAndCollect(session, partId);
                Assert.That(
                    session.InstallPart(ModuleId.Landing, partId).Status,
                    Is.EqualTo(PartInstallationStatus.Installed));
            }

            Assert.That(
                session.InstallModule(ModuleId.Landing, ModuleId.BogieStructure).Status,
                Is.EqualTo(ModuleInstallationStatus.Installed));
            Assert.That(
                session.InstallModule(ModuleId.Landing, ModuleId.SecondarySuspension).Status,
                Is.EqualTo(ModuleInstallationStatus.Installed));
        }

        private static void CompletePartAssembly(
            WhiteboxGameSession session,
            ModuleId moduleId)
        {
            foreach (var partId in session.Catalog.GetModule(moduleId).RequiredParts)
            {
                UnlockAndCollect(session, partId);
                Assert.That(
                    session.InstallPart(moduleId, partId).Status,
                    Is.EqualTo(PartInstallationStatus.Installed));
            }
        }

        private static void UnlockAndCollect(WhiteboxGameSession session, PartId partId)
        {
            var question = QuestionFor(session, partId);
            Assert.That(
                session.SubmitAnswer(question.Id, question.CorrectOptionIndex).Status,
                Is.EqualTo(QuizSubmissionStatus.Correct));
            Assert.That(
                session.CollectPart(partId).Status,
                Is.EqualTo(PartCollectionStatus.Collected));
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

            throw new InvalidOperationException($"Missing question for {partId}.");
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
