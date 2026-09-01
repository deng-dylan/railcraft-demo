# 组员 CAD 源文件登记

本目录只登记源文件，不把 SolidWorks/STEP 文件直接作为运行时模型加载。当前收到的候选为：

- `Y25转向架 欧洲货运火车.stp`：自包含 STEP，优先转换为 `Y25Freight/Y25FreightBogie.fbx`。
- `地铁转向架（简化）（上色版）.SLDASM`：需要 Pack and Go 或真正的 FBX/GLB 导出。
- `地铁转向架（上色版）.stp.SLDASM`：扩展名仍是 SolidWorks 装配体，先确认真实格式。
- `简化铁路转向架（现实无对应）.SLDPRT`：教学版，导出后在 Unity 中重新配材质并保留教学标识。

源文件保存在队员共享目录；完成授权、单位、原点和导出检查后，再把网格放进
`../VariantModels/` 的对应插槽。这样运行时不会把 CAD 内部实体当碰撞体，也不会把教学件
误标为真实车型。
