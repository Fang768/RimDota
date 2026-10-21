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

	private static readonly Texture2D StrengthIcon = ContentFinder<Texture2D>.Get("UI/Icons/Dota/Strength", reportFailure: false);
	private static readonly Texture2D AgilityIcon = ContentFinder<Texture2D>.Get("UI/Icons/Dota/Agility", reportFailure: false);
	private static readonly Texture2D IntelligenceIcon = ContentFinder<Texture2D>.Get("UI/Icons/Dota/Intelligence", reportFailure: false);

	private static readonly Color StrengthColor = new Color(0.78f, 0.24f, 0.24f);
	private static readonly Color AgilityColor = new Color(0.27f, 0.71f, 0.35f);
	private static readonly Color IntelligenceColor = new Color(0.27f, 0.51f, 0.90f);

	private const float IconSize = 18f;
	private const float AttributeRowHeight = 30f;

	private readonly HediffComp_DotaHero comp;

	public Gizmo_DotaAttributes(HediffComp_DotaHero comp)
	{
		this.comp = comp;
		Order = -100f;
	}

	public override float GetWidth(float maxWidth)
	{
		return 230f;
	}

	public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
	{
		float width = GetWidth(maxWidth);
		Rect rect = new Rect(topLeft.x, topLeft.y, width, 75f);
		Rect inner = new Rect(rect.x + 6f, rect.y + 5f, rect.width - 12f, rect.height - 10f);

		Widgets.DrawWindowBackground(rect);

		DrawAttributes(inner);
		DrawManaBar(inner);

		RegisterTooltip(rect);

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

		string tip = "力量 " + comp.Strength.ToString("0") + "\n"
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
