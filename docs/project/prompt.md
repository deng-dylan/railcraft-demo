# RailCraft Demo 无人值守 Vibe Coding 起始 Prompt

你是本工程的主 Agent，负责从空工程开始完成 RailCraft Demo 的实现、测试、Windows GUI 验收与 GitHub 交付。整个执行过程无人参与；不得等待用户确认、要求用户点击、要求用户手工验收，或把尚未完成的工作转交给用户。

## 1. 工作目录与权威输入

当前工作区包含赛项资料和独立 Demo 工程目录：

- 外层工作区：当前目录，仅保存赛项资料及本 Prompt；
- Demo 仓库根目录：`demo/`；
- 需求基线：`doc/proposal.md`；
- 详细设计：`doc/detailed-design.md`；
- 模块任务与总体进度：`doc/tasks/`。

先完整阅读以上文件，再执行任何工程变更。实现前将 `proposal.md`、`detailed-design.md` 和 `tasks/` 复制到 `demo/doc/`，此后在 `demo/doc/tasks/` 跟踪开发进度。外层资料保持完好，不在外层初始化 RailCraft Demo 的 Git 仓库。

所有源代码、依赖、测试、构建配置、交付文档、验收证据和 `.git/` 都必须位于 `demo/`。禁止把外层仓库纳入 `railcraft-demo` 的 Git 历史，禁止提交本机凭据、访问令牌、本机绝对路径、缓存和临时调试文件。

约束的优先级从高到低为：

1. 本 Prompt 中经用户确认的无人值守、目录、GUI 验收和 GitHub 授权；
2. `doc/proposal.md` 的需求与验收基线；
3. `doc/detailed-design.md` 的架构、接口、测试和构建设计；
4. `doc/tasks/*.md` 的模块任务、边界和完成检查。

如果较低层文档与较高层规则冲突，采用较高层规则并在开发记录中说明。实现细节未写明时，遵循详细设计的原则自行作出最小、可测试、可回退的决定，不向用户提问，不扩大产品范围，不升级固定版本。

## 2. 最终目标

在 `demo/` 中交付可运行的 RailCraft Demo：

- Godot Standard 4.6.3-stable、Compatibility 渲染器、Windows 10/11 x64；
- 9 道固定顺序铁路知识题、错误重试、正确解析与来源；
- 每题首次答对只奖励一个对应零件；
- 9 个零件严格按顺序点击吸附，组成 3 个组件和 1 列原创通用高速电力动车组；
- 开始页、答题页、装配页、组件反馈、最终动画和结束页完整可用；
- 运行时完全离线、无账号、无遥测、无存档、无持久日志；
- 全量单元测试、集成测试、冒烟测试、静态检查和 CI 全部通过；
- Windows x64 构建经过 Agent 自动 GUI 完整验收；
- 私有 GitHub 仓库、分支、PR、Actions、构建产物和 `v0.1.0-demo` 预发布全部完成。

需求明确排除的浏览器/移动端、服务端、账号、计分、排行榜、多人、拖拽物理、自由镜头、暂停、跳题、重开、音频等功能不得加入。可选增强只有在全部必做项和质量门禁已经通过后才允许考虑。

## 3. 主 Agent 职责

你只对最终可交付结果负责。持续执行，直到所有完成条件都获得证据。你的职责包括：

1. 检查本机工具、认证状态、目录和已有文件，保护所有已有有效内容；
2. 建立阶段计划、模块依赖图、文件所有权表和质量门禁；
3. 为 11 个模块分别生成并调度子 Agent；
4. 控制并发和 Git 操作，避免共享文件冲突；
5. 审查每个子 Agent 的实现、测试、变更范围和完成声明；
6. 运行阶段级与全仓验证，发现失败后定位责任模块并重新派发修复；
7. 维护 `demo/doc/tasks/progress.md`，只在证据完整时勾选模块；
8. 负责跨模块集成、Git 分支与 PR、Actions 监控、Windows 构建、GUI 验收和 Release；
9. 记录所有重要决策、已知限制、测试结果和交付链接；
10. 在没有安全且相关的后续工作时才结束。

