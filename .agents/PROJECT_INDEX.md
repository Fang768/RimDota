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
| [../Source/HarmonyPatches.cs](../Source/HarmonyPatches.cs) | 生命上限补丁（`GetMaxHealth`×HealthScaleMult）、一次性示例日志 |
| [../Source/Hediff_DotaHero.cs](../Source/Hediff_DotaHero.cs) | 英雄属性组件：三维属性/成长/装备属性汇总、回血池、法力、存档、Dota 属性栏 Gizmo |
| [../Source/Gizmo_DotaAttributes.cs](../Source/Gizmo_DotaAttributes.cs) | Dota 属性栏：法力条 + 三维属性显示 |
| [../Source/HediffComp_DotaInventory.cs](../Source/HediffComp_DotaInventory.cs) | 六格 Dota 装备栏（ThingOwner）与装备生命周期 |
| [../Source/FloatMenuOptionProvider_DotaEquip.cs](../Source/FloatMenuOptionProvider_DotaEquip.cs) | 右键地面 Dota 装备 → 放入六格装备栏 |
| [../Source/Gizmo_DotaEquipmentSlots.cs](../Source/Gizmo_DotaEquipmentSlots.cs) | 六格装备 UI（属性栏旁） |
| [../Source/DotaDamageTypes.cs](../Source/DotaDamageTypes.cs) | 伤害元素/来源分类与上下文栈 |
| [../Source/Harmony_DotaDamage.cs](../Source/Harmony_DotaDamage.cs) | 物理/法术减伤与攻击/技能吸血补丁 |
| [../Source/StatPart_DotaUniversalDamageReduction.cs](../Source/StatPart_DotaUniversalDamageReduction.cs) | Dota2 通用全伤害减免 StatPart |
| [../Source/DotaHeroUtil.cs](../Source/DotaHeroUtil.cs) | 框架 API：IsHero / GetHeroComp / GetInventoryComp / GetMana / GetAttribute |
| [../Source/DotaAttribute.cs](../Source/DotaAttribute.cs) | 三维枚举 Strength/Agility/Intelligence |
| [../Source/DotaPrimaryAttribute.cs](../Source/DotaPrimaryAttribute.cs) | 主属性枚举 Strength/Agility/Intelligence/Universal |
| [../Source/DotaHeroDebugActions.cs](../Source/DotaHeroDebugActions.cs) | Dev 调试入口（设为/移除/查看英雄） |
| [../Source/StatPart_DotaHeroAttribute.cs](../Source/StatPart_DotaHeroAttribute.cs) | 属性→Stat 动态加成（心灵敏感度等） |
| [../Source/StatPart_DotaHeroAttackSpeed.cs](../Source/StatPart_DotaHeroAttackSpeed.cs) | 攻速→瞄准/冷却 StatPart |
| [../Source/DotaEquipmentUtil.cs](../Source/DotaEquipmentUtil.cs) | DotaEquipmentExtension 判断 |
| [../Source/DotaEquipmentExtension.cs](../Source/DotaEquipmentExtension.cs) | Dota 装备 ThingDef 标记 |
| [../Source/Comp_DestroyedSpawnsItems.cs](../Source/Comp_DestroyedSpawnsItems.cs) | 建筑摧毁掉落 |
| [../Source/DotaDefOf.cs](../Source/DotaDefOf.cs) | DefOf 强类型引用 |
| [../Source/RimDota.csproj](../Source/RimDota.csproj) | 工程配置：net10.0、OutDir、依赖 |
| [../Defs/](../Defs/) | HediffDefs、ResearchProjectDefs、StatDefs、ThingDefs_Buildings/Items、MapGeneration |
| [../Textures/UI/Icons/Dota/](../Textures/UI/Icons/Dota/) | 三维占位图标（Strength/Agility/Intelligence.png） |
| [../Languages/](../Languages/) | Keyed（`DotaMana`=法力/Mana）与 DefInjected |
| [../About/About.xml](../About/About.xml) | Mod 元数据（packageId、HarmonyRimWorld 依赖） |
