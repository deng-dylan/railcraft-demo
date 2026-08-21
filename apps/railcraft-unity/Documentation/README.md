# RailCraft Unity 文档导航

本目录同时保存当前第三人称白盒文档和冻结 Unity v0.1 文档。阅读或更新前先确认
目标状态，避免把当前主线结论写入历史验收基线。

## 状态总览

| 状态 | 实现入口 | 产品范围 | 文档与证据 |
| --- | --- | --- | --- |
| 当前开发主线 | `Assets/RailCraft/ThirdPerson/` | v0.3 候选版：标准工单、58题、14零件、23步流程、存档、知识图鉴、结算与调试闭环 | [`ThirdPersonWhitebox.md`](ThirdPersonWhitebox.md)、[`../Artifacts/Whitebox/Acceptance`](../Artifacts/Whitebox/Acceptance) |
| 冻结 Unity v0.1 | `Assets/RailCraft/Scenes/Bootstrap.unity`、`Factory.unity` | 固定视角、48题、15步引导流程 | [`Scope.md`](Scope.md)、[`Acceptance.md`](Acceptance.md)、[`PerformanceBudget.md`](PerformanceBudget.md)、[`../Artifacts/Acceptance`](../Artifacts/Acceptance) |
| 冻结 Godot Demo | `../../railcraft-godot/` | 9题、9零件、3组件的历史 Demo | [`../../railcraft-godot/README.md`](../../railcraft-godot/README.md) |

## 当前主线入口

- 场景：`Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`
- 默认 Build Settings：只启用上述白盒场景；旧 v0.1 构建脚本显式使用其历史场景。
- 重建场景：`RailCraft > Third Person Whitebox > Rebuild Scene`
- 构建 Windows：`RailCraft > Third Person Whitebox > Build Windows x86_64`（自动重建白盒；检测到复兴号 FBX 时同步重建并打包 `FinalShowcase`）
- 本地产物：`Builds/Whitebox/RailCraftWhitebox.exe`
- 流程、题库、Blender 替换与验收条件：
  [`ThirdPersonWhitebox.md`](ThirdPersonWhitebox.md)
- 当前计划完成度、发行分级和后续优先级：
  [`ReleaseReadiness.md`](ReleaseReadiness.md)
- 模型身份、单位、坐标、LOD 和生产资产门禁：[`ModelHandoff.md`](ModelHandoff.md)
- 可选的完整编组出厂展示场景与 FBX 固定接入路径：
  [`FinalShowcase.md`](FinalShowcase.md)
- 组装阶段转向架结构示范件、来源转换、语义映射与防穿模约束：
  [`AssemblyDemonstrationBogie.md`](AssemblyDemonstrationBogie.md)
- 组装车间的 Kenney Factory Kit 资源、授权和布置规则：
  [`FactoryKitEnvironment.md`](FactoryKitEnvironment.md)
- 车型方案如何进入答题、拾取、装配、落车和调试流程：
  [`AssemblyVariantModels.md`](AssemblyVariantModels.md)
- 组员 CAD 候选文件、转换顺序和网格插槽：
  [`ModelCandidateIntegration.md`](ModelCandidateIntegration.md)

当前白盒证据目录包含构建日志、EditMode XML、成品冒烟日志以及转向架、落车和
最终完成截图，具体
覆盖批次以目录内验收报告为准。每批主线改动应更新与该批风险相匹配的证据；完整
Build、Unity 缓存和中间截图保持本地。

## 冻结文档边界

`Scope.md`、`Acceptance.md`、`PerformanceBudget.md` 和 `Artifacts/Acceptance/`
共同描述已经验收的固定视角 Unity v0.1。其48题、15步、镜头、拖放和性能结论只
适用于该版本。除可追溯勘误外，不用当前白盒数据回写这些历史结论。

`ModelHandoff.md` 的生产模型门禁继续对当前白盒有效，直到新的模型规范通过正式
决策替代。

## 文档更新规则

1. 每批玩法、题库、场景或美术改动先更新 `ThirdPersonWhitebox.md` 和对应测试。
2. 测试、Windows 构建与成品冒烟通过后，更新
   `Artifacts/Whitebox/Acceptance/` 中的最终证据。
3. 代码、文档、测试和证据按一个可验证目标组成独立提交，并及时推送当前功能分支。
4. 冻结文档的勘误单独提交，在说明中写清原因、证据和未改变的历史结论。
5. 当前主线状态发生变化时，通过 `docs/decisions/` 新增 ADR，不覆盖旧 ADR。
