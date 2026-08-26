# RailCraft Unity 文档导航

## 当前发行与上手

RailCraft v0.3.0-preview.1 是当前 Unity ThirdPerson 标准实训预发布。Windows x86_64 是唯一发行平台，普通玩家入口固定为标准工单 RC-EMU-01。

- [Release.md](Release.md)：发行范围、运行方式、已知限制与附件清单。
- [ThirdPersonWhitebox.md](ThirdPersonWhitebox.md)：当前流程、题库、资产替换和验收条件。
- [当前验收报告](../Artifacts/Whitebox/Acceptance/acceptance-report.md)：构建、EditMode、成品冒烟与截图。
- [ReleaseReadiness.md](ReleaseReadiness.md)：2026-08-21 候选评估快照与未完成发行工作。

## 当前源码与构建

- 场景：Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity
- 重建场景：RailCraft > Third Person Whitebox > Rebuild Scene
- 构建 Windows：RailCraft > Third Person Whitebox > Build Windows x86_64
- 本地产物：Builds/Whitebox/RailCraftWhitebox.exe

构建入口会重建标准实训场景；检测到复兴号 FBX 时，还会将完成后的 FinalShowcase 作为可选展示场景一并打包。

## 当前规格与资产

- [ModelHandoff.md](ModelHandoff.md)：模型身份、单位、坐标、LOD 和生产资产门禁。
- [FinalShowcase.md](FinalShowcase.md)：完成后的完整编组出厂展示。
- [AssemblyDemonstrationBogie.md](AssemblyDemonstrationBogie.md)：转向架结构示范件、语义映射与约束。
- [FactoryKitEnvironment.md](FactoryKitEnvironment.md)：Factory Kit 资源、授权和布置规则。
- [AssemblyVariantModels.md](AssemblyVariantModels.md)：开发者扩展车型登记，不在普通玩家入口展示。

## 归档资料

<details>
<summary><strong>冻结 Unity v0.1 与 Godot Demo（仅供复现、审计和参考）</strong></summary>

- Unity 固定视角 v0.1：Scope.md、Acceptance.md、PerformanceBudget.md 与 ../Artifacts/Acceptance/。
- Godot v0.1.0-demo：../../railcraft-godot/。
- 历史实现、原型和启动期资料统一见 ../../../docs/archive/README.md。

冻结资料只接受可追溯的勘误、安全维护或复现修复。当前玩法、题库、场景、美术替换和发行证据统一进入 ThirdPerson 主线。

</details>
