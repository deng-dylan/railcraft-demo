# RailCraft 当前资产审计与缺口清单

审计日期：2026-09-04。本文针对 `v0.4.0-art-alpha.5` 当前工作树，区分已可用、
可从现有成品参考件拆分、需要外部资产和需要工程验证的内容。

## 子部件矩阵（域模型层）

| 零件 | 当前来源 | 当前状态 | 下一步 |
| --- | --- | --- | --- |
| 车轴 | `BogieAssemblyDemo.fbx` | 低模语义网格 | 保留；后续从中性 CAD 确认轴身边界 |
| 车轮 | `BogieAssemblyDemo.fbx` | 低模语义网格 | 可从 CW-200K 轮对组提取参考外形 |
| 轴承 | `BogieAssemblyDemo.fbx` | 低模轴箱/轴承座示意 | 可从 `ZHOUCHENGGAI/ZHOUCHENG` 提取参考外形 |
| 制动装置 | `BogieAssemblyDemo.fbx` | 低模盘与卡钳 | 可从 `PANXINGZHIDONGGANG*` 提取参考外形 |
| 牵引拉杆 | CW-200K `QIANYINLAGAN*` | 已接入拆分管线 | 继续做 LOD、碰撞和身份核对 |
| 传感器座 | 当前 Cube 语义占位 | CW-200K 无明确组 | 需要外部模型或自建低模，挂在“构架与制动/牵引”模块 |
| 一系弹性元件 | `BogieAssemblyDemo.fbx` | 低模弹簧 | 可参考 `ZHOUXIANGTANHUANG*` |
| 定位元件 | 当前 Cube 语义占位 | CW-200K 只有候选支架/限位杆 | 需要工程人员确认后再提取，挂在“一系悬挂”模块 |
| 一系减振元件 | `BogieAssemblyDemo.fbx` | 低模减振器 | 可参考 CW-200K `YEYA-1` |
| 二系弹性元件 | `BogieAssemblyDemo.fbx` | 低模空气弹簧示意 | CW-200K 未找到明确空气弹簧组 |
| 高度控制元件 | 当前 Cube 语义占位 | 无明确 CW-200K 几何 | 需要外部高度阀/连杆模型，挂在“二系悬挂”模块 |
| 二系减振元件 | `BogieAssemblyDemo.fbx` | 低模减振器 | 可参考 CW-200K `YEYA-2/YEYA-3` |
| 车体 | Fuxing 中间车 FBX | 已接入 | 继续做车底、门缝和材质精修 |
| 中央牵引装置 | `BogieAssemblyDemo.fbx` | 低模示意 | 可参考 `ZHONGYANGXUANGUA-*`，需确认身份 |

CW-200K 参考件源于客车转向架，不能直接声明为 CR400AF 或 SWM-400E1 工程资产。
当前 FBX 为 103 个网格、428,388 三角面、6 类项目材质；正式拆件建议重新从
`CW200K.step` 导出模块化 FBX，保留来源层级并为每个零件建立独立 LOD、碰撞和 Prefab。

域模型保留 14 个子部件 ID，玩家界面和知识工位收敛为 5 个材料包。子部件共享材料包级
工位和题目池，领取与安装按包完成，不能为了填满题目数量人为增加模型。

## 工位与环境矩阵

