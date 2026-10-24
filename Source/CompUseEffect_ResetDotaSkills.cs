using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// The respec potion clears skill placement and levels. Attribute points and
/// talents are intentionally preserved.
/// </summary>
public class CompUseEffect_ResetDotaSkills : CompUseEffect
{
	public override AcceptanceReport CanBeUsedBy(Pawn p)
	{
		if (!DotaHeroUtil.IsHero(p))
		{
			return "只有 Dota 英雄可以使用洗点药。";
		}
		if (DotaHeroUtil.GetSkillsComp(p) == null)
		{
			return "该英雄没有 Dota 技能组件。";
		}
		return true;
	}

	public override TaggedString ConfirmMessage(Pawn p)
	{
		return "使用后会清空所有技能配置、技能等级、属性点和天赋选择，并返还对应的技能点与天赋点。";
	}

	public override void DoEffect(Pawn usedBy)
	{
		base.DoEffect(usedBy);
		HediffComp_DotaSkills comp = DotaHeroUtil.GetSkillsComp(usedBy);
		if (comp == null)
		{
			return;
		}
		comp.ResetSkillProgression();
		Messages.Message(
			usedBy.LabelShortCap + " 的所有 Dota 技能、属性点与天赋已重置，点数已返还。",
			usedBy,
			MessageTypeDefOf.PositiveEvent);
	}
}
