using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// 给原版 Stat 添加“英雄三维”动态加成：最终值 += 属性值 × 每点加成。
/// </summary>
public class StatPart_DotaHeroAttribute : StatPart
{
	public DotaAttribute attribute;

	public float offsetPerPoint;

	public float factorPerPoint;

	public override void TransformValue(StatRequest req, ref float val)
	{
		Pawn pawn = DotaHeroUtil.RequestPawn(req);
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return;
		}
		float attr = DotaHeroUtil.GetAttribute(pawn, attribute);
		if (factorPerPoint != 0f)
		{
			val *= 1f + attr * factorPerPoint;
		}
		val += attr * offsetPerPoint;
	}

	public override string ExplanationPart(StatRequest req)
	{
		Pawn pawn = DotaHeroUtil.RequestPawn(req);
		if (pawn == null || !DotaHeroUtil.IsHero(pawn))
		{
			return null;
		}
		float attr = DotaHeroUtil.GetAttribute(pawn, attribute);
		string s = "";
		if (factorPerPoint != 0f)
		{
			s += (attr * factorPerPoint).ToString("+0.0%;-0.0%;0%") + " ";
		}
		if (offsetPerPoint != 0f)
		{
			s += (attr * offsetPerPoint).ToString("+0.##;-0.##;0");
		}
		return "Dota " + attribute + ": " + s.Trim();
	}
}
