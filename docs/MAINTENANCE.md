# RailCraft 仓库维护与文件边界

当前开发主线是 `railcraft-unity/Assets/RailCraft/ThirdPerson/`，当前批次为
`v0.4.0-art-alpha.5`。旧 Demo 已退出工作树；维护时以
[`docs/project/CURRENT_BASELINE.md`](project/CURRENT_BASELINE.md) 和
[`docs/project/CURRENT_STATUS.md`](project/CURRENT_STATUS.md) 为准。

## 受版本控制的主线内容

| 区域 | 维护规则 |
| --- | --- |
| `railcraft-unity/Assets/RailCraft/ThirdPerson/` | 跟踪运行时、生成器、场景、视觉工厂、测试和稳定替换契约 |
| `railcraft-unity/Documentation/` | 当前规格、资产审计、模型交接、发行和验收说明 |
| `railcraft-unity/Artifacts/Whitebox/ArtAlpha/` | 当前批次可复核日志、测试 XML、截图和哈希 |
| `docs/project/` | 当前产品基线、约束评审、状态和变更工作流 |
| `docs/decisions/`、`docs/reviews/` | 决策与历史评审记录，需标明适用范围 |
| `deliveries/**/README.md` | 外部资料来源、许可、校验和隔离说明 |

## 退休与归档

第一版白盒之前的 Godot、Ren'Py、外部 Godot 和旧 Unity 固定视角目录已移出工作树。日期
归档位于本机 `.tmp/retired-demos-20260904/`，Git 历史和 tag 仍可恢复。归档内容不参与
当前构建、题库、资产审计或竞赛提交。

## 资产规则

- 模型网格、贴图和音频必须有来源、许可、哈希和用途记录。
- 资产身份分为展示风格、通用教学件、车型专用工程件；身份不明时只能留在审模区。
- 原始 CAD、未经审核的外部包和缺少再分发授权的源码保留在私有/本地存储。
- 运行时网格进入 Unity 前需记录单位、原点、轴向、LOD、碰撞、材质和纹理预算。
- 题库与零件数量相互独立；新增题目不自动新增工位或 PartId。

## 本地可再生内容

Unity 的 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/`、`Builds/`、
`ReleasePackages/`，以及根目录 `.tmp/`、`tmp/`、`TestResults/`、`Logs/` 都不应提交。
外部交付 `deliveries/**/release/**` 和根目录 `*.unitypackage`、`*.zip` 同样保持忽略。

清理前关闭 Unity、相关 Player 和资产处理程序；先核对具体路径与当前证据，再采取可恢复
的移动或删除操作。不要使用 `git clean -fdx`、强制重置或根目录递归删除。

## 批次同步

每批改动围绕一个可验证目标组织，并同步：

1. 代码/场景/资产；
2. 针对性测试；
3. 当前文档与来源清单；
4. `CURRENT_STATUS.md`、必要时的基线和约束评审；
5. 同批次的测试、构建、烟测和截图证据。

提交前运行 `git diff --check`、`git status --short --untracked-files=all` 和
`tools/Test-RepositoryLayout.ps1`，确认没有缓存、原始交付或其他成员改动混入。
