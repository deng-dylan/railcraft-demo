# v0.4.0-art-alpha.4 资产接入增量记录

- 验证日期：2026-09-03
- Unity：6000.3.21f1
- 本增量范围：Kenney 工位视觉组合、CW-200K 明确牵引拉杆组、Kenney 地板面板、
  ArtStationIndustrial 背景层、完整回归

## 已完成

- 14 个答题工位使用 `machine-bed`、`screen-panel-wide`、`button-floor-square`、
  `lever-single` 组合视觉层。
- 4 个子装配台、1 个构体台、3 个调试台复用同一模块化视觉工厂。
- 牵引拉杆从 CW-200K 参考 FBX 的 `QIANYINLAGAN*` 组提取并接入奖励件视觉管线。
- 所有第三方工位模型保持视觉专用；交互触发器仍由原工位根节点负责。
- `floor-large` 四块面板覆盖厂房地面，使用项目深色材质，避免直接套用 Kenney 色彩图集。
- ArtStationIndustrial 背景层包含叉车、砂轮机、电缆盘、搅拌机、货架和隔离墩。
- 未确认的传感器座、定位元件、高度控制元件没有被错误替换。

## 验证结果

| 项目 | 结果 |
| --- | --- |
| Unity EditMode | 228/228 通过 |
| Windows 构建 | 382,130,855 bytes，0 warning、0 error |
| 本地 Player 烟测 | `RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED` |
| 独立 ZIP 烟测 | `RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED` |
| Art Alpha ZIP | 见 `ReleasePackages/RailCraft-v0.4.0-art-alpha.4-windows-x64.zip` |
| Player SHA-256 | `48EFAB523AA684C653BD1254A6962D3410127B5C02DC1310F6F16F4810666556` |
| ZIP SHA-256 | `6953FEB4E3FC1D7D185489F745EAFCB5A6CDCB0036ECB990000F6A79CBB62353` |

## 仍需外部资产或人工确认

1. 传感器座、高度控制元件；定位元件和二系空气弹簧需先确认 CW-200K 的对应组。
2. 真实转向架装配工装、定位夹具、升降台、吊具、扭矩工具和检验仪表。
3. 成品厂房地板、墙体、门窗、地面磨损/贴花和厂区远景。
4. 真实 HMI 测试柜、线缆、警示牌和工业小道具。
5. CR400AF/SWM-400E1 车型身份明确且具备授权的工程来源模型。
6. 所有新增角色、音效、起重机和变电站的发布授权凭证。
7. 完整环境完成后的 1920×1080 目标机性能采样。

详细逐项矩阵见 [`Documentation/ArtAssetAudit.md`](../../Documentation/ArtAssetAudit.md)。
