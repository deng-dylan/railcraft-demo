# RailCraft Unity v0.1 验收矩阵

本文把 `Documentation/Scope.md` 中的 28 条全局约束映射到可复核证据。矩阵描述验收方法，不提前声明 Task 13 的最终结果；最终测试总数、Build 状态、截图清单和 SHA-256 以 `Artifacts/Acceptance/acceptance-report.md` 为准。

## 证据类型

- **自动化**：Unity EditMode、PlayMode 或 Windows Standalone Player 测试。
- **配置/内容**：版本锁、PlayerSettings、场景、prefab、JSON、交付清单或忽略规则。
- **静态检查**：Task 13 末尾执行的源码搜索、文件盘点或哈希校验。
- **GUI**：1920×1080 发布版人工走查与 `Artifacts/Acceptance/screenshots/` 中的截图。
- **人工**：需要操作者观察行为、文字语义、离线运行或程序退出的项目。

## 28 条约束映射

| ID | 验收证据 | 通过条件 |
|---|---|---|
| GC-01 | 自动化：`ProjectConfigurationTests.ProjectTargetsWindowsLinearColorSpace`。配置：`WindowsBuild.Build` 固定 `StandaloneWindows64`，产物为 `Builds/Windows/RailCraft.exe`。 | 构建报告成功，PE 可执行文件与完整 Windows Build 目录存在，SHA-256 已记录。 |
| GC-02 | 配置：`ProjectSettings/ProjectVersion.txt`、`Packages/packages-lock.json`；环境：`Artifacts/Acceptance/environment.txt`。 | 编辑器为 `6000.3.21f1` 且无迁移；最终包解析版本与报告一致。 |
| GC-03 | 自动化：`ProjectConfigurationTests.ProjectTargetsWindowsLinearColorSpace`、`ProjectConfigurationTests.ProjectDoesNotUseTemplateInputActions`、`FactoryPresentationContractTests.FactoryPresentationAssetsUseConservativeUrpSettings`。配置：URP Renderer、uGUI prefabs、`RailCraftControls.inputactions`。 | Linear、URP `17.3.x`、uGUI 和 Input System 生效；运行内容未引入 HDRP、Built-in、UI Toolkit 运行界面或第二输入框架。 |
| GC-04 | 自动化：`ContentValidatorTests.ProductionFlowFreezesAllStagesAndTheTeachingCommissioningLoop`、`ModelContractValidatorTests.HeadDisplayIsStaticPresentationOnly`。GUI：`06-powered-bogie-complete.png`、`11-release-hero-view.png`。 | 交互语境明确为 CR400AF/SWM-400E1，头车与动力转向架身份显示正确。 |
| GC-05 | 配置：`flow.v1.json` 与 `PartPrefabCatalog.asset` 不含 SWT-400E1；交付清单将 SWT-400E1 标为后续版本。静态检查：搜索运行内容中的 `SWT-400E1`。 | 无拖车转向架步骤、资产键、菜单入口或交互对象。 |
| GC-06 | 自动化：`FactorySceneContractTests.FactorySceneContainsRequiredProductionHierarchy`、`FactorySceneContractTests.AssemblyBayPrefabExistsAndContainsFactoryDetails`。GUI：`02-guidance.png`、`06-powered-bogie-complete.png`。 | 发布版运行于列车制造工厂，装配、落车、调试、检验和放行区域可识别。 |
| GC-07 | 自动化：`ModelContractValidatorTests.HeadDisplayIsStaticPresentationOnly`、`FullFlowTests.CarbodyLoweringUsesVerticalMotionConstraint`、`CompletionPresentationTests.CompletionFocusesHeroAndMovesReleasedVehicleToTrackDestination`。GUI：`07-carbody-lowering.png`、`11-release-hero-view.png`。 | 简化动力中间车可落车；CR400AF 风格头车仅参与背景与英雄镜头。 |
| GC-08 | 自动化：`GuidedFlowMachineTests.CorrectAnswersUnlockOnlyTheCurrentStep`、`DropTargetTests.TargetAcceptsOnlyMatchingUnlockedStep`、`FullFlowTests.FullGuidedRunReachesCompletedAndUsesAllProductionIds`。GUI：`04-step-unlocked.png`。 | 每次只开放当前步骤，界面无自由装配入口，非当前模块不能推进。 |
| GC-09 | 自动化：`FactorySceneContractTests.FactoryInputActionsExposeDocumentedBindings`。GUI/人工：完整走查期间只使用键盘和鼠标。 | 所有必需操作可由鼠标、WASD 和方向键完成；无触控专用流程。 |
| GC-10 | 自动化：`FactoryCameraControllerTests` 全类、`DragDropControllerTests.PointerDraggingPreservesAuthoredRotation`、`FactoryCameraControllerTests.CameraInputDoesNotRotateDraggedPart`。GUI/人工：走查镜头控制与拖拽。 | 环绕、平移、缩放和键盘移动有效；拖拽前后旋转差小于 `0.01°`，无零件旋转控件。 |
| GC-11 | 自动化：`GuidedFlowMachineTests.WrongDropIsRejectedWithoutChangingStep`、`DragDropControllerTests.PointerAcceptedDropRaisesCompletionOnlyAfterSnapFinishes`、`FullFlowTests.FullGuidedRunReachesCompletedAndUsesAllProductionIds`。GUI：`05-wrong-drop-feedback.png`。 | 错误拖放不改变进度；15 次正确拖放各产生一次步骤完成。 |
| GC-12 | 配置/内容：15 个 `flow.v1.json` 条目与 15 个 catalog prefab 均为子系统、车体或任务卡。静态检查：运行时代码、UI 与内容搜索螺栓、扭矩、配合公差、紧固件实现。GUI：`06-powered-bogie-complete.png`。 | 未出现单紧固件交互或工程装配参数输入。 |
| GC-13 | 自动化：`FullFlowTests.FullGuidedRunReachesCompletedAndUsesAllProductionIds`、`FullFlowTests.CommissioningUsesTeachingFailureInspectionAndQuestionFreeRetry`。GUI：`06` 至 `11` 截图序列。 | 15 步、48 题、两次调试和最终放行全部完成，顺序与冻结流程一致。 |
| GC-14 | 自动化：`ContentValidatorTests.ProductionFlowFreezesAllStagesAndTheTeachingCommissioningLoop`、`FullFlowTests.CommissioningUsesTeachingFailureInspectionAndQuestionFreeRetry`。GUI：`08-first-commissioning-failure.png`。 | 首次调试显示完整教学占位异常文案，并明确不代表 SWM-400E1 真实故障结论。 |
| GC-15 | 自动化：`ContentValidatorTests.ProductionContentPassesEveryContract`、`ContentValidatorTests.ProductionFlowFreezesAllStagesAndTheTeachingCommissioningLoop`、`FactorySceneContractTests.RebuildingFactoryPreservesModelOverridesAndExistingBuildScenes`。配置：`flow.v1.json` 与流程交付清单。 | 运行顺序由本地 JSON 驱动，15 个稳定 step/asset/target ID 通过校验；流程清单标注暂定性质。 |
| GC-16 | 自动化：`QuestionBankBaselineTests.FrozenQuestionBankContainsFortyChoiceAndEightTrueFalse`、`ContentValidatorTests.ValidBundleUsesEveryQuestionExactlyOnce`、`FullFlowTests.FullGuidedRunReachesCompletedAndUsesAllProductionIds`。 | `q001`–`q048` 连续，40 单选/8 判断，每题在新运行中恰好出现并答对一次。 |
| GC-17 | 配置/人工：`deliveries/content/question-bank-v1/README.md` 的源 SHA-256、`review/question-bank.txt` 和 `questions.v1.json` 三方复核；自动化：`QuestionBankBaselineTests`。 | 冻结 48 题的题干、全部选项和正确答案与审阅转写一致；任何差异阻断发布。 |
| GC-18 | 自动化：`UiPrefabContractTests.QuizPanelContainsRequiredFieldsAndNoScoreSurface`。GUI：`03-wrong-answer-retry.png`、`04-step-unlocked.png`。 | 题目界面不显示“来源”、引用、文献或 source 字段。 |
| GC-19 | 自动化：`GuidedFlowMachineTests.WrongAnswerDoesNotAdvanceQuestionIndex`、`GuidedFlowMachineTests.CorrectAnswersUnlockOnlyTheCurrentStep`、`QuizPresenterTests.WrongAnswerShowsRetryAndKeepsPanelOpen`、`QuizPresenterTests.FinalCorrectAnswerHidesQuizAndUnlocksStepOnce`。GUI：`03`、`04`。 | 错答留在当前题并允许重选；当前阶段末题答对后只解锁一次。 |
| GC-20 | 自动化：`UiPrefabContractTests.QuizPanelContainsRequiredFieldsAndNoScoreSurface`、`FullFlowTests.FullGuidedRunReachesCompletedAndUsesAllProductionIds`。静态检查：搜索 `ScorePresenter`、score/accuracy/grade/rank/mastery 对应实现。GUI：完整截图组。 | 运行时与所有截图无分数、正确率、等级、排名或知识掌握度。 |
| GC-21 | 自动化：`ProjectConfigurationTests.ProjectHasNoAnalyticsMultiplayerOrXrPackages`、`ProjectConfigurationTests.ProjectDisablesUnityAudio`、`MenuAndResetTests.MainMenuContainsRequiredActionsAndNoContinueButton`、`MenuAndResetTests.SettingsContainGraphicsAndWindowModeOnlyAndApplyInMemory`、`GuidedFlowMachineTests.ResetClearsProgressWithoutPersistence`。静态检查：源码搜索 `PlayerPrefs`、账户、网络、分析、音频、Android、XR、触控、多人实现。 | 排除能力均无可达入口或运行时实现，音频关闭，只有 Windows 发布目标。 |
| GC-22 | 自动化：`MenuAndResetTests` 全类、`UiPrefabContractTests.NavigationPrefabsExposeExactV01Surface`。GUI：`01-main-menu.png`、`02-guidance.png`、`12-reset-to-guidance.png`；人工：退出程序。 | 开始、说明、画质/窗口设置、重置与退出完整可用；无继续和音频设置。 |
| GC-23 | 自动化：`BootstrapStartupTests.ProductionBootstrapLoadsFactoryAndWiresFirstInteractiveStep`。配置：Bootstrap 以本地 `TextAsset`、场景和 catalog 组装内容。静态检查：运行时代码搜索网络 API。人工：断网启动并完成关键流程。 | 断开网络后仍可启动、答题、装配、重置和退出。 |
| GC-24 | 自动化：`ModelContractValidatorTests.EveryDraggablePrefabHasRequiredContract`、`FactoryPresentationContractTests.ProductionAssetBudgetsAreExecutableContracts`。配置：`Documentation/ModelHandoff.md`。GUI：占位资产画面。 | 占位模型无未验证工程尺寸声明；生产资产只有在来源、尺寸、单位和身份门禁通过后进入 catalog。 |
| GC-25 | 自动化：`ProductionWheelContractTests.ProductionWheelUsesStableRuntimeContract` 的诚实阻断分支。配置：模型交付清单与 `Documentation/ModelHandoff.md`。 | 当前候选 `wheel.SLDPRT` 状态写入报告；缺少中性格式、尺寸或适用声明时不生成生产轮对视觉资产。 |
| GC-26 | 自动化：`UiPrefabContractTests.NavigationPrefabsExposeExactV01Surface`、`MenuAndResetTests.StartExperienceShowsExactGuidanceThenEntersKnowledgeGate`。GUI：12 张截图。人工：逐页检查可见 UI。 | 菜单、说明、题目、反馈、阶段、异常和完成文案均为简体中文。 |
| GC-27 | 配置：`.gitignore`、`.gitattributes`、交付清单；静态检查：`git status --short`、`git check-ignore deliveries/**/release/**` 与验收目录盘点。 | 源码、优化运行时资产、测试、清单、文档和 12 张审阅图可跟踪；原始 release 二进制、Library、TestResults、Builds 保持忽略。 |
| GC-28 | 人工：实施计划、用户指令和任务记录审阅。 | 实施过程遵循当次明确授权与执行模式；该治理项不向运行时添加功能。 |

