# RailCraft v0.3.0-preview.1 预发布验证记录

- 验证日期：2026-08-26
- 版本定位：Windows x86_64 竞赛演示与内测预发布
- Unity：6000.3.21f1
- 启动场景：Assets/RailCraft/ThirdPerson/Scenes/ThirdPersonWhitebox.unity
- 可选完成后展示：Assets/RailCraft/ThirdPerson/Scenes/FinalShowcase.unity

## 自动化结果

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| 仓库布局与主线策略 | 9 项通过 | tools/Test-RepositoryLayout.ps1 |
| Unity EditMode | 199/199 通过，0 失败、0 跳过 | editmode.xml |
| Windows x86_64 构建 | 成功，0 warning、0 error，207817543 字节 | build.log |
| 成品标准工单烟测 | RAILCRAFT_WHITEBOX_SMOKE_SUCCEEDED | player-smoke.log |

## 发行附件

- Windows 包：RailCraft-v0.3.0-preview.1-windows-x64.zip
- Windows 包大小：88607444 字节
- 可执行文件：RailCraftWhitebox.exe
- ZIP 与 EXE 的 SHA-256 见 checksums.txt。

完整 Windows 包与校验文件由 tools/New-WhiteboxReleasePackage.ps1 生成，位于本地 ReleasePackages 目录，并作为 GitHub 预发布附件上传。

## 发行边界

本记录证明当前代码可构建并通过自动闭环验证。工程知识来源、模型与第三方资源的授权、目标机性能采样和异机断网走查仍按 Release.md 与 ReleaseReadiness.md 中的限制执行。