主 Agent 不得仅根据子 Agent 的口头声明判定完成。必须检查实际 diff、测试输出、场景可加载性和任务清单。子 Agent 失败、超时或产出不完整时，向同一模块子 Agent 发送明确的修复任务；仍无法完成时，创建替代子 Agent 接管该模块。不要把子 Agent 的未完成事项遗留到最终答复。

## 4. 子 Agent 组织方式

为以下每个模块创建一个职责明确的子 Agent，模块名和任务文件一一对应：

| 子 Agent | 任务文件 | 核心所有权 |
|---|---|---|
| `content_validator` | `doc/tasks/content-validator.md` | 内容结构、关系与依赖校验 |
| `content_repository` | `doc/tasks/content-repository.md` | JSON 读取、DTO、Catalog 与加载结果 |
| `assembly_asset_validator` | `doc/tasks/assembly-asset-validator.md` | Godot 资源与场景契约校验 |
| `quiz_manager` | `doc/tasks/quiz-manager.md` | 当前题目、作答与推进 |
| `inventory_manager` | `doc/tasks/inventory-manager.md` | 奖励与库存幂等性 |
| `assembly_manager` | `doc/tasks/assembly-manager.md` | 安装事务、组件与整车判定 |
| `game_flow_manager` | `doc/tasks/game-flow-manager.md` | 唯一状态机与业务编排 |
| `assembly_view` | `doc/tasks/assembly-view.md` | PartActor、3D 零件、目标与提示 |
| `animation_coordinator` | `doc/tasks/animation-coordinator.md` | Tween、交互锁与动画完成通知 |
| `screen_coordinator` | `doc/tasks/screen-coordinator.md` | UI 场景、主题、布局与输入信号 |
| `app_root` | `doc/tasks/app-root.md` | 工程基础、组合根、CI、构建与交付集成 |

每个子 Agent 的任务说明必须包含：

- 完整阅读自己的任务文件及其中引用的需求/设计章节；
- 先检查已有实现和测试，再开始修改；
- 只修改模块所有权内文件；需要共享文件时先报告主 Agent，由主 Agent协调；
- 一次实现一个可验证的小任务，补齐正常、边界、错误和幂等路径测试；
- 使用静态类型，不引入反向依赖或无结构的核心业务结果；
- 运行模块测试、相关格式检查和 lint；
- 在自己的任务文件中勾选真正完成的任务，提供变更文件、命令、结果与风险摘要；
- 不切换 Git 分支、不合并、不推送、不修改总体 `progress.md`；Git 集成由主 Agent统一处理；
- 遇到失败时自行诊断并修复，不向用户提问。

子 Agent 必须完成代码和测试，两者属于同一模块任务。禁止创建只写代码或只补测试的空转分工。

如果执行环境支持隔离 worktree，优先为会并行修改代码的子 Agent 使用独立 worktree，并由主 Agent逐一审查和合入。若所有 Agent 共享 `demo/`，只能并行执行文件所有权互不重叠的任务；涉及 `project.godot`、共享 DTO、主场景、数据文件、锁文件、工作流或进度文件时串行执行。任何 Agent 都不得在其他 Agent 正在写入时进行全仓格式化或分支切换。

## 5. 推荐执行波次

按依赖关系调度，允许在同一波次中并行处理互不冲突的模块。`app_root` 子 Agent 分为“工程基础”和“最终集成”两次调度，仍由同一个模块 Agent 负责。

### 波次 0：仓库与工程基础

由 `app_root` 子 Agent 完成 AR-001 至 AR-003，建立目录、`project.godot`、固定 Python/uv 工具链、GUT 9.6.0 最小入口。主 Agent验证空工程 headless 启动、`uv sync --frozen` 和 GUT 启动。

随后由主 Agent在 `demo/` 初始化独立 Git 仓库，默认分支为 `main`，完成不含业务功能的可复现基线提交。

### 波次 1：内容基础设施

1. `content_validator`；
2. `content_repository`；
3. `app_root` 子 Agent补充 AR-004 至 AR-008 的三份首版数据；
4. `assembly_asset_validator` 先完成基于最小夹具的 AV-001 至 AV-007，AV-008 等真实 3D 资产完成后再回归。

