using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// 隐藏的英雄标记：重写 Visible 返回 false，不出现在健康面板。
/// 仅作为“英雄”标记，承载后续英雄系统（法力、等级、经验、技能）。
/// </summary>
public class Hediff_DotaHero : HediffWithComps
{
	public override bool Visible => false;
}

/// <summary>
/// 英雄状态组件：存放英雄专属数据（法力、三维属性、成长与装备加成）。
/// </summary>
public class HediffComp_DotaHero : HediffComp
{
	public const float BaseMana = 200f;
	public const float BaseAttribute = 20f;
	public const float BaseAttackSpeed = 100f;
	public const float MinAttackSpeed = 20f;
	public const float MaxAttackSpeed = 500f;

	private const int RegenIntervalTicks = 15;
	private const float MinSettleHealing = 0.1f;

	public HediffCompProperties_DotaHero Props => (HediffCompProperties_DotaHero)props;

	/// <summary>英雄主属性，决定 Dota 属性栏中三维数据的着色。</summary>
	public DotaPrimaryAttribute PrimaryAttribute => primaryAttribute;

	/// <summary>英雄是否仍在等待玩家完成初始属性分配。</summary>
	public bool AwaitingInitialSetup => !initialSetupComplete;

	public int HeroLevel => heroLevel;

	public float Experience => experience;

	public float ExperienceWithinLevel =>
		experience - DotaHeroLeveling.TotalExperienceForLevel(heroLevel);

	public float ExperienceForNextLevel => heroLevel >= DotaHeroLeveling.MaxLevel
		? 0f
		: DotaHeroLeveling.TotalExperienceForLevel(heroLevel + 1)
			- DotaHeroLeveling.TotalExperienceForLevel(heroLevel);

	/// <summary>当前待结算的 Dota 回血量（点）。</summary>
	public float PendingHealing => pendingHealing;

	/// <summary>治疗系数（默认 1）；治疗增强/削弱通过修改它实现。</summary>
	public float HealingMultiplier
	{
		get => healingMultiplier;
		set => healingMultiplier = value;
	}

	private static readonly Comparison<Hediff_Injury> SeverityDescending = (a, b) => b.Severity.CompareTo(a.Severity);

	private float mana = BaseMana;
	private int heroLevel = 1;
	private float experience;
	private bool initialSetupComplete = true;
	private DotaPrimaryAttribute primaryAttribute = DotaPrimaryAttribute.Universal;
	private float strength = BaseAttribute;
	private float agility = BaseAttribute;
	private float intelligence = BaseAttribute;
	private int nextRegenTick;
	private float pendingHealing;
	private float healingMultiplier = 1f;
	private readonly List<Hediff_Injury> tmpInjuries = new List<Hediff_Injury>();
	private float strengthGrowth;
	private float agilityGrowth;
	private float intelligenceGrowth;
	private float bonusHealthScaleMult;
	private float baseAttackDamageBonus;
	private float bonusAttackDamageBonus;
	private float bonusHealthRegen;
	private float bonusArmor;
	private float bonusAimAndCooldown;
	private float bonusMaxMana;
	private float bonusManaRegen;
	private float bonusPsychic;
	private float bonusMagicResistance;
	private float bonusUniversalDamageReduction;
	private float bonusAttackLifesteal;
	private float bonusSpellLifesteal;

	public float Strength
	{
		get => strength + EquippedBonus(DotaDefOf.DotaStrengthBonus);
		set => strength = value < 0f ? 0f : value;
	}

	public float Agility
	{
		get => agility + EquippedBonus(DotaDefOf.DotaAgilityBonus);
		set => agility = value < 0f ? 0f : value;
	}

	public float Intelligence
	{
		get => intelligence + EquippedBonus(DotaDefOf.DotaIntelligenceBonus);
		set => intelligence = value < 0f ? 0f : value;
	}

