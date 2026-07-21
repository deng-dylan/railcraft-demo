# GOOD2 Ren'Py 原型快照

这是团队成员交付的 GOOD2 1.0 PC 包中可阅读脚本的审计快照，用于产品比较、知识内容核验和后续需求讨论。

## 内容

`source/game/` 保留以下原始脚本：

- `script.rpy`：10 道题、收集进度、积分和外部 JSON 触发逻辑。
- `gui.rpy`、`options.rpy`、`screens.rpy`：Ren'Py 界面与运行设置。
- `script_version.txt`：脚本版本标记。

## 边界

- 此目录不包含 Ren'Py 运行时、可执行文件、缓存、字节码或默认 GUI 图片。
- `SourceHanSansLite.ttf` 未随快照纳入，原因是交付包未提供可核验的字体许可材料。
- 快照用于阅读和审查；要获得可运行版本，请从交付归档取原始包，或在许可和作者信息完善后以 Ren'Py SDK 重建项目。
- 对主 Demo 有价值的想法需要在 Godot 工程中重新实现，并补齐题目来源、资源许可和自动化验证。

完整结论见 [`../../docs/reviews/good2-renpy-v1.md`](../../docs/reviews/good2-renpy-v1.md) 和 [`../../docs/reviews/good2-renpy-v1.addendum.md`](../../docs/reviews/good2-renpy-v1.addendum.md)。
