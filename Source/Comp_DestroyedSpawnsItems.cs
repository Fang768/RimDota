using System.Collections.Generic;
using Verse;

namespace RimDota;

/// <summary>
/// 建筑被摧毁（Killed）时，在其位置生成指定物品（月亮碎片 → 遗迹碎片 + 翡翠）。
/// </summary>
public class Comp_DestroyedSpawnsItems : ThingComp
{
	public CompProperties_DestroyedSpawnsItems Props => (CompProperties_DestroyedSpawnsItems)props;

	public override IEnumerable<ThingDefCountClass> GetAdditionalLeavings(Map map, DestroyMode mode)
	{
		if (mode != DestroyMode.KillFinalize && mode != DestroyMode.KillFinalizeLeavingsOnly)
		{
			yield break;
		}
		foreach (ThingDefCountClass drop in Props.spawnOnDestroy)
		{
			yield return drop;
		}
	}
}

public class CompProperties_DestroyedSpawnsItems : CompProperties
{
	public List<ThingDefCountClass> spawnOnDestroy = new List<ThingDefCountClass>();

	public CompProperties_DestroyedSpawnsItems()
	{
		compClass = typeof(Comp_DestroyedSpawnsItems);
	}
}