	private float EquippedBonus(StatDef stat)
	{
		if (Pawn == null)
		{
			return 0f;
		}

		HediffComp_DotaInventory inventory = DotaHeroUtil.GetInventoryComp(Pawn);
		if (inventory == null)
		{
			return 0f;
		}

		float total = 0f;
		for (int i = 0; i < inventory.Count; i++)
		{
			Thing item = inventory.GetAt(i);
			if (item != null)
			{
				total += StatWorker.StatOffsetFromGear(item, stat);
			}
		}
		return total;
	}

	public float StrengthGrowth
	{
		get => strengthGrowth;
		set => strengthGrowth = value;
	}

	public float AgilityGrowth
	{
		get => agilityGrowth;
		set => agilityGrowth = value;
	}

	public float IntelligenceGrowth
	{
		get => intelligenceGrowth;
		set => intelligenceGrowth = value;
	}

	public override void CompPostMake()
	{
		base.CompPostMake();
		primaryAttribute = Props.primaryAttribute;
	}

	/// <summary>新英雄获得身份后进入待分配状态，并清空初始战斗属性。</summary>
	public void BeginInitialSetup()
	{
		heroLevel = 1;
		experience = 0f;
		initialSetupComplete = false;
		primaryAttribute = DotaPrimaryAttribute.Universal;
		strength = 0f;
		agility = 0f;
		intelligence = 0f;
		strengthGrowth = 0f;
		agilityGrowth = 0f;
		intelligenceGrowth = 0f;
		mana = 0f;
	}

	public void ApplyInitialSetup(
		DotaPrimaryAttribute primary,
		float initialStrength,
		float initialAgility,
		float initialIntelligence,
		float strengthPerLevel,
		float agilityPerLevel,
		float intelligencePerLevel)
	{
		primaryAttribute = primary;
		strength = Mathf.Clamp(initialStrength, 0f, 35f);
		agility = Mathf.Clamp(initialAgility, 0f, 35f);
		intelligence = Mathf.Clamp(initialIntelligence, 0f, 35f);
		strengthGrowth = RoundGrowth(Mathf.Clamp(strengthPerLevel, 0f, 5f));
		agilityGrowth = RoundGrowth(Mathf.Clamp(agilityPerLevel, 0f, 5f));
		intelligenceGrowth = RoundGrowth(Mathf.Clamp(intelligencePerLevel, 0f, 5f));
		heroLevel = 1;
		experience = 0f;
		initialSetupComplete = true;
		mana = MaxMana;
		NotifyHeroStatsChanged();
	}

	private static float RoundGrowth(float value)
	{
		return Mathf.Round(value * 10f) / 10f;
	}

	public void AddExperience(float amount)
	{
		int startingLevel = heroLevel;
		if (!initialSetupComplete
			|| amount <= 0f
			|| heroLevel >= DotaHeroLeveling.MaxLevel)
		{
			return;
		}

		experience += amount;
		while (heroLevel < DotaHeroLeveling.MaxLevel
			&& experience >= DotaHeroLeveling.TotalExperienceForLevel(heroLevel + 1))
		{
			heroLevel++;
			strength += strengthGrowth;
			agility += agilityGrowth;
			intelligence += intelligenceGrowth;
		}

		if (heroLevel >= DotaHeroLeveling.MaxLevel)
		{
			experience = DotaHeroLeveling.TotalExperienceForLevel(DotaHeroLeveling.MaxLevel);
		}
		NotifyHeroStatsChanged();
		if (heroLevel > startingLevel && Pawn != null)
		{
			Messages.Message(
				Pawn.LabelShortCap + " 升至 Dota 等级 " + heroLevel + "。",
				Pawn,
				MessageTypeDefOf.PositiveEvent);
		}
	}

	private void NotifyHeroStatsChanged()
	{
		if (Pawn?.health != null && parent != null)
		{
			Pawn.health.Notify_HediffChanged(parent);
		}
	}

	/// <summary>生命上限倍率（力量部分）：力量 × 2%/点。</summary>
	public float HealthScaleMultFromStrength => Strength * 0.02f;

