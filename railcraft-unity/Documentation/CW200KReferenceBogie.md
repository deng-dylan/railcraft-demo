# CW-200K 客车转向架完成态参考模型

当前标准实训在转向架构体完成后，使用 CW-200K 客车转向架参考模型替换原低模完成态；落车完成车辆同样使用两套该模型。答题、零件、子总成、库存、吸附和调试规则保持不变。

## 视觉目标

- 完成态具有完整构架、轮对、轴箱、悬挂、制动和牵引结构。
- 保留部件级灰阶 PBR 材质，不再对整副转向架覆盖青色训练材质。
- 删除高密度紧固件并减面，使每套模型控制在约 43 万三角面。
- 轨面原点、中心和车辆安装面均由 FBX 锚点定义。

## 模型身份

该资产来自 STEP 顶层产品 CW-200K，属于客车转向架参考模型。它用于提升训练完成态的视觉可信度，不构成 CR400AF、复兴号或 SWM-400E1 的工程模型、尺寸依据或作业指导。

运行时资产：

Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/CW200KReference.fbx

转换清单：

Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/CW200KReference.manifest.json

转换脚本：

Tools/Blender/export_cw200k_reference.py

源文件与授权登记见：

deliveries/models/cw200k-bogie-v1/README.md

## 当前组合方式

- 零件工位和基础子总成：继续使用原语义低模，便于逐件显隐和交互。
- 构体装配完成：隐藏已安装低模层，显示 CW-200K 完成态参考模型。
- 落车完成：一节复兴号中间车体视觉搭配两套 CW-200K 参考模型。
- 最终八编组展示：继续使用原完整复兴号 FBX，不从装配场景复制模型。

该组合消除了完成态仍像积木的问题，同时保留既有玩法。后续若取得目标动车组的正式部件级 CAD，应继续将每个子总成替换为同源网格。
