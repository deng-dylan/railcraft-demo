# RailCraft Unity v0.1

RailCraft 的 Unity v0.1 测试版主线位于此目录，历史 Godot Demo 保留在
[`../railcraft-godot`](../railcraft-godot)。

## 环境与依赖

- Unity Editor：`6000.3.21f1`
- Windows Build Support：IL2CPP
- 模板：Universal 3D / URP
- URP：直接依赖与编辑器解析结果均为 `17.3.0`
- 直接依赖：Input System `1.17.0`、Test Framework `1.4.3`、UGUI `2.0.0`

该编辑器将 Test Framework 作为内置包解析为 `1.6.0`；`Packages/manifest.json`
仍保留 v0.1 所需的直接依赖版本 `1.4.3`。

## 项目配置与测试

在编辑器中选择 `RailCraft > Apply Project Configuration`，设置产品名、公司名、
线性色彩空间与 Windows 64 位构建目标。

在已激活许可证的 Windows 环境运行：

```powershell
$UnityExe = 'C:\Program Files\Unity 6000.3.21f1\Editor\Unity.exe'
& $UnityExe -batchmode -nographics `
  -projectPath 'D:\documents\project\gingchuangsai\apps\railcraft-unity' `
  -runTests -testPlatform EditMode `
  -testResults 'D:\documents\project\gingchuangsai\apps\railcraft-unity\TestResults\editmode.xml'
```
