# RailCraft Unity

> **当前开发主线：第三人称流程白盒。** Unity 固定视角 v0.1 已冻结为验收
> 基线；Godot `v0.1.0-demo` 已冻结为历史 Demo。三者的版本关系和文档入口见
> [`Documentation/README.md`](Documentation/README.md)。

## 当前开发主线：第三人称流程白盒

当前主线验证工厂第三人称探索、答题、拾取、库存、分级装配、落车、调试回路和
投入使用的完整闭环。内容规模为 58 道题、14 个答题与零件拾取工位、14 个零件、
6 个装配节点，以及调试失败、重新调试、检验和复测流程。题目答错可重试，所有
奖励、拾取、安装和调试动作均有重复交互保护。

当前 v0.2 增加主菜单、继续游戏、音效与画质设置、23 步装配进度、流程状态机、
自动存档、成绩结算、一键重玩和工程知识图鉴。完成节点会显示知识提示，可交互
工位带有高亮与成功/失败颜色反馈，关键总成完成时镜头自动聚焦后回到玩家视角。
装配计时会在主菜单、应用暂停和退出期间停表，继续游戏后从原有效用时接着累计。

- 主线场景：`Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`
- 重新生成：`RailCraft > Third Person Whitebox > Rebuild Scene`
- Windows 构建：`RailCraft > Third Person Whitebox > Build Windows x86_64`（构建前自动重建生成场景）
- 本地入口：`Builds/Whitebox/RailCraftWhitebox.exe`
- 当前规格：[`Documentation/ThirdPersonWhitebox.md`](Documentation/ThirdPersonWhitebox.md)
- 当前证据：[`Artifacts/Whitebox/Acceptance`](Artifacts/Whitebox/Acceptance)

从源码直接进入编辑器 Play 前先执行一次 `Rebuild Scene`，让生成器把 v0.2 组件和
UI 写入场景；Windows 白盒构建入口会自动完成这一步。证据目录以其中验收报告标注
的批次为准，只有重新完成 Unity 测试、构建与成品冒烟后才能作为 v0.2 最终证据。

白盒使用 Unity `6000.3.21f1`、URP、Input System 和 uGUI。玩家使用 `WASD`
移动、`Shift` 奔跑、鼠标控制镜头、滚轮缩放并按 `E` 交互。场景中的方块、圆柱、
胶囊和程序化标识是可替换视觉资产，后续 Blender prefab 继续遵守
[`Documentation/ModelHandoff.md`](Documentation/ModelHandoff.md)。

项目默认 `EditorBuildSettings` 只启用 `ThirdPersonWhitebox`。白盒构建入口只打包
这一张主线场景；冻结 v0.1 的历史构建脚本显式打包 `Bootstrap` 与 `Factory`，不读取
当前默认场景。每批主线改动通过对应测试、Windows 构建和成品冒烟后单独
提交并及时推送当前功能分支；该批次产生的日志、测试 XML 和最终截图同步更新到
`Artifacts/Whitebox/Acceptance/`。

## 冻结验收基线：Unity 固定视角 v0.1

> v0.1 Windows x86_64 测试版已完成最终验收：EditMode 50/50 通过、PlayMode 50 通过且 1 个硬件性能门禁按设计跳过，Windows 构建 0 warning / 0 error。可复核的构建日志、测试 XML、性能原始数据、12 张 GUI 截图、环境记录和 SHA-256 位于 [`Artifacts/Acceptance`](Artifacts/Acceptance/acceptance-report.md)。

RailCraft Unity v0.1 是 Windows x86_64 第一版固定视角测试版。体验包含固定 48 道知识准备题，以及 SWM-400E1 动力转向架子系统装配、动力中间车落车、初次调试教学异常、整改检验、二次调试和投入使用的 15 步引导流程。该版本已冻结，仅保留回归、复核和可追溯维护；当前玩法功能进入上方第三人称白盒。

