# 当前状态（NOW）

> 最后更新：2026-10-22（英雄等级、属性成长与邪月祭坛）。

## 目标
- 本次任务：把 RimDota 建成框架 Mod，已落地 DotaHero 隐藏标记、三维属性（力量/敏捷/智力）、法力值蓝条、装备加成“双层”数据模型，以及基础科技树/建筑/物品。
- 项目整体：RimDota（magicconch.rimdota）即框架，后续内容 Mod 以 RimDota_**** 命名并依赖框架。

## 设计草案（已讨论，待实现）
- 技能系统（本轮仅记录，不实现）：沿用 Dota2 1–30 级和手动技能升级。英雄实例保存可用技能点、已学技能及技能等级；普通技能通常最高 4 级，大招通常 6/12/18 级解锁、最高 3 级，具体解锁和上限按英雄/技能数据配置。能力底层使用 RimWorld `AbilityDef`，Dota 法力消耗由专用 Ability 组件结算；第一版 UI 可先做 Q/W/E/R 占位。PawnKindDef、特殊外观和英雄到来条件暂不进入本阶段。
- 伤害分类：用自定义 DamageDef + 分类表（Attack/Skill/Splash × Physical/Magic），不使用全局静态上下文；进攻吸血在 `Pawn_HealthTracker.PostApplyDamage` 按 `DamageInfo.Def` 分类，攻击吸血/技能吸血分别处理，Splash 不吸血；防御用 `DamageDef.armorCategory`：物理走 Sharp/Blunt，法术走自定义 `Dota_Magic` 分类 + `DotaMagicArmor` StatDef；原版伤害用 `Weapon/Tool → Attack`、`isExplosive → Splash/Skill` 兜底，并预留 XML 映射表。
- 伤害元素分类（用户已定，按最初 1–23 编号）：物理 = Beam、BeamBypassShields、Burn、Digested（以及所有 Sharp/Blunt 分类伤害）；法术 = ElectricalBurn、Flame、NociosphereVaporize、Vaporize、Psychic；其余（Frostbite、EMP、ToxGas、VacuumBurn 及已排除的效果类）不做处理。
- 减伤规则（用户已定）：敏捷→Dota 护甲按 1/6；物理走 Dota 护甲公式（有效护甲 = DotaArmor，不扣原版护甲穿透；原版护甲穿透只作用于原版护甲步骤）；法术走 Dota 2 魔抗公式（基础 25%，智力每点 +0.1% 汇总为一个魔抗值，装备魔抗同样作为来源，正魔抗乘算 R = 1 - Π(1-Ri)）；Dota 减伤之后仍走原版护甲（法术保留原 armorCategory），再叠加加算且可为负的通用减伤（IncomingDamageFactor）；未处理伤害只走原版护甲；攻击/技能分类只用于吸血，与元素分类正交；溅射只标元素、不标攻击/技能，因此不吸血。
- 通用减伤默认规则（用户已定）：之后所有 Dota2 伤害若无特别说明，都默认吃通用减伤；自定义 Dota2 DamageDef 默认 externalViolence=true，例外需显式标注。

