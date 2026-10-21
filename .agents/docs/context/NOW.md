# 当前状态（NOW）

> 最后更新：2026-10-21（Dota 伤害系统实现）。

## 目标
- 本次任务：把 RimDota 建成框架 Mod，已落地 DotaHero 隐藏标记、三维属性（力量/敏捷/智力）、法力值蓝条、装备加成“双层”数据模型，以及基础科技树/建筑/物品。
- 项目整体：RimDota（magicconch.rimdota）即框架，后续内容 Mod 以 RimDota_**** 命名并依赖框架。

## 设计草案（已讨论，待实现）
- 伤害分类：用自定义 DamageDef + 分类表（Attack/Skill/Splash × Physical/Magic），不使用全局静态上下文；进攻吸血在 `Pawn_HealthTracker.PostApplyDamage` 按 `DamageInfo.Def` 分类，攻击吸血/技能吸血分别处理，Splash 不吸血；防御用 `DamageDef.armorCategory`：物理走 Sharp/Blunt，法术走自定义 `Dota_Magic` 分类 + `DotaMagicArmor` StatDef；原版伤害用 `Weapon/Tool → Attack`、`isExplosive → Splash/Skill` 兜底，并预留 XML 映射表。
- 伤害元素分类（用户已定，按最初 1–23 编号）：物理 = Beam、BeamBypassShields、Burn、Digested（以及所有 Sharp/Blunt 分类伤害）；法术 = ElectricalBurn、Flame、NociosphereVaporize、Vaporize、Psychic；其余（Frostbite、EMP、ToxGas、VacuumBurn 及已排除的效果类）不做处理。
- 减伤规则（用户已定）：敏捷→Dota 护甲按 1/6；物理走 Dota 护甲公式（有效护甲 = DotaArmor，不扣原版护甲穿透；原版护甲穿透只作用于原版护甲步骤）；法术走 Dota 2 魔抗公式（基础 25%，智力每点 +0.1% 汇总为一个魔抗值，装备魔抗同样作为来源，正魔抗乘算 R = 1 - Π(1-Ri)）；Dota 减伤之后仍走原版护甲（法术保留原 armorCategory），再叠加加算且可为负的通用减伤（IncomingDamageFactor）；未处理伤害只走原版护甲；攻击/技能分类只用于吸血，与元素分类正交；溅射只标元素、不标攻击/技能，因此不吸血。
- 通用减伤默认规则（用户已定）：之后所有 Dota2 伤害若无特别说明，都默认吃通用减伤；自定义 Dota2 DamageDef 默认 externalViolence=true，例外需显式标注。

