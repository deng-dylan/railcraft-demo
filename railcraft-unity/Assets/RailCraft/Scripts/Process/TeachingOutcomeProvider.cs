namespace RailCraft.Process
{
    public static class TeachingOutcomeProvider
    {
        public const string TeachingAnomalyMessage =
            "教学占位异常：检测到传感器信号不一致。该内容用于演示“调试—整改—检验—再调试”闭环，不代表 SWM-400E1 的真实故障结论。";

        public const string EnterReworkMessage = "进入整改";
        public const string InspectionCompleteMessage = "整改检验完成，返回调试";
        public const string SecondCommissioningPassedMessage = "再次调试通过，进入投入使用准备。";
    }
}