## 进展（证据）
- DotaHero 隐藏标记：`Defs/HediffDefs/Hediffs.xml`（`hediffClass=RimDota.Hediff_DotaHero`，Visible=false）+ `Source/Hediff_DotaHero.cs` 的 `HediffComp_DotaHero`。
- 三维属性与派生数值（`Source/Hediff_DotaHero.cs`）：获得英雄身份后通过初始化窗口分配力量/敏捷/智力、主属性和每级成长；力量→生命上限 +2%/点、回血 +0.1/点/秒；敏捷→Dota 护甲 1/6 点/敏捷、攻击速度 +1 点/敏捷；智力→法力上限 +5/点、回蓝 +0.05/点/秒、魔抗 +0.1%/点、心灵敏感度 +2%/点。所有派生值均为“属性贡献 + 装备贡献”，Bonus* 与新增字段默认 0 且入存档。
- 英雄等级与初始化（2026-10-22）：`HediffComp_DotaHero` 新增等级、累计经验、待初始化状态和按 Pawn 序列化的主属性。初始属性预算 = 45 + `floor(所有原版技能等级总和 / 4)`（单个技能最多计 20 级）；成长预算 = 5 + 所有兴趣值合计 × 0.5（无/轻微/强烈分别计 0/1/2，单个最多 2）。每项初始属性最高 35，每项成长最高 5。`Alert_DotaHeroSetupAvailable` 类似皇权“许可未选择”提示，点击打开原生风格 `Dialog_DotaHeroInitialization`；初始三围和成长均可直接输入，成长加减按钮每次调整 0.1；确认前不清除待分配状态。
- Dota2 等级经验（2026-10-22）：`Source/DotaHeroLeveling.cs` 使用 Dota2 当前 1–30 级累计经验表（0、240、640……63900），`Harmony_DotaHeroLeveling` 在敌对 Pawn、野生动物或无阵营动物被英雄击杀时按死者 `MarketValue` 增加经验；升级时按初始化窗口指定的成长值提高三围。属性栏左侧用圆形徽章显示等级，圆环显示本级经验进度，悬停显示本级经验、升级所需经验和累计经验；属性值后的 `+` 继续显示每级成长。
- 回复换算与平衡（`Source/DotaModSettings.cs` + `Source/RimDotaMod.cs`）：生命/法力回复核心单位统一为“点/秒”，每 15 tick 按 `回复/秒 × 15 / 60` 累积。`HealthRegenBalanceScale`（默认 0.2、范围 0–5）只乘到实际生命回复结算，不改变装备描述或属性栏显示的原始值，也不影响法力回复；设置持久化。
- 装备锁定与自动合成（2026-10-21）：`Source/Comp_DotaEquipmentLock.cs` 为每件装备保存 `locked` 状态，锁定装备不参与合成；`Source/Gizmo_DotaEquipmentSlots.cs` 右键菜单按状态显示锁定/解锁，并在锁定装备上覆盖 `Textures/UI/Icons/Dota/DotaEquipmentLockedOverlay.png`（透明背景、右下角锁形占位，后续可用 AI 替换同名资源）。`Source/DotaCombineRecipeDef.cs` + `Defs/DotaCombineRecipes/DotaCombineRecipes.xml` 提供配方数据；`HediffComp_DotaInventory.TryAutoCombine()` 在装备、解锁和读档后按槽位 1→6 扫描，选择最早完成的配方，同格完成时比较实际材料槽位序列，再以 defName 决胜；结果放入最早消耗槽并循环合成。首个配方：吸血面具 + 阔剑 → 疯狂面具（`Dota_MaskOfMadness`，当前接入 +24% 攻击吸血；白/红对半圆形占位图标）。
- Dota 底部状态与英雄页（2026-10-23）：旧 `Gizmo_DotaAttributes` 已替换为 `Source/Gizmo_DotaHeroStatus.cs`。折叠状态宽约 174：等级/经验徽章和展开按钮在上，法力比例条位于整个组件底部；悬停状态区域显示等级、本级/累计经验、法力当前值/最大值及百分比。展开后右侧显示六格装备，底部法力条横跨状态和装备区域，并显示具体数值；`HediffComp_DotaInventory.inventoryExpanded` 入存档，非征兆状态点击按钮保持展开，征兆状态自动强制展开。完整属性与装备管理迁入全局注入的 `Source/ITab_Pawn_Dota.cs`，当前分为“概览/装备”两页；概览按 Dota2 截图改为攻击/防御双面板和三色属性块。`Source/Harmony_InjectDotaInspectTab.cs` 在 `ThingDef.ResolveReferences` 前为所有 Humanlike 种族注入页签，非英雄通过 `IsVisible` 自动隐藏，其他种族内容 Mod 无需再写 UI 补丁。
- Dota 攻击力显示（2026-10-23）：`HediffComp_DotaHero` 新增基础攻击力额外加成 `baseAttackDamageBonus`、额外攻击力加成 `bonusAttackDamageBonus`（入存档）、主属性攻击力和装备攻击力动态属性。单项主属性每点提供 1 点基础攻击力，全才每点力量/敏捷/智力提供 0.5 点；装备通过 `DotaAttackBonus` 汇总为额外攻击力。游戏内只显示“攻击力：基础值 +额外值”。当前攻击力只用于 UI，不影响 RimWorld 伤害。装备数值：攻击之刃 +9、阔剑 +15、秘银锤 +24、疯狂面具 +20（Dota2 当前数据）。
- Dota 攻速与 RimWorld 攻击时间（2026-10-23）：总攻速 = 100 + 敏捷 + 装备攻速 + 临时攻速，限制为 20–500；RimWorld 瞄准延迟、远程冷却和近战冷却统一乘以 `100 / 总攻速`。100 为原速，500 为 5 倍攻速（间隔20%），20 为 0.2 倍攻速（间隔500%）。属性页显示限幅后的总攻速。
- Dota StatPart 请求兼容修复（2026-10-23）：RimWorld 计算小人自身 Stat 时把 Pawn 放在 `StatRequest.Thing` 而非 `StatRequest.Pawn`。四类 Dota StatPart 现统一通过 `DotaHeroUtil.RequestPawn` 兼容两种请求；此前只读 `req.Pawn` 会导致狂热攻速、移速、智力心灵敏感度等修正被静默跳过。`dotnet build Source/RimDota.csproj` 已通过，待游戏内验收。
- 远程闪避显示（2026-10-23）：新增独立 `DotaRangedDodgeChance` StatDef（默认 0），不与绮罗的 `Ancot_RangeDodgeChance` 或命中流程交互；近战闪避读取原版 `MeleeDodgeChance`。属性页显示为 `远程 / 近战`，当前只用于显示。
- Dota 装备栏交互（2026-10-23）：`HediffComp_DotaInventory` 现在额外保存六格 `slotThingIds` 映射，支持真正的固定空格、拖动移动和两格交换；旧存档无映射时按 `ThingOwner` 原顺序迁移。自动合成扫描固定槽位顺序 1→6。左键单击可使用主动装备，左键按住拖动换位或拖出装备栏丢下，右键继续显示锁定/解锁、丢下、查看信息菜单。当前不映射数字快捷键、不显示槽位数字，但保留 `TryUseEquipmentSlot(int)` 公共入口。
- 主动装备与冷却（2026-10-23）：新增 `DotaActiveItemExtension`、装备实例 `Comp_DotaEquipmentCooldown` 和 `DotaActiveUseMode.Self`。相同 `cooldownGroup` 的装备共享最长剩余冷却；冷却随装备实例移动/丢下/重新装备保存。槽位使用 Dota 式半透明旋转扇形遮罩显示剩余冷却，并在中央显示剩余秒数。
- 装备背景描述（2026-10-23）：从 `https://wiki.dota2.com.cn/` 的物品 API 匹配现有装备 lore，并作为斜体文本追加到机制描述后；未在当前 wiki 条目中定义的“超级闪烁”保持原描述。
- 疯狂面具主动（2026-10-23）：左键使用消耗 25 法力，冷却 16 秒（960 tick），添加持续 6 秒（360 tick）的 `DotaMaskOfMadnessBuff`。状态实际提供 +100 攻击速度、近战 +12%/远程 +8% 实际移动速度、-7 Dota 护甲和语言能力 ×0%；30% 减速抗性仅在 XML 注释记录，尚未实现。沉默暂时只影响语言能力，不阻止使用装备或释放技能。
- 调试入口 `Source/DotaHeroDebugActions.cs`：Dev 模式 → 右键小人 → Pawns → “设为/移除 Dota 英雄（选中）”“查看 Dota 英雄属性”。
- Harmony 补丁 `Source/HarmonyPatches.cs`：`BodyPartDef.GetMaxHealth` 后置乘 HealthScaleMult 并向上取整（生命上限实际生效、无小数）；另有一次性示例日志补丁。
- StatPart 接线 `Patches/Patch_StatDef_DotaHeroAttributes.xml`：攻速（AimingDelayFactor/RangedCooldownFactor/MeleeCooldownFactor 统一乘以 `AttackCooldownMultiplier = 100 / DisplayAttackSpeed`）与心灵敏感度；护甲改由伤害系统接管。
- 属性装备 `Defs/ThingDefs_Items/DotaEquipment.xml` + `Defs/StatDefs/DotaStats.xml`：力量手套/敏捷便鞋/智力斗篷（各 +3）、力量腰带（+5），均为普通物品并存入六格装备栏，通过 equippedStatOffsets 写入 DotaStrengthBonus/DotaAgilityBonus/DotaIntelligenceBonus；`HediffComp_DotaHero` 三维属性改为“基础值 + 装备加成”，派生数值改用最终属性，使属性加成联动生命/护甲/攻速/法力/心灵。
- Dota 基础组件 `Defs/ThingDefs_Items/DotaEquipment_Components.xml`（32 件）：精灵布带/欢欣之刃/圆环/宝冕/神秘法杖/食人魔之斧/法师长袍/极限法球（属性），锁子甲/板甲/守护指环（护甲），治疗指环/回复戒指/恐鳌之戒（生命回复），贤者面罩/虚无宝石（法力回复），精气之球（法力上限 + 生命上限）、活力之球（生命上限）、短棍（攻速）；攻击力/移速/攻击特效/主动技能先留空并在描述标注“待实现”。新增装备通道 DotaArmorBonus/DotaHealthRegenBonus/DotaMaxHealthBonus/DotaMaxManaBonus/DotaManaRegenBonus/DotaAttackSpeedBonus/DotaMagicResistance/DotaAttackLifesteal/DotaSpellLifesteal 与 `StatPart_DotaHeroAttackSpeed`；派生数值统一为“属性贡献 + 装备贡献”：回血/护甲/法力上限/法力回复扁平合并，攻速按 `100 / 总攻速` 换算实际攻击间隔，生命上限 = 力量百分比 + 装备扁平/1000（每 10 点 = 1%）；披巾 +10% 魔抗、吸血面具 +18% 攻击吸血、巫毒面具 +15% 技能吸血。图标来自 `D:\Code\RimWorld\RimDota\Icon`，力量手套/敏捷便鞋/智力斗篷三件已有装备已替换图标。护甲按敏捷 1/6 → 原始 Dota 护甲点数，物理减伤走 Dota 公式；生命/法力回复数值按“每秒回复”解释，并受 Mod 设置中的平衡系数缩放。
- 回血池与结算 `Source/Hediff_DotaHero.cs`：新增 `pendingHealing`（入存档）与 `HealingMultiplier`（默认 1）；所有 Dota 回血（力量/装备再生、后续吸血/直接回血）统一走 `AddHealing()`，每 15 tick 结算一次，按伤口严重度降序消耗总预算（单伤口最多治到 0，预算耗尽停止），余量丢弃；修复了原先“每个伤口都吃满回血”导致总回血随伤口数放大的问题。
- 邪月祭坛（2026-10-22）：`DotaCrescentAltar` 为 3×2 `Building_WorkTable`，造价 100 任意石料、50 翡翠、250 铁，带 `ITab_Bills`，后续可直接添加工作台配方。研究页已改名“遗迹/遗迹科技”；新增 `DesignationCategoryDef DotaRelicBuildings`（“遗迹建筑”，位于生产和家具之间），祭坛放入该分类并需要 `DotaTech1` 解锁。`FloatMenuOptionProvider_DotaSacrificeRelic` 在选中的非英雄殖民者右键祭坛时寻找地图上最近、可到达且可预约的遗迹碎片；`JobDriver_DotaSacrificeRelic` 拾取碎片、走到祭坛、持续 300 tick 后销毁碎片并授予 Dota 英雄身份。占位图标为 `Textures/Things/Building/DotaCrescentAltar.png`（棕色矩形底座 + 淡蓝色圆）。
- 基础内容：月亮碎片建筑（3×2，开局生成）+ 摧毁掉落遗迹碎片与 50 翡翠（`Defs/ThingDefs_Buildings`、`Defs/ThingDefs_Items`、`Defs/MapGeneration`、`Source/Comp_DestroyedSpawnsItems.cs`）；遗迹科技线（`Defs/ResearchProjectDefs/Research_Dota.xml`，神秘通讯频道需微电子基础，按用户更正）；攻击之爪占位装备（`Defs/ThingDefs_Items/DotaEquipment.xml`，普通物品，暂无属性）。
- 工具链：rimworld_modder 分析工具已挂载；构建 `dotnet build Source/RimDota.csproj` → 0 警告 0 错误（2026-10-21）。
- 伤害系统（2026-10-21）：`Source/DotaDamageTypes.cs` + `Source/Harmony_DotaDamage.cs`；元素/来源分类、线程静态上下文栈；物理走 Dota 护甲（敏捷/6、装备原始点数、不扣原版护甲穿透）、法术走 Dota 2 魔抗（基础 25% + 智力 0.1%/点 + 装备，正魔抗乘算）；Dota 减伤后仍走原版护甲；通用减伤走 `IncomingDamageFactor`（加算、可为负，接口已留）；攻击/技能吸血在 `Pawn.PostApplyDamage` 用实际伤害结算，溅射不吸血。旧的 `StatPart_DotaHeroArmor` 与 ArmorRating 补丁已移除。

