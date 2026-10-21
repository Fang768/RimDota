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
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			LongEventHandler.ExecuteWhenFinished(() =>
			{
				if (Pawn != null)
				{
					TryAutoCombine(notify: false);
				}
			});
		}
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
		TryAutoCombine();
		return true;
	}

	public bool TrySetLocked(Thing item, bool locked)
	{
		if (item == null || inventory == null || !inventory.Contains(item))
		{
			return false;
		}

		Comp_DotaEquipmentLock lockComp = DotaEquipmentLockUtility.GetLockComp(item);
		if (lockComp == null)
		{
			Log.Warning("RimDota: Dota equipment is missing Comp_DotaEquipmentLock: " + item.LabelCap);
			return false;
		}

		if (lockComp.Locked == locked)
		{
			return true;
		}

		lockComp.Locked = locked;
		if (!locked)
		{
			TryAutoCombine();
		}
		return true;
	}

	/// <summary>
	/// 按当前槽位顺序自动合成。每次选择最早形成完整配方的组合；
	/// 同格完成时比较实际材料槽位序列，再以 defName 保证结果确定。
	/// </summary>
	public int TryAutoCombine(bool notify = true)
	{
		EnsureInventory();
		int combinedCount = 0;
		while (combinedCount < SlotCount - 1)
		{
			DotaCombineRecipeDef recipe = FindBestCombination(out List<int> usedSlots);
			if (recipe == null || !PerformCombination(recipe, usedSlots, notify))
			{
				break;
			}
			combinedCount++;
		}
		return combinedCount;
	}

	private DotaCombineRecipeDef FindBestCombination(out List<int> bestSlots)
	{
		bestSlots = null;
		if (inventory == null || inventory.Count < 2)
		{
			return null;
		}

		DotaCombineRecipeDef bestRecipe = null;
		foreach (DotaCombineRecipeDef recipe in DefDatabase<DotaCombineRecipeDef>.AllDefsListForReading)
		{
			if (recipe?.result == null
				|| !DotaEquipmentUtil.IsDotaEquipment(recipe.result)
				|| !TryMatchRecipe(recipe, out List<int> slots))
			{
				continue;
			}

			if (bestRecipe == null || IsPreferredCombination(recipe, slots, bestRecipe, bestSlots))
			{
				bestRecipe = recipe;
				bestSlots = slots;
			}
		}
		return bestRecipe;
	}

	private bool TryMatchRecipe(DotaCombineRecipeDef recipe, out List<int> usedSlots)
	{
		usedSlots = null;
		Dictionary<ThingDef, int> remaining = new Dictionary<ThingDef, int>();
		int requiredCount = 0;
		foreach (ThingDefCountClass ingredient in recipe.ingredients)
		{
			if (ingredient?.thingDef == null || ingredient.count <= 0)
			{
				return false;
			}

			if (!remaining.TryGetValue(ingredient.thingDef, out int count))
			{
				count = 0;
			}
			remaining[ingredient.thingDef] = count + ingredient.count;
			requiredCount += ingredient.count;
		}

		// 至少消耗两件物品，保证每次合成都会减少槽位数量并阻止无限循环。
		if (requiredCount < 2)
		{
			return false;
		}

		List<int> selected = new List<int>(requiredCount);
		int selectedCount = 0;
		for (int i = 0; i < inventory.Count && selectedCount < requiredCount; i++)
		{
			Thing item = inventory[i];
			if (item == null || DotaEquipmentLockUtility.IsLocked(item))
			{
				continue;
			}

			if (remaining.TryGetValue(item.def, out int count) && count > 0)
			{
				selected.Add(i);
				remaining[item.def] = count - 1;
				selectedCount++;
			}
		}

		if (selectedCount != requiredCount)
		{
			return false;
		}

		usedSlots = selected;
		return true;
	}

	private static bool IsPreferredCombination(
		DotaCombineRecipeDef candidateRecipe,
		List<int> candidateSlots,
		DotaCombineRecipeDef currentRecipe,
		List<int> currentSlots)
	{
		int candidateCompletion = candidateSlots[candidateSlots.Count - 1];
		int currentCompletion = currentSlots[currentSlots.Count - 1];
		if (candidateCompletion != currentCompletion)
		{
			return candidateCompletion < currentCompletion;
		}

		int slotComparison = CompareSlotLists(candidateSlots, currentSlots);
		if (slotComparison != 0)
		{
			return slotComparison < 0;
		}

		return string.CompareOrdinal(candidateRecipe.defName, currentRecipe.defName) < 0;
	}

	private static int CompareSlotLists(List<int> left, List<int> right)
	{
		int count = left.Count < right.Count ? left.Count : right.Count;
		for (int i = 0; i < count; i++)
		{
			if (left[i] != right[i])
			{
				return left[i] < right[i] ? -1 : 1;
			}
		}
		return left.Count.CompareTo(right.Count);
	}

	private bool PerformCombination(DotaCombineRecipeDef recipe, List<int> usedSlots, bool notify)
	{
		List<Thing> originals = new List<Thing>(inventory.Count);
		for (int i = 0; i < inventory.Count; i++)
		{
			originals.Add(inventory[i]);
		}

		HashSet<Thing> consumed = new HashSet<Thing>();
		foreach (int slot in usedSlots)
		{
			consumed.Add(originals[slot]);
		}

		Thing result = ThingMaker.MakeThing(recipe.result);
		int resultSlot = usedSlots[0];
		List<Thing> rebuilt = new List<Thing>(originals.Count - consumed.Count + 1);
		for (int i = 0; i < originals.Count; i++)
		{
			if (i == resultSlot)
			{
				rebuilt.Add(result);
			}
			if (!consumed.Contains(originals[i]))
			{
				rebuilt.Add(originals[i]);
			}
		}

		inventory.Clear();
		bool success = true;
		foreach (Thing item in rebuilt)
		{
			if (!inventory.TryAddOrTransfer(item))
			{
				success = false;
				break;
			}
		}

		if (!success)
		{
			inventory.Clear();
			foreach (Thing item in originals)
			{
				if (!inventory.TryAddOrTransfer(item))
				{
					Log.Error("RimDota: 合成失败后无法恢复 Dota 装备 " + item.LabelCap);
				}
			}
			result.Destroy(DestroyMode.Vanish);
			Messages.Message("Dota 装备合成失败，已恢复原装备。", MessageTypeDefOf.RejectInput);
			return false;
		}

		foreach (Thing item in consumed)
		{
			item.Destroy(DestroyMode.Vanish);
		}

		if (notify)
		{
			Messages.Message(
				"Dota 装备自动合成：" + result.LabelCap,
				MessageTypeDefOf.PositiveEvent);
		}
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
