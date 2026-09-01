using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    public static class WhiteboxWindowsBuild
    {
        public const string ScenePath =
            "Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity";
        public const string FinalShowcaseScenePath =
            "Assets/RailCraft/ThirdPerson/Scenes/FinalShowcase.unity";
        public const string RelativeOutputPath =
            "Builds/Whitebox/RailCraftWhitebox.exe";
        public const string SuccessLogMarker =
            "RAILCRAFT_WHITEBOX_BUILD_SUCCEEDED";

        [MenuItem("RailCraft/Third Person Whitebox/Build Windows x86_64")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void Build()
        {
            // The whitebox scene is generated source-of-truth. Rebuild it before every
            // player build so a clean checkout cannot package a stale serialized scene.
            WhiteboxSceneBuilder.Build();
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (scene == null)
            {
                throw new BuildFailedException(
                    $"Whitebox scene is missing: {ScenePath}. " +
                    "Run RailCraft/Third Person Whitebox/Rebuild Scene first.");
            }

            var showcaseModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                FinalShowcaseSceneBuilder.ModelAssetPath);
            var finalShowcaseReady = false;
            if (showcaseModel != null)
            {
                finalShowcaseReady = FinalShowcaseSceneBuilder.Build();
                if (!finalShowcaseReady)
                {
                    throw new BuildFailedException(
                        "FinalShowcase model was found but could not produce an imported-train scene.");
                }
            }
            else if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FinalShowcaseScenePath) != null)
            {
                Debug.LogWarning(
                    "Ignoring an existing FinalShowcase scene because the current FBX could not be loaded.");
            }

            var buildScenes = ResolveBuildScenePaths(finalShowcaseReady);

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new BuildFailedException("Could not resolve the Unity project root.");

            var outputPath = Path.GetFullPath(Path.Combine(
                projectRoot,
                RelativeOutputPath.Replace('/', Path.DirectorySeparatorChar)));
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new BuildFailedException("Could not resolve the whitebox build directory.");

            var buildsRoot = Path.GetFullPath(Path.Combine(projectRoot, "Builds"));
            var stagingDirectory = Path.Combine(buildsRoot, "Whitebox.staging");
            var previousDirectory = Path.Combine(buildsRoot, "Whitebox.previous");
            ValidateManagedBuildDirectory(buildsRoot, outputDirectory);
            ValidateManagedBuildDirectory(buildsRoot, stagingDirectory);
            ValidateManagedBuildDirectory(buildsRoot, previousDirectory);

            if (Directory.Exists(stagingDirectory))
                Directory.Delete(stagingDirectory, true);
            Directory.CreateDirectory(stagingDirectory);
            var stagingOutputPath = Path.Combine(stagingDirectory, Path.GetFileName(outputPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = buildScenes,
                locationPathName = stagingOutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode
            });

            if (report == null || report.summary.result != BuildResult.Succeeded)
            {
                var summary = report?.summary;
                throw new BuildFailedException(
                    $"Whitebox Windows build failed: result={summary?.result};" +
                    $"errors={summary?.totalErrors};warnings={summary?.totalWarnings}");
            }

            if (!File.Exists(stagingOutputPath))
                throw new BuildFailedException($"Whitebox executable is missing: {stagingOutputPath}");

            PromoteSuccessfulBuild(outputDirectory, stagingDirectory, previousDirectory);
            if (!File.Exists(outputPath))
                throw new BuildFailedException($"Promoted whitebox executable is missing: {outputPath}");

            Debug.Log(
                $"{SuccessLogMarker} output={RelativeOutputPath};" +
                $"bytes={report.summary.totalSize};" +
                $"warnings={report.summary.totalWarnings};" +
                $"errors={report.summary.totalErrors}");
        }

        public static string[] ResolveBuildScenePaths(bool finalShowcaseExists)
        {
            var scenes = new List<string> { ScenePath };
            if (finalShowcaseExists)
                scenes.Add(FinalShowcaseScenePath);
            return scenes.ToArray();
        }

        private static void PromoteSuccessfulBuild(
            string outputDirectory,
            string stagingDirectory,
            string previousDirectory)
        {
            if (Directory.Exists(previousDirectory))
                Directory.Delete(previousDirectory, true);

            try
            {
                if (Directory.Exists(outputDirectory))
                    Directory.Move(outputDirectory, previousDirectory);
                Directory.Move(stagingDirectory, outputDirectory);
                if (Directory.Exists(previousDirectory))
                    Directory.Delete(previousDirectory, true);
            }
            catch
            {
                if (!Directory.Exists(outputDirectory) && Directory.Exists(previousDirectory))
                    Directory.Move(previousDirectory, outputDirectory);
                throw;
            }
        }

        private static void ValidateManagedBuildDirectory(string buildsRoot, string candidatePath)
        {
            var fullRoot = Path.GetFullPath(buildsRoot).TrimEnd(Path.DirectorySeparatorChar);
            var fullCandidate = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar);
            var requiredPrefix = fullRoot + Path.DirectorySeparatorChar;
            if (!fullCandidate.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException($"Unsafe whitebox build path: {fullCandidate}");

            var current = new DirectoryInfo(fullCandidate);
            while (current != null && current.FullName.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new BuildFailedException($"Whitebox build path contains a reparse point: {current.FullName}");
                if (string.Equals(current.FullName.TrimEnd(Path.DirectorySeparatorChar), fullRoot,
                    StringComparison.OrdinalIgnoreCase))
                    break;
                current = current.Parent;
            }
        }
    }
}
