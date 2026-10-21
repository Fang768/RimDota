# 项目索引（任务 → 文件/章节）

项目定位：RimWorld 1.7 的 C# 框架 Mod「RimDota」——已落地 DotaHero 标记、三维属性、法力值蓝条与基础科技树/建筑/物品，后续内容 Mod 以 RimDota_**** 命名依赖本框架。权威概览见 [AGENTS.md](AGENTS.md)。

## 路由

| 任务 | 去读/去改 |
| --- | --- |
| 了解项目与规则 | [AGENTS.md](AGENTS.md) |
| 了解当前进展与下一步 | [docs/context/NOW.md](docs/context/NOW.md) |
| 构建、热重载、发布前清理 | [docs/context/RUNBOOK.md](docs/context/RUNBOOK.md) + [AGENTS.md](AGENTS.md) 构建/热重载章节 |
| 目录/版本/配置/部署 | [docs/context/MAP.md](docs/context/MAP.md) |
| 已确认技术决策 | [docs/context/DECISIONS.md](docs/context/DECISIONS.md) |
| 风险与验证缺口 | [docs/context/RISKS.md](docs/context/RISKS.md) |
| 历史追溯 | [history/README.md](history/README.md) |

## 源码与资源

| 位置 | 内容 |
| --- | --- |
| [../Source/RimDotaMod.cs](../Source/RimDotaMod.cs) | Mod 入口；构造函数 `PatchAll()`；`HarmonyId` |
| [../Source/HarmonyPatches.cs](../Source/HarmonyPatches.cs) | 生命上限补丁（`GetMaxHealth`×HealthScaleMult）、DotaGear 穿戴门控、一次性示例日志 |
| [../Source/Hediff_DotaHero.cs](../Source/Hediff_DotaHero.cs) | 英雄属性组件：属性/成长/Bonus 双层派生值、回蓝回血 Tick、存档、法力 Gizmo |
| [../Source/Gizmo_DotaMana.cs](../Source/Gizmo_DotaMana.cs) | 法力条 + 三维属性显示（图标 + 值 + 成长） |
| [../Source/DotaHeroUtil.cs](../Source/DotaHeroUtil.cs) | 框架 API：IsHero / GetHeroComp / GetMana / GetAttribute |
| [../Source/DotaAttribute.cs](../Source/DotaAttribute.cs) | 三维枚举 Strength/Agility/Intelligence |
| [../Source/DotaHeroDebugActions.cs](../Source/DotaHeroDebugActions.cs) | Dev 调试入口（设为/移除/查看英雄） |
| [../Source/StatPart_DotaHeroAttribute.cs](../Source/StatPart_DotaHeroAttribute.cs) | 属性→Stat 动态加成（尚未接入 StatDef） |
| [../Source/DotaEquipmentUtil.cs](../Source/DotaEquipmentUtil.cs) | DotaGear 层判断 |
| [../Source/Comp_DestroyedSpawnsItems.cs](../Source/Comp_DestroyedSpawnsItems.cs) | 建筑摧毁掉落 |
| [../Source/DotaDefOf.cs](../Source/DotaDefOf.cs) | DefOf 强类型引用 |
| [../Source/RimDota.csproj](../Source/RimDota.csproj) | 工程配置：net10.0、OutDir、依赖 |
| [../Defs/](../Defs/) | HediffDefs、ApparelLayerDefs、ResearchProjectDefs、ThingDefs_Buildings/Items/Apparel、MapGeneration |
| [../Textures/UI/Icons/Dota/](../Textures/UI/Icons/Dota/) | 三维占位图标（Strength/Agility/Intelligence.png） |
| [../Languages/](../Languages/) | Keyed（`DotaMana`=法力/Mana）与 DefInjected |
| [../About/About.xml](../About/About.xml) | Mod 元数据（packageId、HarmonyRimWorld 依赖） |
