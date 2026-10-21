using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// Dota 六格装备栏 UI，排在 Dota 属性栏旁边。
/// </summary>
[StaticConstructorOnStartup]
public class Gizmo_DotaEquipmentSlots : Gizmo
{
	private static readonly Texture2D LockedOverlay = ContentFinder<Texture2D>.Get(
		"UI/Icons/Dota/DotaEquipmentLockedOverlay",
		reportFailure: false);

	private const float SlotSize = 30f;
	private const float Gap = 5f;
	private const float Width = 120f;
	private const float GizmoHeight = 77f;
	private const int Columns = 3;
	private const int Rows = 2;

	private readonly HediffComp_DotaInventory comp;

	public Gizmo_DotaEquipmentSlots(HediffComp_DotaInventory comp)
	{
		this.comp = comp;
		Order = -99f;
	}

	public override float GetWidth(float maxWidth)
	{
		return Width;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		float width = GetWidth(maxWidth);
		Rect rect = new Rect(topLeft.x, topLeft.y, width, GizmoHeight);
		float gridWidth = SlotSize * Columns + Gap * (Columns - 1);
		float gridHeight = SlotSize * Rows + Gap * (Rows - 1);
		Rect inner = new Rect(rect.center.x - gridWidth / 2f, rect.y + 6f, gridWidth, gridHeight);
		Widgets.DrawWindowBackground(rect);

		for (int i = 0; i < HediffComp_DotaInventory.SlotCount; i++)
		{
			int col = i % Columns;
			int row = i / Columns;
			Rect slot = new Rect(
				inner.x + col * (SlotSize + Gap),
				inner.y + row * (SlotSize + Gap),
				SlotSize,
				SlotSize);
			Widgets.DrawBoxSolid(slot, new Color(0.08f, 0.08f, 0.12f));
			Widgets.DrawBox(slot);

			Thing item = comp.GetAt(i);
			if (item == null)
			{
				TooltipHandler.TipRegion(slot, "空 Dota 装备槽");
				continue;
			}

			GUI.DrawTexture(slot, item.def.uiIcon, ScaleMode.ScaleToFit);
			bool locked = DotaEquipmentLockUtility.IsLocked(item);
			if (locked && LockedOverlay != null)
			{
				GUI.DrawTexture(slot, LockedOverlay, ScaleMode.ScaleToFit);
			}

			string tooltip = item.LabelCap + "\n" + item.DescriptionDetailed;
			if (locked)
			{
				tooltip += "\n\n已锁定：不参与自动合成。";
			}
			TooltipHandler.TipRegion(slot, tooltip);

			if (Widgets.ButtonInvisible(slot))
			{
				List<FloatMenuOption> options = new List<FloatMenuOption>
				{
					locked
						? new FloatMenuOption("解锁（允许自动合成）", () => comp.TrySetLocked(item, false))
						: new FloatMenuOption("锁定（不参与自动合成）", () => comp.TrySetLocked(item, true)),
					new FloatMenuOption("丢下 " + item.LabelCap, () => comp.TryUnequip(item)),
					new FloatMenuOption("查看信息", () => Find.WindowStack.Add(new Dialog_InfoCard(item)))
				};
				Find.WindowStack.Add(new FloatMenu(options));
			}
		}

		return new GizmoResult(GizmoState.Clear);
	}
}
