# 已确认决策（DECISIONS）

> 以下为工程中已落地的约定（区别于建议/计划）。最后更新：2026-10-18。

| 决策 | 理由/范围 | 来源 |
| --- | --- | --- |
| 语言与目标框架：C# / .NET 10（net10.0） | RimWorld 1.7 起迁移到 .NET 10（Waylith 引擎） | `Source/RimDota.csproj` + 游戏 `RimWorld.runtimeconfig.json` |
| 使用 Harmony 补丁（Lib.Harmony 2.*） | 定制游戏逻辑 | `Source/RimDota.csproj` + [../../AGENTS.md](../../AGENTS.md) |
| RimWorld 引用走 NuGet `Krafs.Rimworld.Ref` 1.7.4979-alpha | 与 1.7 测试版匹配，不直接引用游戏程序集 | `Source/RimDota.csproj` |
| 命名空间/程序集：`RimDota` / `RimDota.dll` | 统一命名 | `Source/RimDota.csproj` |
| packageId `magicconch.rimdota`，HarmonyId 与其一致 | 唯一标识写进存档；Harmony 约定 `author.modname` | `About/About.xml` + `Source/RimDotaMod.cs` |
| Harmony 入口在 `RimDotaMod` 构造函数 `PatchAll()` | 初始化时机 | `Source/RimDotaMod.cs` |
| XML def 放 `Defs/`，代码经 `DefOf`（`DotaDefOf`）强类型引用 | 强类型接线，字段名=defName | [../../AGENTS.md](../../AGENTS.md) + `Source/DotaDefOf.cs` |
| 移除 0Reloader；Harmony 用 `Lib.Harmony.Ref` 引用 + 依赖 HarmonyRimWorld（`brrainz.harmony`） | 1.7 游戏不再自带 Harmony；由 HarmonyRimWorld 统一提供，避免版本冲突 | `Source/RimDota.csproj` + `About/About.xml` |
| 编译输出到 `Assemblies/`（`OutDir=..\Assemblies\`） | 游戏直接从 Assemblies 加载 | `Source/RimDota.csproj` |
| 三维属性系统：力量/敏捷/智力，初始 20/20/20，成长值单独存档（默认 0） | 后续按等级提升，成长逻辑未做 | `Source/Hediff_DotaHero.cs` |
| 派生数值双层结构：属性基础 + 装备 Bonus* 加成，Bonus* 默认 0 且入存档 | 装备加数值统一写 Bonus*，避免重复权威来源 | `Source/Hediff_DotaHero.cs` |
| 属性换算系数：力量→生命上限 +2%/点、回血 +0.1/点/秒；敏捷→护甲 +1%/点、攻击速度 +1点/点；智力→法力上限 +5/点、回蓝 +0.05/点/秒、心灵敏感度 +2%/点 | 用户确认；回复速度采用 Dota 2 每秒单位，生命回复每 15 tick 按 `回复/秒 × 15 / 60` 累积 | `Source/Hediff_DotaHero.cs` |
| 生命回复平衡系数 `HealthRegenBalanceScale`：默认 0.2，范围 0–5 | 只乘到实际生命回复结算；不改变装备描述和 Dota 属性栏显示的原始每秒值，法力回复不受影响；在 Mod 设置中调整并持久化 | `Source/DotaModSettings.cs` + `Source/RimDotaMod.cs` |
| Dota 装备锁定状态存放在装备自身的 `Comp_DotaEquipmentLock` | 锁定状态跟随物品而非槽位，卸下/重装不会串位；锁定装备仅退出自动合成，仍可手动丢下和查看 | `Source/Comp_DotaEquipmentLock.cs` |
| Dota 装备自动合成使用独立 `DotaCombineRecipeDef` | 配方只声明结果和材料，不设置配方优先级；装备、解锁和读档后按槽位顺序选择最早完成的配方，同格完成时比较实际材料槽位序列，再以 defName 决胜 | `Source/DotaCombineRecipeDef.cs` + `Source/HediffComp_DotaInventory.cs` |
| 疯狂面具配方：吸血面具 + 阔剑 | 第一个自动合成配方；装备提供 +24% 攻击吸血和 +20 攻击力，主动状态为“狂热”，图标暂用白/红对半占位 | `Defs/DotaCombineRecipes/DotaCombineRecipes.xml` + `Defs/ThingDefs_Items/DotaEquipment.xml` |
| 英雄初始属性预算：基础 45 点 + 每累计 4 点原版技能等级增加 1 点 | 单个技能等级最多按 20 计；玩家在原生风格窗口分配力量/敏捷/智力，每项最高 35 | `Source/DotaHeroSetupUtility.cs` + `Source/Dialog_DotaHeroInitialization.cs` |
| 英雄初始成长预算：基础 5 点 + 每个兴趣值 0.5 点 | 无/轻微/强烈兴趣分别计 0/1/2，单项最高计 2；每项 Dota 成长最高 5 | `Source/DotaHeroSetupUtility.cs` + `Source/Dialog_DotaHeroInitialization.cs` |
| 英雄等级使用 Dota2 1–30 级经验表，击杀敌对 Pawn、野生动物或无阵营动物按 MarketValue 给实际击杀英雄奖励经验 | 玩家阵营动物不奖励经验；等级和属性成长存入 Hero 组件，升级时增加对应成长值 | `Source/DotaHeroLeveling.cs` + `Source/Harmony_DotaHeroLeveling.cs` |
| 邪月祭坛：3×2 工作台，造价 100 任意石料 + 50 翡翠 + 250 铁 | 右键祭坛可由非英雄殖民者献祭地图上可用的遗迹碎片；拾取并行走至祭坛后持续 300 tick，完成后授予英雄身份 | `Defs/ThingDefs_Buildings/Buildings_Dota.xml` + `Source/JobDriver_DotaSacrificeRelic.cs` |
| 遗迹科技与建筑分类：研究页显示“遗迹/遗迹科技”；新增“遗迹建筑”分类 | 邪月祭坛放入 `DotaRelicBuildings`，并且需要 `DotaTech1` 解锁 | `Defs/ResearchProjectDefs/Research_Dota.xml` + `Defs/DesignationCategories/DotaDesignationCategories.xml` |
| 技能系统本轮只保存方案 | 后续按 Dota2 等级手动升级技能；使用 `AbilityDef` 承载效果，Dota 法力消耗独立结算 | `.agents/docs/context/NOW.md` |
| 底部 Dota UI 采用圆形等级/经验徽章 + 法力比例条 + 展开按钮 | 非征兆状态保存展开偏好；征兆状态强制展开六格装备，展开时法力条显示具体数值 | `Source/Gizmo_DotaHeroStatus.cs` + `Source/HediffComp_DotaInventory.cs` |
| Dota ITab 通过全局注入提供给所有 Humanlike 种族 | 在 `ThingDef.ResolveReferences` 时为 Humanlike ThingDef 添加 `ITab_Pawn_Dota`，页签只对 Dota 英雄可见；其他种族 Mod 无需额外 UI 补丁 | `Source/Harmony_InjectDotaInspectTab.cs` + `Source/ITab_Pawn_Dota.cs` |
| 攻击力拆分基础攻击力和额外攻击力 | 基础攻击力 = 主属性攻击力 + 基础攻击力加成；额外攻击力 = 装备 `DotaAttackBonus` + 其他额外攻击力加成。单项主属性每点 +1，全才每点三围 +0.5；游戏内只显示“攻击力”，当前仅用于显示，不影响战斗 | `Source/Hediff_DotaHero.cs` + `Defs/StatDefs/DotaStats.xml` |
| Dota2 当前基础装备攻击力：攻击之刃 9、阔剑 15、秘银锤 24、疯狂面具 20 | 这些数值进入 Dota 额外攻击力显示，暂不修改 RimWorld 实际伤害 | `Defs/ThingDefs_Items/DotaEquipment.xml` + `DotaEquipment_Components.xml` |
| 远程闪避使用 RimDota 独立 `DotaRangedDodgeChance` | 不改绮罗的 `Ancot_RangeDodgeChance` 或闪避实现；近战闪避读取原版 `MeleeDodgeChance`，当前仅显示 | `Defs/StatDefs/DotaStats.xml` + `Source/ITab_Pawn_Dota.cs` |
| Dota 总攻速限制为 20–500，RimWorld 攻击间隔倍率 = 100 / 总攻速 | 同一倍率乘入 AimingDelayFactor、RangedCooldownFactor、MeleeCooldownFactor；500 为 5 倍攻速，20 为 0.2 倍攻速 | `Source/Hediff_DotaHero.cs` + `Source/StatPart_DotaHeroAttackSpeed.cs` |
| 非 Dota 角色的攻速削弱以后直接作用于 RimWorld 瞄准与冷却 Stat | 使用独立 Hediff/StatPart 乘到 AimingDelayFactor、RangedCooldownFactor、MeleeCooldownFactor，不受 Dota 20–500 攻速通道限制 | 待后续具体效果实现 |
| 六格装备栏使用固定 `slotThingIds` 映射 | ThingOwner 保持物品所有权，映射决定固定槽位；支持空格、拖动、交换和旧存档顺序迁移，自动合成按固定槽位顺序扫描 | `Source/HediffComp_DotaInventory.cs` |
| 主动装备使用统一 `TryUseEquipmentSlot(int)` 入口 | 当前左键使用；数字键映射和槽位数字显示暂不实现，后续可直接接该接口 | `Source/HediffComp_DotaInventory.cs` |
| 主动装备冷却按 `cooldownGroup` 共享 | 每个装备实例保存冷却结束 tick；使用其中一件时同组所有已装备物品同步。冷却跟随物品，重新装备时取同组最长剩余时间 | `Source/DotaActiveItemExtension.cs` + `Source/Comp_DotaEquipmentCooldown.cs` |
| 疯狂面具主动：25 法力、16 秒冷却、6 秒持续 | 生效期间 +100 攻击速度、近战 +12%/远程 +8% RimWorld 移动速度、-7 Dota 护甲、语言能力 ×0%；30% 减速抗性仅保留 XML 注释，不进入描述 | `Defs/ThingDefs_Items/DotaEquipment.xml` + `Defs/HediffDefs/Hediffs.xml` |
| 蓝条布局：法力标签在左、三维“图标+值+成长”在上、成长用 Tiny 灰字；占位图标 | 用户要求；真图标需 VTex 解码 | `Source/Gizmo_DotaMana.cs` + `Textures/UI/Icons/Dota/` |
| 调试入口：Dev 右键 Pawns 设为/移除/查看英雄 | 便于游戏内验证 | `Source/DotaHeroDebugActions.cs` |
| 护甲/攻速/心灵敏感度当前用 XML 静态 statOffsets，暂不动态驱动 | 动态 StatPart 待用户确认后再接线 | `Defs/HediffDefs/Hediffs.xml` + `Source/StatPart_DotaHeroAttribute.cs` |

> 未确认/待核实：护甲/攻速/心灵敏感度是否改为动态、实际 Dota 玩法范围、正式 Mod 描述、是否沿用 MIT 发布（LICENSE 缺版权行）、发布目标。