	/// <summary>生命回复基础值（点/秒）：力量 × 0.1。</summary>
	public float HealthRegenFromStrength => Strength * 0.1f;

	/// <summary>护甲基础值（Dota 原始护甲点数）：敏捷 × 1/6。</summary>
	public float ArmorFromAgility => Agility / 6f;

	/// <summary>法力上限基础值：智力 × 5。</summary>
	public float MaxManaFromIntelligence => Intelligence * 5f;

	/// <summary>法力回复基础值（点/秒）：智力 × 0.05。</summary>
	public float ManaRegenFromIntelligence => Intelligence * 0.05f;

	/// <summary>心灵敏感度基础加成：智力 × 2%/点。</summary>
	public float PsychicFromIntelligence => Intelligence * 0.02f;

	/// <summary>装备等外部来源的生命上限百分比加成。</summary>
	public float BonusHealthScaleMult
	{
		get => bonusHealthScaleMult;
		set => bonusHealthScaleMult = value;
	}

	/// <summary>装备等外部来源的生命回复加成（点/秒）。</summary>
	public float BonusHealthRegen
	{
		get => bonusHealthRegen;
		set => bonusHealthRegen = value;
	}

	/// <summary>装备等外部来源的护甲加成（原始 Dota 护甲点数）。</summary>
	public float BonusArmor
	{
		get => bonusArmor;
		set => bonusArmor = value;
	}

	/// <summary>旧版线性攻速字段，保留存档兼容；不再参与实际计算。</summary>
	public float BonusAimAndCooldown
	{
		get => bonusAimAndCooldown;
		set => bonusAimAndCooldown = value;
	}

	/// <summary>装备等外部来源的法力上限加成。</summary>
	public float BonusMaxMana
	{
		get => bonusMaxMana;
		set => bonusMaxMana = value;
	}

	/// <summary>装备等外部来源的法力回复加成（点/秒）。</summary>
	public float BonusManaRegen
	{
		get => bonusManaRegen;
		set => bonusManaRegen = value;
	}

	/// <summary>装备等外部来源的心灵敏感度加成。</summary>
	public float BonusPsychic
	{
		get => bonusPsychic;
		set => bonusPsychic = value;
	}

	/// <summary>装备等外部来源的魔抗加成（比例）。</summary>
	public float BonusMagicResistance
	{
		get => bonusMagicResistance;
		set => bonusMagicResistance = value;
	}

	/// <summary>Dota2 用的全伤害减免（加算，可为负）。</summary>
	public float BonusUniversalDamageReduction
	{
		get => bonusUniversalDamageReduction;
		set => bonusUniversalDamageReduction = value;
	}

	/// <summary>攻击吸血（比例）。</summary>
	public float BonusAttackLifesteal
	{
		get => bonusAttackLifesteal;
		set => bonusAttackLifesteal = value;
	}

	/// <summary>技能吸血（比例）。</summary>
	public float BonusSpellLifesteal
	{
		get => bonusSpellLifesteal;
		set => bonusSpellLifesteal = value;
	}

	/// <summary>装备提供的扁平生命上限加成（点）。</summary>
	public float BonusMaxHealthFlat => EquippedBonus(DotaDefOf.DotaMaxHealthBonus);

	/// <summary>装备扁平生命换算成生命上限百分比：每 10 点 = 1%（250 → 25%）。</summary>
	public float HealthScaleMultFromEquipmentHealth => BonusMaxHealthFlat / 1000f;

	/// <summary>最终生命上限倍率 = 1 + 力量百分比 + 其他百分比 + 装备扁平换算百分比。</summary>
	public float HealthScaleMult => 1f + HealthScaleMultFromStrength + bonusHealthScaleMult + HealthScaleMultFromEquipmentHealth;

	/// <summary>最终增加的生命上限（百分比点数）：力量 ×2 + 装备扁平/10。</summary>
	public float FinalMaxHealthBonusPercent => (HealthScaleMult - 1f) * 100f;

