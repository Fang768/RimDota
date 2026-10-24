using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>Dota 英雄完整属性与装备页，只对 Dota 英雄显示。</summary>
[StaticConstructorOnStartup]
public class ITab_Pawn_Dota : ITab
{
	private static readonly Texture2D StrengthIcon =
		ContentFinder<Texture2D>.Get("UI/Icons/Dota/Strength", reportFailure: false);
	private static readonly Texture2D AgilityIcon =
		ContentFinder<Texture2D>.Get("UI/Icons/Dota/Agility", reportFailure: false);
	private static readonly Texture2D IntelligenceIcon =
		ContentFinder<Texture2D>.Get("UI/Icons/Dota/Intelligence", reportFailure: false);

	private static readonly Color StrengthColor = new Color(0.78f, 0.24f, 0.24f);
	private static readonly Color AgilityColor = new Color(0.27f, 0.71f, 0.35f);
	private static readonly Color IntelligenceColor = new Color(0.27f, 0.51f, 0.90f);
	private static readonly Color PanelColor = new Color(0.035f, 0.035f, 0.04f, 0.96f);
	private static readonly Color PanelOutline = new Color(0.22f, 0.22f, 0.25f);
	private static readonly Color SecondaryText = new Color(0.72f, 0.72f, 0.72f);

	private enum DotaTabPage
	{
		Overview,
		Equipment,
		Skills
	}

	private DotaTabPage currentPage;
	private readonly DotaSkillsTabView skillsView = new DotaSkillsTabView();

	public ITab_Pawn_Dota()
	{
		size = new Vector2(900f, 640f);
		labelKey = "DotaTab";
	}

	public override bool IsVisible =>
		SelPawn != null && DotaHeroUtil.IsHero(SelPawn);

	protected override void CloseTab()
	{
		skillsView.DiscardDraft();
		base.CloseTab();
	}

	public override void TabUpdate()
	{
		base.TabUpdate();
		if (!StillValid)
		{
			skillsView.DiscardDraft();
		}
	}

	protected override void FillTab()
	{
		Pawn pawn = SelPawn;
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (pawn == null || comp == null)
		{
			return;
		}

		Rect rect = new Rect(0f, 20f, size.x, size.y - 20f).ContractedBy(10f);
		Text.Font = GameFont.Medium;
		Widgets.Label(
			new Rect(rect.x, rect.y, rect.width, 32f),
			pawn.LabelShortCap + " · " + PrimaryAttributeLabel(comp.PrimaryAttribute));
		Text.Font = GameFont.Small;

		Rect tabRow = new Rect(rect.x, rect.y + 34f, 278f, 30f);
		if (Widgets.ButtonText(
			new Rect(tabRow.x, tabRow.y, 86f, tabRow.height),
			currentPage == DotaTabPage.Overview ? "● 概览" : "概览"))
		{
			currentPage = DotaTabPage.Overview;
		}
		if (Widgets.ButtonText(
			new Rect(tabRow.x + 92f, tabRow.y, 86f, tabRow.height),
			currentPage == DotaTabPage.Equipment ? "● 装备" : "装备"))
		{
			currentPage = DotaTabPage.Equipment;
		}
		if (Widgets.ButtonText(
			new Rect(tabRow.x + 184f, tabRow.y, 86f, tabRow.height),
			currentPage == DotaTabPage.Skills ? "● 技能" : "技能"))
		{
			currentPage = DotaTabPage.Skills;
		}

		Rect content = new Rect(
			rect.x,
			rect.y + 74f,
			rect.width,
			rect.height - 74f);
		if (currentPage == DotaTabPage.Overview)
		{
			DrawOverview(content, pawn, comp);
		}
		else if (currentPage == DotaTabPage.Equipment)
		{
			DrawEquipment(content, pawn);
		}
		else
		{
			skillsView.Draw(
				content,
				pawn,
				DotaHeroUtil.GetSkillsComp(pawn));
		}
	}

