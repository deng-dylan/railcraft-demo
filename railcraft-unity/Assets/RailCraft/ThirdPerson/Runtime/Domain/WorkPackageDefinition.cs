using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RailCraft.ThirdPerson.Domain
{
    /// <summary>
    /// A player-facing engineering work package. A package may contain several
    /// domain PartId values; question count and package count are independent.
    /// </summary>
    public enum WorkPackageId
    {
        WheelsetAxlebox,
        FrameAndBrakeTraction,
        PrimarySuspension,
        SecondarySuspension,
        CarbodyAndLanding,
        Commissioning
    }

    public sealed class WorkPackageDefinition
    {
        private readonly ReadOnlyCollection<PartId> requiredParts;
        private readonly ReadOnlyCollection<string> coreQuestionIds;

        internal WorkPackageDefinition(
            WorkPackageId id,
            string key,
            string displayName,
            IEnumerable<PartId> requiredParts,
            bool materialPackage)
            : this(
                id,
                key,
                displayName,
                requiredParts,
                materialPackage,
                null,
                Array.Empty<string>())
        {
        }

        internal WorkPackageDefinition(
            WorkPackageId id,
            string key,
            string displayName,
            IEnumerable<PartId> requiredParts,
            bool materialPackage,
            ModuleId? assemblyModule,
            IEnumerable<string> coreQuestionIds)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A work package key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A work package display name is required.", nameof(displayName));
            if (requiredParts == null)
                throw new ArgumentNullException(nameof(requiredParts));
            if (coreQuestionIds == null)
                throw new ArgumentNullException(nameof(coreQuestionIds));

            var copiedParts = new List<PartId>(requiredParts);
            if (copiedParts.Count != new HashSet<PartId>(copiedParts).Count)
                throw new ArgumentException("A work package cannot repeat a part.", nameof(requiredParts));

            var copiedQuestionIds = new List<string>();
            var questionIdSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var questionId in coreQuestionIds)
            {
                if (string.IsNullOrWhiteSpace(questionId))
                    throw new ArgumentException(
                        "Core question ids cannot be blank.",
                        nameof(coreQuestionIds));
                if (!questionIdSet.Add(questionId))
                    throw new ArgumentException(
                        "A work package cannot repeat a core question.",
                        nameof(coreQuestionIds));
                copiedQuestionIds.Add(questionId);
            }

            if (!materialPackage && (assemblyModule.HasValue || copiedQuestionIds.Count > 0))
            {
                throw new ArgumentException(
                    "A non-material work package cannot define an assembly module or core questions.");
            }

            Id = id;
            Key = key;
            DisplayName = displayName;
            this.requiredParts = copiedParts.AsReadOnly();
            this.coreQuestionIds = copiedQuestionIds.AsReadOnly();
            IsMaterialPackage = materialPackage;
            AssemblyModule = assemblyModule;
        }

        public WorkPackageId Id { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public IReadOnlyList<PartId> RequiredParts => requiredParts;
        /// <summary>Question ids that must be answered correctly for this package gate.</summary>
        public IReadOnlyList<string> CoreQuestionIds => coreQuestionIds;
        public int RequiredQuestionCount => coreQuestionIds.Count;
        public bool IsMaterialPackage { get; }
        /// <summary>The direct assembly node that consumes this package's parts.</summary>
        public ModuleId? AssemblyModule { get; }

        // Alias retained for callers that describe the package's target as a
        // related module rather than an assembly module.
        public ModuleId? RelatedModule => AssemblyModule;

        public bool ContainsCoreQuestion(string questionId)
        {
            return questionId != null && coreQuestionIds.Contains(questionId);
        }
    }

    /// <summary>
    /// Single source of truth for the player-facing package grouping. The
    /// domain keeps the finer PartId recipe for assembly and save compatibility.
    /// </summary>
    public static class WhiteboxWorkPackageCatalog
    {
        private static readonly WorkPackageDefinition[] definitions =
        {
            new WorkPackageDefinition(
                WorkPackageId.WheelsetAxlebox,
                "wheelset-axlebox",
                "轮对轴箱",
                new[] { PartId.Axle, PartId.Wheel, PartId.Bearing },
                true,
                ModuleId.WheelsetAxlebox,
                new[] { "bank_mc04", "bank_mc05" }),
            new WorkPackageDefinition(
                WorkPackageId.FrameAndBrakeTraction,
                "frame-brake-traction",
                "构架与制动/牵引",
                new[] { PartId.BrakeDevice, PartId.TractionRod, PartId.SensorBracket },
                true,
                ModuleId.Frame,
                new[] { "bank_mc03", "bank_mc15" }),
            new WorkPackageDefinition(
                WorkPackageId.PrimarySuspension,
                "primary-suspension",
                "一系悬挂",
                new[]
                    {
                        PartId.PrimaryElasticElement,
                        PartId.PrimaryPositioningElement,
                        PartId.PrimaryDamper
                    },
                true,
                ModuleId.PrimarySuspension,
                new[] { "bank_mc10", "bank_tf02" }),
            new WorkPackageDefinition(
                WorkPackageId.SecondarySuspension,
                "secondary-suspension",
                "二系悬挂",
                new[]
                    {
                        PartId.SecondaryElasticElement,
                        PartId.HeightControlElement,
                        PartId.SecondaryDamper
                    },
                true,
                ModuleId.SecondarySuspension,
                new[] { "bank_mc22", "bank_mc25" }),
            new WorkPackageDefinition(
                WorkPackageId.CarbodyAndLanding,
                "carbody-landing",
                "车体与落车",
                new[] { PartId.Carbody, PartId.CentralTractionDevice },
                true,
                ModuleId.Landing,
                new[] { "bank_mc07", "bank_mc13" }),
            new WorkPackageDefinition(
                WorkPackageId.Commissioning,
                "commissioning",
                "调试与检验",
                Array.Empty<PartId>(),
                false)
        };

        private static readonly Dictionary<WorkPackageId, WorkPackageDefinition> byId = BuildById();
        private static readonly Dictionary<PartId, WorkPackageDefinition> byPart = BuildByPart();
        private static readonly Dictionary<string, WorkPackageDefinition> byCoreQuestion =
            BuildByCoreQuestion();

        public static IReadOnlyList<WorkPackageDefinition> Definitions => definitions;

        public static WorkPackageDefinition Get(WorkPackageId id)
        {
            if (!byId.TryGetValue(id, out var definition))
                throw new KeyNotFoundException($"Unknown work package: {id}.");
            return definition;
        }

        public static bool TryGet(
            WorkPackageId id,
            out WorkPackageDefinition definition)
        {
            return byId.TryGetValue(id, out definition);
        }

        public static bool TryGetForPart(PartId partId, out WorkPackageDefinition definition)
        {
            return byPart.TryGetValue(partId, out definition);
        }

        public static bool TryGetForAssemblyModule(
            ModuleId moduleId,
            out WorkPackageDefinition definition)
        {
            foreach (var candidate in definitions)
            {
                if (candidate.AssemblyModule.HasValue && candidate.AssemblyModule.Value == moduleId)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static bool TryGetForCoreQuestion(
            string questionId,
            out WorkPackageDefinition definition)
        {
            if (questionId == null)
            {
                definition = null;
                return false;
            }

            return byCoreQuestion.TryGetValue(questionId, out definition);
        }

        public static bool TryGetForQuestion(
            string questionId,
            out WorkPackageDefinition definition)
        {
            return TryGetForCoreQuestion(questionId, out definition);
        }

        private static Dictionary<WorkPackageId, WorkPackageDefinition> BuildById()
        {
            var lookup = new Dictionary<WorkPackageId, WorkPackageDefinition>();
            foreach (var definition in definitions)
            {
                if (lookup.ContainsKey(definition.Id))
                    throw new InvalidOperationException($"Duplicate work package: {definition.Id}.");
                lookup.Add(definition.Id, definition);
            }
            return lookup;
        }

        private static Dictionary<PartId, WorkPackageDefinition> BuildByPart()
        {
            var lookup = new Dictionary<PartId, WorkPackageDefinition>();
            foreach (var definition in definitions)
            {
                foreach (var partId in definition.RequiredParts)
                {
                    if (lookup.ContainsKey(partId))
                        throw new InvalidOperationException($"Part belongs to multiple work packages: {partId}.");
                    lookup.Add(partId, definition);
                }
            }
            return lookup;
        }

        private static Dictionary<string, WorkPackageDefinition> BuildByCoreQuestion()
        {
            var lookup = new Dictionary<string, WorkPackageDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                foreach (var questionId in definition.CoreQuestionIds)
                {
                    if (lookup.ContainsKey(questionId))
                    {
                        throw new InvalidOperationException(
                            $"Core question belongs to multiple work packages: {questionId}.");
                    }

                    lookup.Add(questionId, definition);
                }
            }

            return lookup;
        }
    }
}