	/// <summary>主属性提供的基础攻击力属性部分。全才每点三围提供 0.5。</summary>
	public float AttributeAttackDamage => PrimaryAttribute switch
	{
		DotaPrimaryAttribute.Strength => Strength,
		DotaPrimaryAttribute.Agility => Agility,
		DotaPrimaryAttribute.Intelligence => Intelligence,
		_ => (Strength + Agility + Intelligence) * 0.5f,
	};

	/// <summary>基础攻击力 = 主属性基础攻击力 + 其他基础攻击力加成。</summary>
	public float BaseAttackDamage => AttributeAttackDamage + baseAttackDamageBonus;

	/// <summary>装备提供、在 UI 中显示为绿色的攻击力。</summary>
	public float EquipmentAttackDamage => EquippedBonus(DotaDefOf.DotaAttackBonus);

	/// <summary>额外攻击力 = 装备攻击力 + 其他额外攻击力加成。</summary>
	public float BonusAttackDamage => EquipmentAttackDamage + bonusAttackDamageBonus;

	public float TotalAttackDamage => BaseAttackDamage + BonusAttackDamage;

	/// <summary>Dota 总攻击速度：100 为基础，限制在 20~500。</summary>
	public float DisplayAttackSpeed => Mathf.Clamp(
		BaseAttackSpeed
			+ Agility
			+ EquippedBonus(DotaDefOf.DotaAttackSpeedBonus)
			+ (Pawn?.GetStatValue(DotaDefOf.DotaExternalAttackSpeedBonus) ?? 0f),
		MinAttackSpeed,
		MaxAttackSpeed);

	/// <summary>RimWorld 攻击间隔倍率 = 100 / Dota 总攻速。</summary>
	public float AttackCooldownMultiplier => BaseAttackSpeed / DisplayAttackSpeed;

	/// <summary>永久或临时基础攻击力加成，由外部效果写入。</summary>
	public float BaseAttackDamageBonus
	{
		get => baseAttackDamageBonus;
		set => baseAttackDamageBonus = value;
	}

	/// <summary>永久或临时额外攻击力加成（不含装备），由外部效果写入。</summary>
	public float BonusAttackDamageBonus
	{
		get => bonusAttackDamageBonus;
		set => bonusAttackDamageBonus = value;
	}

	/// <summary>显示用生命回复（点/秒）= 力量部分 + 装备部分，不受平衡系数影响。</summary>
	public float HealthRegenPerSecond => HealthRegenFromStrength
		+ bonusHealthRegen
		+ EquippedBonus(DotaDefOf.DotaHealthRegenBonus);

	/// <summary>实际结算的生命回复（点/秒）= 显示值 × 生命回复平衡系数。</summary>
	public float ActualHealthRegenPerSecond => HealthRegenPerSecond
		* RimDotaMod.Settings.HealthRegenBalanceScale;

	/// <summary>最终 Dota 护甲点数 = 敏捷/6 + 装备护甲 + 其他加成。</summary>
	public float ArmorPoints => ArmorFromAgility
		+ bonusArmor
		+ EquippedBonus(DotaDefOf.DotaArmorBonus)
		+ (Pawn?.GetStatValue(DotaDefOf.DotaExternalArmorBonus) ?? 0f);

	/// <summary>Dota 物理伤害倍率（不扣原版护甲穿透）。</summary>
	public float PhysicalDamageMultiplier
	{
		get
		{
			float a = ArmorPoints;
			return 1f - 0.06f * a / (1f + 0.06f * Mathf.Abs(a));
		}
	}

	/// <summary>智力提供的魔抗：0.1%/点，汇总成一个来源。</summary>
	public float MagicResistanceFromIntelligence => Intelligence * 0.001f;

	/// <summary>装备/外部来源的魔抗（比例）。</summary>
	public float MagicResistanceFromEquipment => EquippedBonus(DotaDefOf.DotaMagicResistance) + bonusMagicResistance;

