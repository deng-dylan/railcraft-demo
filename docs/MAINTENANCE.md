# RailCraft 仓库维护与文件边界

本说明用于保持 RailCraft 总仓库可审计、可构建，并避免将可再生缓存或成员原始交付误纳入版本控制。

当前开发主线是 `apps/railcraft-unity/Assets/RailCraft/ThirdPerson/`。同一 Unity
工程中的固定视角 v0.1 是冻结验收基线，`apps/railcraft-godot/` 是冻结历史
Demo。维护时必须区分三者，避免用当前白盒证据覆盖历史证据。

## 受版本控制的主线内容

| 区域 | 维护规则 |
| --- | --- |
| `apps/railcraft-unity/Assets/RailCraft/ThirdPerson/` | 当前第三人称白盒主线。跟踪源码、场景、白盒视觉、测试和稳定替换契约。 |
| `apps/railcraft-unity/` 的既有 `Bootstrap`/`Factory` | 冻结的 Unity 固定视角 v0.1 验收基线，只接受可追溯勘误或必要维护。 |
| `apps/railcraft-godot/` | 冻结的 Godot `v0.1.0-demo` 历史 Demo，保留原位以便回溯。 |
| `prototypes/` | 非主线原型的包装与说明；只有再分发权明确的源码快照可以进入 Git。 |
| `docs/project/`、`docs/decisions/`、`docs/reviews/` | 项目基线、决策和评审记录。 |
| `deliveries/*/*/README.md` | 外部资料与成员交付的清单、来源、许可和校验值。 |

## 本地可再生内容

以下目录已由 `.gitignore` 排除。关闭相应应用后可以按需清理，重新打开项目时会自动或通过构建流程生成。

| 范围 | 可再生目录 |
| --- | --- |
| Unity | `Library/`、`Temp/`、`obj/`、`Logs/`、`UserSettings/`、`TestResults/`、`Builds/` |
| 本地工作 | `.tmp/`、`tmp/`、`.agents/`、`.superpowers/` |

`Builds/Whitebox/` 是当前白盒本地运行包，`Builds/Windows/` 是冻结 v0.1 的本地
运行包；对外发布时均应作为 GitHub Release 附件或交付包处理。当前白盒证据写入
`Artifacts/Whitebox/Acceptance/`，冻结 v0.1 证据保存在 `Artifacts/Acceptance/`。
证据目录只跟踪可复核的最终日志、测试 XML、性能数据、截图和校验值，不跟踪完整
Build 目录或中间捕获。

## 交付与归档规则

- `deliveries/**/release/**` 保存未经修改的原始二进制交付，保持忽略状态；对应 README 记录来源与 SHA-256。
- 缺少许可证或再分发授权的外部源码保持本地隔离；Git 只跟踪交付登记、校验值、评审结论和隔离规则。
- `deliveries/**/review/**` 中的评审预览仅在完成审核并确认需要版本化时单独暂存，避免把候选模型误当作生产资产。
- 不使用 `git add -A`、`git clean -fdx` 或根目录递归移动/删除来整理项目；先确认具体路径、归属和可再生性。
- 需要清理大型缓存前，确认 Unity、Godot 和相关构建进程均已关闭，并保留当前可运行包或交付副本。

## 当前开发与冻结基线

- 当前第三人称白盒的规格、流程和验收条件位于
  `apps/railcraft-unity/Documentation/ThirdPersonWhitebox.md`，当前证据位于
  `apps/railcraft-unity/Artifacts/Whitebox/Acceptance/`。
- 冻结 Unity v0.1 的范围、验收条件、性能基线和最终证据位于既有
  `Documentation/Scope.md`、`Acceptance.md`、`PerformanceBudget.md` 与
  `Artifacts/Acceptance/`；这些文件的历史结论不随白盒迭代改写。
- Godot 历史 Demo 的发布、运行和验收资料保留在 `apps/railcraft-godot/`。
- 当前计划的登记规则见 `docs/project/plans/README.md`。`docs/superpowers/` 和
  `.superpowers/` 中的工具执行计划不会自动成为项目基线或进入暂存。
- 公开推送前，复核 `git status --short`，确保候选 CAD、研究原件和本地缓存不会进入提交。

## 批次提交与推送

1. 每批改动围绕一个可验收目标组织，并同步代码、场景、测试、文档和该批最终证据。
2. 运行与风险相称的自动化测试、Windows 构建和成品冒烟；结果不完整时不标记该批完成。
3. 提交前逐项检查 `git diff`、`git status --short` 和目标路径，排除缓存、完整 Build、
   原始 `deliveries/**/release/**`、临时截图和其他成员改动。
4. 每个通过验证的批次创建独立提交并及时推送当前功能分支。不要把多个无关批次压在
   一次提交或一次最终推送中，也不要为整理历史而强制推送共享分支。
5. 证据与产生它的代码保持同批；冻结基线的勘误使用独立提交，并在提交说明中标出
   影响范围和未改变的历史结论。
