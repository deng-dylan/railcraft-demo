# RailCraft 第三人称标准实训候选版验收报告

- 验收日期：2026-08-21
- Unity：`6000.3.21f1 (c02631ffc030)`
- 平台：Windows x86_64
- 主场景：`Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`
- 构建入口：`RailCraft.ThirdPerson.Editor.WhiteboxWindowsBuild.BuildFromCommandLine`
- 候选定位：`v0.3.0-preview.1`

## 验收范围

本轮验证覆盖第三人称移动、镜头、标准工单主菜单、答题、零件解锁、拾取、库存、
分级装配、落车、教学故障注入、重新调试、检验、复测成功、结算、存档与重玩。

内容基线包括：

- 14 种基础零件与 14 个答题/拾取工位；
- 4 个基础模块、转向架构体和落车，共 6 个装配节点；
- 50 道四选一和 8 道判断题，共 58 题；
- 23 步进度、83 条工程知识、成绩结算和知识图鉴；
- 教学故障注入 → 重新调试 → 检验 → 返回调试 → 标准实训完成的完整闭环；
- 代表性转向架、带两套转向架的动力中间车和八编组出厂展示。

## 自动化结果

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| EditMode | 199/199 通过，0 失败、0 跳过 | [`editmode.xml`](editmode.xml) |
| Windows 构建 | 成功，0 warning、0 error | [`build.log`](build.log) |
| 成品流程烟测 | `RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED` | [`player-smoke.log`](player-smoke.log) |
| 代表性转向架 | 完成一套转向架构体并保持轮对、构架、悬挂和驱动语义 | [`screenshots/02-bogie.png`](screenshots/02-bogie.png) |
| 落车成品 | 显示动力中间车车体、两套转向架和统一识别层 | [`screenshots/03-landing.png`](screenshots/03-landing.png) |
| 完成画面 | 显示标准实训结算和八编组展示入口 | [`screenshots/01-complete.png`](screenshots/01-complete.png) |

构建日志记录的 Unity Player 总构建大小为 `207,955,367` 字节。本地完整运行目录位于
`Builds/Whitebox/`，该目录属于可再生构建产物，由 Git 忽略。当前测试与构建通过
Unity Hub 启动的已激活普通编辑器执行；本机账户缺少命令行 headless entitlement。

## 完整性

验收证据与本地可执行文件的 SHA-256 见 [`checksums.txt`](checksums.txt)。Git 跟踪源码、
场景、测试、文档和验收证据；Windows Build 通过相同 Unity 版本和上述构建入口重新生成。

本报告证明当前候选批次可构建、可运行并通过自动闭环。公开发行或工程实训交付前的
内容来源、资产授权、发行元数据和异机走查清单见
[`Documentation/ReleaseReadiness.md`](../../../Documentation/ReleaseReadiness.md)。
