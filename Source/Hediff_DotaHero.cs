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

	private const int RegenIntervalTicks = 15;
	private const float MinSettleHealing = 0.1f;

	public HediffCompProperties_DotaHero Props => (HediffCompProperties_DotaHero)props;

	/// <summary>英雄主属性，决定 Dota 属性栏中三维数据的着色。</summary>
	public DotaPrimaryAttribute PrimaryAttribute => Props.primaryAttribute;

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

	/// <summary>生命上限倍率（力量部分）：力量 × 2%/点。</summary>
	public float HealthScaleMultFromStrength => Strength * 0.02f;

	/// <summary>生命回复基础值（点/天）：力量 × 1。</summary>
	public float HealthRegenFromStrength => Strength * 1f;

	/// <summary>护甲基础值（Dota 原始护甲点数）：敏捷 × 1/6。</summary>
	public float ArmorFromAgility => Agility / 6f;

	/// <summary>瞄准/攻速基础加成：敏捷 × -0.5%/点。</summary>
	public float AimAndCooldownFromAgility => Agility * -0.005f;

	/// <summary>法力上限基础值：智力 × 5。</summary>
	public float MaxManaFromIntelligence => Intelligence * 5f;

	/// <summary>法力回复基础值（点/天）：智力 × 5。</summary>
	public float ManaRegenFromIntelligence => Intelligence * 5f;

	/// <summary>心灵敏感度基础加成：智力 × 2%/点。</summary>
	public float PsychicFromIntelligence => Intelligence * 0.02f;

	/// <summary>装备等外部来源的生命上限百分比加成。</summary>
	public float BonusHealthScaleMult
	{
		get => bonusHealthScaleMult;
		set => bonusHealthScaleMult = value;
	}

	/// <summary>装备等外部来源的生命回复加成（点/天）。</summary>
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

	/// <summary>装备等外部来源的瞄准/攻速加成。</summary>
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

	/// <summary>装备等外部来源的法力回复加成（点/天）。</summary>
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

	/// <summary>最终生命回复 = 力量部分 + 装备部分。</summary>
	public float HealthRegenPerDay => HealthRegenFromStrength + bonusHealthRegen + EquippedBonus(DotaDefOf.DotaHealthRegenBonus);

	/// <summary>最终 Dota 护甲点数 = 敏捷/6 + 装备护甲 + 其他加成。</summary>
	public float ArmorPoints => ArmorFromAgility + bonusArmor + EquippedBonus(DotaDefOf.DotaArmorBonus);

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

	/// <summary>最终瞄准/攻速加成 = 敏捷部分 + 装备攻速 + 外部加成；1 点攻速 = -0.5% 冷却。</summary>
	public float AimAndCooldownFactor => AimAndCooldownFromAgility + bonusAimAndCooldown + EquipmentAttackSpeed * -0.005f;

	/// <summary>最终法力上限 = 基础 200 + 智力部分 + 装备部分。</summary>
	public float MaxMana => BaseMana + MaxManaFromIntelligence + bonusMaxMana + EquippedBonus(DotaDefOf.DotaMaxManaBonus);

	/// <summary>最终法力回复 = 智力部分 + 装备部分。</summary>
	public float ManaRegenPerDay => ManaRegenFromIntelligence + bonusManaRegen + EquippedBonus(DotaDefOf.DotaManaRegenBonus);

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
			Mana += ManaRegenPerDay / 60000f * RegenIntervalTicks;
		}

		AddHealing(HealthRegenPerDay / 60000f * RegenIntervalTicks);
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
		Scribe_Values.Look(ref strength, "strength", BaseAttribute);
		Scribe_Values.Look(ref agility, "agility", BaseAttribute);
		Scribe_Values.Look(ref intelligence, "intelligence", BaseAttribute);
		Scribe_Values.Look(ref strengthGrowth, "strengthGrowth", 0f);
		Scribe_Values.Look(ref agilityGrowth, "agilityGrowth", 0f);
		Scribe_Values.Look(ref intelligenceGrowth, "intelligenceGrowth", 0f);
		Scribe_Values.Look(ref bonusHealthScaleMult, "bonusHealthScaleMult", 0f);
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
		yield return new Gizmo_DotaAttributes(this);
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