| 区域 | 当前状态 | 现有资产可完成的部分 | 仍欠 |
| --- | --- | --- | --- |
| 5 个材料包知识/领取工位 | 已改为模块化 Kenney 外观 | `machine-bed`、`screen-panel-wide`、按钮、手柄 | 专用 HMI 面板、线缆、工装收纳 |
| 4 个子装配台 | 已改为模块化 Kenney 外观 | 双机座、平面屏、控制件 | 专用转向架工装、定位夹具 |
| 构体装配台 | 已改为模块化 Kenney 外观 | 同上，可调跨度 | 大型装配工装、吊具 |
| 3 个调试台 | 已改为模块化 Kenney 外观 | 状态屏、确认按钮、手柄 | 真实测试柜、仪表、线缆 |
| 落车平台 | 程序化平台与轨道 | 可复用 Kenney 轨道片 | 真实升降台、轨旁设备和地面材质 |
| 厂房地板/墙体/屋顶 | 地板、墙体、标识仍以程序几何为主；屋顶已补 | 现有 Kenney 可补局部结构 | 成品厂房模块、地面磨损/贴花、门窗 |
| 远景 | 起重机、集装箱、变电站、维修轨道已加入 | 当前足够做近中景层次 | 大厅外景、堆场、车辆远景、天空/雾效 |
| 工业小道具 | ArtStationIndustrial 已接入叉车、设备、货架、隔离墩 | 适合背景层 | 授权确认、碰撞/交互道具仍待处理 |
| 完成态转向架 | CW-200K 参考件 | 已有完整视觉 | 3 级 LOD、碰撞和更真实材质 |
| 八节编组展示 | FuxingTrain FBX + LOD | 已完成 | 远景材质、灯光和目标机性能复测 |

## 当前已隔离的资源

- `Simple Factory`：269,600 三角面、UV 覆盖不足、无可靠许可凭证，仅在本机 `.tmp`
  隔离归档保留审模参考，不进入 `Assets`、Git 或发布包。
- `citysubwaytrain`：包含旧 Cinemachine/C# 依赖，暂不整包导入。
- `bridge-gantry-crane`：与现有起重机功能重叠，模型藏在内层 7z，授权未确认。

## 外部资产优先级

1. 传感器座、定位元件、高度控制元件和明确的二系空气弹簧。
2. 转向架装配工装：定位夹具、升降台、吊具、扭矩工具和检验仪表。
3. 成品厂房地板、墙体、门窗、贴花与远景堆场。
4. 真实 HMI 测试柜、线缆、警示牌和工业小道具。
5. 通过身份、单位、尺寸和授权门禁的通用转向架模块源模型；车型专用件属于后续增强项。

## 本批免费背景资产

已从 ArtStation 免费条目领取并放入 `ArtStationIndustrial` 的精选 FBX：

- 模块化叉车（详细版）；
- 工业砂轮机、线缆盘、两种搅拌机和低模混凝土隔离栏；
- 红/白仓储货架通道与远景货架。

来源、下载包校验值和页面许可见
`Assets/RailCraft/ThirdPerson/Art/ThirdParty/Environment/ArtStationIndustrial/SOURCE.md`。
这些资产只进入环境背景层，不替换 14 个工程语义零件，也不改变工位碰撞和流程状态。

## 登录后新增的免费候选

已在本地 staging 目录 `.tmp/free-asset-candidates/` 保留并记录 SHA-256：

- Quaternius `Bouncer.fbx`（CC0）：一系弹性元件低模候选。
- Quaternius `Pipes.fbx`（CC0）：高度控制连杆/气路低模候选。
- BlendSwap `Control valve.blend`（CC0）：含气动执行器、控制阀和差压变送器的高模参考。
- BlendSwap `RR_Barber_Truck_Done_16.blend`（CC BY 3.0）：轮对/转向架研究参考，使用时需署名 `anacraiga`。

其中两个 FBX 已整理到 `.tmp/free-asset-candidates/unity-ready-components/`，等待 Unity 审模后再生成 `.meta` 并接入。它们都只表达通用造型，不承担 SWM-400E1、CR400AF 或 CW-200K 的工程身份。

BlendSwap 的 `Low Poly Steam Train` 页面显示 CC0，但下载包内许可证为 CC BY-NC-SA 3.0；该文件已列为许可冲突并隔离。列车背景优先使用 Quaternius/Poly Pizza 明确标注 CC0 的模块化列车包。

所有外部模型进入主线前仍需记录单位、原点、轴向、LOD、碰撞、材质、SHA-256 和许可。