## 进展（证据）
- DotaHero 隐藏标记：`Defs/HediffDefs/Hediffs.xml`（`hediffClass=RimDota.Hediff_DotaHero`，Visible=false）+ `Source/Hediff_DotaHero.cs` 的 `HediffComp_DotaHero`。
- 三维属性与派生数值（`Source/Hediff_DotaHero.cs`）：力量/敏捷/智力初始 20/20/20；力量→生命上限 +2%/点、回血 +1/点/天；敏捷→Dota 护甲 1/6 点/敏捷、瞄准/攻速 -0.5%/点；智力→法力上限 +5/点、回蓝 +5/点/天、魔抗 +0.1%/点、心灵敏感度 +2%/点。所有派生值均为“属性贡献 + 装备贡献”，Bonus* 与新增字段默认 0 且入存档。
- Dota 属性栏 `Source/Gizmo_DotaAttributes.cs`（原 `Gizmo_DotaMana.cs`）：宽 230；“法力”标签在蓝条左侧；顶部三组“属性图标 + 当前值 + 成长（Tiny 灰字）”；蓝条居中显示“当前值/上限”；悬停 tooltip 展示完整换算。占位图标为 `Textures/UI/Icons/Dota/{Strength,Agility,Intelligence}.png`（32×32 红/绿/蓝圆点）。主属性枚举 `Source/DotaPrimaryAttribute.cs`（力量/敏捷/智力/全才）挂在 `HediffCompProperties_DotaHero.primaryAttribute`，默认全才；主属性对应数值用其颜色显示（力量红/敏捷绿/智力蓝），全才三色齐亮，其余默认白。
- 调试入口 `Source/DotaHeroDebugActions.cs`：Dev 模式 → 右键小人 → Pawns → “设为/移除 Dota 英雄（选中）”“查看 Dota 英雄属性”。
- Harmony 补丁 `Source/HarmonyPatches.cs`：`BodyPartDef.GetMaxHealth` 后置乘 HealthScaleMult 并向上取整（生命上限实际生效、无小数）；`Pawn_ApparelTracker.Wear` 限制 DotaGear 层只能英雄穿；另有一次性示例日志补丁。
- StatPart 接线 `Patches/Patch_StatDef_DotaHeroAttributes.xml`：攻速（AimingDelayFactor/RangedCooldownFactor/MeleeCooldownFactor，敏捷/装备统一走 AimAndCooldownFactor）与心灵敏感度；护甲改由伤害系统接管。
- 属性装备 `Defs/ThingDefs_Apparel/Apparel_Dota.xml` + `Defs/StatDefs/DotaStats.xml`：力量手套/敏捷便鞋/智力斗篷（各 +3）、力量腰带（+5），均为 DotaGear 层，通过 equippedStatOffsets 写入 DotaStrengthBonus/DotaAgilityBonus/DotaIntelligenceBonus；`HediffComp_DotaHero` 三维属性改为“基础值 + 装备加成”，派生数值改用最终属性，使属性加成联动生命/护甲/攻速/法力/心灵。
- Dota 基础组件 `Defs/ThingDefs_Apparel/Apparel_Dota_Components.xml`（32 件）：精灵布带/欢欣之刃/圆环/宝冕/神秘法杖/食人魔之斧/法师长袍/极限法球（属性），锁子甲/板甲/守护指环（护甲），治疗指环/回复戒指/恐鳌之戒（生命回复），贤者面罩/虚无宝石（法力回复），精气之球（法力上限 + 生命上限）、活力之球（生命上限）、短棍（攻速）；攻击力/移速/攻击特效/主动技能先留空并在描述标注“待实现”。新增装备通道 DotaArmorBonus/DotaHealthRegenBonus/DotaMaxHealthBonus/DotaMaxManaBonus/DotaManaRegenBonus/DotaAttackSpeedBonus/DotaMagicResistance/DotaAttackLifesteal/DotaSpellLifesteal 与 `StatPart_DotaHeroAttackSpeed`；派生数值统一为“属性贡献 + 装备贡献”：回血/护甲/法力上限/法力回复扁平合并，攻速走 AimAndCooldownFactor（1 点装备攻速 = -0.5% 冷却），生命上限 = 力量百分比 + 装备扁平/1000（每 10 点 = 1%）；披巾 +10% 魔抗、吸血面具 +18% 攻击吸血、巫毒面具 +15% 技能吸血。图标来自 `D:\Code\RimWorld\RimDota\Icon`，力量手套/敏捷便鞋/智力斗篷三件已有装备已替换图标。护甲按敏捷 1/6 → 原始 Dota 护甲点数，物理减伤走 Dota 公式；生命/法力回复数值直接作为“每天回复”（待平衡调整）。
- 回血池与结算 `Source/Hediff_DotaHero.cs`：新增 `pendingHealing`（入存档）与 `HealingMultiplier`（默认 1）；所有 Dota 回血（力量/装备再生、后续吸血/直接回血）统一走 `AddHealing()`，每 15 tick 结算一次，按伤口严重度降序消耗总预算（单伤口最多治到 0，预算耗尽停止），余量丢弃；修复了原先“每个伤口都吃满回血”导致总回血随伤口数放大的问题。
- 基础内容：月亮碎片建筑（3×2，开局生成）+ 摧毁掉落遗迹碎片与 50 翡翠（`Defs/ThingDefs_Buildings`、`Defs/ThingDefs_Items`、`Defs/MapGeneration`、`Source/Comp_DestroyedSpawnsItems.cs`）；遗迹科技线（`Defs/ResearchProjectDefs/Research_Dota.xml`，神秘通讯频道需微电子基础，按用户更正）；攻击之爪占位装备（`Defs/ThingDefs_Apparel`，DotaGear 层，暂无属性）。
- 工具链：rimworld_modder 分析工具已挂载；构建 `dotnet build Source/RimDota.csproj` → 0 警告 0 错误（2026-10-21）。
- 伤害系统（2026-10-21）：`Source/DotaDamageTypes.cs` + `Source/Harmony_DotaDamage.cs`；元素/来源分类、线程静态上下文栈；物理走 Dota 护甲（敏捷/6、装备原始点数、不扣原版护甲穿透）、法术走 Dota 2 魔抗（基础 25% + 智力 0.1%/点 + 装备，正魔抗乘算）；Dota 减伤后仍走原版护甲；通用减伤走 `IncomingDamageFactor`（加算、可为负，接口已留）；攻击/技能吸血在 `Pawn.PostApplyDamage` 用实际伤害结算，溅射不吸血。旧的 `StatPart_DotaHeroArmor` 与 ArmorRating 补丁已移除。

