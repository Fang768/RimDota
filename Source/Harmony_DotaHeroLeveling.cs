using HarmonyLib;
using RimWorld;
using Verse;

namespace RimDota;

/// <summary>英雄击杀敌对 Pawn 或野生动物时，按目标市场价值奖励 Dota 经验。</summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class Harmony_DotaHeroLeveling
{
	public static void Postfix(Pawn __instance, DamageInfo? dinfo)
	{
		if (!dinfo.HasValue || __instance == null || !IsExperienceTarget(__instance))
		{
			return;
		}

		Pawn killer = dinfo.Value.Instigator as Pawn;
		if (killer == null || killer == __instance)
		{
			return;
		}

		DotaHeroUtil.GetHeroComp(killer)?.AddExperience(__instance.MarketValue);
	}

	private static bool IsExperienceTarget(Pawn victim)
	{
		if (victim.HostileTo(Faction.OfPlayer))
		{
			return true;
		}

		return victim.RaceProps.Animal
			&& (victim.Faction == null || victim.Faction.HostileTo(Faction.OfPlayer));
	}
}
