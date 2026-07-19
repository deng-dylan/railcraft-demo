# RailCraft Demo

RailCraft Demo 是一个使用 Godot 4.6.3 制作的铁路知识答题与列车装配玩法原型。玩家依次完成 9 道铁路知识单选题，每题答对后获得一个零件，并在固定 3D 视角中完成一次点击吸附装配。9 个零件组成 3 个系统级组件，最终形成一列原创通用高速电力动车组教学模型。

## 当前状态

当前功能分支已包含：

- 数据驱动的 9 道题、9 个零件、3 个组件和整车配方；
- 错误重试、正确答案解析及来源显示；
- 答题、奖励、顺序安装和完整状态机；
- 9 个可替换的 Godot 原生低多边形占位零件；
- 零件吸附、组件反馈和整车完成动画；
- 开始页、答题页、装配 HUD、组件覆盖层、结束页和致命错误页；
- 内容校验、资产契约校验、单元测试、集成测试和主场景冒烟测试；
- 固定版本的格式检查、lint、GitHub Actions 与 Windows x64 导出配置。

完整交付状态以 [`doc/tasks/progress.md`](doc/tasks/progress.md) 和 GitHub Actions 为准。

## 快速开始

### 使用 Godot 编辑器

1. 安装 Godot Standard `4.6.3-stable`。
2. 使用 Godot 导入仓库根目录中的 `project.godot`。
3. 运行项目。主场景为 `res://scenes/main/main.tscn`。

### 使用 Windows 构建

发布版本解压后运行：

```text
RailCraft-Demo.exe
```

程序完全离线运行，不需要账号、网络、数据库或额外运行时。详细步骤见 [`doc/running.md`](doc/running.md)。

## 体验流程

```text
开始页面
→ 选择一道题的答案
→ 回答错误时保留原题并继续作答
→ 回答正确后查看解析与来源
→ 进入装配
→ 点击新获得的零件并播放吸附动画
→ 每 3 个零件完成一个系统级组件
→ 第 9 个零件后播放整车完成动画
→ 完成页面
```

体验过程中没有失败结局。当前题目必须答对后才能继续。

## 开发验证

项目固定使用：

- Godot Standard `4.6.3-stable`；
- GUT `9.6.0`；
- Python `3.12.13`；
- uv `0.11.8`；
- gdtoolkit `4.5.0`；
- pre-commit `4.6.0`。

常用验证命令：

```text
uv sync --frozen
uv run --frozen gdformat --check scripts tests
uv run --frozen gdlint scripts tests
uv run --frozen pre-commit run --all-files
```

Godot headless、GUT 和 Windows 导出命令见 [`doc/development.md`](doc/development.md)。

## 主要目录

```text
data/                 题目、零件和配方数据
scenes/main/          主场景
scenes/assembly/      装配视图和 PartActor 基础场景
scenes/train/         列车根节点及 9 个零件场景
scenes/ui/            界面组合场景
scripts/domain/       答题、库存和装配领域逻辑
scripts/flow/         游戏流程状态机
scripts/infrastructure/ 内容和资产校验
scripts/presentation/ UI、3D 表现和动画协调
tests/                单元、集成、夹具和冒烟测试
doc/                  需求、设计、运行、开发和来源文档
```

## 内容与许可

题库来源、字体、测试框架、开发工具和资产许可记录在 [`doc/sources.md`](doc/sources.md)。项目不使用真实动车组 Logo、企业标志、车型编号或受限涂装。

## 范围限制

当前版本用于玩法与工程结构验证。它不包含账号、服务器、存档、排行榜、多人、自由镜头、拖拽物理、暂停、跳题、重开和音频，也不代表特定真实动车组型号的精确结构。
