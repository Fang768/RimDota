using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// Dota2 用的全伤害减免：加算、可为负，作用在 IncomingDamageFactor 上，
/// 对物理、法术、纯粹伤害都生效。具体来源（刚被兽被动/赏金猎人天赋/
/// 幽鬼折射/司夜刺客神杖等）后续接入，这里先留通用接口。
/// </summary>
public class StatPart_DotaUniversalDamageReduction : StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		Pawn pawn = req.Pawn;
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return;
		}

		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (comp == null)
		{
			return;
		}

		val *= Mathf.Max(0f, 1f - comp.UniversalDamageReduction);
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

		return "Dota2 全伤害减免: " + (comp.UniversalDamageReduction * 100f).ToString("+0.##;-0.##;0") + "%";
	}
}
