# RailCraft `v0.4.0-internal.1` 内测说明

## 版本范围

本内测候选包含 FLOW-002 材料包流程、ART-001 免费资产扩展、ART-002 原创教学设备，
以及 `DEBUG-001` 受控调试模式。当前仍属于内部测试用途，第三方授权总表、目标机性能和
异机断网门禁完成前不得作为公开发布包。

## 调试模式入口

调试入口采用两层门禁：

1. 仅在进程启动参数包含 `-railcraft-internal-debug` 时进入待授权状态；
2. 在主界面按 `Ctrl+Alt+Shift+F10` 完成运行时解锁。

普通双击 EXE、只按 F10、只按组合键、只添加其他 `-debug` 参数都不会打开调试模式。
解锁成功后左上角持续显示“内部调试模式”水印；F10 可开关调试面板。该机制用于避免
普通用户误触和常规演示时暴露入口，不构成密码学安全或反篡改保护。

推荐从项目目录运行：

```powershell
pwsh -NoProfile -File tools/Start-RailCraftInternalTest.ps1
```

## 调试能力

- 解锁并领取全部 5 个材料包；
- 推进至落车完成；
- 完成教学故障、处理、检验和复测；
- 重置本轮内测进度；
- 每次动作写入 `RAILCRAFT_INTERNAL_DEBUG_ACTION` 日志并自动保存。

调试推进仍调用正式会话 API，不直接修改私有字段。调试产生的存档只用于内测，正式演示
前应通过面板重置，或从主菜单开始新一轮标准实训。

## 构建

Unity 菜单：`RailCraft > Third Person Whitebox > Build Internal Test Windows x86_64`

命令行入口：

```text
RailCraft.ThirdPerson.Editor.WhiteboxWindowsBuild.BuildInternalTestFromCommandLine
```

输出：`Builds/InternalTest/RailCraftInternalTest.exe`。内测构建启用 Unity Development Build，
仍要求调试模式的启动参数和组合键双门禁。

构建成功后打包：

```powershell
pwsh -NoProfile -File tools/New-WhiteboxReleasePackage.ps1 `
  -Version v0.4.0-internal.1 -PackageKind InternalTest
```

## 验收门禁

1. 完整 EditMode 测试通过；
2. 内测 Windows 构建 0 error；
3. 不带参数启动时调试模式不可见；
4. 带参数但未按组合键时调试模式不可见；
5. 双门禁后四项调试动作可用且水印持续可见；
6. 标准流程 Player 烟测通过并生成当前资产截图；
7. ZIP、文件清单和 SHA-256 写入内测证据目录。
