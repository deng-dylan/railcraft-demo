# RailCraft Unity v0.1 性能预算与实测

记录日期：2026-08-01

本页记录 Task 12 的工厂表现预算、最终烘焙配置，以及在开发机 Windows Player 中完成整条生产流程后的五状态实测。原始数据来自 `TestResults/task12-performance.json`，自动化结果来自 `TestResults/task12-performance-player.xml`。

## 结论

最终占位资产场景满足本版本的全部运行预算：峰值可见三角形低于 2,000,000，峰值 Draw Calls 低于 500，平均帧率高于 60 FPS，1% low 高于 45 FPS，首次工厂场景加载低于 10 秒。当前数值反映轻量占位几何体，生产级 SWM-400E1 模型导入后必须在同一设备、同一设置和同一五状态流程下重新采集。

## 测试环境

| 项目 | 实测值 |
|---|---|
| Unity | 6000.3.21f1（revision `c02631ffc030`） |
| URP | 17.3.0 |
| Unity Test Framework | 1.6.0（Unity 6000.3 内置解析版本） |
| 操作系统 | Windows 11（10.0.26200） |
| CPU | Intel(R) Core(TM) Ultra 7 255HX |
| GPU | NVIDIA GeForce RTX 5070 Ti Laptop GPU |
| 图形 API | Direct3D12 |
| 显存 | 11,944 MiB |
| 系统内存 | 32,213 MiB |
| 分辨率 | 1920×1080 |
| 质量级别 | High |
| 窗口模式 | Windowed |
| VSync | 关闭 |
| Application.targetFrameRate | -1（不设上限） |
| Frame Timing Stats | 开启并在 Player 内确认可用 |
| Player batch mode | false |

采集逻辑在每个状态预热 30 帧，然后采集约 1.5 秒。平均 FPS 使用样本帧数除以全部样本帧时总和；1% low 使用最慢 1% 帧的样本数除以这些帧的帧时总和。Draw Calls、Triangles 与已分配内存记录采样窗口峰值；主线程和渲染线程耗时优先读取 `FrameTimingManager`，并以 `ProfilerRecorder` 作为回退。

## 性能预算

| 指标 | 预算 | 门禁 |
|---|---:|---|
| 峰值可见三角形 | ≤ 2,000,000 | Player 测试逐状态断言 |
| 峰值 Draw Calls | ≤ 500 | Player 测试逐状态断言 |
| 实时投射阴影灯光 | ≤ 2 | EditMode 场景契约断言 |
| 生产子系统网格 LOD | 3 级 | 生产模型导入后处理器与验证器 |
| 大型 hero 资产贴图上限 | 2048 | TextureImporter 自动限制 |
| 工厂 props 贴图上限 | 1024 | TextureImporter 自动限制 |
| 1920×1080 平均帧率 | ≥ 60 FPS | Player 测试逐状态断言 |
| 1920×1080 1% low | ≥ 45 FPS | Player 测试逐状态断言 |
| 首次工厂场景加载 | ≤ 10 秒 | Player 测试断言 |

## 五状态实测

首次 Bootstrap 加载并完成 Factory 绑定：**0.211 秒**，预算为 10 秒。

| 状态 | 平均 FPS | 1% low FPS | 峰值 Draw Calls | 峰值三角形 | 主线程平均 ms | 渲染线程平均 ms | 峰值已分配内存 MiB | 采样帧 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| factory idle | 805.54 | 342.18 | 218 | 12,759 | 0.718 | 0.466 | 135.61 | 1,209 |
| all bogie modules installed | 848.62 | 359.04 | 209 | 12,973 | 0.702 | 0.446 | 136.75 | 1,273 |
| carbody lowering | 787.03 | 321.84 | 196 | 10,719 | 0.741 | 0.487 | 136.56 | 1,181 |
| commissioning feedback | 843.38 | 348.76 | 184 | 7,699 | 0.708 | 0.431 | 136.62 | 1,266 |
| final hero view | 807.47 | 326.79 | 219 | 13,371 | 0.727 | 0.464 | 136.65 | 1,212 |

五个状态均通过全部性能门禁。高帧率来自当前占位几何体规模、VSync 关闭和未限帧设置；该结果用于 v0.1 基线及回归对比。

