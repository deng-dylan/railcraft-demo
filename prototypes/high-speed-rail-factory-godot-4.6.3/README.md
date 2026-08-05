# 智造复兴号：高铁装配工厂 Godot 4.6.3 本地隔离包装

这是外部交付 Godot 教育游戏 Demo 的本地隔离包装，供授权核验、内部评审和后续需求讨论使用。它属于 Legacy 参考资料，不进入 RailCraft Unity 主线。

## Git 跟踪边界

Git 只跟踪本包装说明和 `.gitignore`。从原始包解出的 `source/` 目录包含代码、题目文案和程序化资产，因交付包未提供许可证、作者信息或再分发授权，整个目录已被忽略，不进入 Git 历史，也不得随公开分支或 Release 发布。

原始包与本地 `source/` 仅保存在授权明确的本机或私有存储。交付登记、校验值和授权边界见 [`../../deliveries/external/high-speed-rail-factory-godot-4.6.3/README.md`](../../deliveries/external/high-speed-rail-factory-godot-4.6.3/README.md)。既有评审结论见 [`../../docs/reviews/high-speed-rail-factory-godot-4.6.3.md`](../../docs/reviews/high-speed-rail-factory-godot-4.6.3.md)，其中涉及源码快照的描述以本页当前隔离策略为准。

## 本地内容概览

本地 `source/` 是一个 Godot 4.6.3 Forward+ 项目，包含第三人称移动与交互、6 道题、6 个零件、3 个模块、程序化厂房和列车，以及状态机烟雾测试。2026-08-05 的内部核验确认：原始 ZIP 含 80 个条目，解压得到 49 个常规文件；本地副本曾完成逐文件哈希比对。

## 验证结果

2026-08-05 在 Windows 上使用 Godot `4.6.3.stable.official.7d41c59c4` 完成了下列复验：

- `python tools/static_validate.py`：通过；数据、资源引用、场景和状态机契约均通过检查。
- `Godot --headless --editor --path . --quit --verbose`：通过；所有 16 个 GDScript 可导入。
- `Godot --headless --path . --quit-after 8 --verbose`：通过；主场景可无窗口启动。
- `Godot --headless --path . --verbose -- --smoke-test`：通过，并输出 `[SMOKE TEST] PASS`。

烟雾测试结束时 Godot 输出过 `SceneTreeTimer` 清理警告；评审已将干净退出列为待复验项。图形界面的操作手感、镜头遮挡、动画观感和多分辨率 UI 仍需人工验收。

## 运行方式

仅在本地已取得原始交付包并解压到 `source/` 后，使用 Godot 4.6.3 Standard 打开 `source/project.godot`，或在 `source/` 中执行：

```powershell
& $env:GODOT_BIN --headless --path . --verbose -- --smoke-test
```

`source/` 及其 Godot 缓存、UID 和其他派生文件均由本目录的 `.gitignore` 排除。若后续取得完整授权并决定建立可维护分支，应先完成许可证、题目来源和第三方资产审查，再通过新的、可审计的导入流程纳入源码。

## 使用边界

- 原始包未提供作者、许可证或第三方内容清单，当前内容仅限授权范围内的内部评审。
- 未经来源和许可补齐，代码、题目文案及程序化资产不得进入 Git、公开发布或产品构建。
- 可采纳的玩法方向须按 Unity 主线的数据、内容来源、许可和验收标准重新实现；此项目的 `project.godot`、自动加载和场景保持本地隔离。
