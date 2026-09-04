# RailCraft 变更与新对话工作流

本页用于让每个需求、Bug 或美术批次快速恢复同一份项目口径。

## 新对话开始时

按顺序阅读：

1. `docs/project/CURRENT_STATUS.md`
2. `docs/project/CURRENT_BASELINE.md`
3. `docs/project/CONSTRAINT_REVIEW.md`
4. 与任务直接相关的 `railcraft-unity/Documentation/*`、代码和测试

随后执行：

```powershell
git branch --show-current
git status --short --untracked-files=all
git log -1 --oneline
```

归档目录、旧验收报告和历史聊天只用于追溯。若引用另一项 Codex 任务，先读取该任务的
完整摘要和最近结果，再把结论写回当前状态文件。

## 需求/bug 编号

每一项变更使用一个短编号，例如：

- `ART-001`：环境/模型/材质批次；
- `FLOW-001`：流程和状态机；
- `QUIZ-001`：题库和知识内容；
- `DOC-001`：基线、约束和交付文档；
- `BUG-001`：可复现缺陷。

编号写入提交说明、状态文件和对应测试名。一个对话可以处理多个连续小改动，但每个改动
都要有清楚的验收结果。

## 实施前检查

- 确认改动属于当前主线，排除旧 Demo 和归档资料；
- 查看 `git status`，保留其他成员已有修改；
- 涉及模型时确认身份层级、来源、许可、单位、原点、轴向和目标面数；
- 涉及题目时确认原件 ID、解析、主题、模块和审核状态；
- 涉及场景时确认改动进入生成器、Prefab 或配置，不只改生成结果。

## 实施后同步清单

1. 代码/场景/资产完成修改；
2. 添加或更新针对性测试；
3. 更新相关 `Documentation/*.md`；
4. 更新 `CURRENT_STATUS.md` 的已完成、风险和下一步；
5. 若改变范围或约束，更新 `CURRENT_BASELINE.md` 和 `CONSTRAINT_REVIEW.md`；
6. 运行与风险相称的 EditMode、构建和 Player 烟测；
7. 把日志、截图、哈希放入对应批次证据目录；
8. 检查 `git diff --check` 与 `git status`，确认没有缓存、构建包或原始交付物混入。

## 每个 Bug 对话的最小报告

```text
编号：BUG-xxx
现象：
复现步骤：
期望行为：
实际行为：
涉及场景/资产/题目版本：
修复文件：
回归测试：
证据路径：
剩余风险：
```

## 每个资产批次的最小报告

```text
编号：ART-xxx
资产身份：展示风格 / 通用教学件 / 车型专用工程件
来源与许可：
源文件/哈希：
单位、原点、轴向：
LOD0/LOD1/LOD2：
碰撞体：
材质与纹理：
接入工厂或 Prefab：
验证结果：
```

## 对话结束前

最后一条状态消息要能让下一项任务直接继续：写明完成了什么、证据在哪里、当前阻塞、
下一步推荐，以及是否需要用户提供来源、许可或工程资料。不要只写“已修改文件”。
