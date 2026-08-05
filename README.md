# RailCraft 工程场景数字化项目

本仓库是 RailCraft 项目的总仓库，统一管理需求、设计、调研、原型评审、交付登记，以及可直接构建和运行的应用源码。

当前开发主线是 [`apps/railcraft-unity`](apps/railcraft-unity) 中的 Unity
第三人称流程白盒。玩家在工厂中完成第三人称移动、58 道题循环、14 个零件的
答题解锁与拾取、6 个装配节点，以及落车后的调试失败、重新调试、检验、复测和
投入使用闭环。主线场景、构建入口和验收约定见
[`ThirdPersonWhitebox.md`](apps/railcraft-unity/Documentation/ThirdPersonWhitebox.md)。

## 当前组成

仓库同时保留两套已冻结实现：Unity 固定视角 v0.1 是可复核的验收基线；Godot
`v0.1.0-demo` 是历史 Demo。两者用于回归、参考和追溯，不接收当前玩法功能。

| 区域 | 用途 | 状态 |
| --- | --- | --- |
| [`apps/railcraft-unity`](apps/railcraft-unity) | Unity 第三人称白盒当前开发主线；同工程保留固定视角 v0.1 | 主线持续迭代；旧 v0.1 已冻结 |
| [`apps/railcraft-godot`](apps/railcraft-godot) | 项目自有 Godot `v0.1.0-demo` | 冻结历史 Demo |
| [`docs/project`](docs/project) | Godot 启动期需求、设计和实施任务资料 | 历史项目基线 |
| [`docs/reviews`](docs/reviews) | 外部 Demo、交付物与技术方案评审 | 持续更新 |
| [`docs/decisions`](docs/decisions) | 团队已确认的仓库和技术决策 | 持续更新 |
| [`docs/MAINTENANCE.md`](docs/MAINTENANCE.md) | 文件边界、缓存与归档维护规则 | 当前有效 |
| [`apps/railcraft-unity/Artifacts/Whitebox/Acceptance`](apps/railcraft-unity/Artifacts/Whitebox/Acceptance) | 当前第三人称白盒的构建、测试、冒烟和截图证据 | 随主线批次更新 |
| [`apps/railcraft-unity/Artifacts/Acceptance`](apps/railcraft-unity/Artifacts/Acceptance) | Unity 固定视角 v0.1 的可复核验收证据 | 冻结保留 |
| [`prototypes`](prototypes) | 独立原型的可审计源快照与说明 | 参考用途 |
| [`deliveries`](deliveries) | 外部交付登记和原始包的存放约定 | 原始包不进入 Git |
| [`ideas`](ideas) | 待讨论的产品、内容和技术想法 | 待补充 |

## 获取项目

```powershell
git clone https://github.com/deng-dylan/railcraft-demo.git
```

克隆后即可获得项目资料、历史原型快照、Unity 源码与验收证据。当前白盒在
Unity 菜单中使用 `RailCraft > Third Person Whitebox > Build Windows x86_64`
构建，本地产物入口为 `apps/railcraft-unity/Builds/Whitebox/RailCraftWhitebox.exe`。
完整运行与开发说明见 [`apps/railcraft-unity/README.md`](apps/railcraft-unity/README.md)；
Godot 历史 Demo 的说明见 [`apps/railcraft-godot/README.md`](apps/railcraft-godot/README.md)。

## 原型管理原则

- 项目主线应用放在 `apps/` 下，与需求、设计和验收资料使用同一套版本历史。
- 与主应用技术路线不同的探索版本放入 `prototypes/`，保存源快照、审核结论和采用边界。
- 二进制安装包、可重新获得的运行时和大型构建产物放在交付存储或 Release 附件；仓库只保存校验值、版本信息和必要的源文件。
- 新内容进入主 Demo 前，需要有需求来源、资料出处、许可信息和验收方式。

## 当前决策

- Unity 第三人称流程白盒是当前开发主线，入口为 `apps/railcraft-unity`。
- Unity 固定视角 v0.1 作为冻结验收基线保留，旧证据不随白盒迭代改写。
- Godot `v0.1.0-demo` 作为冻结历史 Demo 保留在 `apps/railcraft-godot`。
- GOOD2 Ren'Py 版本以独立原型资料归档，详见 [`docs/reviews/good2-renpy-v1.md`](docs/reviews/good2-renpy-v1.md)。
- 智造复兴号 Godot 4.6.3 外部 Demo 以本地隔离包装登记；源码授权待补，不进入 Git。详见 [`prototypes/high-speed-rail-factory-godot-4.6.3`](prototypes/high-speed-rail-factory-godot-4.6.3) 和 [`docs/reviews/high-speed-rail-factory-godot-4.6.3.md`](docs/reviews/high-speed-rail-factory-godot-4.6.3.md)。
- 项目总仓库结构的依据见 [`docs/decisions/0001-project-repository-layout.md`](docs/decisions/0001-project-repository-layout.md)。
- 当前主线切换的正式决策见 [`docs/decisions/0002-third-person-whitebox-mainline.md`](docs/decisions/0002-third-person-whitebox-mainline.md)。
- 日常清理与归档边界见 [`docs/MAINTENANCE.md`](docs/MAINTENANCE.md)。

## 开发批次与推送

- 每批改动围绕一个可验证目标组织，例如玩法、题库、场景、美术替换或验收证据；
  通过对应测试和构建后单独提交。
- 代码、场景、文档和由该批次产生的可复核证据放在同一批提交中；缓存、本地 Build、
  原始交付二进制和临时文件不进入提交。
- 提交前检查 `git diff` 与 `git status --short`，确认没有混入其他成员或其他批次的改动；
  提交通过后及时推送当前功能分支，避免积压多个无关批次后一次性推送。
- 冻结基线只接受可追溯的勘误或安全维护；任何新玩法进入当前 Unity 白盒主线。
