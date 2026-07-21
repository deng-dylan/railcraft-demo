# Windows v0.1.0-demo 验收记录

- 验收日期：2026-07-19
- 验收对象：本仓库 `feat/release-integration` 发布候选
- 运行环境：Windows NT 10.0.26200.0 x64、NVIDIA GeForce RTX 5070 Ti Laptop GPU
- 引擎：Godot 4.6.3-stable
- 自动化结果：13 个测试脚本、124 个测试、1598 个断言全部通过
- GUI 方法：在导出的 `RailCraft-Demo.exe` 上通过 Windows 窗口控制逐步点击、观察并截图

## 结论

详细设计第 21 节的 20 项检查全部通过。验收完整走过“开始 → 错误重试 → 9 题正确反馈 → 9 次装配 → 3 个组件完成页 → 整车终局 → 退出”，随后尝试缩小窗口并重新启动。三个运行日志均未出现脚本错误、警告或崩溃信息；自动化主流程另覆盖每题先错后对。

## 20 项验收结果

| # | 检查项 | 结果 | 主要证据 |
|---:|---|---|---|
| 1 | Windows 10/11 x64 解压并启动 | 通过 | 导出包 headless 启动退出码 0；真实窗口启动成功 |
| 2 | 默认 1280×720，可调整且不小于 960×540 | 通过 | `AppRoot.MINIMUM_WINDOW_SIZE`、双分辨率测试；GUI 缩小拖动后窗口未继续收缩 |
| 3 | 开始页标题、说明、开始与退出按钮正确 | 通过 | [`01-start.png`](../../artifacts/acceptance/screenshots/01-start.png) |
| 4 | 中文无乱码、缺字或方框 | 通过 | 内置 Noto Sans SC；10 张实际运行截图 |
| 5 | 9 道题顺序和文案符合基线 | 通过 | 完整 GUI 路径；`expected_question_baseline.json` 逐字段回归 |
| 6 | 错误答案后保留原题并可重试 | 通过 | [`02-wrong-feedback.png`](../../artifacts/acceptance/screenshots/02-wrong-feedback.png) |
| 7 | 正确后显示解析、来源机构、标题和 URL | 通过 | [`03-correct-feedback-source.png`](../../artifacts/acceptance/screenshots/03-correct-feedback-source.png) |
| 8 | 每题只发放一个对应零件 | 通过 | 完整 GUI 路径、库存幂等测试和流程断言 |
| 9 | 装配位置发光提示易理解 | 通过 | [`04-assembly-highlight.png`](../../artifacts/acceptance/screenshots/04-assembly-highlight.png) |
| 10 | 单击零件后自动移动、旋转和吸附 | 通过 | 9 次实际点击；0.15/0.60/0.15 秒阶段测试 |
| 11 | 动画期间重复点击不重复安装 | 通过 | 交互锁、安装幂等和重复输入测试 |
| 12 | 第 3、6、9 个零件后显示组件反馈 | 通过 | [`05-component-1.png`](../../artifacts/acceptance/screenshots/05-component-1.png)、[`06-component-2.png`](../../artifacts/acceptance/screenshots/06-component-2.png)、[`07-component-3.png`](../../artifacts/acceptance/screenshots/07-component-3.png) |
| 13 | 第 9 个零件后播放第三组件与整车动画 | 通过 | 实际终局路径、动画时序与终态测试 |
| 14 | 最终列车可辨认为原创通用高速动车组 | 通过 | [`09-end.png`](../../artifacts/acceptance/screenshots/09-end.png) |
| 15 | 最终动画包含组件强调、车灯、受电弓和车轮反馈 | 通过 | [`08-final-animation.png`](../../artifacts/acceptance/screenshots/08-final-animation.png)；最终节点状态测试 |
| 16 | 完成页显示祝贺文字和退出按钮 | 通过 | [`09-end.png`](../../artifacts/acceptance/screenshots/09-end.png) |
| 17 | 开始页与完成页退出均能关闭程序 | 通过 | 终局退出后窗口列表为空；退出/清理测试 |
| 18 | 全流程无需网络 | 通过 | 题库、字体、场景和逻辑均随包提供 |
| 19 | 无明显卡顿、穿模、闪烁或控制台错误 | 通过 | 完整 GUI 路径；三个 GUI 日志仅含引擎与图形设备信息 |
| 20 | 关闭重开后从开始页进入，无旧进度 | 通过 | [`10-restart.png`](../../artifacts/acceptance/screenshots/10-restart.png) |

## 自动化与构建摘要

- `uv sync --frozen`：通过；
- `gdformat --check scripts tests`：47 个文件通过；
- `gdlint scripts tests`：通过；
- `pre-commit run --all-files`：通过；
- Godot headless 导入及主场景加载：通过；
- GUT：124/124 通过，1598 个断言；
- Windows x64 导出：通过；
- 导出包 headless 启动：退出码 0；
- `RailCraft-Demo.exe`：118069960 字节，SHA-256 `b31703373fb99c53f085ad9e2c8a8f42474464fb7148906f315d32bfdac24b9a`；
- `RailCraft-Demo-windows-x64.zip`：49498195 字节，SHA-256 `85f6a2f8329d5a4e3bb9c1a15eb8d77ae56c6967aed1ab9ec13cb35c5ffc5a11`。

原始证据位于 [`artifacts/acceptance`](../../artifacts/acceptance/)。GitHub Actions 与 Release 的最终链接在远端整理完成后以仓库页面为准。

## 已知限制

- 当前模型是可替换的原创低多边形教学资产，不对应特定真实车型的精确结构。
- 首版不包含存档、音频、账号、网络服务、自由镜头、多人或排行榜。
- GitHub hosted Windows runner 的交互式桌面和图形驱动可能波动，真实 Windows GUI 证据随仓库保存，CI 负责可重复的质量与导出门禁。
