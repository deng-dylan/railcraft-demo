# 车型方案与现有玩法的接入约定

当前主线把 `FuxingDemo` 设为普通玩家唯一可开始的新工单，沿用已有的答题 → 零件领取 →
子总成装配 → 转向架构体装配 → 落车 → 调试/检验流程。车型方案字段继续保留在存档和
开发者工具中，旧存档可以按原值恢复。最终 `FinalShowcase` 固定展示完整复兴号。

## 标准方案与扩展登记

- `FuxingDemo`：标准工单；使用队员转向架 FBX 与截取的一节复兴号车体。
- `MetroSimplified`：地铁简化上色装配体的扩展示范登记。
- `Y25Freight`：Y25 欧洲货运转向架 STEP 的导入与轻量化登记。
- `TeachingConcept`：“简化铁路转向架（现实无对应）”教学概念件登记。

后面三个方案的源文件仍是 SolidWorks/STEP 格式，项目保留统一网格插槽。网格文件放入
`Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/` 后，还需补齐对应工艺差异、题目目标、
落车对象和验收说明，之后才能升级为玩家可选扩展关卡。当前主菜单不显示这些登记项。

## 交付网格最低契约

1. FBX、GLB 或 OBJ，单位为米，Y 轴向上。
2. 车轮接触面作为局部安装基准，模型原点位于转向架中心。
3. 视觉网格不带运行时 Collider/Rigidbody；需要碰撞时单独提供简化碰撞体。
4. 至少一个 LOD 或面数说明，并提供颜色/材质清单。
5. 确认组员授权和“教学概念”标注，避免把无现实对应的教学件描述成真实车型。

导入后应重新执行 EditMode、Windows Player 构建和完整冒烟，确认扩展内容不影响标准工单。

开发者可使用 `-whitebox-smoke-variant=<key>` 指定兼容性烟测方案，当前 key 为
`fuxing-demo`、`metro-simplified`、`y25-freight`、`teaching-concept`。
