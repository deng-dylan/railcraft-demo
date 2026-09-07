# RailCraft Unity 文档导航

## 当前权威入口

- [当前基线](../../docs/project/CURRENT_BASELINE.md)：阶段、产品身份、模块规模和资产缺口。
- [当前状态](../../docs/project/CURRENT_STATUS.md)：已完成项、风险、证据和下一步。
- [约束评审](../../docs/project/CONSTRAINT_REVIEW.md)：旧 GC-01 至 GC-28 的保留、改写和移除结果。
- [变更工作流](../../docs/project/CHANGE_WORKFLOW.md)：新对话、Bug、资产批次和验收同步方法。

## 当前批次

- [ArtAlpha.md](ArtAlpha.md)：白盒之后的首个视觉生产批次。
- [ThirdPersonWhitebox.md](ThirdPersonWhitebox.md)：当前玩法流程、题库和运行时契约。
- [ArtAssetAudit.md](ArtAssetAudit.md)：资产状态、缺口和免费候选边界。
- [Release.md](Release.md)：当前运行、构建、限制和交付说明。
- [Art Alpha 验收记录](../Artifacts/Whitebox/ArtAlpha/acceptance-report.md)：测试、构建、烟测和截图。

## 模型与场景

- [ModelHandoff.md](ModelHandoff.md)：模型身份、单位、坐标、LOD、碰撞和材质门禁。
- [FinalShowcase.md](FinalShowcase.md)：完整编组展示场景（展示风格层）。
- [AssemblyDemonstrationBogie.md](AssemblyDemonstrationBogie.md)：转向架结构示范件。
- [CW200KReferenceBogie.md](CW200KReferenceBogie.md)：CW-200K 通用参考件说明。
- [FactoryKitEnvironment.md](FactoryKitEnvironment.md)：Factory Kit 资源、授权和布置规则。
- [AssemblyVariantModels.md](AssemblyVariantModels.md)：车型构型扩展登记。

## 打开与构建

- 主场景：`Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`
- 重建场景：`RailCraft > Third Person Whitebox > Rebuild Scene`
- 构建 Windows：`RailCraft > Third Person Whitebox > Build Windows x86_64`
- 本地 Player：`Builds/Whitebox/RailCraftWhitebox.exe`

生成器会重建主线场景。持久的视觉调整应进入生成器、Prefab 或配置，并在同一批次补充
测试和文档。

## 历史资料

旧 Unity 固定视角、Godot、Ren'Py 和外部 Demo 已从工作树退休。历史文件仍可从 Git tag
或本机 `.tmp/retired-demos-20260904/` 恢复，详见仓库
[归档索引](../../docs/archive/README.md)。历史 `Scope.md`、`Acceptance.md`、
`PerformanceBudget.md` 和 `ReleaseReadiness.md` 只用于追溯；与当前基线冲突时，以当前
基线和状态文件为准。
