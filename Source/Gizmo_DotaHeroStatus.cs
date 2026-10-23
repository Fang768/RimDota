using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>底部紧凑 Dota 状态：等级/经验圆、法力比例、可展开装备栏。</summary>
[StaticConstructorOnStartup]
public class Gizmo_DotaHeroStatus : Gizmo
{
	private static readonly Texture2D ManaBarTex =
		SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.45f, 0.95f));
	private static readonly Texture2D ManaBgTex =
		SolidColorMaterials.NewSolidColorTexture(new Color(0.08f, 0.08f, 0.12f));
	private static readonly Dictionary<int, Texture2D> ExperienceRingTextures =
		new Dictionary<int, Texture2D>();

	private const float CircleSize = 52f;
	private const float GizmoHeight = 94f;
	private const float ExpandButtonWidth = 20f;
	private const float EquipmentWidth = 92f;

	private readonly HediffComp_DotaHero comp;

	public Gizmo_DotaHeroStatus(HediffComp_DotaHero comp)
	{
		this.comp = comp;
		Order = -100f;
	}

	private HediffComp_DotaInventory Inventory =>
		DotaHeroUtil.GetInventoryComp(comp.Pawn);

	private bool InventoryExpanded =>
		comp.Pawn?.Drafted == true
		|| Inventory?.InventoryExpanded == true;

	public override float GetWidth(float maxWidth)
	{
		return InventoryExpanded ? 186f : 90f;
	}

	public override GizmoResult GizmoOnGUI(
		Vector2 topLeft,
		float maxWidth,
		GizmoRenderParms parms)
	{
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), GizmoHeight);
		Rect inner = rect.ContractedBy(6f);
		Widgets.DrawWindowBackground(rect);

		Rect circleRect = new Rect(
			inner.x,
			inner.y + 6f,
			CircleSize,
			CircleSize);
		DrawLevelRing(circleRect, comp, registerTooltip: false);

		Rect expandRect = new Rect(
			circleRect.xMax + 6f,
			circleRect.center.y - 12f,
			ExpandButtonWidth,
			24f);
		HediffComp_DotaInventory inventory = Inventory;
		bool canToggle = comp.Pawn?.Drafted != true && inventory != null;
		if (Widgets.ButtonText(
			expandRect,
			InventoryExpanded ? "<" : ">",
			active: canToggle)
			&& canToggle)
		{
			inventory.InventoryExpanded = !inventory.InventoryExpanded;
		}
		TooltipHandler.TipRegion(
			expandRect,
			comp.Pawn?.Drafted == true
				? "征兆状态下物品栏会自动展开。"
				: InventoryExpanded
					? "收起 Dota 物品栏。"
					: "展开 Dota 物品栏。");

		Rect statusRect = new Rect(
			inner.x,
			inner.y,
			expandRect.xMin - inner.x,
			inner.height - 14f);
		RegisterStatusTooltip(statusRect);

		if (InventoryExpanded && inventory != null)
		{
			Rect equipmentRect = new Rect(
				expandRect.xMax + 4f,
				inner.y + 2f,
				EquipmentWidth,
				64f);
			Gizmo_DotaEquipmentSlots.DrawSlots(
				equipmentRect,
				inventory,
				28f,
				4f,
				drawBackground: false);
		}

		Rect manaRect = new Rect(
			inner.x,
			inner.yMax - 10f,
			InventoryExpanded ? inner.width : expandRect.xMax - inner.x,
			10f);
		DrawManaBar(manaRect, comp, showValues: InventoryExpanded);
		TooltipHandler.TipRegion(manaRect, StatusTooltip(comp));

		Text.Anchor = TextAnchor.UpperLeft;
		return new GizmoResult(GizmoState.Clear);
	}

	public static void DrawLevelRing(
		Rect circleRect,
		HediffComp_DotaHero comp,
		bool registerTooltip = true)
	{
		float progress = comp.HeroLevel >= DotaHeroLeveling.MaxLevel
			? 1f
			: Mathf.Clamp01(comp.ExperienceWithinLevel / comp.ExperienceForNextLevel);
		Texture2D ring = GetExperienceRingTexture(Mathf.RoundToInt(progress * 100f));
		GUI.DrawTexture(circleRect, ring, ScaleMode.ScaleToFit);

		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(circleRect, comp.HeroLevel.ToString());

		if (registerTooltip)
		{
			TooltipHandler.TipRegion(circleRect, ExperienceTooltip(comp));
		}
	}

	public static void DrawManaBar(
		Rect barRect,
		HediffComp_DotaHero comp,
		bool showValues)
	{
		Widgets.FillableBar(
			barRect,
			comp.ManaPercent,
			ManaBarTex,
			ManaBgTex,
			doBorder: true);
		if (showValues)
		{
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(
				barRect,
				comp.Mana.ToString("0") + "/" + comp.MaxMana.ToString("0"));
		}
	}

	private void RegisterStatusTooltip(Rect rect)
	{
		TooltipHandler.TipRegion(rect, StatusTooltip(comp));
	}

	private static string StatusTooltip(HediffComp_DotaHero comp)
	{
		return ExperienceTooltip(comp)
			+ "\n法力：" + comp.Mana.ToString("0") + " / " + comp.MaxMana.ToString("0")
			+ "（" + (comp.ManaPercent * 100f).ToString("0") + "%）";
	}

	private static string ExperienceTooltip(HediffComp_DotaHero comp)
	{
		return comp.HeroLevel >= DotaHeroLeveling.MaxLevel
			? "等级 " + comp.HeroLevel + " / " + DotaHeroLeveling.MaxLevel
				+ "\n经验：已满级"
			: "等级 " + comp.HeroLevel + " / " + DotaHeroLeveling.MaxLevel
				+ "\n本级经验：" + comp.ExperienceWithinLevel.ToString("0")
				+ " / " + comp.ExperienceForNextLevel.ToString("0")
				+ "\n累计经验：" + comp.Experience.ToString("0");
	}

	private static Texture2D GetExperienceRingTexture(int progressPercent)
	{
		progressPercent = Mathf.Clamp(progressPercent, 0, 100);
		if (ExperienceRingTextures.TryGetValue(progressPercent, out Texture2D texture))
		{
			return texture;
		}

		const int textureSize = 64;
		const float center = (textureSize - 1f) * 0.5f;
		const float outerRadius = 30f;
		const float innerRadius = 24f;
		Color32 progressColor = new Color32(229, 184, 64, 255);
		Color32 backgroundRing = new Color32(55, 47, 29, 255);
		Color32 centerColor = new Color32(17, 17, 23, 220);
		Color32[] pixels = new Color32[textureSize * textureSize];

		float progressAngle = progressPercent / 100f * Mathf.PI * 2f;
		for (int y = 0; y < textureSize; y++)
		{
			for (int x = 0; x < textureSize; x++)
			{
				float dx = x - center;
				float dy = y - center;
				float distance = Mathf.Sqrt(dx * dx + dy * dy);
				int index = y * textureSize + x;
				if (distance > outerRadius)
				{
					continue;
				}
				if (distance < innerRadius)
				{
					pixels[index] = centerColor;
					continue;
				}

				float angle = Mathf.Atan2(dx, -dy);
				if (angle < 0f)
				{
					angle += Mathf.PI * 2f;
				}
				pixels[index] = angle <= progressAngle
					? progressColor
					: backgroundRing;
			}
		}

		texture = new Texture2D(
			textureSize,
			textureSize,
			TextureFormat.RGBA32,
			mipChain: false);
		texture.name = "DotaLevelExperienceRing" + progressPercent;
		texture.SetPixels32(pixels);
		texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		ExperienceRingTextures.Add(progressPercent, texture);
		return texture;
	}
}
