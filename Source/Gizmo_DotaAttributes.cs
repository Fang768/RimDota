using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// Dota 属性栏：仿皇权 PsychicEntropyGizmo，显示在英雄选中后的底部命令栏。
/// 顶部显示三维属性（图标 + 当前值 + 成长），底部显示“法力”标签与蓝条。
/// 主属性对应的数值使用其颜色（力量红/敏捷绿/智力蓝），全才三色齐亮，其余默认白。
/// 纯展示，不处理交互。
/// </summary>
[StaticConstructorOnStartup]
public class Gizmo_DotaAttributes : Gizmo
{
	private static readonly Texture2D ManaBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.25f, 0.45f, 0.95f));
	private static readonly Texture2D ManaBgTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.08f, 0.08f, 0.12f));
	private static readonly Dictionary<int, Texture2D> ExperienceRingTextures =
		new Dictionary<int, Texture2D>();

	private static readonly Texture2D StrengthIcon = ContentFinder<Texture2D>.Get("UI/Icons/Dota/Strength", reportFailure: false);
	private static readonly Texture2D AgilityIcon = ContentFinder<Texture2D>.Get("UI/Icons/Dota/Agility", reportFailure: false);
	private static readonly Texture2D IntelligenceIcon = ContentFinder<Texture2D>.Get("UI/Icons/Dota/Intelligence", reportFailure: false);

	private static readonly Color StrengthColor = new Color(0.78f, 0.24f, 0.24f);
	private static readonly Color AgilityColor = new Color(0.27f, 0.71f, 0.35f);
	private static readonly Color IntelligenceColor = new Color(0.27f, 0.51f, 0.90f);

	private const float IconSize = 18f;
	private const float AttributeRowHeight = 30f;
	private const float LevelCircleSize = 52f;
	private const float GizmoHeight = 72f;

	private readonly HediffComp_DotaHero comp;

	public Gizmo_DotaAttributes(HediffComp_DotaHero comp)
	{
		this.comp = comp;
		Order = -100f;
	}

	public override float GetWidth(float maxWidth)
	{
		return 275f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		float width = GetWidth(maxWidth);
		Rect rect = new Rect(topLeft.x, topLeft.y, width, GizmoHeight);
		Rect inner = new Rect(rect.x + 6f, rect.y + 5f, rect.width - 12f, rect.height - 10f);

		Widgets.DrawWindowBackground(rect);

		Rect circleRect = new Rect(
			inner.x,
			inner.y + (inner.height - LevelCircleSize) * 0.5f,
			LevelCircleSize,
			LevelCircleSize);
		DrawLevelCircle(circleRect);

		Rect lower = new Rect(
			inner.x + LevelCircleSize + 6f,
			inner.y,
			inner.width - LevelCircleSize - 6f,
			inner.height);
		DrawAttributes(lower);
		DrawManaBar(lower);

		RegisterTooltip(lower);

		Text.Anchor = TextAnchor.UpperLeft;
		return new GizmoResult(GizmoState.Clear);
	}

	private void DrawAttributes(Rect inner)
	{
		float groupWidth = inner.width / 3f;

		DrawAttributeGroup(
			new Rect(inner.x, inner.y, groupWidth, AttributeRowHeight),
			StrengthIcon, comp.Strength, comp.StrengthGrowth, ValueColorFor(DotaPrimaryAttribute.Strength));
		DrawAttributeGroup(
			new Rect(inner.x + groupWidth, inner.y, groupWidth, AttributeRowHeight),
			AgilityIcon, comp.Agility, comp.AgilityGrowth, ValueColorFor(DotaPrimaryAttribute.Agility));
		DrawAttributeGroup(
			new Rect(inner.x + groupWidth * 2f, inner.y, groupWidth, AttributeRowHeight),
			IntelligenceIcon, comp.Intelligence, comp.IntelligenceGrowth, ValueColorFor(DotaPrimaryAttribute.Intelligence));
	}

	private Color ValueColorFor(DotaPrimaryAttribute attribute)
	{
		if (comp.PrimaryAttribute == DotaPrimaryAttribute.Universal)
		{
			return ColorFor(attribute);
		}
		return comp.PrimaryAttribute == attribute ? ColorFor(attribute) : Color.white;
	}

	private static Color ColorFor(DotaPrimaryAttribute attribute)
	{
		return attribute switch
		{
			DotaPrimaryAttribute.Strength => StrengthColor,
			DotaPrimaryAttribute.Agility => AgilityColor,
			DotaPrimaryAttribute.Intelligence => IntelligenceColor,
			_ => Color.white,
		};
	}

	private static void DrawAttributeGroup(Rect group, Texture2D icon, float value, float growth, Color valueColor)
	{
		if (icon != null)
		{
			Rect iconRect = new Rect(group.x, group.y + (AttributeRowHeight - IconSize) * 0.5f, IconSize, IconSize);
			GUI.DrawTexture(iconRect, icon);
		}

		float x = group.x + IconSize + 4f;

		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		string valueStr = value.ToString("0");
		Vector2 valueSize = Text.CalcSize(valueStr);
		Rect valueRect = new Rect(x, group.y, valueSize.x + 1f, group.height);
		GUI.color = valueColor;
		Widgets.Label(valueRect, valueStr);
		GUI.color = Color.white;

		Text.Font = GameFont.Tiny;
		GUI.color = new Color(0.72f, 0.72f, 0.72f);
		string growthStr = growth >= 0f ? "+" + growth.ToString("0.0") : growth.ToString("0.0");
		Rect growthRect = new Rect(valueRect.xMax + 2f, group.y, group.xMax - valueRect.xMax - 2f, group.height);
		Widgets.Label(growthRect, growthStr);
		GUI.color = Color.white;
	}

	private void DrawManaBar(Rect inner)
	{
		Rect bottomRect = new Rect(inner.x, inner.y + AttributeRowHeight + 4f, inner.width, 22f);

		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Rect manaLabelRect = new Rect(bottomRect.x, bottomRect.y, 38f, bottomRect.height);
		Widgets.Label(manaLabelRect, "DotaMana".Translate());

		Rect barRect = new Rect(bottomRect.x + 44f, bottomRect.y, bottomRect.width - 44f, bottomRect.height);
		Widgets.FillableBar(barRect, comp.ManaPercent, ManaBarTex, ManaBgTex, doBorder: true);

		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(barRect, comp.Mana.ToString("0") + "/" + comp.MaxMana.ToString("0"));
	}

	private void DrawLevelCircle(Rect circleRect)
	{
		float progress = comp.HeroLevel >= DotaHeroLeveling.MaxLevel
			? 1f
			: Mathf.Clamp01(comp.ExperienceWithinLevel / comp.ExperienceForNextLevel);
		Texture2D ring = GetExperienceRingTexture(Mathf.RoundToInt(progress * 100f));
		GUI.DrawTexture(circleRect, ring, ScaleMode.ScaleToFit);

		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(circleRect, comp.HeroLevel.ToString());

		string tooltip = comp.HeroLevel >= DotaHeroLeveling.MaxLevel
			? "等级 " + comp.HeroLevel + " / " + DotaHeroLeveling.MaxLevel
				+ "\n经验：已满级"
			: "等级 " + comp.HeroLevel + " / " + DotaHeroLeveling.MaxLevel
				+ "\n本级经验：" + comp.ExperienceWithinLevel.ToString("0")
				+ " / " + comp.ExperienceForNextLevel.ToString("0")
				+ "\n累计经验：" + comp.Experience.ToString("0");
		TooltipHandler.TipRegion(circleRect, tooltip);
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

		texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, mipChain: false);
		texture.name = "DotaLevelExperienceRing" + progressPercent;
		texture.SetPixels32(pixels);
		texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		ExperienceRingTextures.Add(progressPercent, texture);
		return texture;
	}

	private void RegisterTooltip(Rect rect)
	{
		float totalMaxHp = 0f;
		if (comp.Pawn?.health?.hediffSet != null)
		{
			foreach (BodyPartRecord part in comp.Pawn.health.hediffSet.GetNotMissingParts())
			{
				totalMaxHp += part.def.GetMaxHealth(comp.Pawn);
			}
		}

		string tip = "等级 " + comp.HeroLevel + " / " + DotaHeroLeveling.MaxLevel + "\n"
			+ (comp.HeroLevel >= DotaHeroLeveling.MaxLevel
				? "  经验：已满级\n"
				: "  经验：" + comp.ExperienceWithinLevel.ToString("0") + " / "
					+ comp.ExperienceForNextLevel.ToString("0") + "\n")
			+ "力量 " + comp.Strength.ToString("0") + "\n"
			+ "  生命上限 +" + ((comp.HealthScaleMult - 1f) * 100f).ToString("0") + "%（总 " + totalMaxHp.ToString("0") + "）\n"
			+ "  回血 " + comp.HealthRegenPerSecond.ToString("0.##") + " 点/秒\n"
			+ "敏捷 " + comp.Agility.ToString("0") + "\n"
			+ "  护甲 " + comp.ArmorPoints.ToString("0.##") + "（物理减伤 " + ((1f - comp.PhysicalDamageMultiplier) * 100f).ToString("0.#") + "%）\n"
			+ "  瞄准/攻速 " + (comp.AimAndCooldownFactor * 100f).ToString("0") + "%\n"
			+ "智力 " + comp.Intelligence.ToString("0") + "\n"
			+ "  魔抗 " + (comp.MagicResistance * 100f).ToString("0.#") + "%\n"
			+ "  法力上限 " + comp.MaxMana.ToString("0") + "\n"
			+ "  回蓝 " + comp.ManaRegenPerSecond.ToString("0.##") + " 点/秒\n"
			+ "  心灵敏感度 +" + (comp.PsychicBonus * 100f).ToString("0") + "%";
		TooltipHandler.TipRegion(rect, tip);
	}
}