	private static void DrawOverview(
		Rect rect,
		Pawn pawn,
		HediffComp_DotaHero comp)
	{
		float gap = 8f;
		float panelWidth = (rect.width - gap) * 0.5f;
		Rect attackPanel = new Rect(rect.x, rect.y, panelWidth, 164f);
		Rect defensePanel = new Rect(
			attackPanel.xMax + gap,
			rect.y,
			panelWidth,
			164f);
		DrawAttackPanel(attackPanel, pawn, comp);
		DrawDefensePanel(defensePanel, pawn, comp);

		float rowY = attackPanel.yMax + 8f;
		DrawAttributeBlock(
			new Rect(rect.x, rowY, rect.width, 108f),
			StrengthIcon,
			DotaPrimaryAttribute.Strength,
			comp.Strength,
			comp.StrengthGrowth,
			comp,
			StrengthAttackLine(comp),
			"生命上限 +" + ((comp.HealthScaleMult - 1f) * 100f).ToString("0") + "%",
			comp.HealthRegenPerSecond.ToString("0.##") + " 点/秒生命恢复");
		rowY += 116f;
		DrawAttributeBlock(
			new Rect(rect.x, rowY, rect.width, 108f),
			AgilityIcon,
			DotaPrimaryAttribute.Agility,
			comp.Agility,
			comp.AgilityGrowth,
			comp,
			AgilityAttackLine(comp),
			comp.ArmorPoints.ToString("0.##") + " 点护甲",
			comp.Agility.ToString("0") + " 点攻击速度");
		rowY += 116f;
		DrawAttributeBlock(
			new Rect(rect.x, rowY, rect.width, 108f),
			IntelligenceIcon,
			DotaPrimaryAttribute.Intelligence,
			comp.Intelligence,
			comp.IntelligenceGrowth,
			comp,
			IntelligenceAttackLine(comp),
			comp.MaxManaFromIntelligence.ToString("0") + " 点法力",
			comp.ManaRegenFromIntelligence.ToString("0.##") + " 点/秒法力恢复",
			"+" + (comp.Intelligence * 0.1f).ToString("0.#") + "% 魔法抗性");
	}

	private static void DrawAttackPanel(
		Rect rect,
		Pawn pawn,
		HediffComp_DotaHero comp)
	{
		DrawPanel(rect, "攻击");
		float y = rect.y + 38f;
		DrawLabelValue(
			rect,
			ref y,
			"攻击力",
			"<color=#FFFFFF>" + comp.BaseAttackDamage.ToString("0.#") + "</color>"
				+ " <color=#63D66D>+" + comp.BonusAttackDamage.ToString("0.#") + "</color>");
		DrawLabelValue(
			rect,
			ref y,
			"攻击速度",
			comp.DisplayAttackSpeed.ToString("0"));
		DrawLabelValue(
			rect,
			ref y,
			"移动速度",
			pawn.GetStatValue(StatDefOf.MoveSpeed).ToString("0.0"));
		DrawLabelValue(rect, ref y, "技能增强", "0.0%");
		DrawLabelValue(
			rect,
			ref y,
			"魔法恢复",
			comp.ManaRegenPerSecond.ToString("0.##"));
	}

	private static void DrawDefensePanel(
		Rect rect,
		Pawn pawn,
		HediffComp_DotaHero comp)
	{
		DrawPanel(rect, "防御");
		float y = rect.y + 38f;
		DrawLabelValue(rect, ref y, "护甲", comp.ArmorPoints.ToString("0.0"));
		DrawLabelValue(
			rect,
			ref y,
			"物理抗性",
			((1f - comp.PhysicalDamageMultiplier) * 100f).ToString("0") + "%");
		DrawLabelValue(
			rect,
			ref y,
			"魔法抗性",
			(comp.MagicResistance * 100f).ToString("0") + "%");
		float rangedDodge = pawn.GetStatValue(DotaDefOf.DotaRangedDodgeChance);
		float meleeDodge = pawn.GetStatValue(StatDefOf.MeleeDodgeChance);
		DrawLabelValue(
			rect,
			ref y,
			"闪避（远程/近战）",
			(rangedDodge * 100f).ToString("0") + "% / "
				+ (meleeDodge * 100f).ToString("0") + "%");
		DrawLabelValue(
			rect,
			ref y,
			"生命恢复",
			comp.HealthRegenPerSecond.ToString("0.##"));
	}

