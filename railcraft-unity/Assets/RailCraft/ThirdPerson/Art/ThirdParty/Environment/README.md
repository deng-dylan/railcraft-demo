# 第三方环境资产（首批）

本目录保存白盒后首个完整版本使用的精选环境原始资产。当前只完成受控入库，后续应通过项目内 Prefab、URP 材质和布局配置引用，避免直接修改第三方源文件。

## 已入库内容

| 目录 | 模型 | 贴图 | 文档 | 计划用途 |
| --- | ---: | ---: | ---: | --- |
| `OverheadCrane` | 1 FBX | 4 JPG | 1 来源说明 | 检修库主跨桥式起重机 |
| `KenneyTrainKit` | 9 FBX | 1 PNG | 来源说明 + 原始 CC0 许可 | 厂内轨道、轨道散件、平板车和车钩道具 |
| `ShippingContainers` | 2 FBX | 4 TGA | 原始 ReadMe + 来源说明 | 西侧墙边储运背景 |
| `ElectricalSubstation` | 1 FBX + 1 Prefab | 5 TGA | 来源说明 | 北侧电气设备背景 |
| `ArtStationIndustrial` | 9 FBX | 1 PNG 图集 | 来源说明 | 叉车、工业设备、货架和隔离栏背景 |

Kenney Train Kit 原包中没有护栏模型。本批未用近似模型代替，护栏需要从 Factory Kit 现有资产中复用或在后续批次另行补充。

## 入库原则

- 保留原始文件名，方便追溯和后续重新导入。
- Kenney 仅保留 FBX 子集、共用 `colormap.png` 和原始许可文本。
- 未导入 OBJ、GLB、网页、URL、预览图、损坏轨道、坡道以及列车整车模型。
- 第三方原始资产保持只读语义；材质、Prefab、碰撞体和 LOD 放在项目自有目录中维护。
- 发布前核对每个来源目录中的授权说明。

## 第二批审模结论

- Shipping Containers 原始 ReadMe 允许项目使用并要求署名；运行时仅使用模型与四张
  2K PBR 贴图，Standard 材质、天空盒、演示场景和烘焙 EXR 均被排除。
- Electrical Substation 只有 2,120 个三角面，适合作为侧区背景；原 4K 贴图在 Unity
  导入时限制为 2K，原 Built-in 材质由项目 URP 材质替换，许可仍需发布负责人确认。
- Simple Factory 原始整景包含 269,600 个三角面，门与灯约占 92%，57 个网格中只有
  8 个有 UV，且没有许可凭证。源文件已移到本机 `.tmp` 隔离归档，不进入 `Assets`、Git
  或发布包。
- ArtStationIndustrial 三组资源均为免费条目，页面许可为 Extended Commercial License；
  原始下载包不进入 Git，发布前需再次核对账号资源库条款。
