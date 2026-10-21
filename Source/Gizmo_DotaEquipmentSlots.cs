using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// Dota 六格装备栏 UI，排在 Dota 属性栏旁边。
/// </summary>
public class Gizmo_DotaEquipmentSlots : Gizmo
{
	private const float SlotSize = 30f;
	private const float Gap = 5f;
	private const float Width = 230f;
	private const float GizmoHeight = 42f;

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
		Rect inner = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, SlotSize);
		Widgets.DrawWindowBackground(rect);

		for (int i = 0; i < HediffComp_DotaInventory.SlotCount; i++)
		{
			Rect slot = new Rect(inner.x + i * (SlotSize + Gap), inner.y, SlotSize, SlotSize);
			Widgets.DrawBoxSolid(slot, new Color(0.08f, 0.08f, 0.12f));
			Widgets.DrawBox(slot);

			Thing item = comp.GetAt(i);
			if (item == null)
			{
				TooltipHandler.TipRegion(slot, "空 Dota 装备槽");
				continue;
			}

			GUI.DrawTexture(slot, item.def.uiIcon, ScaleMode.ScaleToFit);
			TooltipHandler.TipRegion(slot, item.LabelCap + "\n" + item.DescriptionDetailed);

			if (Widgets.ButtonInvisible(slot))
			{
				Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
				{
					new FloatMenuOption("卸下 " + item.LabelCap, () => comp.TryUnequip(item)),
					new FloatMenuOption("查看信息", () => Find.WindowStack.Add(new Dialog_InfoCard(item)))
				}));
			}
		}

		return new GizmoResult(GizmoState.Clear);
	}
}