	private static void DrawPanel(Rect rect, string title)
	{
		Widgets.DrawBoxSolid(rect, PanelColor);
		Widgets.DrawBox(rect);
		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.UpperCenter;
		Widgets.Label(new Rect(rect.x, rect.y + 7f, rect.width, 28f), title);
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private static void DrawLabelValue(
		Rect panel,
		ref float y,
		string label,
		string value)
	{
		Text.Font = GameFont.Small;
		Rect row = new Rect(panel.x + 10f, y, panel.width - 20f, 22f);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(row, label + "：");
		Text.Anchor = TextAnchor.MiddleRight;
		Widgets.Label(row, value);
		Text.Anchor = TextAnchor.UpperLeft;
		y += 24f;
	}

	private static void DrawAttributeBlock(
		Rect rect,
		Texture2D icon,
		DotaPrimaryAttribute attribute,
		float value,
		float growth,
		HediffComp_DotaHero comp,
		string attackLine,
		params string[] effectLines)
	{
		Color color = ColorFor(attribute);
		bool main = comp.PrimaryAttribute == attribute
			|| comp.PrimaryAttribute == DotaPrimaryAttribute.Universal;
		Color background = main
			? new Color(color.r * 0.24f, color.g * 0.24f, color.b * 0.24f, 0.92f)
			: new Color(0.025f, 0.025f, 0.03f, 0.94f);
		Widgets.DrawBoxSolid(rect, background);
		Widgets.DrawBox(rect);

		if (icon != null)
		{
			GUI.DrawTexture(new Rect(rect.x + 16f, rect.y + 24f, 46f, 46f), icon);
		}

		float x = rect.x + 76f;
		float width = rect.width - 92f;
		Text.Font = GameFont.Medium;
		GUI.color = main ? color : Color.white;
		Widgets.Label(
			new Rect(x, rect.y + 8f, width, 28f),
			value.ToString("0") + "（每级获得 " + growth.ToString("0.0") + " 点）");

		Text.Font = GameFont.Small;
		GUI.color = main ? color : SecondaryText;
		float lineY = rect.y + 36f;
		if (!attackLine.NullOrEmpty())
		{
			Widgets.Label(new Rect(x, lineY, width, 20f), "= " + attackLine);
			lineY += 17f;
		}
		foreach (string line in effectLines)
		{
			Widgets.Label(new Rect(x, lineY, width, 20f), "= " + line);
			lineY += 17f;
		}
		GUI.color = Color.white;
	}

	private static string StrengthAttackLine(HediffComp_DotaHero comp)
	{
		if (comp.PrimaryAttribute == DotaPrimaryAttribute.Strength)
		{
			return comp.AttributeAttackDamage.ToString("0.#") + " 点基础攻击力";
		}
		return comp.PrimaryAttribute == DotaPrimaryAttribute.Universal
			? (comp.Strength * 0.5f).ToString("0.#") + " 点基础攻击力"
			: "";
	}

	private static string AgilityAttackLine(HediffComp_DotaHero comp)
	{
		if (comp.PrimaryAttribute == DotaPrimaryAttribute.Agility)
		{
			return comp.AttributeAttackDamage.ToString("0.#") + " 点基础攻击力";
		}
		return comp.PrimaryAttribute == DotaPrimaryAttribute.Universal
			? (comp.Agility * 0.5f).ToString("0.#") + " 点基础攻击力"
			: "";
	}

	private static string IntelligenceAttackLine(HediffComp_DotaHero comp)
	{
		if (comp.PrimaryAttribute == DotaPrimaryAttribute.Intelligence)
		{
			return comp.AttributeAttackDamage.ToString("0.#") + " 点基础攻击力";
		}
		return comp.PrimaryAttribute == DotaPrimaryAttribute.Universal
			? (comp.Intelligence * 0.5f).ToString("0.#") + " 点基础攻击力"
			: "";
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

	private static void DrawEquipment(Rect rect, Pawn pawn)
	{
		HediffComp_DotaInventory inventory = DotaHeroUtil.GetInventoryComp(pawn);
		if (inventory == null)
		{
			Widgets.Label(rect, "该英雄没有 Dota 装备栏。");
			return;
		}

		Widgets.Label(
			new Rect(rect.x, rect.y, rect.width, 24f),
			"Dota 装备栏：" + inventory.Count + " / " + HediffComp_DotaInventory.SlotCount);
		Rect slotsRect = new Rect(
			rect.center.x - 81f,
			rect.y + 48f,
			162f,
			104f);
		Gizmo_DotaEquipmentSlots.DrawSlots(
			slotsRect,
			inventory,
			46f,
			8f,
			drawBackground: false);
		Widgets.Label(
			new Rect(rect.x + 20f, slotsRect.yMax + 18f, rect.width - 40f, 40f),
			"左键使用主动装备，按住拖动可换位或拖出丢下；右键可锁定、解锁、丢下或查看信息。");
	}

	private static string PrimaryAttributeLabel(DotaPrimaryAttribute attribute)
	{
		return attribute switch
		{
			DotaPrimaryAttribute.Strength => "力量",
			DotaPrimaryAttribute.Agility => "敏捷",
			DotaPrimaryAttribute.Intelligence => "智力",
			_ => "全才",
		};
	}
}

internal static class DotaHeroUiTextUtility
{
	public static string ExperienceWithinLabel(this HediffComp_DotaHero comp)
	{
		return comp.ExperienceWithinLevel.ToString("0")
			+ " / " + comp.ExperienceForNextLevel.ToString("0");
	}
}
