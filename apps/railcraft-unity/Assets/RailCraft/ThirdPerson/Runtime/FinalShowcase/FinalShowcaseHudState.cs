namespace RailCraft.ThirdPerson.FinalShowcase
{
    /// <summary>
    /// Immutable snapshot intended for an optional HUD presenter.
    /// </summary>
    public readonly struct FinalShowcaseHudState
    {
        public FinalShowcaseHudState(
            FinalShowcaseCameraPreset cameraPreset,
            int selectedCarIndex,
            int carCount,
            int explodableCarCount,
            bool explodedTarget,
            float explodeProgress,
            bool ready,
            string bindingMessage)
        {
            CameraPreset = cameraPreset;
            SelectedCarIndex = selectedCarIndex;
            CarCount = carCount;
            ExplodableCarCount = explodableCarCount;
            ExplodedTarget = explodedTarget;
            ExplodeProgress = explodeProgress;
            Ready = ready;
            BindingMessage = bindingMessage ?? string.Empty;
        }

        public FinalShowcaseCameraPreset CameraPreset { get; }
        public int SelectedCarIndex { get; }
        public int SelectedCarNumber => SelectedCarIndex < 0 ? 0 : SelectedCarIndex + 1;
        public int CarCount { get; }
        public int ExplodableCarCount { get; }
        public bool ExplodedTarget { get; }
        public float ExplodeProgress { get; }
        public bool Ready { get; }
        public string BindingMessage { get; }

        public string CameraLabel => GetCameraLabel(CameraPreset);

        public string SelectedCarLabel =>
            SelectedCarIndex < 0 || CarCount == 0
                ? "未发现车厢"
                : $"第 {SelectedCarNumber:00}/{CarCount:00} 节";

        public string MotionLabel
        {
            get
            {
                if (ExplodeProgress > 0.0001f && ExplodeProgress < 0.9999f)
                    return ExplodedTarget ? "分解中" : "复位中";
                return ExplodedTarget ? "分解视图" : "整列视图";
            }
        }

        public string StatusText => $"{CameraLabel} · {SelectedCarLabel} · {MotionLabel}";

        public static string ShortcutHelp =>
            "F1-F4 切换机位  |  1-8 选择车厢  |  ←/→ 逐节选择  |  Tab 下一个机位  |  X 分解/复位  |  R 重置";

        public static string GetCameraLabel(FinalShowcaseCameraPreset preset)
        {
            switch (preset)
            {
                case FinalShowcaseCameraPreset.Head:
                    return "车头机位";
                case FinalShowcaseCameraPreset.Side:
                    return "侧面机位";
                case FinalShowcaseCameraPreset.Departure:
                    return "出发机位";
                default:
                    return "全景机位";
            }
        }
    }
}