- Dota 六格装备栏（2026-10-21）：`Source/HediffComp_DotaInventory.cs` 用独立 `ThingOwner<Thing>` 保存最多 6 件装备，装备不进入 `WornApparel`；`Source/Gizmo_DotaEquipmentSlots.cs` 的静态绘制方法按 3×2 显示六格，供底部展开状态和 Dota ITab 共用；`Source/FloatMenuOptionProvider_DotaEquip.cs` 支持右键地面装备，满 6 件显示“Dota 装备栏已满”，不可达时显示“无法到达 Dota 装备”，可达时通过 `JobDriver_DotaEquip` 让小人寻路到物品旁再装备；`HediffComp_DotaHero.EquippedBonus` 从六件装备汇总；死亡按用户要求不掉落（销毁），手动移除英雄会掉出。2026-10-21 重构：Dota 装备改为普通 ThingWithComps（DotaItemBase + DotaEquipmentExtension），删除 DotaGear 层与 Wear/隐藏 Wear 补丁；旧存档如仍有 WornApparel 装备，需要先用上一版迁移。

## 阻塞 / 待确认
- StatPart 接线、回血池、32 件基础组件、伤害系统、固定六格装备栏、装备锁定、自动合成、英雄等级/经验、初始化属性窗口、邪月祭坛献祭、参考图属性页、主动装备和共享冷却已完成（XML + C#），但尚未游戏内验证：需确认固定空格拖动、交换/拖出、右键菜单、疯狂面具耗蓝/冷却/遮罩/状态、同组冷却、属性页、祭坛和献祭、击杀升级，以及日志无红字。
- 属性图标：已决定暂用红/绿/蓝占位圆点；真图标已定位到本机 Dota 2 VPK（`panorama/images/primary_attribute_icons/primary_attribute_icon_{strength,agility,intelligence}_psd.vtex_c` 等），为 Valve 编译纹理（BinaryKV3 + BC 压缩），待后续写 VTex 解码器或找 PNG 镜像，发布前需替换（Valve 版权）。
- 技能点、技能列表、手动技能升级和技能 UI 尚未实现，方案记录在设计草案。

## 下一步（建议）
- （延后）补上三张真属性图标（写 VTex 解码器或找 PNG 镜像）。
- （暂缓，用户指示先不做）攻击之刃等“派生属性”装备（如 +攻击）接入 `HediffComp_DotaHero` 的 Bonus* 字段；属性加成装备通道已就绪。
- （暂缓，用户指示先不做）实现技能点、技能列表、手动技能升级和技能占位 UI。

## 证据入口
- 构建：`dotnet build Source/RimDota.csproj` → 0 警告 0 错误（2026-10-21）。
- 游戏日志：`C:\Users\Administrator\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Logs\Player.log`。
- 目录/版本/配置：见 [MAP.md](MAP.md)。
- 构建/热重载/验证步骤：见 [RUNBOOK.md](RUNBOOK.md)。
