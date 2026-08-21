# 组装阶段转向架结构示范件

## 使用边界

白盒组装流程使用队员提供的转向架模型作为结构示范件。它帮助玩家理解零件收集、吸附、子总成合成和落车顺序；外形、尺寸及部件配置不作为 CR400AF / SWM-400E1 工程依据。

最终出厂展示仍由 `FinalShowcase` 和 `FuxingTrain.fbx` 驱动。本示范资产不得替换复兴号完整编组，也不得在界面中标注为 SWM-400E1 正式模型。

运行时资产：

`Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/BogieAssemblyDemo.fbx`

组装阶段车体示范件：

`Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/FuxingCarbodyAssemblyDemo.fbx`

它从完整复兴号的 `car_02` 中截取一节中间车厢，约 `25.68 × 3.47 × 3.07 m`、
247,854 三角面。零件知识工位使用约 7.5 m 的缩略展示；落车完成车辆使用 1:1
车厢，并复用最终展示的白色车体、深色底架材质，再添加蓝色腰线、车窗和端部门板识别层。
两者都只承担视觉表现，最终展示场景继续直接使用完整 `FuxingTrain.fbx`。

来源清单：

`Assets/RailCraft/ThirdPerson/Art/Models/AssemblyDemo/BogieAssemblyDemo.manifest.json`

## 来源和语义几何可复现转换

- 源文件：队员交付的 `转向架.blend`
- 源 SHA-256：`F0FB4A7BEB5D25122CA654871918EFD7922A152534865AFB5778DC780E13DB3B`
- 转换器：Blender 5.2.0 LTS
- 转换脚本：`Tools/Blender/export_bogie_assembly_demo.py`
- 导出帧：第 1 帧静态装配态
- 输出：35 个网格对象、26,860 个三角面

示例命令：

```powershell
& $Blender --background --factory-startup --disable-autoexec $SourceBlend `
  --python "$Project\Tools\Blender\export_bogie_assembly_demo.py" -- `
  --output "$Project\Assets\RailCraft\ThirdPerson\Art\Models\AssemblyDemo\BogieAssemblyDemo.fbx"
```

脚本会校验源文件哈希、对象清单、连通岛数量、三角面预算和轮轨高度关系。重复运行时 FBX 容器可能写入不同的时间戳和记录 ID；验收以 manifest、网格语义和几何边界一致为准。校验失败时应停止导出并重新检查源文件。

## 几何清理

转换过程会执行以下固定处理：

1. 切换到第 1 帧并冻结该帧的对象变换。
2. 删除 `Camera`、`Light`、`Rail_L`、`Rail_R` 和异常高面的 `Screw`。
3. 将两套 `Wheelset` 按连通岛拆成车轴、车轮和制动盘。
4. 将前轮对车轮和制动盘移动 `(0, +1.25, +0.46) m`。
5. 将后轮对车轮和制动盘移动 `(0, -1.25, +0.46) m`。
6. 建立 `BogieCenter`、`RailContactPlane`、`VehicleMount`、`Axle_01/02` 锚点。
7. 清除源文件的第 1–60 帧分解动画，仅导出静态组装资产。

修复后模型最低点为 `-0.018 m`，源参考轨顶为 `-0.030 m`，轮对前后中心距为 `2.50 m`。

## 玩法映射

| 白盒输入 | 示范几何 |
|---|---|
| 车轴 | `Axle_F`、`Axle_R` |
| 车轮 | `Wheels_F`、`Wheels_R` |
| 轴承 | 四个 `Axlebox_*`，界面语义视为轴箱/轴承座位置示意 |
| 制动装置 | `BrakeDiscs_F/R` 和四个 `Caliper_*` |
| 一系弹性元件 | 四个 `Spring_*` |
| 一系减振元件 | 四个 `DamperV_*` |
| 二系弹性元件 | `AirSpring_L/R` |
| 二系减振元件 | `DamperY_L/R`、`DamperT_C` |
| 中央牵引装置 | `Traction` |
| 转向架构体完成附加包 | `Motor_*`、`Gearbox_*`、`DriveShaft_*` |
| 车体零件/落车完成车辆 | `FuxingCarbodyAssemblyDemo.fbx` 的缩略件/1:1 成品车厢 + 两套完整示范转向架；第二套按配套生产线供件说明处理 |

牵引拉杆、传感器座、一系定位元件和高度控制元件仍使用程序化小件。车体已经接入
真实截取件；它只用于演示装配比例，不能替代经过单位、接口和车型门禁的工程车体。
玩法 `PartId`、`ModuleId`、库存、存档、吸附槽和完成判定保持不变。

## 防穿模约束

- 示范网格仅承担视觉表现，不生成 `MeshCollider`、刚体或动画控制器。
- 交互范围继续使用工位原有 BoxCollider。
- 零件根节点吸附后保持位置零、旋转单位值、缩放一；尺寸适配放在 `DemonstrationModelContent` 子节点。
- 转向架构体的轮对轴箱、构架和一系悬挂共用 `RailContactPlane` 原点，在同一吸附槽中逐层合成。
- `Traction` 只用于中央牵引装置，避免在构架和落车阶段重复。
- 原模型钢轨不进入场景，最终轮轨关系以项目场景钢轨为准。
- 落车工位钢轨中心距使用源模型的 `1.435 m`，车体底面与示范转向架最高点保留约 `0.045 m` 间隙；落车平台纵向约 27 m。

## 验收

- `BogieAssemblyDemoVisualTests` 全部通过。
- 白盒场景中显示“结构示范件”说明。
- 四个基础装配台能够逐件显隐、吸附并在重置后恢复。
- 构体装配台中的前后轮对无重叠，电机和传动包只在构体完成后出现。
- `FinalShowcase` 仍加载 `FuxingTrain.fbx`。
- 展示相关 27 项测试、Windows 双场景构建和成品冒烟继续通过。

成品冒烟可附加 `-whitebox-smoke-bogie-screenshot=<path>` 与
`-whitebox-smoke-landing-screenshot=<path>`，分别输出无 UI、无交互高亮的构体和落车近景。
