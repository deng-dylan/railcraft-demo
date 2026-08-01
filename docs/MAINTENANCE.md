# RailCraft 仓库维护与文件边界

本说明用于保持 RailCraft 总仓库可审计、可构建，并避免将可再生缓存或成员原始交付误纳入版本控制。

## 受版本控制的主线内容

| 区域 | 维护规则 |
| --- | --- |
| `apps/railcraft-unity/` | 当前 v0.1 主线。跟踪源码、场景、正式美术、Packages、ProjectSettings、测试、产品文档和 `Artifacts/Acceptance/`。 |
| `apps/railcraft-godot/` | 历史 Demo 与参考实现，保留原位以便回溯。 |
| `prototypes/` | 非主线原型的可审计快照与说明。 |
| `docs/project/`、`docs/decisions/`、`docs/reviews/` | 项目基线、决策和评审记录。 |
| `deliveries/*/*/README.md` | 外部资料与成员交付的清单、来源、许可和校验值。 |

## 本地可再生内容

以下目录已由 `.gitignore` 排除。关闭相应应用后可以按需清理，重新打开项目时会自动或通过构建流程生成。

| 范围 | 可再生目录 |
| --- | --- |
| Unity | `Library/`、`Temp/`、`obj/`、`Logs/`、`UserSettings/`、`TestResults/`、`Builds/` |
| 本地工作 | `.tmp/`、`tmp/`、`.agents/`、`.superpowers/` |

`Builds/Windows/` 是本地验收运行包；对外发布时应作为 GitHub Release 附件或交付包处理。`Artifacts/Acceptance/` 保存可复核的日志、测试 XML、性能数据、截图和校验值，属于主线证据，必须继续跟踪。

## 交付与归档规则

- `deliveries/**/release/**` 保存未经修改的原始二进制交付，保持忽略状态；对应 README 记录来源与 SHA-256。
- `deliveries/**/review/**` 中的评审预览仅在完成审核并确认需要版本化时单独暂存，避免把候选模型误当作生产资产。
- 不使用 `git add -A`、`git clean -fdx` 或根目录递归移动/删除来整理项目；先确认具体路径、归属和可再生性。
- 需要清理大型缓存前，确认 Unity、Godot 和相关构建进程均已关闭，并保留当前可运行包或交付副本。

## 当前 v0.1 基线

- Unity v0.1 的范围、验收条件和最终证据位于 `apps/railcraft-unity/Documentation/` 与 `apps/railcraft-unity/Artifacts/Acceptance/`。
- `docs/superpowers/` 保存本地执行计划，不随本说明自动迁移或暂存。
- 公开推送前，复核 `git status --short`，确保候选 CAD、研究原件和本地缓存不会进入提交。
