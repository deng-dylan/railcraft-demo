# GOOD2 1.0 PC 包复核补记

- 复核日期：2026-07-21
- 原始交付：`deliveries/external/good2-renpy-1.0/release/GOOD2-1.0-pc.zip`
- SHA-256：`AB73CA06B812B472A2B75199AD553C32F397AAFD37E1A86DB92D57AA1460E298`

## 静态审查结果

- 交付包为 Ren'Py `8.5.3.26051504` 的 Windows/Linux 运行包，共 1,619 个条目；其中约 1,502 个位于 `renpy/` 或 `lib/`，属于引擎与 Python 运行时。
- 自定义游戏文件集中于 5 个 `.rpy`/版本文件和一个字体文件；核心 `script.rpy` 为 338 行，包含 10 道单选题。
- 核心脚本未发现 HTTP、socket、requests、urllib、子进程或动态执行调用。
- 脚本会在工作目录写入 `progress.json`、`unlocked_part.json`、`game_complete.json`，并轮询 `unity_trigger.json` 作为外部触发信号。运行时应在隔离目录内解压。
- 包内的 Ren'Py 运行时携带许可证文件；游戏源代码和 `SourceHanSansLite.ttf` 的来源、权属和字体许可未随交付包完整说明。

## 纳入范围

仓库保存了可阅读的 Ren'Py 脚本快照，路径为 [`prototypes/good2-renpy/source/game`](../../prototypes/good2-renpy/source/game)。不纳入运行时、可执行文件、缓存、字节码和字体。该快照用于评审与内容迁移，不能直接作为可构建发行版。

## 结论

GOOD2 保持独立参考原型。它的叙事节奏、积分和知识主题可进入需求讨论；代码、运行时和发布结构不进入 Godot 主线。
