# RailCraft Unity v0.1 验收报告

- 验收日期：2026-08-01
- 发布候选：v0.1 Windows x86_64 测试版
- Unity：6000.3.21f1（revision `c02631ffc030`）
- 最终结论：**通过（限定于 `Documentation/Scope.md` 定义的 v0.1 测试版范围）**

本结论确认可交付的 Windows 本地体验、固定题库、引导式流程、自动化回归和验收证据完整可复核。占位几何、暂定流程和教学异常不构成 SWM-400E1 生产工艺、尺寸、检验标准或真实故障诊断结论。

## 证据目录

| 内容 | 位置 | 结论 |
|---|---|---|
| 最终 Windows 构建摘要 | `build.log` | 成功，2 个场景、0 warning、0 error；机器信息已脱敏 |
| EditMode XML | `editmode.xml` | 50/50 通过 |
| PlayMode XML | `playmode.xml` | 50 通过、0 失败、1 个预期性能门禁跳过 |
| 独立 Player 性能 XML | `performance-player.xml` | 1/1 通过 |
| 性能原始数据 | `performance.json` | 5 个场景状态均记录 |
| 机器与运行环境 | `environment.txt` | 已记录 |
| 可执行文件和证据哈希 | `checksums.txt` | 已记录 |
| GUI 截图 | `screenshots/01` 至 `12` | 12/12 存在，均为 1920×1080 PNG |

## 自动化回归

| 套件 | 时间（UTC） | Total | Passed | Failed | Skipped | 结论 |
|---|---|---:|---:|---:|---:|---|
| EditMode | 2026-08-01 17:00:10–17:00:12 | 50 | 50 | 0 | 0 | 通过 |
| PlayMode | 2026-08-01 17:01:05–17:01:23 | 51 | 50 | 0 | 1 | 通过 |
| Windows Standalone 性能门禁（Task 12 基线） | `performance-player.xml` | 1 | 1 | 0 | 0 | 通过 |

PlayMode 中跳过的用例为 `PerformanceCaptureTests.CompleteProductionFlowMeetsPlaceholderPerformanceBudget`。该用例仅在设置 `RAILCRAFT_CAPTURE_PERFORMANCE=1` 时执行，普通全量回归刻意不伪造硬件帧时序数据。独立 Player 性能门禁保留了真实图形设备采集结果。

本轮新增或加强的回归覆盖：

- 快速拖放时，鼠标按下位置会被即时记录；即使同一输入帧移动到目标区域，仍从原始模块位置开始拖放。
- 投放以目标在屏幕上的可见位置判定，模块与目标处于不同深度时仍可稳定吸附。
- 车体落车阶段会验证待装车体碰撞体与目标接口同时处于主相机视口内。
- Bootstrap HUD 和错误投放反馈的锚点、枢轴和边距有场景契约测试保护。
- 项目配置测试禁止重新引入 Purchasing、Services Core 与 Analytics 包。

## Windows 构建

| 项目 | 最终记录 |
|---|---|
| 构建入口 | `RailCraft.Editor.WindowsBuild.Build` |
| 目标 | `StandaloneWindows64` |
| 已启用场景 | 2（Bootstrap、Factory） |
| BuildPipeline 汇总大小 | 109,337,215 bytes |
| 构建耗时 | 22,457 ms |
| Warning / Error | 0 / 0 |
| 启动文件 | `Builds/Windows/RailCraft.exe` |
| EXE 大小 | 667,136 bytes |
| SHA-256 | `48EFAB523AA684C653BD1254A6962D3410127B5C02DC1310F6F16F4810666556` |
| 启动方式 | 从完整 `Builds/Windows/` 目录直接启动，完成走查后正常关闭 |

`build.log` 保留机器可读的 `RAILCRAFT_WINDOWS_BUILD_SUCCEEDED` 标记和原始日志哈希，省略 Unity 许可会话、网络、进程、用户目录与机器标识。发布时必须整体复制或压缩 `Builds/Windows/`，不可只分发 `RailCraft.exe`。

## GUI 走查

走查在 1920×1080 Windowed Player 中完成，使用实际鼠标输入完成 48 道题、15 个内容步骤和完整的首次调试—整改—检验—再次调试—投入使用闭环。最后执行重置并以正常窗口关闭方式退出，RailCraft 窗口已消失。

