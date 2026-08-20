# RailCraft 原型归档

`prototypes/` 保存没有进入当前主线的独立原型源码快照和说明，用于评审、回溯和方案比较。

## 当前内容

- `good2-renpy/`：GOOD2 Ren'Py 原型快照。
- `high-speed-rail-factory-godot-4.6.3/`：外部 Godot 4.6.3 Demo 的本地可审计包装。

## 维护规则

- 原型目录保留可复核的源码快照、运行说明、来源和采用边界。
- 是否进入主线，以 `docs/reviews/` 评审结论和 `docs/decisions/` 正式决策为准。
- 缺少再分发授权的外部源码继续按隔离规则处理，不直接混入 `apps/`。