## 光照、探针与遮挡烘焙

最终工厂场景使用静态烘焙与轻量后处理：

- 光照贴图器：Progressive CPU。
- Mixed Lighting：Shadowmask；方向光为 Mixed、Soft Shadows。
- 12 个工位点光源：Baked、无实时阴影。
- 实时/混合投射阴影灯光数量：1，预算 ≤ 2。
- Lightmap 分辨率：10 texels/unit；最大尺寸 1024；padding 2；Normal Quality 压缩。
- 方向模式：Combined Directional。
- 采样：Direct 16、Indirect 64、Environment 32；最少反弹 1、最多反弹 2。
- AO：开启，最大距离 1 m；间接指数 1，直接指数 0.5。
- Light Probes：1 个 LightProbeGroup，共 50 个位置，供可拖拽动态模块采样。
- Reflection Probes：东西两区各 1 个，共 2 个 Baked 探针；分辨率 128，Box Projection 开启，Blend Distance 2 m。
- Occlusion Culling：Smallest Occluder 3 m、Smallest Hole 0.25 m、Backface Threshold 100。
- 可拖拽模块和 3 个未来锁定预览保持动态，未标记 ContributeGI、OccluderStatic 或 OccludeeStatic。
- 全局 Volume：ACES、Bloom 0.12（低质量过滤、最多 4 次迭代）、轻量 Color Adjustments。

最终烘焙产物：

| 证据 | 结果 |
|---|---|
| Lighting bake | 10.93 秒 |
| Occlusion compute | 0.08 秒 |
| Combined lightmaps | 2 组有效 `Lightmap-*_comp_light.exr` |
| Reflection components | 2 个场景内 Baked ReflectionProbe |
| LightingData.asset | 59,313 bytes |
| FactoryOcclusion.asset | 27,118 bytes |

最终日志直接读取稳定路径下的遮挡资产文件大小，记录为 `occlusion_bytes=27118`。

## LOD、贴图与生产材质门禁

`ProductionAssetBudgetPostprocessor` 执行以下规则：

- 路径包含 `Assets/RailCraft/Art/Textures/Hero/` 的贴图上限为 2048。
- 路径包含 `Assets/RailCraft/Art/Textures/Props/` 的贴图上限为 1024。
- 标记为 production 的模型契约必须包含 3 个 LOD 层级。
- `Assets/RailCraft/Art/Models/Production/` 中的模型在两种 Unity 材质导入路径中都会配置 property-block emission。
- Catalog 中每个 `DraggableModule.VisualRoot` 的材质必须提供可写基础色、`_EmissionColor`、有效且启用的本地 `_EMISSION` keyword，并精确使用 `RealtimeEmissive` GI flags。
- 固定占位高亮材质使用黑色 emission 基线；导入的生产材质保留作者化 `_EmissionColor`。

当前 v0.1 工厂仍使用程序生成的占位几何体，尚无 hero/props 生产贴图。SWM-400E1 车轮生产模型交付门禁仍待外部输入：需要 STEP AP242 或 Parasolid 中性格式、确认的名义尺寸、坐标/单位、车型适配与许可信息。候选模型状态见 `Documentation/ModelHandoff.md`。生产模型进入目录后必须通过 3 LOD、贴图上限与材质发光能力验证，并重跑本页全部测试。

## 自动化证据

- `TestResults/task12-performance-player.xml`：Windows Standalone 性能用例 1/1 通过。
- `TestResults/task12-performance.json`：最终五状态原始数据与设备信息。
- `TestResults/task12-bake-presentation-final4.log`：最终光照和遮挡烘焙记录。
- `TestResults/task12-presentation-editmode.xml`：工厂表现与生产材质契约 6/6 通过。
- `Assets/RailCraft/Tests/PlayMode/VisualFeedbackTests.cs`：青色当前模块、琥珀目标、绿色成功、红色拒绝、未来模块中性灰及生命周期清理契约。
- `Assets/RailCraft/Tests/EditMode/FactoryPresentationContractTests.cs`：烘焙、探针、遮挡、动态对象、URP emission、LOD/贴图/生产材质预算与文档契约。
