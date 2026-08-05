using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RailCraft.ThirdPerson.Domain
{
    public sealed class EngineeringKnowledgeCatalog
    {
        private readonly ReadOnlyCollection<KnowledgeEntry> entries;
        private readonly Dictionary<string, KnowledgeEntry> entriesById;
        private readonly Dictionary<string, ReadOnlyCollection<KnowledgeEntry>> answerEntries;
        private readonly Dictionary<PartId, ReadOnlyCollection<KnowledgeEntry>> entriesByPart;
        private readonly Dictionary<ModuleId, ReadOnlyCollection<KnowledgeEntry>> entriesByModule;
        private readonly Dictionary<CommissioningPhase, ReadOnlyCollection<KnowledgeEntry>>
            entriesByCommissioningPhase;
        private readonly Dictionary<KnowledgeEntryCategory, ReadOnlyCollection<KnowledgeEntry>>
            entriesByCategory;
        private readonly ReadOnlyCollection<KnowledgeEntry> vehicleCompletionEntries;

        public EngineeringKnowledgeCatalog(IEnumerable<KnowledgeEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var copiedEntries = new List<KnowledgeEntry>(entries);
            if (copiedEntries.Contains(null))
                throw new ArgumentException("Knowledge entries cannot contain null values.", nameof(entries));
            copiedEntries.Sort((left, right) => left.UnlockOrder.CompareTo(right.UnlockOrder));
            if (copiedEntries.Count == 0)
                throw new ArgumentException("A knowledge catalog needs at least one entry.", nameof(entries));

            entriesById = new Dictionary<string, KnowledgeEntry>(StringComparer.Ordinal);
            var unlockOrders = new HashSet<int>();
            foreach (var entry in copiedEntries)
            {
                if (entriesById.ContainsKey(entry.Id))
                    throw new ArgumentException($"Duplicate knowledge entry id: {entry.Id}.", nameof(entries));
                if (!unlockOrders.Add(entry.UnlockOrder))
                {
                    throw new ArgumentException(
                        $"Duplicate knowledge unlock order: {entry.UnlockOrder}.",
                        nameof(entries));
                }

                entriesById.Add(entry.Id, entry);
            }

            this.entries = copiedEntries.AsReadOnly();
            entriesByPart = GroupByPart(copiedEntries);
            entriesByModule = GroupByModule(copiedEntries);
            entriesByCommissioningPhase = GroupByCommissioningPhase(copiedEntries);
            entriesByCategory = GroupByCategory(copiedEntries);
            answerEntries = BuildAnswerEntries(copiedEntries, entriesByPart);
            vehicleCompletionEntries = SelectByUnlockKind(
                copiedEntries,
                KnowledgeUnlockKind.VehicleCompleted);
        }

        public IReadOnlyList<KnowledgeEntry> Entries => entries;

        public static EngineeringKnowledgeCatalog CreateDefault()
        {
            return CreateDefault(WhiteboxGameCatalog.CreateDefault());
        }

        public static EngineeringKnowledgeCatalog CreateDefault(WhiteboxGameCatalog gameCatalog)
        {
            if (gameCatalog == null)
                throw new ArgumentNullException(nameof(gameCatalog));

            var definitions = new List<KnowledgeEntry>();
            AddPartEntries(definitions, gameCatalog);
            AddQuestionEntries(definitions, gameCatalog);
            AddModuleEntries(definitions, gameCatalog);
            AddCommissioningEntries(definitions);
            AddVehicleOverview(definitions);
            return new EngineeringKnowledgeCatalog(definitions);
        }

        public bool TryGetEntry(string id, out KnowledgeEntry entry)
        {
            if (id == null)
            {
                entry = null;
                return false;
            }

            return entriesById.TryGetValue(id, out entry);
        }

        public KnowledgeEntry GetEntry(string id)
        {
            if (!TryGetEntry(id, out var entry))
                throw new KeyNotFoundException($"Unknown knowledge entry id: {id}.");
            return entry;
        }

        public IReadOnlyList<KnowledgeEntry> GetEntriesForPart(PartId partId)
        {
            return entriesByPart.TryGetValue(partId, out var matches)
                ? matches
                : Array.Empty<KnowledgeEntry>();
        }

        public IReadOnlyList<KnowledgeEntry> GetEntriesForModule(ModuleId moduleId)
        {
            return entriesByModule.TryGetValue(moduleId, out var matches)
                ? matches
                : Array.Empty<KnowledgeEntry>();
        }

        public IReadOnlyList<KnowledgeEntry> GetEntriesForCategory(
            KnowledgeEntryCategory category)
        {
            return entriesByCategory.TryGetValue(category, out var matches)
                ? matches
                : Array.Empty<KnowledgeEntry>();
        }

        internal IReadOnlyList<KnowledgeEntry> GetEntriesForCorrectAnswer(string questionId)
        {
            if (questionId == null)
                return Array.Empty<KnowledgeEntry>();
            return answerEntries.TryGetValue(questionId, out var matches)
                ? matches
                : Array.Empty<KnowledgeEntry>();
        }

        internal IReadOnlyList<KnowledgeEntry> GetEntriesForCompletedModule(ModuleId moduleId)
        {
            return GetEntriesForModule(moduleId);
        }

        internal IReadOnlyList<KnowledgeEntry> GetEntriesForCommissioningPhase(
            CommissioningPhase phase)
        {
            return entriesByCommissioningPhase.TryGetValue(phase, out var matches)
                ? matches
                : Array.Empty<KnowledgeEntry>();
        }

        internal IReadOnlyList<KnowledgeEntry> GetVehicleCompletionEntries()
        {
            return vehicleCompletionEntries;
        }

        private static void AddPartEntries(
            ICollection<KnowledgeEntry> definitions,
            WhiteboxGameCatalog gameCatalog)
        {
            for (var index = 0; index < gameCatalog.Parts.Count; index++)
            {
                var part = gameCatalog.Parts[index];
                definitions.Add(new KnowledgeEntry(
                    "knowledge_" + part.Key,
                    part.DisplayName,
                    GetPartBody(part.Id),
                    KnowledgeEntryCategory.Part,
                    100 + index,
                    KnowledgeUnlockKind.PartKnowledgeFromCorrectAnswer,
                    relatedPart: part.Id));
            }
        }

        private static void AddQuestionEntries(
            ICollection<KnowledgeEntry> definitions,
            WhiteboxGameCatalog gameCatalog)
        {
            for (var index = 0; index < gameCatalog.Questions.Count; index++)
            {
                var question = gameCatalog.Questions[index];
                definitions.Add(new KnowledgeEntry(
                    "knowledge_question_" + question.Id,
                    question.Prompt,
                    question.Explanation,
                    KnowledgeEntryCategory.QuestionExplanation,
                    1000 + index,
                    KnowledgeUnlockKind.CorrectAnswer,
                    question.Id,
                    question.RewardPart));
            }
        }

        private static void AddModuleEntries(
            ICollection<KnowledgeEntry> definitions,
            WhiteboxGameCatalog gameCatalog)
        {
            for (var index = 0; index < gameCatalog.Modules.Count; index++)
            {
                var module = gameCatalog.Modules[index];
                definitions.Add(new KnowledgeEntry(
                    "knowledge_" + module.Key,
                    module.DisplayName + "装配",
                    BuildModuleBody(module, gameCatalog),
                    KnowledgeEntryCategory.Assembly,
                    2000 + index,
                    KnowledgeUnlockKind.ModuleCompleted,
                    relatedModule: module.Id));
            }
        }

        private static void AddCommissioningEntries(ICollection<KnowledgeEntry> definitions)
        {
            definitions.Add(new KnowledgeEntry(
                "knowledge_commissioning_initial_failure",
                "初次调试与问题闭环",
                "落车完成后先进行初次调试。白盒固定进入教学失败分支，用于说明调试发现问题后必须进入整改流程；该结果不代表真实车型故障结论。",
                KnowledgeEntryCategory.Commissioning,
                3000,
                KnowledgeUnlockKind.CommissioningPhaseReached,
                relatedCommissioningPhase: CommissioningPhase.NeedsRetuning));
            definitions.Add(new KnowledgeEntry(
                "knowledge_commissioning_retuning",
                "重新调试",
                "重新调试用于根据初次调试反馈完成白盒化整改，并把流程推进到检验准备状态。正式作业参数必须来自经批准的工艺文件。",
                KnowledgeEntryCategory.Commissioning,
                3001,
                KnowledgeUnlockKind.CommissioningPhaseReached,
                relatedCommissioningPhase: CommissioningPhase.ReadyForInspection));
            definitions.Add(new KnowledgeEntry(
                "knowledge_commissioning_inspection",
                "整改后检验",
                "检验用于确认整改项已经完成并具备复测条件。白盒只验证流程门禁，不提供真实尺寸、限值或判废标准。",
                KnowledgeEntryCategory.Commissioning,
                3002,
                KnowledgeUnlockKind.CommissioningPhaseReached,
                relatedCommissioningPhase: CommissioningPhase.ReadyForRetest));
            definitions.Add(new KnowledgeEntry(
                "knowledge_commissioning_in_service",
                "复测通过与投入使用",
                "复测通过后车辆进入投入使用状态，表示答题、零件获取、分级装配、落车和调试检验闭环已经完整完成。",
                KnowledgeEntryCategory.Commissioning,
                3003,
                KnowledgeUnlockKind.CommissioningPhaseReached,
                relatedCommissioningPhase: CommissioningPhase.InService));
        }

        private static void AddVehicleOverview(ICollection<KnowledgeEntry> definitions)
        {
            definitions.Add(new KnowledgeEntry(
                "knowledge_vehicle_complete",
                "整车装配知识总览",
                "整车流程由零件知识问答、轮对轴箱与构架等基础装配、转向架构体组合、二系悬挂与中央牵引连接、落车以及调试检验闭环构成。完成整车后开放图鉴总入口，可回看本次已解锁的工程知识。",
                KnowledgeEntryCategory.VehicleOverview,
                4000,
                KnowledgeUnlockKind.VehicleCompleted));
        }

        private static string BuildModuleBody(
            ModuleDefinition module,
            WhiteboxGameCatalog gameCatalog)
        {
            var inputNames = new List<string>();
            foreach (var partId in module.RequiredParts)
                inputNames.Add(gameCatalog.GetPart(partId).DisplayName);
            foreach (var moduleId in module.RequiredModules)
                inputNames.Add(gameCatalog.GetModule(moduleId).DisplayName);

            return GetModuleSummary(module.Id) +
                " 白盒装配输入：" +
                string.Join("、", inputNames) +
                "。";
        }

        private static string GetPartBody(PartId partId)
        {
            switch (partId)
            {
                case PartId.Axle:
                    return "车轴与车轮、轴承共同形成轮对轴箱的走行基础，承担并传递轮轨载荷。白盒不表达过盈配合、材料和尺寸参数。";
                case PartId.Wheel:
                    return "车轮直接与钢轨接触，与车轴构成轮对，并通过轮轨黏着传递牵引力和制动力。";
                case PartId.Bearing:
                    return "轴承支承车轴旋转，并通过轴箱连接关系把轮对载荷传向构架；安装状态会影响走行可靠性。";
                case PartId.BrakeDevice:
                    return "基础制动装置通过摩擦把列车动能转化为热能，实现减速和停车，是构架侧的重要安装子系统。";
                case PartId.TractionRod:
                    return "牵引拉杆用于在连接层级传递纵向牵引力和制动力，同时需要允许悬挂系统规定的相对运动。";
                case PartId.SensorBracket:
                    return "传感器座为监测元件提供稳定的安装基准，使测点位置和方向能够在检修后保持一致。";
                case PartId.PrimaryElasticElement:
                    return "一系弹性元件位于轮对轴箱与构架之间，用于弹性支承并缓和轨道不平顺带来的冲击。";
                case PartId.PrimaryPositioningElement:
                    return "一系定位元件约束轮对轴箱相对构架的运动，并提供适宜的纵向与横向定位刚度。";
                case PartId.PrimaryDamper:
                    return "一系减振元件耗散轮对轴箱与构架之间的振动能量，配合弹性和定位元件改善走行稳定性。";
                case PartId.SecondaryElasticElement:
                    return "二系弹性元件位于转向架与车体之间，承担车体支承并隔离振动；高速车辆常采用空气弹簧方案。";
                case PartId.HeightControlElement:
                    return "高度控制元件根据载荷变化调节二系空气弹簧状态，使车体尽量保持设计高度。";
                case PartId.SecondaryDamper:
                    return "二系减振元件衰减车体与转向架之间的相对振动，服务于乘坐舒适性和运行稳定性。";
                case PartId.Carbody:
                    return "车体是落车阶段的上部结构，需要通过二系悬挂和中央牵引连接与转向架形成完整车辆关系。";
                case PartId.CentralTractionDevice:
                    return "中央牵引装置连接车体与转向架并传递纵向力，同时适应两者在悬挂允许范围内的相对运动。";
                default:
                    throw new ArgumentOutOfRangeException(nameof(partId));
            }
        }

        private static string GetModuleSummary(ModuleId moduleId)
        {
            switch (moduleId)
            {
                case ModuleId.WheelsetAxlebox:
                    return "轮对轴箱把车轴、车轮和轴承组织为承载、滚动与载荷传递单元。";
                case ModuleId.Frame:
                    return "构架是转向架的装配骨架，为制动、牵引连接和监测安装提供基准。";
                case ModuleId.PrimarySuspension:
                    return "一系悬挂在轮对轴箱与构架之间完成弹性支承、定位和减振。";
                case ModuleId.BogieStructure:
                    return "转向架构体把走行单元、构架和一系悬挂组合成可承接上部连接的主体结构。";
                case ModuleId.SecondarySuspension:
                    return "二系悬挂在转向架与车体之间完成车体支承、高度调节和振动衰减。";
                case ModuleId.Landing:
                    return "落车把车体、中央牵引装置、转向架构体和二系悬挂组合为完整车辆关系。";
                default:
                    throw new ArgumentOutOfRangeException(nameof(moduleId));
            }
        }

        private static Dictionary<string, ReadOnlyCollection<KnowledgeEntry>> BuildAnswerEntries(
            IEnumerable<KnowledgeEntry> definitions,
            IReadOnlyDictionary<PartId, ReadOnlyCollection<KnowledgeEntry>> partLookup)
        {
            var result = new Dictionary<string, ReadOnlyCollection<KnowledgeEntry>>(
                StringComparer.Ordinal);
            foreach (var entry in definitions)
            {
                if (entry.UnlockKind != KnowledgeUnlockKind.CorrectAnswer)
                    continue;
                if (result.ContainsKey(entry.SourceQuestionId))
                {
                    throw new ArgumentException(
                        $"Duplicate source question knowledge: {entry.SourceQuestionId}.");
                }

                var unlocked = new List<KnowledgeEntry> { entry };
                if (entry.RelatedPart.HasValue &&
                    partLookup.TryGetValue(entry.RelatedPart.Value, out var relatedEntries))
                {
                    foreach (var relatedEntry in relatedEntries)
                    {
                        if (relatedEntry.UnlockKind ==
                            KnowledgeUnlockKind.PartKnowledgeFromCorrectAnswer)
                        {
                            unlocked.Add(relatedEntry);
                        }
                    }
                }

                unlocked.Sort((left, right) => left.UnlockOrder.CompareTo(right.UnlockOrder));
                result.Add(entry.SourceQuestionId, unlocked.AsReadOnly());
            }

            return result;
        }

        private static Dictionary<PartId, ReadOnlyCollection<KnowledgeEntry>> GroupByPart(
            IEnumerable<KnowledgeEntry> definitions)
        {
            var groups = new Dictionary<PartId, List<KnowledgeEntry>>();
            foreach (var entry in definitions)
            {
                if (!entry.RelatedPart.HasValue)
                    continue;
                if (!groups.TryGetValue(entry.RelatedPart.Value, out var group))
                {
                    group = new List<KnowledgeEntry>();
                    groups.Add(entry.RelatedPart.Value, group);
                }
                group.Add(entry);
            }
            return Freeze(groups);
        }

        private static Dictionary<ModuleId, ReadOnlyCollection<KnowledgeEntry>> GroupByModule(
            IEnumerable<KnowledgeEntry> definitions)
        {
            var groups = new Dictionary<ModuleId, List<KnowledgeEntry>>();
            foreach (var entry in definitions)
            {
                if (!entry.RelatedModule.HasValue)
                    continue;
                if (!groups.TryGetValue(entry.RelatedModule.Value, out var group))
                {
                    group = new List<KnowledgeEntry>();
                    groups.Add(entry.RelatedModule.Value, group);
                }
                group.Add(entry);
            }
            return Freeze(groups);
        }

        private static Dictionary<CommissioningPhase, ReadOnlyCollection<KnowledgeEntry>>
            GroupByCommissioningPhase(IEnumerable<KnowledgeEntry> definitions)
        {
            var groups = new Dictionary<CommissioningPhase, List<KnowledgeEntry>>();
            foreach (var entry in definitions)
            {
                if (!entry.RelatedCommissioningPhase.HasValue)
                    continue;
                var phase = entry.RelatedCommissioningPhase.Value;
                if (!groups.TryGetValue(phase, out var group))
                {
                    group = new List<KnowledgeEntry>();
                    groups.Add(phase, group);
                }
                group.Add(entry);
            }
            return Freeze(groups);
        }

        private static Dictionary<KnowledgeEntryCategory, ReadOnlyCollection<KnowledgeEntry>>
            GroupByCategory(IEnumerable<KnowledgeEntry> definitions)
        {
            var groups = new Dictionary<KnowledgeEntryCategory, List<KnowledgeEntry>>();
            foreach (var entry in definitions)
            {
                if (!groups.TryGetValue(entry.Category, out var group))
                {
                    group = new List<KnowledgeEntry>();
                    groups.Add(entry.Category, group);
                }
                group.Add(entry);
            }
            return Freeze(groups);
        }

        private static ReadOnlyCollection<KnowledgeEntry> SelectByUnlockKind(
            IEnumerable<KnowledgeEntry> definitions,
            KnowledgeUnlockKind unlockKind)
        {
            var matches = new List<KnowledgeEntry>();
            foreach (var entry in definitions)
            {
                if (entry.UnlockKind == unlockKind)
                    matches.Add(entry);
            }
            matches.Sort((left, right) => left.UnlockOrder.CompareTo(right.UnlockOrder));
            return matches.AsReadOnly();
        }

        private static Dictionary<TKey, ReadOnlyCollection<KnowledgeEntry>> Freeze<TKey>(
            IDictionary<TKey, List<KnowledgeEntry>> groups)
        {
            var result = new Dictionary<TKey, ReadOnlyCollection<KnowledgeEntry>>();
            foreach (var pair in groups)
            {
                pair.Value.Sort((left, right) => left.UnlockOrder.CompareTo(right.UnlockOrder));
                result.Add(pair.Key, pair.Value.AsReadOnly());
            }
            return result;
        }
    }
}