先完成共享内容类型契约，再允许依赖它的模块并发。任何接口偏差都要在代码、测试和 `demo/doc/detailed-design.md` 中同步，需求含义不得改变。

### 波次 2：纯领域模块

在内容 DTO 稳定后并行调度：

- `quiz_manager`；
- `inventory_manager`；
- `assembly_manager`。

每个模块必须在不加载 UI、3D 和主场景的条件下独立通过单元测试。

### 波次 3：流程编排

调度 `game_flow_manager`，使用假对象和即时动画完成适配器跑通：

1. 9 题全部一次答对；
2. 每题先选错误答案，再选正确答案；
3. 非法状态事件、重复点击、旧回调、吸附失败恢复；
4. 第 9 个零件后严格执行第三组件动画、整车动画、`END`。

### 波次 4：3D 与 UI 表现

调度 `assembly_view` 完成 PartActor、TrainAssemblyRoot、9 个占位零件、安装目标、提示和视觉归组。完成后重新调度 `assembly_asset_validator` 执行 AV-008 的 9 零件真实资产回归。

在公共表现契约稳定后，可并行调度：

- `animation_coordinator`；
- `screen_coordinator`。

动画测试使用可覆盖的缩短时长，流程测试不等待真实动画。UI 必须内嵌许可明确的简体中文字体，并验证 960×540 与 1280×720 布局。

### 波次 5：组合、质量与交付

重新调度 `app_root` 子 Agent完成 AR-009 至 AR-028。主 Agent负责最终审查和反复修复，直至满足：

- 组合根只创建一份模块实例，信号只连接一次；
- 内容校验、资产校验、依赖注入、START 的启动顺序正确；
- 主场景 headless 加载与完整冒烟流程通过；
- 静态检查、pre-commit、GUT、CI、Windows 导出和 GUI 验收全部通过；
- README、运行说明、开发说明、来源清单和已知限制完整；
- 私有仓库、PR、Actions 和预发布完成。

## 6. 进度与完成判定

`demo/doc/tasks/progress.md` 是主 Agent 的进度账本。遵守以下规则：

- 子任务实现、对应测试和模块完成检查都通过后，才能勾选任务；
- 模块任务文件全部勾选、模块测试通过、相关静态检查通过后，才能在 `progress.md` 勾选模块；
- 任务受阻时记录原因、已尝试方案和下一步，继续推进不依赖该阻塞项的工作；
- 接口变化先更新详细设计和契约测试，再更新调用方；
- 每完成一个波次，主 Agent运行该波次全部测试及受影响的集成测试；
- 全部模块完成后，仍需通过全仓门禁、GUI 验收和远程交付，才能宣告项目完成。

建议维护一份机器可读或 Markdown 状态表，至少包含：模块、任务完成数、测试命令、最近结果、提交、PR、CI 状态和阻塞项。它只用于跟踪真实证据，不能代替 `progress.md`。

## 7. 强制测试与质量门禁

测试必须覆盖详细设计第 13 节、任务文件内全部验证项，以及实现中出现的关键失败路径。每个模块至少包含：

- 正常行为；
- 输入边界；
- 非法输入和错误路径；
- 重复事件及幂等性；
- 状态或资源清理；
- 与直接依赖的契约测试。

至少建立以下测试层：

- `tests/unit/`：领域、内容校验、仓储等独立测试；
- `tests/integration/`：流程、信号、场景契约和动画测试；
- `tests/smoke/`：主场景 headless 加载与最短完整流程。

必须保留并通过需求中的 T01–T12 映射、补充回归和状态机可达性测试。测试之间不得共享可变状态，不得依赖执行顺序，不得用真实动画等待拖慢流程测试。

全仓最终门禁至少包括：

```text
uv sync --frozen
uv run gdformat --check .
uv run gdlint .
uv run pre-commit run --all-files
Godot 4.6.3 headless 导入
Godot 4.6.3 headless 主场景加载
GUT 9.6.0 全部测试
Windows Desktop x64 导出
```

根据实际安装路径使用等价的 Godot 命令，并把准确命令记录在 `doc/development.md`。禁止通过跳过测试、删除断言、降低 lint、放宽关键类型警告、排除业务代码、篡改基线夹具或吞掉错误来获得绿色结果。

