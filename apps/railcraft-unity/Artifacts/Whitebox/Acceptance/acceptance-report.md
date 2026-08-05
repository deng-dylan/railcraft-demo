# RailCraft 第三人称白盒验收报告

- 验收日期：2026-08-06
- Unity：`6000.3.21f1 (c02631ffc030)`
- 平台：Windows x86_64
- 主场景：`Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`
- 构建入口：`RailCraft.ThirdPerson.Editor.WhiteboxWindowsBuild.BuildFromCommandLine`

## 验收范围

本轮验证覆盖第三人称移动、镜头、答题、零件解锁、拾取、库存、分级装配、落车、首次调试失败、重新调试、检验、复测成功和投入使用。

内容基线包括：

- 14 种基础零件与 14 个答题/拾取工位；
- 4 个基础模块、转向架构体和落车，共 6 个装配节点；
- 50 道四选一和 8 道判断题，共 58 题；
- 调试失败 → 重新调试 → 检验 → 返回调试 → 投入使用的完整闭环。

## 自动化结果

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| EditMode | 34/34 通过，0 失败、0 跳过 | [`editmode.xml`](editmode.xml) |
| 全仓库 EditMode 回归 | 84/84 通过，覆盖当前白盒与冻结 v0.1 | [`repository-editmode.xml`](repository-editmode.xml)、[`repository-editmode.log`](repository-editmode.log) |
| Windows 构建 | 成功，0 warning、0 error | [`build.log`](build.log) |
| 成品流程烟测 | `RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED` | [`player-smoke.log`](player-smoke.log) |
| 完成画面 | 显示“调试通过，车辆投入使用” | [`screenshots/01-complete.png`](screenshots/01-complete.png) |

构建日志记录的 Unity Player 总构建大小为 `103,580,649` 字节。本地完整运行目录位于 `Builds/Whitebox/`，该目录属于可再生构建产物，由 Git 忽略。

## 完整性

验收证据与本地可执行文件的 SHA-256 见 [`checksums.txt`](checksums.txt)。Git 跟踪源码、场景、测试、文档和验收证据；Windows Build 通过相同 Unity 版本和上述构建入口重新生成。
