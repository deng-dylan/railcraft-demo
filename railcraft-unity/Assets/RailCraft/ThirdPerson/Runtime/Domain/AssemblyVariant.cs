using System;
using System.Collections.Generic;

namespace RailCraft.ThirdPerson.Domain
{
    /// <summary>
    /// A playable assembly plan. The domain flow stays the same for each plan;
    /// the selected plan controls the vehicle identity and its visual asset.
    /// </summary>
    public enum AssemblyVariantId
    {
        FuxingDemo,
        MetroSimplified,
        Y25Freight,
        TeachingConcept
    }

    public sealed class AssemblyVariantDefinition
    {
        internal AssemblyVariantDefinition(
            AssemblyVariantId id,
            string key,
            string displayName,
            string shortName,
            string description,
            string assetStatus,
            bool teachingOnly,
            bool isPrimaryTrainingPlan)
        {
            Id = id;
            Key = key;
            DisplayName = displayName;
            ShortName = shortName;
            Description = description;
            AssetStatus = assetStatus;
            TeachingOnly = teachingOnly;
            IsPrimaryTrainingPlan = isPrimaryTrainingPlan;
        }

        public AssemblyVariantId Id { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public string ShortName { get; }
        public string Description { get; }
        public string AssetStatus { get; }
        public bool TeachingOnly { get; }
        public bool IsPrimaryTrainingPlan { get; }

        public string MenuLabel => IsPrimaryTrainingPlan
            ? $"{DisplayName} · {ShortName}"
            : $"扩展示范 · {DisplayName}";
    }

    /// <summary>
    /// Central catalogue used by the menu, save data and runtime presentation.
    /// Keep this catalogue independent from Unity so it can be tested in the
    /// domain assembly and used by future imported-model tooling.
    /// </summary>
    public static class AssemblyVariantCatalog
    {
        private static readonly AssemblyVariantDefinition[] definitions =
        {
            new AssemblyVariantDefinition(
                AssemblyVariantId.FuxingDemo,
                "fuxing-demo",
                "复兴号教学装配",
                "标准实训线",
                "沿用复兴号车体，子总成使用队员结构示范件，完成态使用 CW-200K 客车转向架参考模型，完整跑通知识确认、拾取、装配、落车和调试流程。",
                "CW-200K 完成态参考网格已接入",
                false,
                true),
            new AssemblyVariantDefinition(
                AssemblyVariantId.MetroSimplified,
                "metro-simplified",
                "地铁简化转向架",
                "扩展示范件",
                "作为组员模型接入验证使用；等待完整 Pack and Go 或导出的 FBX/GLB 后再评估纳入独立关卡。",
                "上色网格插槽／示范件回退",
                false,
                false),
            new AssemblyVariantDefinition(
                AssemblyVariantId.Y25Freight,
                "y25-freight",
                "Y25 欧洲货运转向架",
                "扩展示范件",
                "真实 STEP 网格已导入 Unity 审模展台；完成独立工艺与题目审校后可升级为扩展关卡。",
                "真实 STEP 网格已接入",
                false,
                false),
            new AssemblyVariantDefinition(
                AssemblyVariantId.TeachingConcept,
                "teaching-concept",
                "简化铁路转向架（现实无对应）",
                "教学概念件",
                "真实教学概念网格已进入 Unity 审模展台；它仍标注为“现实无对应”，用于装配逻辑说明和材质流程验证，不作为标准实训考核对象。",
                "SolidWorks AP214 网格已接入",
                true,
                false)
        };

        public static IReadOnlyList<AssemblyVariantDefinition> Definitions => definitions;

        public static AssemblyVariantDefinition Get(AssemblyVariantId id)
        {
            for (var index = 0; index < definitions.Length; index++)
            {
                if (definitions[index].Id == id)
                    return definitions[index];
            }

            return definitions[0];
        }

        public static bool TryParse(string key, out AssemblyVariantId id)
        {
            for (var index = 0; index < definitions.Length; index++)
            {
                if (string.Equals(definitions[index].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    id = definitions[index].Id;
                    return true;
                }
            }

            id = AssemblyVariantId.FuxingDemo;
            return false;
        }

        public static AssemblyVariantId Clamp(AssemblyVariantId id)
        {
            return Get(id).Id;
        }
    }
}
