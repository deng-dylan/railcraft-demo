# RailCraft `v0.4.0-internal.2` 内测说明

## 版本范围

本内测候选包含 FLOW-002 材料包流程、ART-001 免费资产扩展、ART-002 原创教学设备，
以及 `ART-004` 操作台替换、`DEBUG-001` 受控调试模式与 `DEBUG-002` 运行时碰撞箱显示。
13 张工作台（5 张知识、4 张模块装配、1 张总装、3 张调试/检验）统一使用用户提供的
Rebel Hideout 操作台，每张生成 3 个非 Trigger BoxCollider，保留各自的交互 Trigger。
两套 URP PBR 材质包含颜色、法线、金属/光滑度和 AO，装配台尺寸匹配现有槽位。
资产属于展示风格，CC BY 4.0 作者署名、原站链接与修改说明随 ZIP 附于 `ThirdPartyNotices/`。
当前仍属于内部测试用途，第三方授权总表、目标机性能和
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

解压包可运行 `StartInternalDebug.cmd`，再按上述组合键解锁。直接双击 EXE 为普通模式。
碰撞箱使用 Player 可见的运行时线框：青色为静态碰撞体，黄色为刚体碰撞体；关闭开关或
停用控制器即清理线框，不显示 Trigger、禁用组件或隐藏对象。

## 调试能力

- 解锁并领取全部 5 个材料包；
- 推进至落车完成；
- 完成教学故障、处理、检验和复测；
- 重置本轮内测进度；
- 显示/隐藏实际启用的非 Trigger Collider 世界包围盒；
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
  -Version v0.4.0-internal.2 -PackageKind InternalTest
```

## 验收门禁

1. 完整 EditMode 测试通过；
2. 内测 Windows 构建 0 error；
3. 不带参数启动时调试模式不可见；
4. 带参数但未按组合键时调试模式不可见；
5. 双门禁后四项调试动作可用且水印持续可见；
6. 标准流程 Player 烟测通过并生成当前资产截图；
7. ZIP、文件清单和 SHA-256 写入内测证据目录。

本批证据目录：`Artifacts/Whitebox/InternalTest/Internal2/`。
本批已通过 221/221 EditMode、Windows Development Build（0 warning / 0 error）、
标准 Player 烟测与独立 Player 碰撞线框截图检查。解压包最终结果见同目录 `acceptance-report.md`。
工作台 Player 截图参数：`-whitebox-smoke-workbench-directory=<绝对目录>`。
烟测还会核对 13 张新模型、39 个实体 BoxCollider、13 个保留的交互 Trigger 和有效贴图。
烟测使用专用存档键 `railcraft.whitebox.smoke.session.v2`，与普通试玩进度隔离。
调试截图专用参数 `-whitebox-smoke-debug-screenshot=<绝对路径>` 仅在 `-whitebox-smoke`
和 `-railcraft-internal-debug` 同时存在时运行自动验收入口；普通试玩仍按组合键解锁。
