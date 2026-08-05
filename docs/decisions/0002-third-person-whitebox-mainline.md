# ADR-0002：Unity 第三人称流程白盒作为当前开发主线

- 状态：已采纳
- 日期：2026-08-06

## 背景

仓库已经拥有三套可运行或可追溯实现：项目自有的 Godot `v0.1.0-demo`、完成正式
验收的 Unity 固定视角 v0.1，以及后续建立的 Unity 第三人称流程白盒。根 README
虽已指出 Unity 是主线，应用 README、构建入口和验收资料仍可能让读者把旧 Unity
v0.1 或 Godot Demo 当作当前开发目标。

用户当前需要在工厂第三人称场景中完成移动、答题、拾取、库存、分级装配、落车、
调试检验与投入使用，并在后续以 Blender prefab 替换白盒几何。该目标已经在 Unity
`Assets/RailCraft/ThirdPerson/` 中形成独立、可构建的流程。

## 决策

1. `apps/railcraft-unity/Assets/RailCraft/ThirdPerson/` 是当前开发主线。
2. 当前内容基线为原文实际存在的58道题、14个答题与零件拾取工位、14个零件、
   6个装配节点，以及调试失败、重新调试、检验、复测和投入使用闭环。
3. 当前主线场景为
   `Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`；Windows 本地构建
   入口为 `Builds/Whitebox/RailCraftWhitebox.exe`。项目默认 Build Settings 只启用
   当前白盒；冻结 v0.1 构建脚本继续显式打包 `Bootstrap` 与 `Factory`。
4. Unity 固定视角 v0.1 保持冻结验收基线。其 `Bootstrap`、`Factory`、48题、15步
   流程、`Builds/Windows/RailCraft.exe` 和 `Artifacts/Acceptance/` 不随当前白盒
   迭代改写。
5. `apps/railcraft-godot/` 保持冻结历史 Demo，用于复核原发布与追溯设计演进；新
   玩法不再在该目录实现。
6. 当前白盒证据进入 `apps/railcraft-unity/Artifacts/Whitebox/Acceptance/`，与旧
   Unity v0.1 证据分开维护。
7. 每批改动围绕一个可验证目标组织。对应测试、Windows 构建和成品冒烟通过后创建
   独立提交，并及时推送当前功能分支；生成该结论的最终证据与代码同批提交。

## 结果

- 仓库只有一个当前玩法开发入口，同时保留两个历史实现的完整可追溯性。
- 白盒代码不依赖最终网格层级，Blender 资产可以遵守稳定 ID、安装槽和模型交接规范
  逐步替换视觉子节点。
- 旧 Unity 与当前白盒使用独立构建和证据目录，历史验收数字不会被后续迭代覆盖。
- Godot 项目继续保留原场景、测试和发布资料，但不再驱动仓库的产品范围定义。

## 约束

- 当前主线范围以 `apps/railcraft-unity/Documentation/ThirdPersonWhitebox.md` 为准。
- 冻结基线只接受明确的勘误、安全维护或复现修复，且必须使用独立提交记录影响。
- 原始题库、流程图、CAD 和研究文件继续按 `deliveries/` 规则登记；未经审核的工程
  数据与临时文件不直接进入运行时资产。
- 若未来从白盒转入正式美术场景或发布版本，应新增 ADR 和版本化验收目录，不复用
  含义不同的旧路径。
