# 风险与验证缺口（RISKS）

> 仅记录有证据的问题与缺口。最后更新：2026-10-18。

| 风险/缺口 | 证据 | 影响 | 保护/缓解 |
| --- | --- | --- | --- |
| Harmony 在 1.7 WaylithCLR 下兼容性待验证 | 社区实测 99% 工坊 Mod 未破坏；Harmony 由 HarmonyRimWorld 统一提供 | 极少数补丁可能不生效 | 游戏内实测 |
| 1.7 为测试版，API/运行时可能继续变动 | 本机 1.7.4981 rev136；引用包仅到 1.7.4979-alpha | 后续更新可能再次破坏兼容 | 关注官方 changelog；发布前重新验证 |
| 热重载方案未定 | 0Reloader 已移除，官方新 API 待核实 | 调试效率下降 | 待官方文档/社区方案 |
| About.xml 描述仍为模板文案 | `About/About.xml` | 发布描述错误 | 发布前替换 |
| 护甲/攻速/心灵敏感度仍是 XML 静态值，敏捷/智力未实时驱动 | `Defs/HediffDefs/Hediffs.xml`；`Source/StatPart_DotaHeroAttribute.cs` 未接线 | 双层模型对这三项暂只体现在 tooltip，不生效 | 接 StatPart 并决定是否去掉静态值 |
| 部分功能只经用户游戏内目视验证，未做完整回归 | 用户曾报告蓝条显示与报错并已修复；护甲等动态项未验证 | 某些路径可能遗漏 | 按 [RUNBOOK.md](RUNBOOK.md) 验证 |
| 文档与代码可能漂移 | 模板快速演进期 | 文档失真 | 状态变化时更新对应专题并注明日期 |
| 属性图标为 Valve 编译纹理（.vtex_c），未解码；且为版权素材 | 本机 Dota 2 VPK 定位到 `primary_attribute_icon_*_psd.vtex_c` | 暂用占位图；发布前需替换 | 写 VTex 解码器或找 PNG 镜像 |
| 属性成长值未接入升级逻辑 | `Source/Hediff_DotaHero.cs` 的 growth 字段全 0 | 成长显示恒为 +0.0 | 实现等级/经验后接入 |
| 装备尚未写 Bonus* 字段 | 攻击之爪无属性；Bonus* 入口已建 | 装备加数值暂无实际来源 | 做装备时写入 |
