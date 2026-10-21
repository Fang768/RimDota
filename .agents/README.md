# Agent 文档体系入口

本目录存放面向后续 AI/Agent 的上下文文档，按需读取，不要全文遍历。

| 文件 | 内容 | 何时读 |
| --- | --- | --- |
| [AGENTS.md](AGENTS.md) | 长期规则、读取/写入规则、验证要求、文档导航（自动加载） | 每次任务开始 |
| [PROJECT_INDEX.md](PROJECT_INDEX.md) | 项目定位 + 任务 → 文件/章节路由 | 需要定位文件时 |
| [docs/context/NOW.md](docs/context/NOW.md) | 当前目标、进展、阻塞、下一步、证据入口 | 接续任务时 |
| [docs/context/MAP.md](docs/context/MAP.md) | 目录、版本、配置、部署位置关系 | 定位修改/运行对象时 |
| [docs/context/RUNBOOK.md](docs/context/RUNBOOK.md) | 有依据的操作、前置条件、验证步骤 | 执行对应操作前 |
| [docs/context/DECISIONS.md](docs/context/DECISIONS.md) | 已确认决策、理由、范围、来源 | 涉及相关选择时 |
| [docs/context/RISKS.md](docs/context/RISKS.md) | 有证据的问题、风险、验证缺口、保护措施 | 涉及对应风险时 |
| [history/README.md](history/README.md) | 有价值历史记录的索引 | 需要追溯时先搜索再读 |

通用原则见 [AGENTS.md](AGENTS.md) 的“读取规则”与“写入规则”。

更新约定：每类事实只设一个权威来源；状态变化时更新对应专题并注明核验日期与范围；缺失事实标注“待核实”，不得编造。
