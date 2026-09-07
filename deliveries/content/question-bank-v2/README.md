# RailCraft 第三人称白盒题库 v2 交付清单（重构前快照）

> 本页记录 2026-08 的题库快照。当前 Art Alpha 保留 58 题完整内容，核心流程精选约 18 题，
> 玩家界面按 6 个工程模块组织，不再按 14 个零件工位循环。当前口径见
> [`../../../docs/project/CURRENT_BASELINE.md`](../../../docs/project/CURRENT_BASELINE.md)。

- 生效日期：2026-08-06
- 来源原件：`50道选择+30道判断.doc`
- 来源 SHA-256：`45F94190C8486DC78A74994C7120A81C0949331E67956A2751B1A3450C9C0C70`
- 来源登记：[`../question-bank-v1/README.md`](../question-bank-v1/README.md)
- 运行时题库：`railcraft-unity/Assets/RailCraft/ThirdPerson/Runtime/Domain/WhiteboxQuestionBank.cs`

## v2 冻结结果

原件正文实际包含 50 道四选一和 8 道判断题，共 58 道。文件名所述的另外 22 道判断题没有出现在正文中，本版本没有补写题目。

- 四选一 ID：`bank_mc01`–`bank_mc50`；
- 判断题 ID：`bank_tf01`–`bank_tf08`；
- 每题保留题干、选项、正确答案与解析；
- 判断题统一显示“正确 / 错误”；
- 选项展示顺序做确定性轮换，并映射回原答案索引，降低原题答案分布偏斜的影响；
- 题目按主题和工程模块建立题池；核心题与备用题分开，重置后按模块策略轮换。

`MC37` 按原文答案括注规范为第三个选项，并在解析中保留说明。其余题目没有通过语言模型自行修订答案。

## 与 v1 的关系

v1 固定视角版本冻结了原件中的 48 题，用于已退休的旧流程。当前第三人称 Art Alpha 使用
原件正文全部 58 题，题库内容与模块奖励策略独立版本化。

原始 `.doc` 继续保存在被 Git 忽略的 `question-bank-v1/release/` 中；仓库只跟踪来源哈希、版本说明与运行时结构化题库。
