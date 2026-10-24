using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// Bottom-bar placeholder for a learned Dota skill. The first framework pass
/// intentionally has no combat effect; this gizmo makes configured skills
/// visible and exposes their current level and mana cost.
/// </summary>
public class Gizmo_DotaSkill : Gizmo
{
	private static readonly Color Background =
		new Color(0.06f, 0.07f, 0.09f, 1f);
	private static readonly Color ManaBackground =
		new Color(0.02f, 0.03f, 0.06f, 0.88f);

	private readonly HediffComp_DotaSkills comp;
	private readonly int slotIndex;

	public Gizmo_DotaSkill(HediffComp_DotaSkills comp, int slotIndex)
	{
		this.comp = comp;
		this.slotIndex = slotIndex;
		Order = -90f + slotIndex;
	}

	public override float GetWidth(float maxWidth)
	{
		return 64f;
	}

	public override GizmoResult GizmoOnGUI(
		Vector2 topLeft,
		float maxWidth,
		GizmoRenderParms parms)
	{
		AbilityDef ability = comp.GetSkillAt(slotIndex);
		DotaSkillExtension extension = DotaSkillExtension.For(ability);
		if (ability == null || extension == null)
		{
			return new GizmoResult(GizmoState.Clear);
		}

		int level = comp.GetSkillLevelAt(slotIndex);
		Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
		Rect icon = new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, 48f);
		Widgets.DrawBoxSolid(rect, Background);
		Widgets.DrawBoxSolid(icon, extension.placeholderColor);
		DotaSkillsTabView.DrawSteelFrame(rect, 1f);
		DotaSkillsTabView.DrawSteelFrame(icon, 1f);

		Rect manaRect = new Rect(
			icon.x + 2f,
			icon.yMax - 17f,
			28f,
			15f);
		Widgets.DrawBoxSolid(manaRect, ManaBackground);
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(
			manaRect,
			extension.ManaCostAtLevel(Mathf.Max(1, level)).ToString("0"));
		Text.Anchor = TextAnchor.UpperLeft;

		DrawLevelPips(
			new Rect(rect.x + 3f, icon.yMax + 3f, rect.width - 6f, 8f),
			level,
			extension.ResolvedMaxLevel);

		TooltipHandler.TipRegion(
			rect,
			ability.LabelCap + "\n" + ability.description
				+ "\n\n等级：" + level + " / " + extension.ResolvedMaxLevel
				+ "\n法力消耗："
				+ extension.ManaCostAtLevel(Mathf.Max(1, level)).ToString("0")
				+ "\n\n当前为技能占位符，尚无实际施法效果。");

		if (Widgets.ButtonInvisible(rect))
		{
			Messages.Message(
				ability.LabelCap + " 尚未实现实际技能效果。",
				MessageTypeDefOf.RejectInput);
		}
		return new GizmoResult(GizmoState.Clear);
	}

	private static void DrawLevelPips(Rect rect, int current, int maximum)
	{
		if (maximum <= 0)
		{
			return;
		}
		const float pipWidth = 8f;
		const float gap = 4f;
		float totalWidth = maximum * pipWidth
			+ Mathf.Max(0, maximum - 1) * gap;
		float x = rect.center.x - totalWidth / 2f;
		for (int i = 0; i < maximum; i++)
		{
			Rect pip = new Rect(
				x + i * (pipWidth + gap),
				rect.y,
				pipWidth,
				6f);
			Widgets.DrawBoxSolid(
				pip,
				i < current
					? new Color(0.84f, 0.65f, 0.24f, 1f)
					: new Color(0.13f, 0.15f, 0.18f, 1f));
			DotaSkillsTabView.DrawSteelFrame(pip, 1f);
		}
	}
}
