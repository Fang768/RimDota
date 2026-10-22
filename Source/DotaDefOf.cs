using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// DefOf：把 XML 里定义的 def 通过反射绑定到强类型字段，
/// 方便在 C# 代码里引用。字段名必须与 defName 完全一致。
/// </summary>
[DefOf]
public static class DotaDefOf
{
	public static HediffDef DotaHero;

	public static StatDef DotaStrengthBonus;

	public static StatDef DotaAgilityBonus;

	public static StatDef DotaIntelligenceBonus;

	public static StatDef DotaArmorBonus;

	public static StatDef DotaHealthRegenBonus;

	public static StatDef DotaMaxManaBonus;

	public static StatDef DotaMaxHealthBonus;

	public static StatDef DotaManaRegenBonus;

	public static StatDef DotaAttackSpeedBonus;

	public static StatDef DotaMagicResistance;

	public static StatDef DotaUniversalDamageReduction;

	public static StatDef DotaAttackLifesteal;

	public static StatDef DotaSpellLifesteal;

	public static JobDef Dota_EquipItem;

	public static JobDef Dota_SacrificeRelic;

	public static ThingDef Dota_BladesOfAttack;

	public static ThingDef DotaMoonShard;

	public static ThingDef DotaCrescentAltar;

	public static ThingDef DotaRelicFragment;

	public static ResearchProjectDef DotaTech1;

	public static HediffDef Dota_ExampleBuff;
}
