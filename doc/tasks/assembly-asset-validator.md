# AssemblyAssetValidator 最小任务清单

> 设计依据：`doc/detailed-design.md` 第 3.3、7.3、9.3、9.4 节  
> 模块目标：在启动阶段验证零件场景、`PartActor` 契约和安装目标节点，避免进入体验后才发现资源损坏。

## 边界

- 负责：Godot 资源与场景节点契约校验。
- 不负责：JSON 字段规则、模型实例的长期保存、动画和安装判定。
- 前置模块：`ContentRepository`；AV-001 至 AV-007 可使用最小 Catalog 和夹具场景，AV-008 需等待 `AssemblyView` 的 9 个真实零件场景完成。
- 输入：`ContentCatalog`、`TrainAssemblyRoot`。

## 最小任务

- [ ] **AV-001 创建资产校验器接口。** 返回 `Array[ValidationIssue]`，空零件集合返回空数组。验证：脚本加载及空集合测试通过。
- [ ] **AV-002 创建最小有效夹具。** 建立一个 `PartActor` 场景、一个 `Marker3D` 目标和最小 Catalog。验证：夹具可在 headless 模式加载。
- [ ] **AV-003 校验 model_scene_path。** 使用 `ResourceLoader.exists/load` 检查资源存在且为 `PackedScene`。验证：缺失路径和错误资源类型返回稳定错误码。
- [ ] **AV-004 校验零件根节点。** 临时实例化场景并检查根节点继承 `Node3D`。验证：错误根类型夹具被拒绝。
- [ ] **AV-005 校验 PartActor 契约。** 检查脚本能力、`VisualRoot`、`ClickArea` 和 `CollisionShape3D`。验证：逐项缺失夹具返回准确节点路径。
- [ ] **AV-006 校验安装目标。** 按相对路径查找目标并要求其为 `Marker3D`。验证：缺失目标和错误节点类型测试通过。
- [ ] **AV-007 清理临时实例。** 确保成功或失败路径都会释放临时节点，不残留在场景树。验证：重复校验后节点计数保持不变。
- [ ] **AV-008 完成资产校验回归。** 对 9 个首版零件和全部目标运行校验。验证：资产校验测试及 headless 夹具测试全部通过，格式和 lint 通过。

## 模块完成标准

- [ ] 场景路径、根类型、点击契约和目标节点均有正反测试；
- [ ] 校验结束后没有临时节点泄漏；
- [ ] 模块不修改 Catalog 和真实装配状态；
- [ ] 首版 9 个零件资产全部通过；
- [ ] 更新 [`progress.md`](progress.md) 中模块任务数和完成状态。
