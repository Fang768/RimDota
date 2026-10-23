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
	private static readonly Dictionary<int, Texture2D> CooldownOverlayTextures =
		new Dictionary<int, Texture2D>();

	private static HediffComp_DotaInventory dragInventory;
	private static Thing draggedItem;
	private static int dragSourceSlot = -1;

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

	public static void DrawSlots(
		Rect rect,
		HediffComp_DotaInventory comp,
		float slotSize,
		float gap,
		bool drawBackground)
	{
		float gridWidth = slotSize * Columns + gap * (Columns - 1);
		float gridHeight = slotSize * Rows + gap * (Rows - 1);
		Rect inner = new Rect(
			rect.center.x - gridWidth / 2f,
			rect.center.y - gridHeight / 2f,
			gridWidth,
			gridHeight);
		if (drawBackground)
		{
			Widgets.DrawWindowBackground(rect);
		}

		List<Rect> slotRects = new List<Rect>(HediffComp_DotaInventory.SlotCount);
		for (int i = 0; i < HediffComp_DotaInventory.SlotCount; i++)
		{
			int col = i % Columns;
			int row = i / Columns;
			slotRects.Add(new Rect(
				inner.x + col * (slotSize + gap),
				inner.y + row * (slotSize + gap),
				slotSize,
				slotSize));
		}

		if (dragInventory != null
			&& Event.current.type == EventType.MouseUp
			&& Event.current.button == 0)
		{
			int targetSlot = FindSlotUnderMouse(slotRects, Event.current.mousePosition);
			CompleteDrag(targetSlot);
			Event.current.Use();
		}

		for (int i = 0; i < HediffComp_DotaInventory.SlotCount; i++)
		{
			Rect slot = slotRects[i];
			Widgets.DrawBoxSolid(slot, new Color(0.08f, 0.08f, 0.12f));
			Widgets.DrawBox(slot);

			Thing item = comp.GetAt(i);
			if (item == null)
			{
				TooltipHandler.TipRegion(slot, "空 Dota 装备槽");
				continue;
			}

			GUI.DrawTexture(slot, item.def.uiIcon, ScaleMode.ScaleToFit);
			DrawCooldownOverlay(slot, item);
			bool locked = DotaEquipmentLockUtility.IsLocked(item);
			if (locked && LockedOverlay != null)
			{
				GUI.DrawTexture(slot, LockedOverlay, ScaleMode.ScaleToFit);
			}

			string tooltip = ItemTooltip(item);
			if (locked)
			{
				tooltip += "\n\n已锁定：不参与自动合成。";
			}
			TooltipHandler.TipRegion(slot, tooltip);

			if (Event.current.type == EventType.MouseDown
				&& Event.current.button == 1
				&& slot.Contains(Event.current.mousePosition))
			{
				OpenItemMenu(comp, item, locked);
				Event.current.Use();
				continue;
			}

			Widgets.DraggableResult result = Widgets.ButtonInvisibleDraggable(slot);
			if (result == Widgets.DraggableResult.Pressed)
			{
				comp.TryUseEquipmentSlot(i);
				Event.current.Use();
			}
			else if (result == Widgets.DraggableResult.Dragged)
			{
				BeginDrag(comp, i, item);
				Event.current.Use();
			}
		}

		if (dragInventory != null
			&& draggedItem != null
			&& !draggedItem.Destroyed)
		{
			Rect draggedIcon = new Rect(
				Event.current.mousePosition.x - 18f,
				Event.current.mousePosition.y - 18f,
				36f,
				36f);
			GUI.color = new Color(1f, 1f, 1f, 0.75f);
			GUI.DrawTexture(draggedIcon, draggedItem.def.uiIcon, ScaleMode.ScaleToFit);
			GUI.color = Color.white;
		}
	}

	private static void BeginDrag(
		HediffComp_DotaInventory inventory,
		int sourceSlot,
		Thing item)
	{
		dragInventory = inventory;
		dragSourceSlot = sourceSlot;
		draggedItem = item;
	}

	private static void CompleteDrag(int targetSlot)
	{
		if (dragInventory != null && draggedItem != null)
		{
			if (targetSlot >= 0)
			{
				if (targetSlot != dragSourceSlot)
				{
					dragInventory.TryMoveOrSwap(dragSourceSlot, targetSlot);
				}
			}
			else
			{
				dragInventory.TryUnequip(draggedItem);
			}
		}

		dragInventory = null;
		draggedItem = null;
		dragSourceSlot = -1;
	}

	private static int FindSlotUnderMouse(List<Rect> slotRects, Vector2 mousePosition)
	{
		for (int i = 0; i < slotRects.Count; i++)
		{
			if (slotRects[i].Contains(mousePosition))
			{
				return i;
			}
		}
		return -1;
	}

	private static void OpenItemMenu(
		HediffComp_DotaInventory comp,
		Thing item,
		bool locked)
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

	private static string ItemTooltip(Thing item)
	{
		string tooltip = item.LabelCap + "\n" + item.DescriptionDetailed;
		DotaActiveItemExtension active = DotaActiveItemUtility.ActiveExtension(item.def);
		if (active == null)
		{
			return tooltip;
		}

		tooltip += "\n\n主动：消耗 " + active.manaCost.ToString("0") + " 法力";
		Comp_DotaEquipmentCooldown cooldown = DotaActiveItemUtility.CooldownComp(item);
		if (cooldown != null && cooldown.RemainingTicks > 0)
		{
			tooltip += "\n冷却：" + (cooldown.RemainingTicks / 60f).ToString("0.0") + " 秒";
		}
		return tooltip;
	}

	private static void DrawCooldownOverlay(Rect slot, Thing item)
	{
		DotaActiveItemExtension active = DotaActiveItemUtility.ActiveExtension(item.def);
		Comp_DotaEquipmentCooldown cooldown = DotaActiveItemUtility.CooldownComp(item);
		if (active == null || cooldown == null || cooldown.RemainingTicks <= 0)
		{
			return;
		}

		int percent = Mathf.Clamp(
			Mathf.CeilToInt(
				cooldown.RemainingTicks
				/ (float)Mathf.Max(1, active.cooldownTicks)
				* 100f),
			1,
			100);
		GUI.DrawTexture(
			slot,
			GetCooldownOverlayTexture(percent),
			ScaleMode.ScaleToFit);

		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		float seconds = cooldown.RemainingTicks / 60f;
		Widgets.Label(
			slot,
			seconds >= 1f ? seconds.ToString("0") : seconds.ToString("0.0"));
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private static Texture2D GetCooldownOverlayTexture(int percent)
	{
		percent = Mathf.Clamp(percent, 1, 100);
		if (CooldownOverlayTextures.TryGetValue(percent, out Texture2D texture))
		{
			return texture;
		}

		const int textureSize = 64;
		const float center = (textureSize - 1f) * 0.5f;
		const float radius = 31f;
		Color32 shade = new Color32(0, 0, 0, 155);
		Color32[] pixels = new Color32[textureSize * textureSize];
		float shadeAngle = percent / 100f * Mathf.PI * 2f;
		for (int y = 0; y < textureSize; y++)
		{
			for (int x = 0; x < textureSize; x++)
			{
				float dx = x - center;
				float dy = y - center;
				if (dx * dx + dy * dy > radius * radius)
				{
					continue;
				}

				float angle = Mathf.Atan2(dx, -dy);
				if (angle < 0f)
				{
					angle += Mathf.PI * 2f;
				}
				if (angle <= shadeAngle)
				{
					pixels[y * textureSize + x] = shade;
				}
			}
		}

		texture = new Texture2D(
			textureSize,
			textureSize,
			TextureFormat.RGBA32,
			mipChain: false);
		texture.name = "DotaCooldownOverlay" + percent;
		texture.SetPixels32(pixels);
		texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		CooldownOverlayTextures.Add(percent, texture);
		return texture;
	}

	public override float GetWidth(float maxWidth)
	{
		return Width;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		float width = GetWidth(maxWidth);
		Rect rect = new Rect(topLeft.x, topLeft.y, width, GizmoHeight);
		DrawSlots(rect, comp, SlotSize, Gap, drawBackground: true);

		return new GizmoResult(GizmoState.Clear);
	}
}