| # | 截图 | 状态 | 已复核内容 |
|---:|---|---|---|
| 01 | `screenshots/01-main-menu.png` | 通过 | 主菜单含开始、说明、设置、退出；无继续入口。 |
| 02 | `screenshots/02-guidance.png` | 通过 | 显示目标、鼠标/键盘操作、镜头控制和占位范围说明。 |
| 03 | `screenshots/03-wrong-answer-retry.png` | 通过 | 错答保持当前题，可重新选择；无分数、来源或等级。 |
| 04 | `screenshots/04-step-unlocked.png` | 通过 | 首阶段知识准备完成，当前模块与唯一目标接口清晰可见。 |
| 05 | `screenshots/05-wrong-drop-feedback.png` | 通过 | 错误投放给出反馈，未推进装配进度。 |
| 06 | `screenshots/06-powered-bogie-complete.png` | 通过 | 11/15 后进入车体落车阶段，动力转向架子系统完成。 |
| 07 | `screenshots/07-carbody-lowering.png` | 通过 | 简化动力中间车位于可视的竖直落车起始位置。 |
| 08 | `screenshots/08-first-commissioning-failure.png` | 通过 | 初次调试展示完整的教学占位异常声明和整改入口。 |
| 09 | `screenshots/09-inspection-and-rework.png` | 通过 | 整改与检验阶段、传感器检查模块和目标接口可见。 |
| 10 | `screenshots/10-second-commissioning-pass.png` | 通过 | 再次调试通过，随后进入投入使用准备；未重复展示调试题。 |
| 11 | `screenshots/11-release-hero-view.png` | 通过 | 流程完成面板、投入使用状态、放行车辆与最终镜头切换可见。 |
| 12 | `screenshots/12-reset-to-guidance.png` | 通过 | 重置后回到操作说明，运行态安装物与进度已清空。 |

镜头绕转、平移、缩放和拖放期间保持作者设定旋转由 `FactoryCameraControllerTests`、`DragDropControllerTests` 及运行时说明共同覆盖；本轮 GUI 走查聚焦于完整业务闭环和真实拖放输入。

## 本地运行、隐私与排除功能检查

- 运行时代码和内容目录静态扫描未检出 `UnityWebRequest`、`HttpClient`、`WebClient`、`System.Net`、`PlayerPrefs`、`AudioSource`、计分、准确率、排行、多人、XR 或 Android 调用点。
- `Packages/manifest.json` 和 `packages-lock.json` 不含 `com.unity.purchasing`、`com.unity.services.core` 或 `com.unity.analytics`。
- 项目未启用 Services/IAP/Analytics 包或调用点；Unity 引擎自身可能携带内部 Analytics 模块，该事实不表示项目启用了分析功能。
- Player 从完整本地 Build 目录启动。网络独立性以本地内容、无联网 API 调用点和无 Services 包为依据；本次记录未包含操作系统物理断网切换。
- 没有存档、继续、账户、云同步、音频、触控、Android、XR 或多人功能。

## 性能基线

性能数据来自 `performance.json` 中的 Windows Standalone Player 实测：NVIDIA GeForce RTX 5070 Ti Laptop GPU、Direct3D12、1920×1080、High、Windowed、Frame Timing Stats 开启。

| 状态 | 平均 FPS | 1% Low FPS | 峰值 Draw Calls | 峰值三角形 | 峰值内存 MiB |
|---|---:|---:|---:|---:|---:|
| factory idle | 805.54 | 342.18 | 218 | 12,759 | 135.61 |
| all bogie modules installed | 848.62 | 359.04 | 209 | 12,973 | 136.75 |
| carbody lowering | 787.03 | 321.84 | 196 | 10,719 | 136.56 |
| commissioning feedback | 843.38 | 348.76 | 184 | 7,699 | 136.62 |
| final hero view | 807.47 | 326.79 | 219 | 13,371 | 136.65 |

首次 Bootstrap 加载并完成 Factory 绑定耗时 0.211449 秒。该数据适用于当前占位资产；替换为生产模型后必须在同一门禁下重新采集。

## 范围、限制与后续门禁

`Documentation/Scope.md` 固化 GC-01 至 GC-28 的测试版边界，`Documentation/Acceptance.md` 将每条约束映射到测试、配置、静态检查或 GUI 证据。当前通过范围包含引导流程产品化和可追溯验收，不覆盖下列生产结论：

- 占位车体、转向架子系统和流程卡片不提供工程尺寸、材料、扭矩、配合、公差、吊装、检验参数或安全极限。
- 初次调试的传感器异常仅用于教学闭环演示，不可用于实际 SWM-400E1 故障判断。
- `wheel.SLDPRT` 仍是候选源资产。缺少中性格式、毫米单位确认、名义尺寸、轴向/原点、适用性声明、许可证和 LOD/碰撞/材质门禁前，不生成生产运行时车轮资产。
- 导入生产模型或经成员确认的工艺后，需重新执行完整 EditMode、PlayMode、GUI、性能和 Windows 构建验收。
