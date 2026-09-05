# Rebel Hideout Operation Table

批次：`ART-004`。资产身份：**展示风格**。核验日期：2026-09-05。

## 来源与署名

- 原作：[Rebel Hideout Part 2: Main Operation Table](https://sketchfab.com/3d-models/rebel-hideout-part-2-main-operation-table-e716a56134cf459b897cc9107b14125f)
- 作者：[Adrian Dierigl](https://sketchfab.com/adriandierigl)
- 许可：[Creative Commons Attribution 4.0 International / CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)
- 官方核验接口：<https://api.sketchfab.com/v3/models/e716a56134cf459b897cc9107b14125f>
- 用户提供的原始包未纳入仓库；29,015,595 bytes。
- 原始包 SHA-256：`e6f881a2799f45144909adf21ceb4d3df255b0f57e4f63a0090c02b263886c62`。

2026-09-05 通过 Sketchfab 官方公开 API 复核：作品名、作者、可下载状态和许可均已确认，
许可字段明确链接到 CC BY 4.0。历史用户截图也显示 CC Attribution、署名要求及允许商业使用。
历史聊天曾将作者写作 Adrian Dierig，本登记按原站修正为 **Adrian Dierigl**。

许可允许复制、再分发和改编（包括商业用途），使用时需保留作者、原作和许可链接，
说明修改；不得暗示作者认可 RailCraft。完整许可条件以 [legal code](https://creativecommons.org/licenses/by/4.0/legalcode.en) 为准。
`LICENSE.txt` 是随包署名与许可通知，请在包含该资产的交付包中保留。

## 文件与身份边界

`Models/RebelHideoutOperationTable.dae` 取自原 ZIP 内 `source/model.zip` 的 `model/model.dae`；
10 张纹理取自同一嵌套包的 `model/textures/`。已逐文件比对 SHA-256，仓库模型和纹理字节
与嵌套原件一致。仅整理路径和模型文件名。详见 `asset-manifest.json`。

此模型用于材料包知识、装配和调试/检验工位的视觉表达。原作者将其描述为虚构场景的
旧工业操作台；本项目不将其作为列车制造专用工装、真实测量仪表或车型专用工程件。
不从外观推断载荷、制造尺寸、接口、扭矩或精度。

## 静态验收

| 项目 | 结果 |
| --- | --- |
| DAE 几何 | 2 个网格；9,170 个位置记录；12,748 个三角形 |
| 原站统计 | 6,792 vertices、12,748 faces；顶点统计口径与源 DAE 位置记录不同 |
| 单位与轴向 | DAE 为 `unit name="meter" meter="0.01"`、`Y_UP`；1 个源坐标单位对应 0.01 m 的文件标度 |
| 工程尺寸 | 未核验；文件标度不构成真实设备尺寸证明 |
| 原点/接入尺度 | 由生成器按导入后的渲染边界落地和适配工位；属于展示尺度 |
| 材质与纹理 | 1001 / 1002 两材质组，各有 albedo、AO、metallic、normal、roughness；10 张均为 2048×2048 |
| DAE 贴图引用 | DAE 不含有效贴图关联；URP 材质绑定与 roughness→smoothness 由接入生成器处理 |
| LOD | 仅有源 LOD0；未提供或生成 LOD1 / LOD2 |
| 碰撞 | 接入契约为独立 `WorkbenchCollision` 非 Trigger BoxCollider 组；保留交互根 Trigger |
| 生成入口 | `ThirdPerson/Editor/FactoryKitWorkbenchVisualFactory.cs` |
| 运行验收 | 查看本批 ART-004 验收报告、EditMode 与 Player 烟测；本清单记录静态资产与许可核验 |

## RailCraft 修改说明

源 DAE 和纹理保留原字节。RailCraft 接入层会重建 URP 材质绑定，将粗糙度转换成平滑度通道，
调整实例展示尺度与位置，并添加独立碰撞体；这些接入修改由 RailCraft 提供。
随包署名应同时保留该修改说明。该许可核验仅覆盖本目录的原作与纹理；其他第三方资产
仍按各自登记验收。
