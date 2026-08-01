using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RailCraft.Flow;
using RailCraft.Presentation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RailCraft.Tests.PlayMode
{
    public sealed class PerformanceCaptureTests
    {
        private const string CaptureEnvironmentVariable = "RAILCRAFT_CAPTURE_PERFORMANCE";
        private const string OutputEnvironmentVariable = "RAILCRAFT_PERFORMANCE_OUTPUT";
        private const float SampleSeconds = 1.5f;

        [UnityTest]
        public IEnumerator CompleteProductionFlowMeetsPlaceholderPerformanceBudget()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable(CaptureEnvironmentVariable),
                    "1", StringComparison.Ordinal))
            {
                Assert.Ignore("Hardware performance capture requires RAILCRAFT_CAPTURE_PERFORMANCE=1.");
                yield break;
            }

            var previousTargetFrameRate = Application.targetFrameRate;
            var previousVsync = QualitySettings.vSyncCount;
            var previousQuality = QualitySettings.GetQualityLevel();
            var report = new PerformanceReport
            {
                unityVersion = Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                systemMemoryMb = SystemInfo.systemMemorySize,
                width = 1920,
                height = 1080,
                quality = QualitySettings.names.Last(),
                captures = Array.Empty<PerformanceCapture>()
            };
            var captures = new List<PerformanceCapture>();

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
            Screen.SetResolution(1920, 1080, false);
            yield return null;
            yield return null;
            report.width = Screen.width;
            report.height = Screen.height;
            report.quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            report.graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString();
            report.fullScreenMode = Screen.fullScreenMode.ToString();
            report.batchMode = Application.isBatchMode;
            report.frameTimingStatsEnabled = FrameTimingManager.IsFeatureEnabled();
            Assert.That(report.frameTimingStatsEnabled, Is.True, "frame timing stats");
            Assert.That(report.width, Is.EqualTo(1920), "performance capture width");
            Assert.That(report.height, Is.EqualTo(1080), "performance capture height");

            var loadStart = Time.realtimeSinceStartup;
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Additive);
            var bootstrap = SceneManager.GetSceneByName("Bootstrap");
            var controller = bootstrap.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GuidedFlowController>(true))
                .Single();
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!controller.IsConfigured && controller.FatalErrorCode == null
                && Time.realtimeSinceStartup < deadline)
                yield return null;
            report.initialFactoryLoadSeconds = Time.realtimeSinceStartup - loadStart;
            Assert.That(controller.FatalErrorCode, Is.Null);
            Assert.That(controller.IsConfigured, Is.True);
            Assert.That(report.initialFactoryLoadSeconds, Is.LessThanOrEqualTo(10f));

            yield return Capture("factory idle", capture => captures.Add(capture));

            controller.StartNewRun();
            controller.ConfirmGuidance();
            yield return AdvanceUntil(controller, "carbody_lowering", FlowPhase.KnowledgeGate);
            yield return Capture("all bogie modules installed", capture => captures.Add(capture));

            yield return AnswerKnowledgeGate(controller);
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(FlowPhase.StepReady));
            yield return Capture("carbody lowering", capture => captures.Add(capture));
            controller.CompleteCurrentStep();
            yield return null;

            yield return AdvanceUntil(controller, "commissioning", FlowPhase.StepReady);
            controller.CompleteCurrentStep();
            yield return null;
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Rework));
            yield return Capture("commissioning feedback", capture => captures.Add(capture));

            yield return AdvanceToCompletion(controller);
            yield return Capture("final hero view", capture => captures.Add(capture));

            report.captures = captures.ToArray();
            var outputPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..",
                    "TestResults", "task12-performance.json"));
            else
                outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, JsonUtility.ToJson(report, true));
            Debug.Log("RAILCRAFT_PERFORMANCE_REPORT " + outputPath);

            foreach (var capture in captures)
            {
                Assert.That(capture.averageFps, Is.GreaterThanOrEqualTo(60f), capture.state);
                Assert.That(capture.onePercentLowFps, Is.GreaterThanOrEqualTo(45f), capture.state);
                Assert.That(capture.peakDrawCalls, Is.LessThanOrEqualTo(500), capture.state);
                Assert.That(capture.peakTriangles, Is.LessThanOrEqualTo(2000000), capture.state);
            }

            QualitySettings.SetQualityLevel(previousQuality, true);
            QualitySettings.vSyncCount = previousVsync;
            Application.targetFrameRate = previousTargetFrameRate;
            yield return UnloadIfLoaded("Bootstrap");
            yield return UnloadIfLoaded("Factory");
        }

        private static IEnumerator AdvanceUntil(GuidedFlowController controller,
            string stepId, FlowPhase phase)
        {
            var safety = 0;
            while ((!string.Equals(controller.Snapshot.CurrentStepId, stepId,
                       StringComparison.Ordinal) || controller.Snapshot.Phase != phase)
                && safety++ < 128)
            {
                switch (controller.Snapshot.Phase)
                {
                    case FlowPhase.KnowledgeGate:
                        yield return AnswerKnowledgeGate(controller);
                        break;
                    case FlowPhase.StepReady:
                        controller.CompleteCurrentStep();
                        yield return null;
                        break;
                    case FlowPhase.Rework:
                        controller.AcknowledgeRework();
                        yield return null;
                        break;
                    case FlowPhase.SecondCommissioning:
                        controller.CompleteSecondCommissioning();
                        yield return null;
                        break;
                    default:
                        yield return null;
                        break;
                }
            }
            Assert.That(safety, Is.LessThan(128), stepId);
        }

        private static IEnumerator AdvanceToCompletion(GuidedFlowController controller)
        {
            var safety = 0;
            while (controller.Snapshot.Phase != FlowPhase.Completed && safety++ < 160)
            {
                switch (controller.Snapshot.Phase)
                {
                    case FlowPhase.KnowledgeGate:
                        yield return AnswerKnowledgeGate(controller);
                        break;
                    case FlowPhase.StepReady:
                        controller.CompleteCurrentStep();
                        yield return null;
                        break;
                    case FlowPhase.Rework:
                        controller.AcknowledgeRework();
                        yield return null;
                        break;
                    case FlowPhase.SecondCommissioning:
                        controller.CompleteSecondCommissioning();
                        yield return null;
                        break;
                    default:
                        yield return null;
                        break;
                }
            }
            Assert.That(controller.Snapshot.Phase, Is.EqualTo(FlowPhase.Completed));
        }

        private static IEnumerator AnswerKnowledgeGate(GuidedFlowController controller)
        {
            var quizPresenter = controller.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<QuizPresenter>(true))
                .SingleOrDefault();
            Assert.That(quizPresenter, Is.Not.Null);
            var safety = 0;
            while (controller.Snapshot.Phase == FlowPhase.KnowledgeGate && safety++ < 12)
            {
                var question = controller.CurrentQuestion;
                Assert.That(question, Is.Not.Null);
                var questionId = question.id;
                controller.SubmitAnswer(question.correctOptionIndex);
                var deadline = Time.realtimeSinceStartup + 2f;
                while (quizPresenter.IsTransitioning && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(quizPresenter.IsTransitioning, Is.False, questionId);
                Assert.That(controller.Snapshot.Phase != FlowPhase.KnowledgeGate
                    || controller.CurrentQuestion == null
                    || !string.Equals(controller.CurrentQuestion.id, questionId,
                        StringComparison.Ordinal), Is.True, questionId);
                yield return null;
            }
            Assert.That(safety, Is.LessThan(12));
        }

        private static IEnumerator Capture(string state, Action<PerformanceCapture> completed)
        {
            using var drawCalls = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "Draw Calls Count", 1);
            using var triangles = ProfilerRecorder.StartNew(
                ProfilerCategory.Render, "Triangles Count", 1);
            using var mainThread = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal, "Main Thread", 1);
            using var renderThread = ProfilerRecorder.StartNew(
                ProfilerCategory.Internal, "Render Thread", 1);

            Assert.That(drawCalls.Valid, Is.True, "Draw Calls Count");
            Assert.That(triangles.Valid, Is.True, "Triangles Count");
            for (var index = 0; index < 30; index++)
                yield return null;

            var frameDurations = new List<float>();
            var mainTimes = new List<float>();
            var renderTimes = new List<float>();
            var frameTiming = new FrameTiming[1];
            long peakDrawCalls = 0;
            long peakTriangles = 0;
            long peakMemory = 0;
            var end = Time.realtimeSinceStartup + SampleSeconds;
            while (Time.realtimeSinceStartup < end)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                if (Time.unscaledDeltaTime > 0f)
                    frameDurations.Add(Time.unscaledDeltaTime);
                peakDrawCalls = Math.Max(peakDrawCalls, drawCalls.LastValue);
                peakTriangles = Math.Max(peakTriangles, triangles.LastValue);
                peakMemory = Math.Max(peakMemory, Profiler.GetTotalAllocatedMemoryLong());

                var timingCount = FrameTimingManager.GetLatestTimings(1, frameTiming);
                var mainSample = timingCount > 0
                    ? (float)frameTiming[0].cpuMainThreadFrameTime
                    : 0f;
                var renderSample = timingCount > 0
                    ? (float)frameTiming[0].cpuRenderThreadFrameTime
                    : 0f;
                if (mainSample <= 0f && mainThread.Valid && mainThread.LastValue > 0)
                    mainSample = mainThread.LastValue / 1000000f;
                if (renderSample <= 0f && renderThread.Valid && renderThread.LastValue > 0)
                    renderSample = renderThread.LastValue / 1000000f;
                if (mainSample > 0f)
                    mainTimes.Add(mainSample);
                if (renderSample > 0f)
                    renderTimes.Add(renderSample);
            }

            Assert.That(frameDurations, Is.Not.Empty, state);
            Assert.That(peakDrawCalls, Is.GreaterThan(0),
                $"{state}: no rendered draw-call samples were captured");
            Assert.That(peakTriangles, Is.GreaterThan(0),
                $"{state}: no rendered triangle samples were captured");
            Assert.That(mainTimes, Is.Not.Empty, $"{state}: no main-thread samples");
            Assert.That(renderTimes, Is.Not.Empty, $"{state}: no render-thread samples");
            var orderedSlowestFirst = frameDurations.OrderByDescending(value => value).ToArray();
            var lowCount = Math.Max(1,
                (int)Math.Ceiling(orderedSlowestFirst.Length * 0.01));
            var capture = new PerformanceCapture
            {
                state = state,
                averageFps = frameDurations.Count / frameDurations.Sum(),
                onePercentLowFps = lowCount / orderedSlowestFirst.Take(lowCount).Sum(),
                peakDrawCalls = peakDrawCalls,
                peakTriangles = peakTriangles,
                averageMainThreadMs = mainTimes.Count == 0 ? 0f : mainTimes.Average(),
                averageRenderThreadMs = renderTimes.Count == 0 ? 0f : renderTimes.Average(),
                peakAllocatedMemoryMb = peakMemory / (1024f * 1024f),
                sampledFrames = frameDurations.Count
            };
            Debug.Log($"RAILCRAFT_PERF state={state};fps={capture.averageFps:F1};" +
                $"low1={capture.onePercentLowFps:F1};draw={capture.peakDrawCalls};" +
                $"triangles={capture.peakTriangles};main_ms={capture.averageMainThreadMs:F2};" +
                $"render_ms={capture.averageRenderThreadMs:F2};memory_mb={capture.peakAllocatedMemoryMb:F1}");
            completed(capture);
        }

        private static IEnumerator UnloadIfLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        [Serializable]
        private sealed class PerformanceReport
        {
            public string unityVersion;
            public string operatingSystem;
            public string processor;
            public string graphicsDevice;
            public string graphicsDeviceType;
            public int graphicsMemoryMb;
            public int systemMemoryMb;
            public int width;
            public int height;
            public string quality;
            public string fullScreenMode;
            public bool batchMode;
            public bool frameTimingStatsEnabled;
            public float initialFactoryLoadSeconds;
            public PerformanceCapture[] captures;
        }

        [Serializable]
        private sealed class PerformanceCapture
        {
            public string state;
            public float averageFps;
            public float onePercentLowFps;
            public long peakDrawCalls;
            public long peakTriangles;
            public float averageMainThreadMs;
            public float averageRenderThreadMs;
            public float peakAllocatedMemoryMb;
            public int sampledFrames;
        }
    }
}