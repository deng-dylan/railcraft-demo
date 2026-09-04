# 车型方案与现有玩法的接入约定

当前主线把通用高速动车组标准工单设为普通玩家唯一可开始的新工单，沿用已有的答题 → 零件领取 →
子总成装配 → 转向架构体装配 → 落车 → 调试/检验流程。车型方案字段继续保留在存档和
开发者工具中，旧存档可以按原值恢复。`FinalShowcase` 只承担展示风格层，不代表具体车型工程身份。

## 标准方案与扩展登记

- `FuxingDemo`：通用高速动车组标准工单；使用队员车体展示 FBX 与通用转向架示范件。
- `MetroSimplified`：地铁简化上色装配体的扩展示范登记。
- `Y25Freight`：Y25 欧洲货运转向架的真实 STEP 网格已进入 Unity 审模展台。
- `TeachingConcept`：“简化铁路转向架（现实无对应）”教学概念件已进入 Unity 审模展台。

Y25 与教学概念件已完成 STEP 网格化；地铁和机车装配体仍等待 SolidWorks Pack and Go 或 FBX/GLB 导出。
扩展方案还需补齐对应工艺差异、题目目标、落车对象和验收说明，之后才能升级为玩家可选
关卡。当前主菜单不显示这些登记项，独立审模入口为
`RailCraft > Models > Open CAD Preview Gallery`。

## 交付网格最低契约

1. FBX、GLB 或 OBJ，单位为米，Y 轴向上。
2. 车轮接触面作为局部安装基准，模型原点位于转向架中心。
3. 视觉网格不带运行时 Collider/Rigidbody；需要碰撞时单独提供简化碰撞体。
4. 至少一个 LOD 或面数说明，并提供颜色/材质清单。
5. 确认组员授权和“教学概念”标注，避免把无现实对应的教学件描述成真实车型。

导入后应重新执行 EditMode、Windows Player 构建和完整冒烟，确认扩展内容不影响标准工单。

开发者可使用 `-whitebox-smoke-variant=<key>` 指定兼容性烟测方案，当前 key 为
`fuxing-demo`、`metro-simplified`、`y25-freight`、`teaching-concept`；这些 key 是内部构型标识，不代表车型认证。
