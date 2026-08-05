# 项目基线资料说明

这里保存项目启动阶段的需求、设计、实施提示词和模块任务，内容保留原始上下文，
便于追溯决策来源。该批资料以 Godot `v0.1.0-demo` 为目标，现已成为历史项目基线；
其中的引擎、目录和发布要求不再约束当前 Unity 第三人称白盒。

## 当前权威入口

- 当前开发主线：[`../../apps/railcraft-unity`](../../apps/railcraft-unity)
- 当前白盒规格：
  [`../../apps/railcraft-unity/Documentation/ThirdPersonWhitebox.md`](../../apps/railcraft-unity/Documentation/ThirdPersonWhitebox.md)
- 当前与历史 Unity 文档索引：
  [`../../apps/railcraft-unity/Documentation/README.md`](../../apps/railcraft-unity/Documentation/README.md)
- 当前主线决策：
  [`../decisions/0002-third-person-whitebox-mainline.md`](../decisions/0002-third-person-whitebox-mainline.md)
- 后续实施计划规则：[`plans/README.md`](plans/README.md)

当前白盒包含58道题、14个零件、6个装配节点和完整调试闭环，本地构建入口为
`apps/railcraft-unity/Builds/Whitebox/RailCraftWhitebox.exe`。当前验收证据统一写入
`apps/railcraft-unity/Artifacts/Whitebox/Acceptance/`。

## 历史路径映射

2026-07-21 起，项目总仓库采用以下映射：

| 历史文件中的路径 | 当前路径 |
| --- | --- |
| `doc/` | `docs/project/`（启动期资料）或 `apps/railcraft-godot/doc/`（Godot 应用交付文档） |
| `demo/` | `apps/railcraft-godot/` |

历史描述中的“外层仓库不纳入 Demo”反映当时的实施边界。Godot 的独立提交历史后来
通过 Git 合并历史纳入总仓库，当前 `apps/railcraft-godot/` 是普通受版本控制目录，
没有使用 Git 子模块。Godot 应用已冻结为历史 Demo，仍保留自身的构建、测试、发布
资料和原始路径语境。

`proposal.md`、`detailed-design.md`、`prompt.md` 和 `tasks/` 不随 Unity 主线改写。
需要借鉴其中的需求或交互时，应在当前 Unity 文档或新计划中重新登记适用范围、
验收方式和来源，避免把历史 Godot 约束直接当作现行要求。
