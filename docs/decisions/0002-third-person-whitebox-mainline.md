# ADR-0002：Unity 第三人称流程白盒作为当前开发主线

- 状态：已采纳并由 `baseline-2026-09-04` 修订
- 日期：2026-08-06

## 背景

仓库经历过 Godot、Unity 固定视角和 Unity ThirdPerson 三个阶段。旧入口和当前 Art Alpha
资料曾同时存在，容易让读者把历史版本当作当前开发目标。

用户当前需要在工厂第三人称场景中完成移动、答题、拾取、库存、分级装配、落车、
调试检验与投入使用，并在后续以 Blender prefab 替换白盒几何。该目标已经在 Unity
`Assets/RailCraft/ThirdPerson/` 中形成独立、可构建的流程。

## 决策

1. `railcraft-unity/Assets/RailCraft/ThirdPerson/` 是当前唯一开发主线。
2. 当前内容基线为原文实际存在的 58 道题、6 个主交互模块、14 个兼容子部件 ID、
   以及调试失败、重新调试、检验、复测和投入使用闭环。
3. 当前主线场景为 `Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`；
   Windows 本地构建入口为 `Builds/Whitebox/RailCraftWhitebox.exe`。项目默认 Build Settings
   只启用当前主场景。
4. Unity 固定视角、Godot 和其他 Demo 已退休；历史内容通过 Git tag 或本机日期归档恢复。
5. 当前 Art Alpha 证据进入 `railcraft-unity/Artifacts/Whitebox/ArtAlpha/`。
7. 每批改动围绕一个可验证目标组织。对应测试、Windows 构建和成品冒烟通过后创建
   独立提交，并及时推送当前功能分支；生成该结论的最终证据与代码同批提交。

## 结果

- 仓库只有一个当前玩法开发入口，历史实现通过提交、tag 和归档保持可追溯。
- 白盒代码不依赖最终网格层级，Blender 资产可以遵守稳定 ID、安装槽和模型交接规范
  逐步替换视觉子节点。
- 当前 Art Alpha 使用独立批次证据目录，历史验收数字不会被新批次复用。
- 旧 Demo 不再驱动仓库的产品范围定义。

## 约束

- 当前主线范围以 `railcraft-unity/Documentation/ThirdPersonWhitebox.md` 为准。
- 冻结基线只接受明确的勘误、安全维护或复现修复，且必须使用独立提交记录影响。
- 原始题库、流程图、CAD 和研究文件继续按 `deliveries/` 规则登记；未经审核的工程
  数据与临时文件不直接进入运行时资产。
- 若未来从白盒转入正式美术场景或发布版本，应新增 ADR 和版本化验收目录，不复用
  含义不同的旧路径。
