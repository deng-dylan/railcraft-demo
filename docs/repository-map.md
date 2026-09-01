# RailCraft 仓库地图

本页说明当前发行资产、文件放置位置和 Git 提交边界。

## 当前发行与主线

| 路径 | 内容 | 是否继续迭代 |
| --- | --- | --- |
| railcraft-unity/ | Unity ThirdPerson 标准实训当前主线 | 是 |
| railcraft-unity/Assets/RailCraft/ThirdPerson/ | 当前玩法代码、场景、测试、白盒视觉和模型插槽 | 是 |
| railcraft-unity/Artifacts/Whitebox/Acceptance/ | 当前构建、测试、冒烟和截图证据 | 是 |
| railcraft-unity/Documentation/Release.md | 当前预发布的运行、限制与交付说明 | 是 |

## 项目资料与交付

| 路径 | 内容 |
| --- | --- |
| docs/decisions/ | 仓库结构、主线切换等 ADR |
| docs/reviews/ | 外部 Demo、模型候选和技术方案评审 |
| docs/MAINTENANCE.md | 仓库清理、缓存和交付边界 |
| deliveries/ | 外部交付登记、来源、许可、校验与审核记录 |
| README.md | 当前发行入口 |

## 模型与 CAD 放置规则

| 类型 | 放置位置 | 说明 |
| --- | --- | --- |
| 可运行 Unity 网格 | railcraft-unity/Assets/.../Art/Models/ | FBX、GLB 等按 LFS 跟踪 |
| CAD 候选登记 | railcraft-unity/Assets/RailCraft/ThirdPerson/Art/Models/SourceCAD/ | 只放清单、备注、占位说明 |
| 待接入玩法的方案模型插槽 | railcraft-unity/Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/ | 先放 README 或占位，再补正式网格 |
| 外部原始 STEP、SLDPRT、SLDASM | 队员共享目录或私有交付存储 | 不直接进入运行时仓库 |

当前仓库已把 FBX、Blend、STEP、GLB、OBJ、STL 等大模型类型交给 Git LFS。

## 本地可再生内容

下列目录不应进入提交，删掉后可重新生成：

- 根目录：.agents/、.superpowers/、.tmp/、tmp/、Logs/、TestResults/
- Unity：Library/、Temp/、Logs/、UserSettings/、TestResults/、Builds/、ReleasePackages/
- Godot：.godot/、.godot-user/、.uv-cache/、.uv-python/、.venv/、builds/、artifacts/*

## 提交前检查

1. git status --short
2. 只暂存本批次路径，不把缓存、Build、原始交付包带进去。
3. 涉及模型时，确认提交的是 Unity 网格或占位说明，不是未经处理的 CAD 原件。
4. 涉及发行时，确认代码、场景、文档、校验文件与最终证据来自同一批次。

<details>
<summary><strong>归档路径（不纳入当前发行）</strong></summary>

| 路径 | 内容 | 说明 |
| --- | --- | --- |
| railcraft-unity/Assets/RailCraft/Scenes/ | Unity 固定视角 v0.1 场景 | 仅保留回归和勘误 |
| railcraft-unity/Artifacts/Acceptance/ | Unity 固定视角 v0.1 验收证据 | 不被当前白盒结果覆盖 |
| apps/railcraft-godot/ | Godot v0.1.0-demo 历史 Demo | 冻结保留 |
| prototypes/ | 独立原型源快照与说明 | 参考用途 |
| docs/project/ | 项目启动期资料、设计和历史计划 | 历史语境 |

</details>