## GUI 截图清单

所有截图使用最终 Windows Player、1920×1080 分辨率，并保留完整窗口内容。

| 文件 | 必须证明的内容 |
|---|---|
| `01-main-menu.png` | “开始体验 / 操作说明 / 设置 / 退出”，无“继续游戏”。 |
| `02-guidance.png` | 简体中文目标、答题/拖拽说明、镜头控制和占位范围声明。 |
| `03-wrong-answer-retry.png` | 错答反馈、同题仍打开、无分数/来源。 |
| `04-step-unlocked.png` | 当前步骤答完后模块与接口高亮，其他模块保持锁定。 |
| `05-wrong-drop-feedback.png` | 固定错误文案，进度未增加。 |
| `06-powered-bogie-complete.png` | 11 个子系统完成后的 SWM-400E1 动力转向架占位展示。 |
| `07-carbody-lowering.png` | 简化动力中间车沿竖直约束落车。 |
| `08-first-commissioning-failure.png` | 教学占位异常完整文案与整改入口。 |
| `09-inspection-and-rework.png` | 传感器检查标记、整改/检验阶段。 |
| `10-second-commissioning-pass.png` | 二次调试通过且未重复初次调试题。 |
| `11-release-hero-view.png` | “流程完成：已投入使用”、放行车辆和静态 CR400AF 背景头车。 |
| `12-reset-to-guidance.png` | 重置后回到操作说明，已安装视觉与进度清空。 |

## 人工走查记录要求

走查者需从全新进程开始，记录每个题目 ID 和步骤 ID，故意提交至少一次错误答案与错误拖放，操作全部镜头控制，完成调试闭环，执行重置，再次进入操作说明，最后从 UI 正常退出。走查同时确认无分数、等级、继续按钮和音频控制，并在断网状态验证本地运行。

若自动化 XML、截图、人工记录、Build 哈希或内容清单任一缺失，最终状态保持“待验收”或“阻断”，不得填写“通过”。
