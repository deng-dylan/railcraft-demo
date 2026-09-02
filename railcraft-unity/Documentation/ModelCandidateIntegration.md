# 候选模型接入说明

当前玩法已经在用：

- `Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/BogieAssemblyDemo.fbx`
- `Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/CW200KReference.fbx`
- `Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/FuxingCarbodyAssemblyDemo.fbx`
- `Assets/RailCraft/ThirdPerson/Art/Models/FinalShowcase/FuxingTrain.fbx`

新模型建议这样接：

| 模型 | 处理建议 | 玩法位置 |
|---|---|---|
| `TRAIN.stp`（顶层产品 `CW-200K`） | 已完成 OpenCascade → GLB → Blender FBX、硬件精简、减面和材质分层 | 构体完成态/落车完成态参考 |
| `Y25转向架 欧洲货运火车.stp` | 已完成 OpenCascade → GLB → Blender FBX，保留 STEP 材质并修正上方向 | Unity 审模/Y25 扩展方案 |
| `贝滕多夫转向架 低速货运列车.STEP` | 已完成 OpenCascade → GLB → Blender FBX，修正上方向 | Unity 审模/低速货运参考 |
| `简化铁路转向架（现实无对应）.SLDPRT` | 先导出 FBX，Unity 里重配材质，放到 `VariantModels/TeachingConcept/TeachingConceptBogie.fbx` | 教学版替换件 |
| `地铁转向架（简化）（上色版）.SLDASM` | 先做 Pack and Go，再导出 FBX | 备用车型 |
| `地铁转向架（上色版）.stp.SLDASM` | 已确认是 SolidWorks 装配体；先做 Pack and Go，再导出 FBX | 备用车型 |
| `铁路机车转向架（上色）.stp.SLDASM` | 先做 Pack and Go，再导出 FBX | 机车转向架参考 |

当前接法：

- 白盒组装：`BogieAssemblyDemoVisualFactory`
- 完成态参考：`Cw200kReferenceVisualFactory`
- 白盒场景：`WhiteboxSceneBuilder`
- 结算展示：继续保留 `FinalShowcase`

导入后只要文件名落在 `VariantModels` 约定路径里，玩法会优先认新的 FBX；
`Candidates/Y25`、`Candidates/Metro`、`Candidates/Teaching` 也保留为兼容搜索路径。
没有新模型时仍回退到现有示范件，答题、拾取、装配、落车和调试规则保持不变。

独立审模入口为 `RailCraft > Models > Open CAD Preview Gallery`。当前展台展示 Y25 与
贝滕多夫两份真实 STEP 转换结果；SolidWorks 文件会显示为待 Pack and Go 状态，不使用占位
网格冒充原模型。
