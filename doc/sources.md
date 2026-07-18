# RailCraft Demo 来源与许可清单

本清单覆盖 Demo 使用的题目资料、字体、第三方代码与开发工具。题目解析为项目原创概括，来源链接仅在界面中作为可复制文本展示，程序不会联网访问。

## 题目资料

| 题号 | 机构 | 资料标题 | 链接 |
|---|---|---|---|
| Q1 | 中国中车 | 和谐号 CRH6 型城际动车组 | <https://www.crrcgc.cc/pz/2013-12/28/article_0C37D1973CFB4C6382B32FD53F5A40B5.html> |
| Q2 | 中国中车 | 列车网络控制系统 | <https://crrcgc.cc/sfs/2016-05/06/article_CB1A64A1964A46308E720DD62DE69E00.html> |
| Q3 | 中国中车 | 通用机电 | <https://crrcgc.cc/eportal/ui?pageId=715104> |
| Q4 | 国家铁路局 | 城际铁路动车组铁路装备技术规范 | <https://www.nra.gov.cn/jglz/sbjg/jsgf/202602/P020260227614771043169.pdf> |
| Q5 | 国家铁路局 | 城际铁路动车组铁路装备技术规范 | <https://www.nra.gov.cn/jglz/sbjg/jsgf/202602/P020260227614771043169.pdf> |
| Q6 | 中国中车 | CRH2 型制动机 | <https://crrcgc.cc/pz/2013-12/12/article_E928263DDE7244DBBF84BAE4443DAC71.html> |
| Q7 | 国家铁路局 | 基于耦合动力学的高速铁路接触网/受电弓系统技术创新及应用 | <https://www.nra.gov.cn/ztzl/hy/kjcx/kjxm/201802/t20180212_198465.shtml> |
| Q8 | 中国中车 | 通用机电 | <https://crrcgc.cc/eportal/ui?pageId=715104> |
| Q9 | 中国中车 | 通用机电 | <https://crrcgc.cc/eportal/ui?pageId=715104> |

## 随包第三方内容

| 内容 | 固定版本/提交 | 许可 | 项目内位置 | 上游来源 |
|---|---|---|---|---|
| Godot Engine | 4.6.3 stable | MIT | 构建工具，不提交引擎二进制 | <https://godotengine.org/download/archive/4.6.3-stable/> |
| GUT | 9.6.0 | MIT | `addons/gut/` | <https://github.com/bitwes/Gut/releases/tag/v9.6.0> |
| Noto Sans SC Variable | `google/fonts@389b770410cc0b7c21c85673bfa2077420fe7f65` | SIL OFL 1.1 | `assets/fonts/` | <https://github.com/google/fonts/tree/389b770410cc0b7c21c85673bfa2077420fe7f65/ofl/notosanssc> |

字体的文件哈希和许可证全文见 [`assets/fonts/SOURCE.md`](../assets/fonts/SOURCE.md) 与 [`assets/fonts/OFL-NotoSansSC.txt`](../assets/fonts/OFL-NotoSansSC.txt)。

## 开发与质量工具

| 工具 | 固定版本 | 许可 | 上游来源 |
|---|---|---|---|
| Python | 3.12.13 | Python Software Foundation License | <https://www.python.org/downloads/release/python-31213/> |
| uv | 0.11.8 | Apache-2.0 / MIT | <https://github.com/astral-sh/uv/releases/tag/0.11.8> |
| gdtoolkit | 4.5.0 | MIT | <https://github.com/Scony/godot-gdscript-toolkit/releases/tag/4.5.0> |
| pre-commit | 4.6.0 | MIT | <https://github.com/pre-commit/pre-commit/releases/tag/v4.6.0> |

## 车型占位资产

九个零件、列车装配根、材质和灯光均由本项目使用 Godot 原生几何体与代码制作，不含外部模型、纹理、音频或品牌标识。它们用于教学演示，不代表任何特定车型的工程尺寸或结构细节。
