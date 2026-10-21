using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimDota;

/// <summary>
/// 走到地面上的 Dota 装备旁边，然后把它放入六格装备栏。
/// 和原版穿戴一样会先寻路、短暂交互，不会隔空装备。
/// </summary>
public class JobDriver_DotaEquip : JobDriver
{
	private const TargetIndex ItemInd = TargetIndex.A;

	private Thing Item => job.GetTarget(ItemInd).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		Thing item = Item;
		return item != null && pawn.Reserve(item, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		Toil go = Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch);
		go.AddFailCondition(() =>
		{
			Thing item = Item;
			return item == null || item.Destroyed || !item.Spawned || item.IsForbidden(pawn);
		});
		yield return go;

		yield return Toils_General.Do(Equip);
	}

	private void Equip()
	{
		Thing item = Item;
		if (item == null)
		{
			EndJobWith(JobCondition.Incompletable);
			return;
		}

		HediffComp_DotaInventory inventory = DotaHeroUtil.GetInventoryComp(pawn);
		if (inventory == null || !inventory.TryEquip(item))
		{
			EndJobWith(JobCondition.Incompletable);
		}
	}
}