- Dota 六格装备栏（2026-10-21）：`Source/HediffComp_DotaInventory.cs` 用独立 `ThingOwner<Thing>` 保存最多 6 件装备，装备不进入 `WornApparel`；`Source/FloatMenuOptionProvider_DotaEquip.cs` 支持右键地面装备，满 6 件显示“Dota 装备栏已满”；`Source/Gizmo_DotaEquipmentSlots.cs` 在属性栏旁显示六格；`HediffComp_DotaHero.EquippedBonus` 改为从六件装备汇总；死亡按用户要求不掉落（销毁），手动移除英雄会掉出；旧存档的 `WornApparel` 会在加载时迁入六格。

## 阻塞 / 待确认
- StatPart 接线、回血池、32 件基础组件、伤害系统与六格装备栏已完成（XML + C#），但尚未游戏内验证：需确认右键装备、满 6 件拒绝、六格 UI、属性汇总、死亡/移除生命周期、物理/法术减伤、通用减伤、攻击/技能吸血、多伤口回血结算，以及日志无红字。
- 属性图标：已决定暂用红/绿/蓝占位圆点；真图标已定位到本机 Dota 2 VPK（`panorama/images/primary_attribute_icons/primary_attribute_icon_{strength,agility,intelligence}_psd.vtex_c` 等），为 Valve 编译纹理（BinaryKV3 + BC 压缩），待后续写 VTex 解码器或找 PNG 镜像，发布前需替换（Valve 版权）。
- 属性成长值（growth）字段已建，全部为 0；等级/经验/成长系统未做。

## 下一步（建议）
- （延后）补上三张真属性图标（写 VTex 解码器或找 PNG 镜像）。
- （暂缓，用户指示先不做）实现等级/经验与属性成长值（growth 写入逻辑）。
- （暂缓，用户指示先不做）攻击之刃等“派生属性”装备（如 +攻击）接入 `HediffComp_DotaHero` 的 Bonus* 字段；属性加成装备通道已就绪。

## 证据入口
- 构建：`dotnet build Source/RimDota.csproj` → 0 警告 0 错误（2026-10-21）。
- 游戏日志：`C:\Users\Administrator\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Logs\Player.log`。
- 目录/版本/配置：见 [MAP.md](MAP.md)。
- 构建/热重载/验证步骤：见 [RUNBOOK.md](RUNBOOK.md)。