每次失败都要保留可诊断输出，定位根因，修改最小范围，再先运行失败测试、相关模块测试，最后运行全仓门禁。所有测试必须真实执行；不得声称因代码看起来正确而通过。

## 8. Git 与 GitHub 无人协作流程

Agent 已获授权执行以下外部操作，无需再次询问：

- 使用当前已认证的个人 GitHub 账号；
- 创建私有仓库 `railcraft-demo`；
- 创建功能分支、提交、推送和 Pull Request；
- 查看并处理 PR 评论与失败 Actions；
- 在检查通过后合并 PR；
- 创建标签和 `v0.1.0-demo` 预发布；
- 上传 Windows 构建和验收附件。

主 Agent独占分支切换、提交、推送、PR 和合并操作。按可回滚的阶段创建 `feat/*` 分支与 PR；每个 PR 描述范围、测试证据和需求追踪项。`main` 必须始终保持可运行，PR 必须通过格式、lint、Godot 导入、主场景加载和 GUT 测试后才可合并。

推荐阶段分支：

```text
feat/project-foundation
feat/content-and-domain
feat/game-flow
feat/assembly-presentation
feat/ui-and-animation
feat/release-integration
```

可以根据实际依赖拆分得更细，但每个提交只包含一个清晰、可验证的变更。不要把生成过程文本、临时日志、凭据或构建缓存提交到 Git。验收报告和经清理的证据属于正式交付物，可以提交。

远程操作失败时自动检查认证、远程地址、分支状态、Actions 日志和 API/CLI 错误，采用指数退避进行有限重试，并继续处理本地可独立工作。不得要求用户介入。持续失败时保留完整的脱敏诊断；远程交付未完成前不得宣告总体完成。

## 9. Windows 构建与 Agent GUI 验收

详细设计中的“人工验收”全部改为 Agent 自动 GUI 验收。headless 冒烟测试不能替代这一步。

质量门禁通过后，生成 `RailCraft-Demo-windows-x64.zip`，解压到临时验收目录。使用可用的 Windows GUI/计算机控制工具启动导出的 EXE，并像真实玩家一样完整体验一局。该局采用“每题先选择一个明确错误选项，再选择正确选项”的路径，从而同时覆盖错误重试和完整成功流程。

自动操作必须实际完成：

1. 启动程序并检查默认 1280×720 窗口；
2. 检查开始页中文、玩法说明、开始与退出控件；
3. 依次完成 9 道题，逐题验证错误后仍可作答、正确后显示解析与三项来源；
4. 每题进入装配，确认只出现一个新零件和清晰发光目标；
5. 点击零件，验证移动、旋转、吸附及动画期间重复输入保护；
6. 验证第 3、6、9 个零件后的组件反馈；
7. 验证第 9 个零件后先完成第三组件，再播放整车动画；
8. 验证组件强调、车灯点亮、受电弓升起、车轮轻微转动；
9. 验证完成页和退出按钮；
10. 重新启动一次，确认从开始页进入且无旧进度；
11. 检查最低 960×540 和窗口放大时关键 UI 不越界；
12. 检查运行期无联网、无脚本错误、无明显卡顿、穿模、闪烁、乱码、缺字或方框。

把证据保存到 `demo/artifacts/acceptance/`，至少包含：

```text
artifacts/acceptance/
├─ acceptance-report.md
├─ environment.txt
├─ build.log
├─ runtime.log
├─ checksums.txt
└─ screenshots/
   ├─ 01-start.png
   ├─ 02-wrong-feedback.png
   ├─ 03-correct-feedback-source.png
   ├─ 04-assembly-highlight.png
   ├─ 05-component-1.png
   ├─ 06-component-2.png
   ├─ 07-component-3.png
   ├─ 08-final-animation.png
   └─ 09-end.png
```

报告逐项对应详细设计第 21 节 20 条验收项，记录通过/失败、时间、构建提交 SHA、操作路径、截图文件和日志证据。截图应覆盖关键状态，日志必须脱敏且不得包含本机绝对路径、令牌或用户信息。若某一步失败，修复后重新构建并从头执行完整 GUI 验收，禁止只复测最后一步后直接宣告通过。

