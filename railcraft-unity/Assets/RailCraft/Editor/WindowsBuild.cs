using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RailCraft.Editor
{
    public static class WindowsBuild
    {
        public const string RelativeOutputPath = "Builds/Windows/RailCraft.exe";
        public const string SuccessLogMarker = "RAILCRAFT_WINDOWS_BUILD_SUCCEEDED";
        private static readonly string[] LegacyScenePaths =
        {
            "Assets/RailCraft/Scenes/Bootstrap.unity",
            "Assets/RailCraft/Scenes/Factory.unity"
        };

        [MenuItem("RailCraft/Build/Windows x86_64")]
        public static void Build()
        {
            try
            {
                BuildWindowsPlayer();
            }
            catch (BuildFailedException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BuildFailedException(
                    $"RailCraft Windows build aborted: {exception.GetType().Name}: {exception.Message}");
            }
        }

        private static void BuildWindowsPlayer()
        {
            var scenes = LegacyScenePaths.ToArray();
            foreach (var scenePath in scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                    throw new BuildFailedException($"Frozen v0.1 build scene is missing: {scenePath}");
            }

            var projectRoot = GetProjectRoot();
            var platformRelativeOutputPath = RelativeOutputPath.Replace(
                '/',
                Path.DirectorySeparatorChar);
            var outputPath = Path.GetFullPath(Path.Combine(projectRoot, platformRelativeOutputPath));
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(outputDirectory))
                throw new BuildFailedException("Could not resolve the RailCraft Windows build directory.");

            CleanOutputDirectory(outputDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache | BuildOptions.StrictMode
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report == null)
                throw new BuildFailedException("RailCraft Windows build returned no build report.");

            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"RailCraft Windows build failed: result={summary.result};" +
                    $"errors={summary.totalErrors};warnings={summary.totalWarnings};output={outputPath}");
            }

            if (!File.Exists(outputPath))
            {
                throw new BuildFailedException(
                    $"RailCraft Windows build reported success but the executable is missing: {outputPath}");
            }

            var machineSummary = new BuildSummaryRecord
            {
                result = summary.result.ToString(),
                target = BuildTarget.StandaloneWindows64.ToString(),
                output = outputPath,
                sceneCount = scenes.Length,
                totalBytes = checked((long)summary.totalSize),
                durationMilliseconds = checked((long)summary.totalTime.TotalMilliseconds),
                warnings = checked((int)summary.totalWarnings),
                errors = checked((int)summary.totalErrors)
            };

            Debug.Log($"{RelativeOutputPath} exists");
            Debug.Log("Build result: Succeeded");
            Debug.Log($"{SuccessLogMarker} {JsonUtility.ToJson(machineSummary)}");
        }

        private static string GetProjectRoot()
        {
            var assetsDirectory = new DirectoryInfo(Application.dataPath);
            if (assetsDirectory.Parent == null)
                throw new BuildFailedException("Could not resolve the Unity project root from Application.dataPath.");

            return assetsDirectory.Parent.FullName;
        }

        private static void CleanOutputDirectory(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);

            Directory.CreateDirectory(outputDirectory);
        }

        [Serializable]
        private sealed class BuildSummaryRecord
        {
            public string result;
            public string target;
            public string output;
            public int sceneCount;
            public long totalBytes;
            public long durationMilliseconds;
            public int warnings;
            public int errors;
        }
    }
}
