using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RailCraft.ThirdPerson.Editor
{
    public static class ModelCandidateRegistry
    {
        public const string AssemblyDemoRoot =
            "Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo";

        public const string CandidateRoot =
            "Assets/RailCraft/ThirdPerson/Art/Models/Candidates";

        public const string DefaultBogieDemoPath = AssemblyDemoRoot + "/BogieAssemblyDemo.fbx";
        public const string DefaultCarbodyDemoPath =
            AssemblyDemoRoot + "/FuxingCarbodyAssemblyDemo.fbx";

        public static readonly string[] BogieSearchPaths =
        {
            // Semantic part extraction must keep the inspected default FBX
            // whenever it exists; whole-model candidates are selected through
            // AssemblyVariantVisualFactory instead.
            DefaultBogieDemoPath,
            "Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/Y25Freight/Y25FreightBogie.fbx",
            "Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/MetroSimplified/MetroSimplifiedBogie.fbx",
            "Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/TeachingConcept/TeachingConceptBogie.fbx",
            CandidateRoot + "/Y25/Y25BogieAssemblyDemo.fbx",
            CandidateRoot + "/Metro/MetroBogieAssemblyDemo.fbx",
            CandidateRoot + "/Teaching/TeachingBogieAssemblyDemo.fbx"
        };

        public static readonly string[] CarbodySearchPaths =
        {
            DefaultCarbodyDemoPath,
            CandidateRoot + "/Fuxing/FuxingCarbodyAssemblyDemo.fbx"
        };

        public static string ResolveFirstAvailable(IEnumerable<string> searchPaths)
        {
            if (searchPaths == null)
                return null;

            foreach (var path in searchPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                    return path;
            }

            return null;
        }

        public static string GetBogieModelAssetPath() =>
            ResolveFirstAvailable(BogieSearchPaths);

        public static string GetCarbodyModelAssetPath() =>
            ResolveFirstAvailable(CarbodySearchPaths);

        public static bool HasAnyBogieModel => GetBogieModelAssetPath() != null;

        public static bool HasAnyCarbodyModel => GetCarbodyModelAssetPath() != null;

        [MenuItem("RailCraft/Models/Report Candidate Availability")]
        public static void ReportCandidateAvailability()
        {
            var lines = new List<string>
            {
                $"bogie={GetBogieModelAssetPath() ?? "missing"}",
                $"carbody={GetCarbodyModelAssetPath() ?? "missing"}"
            };

            foreach (var path in BogieSearchPaths.Concat(CarbodySearchPaths).Distinct())
            {
                lines.Add($"{path}={(AssetDatabase.LoadAssetAtPath<GameObject>(path) != null ? "loaded" : "missing")}");
            }

            Debug.Log(string.Join(Environment.NewLine, lines));
        }
    }
}