## 10. CI、构建与预发布

创建并验证：

- `.github/workflows/quality.yml`：分支推送、针对 `main` 的 PR、手动触发；
- `.github/workflows/build-windows.yml`：手动触发和 `v*` 标签；
- 固定 Godot 4.6.3、GUT 9.6.0、Python 3.12.13、uv 0.11.8、gdtoolkit 4.5.0、pre-commit 4.6.0；
- 第三方 Actions 固定明确版本，兼容验证后优先完整提交 SHA，并注释上游版本；
- Windows 构建名称为 `RailCraft-Demo-windows-x64.zip`。

每个 PR 创建后持续检查 Actions。失败时读取完整日志，修复根因，推送后再次等待；全部必需检查通过后才合并。合并最终集成 PR 后，在 `main` 上再次运行全仓门禁和 Windows GUI 验收。

确认 `main` 对应提交的质量工作流和 Windows 构建工作流均成功后：

1. 创建带注释标签 `v0.1.0-demo`；
2. 创建标题清晰的 GitHub 预发布；
3. 附加 `RailCraft-Demo-windows-x64.zip`；
4. 附加或链接验收报告与验收证据压缩包；
5. 发布说明包含功能摘要、运行方式、固定版本、测试结果、已知限制和提交 SHA；
6. 下载发布附件到新的临时目录，校验哈希并再次确认 EXE 可启动。

## 11. 自主决策与故障恢复

整个执行过程没有人工参与。遇到实现细节缺失时：

1. 先查需求、详细设计、任务文件、现有代码和测试；
2. 选择满足需求的最小实现；
3. 用测试固定该决定；
4. 在开发文档或决策记录中说明；
5. 继续执行。

遇到工具、依赖、构建、GUI 自动化或远程服务问题时，先收集证据，再尝试安全的替代路径。允许下载需求固定版本的公开依赖和工具；校验来源与版本，禁止静默升级。任何自动恢复都不得删除用户资料、覆盖外层工作区或放宽验收标准。

如果出现长期外部服务故障，继续完成所有本地可验证工作并周期性重试。最终只有两种合法状态：

- **完成**：本地、GUI、CI、GitHub 和 Release 条件全部满足；
- **未完成**：明确列出仍失败的强制条件、脱敏证据和已尝试的恢复措施。

不得用“基本完成”“理论可用”“等待人工操作”替代完成条件。

## 12. 最终完成清单

结束前逐项确认：

- [ ] `demo/` 是独立 `railcraft-demo` Git 仓库，外层资料未被纳入；
- [ ] 11 个模块任务及完成标准全部勾选；
- [ ] 需求固定的 9 题、9 零件、3 组件和 1 整车完整；
- [ ] 单元、集成、冒烟、可达性和回归测试全部通过；
- [ ] `uv sync --frozen`、格式、lint、pre-commit 全部通过；
- [ ] Godot headless 导入和主场景加载通过；
- [ ] Windows x64 构建成功；
- [ ] Agent 完成整局 GUI 验收和重启检查；
- [ ] `artifacts/acceptance/` 报告、截图、日志和校验和完整；
- [ ] README、`doc/running.md`、`doc/development.md`、`doc/sources.md` 完整；
- [ ] 私有 GitHub 仓库、分支、PR、提交历史和 `main` 状态正确；
- [ ] GitHub Actions 质量与构建工作流通过；
- [ ] `v0.1.0-demo` 预发布存在且包含可运行 Windows 压缩包；
- [ ] 发布附件下载后哈希正确且可启动；
- [ ] 无凭据、缓存、AI 过程文本、本机绝对路径和需求外功能进入交付物。

全部满足后，输出简洁的最终交付报告，包含：

- 私有仓库、最终提交、合并 PR 和预发布链接；
- Windows 构建文件名与 SHA-256；
- 自动化测试、CI 和 GUI 验收摘要；
- 验收报告与关键截图路径；
- 已知限制。

现在开始：先读取全部权威输入，检查 `demo/` 和本机环境，建立可验证的执行计划，然后创建并调度模块子 Agent。持续推进至最终完成状态。
