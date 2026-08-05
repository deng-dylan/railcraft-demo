# 智造复兴号：高铁装配工厂 Godot 4.6.3 Demo 评审

评审对象：`../../deliveries/external/high-speed-rail-factory-godot-4.6.3/release/high_speed_rail_factory_godot_4_6_3.zip`
评审日期：2026-08-05
结论：以本地隔离包装登记在 [`../../prototypes/high-speed-rail-factory-godot-4.6.3/`](../../prototypes/high-speed-rail-factory-godot-4.6.3/)。Git 只保存包装、交付清单和本评审；缺少再分发授权的源码保持本地隔离，不接入 Unity 主线，也不覆盖历史 Godot Demo。

## 包与快照核验

| 项目 | 结果 |
| --- | --- |
| 原始包 SHA-256 | `4D93D1979AC7918E84CAD6570C4201C8849BDA94FA4A582121066F1A5750ED34` |
| 原始包大小 | 62,172 字节 |
| ZIP 安全检查 | 80 个条目，无绝对路径、`..` 路径穿越或重复路径 |
| 解压内容 | 49 个 UTF-8 文本文件，共 121,865 字节；无二进制模型、纹理、音频或可执行运行时 |
| 本地核验副本 | `prototypes/.../source/` 的 49 个文件与原始包逐文件哈希一致；该目录不进入 Git |
| 授权材料 | 未发现 `LICENSE`、`NOTICE`、作者信息或第三方内容清单 |

包的 UTF-8 顶层目录没有设置 ZIP UTF-8 标记，部分工具会显示乱码目录名。导入目录使用 ASCII 名称，项目内部相对资源路径保持原样。

## 功能与架构概览

该 Demo 是 Godot 4.6.3 Forward+ 的第三人称 3D 教学垂直切片：玩家使用 WASD、鼠标和 E 键在程序化厂房中完成零件问答、拾取、工位安装、模块运输和整车总装。核心链路为 6 道四选一题 → 6 个零件 → 3 个模块 → 1 条整车装配流程。

`DataRepository` 从 JSON 加载题目、零件和模块；`GameStateManager` 统一维护零件、模块与整车状态；`Interactable` 为零件箱、工位与总装台提供交互协议。`SmokeTestRunner` 覆盖答错重试、单件携带、错误工位拒绝、模块与总装防重复、正确率及重置。

## 实际验证

使用仓库已有的 Godot `4.6.3.stable.official.7d41c59c4` Windows Console 运行时，结果如下：

| 检查 | 命令要点 | 结果 |
| --- | --- | --- |
| 结构校验 | `python tools/static_validate.py` | 退出码 0，`PASS`；检查 16 个 GDScript、数据契约、资源图、场景与状态机 |
| 编辑器导入 | `--headless --editor --path . --quit --verbose` | 退出码 0；16 个 GDScript 和全部主场景依赖均完成加载 |
| 无窗口启动 | `--headless --path . --quit-after 8 --verbose` | 退出码 0；`DataRepository` 输出 6 个零件、3 个模块、6 道题加载成功 |
| 状态机烟雾测试 | `--headless --path . --verbose -- --smoke-test` | 退出码 0；输出 `[SMOKE TEST] PASS` |

运行期间有两类需区分的输出：

- Windows 运行环境出现根证书存储读取失败。该信息发生在 Godot 平台初始化阶段，且未影响项目导入、启动或测试；当前不归因于 Demo 源码。
- 烟雾测试在立即退出时出现 `SceneTreeTimer` 泄漏警告。`source/scripts/main/Main.gd` 的完成页等待计时器与 `source/scripts/assembly/FinalAssembler.gd` 的动画等待都使用 `create_timer()`；当前自动化链路功能通过，干净退出日志列为条件通过。

后续复验应在正常完成后等待至少 3 秒再退出，并覆盖总装/结算等待期间的重置和退出，确认计时器警告是否消失及是否存在可见副作用。图形界面、镜头遮挡、输入手感、动画和多分辨率 UI 尚未完成人工验收。

## 后续技术风险

- `source/scripts/core/DataRepository.gd` 以运行时读取的裸 JSON 数组构建索引，加载失败只累积错误文本，未提供“数据已就绪”信号；异常数据进入时需要补强启动阻断与界面反馈。
- `source/scripts/core/GameStateManager.gd` 的状态字典对外可写。当前交互均经迁移函数，后续扩展若直接修改字典会绕开状态机约束，建议将状态容器收口为只读查询接口。
- 工位与总装流程包含多帧 `await`。当前重启采用重载场景，尚未验证不重载场景时的取消、重置和旧协程隔离。
- 厂房、列车和粒子全由程序化几何创建；Forward+ 下存在多盏动态灯和大量独立材质，尚无帧率、显存或目标机性能证据。
- 项目未随包提供 GUT、锁定工具链或 CI 配置。烟雾测试对核心迁移覆盖较好，UI、碰撞、输入和动画仍缺自动化回归。

## 与 RailCraft 主线的关系

当前主线为 Unity 第三人称流程白盒：题库、零件配方、状态模型、渲染设置和验收基线均由项目自身实现。该外部包采用 Forward+、Godot 自动加载、6 题/6 零件/3 模块、第三人称 E 键交互、音频接口、用时和正确率统计；当前 Unity 白盒使用 58 题、14 零件、6 个装配节点和独立的调试闭环。

因此，以下内容保留为参考，不进入现有应用工程：

- `project.godot`、自动加载、场景树、GDScript 和 Godot 运行时；
- 6 道题的文案、答案与解释，它们未提供可追溯资料来源；
- 计时/正确率、音频和第三人称探索等玩法能力，除非形成新的需求与验收标准。

状态机、错误工位反馈、单件携带限制、模块装配演出与程序化白盒等方向已经在 Unity 主线按自身领域模型和测试重新实现；没有复制该包的源码、题目或资产。后续采纳仍需通过内容来源、许可、性能和自动化测试要求。

## Git 与本地隔离范围

- 跟踪：原型包装 README 与 `.gitignore`、本评审、外部交付登记。
- 忽略：原始 ZIP、`prototypes/.../source/` 的全部源码与内容，以及其中的 Godot 缓存和派生文件。
- 解封条件：补齐作者、许可证、题目来源和第三方资产权属，并通过新的可审计导入决策。
