# RailCraft 当前状态

更新时间：2026-09-07
主线分支：`main`
本批开发分支：`codex/cw200k-model-upgrade`
当前内测候选：`v0.4.0-internal.2`（ART-004 / DEBUG-002 自动验收完成）
版本提交：`a541471 feat(unity): ship internal.2 ART-004 workbenches and DEBUG-002 colliders`；2026-09-07 已将本批脱敏版本推送 GitHub，主线合并与 CI 记录见 [PR #11](https://github.com/deng-dylan/railcraft-demo/pull/11)。公开验收证据见 `Artifacts/Whitebox/InternalTest/Internal2/`。

## 阶段与结论

- 阶段：`v0.4.0-internal.2` 工作台与碰撞批次已完成自动验收和 ZIP 交付。
- 主线：`railcraft-unity/Assets/RailCraft/ThirdPerson/`。
- 产品定位：通用高速动车组工程场景模拟；车型外观采用原创风格表达。
- 交互规模决策：5 个玩家材料包 + 1 个调试/检验流程模块；14 个内部子部件 ID 只用于配方、视觉分解和旧存档兼容。
- 旧 Demo：已从工作树移除，日期归档位于本机 `.tmp/retired-demos-20260904/`，Git 历史仍可追溯；`manifest.tsv` SHA-256 为 `8e8a158419ef7d70da6ef9987a35737fcdec7733bcd0288a951f25e41dda2ba9`。
- 删除前生成的 `Builds/Whitebox` 与 `ReleasePackages` 已移至归档的 `generated-before-retirement/`；当前材料包版 Player 已于 2026-09-04 重新构建。
- `ART-001` 免费资产扩展已写入生成器并重建场景：传感器座、定位元件、高度控制元件和一系弹性元件使用通用教学视觉；工具、安全隔离、厂区远景和远景车辆使用展示风格资产。
- `ART-002` 已在生成器中补入项目自有程序化教学件：二系空气弹簧、剪叉升降台、四点吊具、HMI 柜和线缆展示。该层不含车型专用尺寸与性能参数，序列化场景和运行证据已刷新。
- `DEBUG-001` 受控调试模式已接入：启动参数 `-railcraft-internal-debug` 加 `Ctrl+Alt+Shift+F10` 双门禁；解锁后可准备材料、推进落车、完成调试检验和重置进度，并显示持续水印。
- `DEBUG-002` 已加入碰撞箱调试显示：仅在 `DEBUG-001` 解锁后通过 F10 面板开关，绘制实际启用的非 Trigger Collider 世界包围盒。
- `ART-003` 的程序化工作台结构由 `ART-004` 接续：13 张知识、模块装配、总装与调试/检验工作台统一替换为用户提供的 Rebel Hideout 操作台；已接入两套 URP PBR 材质、按网格边界落地和 39 个实体 BoxCollider，保留 13 个根交互 Trigger。原站确认作者 Adrian Dierigl、CC BY 4.0，包内附署名。
- `DEBUG-002` 使用 Player 可见的运行时线框，取实际启用碰撞体世界包围盒；完整自动验收记录于 `Artifacts/Whitebox/InternalTest/Internal2/`。

## 已完成

| 项目 | 证据/位置 |
| --- | --- |
| 工厂第三人称主线 | `ThirdPerson/Scenes/ThirdPersonWhitebox.unity` |
| Art Alpha 视觉层 | `Documentation/ArtAlpha.md` |
| 角色、动画、起重机、轨道、集装箱、变电站和背景道具 | `ThirdPerson/Editor/*VisualFactory.cs` |
| CW-200K 完成态转向架与牵引拉杆视觉接入 | `Cw200kReferenceVisualFactory.cs`、Art Alpha 截图 |
| 58 题原件逐题导入，10 题作为材料包核心门禁 | `Runtime/Domain/WhiteboxQuestionBank.cs`、`Runtime/Domain/WorkPackageDefinition.cs` |
| 材料包级答题、领取和批量安装 API | `Runtime/Domain/WhiteboxGameSession.cs`、`Runtime/World/IWorkPackageGameSession.cs` |
| 题库/核心题/材料包/奖励路由独立版本 | `Runtime/Domain/WhiteboxContentVersions.cs`、会话快照 |
| EditMode | 当前主线 221/221，0 失败、0 跳过（`Artifacts/Whitebox/InternalTest/Internal2/acceptance-report.md`） |
| Windows 构建 | 0 warning / 0 error，BuildReport 485,632,394 bytes（`InternalTest/Internal2/acceptance-report.md`） |
| Player 成品烟测 | 普通模式及碰撞箱模式均通过，13 新模型 / 39 实体 BoxCollider / 13 交互 Trigger（`InternalTest/Internal2/acceptance-report.md`） |
| 独立内测包 | `v0.4.0-internal.2` ZIP 236,298,877 bytes；305 文件逐一哈希一致，新解压目录完整烟测通过 |
| ART-001 静态接入 | `ThirdPerson/Editor/FreeAssetExpansionVisualFactory.cs`、`Artifacts/Whitebox/ArtAlpha/art-001-asset-expansion.md` |
| ART-002 原创补缺 | `ThirdPerson/Editor/ProjectAuthoredEquipmentVisualFactory.cs`、`Artifacts/Whitebox/ArtAlpha/art-002-authored-gap-fill.md` |
| DEBUG-001 内测调试 | `Runtime/UI/WhiteboxInternalDebugController.cs`、`Documentation/InternalTest.md` |
| v0.4.0-internal.1 自动验收 | 212/212 EditMode、构建目录与暂存包烟测均通过（`Artifacts/Whitebox/InternalTest/`） |
| ART-004 / DEBUG-002 | `Artifacts/Whitebox/InternalTest/Internal2/acceptance-report.md`，含三类工作台与运行时碰撞线框截图 |

## 仍在处理

1. 为非核心题补齐更细的主题标签与来源审核记录；
2. 继续清理历史文档中的 CR400AF/SWM-400E1 锁定表述；
3. 以工程来源替换 ART-001/ART-002 的传感器座、定位元件、高度控制和二系空气弹簧通用教学视觉；
4. 补齐定位夹具、扭矩工具、真实检验仪表、地面贴花和雾效；升降台、吊具、HMI 与线缆已有原创展示层；工作台外部模型、材质和 CC BY 4.0 许可已完成 ART-004 核验；
5. 为正式模型建立 LOD、碰撞、材质和纹理验收报告；
6. 复核所有第三方资产许可证、署名和竞赛提交范围；
7. 在目标机完成 1920×1080 性能和异机断网走查。

## 当前风险

| 风险 | 影响 | 处理 |
| --- | --- | --- |
| 参考转向架来源于 CW-200K | 车型表述可能误导评审 | 统一标记为通用教学/参考件，不做具体车型声明 |
| 部分题目含车型相关数值 | 工程知识可信度不足 | 标记 `pending-review`，核心流程先使用概念性题目 |
| 变体缺模型时存在回退 | 菜单身份与画面可能不一致 | 变体接入前做资源存在性门禁，禁止静默冒充 |
| 第三方许可证据不完整 | 无法公开再分发 | 仅限本地/内部集成，提交包前逐项核验 |
| 生成器会覆盖手工场景修改 | 美术返工丢失 | 视觉改动进入 Prefab/工厂生成器，并记录批次 |
| 新工作台仅有 LOD0 | 目标机性能仍待实测 | 当前 13 实例共享网格与材质；许可及碰撞已核验，未虚构 LOD1/LOD2 |

## 下一批建议顺序

1. 在目标展示机完成 1920×1080、组合键调试入口和断网解压人工走查；
2. 为已接入通用教学件建立工程替换清单，并接入可核验工装/HMI 资产；
3. 完成厂房近景/中景/远景三层美术；
4. 复核竞赛提交范围内全部第三方许可证与署名；
5. 通过人工门禁后再命名下一预发布版本。
