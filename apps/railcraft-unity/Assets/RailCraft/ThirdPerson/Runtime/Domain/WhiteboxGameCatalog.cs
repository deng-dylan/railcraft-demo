using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class WhiteboxGameCatalog
    {
        private readonly ReadOnlyCollection<PartDefinition> parts;
        private readonly ReadOnlyCollection<ModuleDefinition> modules;
        private readonly ReadOnlyCollection<QuizQuestionDefinition> questions;
        private readonly Dictionary<PartId, PartDefinition> partsById;
        private readonly Dictionary<string, PartDefinition> partsByKey;
        private readonly Dictionary<ModuleId, ModuleDefinition> modulesById;
        private readonly Dictionary<string, ModuleDefinition> modulesByKey;
        private readonly Dictionary<string, QuizQuestionDefinition> questionsById;

        public WhiteboxGameCatalog(
            IEnumerable<PartDefinition> parts,
            IEnumerable<ModuleDefinition> modules,
            IEnumerable<QuizQuestionDefinition> questions)
        {
            if (parts == null)
                throw new ArgumentNullException(nameof(parts));
            if (modules == null)
                throw new ArgumentNullException(nameof(modules));
            if (questions == null)
                throw new ArgumentNullException(nameof(questions));

            var copiedParts = new List<PartDefinition>(parts);
            var copiedModules = new List<ModuleDefinition>(modules);
            var copiedQuestions = new List<QuizQuestionDefinition>(questions);

            partsById = IndexPartsById(copiedParts);
            partsByKey = IndexPartsByKey(copiedParts);
            modulesById = IndexModulesById(copiedModules);
            modulesByKey = IndexModulesByKey(copiedModules);
            questionsById = IndexQuestionsById(copiedQuestions);

            ValidateCompleteIdentifierSets();
            ValidateRecipes(copiedModules);
            ValidateQuestionRewards(copiedQuestions);

            this.parts = copiedParts.AsReadOnly();
            this.modules = copiedModules.AsReadOnly();
            this.questions = copiedQuestions.AsReadOnly();
        }

        public IReadOnlyList<PartDefinition> Parts => parts;
        public IReadOnlyList<ModuleDefinition> Modules => modules;
        public IReadOnlyList<QuizQuestionDefinition> Questions => questions;

        public static WhiteboxGameCatalog CreateDefault()
        {
            var parts = new[]
            {
                new PartDefinition(PartId.Axle, "part_axle", "车轴"),
                new PartDefinition(PartId.Wheel, "part_wheel", "车轮"),
                new PartDefinition(PartId.Bearing, "part_bearing", "轴承"),
                new PartDefinition(PartId.BrakeDevice, "part_brake_device", "制动装置"),
                new PartDefinition(PartId.TractionRod, "part_traction_rod", "牵引拉杆"),
                new PartDefinition(PartId.SensorBracket, "part_sensor_bracket", "传感器座"),
                new PartDefinition(PartId.PrimaryElasticElement, "part_primary_elastic_element", "一系弹性元件"),
                new PartDefinition(PartId.PrimaryPositioningElement, "part_primary_positioning_element", "定位元件"),
                new PartDefinition(PartId.PrimaryDamper, "part_primary_damper", "一系减振元件"),
                new PartDefinition(PartId.SecondaryElasticElement, "part_secondary_elastic_element", "二系弹性元件"),
                new PartDefinition(PartId.HeightControlElement, "part_height_control_element", "高度控制元件"),
                new PartDefinition(PartId.SecondaryDamper, "part_secondary_damper", "二系减振元件"),
                new PartDefinition(PartId.Carbody, "part_carbody", "车体"),
                new PartDefinition(PartId.CentralTractionDevice, "part_central_traction_device", "中央牵引装置")
            };

            var modules = new[]
            {
                new ModuleDefinition(
                    ModuleId.WheelsetAxlebox,
                    "module_wheelset_axlebox",
                    "轮对轴箱",
                    new[] { PartId.Axle, PartId.Wheel, PartId.Bearing }),
                new ModuleDefinition(
                    ModuleId.Frame,
                    "module_frame",
                    "构架",
                    new[] { PartId.BrakeDevice, PartId.TractionRod, PartId.SensorBracket }),
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

            return new WhiteboxGameCatalog(parts, modules, WhiteboxQuestionBank.Create());
        }

        public bool TryGetPart(PartId id, out PartDefinition definition)
        {
            return partsById.TryGetValue(id, out definition);
        }

        public bool TryGetPart(string key, out PartDefinition definition)
        {
            if (key == null)
            {
                definition = null;
                return false;
            }

            return partsByKey.TryGetValue(key, out definition);
        }

        public PartDefinition GetPart(PartId id)
        {
            if (!TryGetPart(id, out var definition))
                throw new KeyNotFoundException($"Unknown part id: {id}.");
            return definition;
        }

        public bool TryGetModule(ModuleId id, out ModuleDefinition definition)
        {
            return modulesById.TryGetValue(id, out definition);
        }

        public bool TryGetModule(string key, out ModuleDefinition definition)
        {
            if (key == null)
            {
                definition = null;
                return false;
            }

            return modulesByKey.TryGetValue(key, out definition);
        }

        public ModuleDefinition GetModule(ModuleId id)
        {
            if (!TryGetModule(id, out var definition))
                throw new KeyNotFoundException($"Unknown module id: {id}.");
            return definition;
        }

        public bool TryGetQuestion(string id, out QuizQuestionDefinition definition)
        {
            if (id == null)
            {
                definition = null;
                return false;
            }

            return questionsById.TryGetValue(id, out definition);
        }

        public QuizQuestionDefinition GetQuestion(string id)
        {
            if (!TryGetQuestion(id, out var definition))
                throw new KeyNotFoundException($"Unknown question id: {id}.");
            return definition;
        }

        private static Dictionary<PartId, PartDefinition> IndexPartsById(
            IEnumerable<PartDefinition> definitions)
        {
            var lookup = new Dictionary<PartId, PartDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Part definitions cannot contain null entries.", nameof(definitions));
                if (lookup.ContainsKey(definition.Id))
                    throw new ArgumentException($"Duplicate part id: {definition.Id}.", nameof(definitions));
                lookup.Add(definition.Id, definition);
            }
            return lookup;
        }

        private static Dictionary<string, PartDefinition> IndexPartsByKey(
            IEnumerable<PartDefinition> definitions)
        {
            var lookup = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (lookup.ContainsKey(definition.Key))
                    throw new ArgumentException($"Duplicate part key: {definition.Key}.", nameof(definitions));
                lookup.Add(definition.Key, definition);
            }
            return lookup;
        }

        private static Dictionary<ModuleId, ModuleDefinition> IndexModulesById(
            IEnumerable<ModuleDefinition> definitions)
        {
            var lookup = new Dictionary<ModuleId, ModuleDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Module definitions cannot contain null entries.", nameof(definitions));
                if (lookup.ContainsKey(definition.Id))
                    throw new ArgumentException($"Duplicate module id: {definition.Id}.", nameof(definitions));
                lookup.Add(definition.Id, definition);
            }
            return lookup;
        }

        private static Dictionary<string, ModuleDefinition> IndexModulesByKey(
            IEnumerable<ModuleDefinition> definitions)
        {
            var lookup = new Dictionary<string, ModuleDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (lookup.ContainsKey(definition.Key))
                    throw new ArgumentException($"Duplicate module key: {definition.Key}.", nameof(definitions));
                lookup.Add(definition.Key, definition);
            }
            return lookup;
        }

        private static Dictionary<string, QuizQuestionDefinition> IndexQuestionsById(
            IEnumerable<QuizQuestionDefinition> definitions)
        {
            var lookup = new Dictionary<string, QuizQuestionDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("Question definitions cannot contain null entries.", nameof(definitions));
                if (lookup.ContainsKey(definition.Id))
                    throw new ArgumentException($"Duplicate question id: {definition.Id}.", nameof(definitions));
                lookup.Add(definition.Id, definition);
            }
            return lookup;
        }

        private void ValidateCompleteIdentifierSets()
        {
            var allPartIds = (PartId[])Enum.GetValues(typeof(PartId));
            if (partsById.Count != allPartIds.Length)
                throw new ArgumentException($"The catalog must define all {allPartIds.Length} parts.");
            foreach (var partId in allPartIds)
            {
                if (!partsById.ContainsKey(partId))
                    throw new ArgumentException($"The catalog is missing part id: {partId}.");
            }

            var allModuleIds = (ModuleId[])Enum.GetValues(typeof(ModuleId));
            if (modulesById.Count != allModuleIds.Length)
                throw new ArgumentException($"The catalog must define all {allModuleIds.Length} modules.");
            foreach (var moduleId in allModuleIds)
            {
                if (!modulesById.ContainsKey(moduleId))
                    throw new ArgumentException($"The catalog is missing module id: {moduleId}.");
            }
        }

        private void ValidateRecipes(IEnumerable<ModuleDefinition> definitions)
        {
            var assignedParts = new HashSet<PartId>();
            var parentCount = new Dictionary<ModuleId, int>();
            foreach (var moduleId in modulesById.Keys)
                parentCount.Add(moduleId, 0);

            foreach (var definition in definitions)
            {
                foreach (var partId in definition.RequiredParts)
                {
                    if (!partsById.ContainsKey(partId))
                        throw new ArgumentException($"Module {definition.Id} uses unknown part {partId}.");
                    if (!assignedParts.Add(partId))
                        throw new ArgumentException($"Part {partId} occurs in more than one module recipe.");
                }

                foreach (var childModuleId in definition.RequiredModules)
                {
                    if (!modulesById.ContainsKey(childModuleId))
                        throw new ArgumentException($"Module {definition.Id} uses unknown child module {childModuleId}.");
                    parentCount[childModuleId]++;
                }
            }

            if (assignedParts.Count != partsById.Count)
                throw new ArgumentException("Every part must occur in exactly one module recipe.");

            foreach (var pair in parentCount)
            {
                var expectedParentCount = pair.Key == ModuleId.Landing ? 0 : 1;
                if (pair.Value != expectedParentCount)
                    throw new ArgumentException(
                        $"Module {pair.Key} must have exactly {expectedParentCount} parent assembly entries.");
            }

            ValidateAcyclicAssemblyGraph();
        }

        private void ValidateAcyclicAssemblyGraph()
        {
            var visiting = new HashSet<ModuleId>();
            var visited = new HashSet<ModuleId>();
            foreach (var moduleId in modulesById.Keys)
                Visit(moduleId, visiting, visited);
        }

        private void Visit(
            ModuleId moduleId,
            HashSet<ModuleId> visiting,
            HashSet<ModuleId> visited)
        {
            if (visited.Contains(moduleId))
                return;
            if (!visiting.Add(moduleId))
                throw new ArgumentException($"Assembly recipes contain a cycle at {moduleId}.");

            foreach (var childModuleId in modulesById[moduleId].RequiredModules)
                Visit(childModuleId, visiting, visited);

            visiting.Remove(moduleId);
            visited.Add(moduleId);
        }

        private void ValidateQuestionRewards(IEnumerable<QuizQuestionDefinition> definitions)
        {
            var rewardedParts = new HashSet<PartId>();
            foreach (var definition in definitions)
            {
                if (!partsById.ContainsKey(definition.RewardPart))
                    throw new ArgumentException($"Question {definition.Id} rewards an unknown part.");
                rewardedParts.Add(definition.RewardPart);
            }

            if (rewardedParts.Count != partsById.Count)
                throw new ArgumentException("Every part must be unlockable by at least one question.");
        }
    }
}
