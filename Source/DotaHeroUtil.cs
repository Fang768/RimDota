using Verse;

namespace RimDota;

/// <summary>
/// 框架对外 API：内容 Mod（RimDota_****）通过这里判断英雄、读取法力值。
/// </summary>
public static class DotaHeroUtil
{
	public static bool IsHero(Pawn pawn)
	{
		return pawn?.health?.hediffSet?.HasHediff(DotaDefOf.DotaHero) ?? false;
	}

	public static HediffComp_DotaHero GetHeroComp(Pawn pawn)
	{
		Hediff hediff = pawn?.health?.hediffSet?.GetFirstHediffOfDef(DotaDefOf.DotaHero);
		return (hediff as HediffWithComps)?.TryGetComp<HediffComp_DotaHero>();
	}

	public static HediffComp_DotaInventory GetInventoryComp(Pawn pawn)
	{
		Hediff hediff = pawn?.health?.hediffSet?.GetFirstHediffOfDef(DotaDefOf.DotaHero);
		return (hediff as HediffWithComps)?.TryGetComp<HediffComp_DotaInventory>();
	}

	public static float GetMana(Pawn pawn)
	{
		return GetHeroComp(pawn)?.Mana ?? 0f;
	}

	public static float GetAttribute(Pawn pawn, DotaAttribute attribute)
	{
		HediffComp_DotaHero comp = GetHeroComp(pawn);
		if (comp == null)
		{
			return 0f;
		}
		return attribute switch
		{
			DotaAttribute.Strength => comp.Strength,
			DotaAttribute.Agility => comp.Agility,
			DotaAttribute.Intelligence => comp.Intelligence,
			_ => 0f,
		};
	}
}
