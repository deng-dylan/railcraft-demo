# RailCraft 应用入口

`apps/` 保存当前可运行应用和仍需保留的历史实现。

## 目录

- `railcraft-unity/`：当前开发主线。Unity 第三人称流程白盒与冻结的 Unity v0.1 验收基线共用同一工程，详细入口见 [`railcraft-unity/README.md`](railcraft-unity/README.md)。
- `railcraft-godot/`：冻结的 Godot `v0.1.0-demo` 历史 Demo，仅用于回溯、对照和资料保留。

## 使用规则

- 新玩法、主线场景和当前验收证据继续进入 `railcraft-unity/`。
- 历史 Demo 仅接受可追溯勘误，不承接当前玩法扩展。
- 与主线技术路线不同的探索版本进入仓库前，先评估是否更适合归档到 `prototypes/`。
