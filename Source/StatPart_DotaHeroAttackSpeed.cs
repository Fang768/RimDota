using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// 将 Dota 总攻速换算为 RimWorld 攻击间隔倍率，并应用到瞄准和冷却。
/// </summary>
public class StatPart_DotaHeroAttackSpeed : StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		Pawn pawn = DotaHeroUtil.RequestPawn(req);
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return;
		}
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (comp != null)
		{
			val *= comp.AttackCooldownMultiplier;
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		Pawn pawn = DotaHeroUtil.RequestPawn(req);
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return null;
		}
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (comp == null)
		{
			return null;
		}
		return "Dota 攻速: " + comp.DisplayAttackSpeed.ToString("0")
			+ "（攻击间隔 ×" + comp.AttackCooldownMultiplier.ToString("0.##") + "）";
	}
}
