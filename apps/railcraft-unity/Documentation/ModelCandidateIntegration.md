# 候选模型接入说明

当前玩法已经在用：

- `Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/BogieAssemblyDemo.fbx`
- `Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/FuxingCarbodyAssemblyDemo.fbx`
- `Assets/RailCraft/ThirdPerson/Art/Models/FinalShowcase/FuxingTrain.fbx`

新模型建议这样接：

| 模型 | 处理建议 | 玩法位置 |
|---|---|---|
| `Y25转向架 欧洲货运火车.stp` | 先转成 FBX，再放到 `Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/Y25Freight/Y25FreightBogie.fbx` | 转向架/构体/落车 |
| `简化铁路转向架（现实无对应）.SLDPRT` | 先导出 FBX，Unity 里重配材质，放到 `VariantModels/TeachingConcept/TeachingConceptBogie.fbx` | 教学版替换件 |
| `地铁转向架（简化）（上色版）.SLDASM` | 先做 Pack and Go，再导出 FBX | 备用车型 |
| `地铁转向架（上色版）.stp.SLDASM` | 先确认真实格式，再转换 | 备用车型 |

现在的接法已经留好：

- 白盒组装：`BogieAssemblyDemoVisualFactory`
- 白盒场景：`WhiteboxSceneBuilder`
- 结算展示：继续保留 `FinalShowcase`

导入后只要文件名落在 `VariantModels` 约定路径里，玩法会优先认新的 FBX；
`Candidates/Y25`、`Candidates/Metro`、`Candidates/Teaching` 也保留为兼容搜索路径。
没有新模型时仍回退到现有示范件，答题、拾取、装配、落车和调试规则保持不变。
