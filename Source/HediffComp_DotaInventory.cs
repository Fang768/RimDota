using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// Dota 英雄专属六格装备栏。
/// 装备保存在独立的 ThingOwner 中，不进入 RimWorld 的 WornApparel，
/// 因此不受身体部位组/衣物层冲突限制。
/// </summary>
public class HediffComp_DotaInventory : HediffComp, IThingHolder
{
	public const int SlotCount = 6;

	private ThingOwner<Thing> inventory;

	public int Count => inventory?.Count ?? 0;

	public IThingHolder ParentHolder => null;

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return inventory;
	}

	public Thing GetAt(int index)
	{
		if (inventory == null || index < 0 || index >= inventory.Count)
		{
			return null;
		}
		return inventory[index];
	}

	public bool Contains(Thing item)
	{
		return inventory != null && item != null && inventory.Contains(item);
	}

	private void EnsureInventory()
	{
		if (inventory == null)
		{
			inventory = new ThingOwner_DotaInventory(this);
		}
	}

	public override void CompPostMake()
	{
		base.CompPostMake();
		EnsureInventory();
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		EnsureInventory();
		inventory.ExposeData();
	}

	public bool TryEquip(Thing item)
	{
		if (item == null || item.Destroyed)
		{
			return false;
		}

		if (!DotaEquipmentUtil.IsDotaEquipment(item.def))
		{
			Messages.Message("这不是 Dota 装备。", MessageTypeDefOf.RejectInput);
			return false;
		}

		if (Pawn == null || !DotaHeroUtil.IsHero(Pawn))
		{
			Messages.Message("只有 Dota 英雄才能装备这件物品。", MessageTypeDefOf.RejectInput);
			return false;
		}

		EnsureInventory();
		if (inventory.Count >= SlotCount)
		{
			Messages.Message("Dota 装备栏已满。", MessageTypeDefOf.RejectInput);
			return false;
		}

		if (inventory.Contains(item))
		{
			Messages.Message("已经装备了这件物品。", MessageTypeDefOf.RejectInput);
			return false;
		}

		bool wasSpawned = item.Spawned;
		if (wasSpawned)
		{
			item.DeSpawn();
		}

		if (!inventory.TryAddOrTransfer(item))
		{
			if (wasSpawned && Pawn.Spawned && Pawn.MapHeld != null)
			{
				GenPlace.TryPlaceThing(item, Pawn.PositionHeld, Pawn.MapHeld, ThingPlaceMode.Near);
			}
			Messages.Message("无法装备这件物品。", MessageTypeDefOf.RejectInput);
			return false;
		}

		Messages.Message("已装备 " + item.LabelCap + "。", MessageTypeDefOf.PositiveEvent);
		return true;
	}

	public bool TryUnequip(Thing item)
	{
		if (item == null || inventory == null || !inventory.Contains(item) || Pawn == null)
		{
			return false;
		}

		bool dropped = false;
		if (Pawn.Spawned && Pawn.MapHeld != null)
		{
			dropped = inventory.TryDrop(item, Pawn.PositionHeld, Pawn.MapHeld, ThingPlaceMode.Near, out Thing _);
		}
		else if (Pawn.inventory?.innerContainer != null)
		{
			dropped = Pawn.inventory.innerContainer.TryAddOrTransfer(item);
		}

		if (dropped)
		{
			Messages.Message("已卸下 " + item.LabelCap + "。", MessageTypeDefOf.PositiveEvent);
		}
		return dropped;
	}

	public void DropAll()
	{
		if (inventory == null || Pawn == null)
		{
			return;
		}

		if (Pawn.Spawned && Pawn.MapHeld != null)
		{
			inventory.TryDropAll(Pawn.PositionHeld, Pawn.MapHeld, ThingPlaceMode.Near);
			return;
		}

		if (Pawn.inventory?.innerContainer != null)
		{
			for (int i = inventory.Count - 1; i >= 0; i--)
			{
				Thing item = inventory[i];
				inventory.Remove(item);
				Pawn.inventory.innerContainer.TryAddOrTransfer(item);
			}
		}
	}

	public void DestroyAll()
	{
		inventory?.ClearAndDestroyContents();
	}

	public override void Notify_PawnKilled()
	{
		base.Notify_PawnKilled();
		// 用户要求：死亡不掉落 Dota 装备，随英雄一起销毁。
		DestroyAll();
	}

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		if (Pawn != null && !Pawn.Dead)
		{
			DropAll();
		}
		else
		{
			DestroyAll();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmos()
	{
		yield return new Gizmo_DotaEquipmentSlots(this);
	}
}

/// <summary>固定 6 格容量的 ThingOwner。</summary>
public class ThingOwner_DotaInventory : ThingOwner<Thing>
{
	public ThingOwner_DotaInventory(IThingHolder owner) : base(owner)
	{
		maxStacks = HediffComp_DotaInventory.SlotCount;
	}
}

public class HediffCompProperties_DotaInventory : HediffCompProperties
{
	public HediffCompProperties_DotaInventory()
	{
		compClass = typeof(HediffComp_DotaInventory);
	}
}