历史 Godot Demo 保留在 [`../railcraft-godot`](../railcraft-godot)，Unity 运行时不加载其中的场景或脚本。

### 固定环境

- Unity Editor：`6000.3.21f1`（revision `c02631ffc030`）
- 编辑器模块：Windows Build Support
- 发布平台：Windows x86_64 / `StandaloneWindows64`
- 渲染：Universal Render Pipeline `17.3.0`，Linear 色彩空间
- 输入：Input System `1.17.0`
- UI：uGUI `2.0.0`
- Unity Test Framework：`Packages/manifest.json` 请求 `1.4.3`；Unity 6000.3.21f1 在 `packages-lock.json` 中解析为内置 `1.6.0`
- 默认窗口：1920×1080、Windowed

使用固定编辑器打开 `apps/railcraft-unity`。冻结 v0.1 的场景仍位于
`Assets/RailCraft/Scenes/Bootstrap.unity` 与 `Factory.unity`。其历史构建入口显式使用
这两张场景，因此无需覆盖当前白盒的默认 Build Settings。

### 自动化测试

以下命令在已激活许可证的 Windows 环境执行。Unity Test Framework `1.6.0` 的命令行测试不附加 `-quit`；测试运行器会在完成后退出并写入 XML。

```powershell
$UnityExe = 'C:\Program Files\Unity 6000.3.21f1\Editor\Unity.exe'
$Project = (Resolve-Path '.\apps\railcraft-unity').Path

& $UnityExe -batchmode -nographics `
  -projectPath $Project `
  -runTests -testPlatform EditMode `
  -testResults "$Project\TestResults\editmode.xml" `
  -logFile "$Project\TestResults\editmode.log"

& $UnityExe -batchmode -nographics `
  -projectPath $Project `
  -runTests -testPlatform PlayMode `
  -testResults "$Project\TestResults\playmode.xml" `
  -logFile "$Project\TestResults\playmode.log"
```

普通 PlayMode 套件不会伪造性能结果。`PerformanceCaptureTests` 在未设置采集开关时标记为跳过；最终性能门禁使用有图形设备的 Windows Standalone Player 单独运行：

```powershell
$env:RAILCRAFT_CAPTURE_PERFORMANCE = '1'
$env:RAILCRAFT_PERFORMANCE_OUTPUT = "$Project\TestResults\task12-performance.json"

& $UnityExe -batchmode `
  -projectPath $Project `
  -buildTarget StandaloneWindows64 `
  -standaloneBuildSubtarget Player `
  -runTests -testPlatform StandaloneWindows64 `
  -buildPlayerPath "$Project\Builds\Task12Perf" `
  -testFilter 'RailCraft.Tests.PlayMode.PerformanceCaptureTests.CompleteProductionFlowMeetsPlaceholderPerformanceBudget' `
  -playerHeartbeatTimeout 600 `
  -testResults "$Project\TestResults\task12-performance-player.xml" `
  -deviceLogs "$Project\TestResults\task12-player-logs" `
  -logFile "$Project\TestResults\task12-performance-runner.log"
```

该测试要求实际 1920×1080 Player、Frame Timing Stats 可用，并采集 `factory idle`、`all bogie modules installed`、`carbody lowering`、`commissioning feedback`、`final hero view` 五个状态。预算、设备与最终基线见 [`Documentation/PerformanceBudget.md`](Documentation/PerformanceBudget.md)。

### 构建 Windows 测试版

确保没有 Unity Editor 实例占用项目，然后运行：

```powershell
$UnityExe = 'C:\Program Files\Unity 6000.3.21f1\Editor\Unity.exe'
$Project = (Resolve-Path '.\apps\railcraft-unity').Path

& $UnityExe -batchmode -quit `
  -projectPath $Project `
  -executeMethod RailCraft.Editor.WindowsBuild.Build `
  -logFile "$Project\Artifacts\Acceptance\build.log"
