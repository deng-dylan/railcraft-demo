# RailCraft Demo 开发说明

## 固定版本

| 组件 | 版本 |
|---|---:|
| Godot Standard（Compatibility） | 4.6.3 stable |
| GUT | 9.6.0 |
| Python | 3.12.13 |
| uv | 0.11.8 |
| gdtoolkit | 4.5.0 |
| pre-commit | 4.6.0 |

依赖版本分别由 `.python-version`、`pyproject.toml`、`uv.lock` 和已提交的 `addons/gut/` 固定。请勿用浮动版本替换。

## 准备环境

1. 安装 [Godot 4.6.3 Standard Windows x64](https://godotengine.org/download/archive/4.6.3-stable/) 和同版本 Export Templates。
2. 从 [uv 0.11.8 Release](https://github.com/astral-sh/uv/releases/tag/0.11.8) 获取 Windows x64 程序，并确认 `uv --version` 输出 `0.11.8`。
3. 在仓库根目录执行：

```powershell
uv python install 3.12.13
uv sync --frozen
```

本项目没有 Python 运行时依赖；Python 环境只用于 GDScript 静态检查和提交钩子。

## 日常验证

```powershell
uv run --frozen gdformat --check scripts tests
uv run --frozen gdlint scripts tests
uv run --frozen pre-commit run --all-files
```

在受限环境中可将缓存放在仓库的忽略目录：

```powershell
$env:UV_CACHE_DIR = "$PWD\.uv-cache"
$env:UV_PYTHON_INSTALL_DIR = "$PWD\.uv-python"
$env:PRE_COMMIT_HOME = "$PWD\.pre-commit-cache"
```

## Godot 导入与测试

以下示例假设 Godot 控制台程序位于 `$env:GODOT_BIN`：

```powershell
& $env:GODOT_BIN --headless --editor --path . --quit
& $env:GODOT_BIN --headless --path . --quit-after 5
& $env:GODOT_BIN --headless --path . --script res://addons/gut/gut_cmdln.gd `
  -gdir=res://tests -ginclude_subdirs -gexit
```

只运行单个模块时，将测试目录和文件前缀传给 GUT：

```powershell
& $env:GODOT_BIN --headless --path . --script res://addons/gut/gut_cmdln.gd `
  -gdir=res://tests/unit -gprefix=test_quiz_manager -gexit
```

## Windows x64 导出

先在 Godot 中安装 `4.6.3.stable` Export Templates，再执行：

```powershell
New-Item -ItemType Directory -Force builds\windows | Out-Null
& $env:GODOT_BIN --headless --path . --export-release "Windows Desktop" `
  builds\windows\RailCraft-Demo.exe
Compress-Archive -Path builds\windows\* `
  -DestinationPath builds\RailCraft-Demo-windows-x64.zip -Force
```

`export_presets.cfg` 使用 Windows Desktop x64、嵌入 PCK，并排除测试、开发工具、缓存和文档。构建目录已被 Git 忽略。

## 目录

- `data/`：9 道题、9 个零件、3 个组件和整车配方；
- `scripts/domain/`：答题、库存与装配领域规则；
- `scripts/infrastructure/`：内容加载、内容校验与资产契约校验；
- `scripts/flow/`：8 状态体验流程；
- `scripts/presentation/`：UI、3D 装配和动画协调；
- `scenes/`：主场景、UI、列车根与零件场景；
- `tests/`：单元、集成、冒烟和夹具；
- `.github/workflows/`：可复用质量工作流和 Windows 标签构建。

## 内容与模型替换

JSON 字段和约束见 `doc/detailed-design.md`。替换零件场景时须保留 `PartActor` 根脚本、`VisualRoot`、`ClickArea/CollisionShape3D` 及对应的 `Marker3D` 吸附目标。启动资产校验会在进入开始页前拒绝不符合契约的资源。

`.glb`、`.gltf`、`.blend` 和常见 SolidWorks 文件已预留 Git LFS 规则。提交外部资产前还需补充 `doc/sources.md` 中的来源与许可。
