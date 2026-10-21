using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// 右键地面上的 Dota 装备时，给选中的 Dota 英雄添加“装备到 Dota 装备栏”。
/// 1.7 的 FloatMenuMakerMap 会自动扫描并注册所有 FloatMenuOptionProvider 子类。
/// </summary>
public class FloatMenuOptionProvider_DotaEquip : FloatMenuOptionProvider
{
	protected override bool Drafted => true;

	protected override bool Undrafted => true;

	protected override bool Multiselect => false;

	protected override bool AppliesInt(FloatMenuContext context)
	{
		Pawn pawn = context.FirstSelectedPawn;
		return pawn != null && DotaHeroUtil.IsHero(pawn);
	}

	protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
	{
		if (clickedThing == null || !clickedThing.Spawned || clickedThing.Map != context.map)
		{
			return null;
		}

		if (!DotaEquipmentUtil.IsDotaEquipment(clickedThing.def))
		{
			return null;
		}

		Pawn pawn = context.FirstSelectedPawn;
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return null;
		}

		HediffComp_DotaInventory inventory = DotaHeroUtil.GetInventoryComp(pawn);
		if (inventory == null)
		{
			return null;
		}

		if (inventory.Count >= HediffComp_DotaInventory.SlotCount)
		{
			return new FloatMenuOption("Dota 装备栏已满", null, MenuOptionPriority.DisabledOption);
		}

		return new FloatMenuOption("装备到 Dota 装备栏", () => inventory.TryEquip(clickedThing));
	}
}
