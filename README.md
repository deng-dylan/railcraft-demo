# RailCraft 工程场景数字化项目

本仓库是团队的项目总仓库，用于管理需求、设计、调研、原型评审、交付登记和可独立开发的应用工程。

## 当前组成

| 区域 | 用途 | 状态 |
| --- | --- | --- |
| [`apps/railcraft-godot`](apps/railcraft-godot) | Godot 铁路知识与列车装配 Demo（独立 Git 子模块） | 当前可运行主 Demo |
| [`docs/project`](docs/project) | 原始需求、设计和实施任务资料 | 项目基线 |
| [`docs/reviews`](docs/reviews) | 外部 Demo、交付物与技术方案评审 | 持续更新 |
| [`docs/decisions`](docs/decisions) | 团队已确认的仓库和技术决策 | 持续更新 |
| [`prototypes`](prototypes) | 独立原型的可审计源快照与说明 | 参考用途 |
| [`deliveries`](deliveries) | 外部交付登记和原始包的存放约定 | 原始包不进入 Git |
| [`ideas`](ideas) | 待讨论的产品、内容和技术想法 | 待补充 |

## 获取项目

```powershell
git clone --recurse-submodules <项目总仓库地址>
```

已有克隆可执行：

```powershell
git submodule update --init --recursive
```

Godot Demo 的运行与开发说明见 [`apps/railcraft-godot/README.md`](apps/railcraft-godot/README.md)。

## 原型管理原则

- 能独立构建和维护的应用保留为独立 Git 仓库，通过子模块登记到项目总仓库。
- 与主应用技术路线不同的探索版本放入 `prototypes/`，保存源快照、审核结论和采用边界。
- 二进制安装包、可重新获得的运行时和大型构建产物放在交付存储或 Release 附件；仓库只保存校验值、版本信息和必要的源文件。
- 新内容进入主 Demo 前，需要有需求来源、资料出处、许可信息和验收方式。

## 当前决策

- Godot 版本是项目主线实现，继续在 `apps/railcraft-godot` 演进。
- GOOD2 Ren'Py 版本以独立原型资料归档，详见 [`docs/reviews/good2-renpy-v1.md`](docs/reviews/good2-renpy-v1.md)。
- 项目总仓库结构的依据见 [`docs/decisions/0001-project-repository-layout.md`](docs/decisions/0001-project-repository-layout.md)。
