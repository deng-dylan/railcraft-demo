using System;
using RailCraft.ThirdPerson.Domain;

namespace RailCraft.ThirdPerson.World
{
    public static class WhiteboxDisplayNames
    {
        public static string Part(PartId partId)
        {
            return Humanize(partId.ToString());
        }

        public static string Module(ModuleId moduleId)
        {
            return Humanize(moduleId.ToString());
        }

        public static string Commissioning(CommissioningPhase phase)
        {
            switch (phase)
            {
                case CommissioningPhase.Locked: return "等待落车";
                case CommissioningPhase.ReadyForInitialTest: return "等待首次调试";
                case CommissioningPhase.NeedsRetuning: return "需要重新调试";
                case CommissioningPhase.ReadyForInspection: return "等待检验";
                case CommissioningPhase.ReadyForRetest: return "等待复测";
                case CommissioningPhase.InService: return "投入使用";
                default: return phase.ToString();
            }
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            switch (value)
            {
                case "Axle": return "车轴";
                case "Wheel": return "车轮";
                case "Bearing": return "轴承";
                case "BrakeDevice": return "制动装置";
                case "TractionRod": return "牵引拉杆";
                case "SensorBracket": return "传感器座";
                case "PrimaryElasticElement": return "一系弹性元件";
                case "PrimaryPositioningElement": return "一系定位元件";
                case "PrimaryDamper": return "一系减振元件";
                case "SecondaryElasticElement": return "二系弹性元件";
                case "HeightControlElement": return "高度控制元件";
                case "SecondaryDamper": return "二系减振元件";
                case "Carbody": return "车体";
                case "CentralTractionDevice": return "中央牵引装置";
                case "WheelsetAxlebox": return "轮对轴箱";
                case "Frame": return "构架";
                case "PrimarySuspension": return "一系悬挂装置";
                case "BogieStructure": return "转向架构体";
                case "SecondarySuspension": return "二系悬挂装置";
                case "Landing": return "落车";
            }

            return value.Replace("_", " ");
        }
    }
}
