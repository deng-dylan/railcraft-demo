# Electrical Substation 来源记录

- 用户提供 Unity 包：仓库根目录 `electrical_substation.unitypackage`
- 原包内路径：`Assets/Electrical Substation`
- 包文件 SHA-256：`F33C6523568C41BE0C11DA51C6159E9865EE4FDEAE4D4A2A3D3B6FA17738A884`
- 入库日期：2026-09-03
- 许可状态：原包未附带许可文件、作者声明或授权范围。当前仅作项目内部原型资产；对外发布、参赛交付或商业使用前需要核验来源与授权。

## 受控提取范围

- `Prefabs/SM_Electrical_Substation.prefab`
- `Meshes/SM_Electrical_Substation.fbx`
- `Materials/M_Electrical_Substation.mat`
- `Textures/T_Electrical_Substation_BaseColor.tga`
- `Textures/T_Electrical_Substation_Metallic.tga`
- `Textures/T_Electrical_Substation_Normal.tga`
- `Textures/T_Electrical_Substation_Occlusion.tga`
- `Textures/T_Electrical_Substation_Roughness.tga`

上述资产保留原始 `.meta` 和 GUID。依赖链为 Prefab → FBX/主材质 → 五张贴图。

## 已排除内容

- `Scenes/demo.unity` 及 `Scenes` 目录
- 仅供演示地面使用的 `Materials/Plane.mat`
- Unity Package 预览图

## 接入提示

原始主材质使用 Built-in Render Pipeline 的 Standard 系列 Shader，而当前工程使用 URP。编排进场景前需要转换或新建 URP Lit 材质，并检查 Metallic、Roughness/Smoothness、Occlusion 与 Normal 的通道和色彩空间。原始 `.meta` 已把 Normal 识别为法线贴图，但 Metallic、Occlusion 和 Roughness 仍启用 sRGB；URP 接入时应将这三张数据贴图改为线性采样。
