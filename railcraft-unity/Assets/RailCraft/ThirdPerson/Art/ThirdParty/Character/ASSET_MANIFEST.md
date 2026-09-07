# 角色资产清单（首批）

## 集成入口

- 首选玩家 Prefab：`Bubba_The_Handyman/URP/Prefabs/Handyman_ver_1.prefab`（GUID `68df0c2a87566234f803f90fd75f6444`）
- 基础 Prefab：`Bubba_The_Handyman/URP/Prefabs/Handyman_Full.prefab`（GUID `53d400b9ba765294a849519b33520be4`）
- Humanoid Avatar 来源：`Bubba_The_Handyman/URP/FBX_Mesh/Handyman_Full.fbx`（GUID `db4637291be2f55449ebbeae91a6aadf`）
- Idle FBX：`Bubba_The_Handyman/URP/Animations/Handyman_idle_1_animation.fbx`（GUID `d3e27e3d0c7e7864297295a0b4e5b375`，Clip `Handyman_idle1_anim`）
- Walk FBX：`Bubba_The_Handyman/URP/Animations/Handyman_walk_animation.fbx`（GUID `db3d16fd6759ed748b98514df2b355df`，Clip `Handyman_walk_anim`）
- Run FBX：`Bubba_The_Handyman/URP/Animations/Handyman_run_animation.fbx`（GUID `dbbeecfb61dce334f96dd121d5333165`，Clip `Handyman_run_anim`）

三个 FBX 动作在原始 Importer 中均为 Humanoid、循环开启，并复用 `Handyman_Full.fbx` 的 Avatar。Animator Controller 由项目集成层创建，本批次未带入素材包控制器。

## 最小依赖闭包

| 类型 | 文件 | GUID |
| --- | --- | --- |
| Material | `Materials/Handyman_Body.mat` | `5a95f403c27ef6a43b5c405a8d58ee1c` |
| Material | `Materials/Handyman_Cloth_1.mat` | `bcdee416f4b8c3740a2f103140aefade` |
| Material | `Materials/Handyman_Hair_blond.mat` | `fe5fdef9d0014144ab6a018dac92e7a6` |
| Texture | `Textures/Handyman_Body_AlbedoTransparency.png` | `0755a527218d9dd4a9d37213023df408` |
| Texture | `Textures/Handyman_Body_AO.png` | `3d237a9973397d04eb817dbf49a60f31` |
| Texture | `Textures/Handyman_Body_MetallicSmoothness.png` | `5fa09196c357cc147b3855b6096c748c` |
| Texture | `Textures/Handyman_Body_Normal.png` | `5ba532ef453b2184496c86a4d74062de` |
| Texture | `Textures/Handyman_Cloth_Albedo_ver1.png` | `11f331f787f0e604db02fd2d4733b69e` |
| Texture | `Textures/Handyman_Cloth_AO.png` | `19542f20171e6bc45b6644e16ed25ee8` |
| Texture | `Textures/Handyman_Cloth_MetallicSmoothness.png` | `7d6e4fa9258de5c469604fafc54cddd6` |
| Texture | `Textures/Handyman_Cloth_Normal.png` | `0e9454fad646bc44cad3096f85dc0491` |
| Texture | `Textures/Handyman_Hair.png` | `8d2894d8706e162429b9fac4ed772d3d` |
| Texture | `Textures/Handyman_Hair_NormalMap.png` | `a6c25fb62505cc9428d49b31f9d68505` |

这些材质使用 URP/Lit。`Handyman_Full.fbx.meta` 的三项外部材质映射均已包含在上表。

## 候选通用动作

下列 PolyOne 动作为无外部 GUID 依赖的 Humanoid muscle-curve `.anim`，用于后续动作风格对比；首版玩家状态机可直接采用上方 Bubba FBX 动作。

| 文件 | GUID |
| --- | --- |
| `PolyOne/Basic Motions/Animation/Idle.anim` | `75dc0a30dc68695408189f6f0b2ce451` |
| `PolyOne/Basic Motions/Animation/Walk.anim` | `aa47e2a2970913b49a627fdca6b33de7` |
| `PolyOne/Basic Motions/Animation/Run.anim` | `0824048eea4f4c3468da8583ec09a01c` |

## 规模与后续检查

- 叶子资产：22 个，共 279,087,037 bytes（不含 `.meta` 与本说明）。
- 高分辨率 PNG 合计约 109 MB；当前仓库 `.gitattributes` 尚未对 PNG 启用 Git LFS，提交前需评估仓库体积策略。
- 首次由 Unity 导入后需检查 URP 材质显示、Avatar 有效性、原地位移、脚底滑动、Prefab LOD 与移动胶囊高度。
- PolyOne 动作需通过实际 Humanoid 重定向测试后再进入正式状态机。
- 许可状态见 `THIRD_PARTY_NOTICES.md`，当前为待核验。
