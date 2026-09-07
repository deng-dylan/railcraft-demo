# RailCraft 工程场景数字化项目

> 当前批次：`v0.4.0-art-alpha.5`。项目已经完成白盒闭环验证，正在进行白盒后的首个受控视觉生产批次（Art Alpha）。

RailCraft 面向竞赛演示，模拟通用高速动车组在工厂中的知识确认、材料准备、子总成装配、
落车、教学调试、检验和复测流程。复兴号、和谐号只提供原创外观语言参考，当前资源不
承担具体车型工程声明。

## 从这里开始

| 需要什么 | 入口 |
| --- | --- |
| 当前产品口径、模块规模和资产缺口 | [当前基线](docs/project/CURRENT_BASELINE.md) |
| 当前完成度、风险和下一步 | [当前状态](docs/project/CURRENT_STATUS.md) |
| 旧约束逐条处理结果 | [约束评审](docs/project/CONSTRAINT_REVIEW.md) |
| 新对话、Bug 和资产批次同步方法 | [变更工作流](docs/project/CHANGE_WORKFLOW.md) |
| 运行、构建和限制 | [发行说明](railcraft-unity/Documentation/Release.md) |
| 当前流程和验收契约 | [ThirdPerson 规格](railcraft-unity/Documentation/ThirdPersonWhitebox.md) |
| Art Alpha 验收证据 | [验收记录](railcraft-unity/Artifacts/Whitebox/ArtAlpha/acceptance-report.md) |
| 仓库路径、资产和交付边界 | [仓库地图](docs/repository-map.md) |

## 当前主线

| 区域 | 职责 | 状态 |
| --- | --- | --- |
| [railcraft-unity](railcraft-unity) | Unity ThirdPerson 运行时、场景、测试和 Windows 构建 | 唯一主线 |
| [railcraft-unity/Assets/RailCraft/ThirdPerson](railcraft-unity/Assets/RailCraft/ThirdPerson) | 流程代码、视觉工厂、模型插槽和测试 | 持续迭代 |
| [railcraft-unity/Artifacts/Whitebox/ArtAlpha](railcraft-unity/Artifacts/Whitebox/ArtAlpha) | 当前 Art Alpha 的日志、截图和报告 | 当前证据 |

当前域模型保留 14 个可兼容的子部件 ID，玩家界面按 5 个材料包工位和 1 个调试检验模块组织。题库共有 58
题（50 道单选、8 道判断），核心流程使用 10 道材料包门禁题，其余题目用于备用题和知识图鉴。
题目数量不再决定工位或零件数量。

## 快速运行

1. 使用 Unity `6000.3.21f1` 打开 `railcraft-unity`。
2. 执行 `RailCraft > Third Person Whitebox > Rebuild Scene`。
3. 执行 `RailCraft > Third Person Whitebox > Build Windows x86_64`，构建完成后运行
   `railcraft-unity/Builds/Whitebox/RailCraftWhitebox.exe`。
4. 使用 WASD、Shift、鼠标、E 和 ESC 完成标准实训。

构建前会重新生成主线场景；长期视觉改动应进入生成器、Prefab 或配置。

## 当前边界

- Windows x86_64 是当前交付目标；运行时离线工作，不依赖在线下载。
- 当前流程、CW-200K 参考件和队员车体属于教学/展示层，不能写成 CR400AF 或 SWM-400E1
  正式工程模型。
- 未核验的扭矩、公差、绝缘、气密和检修数值不会作为作业标准展示。
- 公开提交前必须完成第三方许可证、署名、再分发范围、目标机性能和异机走查。
- 未授权、盗版、破解或去水印资源不进入项目。

## 历史归档

第一版白盒之前的 Godot、Ren'Py、外部 Godot 和 Unity 固定视角 Demo 已从工作树退休。
本机可回退副本位于 `.tmp/retired-demos-20260904/`（该目录被 Git 忽略），对应的
Git 历史仍可追溯。历史说明、评审结论和恢复方法见 [归档索引](docs/archive/README.md)。

## 协作规则

- 新任务先阅读 [当前状态](docs/project/CURRENT_STATUS.md) 和 [当前基线](docs/project/CURRENT_BASELINE.md)。
- 每个需求或 Bug 使用编号，并同步代码、测试、文档、状态和证据。
- 详细操作见 [变更工作流](docs/project/CHANGE_WORKFLOW.md)。
