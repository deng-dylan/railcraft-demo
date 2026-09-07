# RailCraft ThirdPerson 模型交接规范

本文定义当前 Art Alpha 的 CAD 源文件、交换几何、运行时网格和 Unity Prefab 门禁。模型
身份默认采用通用高速动车组语义；只有来源、单位、尺寸、接口和责任人全部核验后，才可
标记为具体车型工程件。

## 资产身份

每份交付先选择一种身份：

| 身份 | 可表达内容 | 禁止表述 |
| --- | --- | --- |
| 展示风格 | 外观、色彩、镜头和远景 | 具体车型尺寸、接口和作业参数 |
| 通用教学件 | 部件名称、相对位置、装配关系 | 具体车型合格标准或工艺结论 |
| 车型专用工程件 | 已核验车型的几何、接口和工艺语义 | 超出来源适用范围的推断 |

CW-200K 参考转向架、队员车体和教学概念件当前属于前两层。它们不能被描述为
CR400AF、SWM-400E1 或其他车型的正式工程模型。

## 标准资产链

| 层级 | 格式与位置 | 用途 |
| --- | --- | --- |
| source CAD | 私有交付存储中的 SLDPRT/SLDASM 等 | 作者原始工程来源，Unity 不直接导入 |
| neutral geometry | STEP AP242 或 Parasolid `.x_t` | 单位、实体和名义尺寸核验 |
| runtime mesh | FBX/GLB | Unity 渲染、LOD 和材质绑定 |
| Unity Prefab/生成器 | `Assets/RailCraft/ThirdPerson/` | 稳定运行时契约、碰撞和视觉替换 |

推荐坐标约定：

```text
source length unit: millimetre (CAD) or explicitly documented
Unity world unit: metre
local X: axle direction for bogie parts
local Y: up
local Z: vehicle forward
prefab root: position 0, rotation 0, scale 1
```

毫米到米的换算只能在受控导入步骤完成。最终 Prefab 根节点不能依靠补偿缩放或旋转修正
错误资产；导入修正应写在模型 Importer、视觉子节点或确定性工厂中。

## 每次交付的必填元数据

- 资产身份、部件/模块名称和适用车型（如有）；
- 修订号、作者/责任人、导出日期和来源许可；
- 源长度单位、原点和三个局部轴方向；
- 名义尺寸及工程图、标准或测量依据；
- 预期装车数量与所在模块；
- 源文件、中性文件和运行时网格 SHA-256；
- 导入后三角面、材质槽、纹理分辨率和加载规模；
- LOD0/1/2 目标与屏幕切换阈值；
- 简化碰撞体方案和是否参与交互；
- 审核人、审核日期和 accepted/rejected/pending 决定。

## 转换与尺寸门禁

1. 打开中性实体并确认单位；
2. 用交付的名义尺寸核对包围盒和关键特征；
3. 删除构造几何、隐藏重复体和无运行价值的内部细节；
4. 保留轮缘、踏面、安装面、吊点等需要表达的识别轮廓；
5. 生成共享原点和轴向的 LOD0、LOD1、LOD2；
6. 导入 Unity 后按米制复测，并记录误差；
7. 使用 Box/Capsule/Convex 或复合基础碰撞体，避免把高模直接用作非凸动态 MeshCollider；
8. 使用 URP Lit 兼容材质，并限制材质槽、透明面和纹理分辨率；
9. 在目标模块的视觉子节点或视觉工厂中接入，保持 `PartId`、`ModuleId`、吸附点和存档兼容；
10. 运行 EditMode、Windows 构建和 Player 烟测，更新 Art Alpha 证据。

车型专用交付的名义尺寸与 Unity 回算值超过批准误差，或身份、单位、轴向、授权任一项
缺失时，资产保持 `pending`，不能进入正式展示文案。

## 模块接入原则

当前玩家流程按 6 个工程模块组织，14 个内部子部件 ID 只承担配方、显隐和旧存档兼容。
视觉资产可以提供整模块网格，并通过稳定子节点表达需要显隐的部位。题目数量不会新增
模型或工位。

运行时路径示例：

```text
Assets/RailCraft/ThirdPerson/Art/Models/<AssetFamily>/<asset>.fbx
Assets/RailCraft/ThirdPerson/Art/ThirdParty/<Category>/<Asset>/SOURCE.md
Assets/RailCraft/ThirdPerson/Editor/<Asset>VisualFactory.cs
```

## 接收记录模板

```text
asset identity: showcase-style / generic-teaching / vehicle-specific-engineering
part or module:
applicable vehicle:
revision:
author / owner:
export date:
license and redistribution scope:
source unit:
nominal dimensions and evidence:
origin and axes:
expected quantity:
source SHA-256:
neutral geometry SHA-256:
runtime mesh SHA-256:
Unity measured dimensions:
triangles / materials / textures:
LOD0 / LOD1 / LOD2:
collision:
reviewer:
decision: accepted / rejected / pending
```

图片、渲染图或语言模型输出只能用于视觉参考，不能推导缺失工程尺寸或检验阈值。
