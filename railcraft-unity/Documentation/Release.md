# RailCraft Art Alpha `v0.4.0-art-alpha.5`

- 发行定位：白盒之后的受控视觉生产批次，用于内部评审、竞赛预演和小范围体验
- 平台：Windows x86_64
- 运行时：Unity 6000.3.21f1、URP、Input System、uGUI
- 常规入口：通用高速动车组标准工单 `RC-EMU-01`
- 当前证据：[Art Alpha 验收记录](../Artifacts/Whitebox/ArtAlpha/acceptance-report.md)

## 本批内容

玩家在第三人称工厂中完成知识确认、材料领取、分级装配、落车、教学故障处理、检验、
复测和结算。当前域模型保留 14 个子部件 ID，界面和工艺区按 5 个材料包工位与 1 个流程模块组织；题库
保留 58 题，核心流程精选 10 题，其余用于备用题和知识图鉴。

本批已接入工人角色与动画、PBR 桥式起重机、维修轨道、集装箱、变电站、工业背景道具、
Kenney 模块化工位、CW-200K 参考转向架和牵引拉杆视觉。详细范围见
[ArtAlpha.md](ArtAlpha.md) 与 [ThirdPersonWhitebox.md](ThirdPersonWhitebox.md)。

复兴号、和谐号只作为原创外观风格参考。车体和 CW-200K 转向架属于展示/通用教学资产，
当前版本不宣称 CR400AF、SWM-400E1 或其他具体车型的工程复刻。

## 下载与运行

完整 Windows Player 由构建入口生成：

1. 在 Unity 中执行 `RailCraft > Third Person Whitebox > Build Windows x86_64`；
2. 保留 `Builds/Whitebox/` 的全部文件；
3. 运行 `RailCraftWhitebox.exe`。

运行时离线工作，不需要浏览器登录或在线下载。推荐 Windows 10/11 x64，并使用支持
Unity Windows Player 的显卡驱动。

## 构建与验证

```powershell
pwsh -NoProfile -File tools/New-WhiteboxReleasePackage.ps1 -Version v0.4.0-art-alpha.5
```

打包脚本输出位于 `railcraft-unity/ReleasePackages/`（该目录被 Git 忽略）。正式提交前
必须同时完成 EditMode、Windows 构建、Player 烟测、目标机性能和异机断网走查。

## 已知边界

- 厂房、工位和部分零件仍包含白盒或通用示范几何；正式 LOD、碰撞、材质和纹理仍在完善。
- 未核验的车型参数、扭矩、公差、绝缘、气密和检修数值不作为作业标准展示。
- 工人、UI 音效、起重机、变电站和部分背景道具的公开再分发范围仍需逐项核验。
- 目标机 1920×1080 帧率、1% low、显存、加载时间和异机兼容性尚未形成最终基线。
- 未授权、盗版、破解或去水印资源不进入项目或提交包。

## 资产边界

Factory Kit 和 Train Kit 随精选运行时资产保留来源文本。所有其他模型、题库和第三方
资源的来源、许可证、署名与再分发范围以 [资产审计](ArtAssetAudit.md) 和来源清单为准。
完成审核前，候选资源只用于本地观察或内部集成。

## 历史版本

旧 Unity 固定视角、Godot 和其他 Demo 已退休。恢复方式见
[仓库归档索引](../../docs/archive/README.md)；历史版本不属于当前发行入口。