	/// <summary>最终魔抗：基础 25%、智力、装备按 Dota 2 正魔抗乘算。</summary>
	public float MagicResistance
	{
		get
		{
			float intRes = Mathf.Clamp01(MagicResistanceFromIntelligence);
			float equipRes = Mathf.Clamp(MagicResistanceFromEquipment, -1f, 1f);
			return 1f - (1f - 0.25f) * (1f - intRes) * (1f - equipRes);
		}
	}

	/// <summary>Dota 法术伤害倍率。</summary>
	public float MagicDamageMultiplier => 1f - MagicResistance;

	/// <summary>装备提供的攻击速度点数。</summary>
	public float EquipmentAttackSpeed => EquippedBonus(DotaDefOf.DotaAttackSpeedBonus);

	/// <summary>最终法力上限 = 基础 200 + 智力部分 + 装备部分。</summary>
	public float MaxMana => BaseMana + MaxManaFromIntelligence + bonusMaxMana + EquippedBonus(DotaDefOf.DotaMaxManaBonus);

	/// <summary>最终法力回复（点/秒）= 智力部分 + 装备部分，不受生命回复系数影响。</summary>
	public float ManaRegenPerSecond => ManaRegenFromIntelligence
		+ bonusManaRegen
		+ EquippedBonus(DotaDefOf.DotaManaRegenBonus);

	/// <summary>最终心灵敏感度加成 = 智力部分 + 装备部分。</summary>
	public float PsychicBonus => PsychicFromIntelligence + bonusPsychic;

	/// <summary>Dota2 用的全伤害减免：加算，可为负。</summary>
	public float UniversalDamageReduction => EquippedBonus(DotaDefOf.DotaUniversalDamageReduction) + bonusUniversalDamageReduction;

	/// <summary>最终攻击吸血（比例）。</summary>
	public float AttackLifesteal => EquippedBonus(DotaDefOf.DotaAttackLifesteal) + bonusAttackLifesteal;

	/// <summary>最终技能吸血（比例）。</summary>
	public float SpellLifesteal => EquippedBonus(DotaDefOf.DotaSpellLifesteal) + bonusSpellLifesteal;

	/// <summary>当前法力值，范围 0~MaxMana。</summary>
	public float Mana
	{
		get => mana;
		set
		{
			mana = value;
			if (mana < 0f)
			{
				mana = 0f;
			}
			else if (mana > MaxMana)
			{
				mana = MaxMana;
			}
		}
	}

	/// <summary>法力百分比（0~1），用于画条。</summary>
	public float ManaPercent => MaxMana > 0f ? mana / MaxMana : 0f;

