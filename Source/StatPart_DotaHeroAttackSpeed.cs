using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// 给瞄准/冷却类 Stat 添加 Dota 英雄的最终攻速加成：
/// 敏捷贡献 + 装备攻速，统一由 AimAndCooldownFactor 提供。
/// </summary>
public class StatPart_DotaHeroAttackSpeed : StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		Pawn pawn = req.Pawn;
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return;
		}
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (comp != null)
		{
			val *= 1f + comp.AimAndCooldownFactor;
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		Pawn pawn = req.Pawn;
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return null;
		}
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (comp == null)
		{
			return null;
		}
		return "Dota 瞄准/攻速: " + (comp.AimAndCooldownFactor * 100f).ToString("+0.##;-0.##;0") + "%";
	}
}
