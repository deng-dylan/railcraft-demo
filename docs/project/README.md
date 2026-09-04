# RailCraft 项目资料

本目录同时保存历史需求和当前基线。当前任务只能把标记为“当前权威”的文件当作实施
依据；早期 Godot/固定视角资料用于追溯决策来源。

## 当前权威文件

- [CURRENT_BASELINE.md](CURRENT_BASELINE.md)：阶段、产品身份、模块数量、题库策略和资产缺口。
- [CURRENT_STATUS.md](CURRENT_STATUS.md)：版本、完成项、风险、证据和下一步。
- [CONSTRAINT_REVIEW.md](CONSTRAINT_REVIEW.md)：GC-01 至 GC-28 的逐条处理。
- [CHANGE_WORKFLOW.md](CHANGE_WORKFLOW.md)：新对话、Bug、资产批次和同步清单。
- [DECISION_PART_GRANULARITY.md](DECISION_PART_GRANULARITY.md)：材料包、子部件和核心题目的粒度决策。
- [proposal.md](proposal.md)：原始产品方向参考，具体实施以当前基线为准。

## 当前 Unity 入口

- 工程：[../../railcraft-unity](../../railcraft-unity)
- 主场景：`railcraft-unity/Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity`
- 当前规格：[../../railcraft-unity/Documentation/ThirdPersonWhitebox.md](../../railcraft-unity/Documentation/ThirdPersonWhitebox.md)
- 当前 Art Alpha 证据：[../../railcraft-unity/Artifacts/Whitebox/ArtAlpha/acceptance-report.md](../../railcraft-unity/Artifacts/Whitebox/ArtAlpha/acceptance-report.md)

## 历史资料边界

`detailed-design.md`、`prompt.md`、`tasks/` 和 `plans/archive/` 记录早期实现计划，可能写有
Godot、固定视角、9/48 题、旧路径或旧限制。第一版白盒之前的 Demo 已从工作树退休，
恢复方式见 [`../archive/README.md`](../archive/README.md)。这些文件不会自动成为当前
代码、题库、场景或发布约束。
