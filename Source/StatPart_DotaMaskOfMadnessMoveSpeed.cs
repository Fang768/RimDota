using RimWorld;
using Verse;

namespace RimDota;

/// <summary>疯狂面具主动期间按近战/远程提供移动速度倍率。</summary>
public class StatPart_DotaMaskOfMadnessMoveSpeed : StatPart
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		Pawn pawn = DotaHeroUtil.RequestPawn(req);
		if (pawn?.health?.hediffSet == null
			|| !pawn.health.hediffSet.HasHediff(DotaDefOf.DotaMaskOfMadnessBuff))
		{
			return;
		}

		bool ranged = pawn.equipment?.Primary?.def.IsRangedWeapon == true;
		val *= ranged ? 1.08f : 1.12f;
	}

	public override string ExplanationPart(StatRequest req)
	{
		Pawn pawn = DotaHeroUtil.RequestPawn(req);
		if (pawn?.health?.hediffSet == null
			|| !pawn.health.hediffSet.HasHediff(DotaDefOf.DotaMaskOfMadnessBuff))
		{
			return null;
		}

		bool ranged = pawn.equipment?.Primary?.def.IsRangedWeapon == true;
		return "狂热: " + (ranged ? "+8%" : "+12%");
	}
}
