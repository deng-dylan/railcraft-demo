# Shipping Container PBR Sample 来源记录

- 资源名称：Shipping Container PBR（Unity 6 Sample）
- 作者署名：Sayantan Biswas（按原始 `ReadMe.txt` 记录）
- 用户提供归档：仓库根目录 `shippingcontainerpbrsample_2025_22_1_20_05_19.unitypackage`
- 归档大小：132,997,455 bytes
- 归档 SHA-256：`48B17E47E727AE8C84020800ACA99DD1C14660C877520349B41F9688ED10C936`
- 入库日期：2026-09-03
- 兼容性说明：原始文档标注最低 Unity 6000.0.33f1；贴图可用于 URP/HDRP。
- 授权说明：原始文档允许用于商业项目，并要求在 Credits 中简短署名作者；发布前应再次核对随附 `ReadMe.txt` 及资源账户中的许可条款。

## 精选文件

| 入库路径 | 原包路径 | 保留 GUID | 用途 |
| --- | --- | --- | --- |
| `Models/Container20F.fbx` | `Assets/Sayantan/Props/Large/Container_FREE/Models/Container20F.fbx` | `b0124f4e07bf8494ba0b4802b4284a91` | 20 英尺集装箱模型 |
| `Models/Container40F.fbx` | `Assets/Sayantan/Props/Large/Container_FREE/Models/Container40F.fbx` | `8bb941787bcc0074f878e0e2f9b99dfe` | 40 英尺集装箱模型 |
| `Textures/ContainerBeige2K_basecolor.tga` | `Assets/Sayantan/Props/Large/Container_FREE/Textures/ContainerBeige2K_basecolor.tga` | `610bf7936ed70084a910485cdd44d77f` | 米色基础色 |
| `Textures/ContainerBlue2K_basecolor.tga` | `Assets/Sayantan/Props/Large/Container_FREE/Textures/ContainerBlue2K_basecolor.tga` | `4d92e38e950b20c4ba0661ee6a472462` | 蓝色基础色 |
| `Textures/Container2K_Clean_normal.tga` | `Assets/Sayantan/Props/Large/Container_FREE/Textures/Container2K_Clean_normal.tga` | `8ed2147e2eca2744eaf015cfcb8647e0` | 共用法线 |
| `Textures/Container2K_Clean_mask.tga` | `Assets/Sayantan/Props/Large/Container_FREE/Textures/Container2K_Clean_mask.tga` | `9868476ad5209a740852d39debcc5f37` | 共用 PBR 遮罩 |
| `ReadMe.txt` | `Assets/Sayantan/Props/Large/Container_FREE/ReadMe.txt` | `2456aa18592fe3b4cb8a4cbc75b852a4` | 原始内容、兼容性与授权说明 |

模型、贴图、原始说明以及 `ShippingContainers`、`Models`、`Textures` 三个目录均保留包内 `.meta`。两个 FBX 的原始导入设置关闭材质导入，后续应在项目自有目录创建 URP Lit 材质和 Prefab。源包未说明 mask 的通道布局；原始 Standard 材质将它同时用于 Metallic/Gloss 与 Occlusion，接入 URP 前需目视验证通道。

## 有意排除

- 2 个 Standard 材质和 4 个带碰撞体的示例 Prefab：避免把 Built-in/Standard 材质直接带入 URP，后续由项目自有资产完成材质和碰撞体配置。
- 2 个演示场景、2 个 Lighting Settings、4 个后处理/LightingData 资产、4 张烘焙 PNG，以及 56 个 EXR：均为日/夜 Demo 的烘焙或反射数据。
- 2 个天空盒材质、1 个演示地面材质及 3 张地面贴图：与集装箱运行时模型无关。
- 包内 Prefab、Scenes、SceneSettings、Misc 目录及其目录元数据。

第三方源文件保持原样；请勿直接修改本目录中的 FBX、TGA 或原始 `ReadMe.txt`。
