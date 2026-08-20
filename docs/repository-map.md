# RailCraft 仓库地图

本页只回答两件事：文件该放哪里，以及哪些内容可以提交到 Git。

## 当前主线

| 路径 | 内容 | 是否继续迭代 |
| --- | --- | --- |
| `apps/railcraft-unity/` | Unity 第三人称流程白盒当前主线 | 是 |
| `apps/railcraft-unity/Assets/RailCraft/ThirdPerson/` | 当前主线的玩法代码、场景、测试、白盒视觉和模型插槽 | 是 |
| `apps/railcraft-unity/Artifacts/Whitebox/Acceptance/` | 当前主线的构建、测试、冒烟和截图证据 | 是 |

## 冻结基线

| 路径 | 内容 | 说明 |
| --- | --- | --- |
| `apps/railcraft-unity/Assets/RailCraft/Scenes/` | Unity 固定视角 v0.1 场景 | 仅保留回归和勘误 |
| `apps/railcraft-unity/Artifacts/Acceptance/` | Unity 固定视角 v0.1 验收证据 | 不用当前白盒结果覆盖 |
| `apps/railcraft-godot/` | Godot `v0.1.0-demo` 历史 Demo | 冻结保留 |

## 项目资料

| 路径 | 内容 |
| --- | --- |
| `docs/decisions/` | 仓库结构、主线切换等 ADR |
| `docs/reviews/` | 外部 Demo、模型候选和技术方案评审 |
| `docs/project/` | 项目启动期资料、设计和历史计划 |
| `docs/MAINTENANCE.md` | 仓库清理、缓存和交付边界 |
| `README.md` | 总仓库入口 |

## 原型与交付

| 路径 | 内容 | Git 策略 |
| --- | --- | --- |
| `prototypes/` | 独立原型源快照与说明 | 仅在可再分发时入库 |
| `deliveries/**/release/**` | 原始交付包、外部二进制、成员原件 | 默认忽略 |
| `deliveries/**/README.md` | 来源、许可、校验值、审核记录 | 跟踪 |

## 模型与 CAD 放置规则

| 类型 | 放置位置 | 说明 |
| --- | --- | --- |
| 可运行 Unity 网格 | `apps/railcraft-unity/Assets/.../Art/Models/` | `.fbx`、`.glb` 等按 LFS 跟踪 |
| CAD 候选登记 | `apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Art/Models/SourceCAD/` | 只放清单、备注、占位说明 |
| 待接入玩法的方案模型插槽 | `apps/railcraft-unity/Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/` | 先放 README/占位，再补正式网格 |
| 外部原始 STEP/SLDPRT/SLDASM | 队员共享目录或私有交付存储 | 不直接进运行时仓库 |

当前仓库已经把 `*.fbx`、`*.blend`、`*.step`、`*.glb`、`*.obj`、`*.stl` 等大模型类型交给 Git LFS。

## 本地可再生内容

这些目录不应进入提交，删掉后可重新生成：

- 根目录：`.agents/`、`.superpowers/`、`.tmp/`、`tmp/`、`Logs/`、`TestResults/`
- Unity：`Library/`、`Temp/`、`Logs/`、`UserSettings/`、`TestResults/`、`Builds/`
- Godot：`.godot/`、`.godot-user/`、`.uv-cache/`、`.uv-python/`、`.venv/`、`builds/`、`artifacts/*`

## 提交前检查

1. `git status --short`
2. 只暂存本批次路径，不把缓存、Build、原始交付包带进去
3. 如果涉及模型，确认提交的是 Unity 网格或占位说明，不是未经处理的 CAD 原件
4. 如果涉及验收，确认代码、场景、文档和最终证据属于同一批次
