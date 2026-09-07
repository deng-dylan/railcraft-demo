# 可玩车型方案模型插槽

这些目录是现有玩法的模型插槽。把经过检查的 Unity 网格放到对应路径后，执行
`RailCraft > Third Person Whitebox > Rebuild Scene`，模型会出现在答题/拾取/装配流程的
“当前玩法方案参考模型”工位，并在落车完成后沿用同一方案的视觉标识。

| 方案 | Unity 网格路径 | 当前状态 |
| --- | --- | --- |
| Y25 欧洲货运转向架 | `Y25Freight/Y25FreightBogie.fbx` | **真实 STEP 网格已导入** |
| 贝滕多夫低速货运转向架 | `BettendorfFreight/BettendorfFreightBogie.fbx` | **真实 STEP 网格已导入** |
| 地铁上色转向架 | `MetroColored/MetroColoredBogie.fbx` | 等待 Pack and Go 或 FBX/GLB |
| 地铁简化转向架 | `MetroSimplified/MetroSimplifiedBogie.fbx` | 等待 Pack and Go 或 FBX/GLB |
| 教学概念转向架 | `TeachingConcept/TeachingConceptBogie.fbx` | **SolidWorks AP214 网格已导入** |
| 铁路机车上色转向架 | `LocomotiveColored/LocomotiveColoredBogie.fbx` | 等待 Pack and Go 或 FBX/GLB |

CAD 源文件不直接进入 Unity 运行时。导入前请保留毫米单位、安装原点、车轮底面基准，
并删除碰撞体、相机和灯光；运行时碰撞仍由工位触发器负责。

地铁与 Y25 网格会优先保留组员已做好的材质颜色；教学概念件会在 Unity 中使用独立
教学材质，因此 SolidWorks 教学版无法改色不会限制游戏内呈现。

在 Unity 菜单执行 `RailCraft > Models > Open CAD Preview Gallery`，可以在独立展台场景中
查看当前已导入的模型。该场景仅用于审模，不进入正式发行的 Build Settings。
