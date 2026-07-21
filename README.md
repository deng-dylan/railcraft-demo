# RailCraft 工程场景数字化项目

本仓库是 RailCraft 项目的总仓库，统一管理需求、设计、调研、原型评审、交付登记，以及可直接构建和运行的应用源码。

## 当前组成

| 区域 | 用途 | 状态 |
| --- | --- | --- |
| [`apps/railcraft-godot`](apps/railcraft-godot) | Godot 铁路知识与列车装配应用源码 | 当前可运行主线 |
| [`docs/project`](docs/project) | 原始需求、设计和实施任务资料 | 项目基线 |
| [`docs/reviews`](docs/reviews) | 外部 Demo、交付物与技术方案评审 | 持续更新 |
| [`docs/decisions`](docs/decisions) | 团队已确认的仓库和技术决策 | 持续更新 |
| [`prototypes`](prototypes) | 独立原型的可审计源快照与说明 | 参考用途 |
| [`deliveries`](deliveries) | 外部交付登记和原始包的存放约定 | 原始包不进入 Git |
| [`ideas`](ideas) | 待讨论的产品、内容和技术想法 | 待补充 |

## 获取项目

```powershell
git clone https://github.com/deng-dylan/railcraft-demo.git
```

克隆后即可获得项目资料、原型快照和完整 Godot 工程。Godot 应用的运行与开发说明见 [`apps/railcraft-godot/README.md`](apps/railcraft-godot/README.md)。

## 原型管理原则

- 项目主线应用放在 `apps/` 下，与需求、设计和验收资料使用同一套版本历史。
- 与主应用技术路线不同的探索版本放入 `prototypes/`，保存源快照、审核结论和采用边界。
- 二进制安装包、可重新获得的运行时和大型构建产物放在交付存储或 Release 附件；仓库只保存校验值、版本信息和必要的源文件。
- 新内容进入主 Demo 前，需要有需求来源、资料出处、许可信息和验收方式。

## 当前决策

- Godot 版本是项目主线实现，源码直接在 `apps/railcraft-godot` 演进。
- GOOD2 Ren'Py 版本以独立原型资料归档，详见 [`docs/reviews/good2-renpy-v1.md`](docs/reviews/good2-renpy-v1.md)。
- 项目总仓库结构的依据见 [`docs/decisions/0001-project-repository-layout.md`](docs/decisions/0001-project-repository-layout.md)。
