# v0.4.0-internal.2 工作台与碰撞验收

日期：2026-09-05。编号：ART-004 / DEBUG-002。Unity 6000.3.21f1，Windows x86_64 Development Build。
本批承接引用任务未完成的“全部工作台换成用户提供模型、补碰撞并打包”。

## 成品

- ZIP：`../../../../ReleasePackages/RailCraft-v0.4.0-internal.2-windows-x64.zip`，236,298,877 bytes。
- SHA-256：`2303889c8da1e018a0bb170b45e8e64656fb3480a6448dd5040d87d774a98b48`。
- 解压后 305 文件，485,854,171 bytes；全部文件与打包暂存目录 SHA-256 一致，详见 `package-files.csv` 和 `package-verification.json`。
- 正常启动：解压后双击 `RailCraftInternalTest.exe`。
- 内部调试：运行 `StartInternalDebug.cmd`，按 `Ctrl+Alt+Shift+F10` 解锁，再在 F10 面板切换碰撞箱。
- 旧 `v0.4.0-internal.1` ZIP 保留；本批证据已脱敏后纳入 Git。

## 变更与资产验收

| 项目 | 结果 |
| --- | --- |
| 替换范围 | 5 知识工作台、4 模块装配台、1 总装台、3 调试/检验台，共 13 张 |
| 来源许可 | Rebel Hideout Part 2: Main Operation Table；Adrian Dierigl；Sketchfab 官方 API 核验 CC BY 4.0 |
| 资产身份 | 展示风格；工程尺寸、载荷、精度、车型适用性均未声明 |
| 源文件 | DAE 与 10 张 2K 原纹理保持 ZIP 原字节；完整 SHA-256 在资产目录 `asset-manifest.json` |
| 几何 | 2 网格，12,748 三角形，9,170 DAE 位置记录；共享网格；仅 LOD0，无 LOD1/LOD2 |
| 单位、轴向、原点 | DAE Y_UP、meter=0.01；生成器按导入后网格边界校准展示尺寸，水平居中、底部落地 |
| 展示尺寸 | 知识/调试台 2.7×1.13×1.356；模块台 4.428×0.8×2.706；总装台 6.588×0.67×4.026（Unity 单位，宽×高×深） |
| 装配兼容 | 模块台保留原槽位高度，总装台面 0.67、轮底槽位 0.69；材料包及存档结构不变 |
| 碰撞 | 每台 Top + LeftSupport + RightSupport 三个实体 BoxCollider，共 39 个；13 根交互 Trigger 保留；无 MeshCollider/刚体/动画 |
| PBR | 两套 URP/Lit：albedo sRGB；normal、AO、金属/光滑度 linear；R=metallic、A=1-roughness |
| 生成入口 | `OperationTableAssetPreparation.Prepare()` → `WhiteboxSceneBuilder.Build()` → `FactoryKitWorkbenchVisualFactory` |
| 调试线框 | MeshTopology.Lines、每实际碰撞体 12 条边，青色静态/黄色刚体，过滤 Trigger/禁用/隐藏对象，可在独立 Player 显示与关闭 |
| 打包署名 | `ThirdPartyNotices/RebelHideoutOperationTable/` 内附 README、LICENSE 和 manifest |

原作及许可证据：
[Sketchfab 模型页](https://sketchfab.com/3d-models/rebel-hideout-part-2-main-operation-table-e716a56134cf459b897cc9107b14125f)、
[官方 API](https://api.sketchfab.com/v3/models/e716a56134cf459b897cc9107b14125f)、
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)。

## 验证结果

- EditMode：221/221 通过，0 失败、0 跳过。涵盖实际 PBR 引用、尺寸落地、四向人物胶囊碰撞、Trigger 保留、线框渲染像素/过滤/跟随/清理及原有完整玩法。
- 场景重建：`RAILCRAFT_WHITEBOX_SCENE_BUILT`；主机许可环境重建成功。
- Windows 构建：`RAILCRAFT_WHITEBOX_BUILD_SUCCEEDED`，0 warning / 0 error，485,632,394 bytes。
- Player 成品烟测：13 模型 / 39 实体碰撞 / 13 交互 Trigger 核验通过；标准答题、领取、装配、落车、调试检验、复测和重置通过。
- 碰撞线框烟测：`RAILCRAFT_INTERNAL_DEBUG_OVERLAY_VALIDATED` 和 `RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED`；独立 Player 线框材质可用、截图可见、关闭后隐藏。
- ZIP 解压烟测：从实际 ZIP 新解压目录运行，`RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED`。
- `repository-layout.log`：9/9 通过；最终 `git diff --check` 通过。
- 烟测使用独立 `railcraft.whitebox.smoke.session.v2` 存档，普通试玩存档不参与验证。
- 原始 Unity 日志含主机网络和本机路径信息，未纳入公开仓库；本报告、截图、包文件哈希及校验结果构成公开复核证据。

## 实际 Player 截图

- [知识工作台](knowledge-workbench.png)
- [模块装配台](assembly-workbench.png)
- [调试/检验工作台](commissioning-workbench.png)
- [碰撞线框](collision-overlay.png)
- [流程完成态](complete.png)

以上图片来自本批 EXE 的实际相机渲染，均已人工查看。截图为 1600×900；Player 启动参数为 1920×1080。

## 剩余边界

本次模型替换和打包无阻塞。目标展示机帧率/显存、另一台 Windows 断网走查、其他第三方资产最终提交范围以及车型专用工程件来源仍为后续门禁。本批未宣称完成这些人工验收，也未生成额外 LOD。
