# RailCraft Unity — ThirdPerson 当前主线

> 当前批次：`v0.4.0-art-alpha.4`（白盒之后的首个受控视觉生产批次）。

Unity ThirdPerson 是当前唯一的运行时、开发和验收入口。旧 Unity 固定视角、Godot 和
Ren'Py Demo 已退休，历史说明见仓库 [归档索引](../docs/archive/README.md)。

## 当前产品

RailCraft 面向竞赛演示，模拟通用高速动车组的工厂装配与调试流程。复兴号、和谐号仅作
原创外观语言参考，当前车体和 CW-200K 转向架属于展示/通用教学资产，不承担
CR400AF 或 SWM-400E1 工程身份。

当前域模型保留 14 个子部件 ID，玩家界面收敛为 5 个材料工位和 1 个调试检验模块：轮对轴箱、构架与
制动/牵引、一系悬挂、二系悬挂、车体与落车、调试与检验。58 道题（50 道单选、8 道判断）
完整留存，核心流程精选约 18 题，题目数量不决定工位或零件数量。

## 当前批次已完成

- 工厂第三人称移动、答题、领取、分级装配、落车、教学调试、检验、复测和结算闭环；
- URP 工人角色、Idle/Walk/Run 动画、PBR 桥式起重机、维修轨道、集装箱、变电站和工业背景道具；
- Kenney Factory Kit 模块化答题工位、装配台和调试台；
- 队员车体展示 FBX、CW-200K 参考转向架和牵引拉杆视觉接入；
- 自动存档、设置、知识图鉴、中文 UI 和 Windows Player 烟测。

当前批次证据：[`Artifacts/Whitebox/ArtAlpha/acceptance-report.md`](Artifacts/Whitebox/ArtAlpha/acceptance-report.md)。

## 打开与构建

1. 使用 Unity `6000.3.21f1` 打开本目录。
2. 执行 `RailCraft > Third Person Whitebox > Rebuild Scene`。
3. 执行 `RailCraft > Third Person Whitebox > Build Windows x86_64`。
4. 构建完成后 Player 位于 `Builds/Whitebox/RailCraftWhitebox.exe`（该目录被 Git 忽略）。

场景由生成器构建。长期视觉改动应进入 `Assets/RailCraft/ThirdPerson/Editor/` 的生成器、
Prefab 或配置，避免手工修改生成场景后被覆盖。

## 关键入口

- 当前规格：[Documentation/ThirdPersonWhitebox.md](Documentation/ThirdPersonWhitebox.md)
- Art Alpha：[Documentation/ArtAlpha.md](Documentation/ArtAlpha.md)
- 资产审计：[Documentation/ArtAssetAudit.md](Documentation/ArtAssetAudit.md)
- 模型交接：[Documentation/ModelHandoff.md](Documentation/ModelHandoff.md)
- 当前发行说明：[Documentation/Release.md](Documentation/Release.md)
- 当前状态：[../docs/project/CURRENT_STATUS.md](../docs/project/CURRENT_STATUS.md)
- 当前基线：[../docs/project/CURRENT_BASELINE.md](../docs/project/CURRENT_BASELINE.md)

## 当前限制

- 部分厂房、工位和零件仍是白盒或通用示范几何；正式 LOD、碰撞、材质和纹理仍在完善。
- 未核验的车型参数、扭矩、公差、绝缘、气密和检修数值不会作为作业标准。
- 第三方角色、音效、起重机、变电站和背景道具的公开再分发范围需逐项复核。
- 目标机 1920×1080 性能、显存、加载时间和异机断网走查尚未完成。
- 未授权、盗版、破解或去水印资源不进入项目。

## 版本与验收

当前版本号、测试数量、构建哈希和截图以 Art Alpha 验收记录为准。每次变更都应同步
代码、测试、文档、状态和批次证据，具体步骤见
[`../docs/project/CHANGE_WORKFLOW.md`](../docs/project/CHANGE_WORKFLOW.md)。
