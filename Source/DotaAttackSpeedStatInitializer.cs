using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimDota;

/// <summary>确保三个 RimWorld 攻击时间 Stat 都挂载 Dota 攻速 StatPart。</summary>
[StaticConstructorOnStartup]
public static class DotaAttackSpeedStatInitializer
{
	static DotaAttackSpeedStatInitializer()
	{
		Inject(StatDefOf.AimingDelayFactor);
		Inject(StatDefOf.RangedCooldownFactor);
		Inject(StatDefOf.MeleeCooldownFactor);
	}

	private static void Inject(StatDef stat)
	{
		if (stat == null)
		{
			return;
		}
		stat.parts ??= new List<StatPart>();
		if (!stat.parts.OfType<StatPart_DotaHeroAttackSpeed>().Any())
		{
			stat.parts.Add(new StatPart_DotaHeroAttackSpeed());
		}
	}
}
