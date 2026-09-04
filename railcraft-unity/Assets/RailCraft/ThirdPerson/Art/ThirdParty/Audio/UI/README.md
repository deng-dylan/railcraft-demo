# UI SFX 首批精选

来源归档：工作区根目录 `fab_ui_sfx_free_pack.unitypackage`

- 来源 SHA-256：`614b2317130f328779328075360ac37240abe1ea296cc9d2cee05fa747d28905`
- 收录范围：12 个 WAV，覆盖按钮、确认成功、取消/失败、警告弹窗和维修反馈。
- 音频规格：44.1 kHz、双声道、16-bit PCM；单条时长约 0.16–2.22 秒。
- Unity 元数据：音频文件及来源分类目录的原始 `.meta` 已保留，GUID 未改写。
- 运行时接入：本批只落盘素材，尚未绑定 `AudioSource`、Mixer 或业务事件。

## 建议用途

| 目录 | 候选用途 |
| --- | --- |
| `Buttons` | 常规点击、短点击、主操作反馈 |
| `Ok` | 答题正确、装配完成、流程确认 |
| `Cancel` | 取消、操作无效、失败反馈 |
| `Warning_Popup` | 一般提示与重点警告弹窗 |
| `Repair` | 快速维修、中等维修和完整维修过程反馈 |

具体映射及每个文件的 GUID、时长、大小、SHA-256 见 `asset-manifest.json`。正式绑定前建议在 Unity 内按目标响度试听，并统一 UI Mixer 音量。

## 许可状态

**待核验（pending verification）**。当前归档名称表明它是免费 UI 音效包，但本次没有把来源包内 PDF 文档作为运行时资产导入，也未据此确认许可条款。对外发布、比赛提交或再分发前，需要补齐资产商店页面/作者、许可文本或购买记录，并由项目负责人确认允许的使用范围。

## 明确排除

- `Content_for_Demo_Scene/Content_for_DemoScene.unitypackage`
- `Content_for_Demo_Scene/Read_Me.txt`
- `Documentation/UI SFX Free Pack Documentation.pdf`
- 未列入首批的其余 WAV
