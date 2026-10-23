using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// 框架对外 API：内容 Mod（RimDota_****）通过这里判断英雄、读取法力值。
/// </summary>
public static class DotaHeroUtil
{
	/// <summary>
	/// 获取 StatRequest 关联的小人。
	/// 小人自身的 Stat 请求把 Pawn 放在 Thing 上，装备请求则放在 Pawn 上。
	/// </summary>
	public static Pawn RequestPawn(StatRequest request)
	{
		return request.Thing as Pawn ?? request.Pawn;
	}

	public static bool IsHero(Pawn pawn)
	{
		return pawn?.health?.hediffSet?.HasHediff(DotaDefOf.DotaHero) ?? false;
	}

	public static HediffComp_DotaHero GetHeroComp(Pawn pawn)
	{
		Hediff hediff = pawn?.health?.hediffSet?.GetFirstHediffOfDef(DotaDefOf.DotaHero);
		return (hediff as HediffWithComps)?.TryGetComp<HediffComp_DotaHero>();
	}

	public static bool TryMakeHero(Pawn pawn, bool awaitInitialSetup)
	{
		if (pawn?.health?.hediffSet == null || IsHero(pawn))
		{
			return false;
		}

		pawn.health.AddHediff(DotaDefOf.DotaHero);
		HediffComp_DotaHero heroComp = GetHeroComp(pawn);
		if (heroComp == null)
		{
			return false;
		}

		if (awaitInitialSetup)
		{
			heroComp.BeginInitialSetup();
		}
		return true;
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
