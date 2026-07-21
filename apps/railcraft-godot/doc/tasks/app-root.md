# AppRoot 最小任务清单

> 设计依据：`doc/detailed-design.md` 第 2.3、3.2、3.3、14–16、19、22 节  
> 模块目标：建立可复现工程，创建并连接全部模块，完成启动校验、主场景冒烟、CI、Windows 构建和交付集成。

## 边界

- 负责：组合根、工程设置、共享数据文件、信号连接、启动/退出、工程级质量与交付配置。
- 不负责：在组合根内实现答题、库存、安装、动画或页面业务细节。
- 前置条件：AR-001 至 AR-008 可独立执行；AR-009 之后依赖对应模块契约。

## 最小任务

- [x] **AR-001 创建工程目录和 project.godot。** 建立详细设计第 4 节目录，设置 Godot 4.6.3、Compatibility、1280×720、最小 960×540 和窗口模式。验证：headless editor 可打开空工程。
- [x] **AR-002 固定 Python 开发工具。** 创建 `.python-version`、`pyproject.toml`、`uv.lock`，固定 Python 3.12.13、uv 0.11.8、gdtoolkit 4.5.0、pre-commit 4.6.0。验证：`uv sync --frozen` 成功。
- [x] **AR-003 引入固定 GUT。** 将 GUT 9.6.0 提交到 `addons/gut/` 并创建最小测试入口。验证：headless GUT 空测试集可启动并退出。
- [x] **AR-004 创建 Q1–Q3 数据。** 在 `questions.json` 中逐字录入前三题、答案、解析、来源和奖励 ID。验证：JSON 可解析且三题与需求基线一致。
- [x] **AR-005 创建 Q4–Q6 数据。** 逐字录入中间三题并保持连续顺序。验证：JSON 可解析且三题与需求基线一致。
- [x] **AR-006 创建 Q7–Q9 数据。** 逐字录入后三题并保持连续顺序。验证：JSON 可解析且三题与需求基线一致。
- [x] **AR-007 创建零件数据。** 在 `parts.json` 中录入 9 个零件、组件、场景路径、目标、变换和单链依赖。验证：顺序与需求第 6.2 节一致。
- [x] **AR-008 创建配方数据。** 在 `recipes.json` 中录入 3 个组件和整车配方，加入牵引供电教学说明。验证：9→3→1 覆盖完整且说明含义与详细设计一致。
- [x] **AR-009 创建 Main.tscn 与 AppRoot 空壳。** 按设计建立服务、WorldRoot 和 UILayer 节点。验证：主场景 headless 加载无脚本错误。
- [x] **AR-010 创建并注入核心模块。** 在 AppRoot 中构造仓储、三个领域管理器、流程、表现模块和资产校验器，不使用 Autoload。验证：节点和对象各只有一个实例。
- [x] **AR-011 实现启动内容加载。** 调用 `ContentRepository.load_catalog()`，成功后向模块注入同一 Catalog。验证：有效数据进入 START，失败数据进入 FatalErrorView。
- [x] **AR-012 接入资产启动校验。** 内容成功后运行 `AssemblyAssetValidator`，全部通过才允许开始。验证：删除夹具目标时开始按钮不可用且可退出。
- [x] **AR-013 连接视图输入信号。** 集中连接开始、答案、进入装配、零件点击和退出到 GameFlowManager。验证：每个信号只连接一次。
- [x] **AR-014 连接流程输出信号。** 把状态、题目、反馈、装配准备、组件和结束请求连接到 ScreenCoordinator/AssemblyView。验证：记录连接表与详细设计一致。
- [x] **AR-015 连接动画请求与回调。** 流程请求进入 AnimationCoordinator，完成/失败信号回到流程。验证：回调 ID 原样传递且无环形重复调用。
- [x] **AR-016 实现启动错误与退出清理。** 致命错误显示编号，退出时取消动画并释放临时实例。验证：START、FatalError 和 END 三处退出都正常关闭。
- [x] **AR-017 完成主场景冒烟测试。** 在 headless 模式加载 Main，使用测试适配器跑通最短 9 题流程。验证：无脚本错误且最终到达 END。
- [x] **AR-018 配置格式、lint 和 pre-commit。** 固定检查范围并排除插件生成文件的合理部分。验证：全仓 `gdformat --check`、`gdlint`、pre-commit 通过。
- [x] **AR-019 创建 quality.yml。** 按设计执行 uv、格式、lint、Godot 导入、主场景加载、GUT 和测试结果上传，第三方 Action 固定版本或 SHA。验证：分支推送与 PR 工作流通过。
- [x] **AR-020 配置 Windows 导出。** 创建 `export_presets.cfg` 和 `build-windows.yml`，支持手动及 `v*` 标签生成 x64 zip；PR 由 `quality.yml` 提供质量门禁。验证：导出、PE 头、SHA-256、压缩和上传步骤齐备。
- [x] **AR-021 配置 Git 与 LFS。** 创建 `.gitignore`、`.gitattributes` 和模型 LFS 规则，不提交缓存、构建和凭据。验证：`git check-ignore` 与 `git lfs track` 输出符合设计。
- [x] **AR-022 创建私有远程仓库。** 初始化本地 `main`，在当前已认证个人账号创建私有 `railcraft-demo`，添加 `origin` 并推送。验证：仓库可见性、默认分支和远程地址正确。
- [x] **AR-023 完成 README。** 编写项目简介、界面布局预览、快速开始、体验流程、发布校验和当前状态。验证：新用户按快速开始可找到 Windows 与 Godot 运行入口。
- [x] **AR-024 完成运行说明。** 编写 `doc/running.md`，覆盖解压、启动、退出和常见问题。验证：普通用户步骤不依赖开发工具。
- [x] **AR-025 完成开发说明。** 编写 `doc/development.md`，记录固定版本、安装、验证、测试、导出和目录。验证：在新环境按命令可复现依赖。
- [x] **AR-026 完成来源清单。** 编写 `doc/sources.md`，列出 9 题来源、字体、GUT、工具和资产许可。验证：每项第三方内容都有来源和许可状态。
- [x] **AR-027 执行 Windows GUI 验收。** 在发布候选 x64 构建逐项执行详细设计第 21 节清单并记录结果。验证：窗口控制完整走过 9 题、9 次装配、3 个组件、终局、退出、最小尺寸和重启，详见 `doc/acceptance/windows-v0.1.0-demo.md`。
- [x] **AR-028 完成最终交付。** 推送最终提交，确认质量与构建 Actions 通过，并提供 Windows zip 产物。验证：源码、文档、提交历史和可运行构建均可访问。

