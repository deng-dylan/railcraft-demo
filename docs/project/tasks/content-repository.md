# ContentRepository 最小任务清单

> 设计依据：`doc/detailed-design.md` 第 5、7.1 节  
> 模块目标：读取三份固定 JSON，调用校验器，并向其他模块提供类型化、只读用途的 `ContentCatalog`。

## 边界

- 负责：文件读取、JSON 解析、DTO 转换、索引构建、加载结果。
- 不负责：定义校验规则、加载 3D 场景、推进游戏流程。
- 前置模块：`ContentValidator`。
- 输出契约：`load_catalog() -> ContentLoadResult`。

## 最小任务

- [ ] **CR-001 创建基础内容 DTO。** 实现 `SourceData` 和 `TransformData`，字段全部类型化。验证：构造、Vector3 转换和默认值测试通过。
- [ ] **CR-002 创建题目与零件 DTO。** 实现 `QuestionData`、`PartData`，保持设计字段和 0 基答案索引。验证：最小合法字典转换测试通过。
- [ ] **CR-003 创建配方 DTO。** 实现 `ComponentRecipe`、`TrainRecipe`，包含 `teaching_note`。验证：数组字段类型和第三组件教学说明测试通过。
- [ ] **CR-004 创建 ContentCatalog。** 保存排序数组及按 ID 索引，提供查询方法和数组副本。验证：查询存在/缺失 ID、排序和外部修改隔离测试通过。
- [ ] **CR-005 创建 ContentLoadResult。** 明确成功和失败构造方式，失败时 catalog 为空。验证：结果对象状态不出现矛盾组合。
- [ ] **CR-006 实现单文件读取。** 只接受 `res://` 路径，区分文件缺失、读取失败和空文件。验证：有效与缺失夹具测试通过。
- [ ] **CR-007 实现 JSON 解析。** 返回原始 `Dictionary` 或带行号的 `JSON_PARSE_FAILED`。验证：合法、截断和根类型错误测试通过。
- [ ] **CR-008 串联三文件校验。** 读取 `questions.json`、`parts.json`、`recipes.json` 后调用 `ContentValidator`；发现问题时停止 DTO 转换。验证：校验器问题原样进入加载结果。
- [ ] **CR-009 实现 DTO 转换。** 把已校验字典转换为全部类型化对象和 Vector3/Transform 数据。验证：首版夹具字段逐项一致。
- [ ] **CR-010 构建只读目录。** 按 `order` 排序并建立题目、零件、组件索引；拒绝向调用方暴露内部可变数组。验证：查询和副本隔离测试通过。
- [ ] **CR-011 完成仓储回归。** 覆盖成功加载、任一文件缺失、任一文件语法错误、多问题聚合。验证：`test_content_repository.gd` 全部通过，格式和 lint 通过。

## 模块完成标准

- [ ] 三份数据可一次加载为完整 `ContentCatalog`；
- [ ] 所有公开值对象和函数均使用静态类型；
- [ ] 失败结果包含可定位问题且不返回半成品目录；
- [ ] 仓储不加载 UI、3D 场景或网络内容；
- [ ] 单独运行本模块测试全部通过；
- [ ] 更新 [`progress.md`](progress.md) 中模块任务数和完成状态。
