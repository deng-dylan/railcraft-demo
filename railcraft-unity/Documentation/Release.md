# RailCraft 标准工单实训 v0.3.0-preview.1

- 发行定位：竞赛演示与内测预发布
- 平台：Windows x86_64
- 运行时：Unity 6000.3.21f1、URP、Input System、uGUI
- 常规入口：复兴号标准工单 RC-EMU-01
- 发布页：[GitHub Releases](https://github.com/deng-dylan/railcraft-demo/releases/tag/v0.3.0-preview.1)

## 本版主线

本版本将 Unity ThirdPerson 标准实训作为唯一的日常体验、开发和验收入口。玩家在第三人称工厂场景中完成答题解锁、零件领取、分级装配、落车、教学故障处理、检验、复测和结算。

- 58 道题：50 道四选一与 8 道判断题。
- 14 个答题与零件领取工位、14 个可入库零件。
- 6 个装配节点与统一的 23 步进度。
- 主菜单、继续实训、暂停、自动存档、重玩、成绩结算和工程知识图鉴。
- 完成标准实训后可进入复兴号八编组出厂展示。

完整范围见 [ThirdPersonWhitebox.md](ThirdPersonWhitebox.md)。

## 下载与运行

从 GitHub 预发布页下载 RailCraft-v0.3.0-preview.1-windows-x64.zip，完整解压后运行 RailCraftWhitebox.exe。请保留 RailCraftWhitebox_Data、MonoBleedingEdge、D3D12、UnityPlayer.dll 和 Unity 生成的全部同级文件。

应用支持离线运行。推荐使用 Windows 10 或 Windows 11 x64，并确保显卡驱动支持 Unity Windows Player。

## 构建与打包

从最终提交执行 Unity 菜单 RailCraft > Third Person Whitebox > Build Windows x86_64，完成后在仓库根目录运行：

~~~powershell
pwsh -NoProfile -File tools/New-WhiteboxReleasePackage.ps1 -Version v0.3.0-preview.1
~~~

脚本会校验 Unity 运行目录，生成完整 Windows ZIP、离线说明副本和 SHA-256 校验文件；输出位于 railcraft-unity/ReleasePackages/，该目录不进入 Git。

## 预发布验证

2026-08-26 已从本发行分支完成 Windows 构建、EditMode 和内置标准工单烟测：EditMode 199/199 通过，Windows 构建 0 warning / 0 error，Player 输出 RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED。完整记录见 [v0.3.0-preview.1 验证证据](../Artifacts/Whitebox/Releases/v0.3.0-preview.1/acceptance-report.md)。

完整 Windows ZIP、EXE 与 SHA-256 校验文件由发行打包工具生成，并随 GitHub 预发布附件提供。

## 已知边界

- 内容用于教学流程和结构示范，不用于真实动车组作业指导、扭矩标准、公差、检修结论或安全决策。
- 自由抓取、零件旋转与距离/角度吸附尚未提供。
- 部分视觉资产仍为白盒或结构示范资产；正式模型、工程知识来源、来源声明与再分发范围仍需复核。
- 当前版本尚缺目标机性能采样与异机断网人工走查。
- 当前普通玩家入口只开放 RC-EMU-01；地铁、Y25 与教学概念件属于开发者扩展登记。

## 资产与使用说明

Factory Kit 的许可信息随资源保留；其它模型、题库、工程资料和第三方资源的来源与再分发范围以项目审核记录为准。在相关复核完成前，请勿单独提取、再分发或将这些资产用于产品化、工程化用途。

完整的发布前工作与风险说明保留在 [ReleaseReadiness.md](ReleaseReadiness.md)。历史 Unity v0.1、Godot Demo 和原型资料见 [仓库历史归档](../../../docs/archive/README.md)。
