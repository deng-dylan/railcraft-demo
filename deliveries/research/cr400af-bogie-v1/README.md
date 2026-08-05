# CR400AF 转向架公开参考资料

- 下载日期：2026-08-01
- 用途：RailCraft Unity 版首轮需求梳理与子系统级装配流程参考
- 使用范围：项目内部学习、方案核对和占位内容设计

## 已下载文件

### `high-speed-emu-bogies-2023.pdf`

- 题名：中国高速动车组转向架技术发展及展望
- 来源：机车电传动
- DOI：10.13890/j.issn.1000-128X.2023.02.002
- 原始地址：https://edl.csrzic.com/rc-pub/front/front-article/download/37953236/lowqualitypdf/Technological%20development%20and%20prospect%20of%20China%27s%20high%20speed%20EMU%20bogies.pdf
- SHA-256：`8B76101C0C0EAB2A28FFE3E3E5EA287003D96D14CAB16473A181EA00CB3481ED`
- 适用说明：包含 CR400AF 的 SWM-400E1 动力转向架、SWT-400E1 拖车转向架图片、主要技术参数和子系统组成，可作为首版模型拆分与术语参考。

### `high-speed-emus-development-trends-2020.pdf`

- 题名：高速动车组技术发展特点及趋势
- 来源：Engineering / 中国工程院
- DOI：10.1016/j.eng.2020.01.008
- 原始地址：https://www.engineering.org.cn/engi/CN/PDF/10.1016/j.eng.2020.01.008
- 许可：CC BY-NC-ND 4.0
- SHA-256：`B2D55E7BC74FFD49EBF350F87A7927CBAC46A35ED560D70D6AC535C6AC1C54B5`
- 适用说明：用于复兴号和高速动车组的背景介绍，不作为具体装配步骤依据。

## 仅保留网页链接的参考

- 转向架牵引装置装配工艺，CN102963385A：https://patents.google.com/patent/CN102963385A/zh
- 客车转向架组装自动传输生产线，CN102430920A：https://patents.google.com/patent/CN102430920A/zh
- 机车车辆转向架 动车组转向架，TB/T 3316-2020：https://std.samr.gov.cn/hb/search/stdHBDetailed?id=BE3F89B976BD6158E05397BE0A0AAD30

Google Patents 的 PDF 下载端在本次整理时持续超时，因此只登记公开网页。

## 已确认车型与型号

项目首版与公开论文采用一致命名：

- 车型：`CR400AF`
- 动力转向架：`SWM-400E1`
- 拖车转向架：`SWT-400E1`

首版只实现 `SWM-400E1` 动力转向架的可操作装配流程。`SWT-400E1` 拖车转向架计划放入后续版本。

## 使用边界

- 这些材料只支持子系统拆分、公开参数核对和首版教学抽象。
- 专利中的装配步骤并非 CR400AF 专用工艺，不能直接宣称为 SWM-400E1 / SWT-400E1 的真实工厂工艺。
- 下载文件放在 `release/` 下，本仓库按既有规则忽略该目录中的原始交付文件；README 保留来源和校验值。