```

成功后入口文件为：

```text
Builds/Windows/RailCraft.exe
```

构建方法显式读取冻结的 `Bootstrap` 与 `Factory` 场景，以 `CleanBuildCache` 方式生成 Windows x86_64 Player。构建失败会抛出 `BuildFailedException`，调用进程返回失败状态。

### 运行方法与操作

在 64 位 Windows 10/11 上双击 `RailCraft.exe`，或在 PowerShell 中运行：

```powershell
$Project = (Resolve-Path '.\apps\railcraft-unity').Path
& "$Project\Builds\Windows\RailCraft.exe"
```

运行时内容完全本地，不需要网络连接。主要操作：

- 鼠标左键：选择答案、点击 UI、拖动已解锁模块；
- 鼠标右键拖动：环绕当前观察中心；
- 鼠标中键拖动：平移观察中心；
- 鼠标滚轮：缩放；
- WASD 或方向键：平面移动观察中心；
- Esc：取消当前输入或按运行界面处理。

可拖拽模块保持作者设定旋转，用户没有零件旋转控制。错误答案可重试，错误拖放会返回起点且不增加进度。

### 完整 Build 目录交付

`RailCraft.exe` 不能作为单文件独立交付。发布时必须整体复制或压缩 `Builds/Windows/` 的全部内容，并保持 Unity 生成的相对目录结构。典型目录至少包含：

```text
Builds/Windows/
├─ RailCraft.exe
├─ RailCraft_Data/
├─ UnityPlayer.dll
├─ UnityCrashHandler64.exe
└─ 其他由当前脚本后端生成的 DLL/运行库
```

交付步骤：

1. 完成最终 EditMode、PlayMode、Standalone 性能测试和 GUI 走查。
2. 运行干净构建，确认 `Artifacts/Acceptance/build.log` 报告 `Succeeded`。
3. 对 `RailCraft.exe` 生成 SHA-256，并写入 `Artifacts/Acceptance/checksums.txt`。
4. 在另一目录解压完整 Build，断网启动，确认主菜单、Factory 场景和退出均正常。
5. 交付整个 `Windows` 目录或其 ZIP；不要遗漏、重命名或移动 `RailCraft_Data`、`UnityPlayer.dll` 和根目录运行库。

`Builds/` 与 `TestResults/` 被 Git 忽略，属于生成物。可追踪的验收清单、环境、截图、哈希与报告位于：

```text
Artifacts/Acceptance/
├─ acceptance-report.md
├─ build.log
├─ checksums.txt
├─ environment.txt
└─ screenshots/
   ├─ 01-main-menu.png
   └─ ...
      12-reset-to-guidance.png
```

范围与逐条证据见 [`Documentation/Scope.md`](Documentation/Scope.md) 和 [`Documentation/Acceptance.md`](Documentation/Acceptance.md)。

## 共享模型限制

当前第三人称白盒和冻结 v0.1 场景均主要使用程序生成的低多边形占位资产，用于流程、交互、镜头和性能基线。占位几何不提供 SWM-400E1 工程尺寸、安装接口、工艺参数或故障诊断结论。

候选 `deliveries/models/swm-400e1-wheel-v1/release/wheel.SLDPRT` 尚未通过生产门禁。进入 Unity 生产资产前仍需责任成员提供 STEP AP242 或 Parasolid `.x_t`、毫米单位确认、名义轮径/轮宽/轮毂孔径、轴与原点、车型适用声明和许可信息。生产网格还需 3 级 LOD、贴图预算、独立简化碰撞体及 URP 材质验证。详细规范见 [`Documentation/ModelHandoff.md`](Documentation/ModelHandoff.md)。

暂定流程内容与教学占位故障不构成经验证的 SWM-400E1 工厂作业指导、检验标准或真实故障结论。正式工艺和生产模型进入项目后，需要重新执行完整测试、GUI 走查、性能采集与验收。
