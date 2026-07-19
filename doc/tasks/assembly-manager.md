# AssemblyManager 最小任务清单

> 设计依据：`doc/detailed-design.md` 第 5.3、5.4、6.6、7.6 节  
> 模块目标：严格维护安装顺序、待安装事务、组件完成和整车完成状态。

## 边界

- 负责：顺序、依赖、待安装、提交、组件与整车完成判定。
- 不负责：玩家是否拥有零件、模型移动、Tween、页面切换。
- 前置模块：`ContentRepository` 提供零件和配方 DTO。

## 最小任务

- [x] **AM-001 创建安装结果类型。** 实现 `InstallCheck` 状态和 `AssemblyOutcome` 字段。验证：每个状态可构造且字段类型正确。
- [x] **AM-002 实现 configure/reset。** 注入已排序零件、组件和整车配方，清空安装进度。验证：重配后无旧状态。
- [x] **AM-003 实现 get_expected_part_id。** 初始返回第 1 个 ID，全部完成后返回空字符串。验证：边界测试通过。
- [x] **AM-004 实现 can_begin_install。** 区分未知、越序、已安装、依赖缺失和允许，不修改任何状态。验证：每个分支有测试。
- [x] **AM-005 实现 begin_install。** 允许时只设置 `pending_part_id`；已有待安装项时返回 `ANOTHER_INSTALL_PENDING`。验证：已安装集合保持不变。
- [x] **AM-006 实现 abort_pending_install。** 只允许撤销匹配 ID，撤销后可重试。验证：错误 ID 不影响当前待安装项。
- [x] **AM-007 实现 commit_install。** 仅提交匹配待安装 ID，写入已安装集合并推进索引。验证：错误或重复提交不改变状态。
- [x] **AM-008 实现组件完成判定。** 配方三个零件首次全部安装时返回 `completed_component_id`。验证：第 3、6、9 个零件分别触发一次。
- [x] **AM-009 实现整车完成判定。** 三个配方组件全部完成时令 `train_completed=true`。验证：前 8 个零件均为 false，第 9 个为 true。
- [x] **AM-010 实现查询接口。** `is_part_installed()`、`is_train_completed()` 及必要只读快照。验证：查询不泄露内部可变集合。
- [x] **AM-011 覆盖非法事件幂等性。** 重复 begin、重复 commit、旧 ID commit 和完成后继续安装均安全拒绝。验证：状态快照前后一致。
- [x] **AM-012 完成 9→3→1 回归。** 用首版配方按顺序完成 9 个零件，断言 3 个组件和 1 列整车；加入至少一个越序路径。验证：`test_assembly_manager.gd` 全部通过，格式和 lint 通过。

## 模块完成标准

- [x] `begin_install` 与 `commit_install` 形成明确事务边界；
- [x] 所有失败结果都不产生部分提交；
- [x] 三个组件和整车完成只触发一次；
- [x] 模块不依赖库存、场景或动画；
- [x] 单独运行本模块测试全部通过；
- [x] 更新 [`progress.md`](progress.md) 中模块任务数和完成状态。
