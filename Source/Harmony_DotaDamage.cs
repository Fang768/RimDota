using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// 在 TakeDamage 进入时保存原始 DamageDef 和来源标签。
/// 原版护甲可能把 Sharp 改写成 Blunt，线程静态栈保证我们的元素/来源分类不丢失。
/// </summary>
[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
internal static class Patch_Thing_TakeDamage
{
	private static bool Prefix(DamageInfo dinfo, out bool __state)
	{
		DotaDamageSource pending = DotaDamageUtil.ConsumePendingSource();
		DotaDamageSource source = DotaDamageUtil.DetermineSource(dinfo, pending);
		DotaDamageUtil.PushContext(dinfo.Def, source);
		__state = true;
		return true;
	}

	private static Exception Finalizer(Exception __exception, bool __state)
	{
		if (__state)
		{
			DotaDamageUtil.PopContext();
		}
		return __exception;
	}
}

/// <summary>
/// Dota 物理/法术减伤：在原版护甲结算之后乘上 Dota 倍率。
/// 原版护甲结果只有 0/减半/全额，先乘和后乘在最终伤害上等价。
/// </summary>
[HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage))]
internal static class Patch_ArmorUtility_GetPostArmorDamage
{
	private static void Postfix(Pawn pawn, ref DamageDef damageDef, ref float __result)
	{
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return;
		}

		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (comp == null)
		{
			return;
		}

		DotaDamageElement element = DotaDamageUtil.TryPeekContext(out DotaDamageContext context)
			? context.Element
			: DotaDamageUtil.DetermineElement(damageDef);

		switch (element)
		{
			case DotaDamageElement.Physical:
				__result *= comp.PhysicalDamageMultiplier;
				break;
			case DotaDamageElement.Magic:
				__result *= comp.MagicDamageMultiplier;
				break;
		}
	}
}

/// <summary>
/// 攻击/技能吸血：用减伤后的实际伤害 totalDamageDealt 结算；
/// Splash 和未标记来源不吸血。
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
internal static class Patch_Pawn_PostApplyDamage
{
	private static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
	{
		if (totalDamageDealt <= 0f)
		{
			return;
		}

		if (dinfo.Instigator is not Pawn attacker || attacker == __instance)
		{
			return;
		}

		if (attacker.Dead || attacker.Destroyed || !DotaHeroUtil.IsHero(attacker))
		{
			return;
		}

		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(attacker);
		if (comp == null)
		{
			return;
		}

		DotaDamageSource source = DotaDamageUtil.TryPeekContext(out DotaDamageContext context)
			? context.Source
			: DotaDamageUtil.DetermineSource(dinfo, DotaDamageSource.None);

		float lifesteal = source switch
		{
			DotaDamageSource.Attack => comp.AttackLifesteal,
			DotaDamageSource.Skill => comp.SpellLifesteal,
			_ => 0f
		};

		if (lifesteal <= 0f)
		{
			return;
		}

		comp.AddHealing(totalDamageDealt * lifesteal);
		comp.SettlePendingHealing();
	}
}
