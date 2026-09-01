# RailCraft 工程场景数字化项目

> **当前发行：RailCraft v0.3.0-preview.1。**
> Windows x86_64 的 Unity ThirdPerson 标准实训是唯一的日常体验、开发和验收入口。

RailCraft 用工厂第三人称场景验证“标准工单、知识确认、材料领取、分级装配、落车、教学故障调试与检验”的实训闭环。本版本面向竞赛演示、阶段汇报与受控教学体验；工程知识和模型仍须以审核资料为准。

## 从这里开始

| 需要什么 | 入口 |
| --- | --- |
| 下载、运行、限制与校验方式 | [当前发行说明](railcraft-unity/Documentation/Release.md) |
| 当前玩法、操作与开发约定 | [Unity 主线 README](railcraft-unity/README.md) |
| 当前流程、题库、资产替换与验收条件 | [ThirdPerson 标准实训规格](railcraft-unity/Documentation/ThirdPersonWhitebox.md) |
| 构建、测试、冒烟和截图证据 | [当前验收报告](railcraft-unity/Artifacts/Whitebox/Acceptance/acceptance-report.md) |
| 仓库路径、模型和交付边界 | [仓库地图](docs/repository-map.md) |

## 当前主线

| 区域 | 当前职责 | 状态 |
| --- | --- | --- |
| [railcraft-unity](railcraft-unity) | Unity ThirdPerson 标准实训、Windows 构建与当前证据 | **唯一主线** |
| [railcraft-unity/Assets/RailCraft/ThirdPerson](railcraft-unity/Assets/RailCraft/ThirdPerson) | 流程代码、场景、测试、白盒视觉和模型插槽 | **持续迭代** |
| [railcraft-unity/Artifacts/Whitebox/Acceptance](railcraft-unity/Artifacts/Whitebox/Acceptance) | 当前版本的构建、测试、冒烟和截图 | **当前证据** |

当前版本包含 58 道题、14 个答题与零件领取工位、14 个零件、6 个装配节点、23 步进度、自动存档、知识图鉴、成绩结算，以及“教学故障注入 → 处理 → 检验 → 复测”的完整闭环。普通玩家入口固定为复兴号标准工单 RC-EMU-01。

## 快速运行

1. 使用 Unity 6000.3.21f1 打开 railcraft-unity。
2. 在 Unity 菜单执行 RailCraft > Third Person Whitebox > Rebuild Scene。
3. 执行 RailCraft > Third Person Whitebox > Build Windows x86_64，或运行已有完整构建目录中的 Builds/Whitebox/RailCraftWhitebox.exe。
4. 使用 WASD、Shift、鼠标、E 和 ESC 完成标准实训。

构建会先重新生成白盒场景；长期保留的视觉改动应进入生成器、Prefab 或配置，避免直接编辑生成场景。

## 发行边界

- Windows x86_64 是唯一发行平台。
- 发行包必须保留完整 Unity 目录结构，不能只交付 EXE。
- 本版本不表达真实动车组作业指导、扭矩、公差、检修结论或未审核的工程参数。
- 正式工程交付前仍须完成内容来源、资产授权、异机走查和性能采样，详见[发行评估快照](railcraft-unity/Documentation/ReleaseReadiness.md)。

## 开发与治理

- 每批改动围绕一个可验证目标组织，并同步更新代码、文档、测试和验收证据。
- 模型、CAD、外部原始包和大文件遵循[仓库地图](docs/repository-map.md)及[维护规则](docs/MAINTENANCE.md)。
- 当前产品决策以[ADR-0002](docs/decisions/0002-third-person-whitebox-mainline.md)为准。

<details>
<summary><strong>历史归档与原型（仅供复现、审计和参考）</strong></summary>

历史资料不进入当前发行包，也不承接当前玩法功能：

- Unity 固定视角 v0.1 验收基线保留在 railcraft-unity 的 Assets/RailCraft/Scenes 和 Artifacts/Acceptance。
- Godot v0.1.0-demo 保留在 [apps/railcraft-godot](apps/railcraft-godot)。
- GOOD2、外部 Godot 等原型保留在 [prototypes](prototypes)。
- 启动期资料、外部评审与历史交付索引见[历史归档](docs/archive/README.md)。

</details>
