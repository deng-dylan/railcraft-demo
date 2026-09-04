# ART-001 免费资产扩展接入记录

- 接入日期：2026-09-04
- 资产身份：通用教学件 / 展示风格
- 接入入口：`Assets/RailCraft/ThirdPerson/Editor/FreeAssetExpansionVisualFactory.cs`
- 场景生成器：`Assets/RailCraft/ThirdPerson/Editor/WhiteboxSceneBuilder.cs`

## 已接入

| 用途 | 资产 | 身份与边界 |
| --- | --- | --- |
| 一系弹性元件 | Quaternius Spring | 通用教学视觉，不表达车型尺寸 |
| 传感器座、定位元件 | Quaternius Metal Support | 通用安装关系示意 |
| 高度控制元件 | BlendSwap Stop Valve + Quaternius Pipes | 通用阀体和气路示意，不宣称为正式高度阀 |
| 工位道具、窗体 | Low Poly Construction 精选 10 个 FBX | 展示风格，不表达扭矩或计量精度 |
| 安全隔离 | CC0 Safety 精选 3 个 FBX | 展示风格，实际通道仍由项目碰撞体定义 |
| 厂区远景 | Kenney City Kit Industrial 精选 5 个 FBX | 展示风格，不作为建筑设计依据 |
| 远景车辆 | Quaternius Modular Train 精选 4 个 FBX | 展示风格，不对应具体车型 |

各资产的来源链接、许可、原始压缩包 SHA-256 和审模说明保存在对应资产目录的
`SOURCE.md`；BlendSwap 和 Kenney 随包许可证也保留在各自 `Documentation/` 目录。

## 接入合同

- 子件视觉通过零件视觉工厂生成，可随装配件移动，导入碰撞体全部移除；
- 环境视觉由场景生成器确定性生成，标记为静态且不改变交互和碰撞规则；
- 使用项目材质接管外观，未把第三方预览参数写成工程事实；
- 场景中已核对 `FreeAssetExpansion`、四个环境分组和四类子件视觉节点；
- 新增 `FreeAssetExpansionVisualTests.cs`，覆盖资产存在性、来源记录、子件可移动性、
  无导入碰撞以及环境静态合同。

## 当前验证

| 检查 | 结果 |
| --- | --- |
| 生成场景静态节点检查 | 通过 |
| 来源与许可记录检查 | 通过 |
| `git diff --check` | 业务代码、文档与场景无问题；随包原始许可证 HTML/TXT 保留源文件尾随空白 |
| Unity EditMode | 待运行；本机许可返回 code 198 |
| Windows x86_64 构建 | 待运行 |
| Player 烟测与新截图 | 待运行 |

上一批 `FLOW-002` 的 200/200、构建和烟测结果不作为本批运行证据。恢复 Unity 许可后，
应依次重跑完整 EditMode、Windows 构建、标准工单烟测，并更新本记录和当前状态。

## 仍需补齐

1. 明确的二系空气弹簧几何；
2. 定位夹具、升降台、吊具和可表达扭矩流程的工具；
3. 真实 HMI、检验仪表与线缆；
4. 地面磨损/贴花、天空与雾效；
5. 车型专用工程件所需的图纸、尺寸、接口、责任人和授权；
6. 正式 LOD、简化碰撞体、材质纹理与目标机性能验收。
