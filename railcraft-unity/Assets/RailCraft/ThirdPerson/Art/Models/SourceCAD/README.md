# 组员 CAD 源文件登记

本目录只登记源文件，不把 SolidWorks/STEP 文件直接作为运行时模型加载。当前收到的候选为：

- `Y25转向架 欧洲货运火车.stp`：自包含 STEP，已转换为 `Y25Freight/Y25FreightBogie.fbx`。
- `贝滕多夫转向架 低速货运列车.STEP`：自包含 STEP，已转换为 `BettendorfFreight/BettendorfFreightBogie.fbx`。
- `地铁转向架（简化）（上色版）.SLDASM`：需要 Pack and Go 或真正的 FBX/GLB 导出。
- `地铁转向架（上色版）.stp.SLDASM`：已确认是 SolidWorks 装配体，等待 Pack and Go。
- `简化铁路转向架（现实无对应）.SLDPRT`：教学版，已由 SolidWorks 导出 AP214 STEP 并转换为 `TeachingConcept/TeachingConceptBogie.fbx`；Unity 中保留教学标识。
- `铁路机车转向架（上色）.stp.SLDASM`：已确认是 SolidWorks 装配体，等待 Pack and Go。

源文件保存在队员共享目录；完成授权、单位、原点和导出检查后，再把网格放进
`../VariantModels/` 的对应插槽。这样运行时不会把 CAD 内部实体当碰撞体，也不会把教学件
误标为真实车型。

使用 `RailCraft > Models > Open CAD Preview Gallery` 可查看已经转换成功的真实网格。预览场景
不会加入 Windows 构建，也不会替换标准实训模型。
