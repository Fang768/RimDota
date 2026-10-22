using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimDota;

/// <summary>类似皇权“许可未选择”的待分配属性提示。</summary>
public class Alert_DotaHeroSetupAvailable : Alert
{
	private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();
	private Pawn target;

	public Alert_DotaHeroSetupAvailable()
	{
		defaultLabel = "Dota 英雄属性未选择";
		defaultExplanation = "有殖民者获得了 Dota 英雄身份，但尚未分配初始属性和成长。";
		defaultPriority = AlertPriority.Medium;
	}

	public override AlertReport GetReport()
	{
		CalculateTargets();
		return AlertReport.CulpritsAre(targets);
	}

	public override TaggedString GetExplanation()
	{
		return target == null
			? defaultExplanation
			: target.LabelShortCap + " 已获得 Dota 英雄身份，但初始属性和属性成长尚未选择。";
	}

	protected override void OnClick()
	{
		base.OnClick();
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(target);
		if (comp?.AwaitingInitialSetup == true)
		{
			Find.WindowStack.Add(new Dialog_DotaHeroInitialization(target, comp));
		}
	}

	private void CalculateTargets()
	{
		targets.Clear();
		target = null;
		foreach (Pawn pawn in PawnsFinder.HomeMaps_FreeColonistsSpawned)
		{
			if (DotaHeroUtil.GetHeroComp(pawn)?.AwaitingInitialSetup == true)
			{
				target ??= pawn;
				targets.Add(pawn);
			}
		}
	}
}
