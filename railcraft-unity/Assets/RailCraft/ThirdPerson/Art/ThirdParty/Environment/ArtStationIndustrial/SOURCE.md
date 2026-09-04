# ArtStation Industrial Visual Sources

本目录保存从 ArtStation Marketplace 免费领取的三组工业视觉资产精选 FBX。它们只用于
厂房背景与物流/设备展示，不承担 CR400AF、SWM-400E1 或任何工程尺寸结论。

## 来源与许可

| 组 | 商品页 | 页面许可 | 下载包 SHA-256 |
| --- | --- | --- | --- |
| 叉车与模块 | <https://www.artstation.com/marketplace/p/mDqVn/stylized-industrial-forklift-with-modular-parts> | Extended Commercial License，Free | `7BC6031EA00A248C99186F50CECB37E6906671DFBA4FFC259490D6DB78DE1DF0` |
| 工业设备 | <https://www.artstation.com/marketplace/p/XoXL7/stylized-industrial-construction-equipment-collection> | Extended Commercial License，Free | `D9ED746A2F841B793528283823F89D39F882DCC05B6858FC631128E1197962B9` |
| 仓储货架 | <https://www.artstation.com/marketplace/p/Gr1wy/stylized-warehouse-shelving-set-a> | Extended Commercial License，Free | `97EDAB9237338C6D96F05FD6D1143C4712A2EE4DC9466F9A003E1CE27E011962` |

领取日期：2026-09-03。三组 FBX 包中的说明文件均标注 Blender 4.x 导出、UV 和模型统计；
ArtStation 商品页显示对应的 Extended Commercial License。发布前仍应以账号资源库中的
当前条款为准，保留页面与资源库记录，不把原始下载 ZIP 放入 Git。

## 运行时精选

- `Forklift/SM_Ind_Forklift_Detailed.fbx`：叉车近/中景模型，页面统计约 65,086 三角面。
- `Equipment/SM_Industrial_Bench_Grinder_01.fbx`：工业砂轮机。
- `Equipment/SM_Industrial_Cable_Reel_01_Background.fbx`：线缆盘背景模型。
- `Equipment/SM_Industrial_Cement_Mixer_001.fbx`、`SM_Industrial_Cement_Mixer_002.fbx`：两种搅拌机。
- `Equipment/SM_Industrial_Concrete_Barrier_Background.fbx`：混凝土隔离栏。
- `Shelving/SM_Aisle_Red_A.fbx`：红色仓储货架通道。
- `Shelving/SM_ShelfEmptyx3_Red_B.fbx`、`SM_ShelfEmptyx3_White_B.fbx`：低面数远景货架。
- `Textures/Stylized_Industrial_TextureMap_V02.png`：三组共用的颜色图集。

Unity 接入由 `ArtStationIndustrialVisualFactory` 负责：实例化时移除 Collider、Rigidbody、
Animator、Camera、Light 和 AudioSource，统一替换为项目自有 URP/Lit 材质，并按包围盒
归一化到米制场景。任何可交互碰撞仍由项目自有工位或代理体负责。
