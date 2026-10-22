using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimDota;

/// <summary>拾取地图上的遗迹碎片，带到邪月祭坛并献祭五秒。</summary>
public class JobDriver_DotaSacrificeRelic : JobDriver
{
	private const TargetIndex AltarInd = TargetIndex.A;
	private const TargetIndex RelicInd = TargetIndex.B;
	private const int SacrificeDurationTicks = 300;

	private Thing Altar => job.GetTarget(AltarInd).Thing;

	private Thing Relic => job.GetTarget(RelicInd).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(Relic, job, 1, -1, null, errorOnFailed)
			&& pawn.Reserve(Altar, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		AddFinishAction(condition =>
		{
			if (condition == JobCondition.Succeeded || !pawn.Spawned)
			{
				return;
			}

			Thing carried = pawn.carryTracker.CarriedThing;
			if (carried != null && carried.def == DotaDefOf.DotaRelicFragment)
			{
				pawn.carryTracker.TryDropCarriedThing(
					pawn.PositionHeld,
					ThingPlaceMode.Near,
					out Thing _);
			}
		});

		Toil goToRelic = Toils_Goto.GotoThing(
			RelicInd,
			PathEndMode.ClosestTouch);
		goToRelic.AddFailCondition(() =>
		{
			Thing relic = Relic;
			return relic == null
				|| relic.Destroyed
				|| !relic.Spawned
				|| relic.def != DotaDefOf.DotaRelicFragment
				|| relic.IsForbidden(pawn);
		});
		yield return goToRelic;

		yield return Toils_Haul.StartCarryThing(RelicInd);

		Toil goToAltar = Toils_Goto.GotoThing(
			AltarInd,
			PathEndMode.InteractionCell);
		goToAltar.AddFailCondition(() =>
		{
			Thing altar = Altar;
			return altar == null
				|| altar.Destroyed
				|| !altar.Spawned
				|| altar.def != DotaDefOf.DotaCrescentAltar;
		});
		yield return goToAltar;

		Toil wait = Toils_General
			.Wait(SacrificeDurationTicks, AltarInd)
			.WithProgressBarToilDelay(AltarInd);
		wait.AddFailCondition(() =>
		{
			Thing carried = pawn.carryTracker.CarriedThing;
			return carried == null
				|| carried.Destroyed
				|| carried.def != DotaDefOf.DotaRelicFragment;
		});
		yield return wait;

		yield return Toils_General.Do(CompleteSacrifice);
	}

	private void CompleteSacrifice()
	{
		Thing carried = pawn.carryTracker.CarriedThing;
		if (carried == null || carried.def != DotaDefOf.DotaRelicFragment)
		{
			EndJobWith(JobCondition.Incompletable);
			return;
		}

		if (DotaHeroUtil.IsHero(pawn)
			|| !DotaHeroUtil.TryMakeHero(pawn, awaitInitialSetup: true))
		{
			pawn.carryTracker.TryDropCarriedThing(
				pawn.PositionHeld,
				ThingPlaceMode.Near,
				out Thing _);
			EndJobWith(JobCondition.Incompletable);
			return;
		}

		pawn.carryTracker.DestroyCarriedThing();
		Messages.Message(
			pawn.LabelShortCap + " 已完成遗迹碎片献祭，获得了 Dota 英雄身份。请点击警报分配初始属性。",
			pawn,
			MessageTypeDefOf.PositiveEvent);
	}
}