	public bool TrySpendMana(float amount)
	{
		if (amount <= 0f)
		{
			return true;
		}
		if (mana < amount)
		{
			return false;
		}
		Mana -= amount;
		return true;
	}

	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);
		if (Pawn == null || !Pawn.Spawned)
		{
			return;
		}
		int ticks = Find.TickManager.TicksGame;
		if (ticks < nextRegenTick)
		{
			return;
		}
		nextRegenTick = ticks + RegenIntervalTicks;

		if (Mana < MaxMana)
		{
			Mana += ManaRegenPerSecond / 60f * RegenIntervalTicks;
		}

		AddHealing(ActualHealthRegenPerSecond / 60f * RegenIntervalTicks);
		SettlePendingHealing();
	}

	/// <summary>把回血量加入 Dota 回血池；吸血、装备直接回血等外部来源也走这里。</summary>
	public void AddHealing(float amount)
	{
		if (amount > 0f)
		{
			pendingHealing += amount;
		}
	}

	/// <summary>
	/// 结算 Dota 回血池：按伤口严重度从高到低消耗总预算，单伤口最多治到 0，
	/// 预算耗尽即停止；未用完的部分丢弃。
	/// </summary>
	public void SettlePendingHealing()
	{
		if (pendingHealing < MinSettleHealing)
		{
			return;
		}

		float budget = pendingHealing * healingMultiplier;
		pendingHealing = 0f;
		if (budget <= 0f || Pawn?.health?.hediffSet?.hediffs == null)
		{
			return;
		}

		tmpInjuries.Clear();
		foreach (Hediff h in Pawn.health.hediffSet.hediffs)
		{
			if (h is Hediff_Injury injury)
			{
				tmpInjuries.Add(injury);
			}
		}

		if (tmpInjuries.Count == 0)
		{
			return;
		}

		tmpInjuries.Sort(SeverityDescending);
		for (int i = 0; i < tmpInjuries.Count; i++)
		{
			Hediff_Injury injury = tmpInjuries[i];
			float heal = Mathf.Min(budget, injury.Severity);
			if (heal <= 0f)
			{
				continue;
			}
			injury.Heal(heal);
			budget -= heal;
			if (budget <= 0f)
			{
				break;
			}
		}

		tmpInjuries.Clear();
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Values.Look(ref mana, "mana", BaseMana);
		Scribe_Values.Look(ref heroLevel, "heroLevel", 1);
		Scribe_Values.Look(ref experience, "experience", 0f);
		Scribe_Values.Look(ref initialSetupComplete, "initialSetupComplete", true);
		Scribe_Values.Look(
			ref primaryAttribute,
			"primaryAttribute",
			Props.primaryAttribute);
		Scribe_Values.Look(ref strength, "strength", BaseAttribute);
		Scribe_Values.Look(ref agility, "agility", BaseAttribute);
		Scribe_Values.Look(ref intelligence, "intelligence", BaseAttribute);
		Scribe_Values.Look(ref strengthGrowth, "strengthGrowth", 0f);
		Scribe_Values.Look(ref agilityGrowth, "agilityGrowth", 0f);
		Scribe_Values.Look(ref intelligenceGrowth, "intelligenceGrowth", 0f);
		Scribe_Values.Look(ref bonusHealthScaleMult, "bonusHealthScaleMult", 0f);
		Scribe_Values.Look(ref baseAttackDamageBonus, "baseAttackDamageBonus", 0f);
		Scribe_Values.Look(ref bonusAttackDamageBonus, "bonusAttackDamageBonus", 0f);
		Scribe_Values.Look(ref bonusHealthRegen, "bonusHealthRegen", 0f);
		Scribe_Values.Look(ref bonusArmor, "bonusArmor", 0f);
		Scribe_Values.Look(ref bonusAimAndCooldown, "bonusAimAndCooldown", 0f);
		Scribe_Values.Look(ref bonusMaxMana, "bonusMaxMana", 0f);
		Scribe_Values.Look(ref bonusManaRegen, "bonusManaRegen", 0f);
		Scribe_Values.Look(ref bonusPsychic, "bonusPsychic", 0f);
		Scribe_Values.Look(ref bonusMagicResistance, "bonusMagicResistance", 0f);
		Scribe_Values.Look(ref bonusUniversalDamageReduction, "bonusUniversalDamageReduction", 0f);
		Scribe_Values.Look(ref bonusAttackLifesteal, "bonusAttackLifesteal", 0f);
		Scribe_Values.Look(ref bonusSpellLifesteal, "bonusSpellLifesteal", 0f);
		Scribe_Values.Look(ref pendingHealing, "pendingHealing", 0f);
		Scribe_Values.Look(ref healingMultiplier, "healingMultiplier", 1f);
	}

	public override IEnumerable<Gizmo> CompGetGizmos()
	{
		yield return new Gizmo_DotaHeroStatus(this);
	}
}

public class HediffCompProperties_DotaHero : HediffCompProperties
{
	/// <summary>英雄主属性，默认全才（力量/敏捷/智力三色齐亮）。可在 HediffDef 的 comps 里覆盖。</summary>
	public DotaPrimaryAttribute primaryAttribute = DotaPrimaryAttribute.Universal;

	public HediffCompProperties_DotaHero()
	{
		compClass = typeof(HediffComp_DotaHero);
	}
}
