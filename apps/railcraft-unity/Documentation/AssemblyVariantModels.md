# 车型方案与现有玩法的接入约定

当前主线把“车型方案”作为一次游戏会话的属性。玩家在主菜单选择方案后，沿用已有的
答题 → 零件拾取 → 子总成装配 → 转向架构体装配 → 落车 → 调试/检验流程；选择值会写入
存档，继续游戏时恢复。最终 `FinalShowcase` 仍固定展示完整复兴号。

## 已接入的四个方案

- `FuxingDemo`：现有队员转向架 FBX 与截取的一节复兴号车体，默认可运行网格。
- `MetroSimplified`：对应组员的地铁简化上色装配体。
- `Y25Freight`：对应 Y25 欧洲货运转向架 STEP。
- `TeachingConcept`：对应“简化铁路转向架（现实无对应）”教学版，界面会明确标记为教学概念。

后面三个方案的源文件仍是 SolidWorks/STEP 格式，项目已预留统一网格插槽。网格文件放入
`Assets/RailCraft/ThirdPerson/Art/Models/VariantModels/` 后重新生成白盒场景即可替换参考模型；
在网格到位前，玩法会使用现有示范 FBX 的低风险占位，同时通过颜色和方案标识区分方案。

## 交付网格最低契约

1. FBX、GLB 或 OBJ，单位为米，Y 轴向上。
2. 车轮接触面作为局部安装基准，模型原点位于转向架中心。
3. 视觉网格不带运行时 Collider/Rigidbody；需要碰撞时单独提供简化碰撞体。
4. 至少一个 LOD 或面数说明，并提供颜色/材质清单。
5. 确认组员授权和“教学概念”标注，避免把无现实对应的教学件描述成真实车型。

导入后应重新执行 EditMode、Windows Player 构建和完整冒烟，确认方案切换不影响既有流程规则。

可使用 `-whitebox-smoke-variant=<key>` 指定成品烟测方案，当前 key 为
`fuxing-demo`、`metro-simplified`、`y25-freight`、`teaching-concept`。
