# RailCraft 当前状态

更新时间：2026-09-04
当前分支：`codex/cw200k-model-upgrade`
本批实现提交：`077fa3b refactor(repo): establish Unity Art Alpha mainline`

## 阶段与结论

- 阶段：白盒之后的首个受控视觉生产批次 `v0.4.0-art-alpha.5`。
- 主线：`railcraft-unity/Assets/RailCraft/ThirdPerson/`。
- 产品定位：通用高速动车组工程场景模拟；车型外观采用原创风格表达。
- 交互规模决策：5 个玩家材料包 + 1 个调试/检验流程模块；14 个内部子部件 ID 只用于配方、视觉分解和旧存档兼容。
- 旧 Demo：已从工作树移除，日期归档位于本机 `.tmp/retired-demos-20260904/`，Git 历史仍可追溯；`manifest.tsv` SHA-256 为 `8e8a158419ef7d70da6ef9987a35737fcdec7733bcd0288a951f25e41dda2ba9`。
- 删除前生成的 `Builds/Whitebox` 与 `ReleasePackages` 已移至归档的 `generated-before-retirement/`；当前材料包版 Player 已于 2026-09-04 重新构建。
- `ART-001` 免费资产扩展已写入生成器并重建场景：传感器座、定位元件、高度控制元件和一系弹性元件使用通用教学视觉；工具、安全隔离、厂区远景和远景车辆使用展示风格资产。该批仍需 Unity 运行门禁，不能替代上一批 FLOW-002 验收结论。

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
| EditMode | 当前主线 200/200，0 失败、0 跳过（`Artifacts/Whitebox/ArtAlpha/editmode.xml`） |
| Windows 构建 | 0 warning / 0 error，184 文件、381,854,222 bytes（`Artifacts/Whitebox/ArtAlpha/build.log`） |
| Player 成品烟测 | `RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED`（`Artifacts/Whitebox/ArtAlpha/player-smoke.log`） |
| 独立发布包 | `v0.4.0-art-alpha.5` ZIP 184,285,006 bytes，解包目录烟测通过 |
| ART-001 静态接入 | `ThirdPerson/Editor/FreeAssetExpansionVisualFactory.cs`、`Artifacts/Whitebox/ArtAlpha/art-001-asset-expansion.md` |

## 仍在处理

1. 为非核心题补齐更细的主题标签与来源审核记录；
2. 继续清理历史文档中的 CR400AF/SWM-400E1 锁定表述；
3. 以工程来源替换 ART-001 的传感器座、定位元件和高度控制通用教学视觉，并补齐明确的二系空气弹簧几何；
4. 补齐定位夹具、升降台、吊具、扭矩工具、真实 HMI、线缆、地面贴花和雾效；基础手工具、窗体、安全隔离及厂区远景已有展示层；
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
| ART-001 尚未刷新 Unity 自动化结果 | 新资产批次不能直接作为发布候选 | 当前 Unity 许可返回 code 198；恢复许可后重跑 EditMode、构建与 Player 烟测 |
| 生成器会覆盖手工场景修改 | 美术返工丢失 | 视觉改动进入 Prefab/工厂生成器，并记录批次 |

## 下一批建议顺序

1. 恢复 Unity 许可并完成 ART-001 的 EditMode、Windows 构建和 Player 烟测；
2. 为已接入通用教学件建立工程替换清单，并接入可核验工装/HMI 资产；
3. 完成厂房近景/中景/远景三层美术；
4. 重建场景、跑测试、构建、烟测并更新本文件和 Art Alpha 证据；
5. 通过授权与目标机门禁后再命名下一预发布版本。
