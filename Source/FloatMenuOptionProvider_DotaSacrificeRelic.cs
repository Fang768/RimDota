using RimWorld;
using Verse;
using Verse.AI;

namespace RimDota;

/// <summary>选中殖民者后，右键邪月祭坛发起遗迹碎片献祭。</summary>
public class FloatMenuOptionProvider_DotaSacrificeRelic : FloatMenuOptionProvider
{
	protected override bool Drafted => false;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool RequiresManipulation => true;

	protected override FloatMenuOption GetSingleOptionFor(
		Thing clickedThing,
		FloatMenuContext context)
	{
		if (clickedThing?.def != DotaDefOf.DotaCrescentAltar)
		{
			return null;
		}

		Pawn pawn = context.FirstSelectedPawn;
		if (pawn == null
			|| !pawn.IsColonistPlayerControlled
			|| !clickedThing.Spawned
			|| clickedThing.Map != pawn.Map)
		{
			return null;
		}

		if (DotaHeroUtil.IsHero(pawn))
		{
			return new FloatMenuOption(
				"该殖民者已经是 Dota 英雄",
				null,
				MenuOptionPriority.DisabledOption);
		}

		if (!pawn.CanReserveAndReach(
			clickedThing,
			PathEndMode.InteractionCell,
			Danger.Deadly))
		{
			return new FloatMenuOption(
				"无法到达邪月祭坛",
				null,
				MenuOptionPriority.DisabledOption);
		}

		Thing relic = FindAvailableRelic(pawn, clickedThing.Map);
		if (relic == null)
		{
			return new FloatMenuOption(
				"地图上没有可用的遗迹碎片",
				null,
				MenuOptionPriority.DisabledOption);
		}

		return new FloatMenuOption("献祭遗迹碎片", () =>
		{
			Job job = JobMaker.MakeJob(
				DotaDefOf.Dota_SacrificeRelic,
				clickedThing,
				relic);
			job.count = 1;
			pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		});
	}

	private static Thing FindAvailableRelic(Pawn pawn, Map map)
	{
		Thing best = null;
		float bestDistance = float.MaxValue;
		foreach (Thing thing in map.listerThings.ThingsOfDef(DotaDefOf.DotaRelicFragment))
		{
			if (thing.Destroyed
				|| !thing.Spawned
				|| thing.IsForbidden(pawn)
				|| !pawn.CanReserveAndReach(
					thing,
					PathEndMode.ClosestTouch,
					Danger.Deadly))
			{
				continue;
			}

			float distance = thing.Position.DistanceToSquared(pawn.Position);
			if (distance < bestDistance)
			{
				best = thing;
				bestDistance = distance;
			}
		}
		return best;
	}
}
