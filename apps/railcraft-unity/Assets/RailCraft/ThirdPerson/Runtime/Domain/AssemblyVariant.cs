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
            bool teachingOnly)
        {
            Id = id;
            Key = key;
            DisplayName = displayName;
            ShortName = shortName;
            Description = description;
            AssetStatus = assetStatus;
            TeachingOnly = teachingOnly;
        }

        public AssemblyVariantId Id { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public string ShortName { get; }
        public string Description { get; }
        public string AssetStatus { get; }
        public bool TeachingOnly { get; }

        public string MenuLabel => $"{DisplayName} · {ShortName}";
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
                "当前默认方案",
                "沿用现有复兴号车体与队员转向架示范 FBX，完整跑通答题、拾取、装配、落车和调试流程。",
                "Unity 网格已接入",
                false),
            new AssemblyVariantDefinition(
                AssemblyVariantId.MetroSimplified,
                "metro-simplified",
                "地铁简化转向架",
                "组员上色装配体",
                "使用地铁简化转向架的结构方案；等待组员提供完整 Pack and Go 或导出的 FBX/GLB。",
                "上色网格插槽／示范件回退",
                false),
            new AssemblyVariantDefinition(
                AssemblyVariantId.Y25Freight,
                "y25-freight",
                "Y25 欧洲货运转向架",
                "货运方案",
                "使用 Y25 货运转向架的结构方案；STEP 自包含，适合作为下一条导入管线。",
                "STEP 网格插槽／示范件回退",
                false),
            new AssemblyVariantDefinition(
                AssemblyVariantId.TeachingConcept,
                "teaching-concept",
                "简化铁路转向架（现实无对应）",
                "教学概念方案",
                "组员标注为教学版、现实无对应车型；只作为玩法教学分支，完成后仍进入统一落车与调试流程。",
                "教学材质插槽／示范件回退",
                true)
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
