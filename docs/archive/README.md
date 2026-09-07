# RailCraft 历史归档

当前体验、开发、构建和发行入口位于 [Unity ThirdPerson 主线](../../railcraft-unity/README.md)。
本页只说明如何追溯旧资料，不为当前任务提供运行时约束。

## 已退休的 Demo

第一版白盒之前的以下目录已从工作树移除：

- Godot v0.1.0 Demo：`apps/railcraft-godot/`
- GOOD2 Ren'Py 原型：`prototypes/good2-renpy/`
- 外部 Godot 4.6.3 原型包装：`prototypes/high-speed-rail-factory-godot-4.6.3/`
- Unity 固定视角 v0.1：`railcraft-unity/Assets/RailCraft/{Art,Content,Editor,Input,Scenes,Scripts,Tests}/`
- Unity 固定视角验收证据：`railcraft-unity/Artifacts/Acceptance/`

本机在 2026-09-04 建立了可恢复的日期归档：
`.tmp/retired-demos-20260904/manifest.tsv`。清单 SHA-256 为
`8e8a158419ef7d70da6ef9987a35737fcdec7733bcd0288a951f25e41dda2ba9`。该目录被 Git 忽略，
不进入构建或竞赛提交。

删除前生成的 Unity `Builds/Whitebox` 和 `ReleasePackages` 位于归档的
`generated-before-retirement/`，它们只用于对照，必须重新构建后才能运行。归档内可能存在
指向旧工作区的可再生缓存链接，恢复时只取源文件和清单。
已提交的历史内容也可以从 Git tag/提交中读取；恢复前先确认目标路径为空，并重新执行
布局、编译和测试检查。

## 外部交付登记

`deliveries/external/` 仍保留来源、校验和许可登记。原始压缩包位于被忽略的
`release/` 目录或团队私有存储，登记文件不代表当前主线会加载这些资源。需要重新查看时，
先核对许可证和再分发范围，再解压到临时目录。

## 历史文档的使用方式

`docs/project/proposal.md`、`detailed-design.md`、旧计划和评审记录保存早期决策背景。它们
可能包含 Godot、固定视角、48 题或具体车型的旧口径。当前实施请先读
[`../project/CURRENT_BASELINE.md`](../project/CURRENT_BASELINE.md)、
[`../project/CURRENT_STATUS.md`](../project/CURRENT_STATUS.md) 和
[`../project/CONSTRAINT_REVIEW.md`](../project/CONSTRAINT_REVIEW.md)。