## 模块完成标准

- [x] 所有模块仅在 AppRoot 集中创建和连接；
- [x] 启动顺序严格执行内容校验、资产校验、依赖注入和 START；
- [x] 未注册大规模全局 Autoload；
- [x] headless 主场景、GUT、静态检查和 CI 全部通过；
- [x] Windows x64 产物通过 20 项验收记录；
- [x] 私有 `railcraft-demo` 仓库、文档和构建产物完成最终交付；
- [x] 更新 [`progress.md`](progress.md) 中模块任务数和完成状态。

## 集成验证证据

- `res://scenes/main/main.tscn` 已配置为 `project.godot` 的 `run/main_scene`，包含单例 DomainServices、PresentationServices、WorldRoot 和 UILayer。
- `tests/smoke/test_main_scene.gd` 验证模块实例数、输入信号单连接、START 初始化、每题先错后对的完整 9 题流程、9 个视觉安装、END，以及 START/Fatal/END 三种退出清理。
- 固定工具链本地复验全部通过：格式、lint、pre-commit、Godot headless 导入、主场景加载、124 个 GUT 测试与 1598 个断言。
- Windows x64 导出、PE 结构、大小、SHA-256、ZIP 和导出包 headless 启动检查通过。
- Windows 候选 ZIP `85f6a2f8329d5a4e3bb9c1a15eb8d77ae56c6967aed1ab9ec13cb35c5ffc5a11`；EXE `b31703373fb99c53f085ad9e2c8a8f42474464fb7148906f315d32bfdac24b9a`。
- 2026-07-19 Windows GUI 20/20 项验收通过，10 张截图、环境、构建摘要与运行日志位于 `artifacts/acceptance/`。
- hosted Windows runner 的交互式图形步骤保留为诊断；质量与导出结果由可重复门禁决定。
- `README.md`、运行说明、开发说明、来源清单、验收记录、版本说明和 Windows 构建产物均已纳入最终交付。
