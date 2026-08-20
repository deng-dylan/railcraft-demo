# 组装车间环境示范层

## 资源来源

白盒场景接入 Kenney `Factory Kit` 的轻量装饰件。资源下载页为
<https://kenney.nl/assets/factory-kit>，授权文本随项目保存在
`Assets/RailCraft/ThirdPerson/Art/Models/FactoryKit/KenneyFactoryKit-License.txt`。
该资源用于场景视觉填充，程序化地面、墙体、轨道和交互工位仍由
`WhiteboxSceneBuilder` 管理。

## 当前接入内容

`FactoryKitEnvironmentVisualFactory` 在环境构建末尾创建
`Environment/FactoryKitDecorations`，默认包含 17 个实例：

- 后墙高位：吊车、吊钩、长管道、弯头、猫道和楼梯；
- 后墙两侧：机器、结构墙、箱体和警示牌；
- 所有装饰都统一绑定项目 URP 材质，标记为静态对象；
- 递归移除 Collider、Rigidbody、Animator、Animation、Camera、Light 和 AudioSource。

装饰位置避开两侧知识工位、中央构体台、落车轨道和调试工位。它们不参与拾取、
吸附或碰撞，交互仍由原有工位 BoxCollider 负责。

## 后续替换策略

如果后续拿到更接近真实检修库的模块化环境，只需替换
`FactoryKitEnvironmentVisualFactory.BuildDefaultDecorations` 的资源路径和布置表，
无需改动玩法脚本。最终展示场景不依赖这层装饰。
