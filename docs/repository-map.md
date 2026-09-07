# RailCraft 仓库地图

本页说明当前主线资产、文件放置位置和 Git 提交边界。当前开发入口只有 Unity
ThirdPerson；旧 Demo 已退休，历史恢复方式见 [归档索引](archive/README.md)。

## 当前主线

| 路径 | 内容 | 是否继续迭代 |
| --- | --- | --- |
| `railcraft-unity/` | Unity 工程、ThirdPerson 运行时和构建入口 | 是 |
| `railcraft-unity/Assets/RailCraft/ThirdPerson/` | 流程代码、场景、视觉工厂、模型插槽和测试 | 是 |
| `railcraft-unity/Artifacts/Whitebox/ArtAlpha/` | 当前 Art Alpha 日志、截图和验收报告 | 是 |
| `railcraft-unity/Documentation/` | 当前规格、资产审计、发行和模型门禁 | 是 |
| `docs/project/CURRENT_*.md` | 当前产品、状态、约束和工作流基线 | 是 |
| `docs/decisions/` | ADR 与范围决策 | 按需更新 |
| `deliveries/` | 外部资料登记、来源、许可和校验值 | 按需更新 |

## 模型与资产放置

| 类型 | 放置位置 | 说明 |
| --- | --- | --- |
| 可运行 Unity 网格 | `railcraft-unity/Assets/RailCraft/ThirdPerson/Art/Models/` | FBX、GLB 等，需有来源与导入记录 |
| CAD 候选登记 | `.../ThirdPerson/Art/Models/SourceCAD/` | 清单、备注和占位说明；原始 CAD 不直接进运行时 |
| 车型/构型候选 | `.../ThirdPerson/Art/Models/VariantModels/` | 先登记身份和门禁，再接入场景 |
| 第三方运行时子集 | `.../ThirdPerson/Art/ThirdParty/` | 只保留已筛选资源、来源文本和许可证据 |
| 原始外部交付 | `deliveries/**/release/` 或私有存储 | 被 Git 忽略，不作为运行时输入 |

资产身份必须写明：`展示风格`、`通用教学件` 或 `车型专用工程件`。后者需要责任人、
来源、单位、尺寸、接口、LOD、碰撞、材质和授权全部核验。

## 本地可再生内容

- 根目录：`.tmp/`、`tmp/`、`.agents/`、`.superpowers/`、`Logs/`、`TestResults/`
- Unity：`Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/`、`Builds/`、`ReleasePackages/`
- 外部原始包：根目录 `*.unitypackage`、`*.zip`，以及 `deliveries/**/release/**`

删除前先确认应用关闭，并保留当前批次证据。不要使用宽泛递归删除。

## 已退休目录

以下目录已从工作树移除：

- `apps/railcraft-godot/`
- `prototypes/good2-renpy/`
- `prototypes/high-speed-rail-factory-godot-4.6.3/`
- `railcraft-unity/Assets/RailCraft/{Art,Content,Editor,Input,Scenes,Scripts,Tests}/`
- `railcraft-unity/Artifacts/Acceptance/`

本机日期归档在 `.tmp/retired-demos-20260904/`，Git tag 和历史提交仍可恢复。归档不进入
当前构建、提交包或竞赛材料。

## 提交前检查

1. `git status --short --untracked-files=all`
2. `git diff --check`
3. 只暂存当前批次路径，排除缓存、Build、原始交付包和临时截图。
4. 运行 `tools/Test-RepositoryLayout.ps1`。
5. 涉及 Unity 时，确认代码、场景、测试、文档和 Art Alpha 证据来自同一批次。
