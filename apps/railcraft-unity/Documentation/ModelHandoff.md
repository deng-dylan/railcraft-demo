# SWM-400E1 模型交接与 Unity 运行时规范

本文定义 RailCraft v0.1 的 CAD 源文件、交换几何、运行时网格和 Unity prefab 之间的交接边界。任何生产模型进入 `Assets/RailCraft/Art/Models/Production` 前，都必须通过本规范的身份、单位、尺寸和坐标门禁。

## 标准资产链

| 层级 | 格式与位置 | 用途 |
|---|---|---|
| source CAD | `SLDPRT`，保留在 `deliveries/` 的 release 存档 | 原始可编辑工程来源，不由 Unity 直接导入 |
| neutral geometry | STEP AP242 或 Parasolid `.x_t` | 尺寸和实体几何核验的交换基准 |
| runtime mesh | FBX | Unity 渲染、LOD 和材质绑定 |
| Unity prefab | `.prefab` | 稳定运行时契约、碰撞体和视觉替换入口 |

规范值如下：

```text
source CAD: SLDPRT retained in deliveries
neutral geometry: STEP AP242 or Parasolid x_t
runtime mesh: FBX
source length unit: millimetre
Unity world unit: metre
object origin: axle centre
local X: axle direction
local Y: up
local Z: vehicle forward
Unity prefab root: position 0, rotation 0, scale 1
mesh naming: swm400e1_<subsystem>_<side>_<lod>
material naming: mat_<function>_<finish>
```

毫米到 Unity 米制单位的换算只能在受控导入步骤完成。最终 prefab 根节点必须保持 `position (0,0,0)`、`rotation (0,0,0)`、`scale (1,1,1)`，不能依靠根节点缩放修正错误尺寸。

## 每次交付的必填元数据

每份模型交付必须同时记录：

- 零件身份与适用车型说明；
- 修订号；
- 作者或责任人；
- 导出日期；
- 源长度单位；
- 名义尺寸及其工程图或测量依据；
- 预期装车数量；
- 原点和三个局部轴方向；
- 源文件、交换文件和可选 FBX 的 SHA-256。

车轮至少需要名义轮径、轮宽和轮毂孔径。身份说明必须明确属于通用铁路车轮、参考件，或已经按 SWM-400E1 适配。

## 当前车轮交付请求

`deliveries/models/swm-400e1-wheel-v1/` 当前只包含候选 `SLDPRT` 和只读预览。进入转换前仍需提供：

1. STEP AP242 或 Parasolid `.x_t`；
2. 毫米单位确认；
3. 名义轮径、轮宽和轮毂孔径；
4. 车轴中心原点与 X/Y/Z 方向确认；
5. 通用/参考/SWM-400E1 适配身份声明；
6. 可选的三角化 FBX，保留法线且不合并到转向架总成。

不得通过图片生成、语言模型估算或预览图反推缺失工程尺寸。

## 转换与尺寸门禁

收到交换格式后，按以下顺序处理：

1. 在 CAD/DCC 工具中打开中性实体，确认文件单位为毫米。
2. 用交付的三个名义尺寸核对实体包围尺寸和对应特征。
3. 保留踏面、轮缘和轮毂孔的识别轮廓，删除构造几何、隐藏重复体和无运行价值的细节。
4. 生成 LOD0、LOD1、LOD2；各级共享同一原点、轴向和材料槽语义。
5. 以局部 X 沿车轴、Y 向上、Z 沿车辆前进方向导出 FBX。
6. 导入 Unity 后按米制复测，不在 prefab 根节点上保留补偿缩放或旋转。
7. 使用简化凸包或复合基础碰撞体。视觉高模不能直接作为非凸动态 MeshCollider。
8. 记录导入后尺寸、三角面数、材质槽、LOD 阈值和校验哈希。

任一交付名义尺寸与 Unity 导入后回算值相差超过 `1 mm`，立即拒收资产并退回重导。身份、单位或轴向未确认时同样拒收。

## Unity 车轮契约

通过门禁后创建：

```text
Assets/RailCraft/Art/Models/Production/Bogie/Wheel/wheel.fbx
Assets/RailCraft/Art/Prefabs/Modules/WheelRuntime.prefab
```

`WheelRuntime.prefab` 根节点要求：

- `ModelContract.assetKey == "mesh.wheel.production"`；
- `ModelContract.authoredAtMeterScale == true`；
- `localAxleDirection == Vector3.right`，`localUpDirection == Vector3.up`；
- 根节点位置零、旋转零、缩放一；
- 含 LOD0、LOD1、LOD2；
- 含简化碰撞体，不含 `DraggableModule`；
- 网格名使用 `swm400e1_wheel_<side>_<lod>`；
- 材质名使用 `mat_<function>_<finish>`；
- 材质使用 URP Lit 或兼容 shader，并提供 `_BaseColor`/`_Color`、`_EmissionColor` 与有效的本地 `_EMISSION` keyword；
- 若材质从模型中抽取，统一放到 `Assets/RailCraft/Art/Materials/Production/`。

## 只替换视觉子节点

生产车轮不会新增流程步骤。每个轮对轴箱模块下放置两个 `WheelRuntime` 实例，并继续使用已批准的车轴、轴箱占位或生产模型。

当前项目对应 prefab 为：

```text
Assets/RailCraft/Art/Prefabs/Modules/module_wheelset_axlebox_a.prefab
Assets/RailCraft/Art/Prefabs/Modules/module_wheelset_axlebox_b.prefab
```

替换时只编辑视觉子层，必须保持：

- `DraggableModule.stepId`；
- 模块根 `ModelContract.assetKey`；
- prefab 根节点 transform；
- 根交互 collider；
- snap anchor；
- drop target ID；
- `flow.v1.json` 中所有 step ID 与 assetKey。

完成替换后先运行 `RailCraft/Configure Factory Presentation`，它会为 Catalog 中 `DraggableModule.VisualRoot` 下的材质启用 property-block emission；随后运行 `RailCraft/Validate Production Asset Budgets`、生产车轮契约、目录验证、拖放测试和完整 48 题/15 步流程。内容 JSON 不随模型替换改动。

## 接收记录模板

```text
part identity:
revision:
author:
export date:
intended quantity:
source unit: millimetre
nominal diameter (mm):
nominal width (mm):
nominal hub-bore diameter (mm):
origin: axle centre
local X: axle direction
local Y: up
local Z: vehicle forward
neutral file SHA-256:
runtime FBX SHA-256:
Unity measured diameter (mm):
Unity measured width (mm):
Unity measured hub-bore diameter (mm):
reviewer:
decision: accepted / rejected
```
