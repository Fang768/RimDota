using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

public readonly struct DotaHeroSetupBudget
{
	public readonly int SkillLevelSum;
	public readonly int InterestValueSum;
	public readonly int InitialPoints;
	public readonly int GrowthHalfPoints;

	public DotaHeroSetupBudget(
		int skillLevelSum,
		int interestValueSum,
		int initialPoints,
		int growthHalfPoints)
	{
		SkillLevelSum = skillLevelSum;
		InterestValueSum = interestValueSum;
		InitialPoints = initialPoints;
		GrowthHalfPoints = growthHalfPoints;
	}

	public float GrowthPoints => GrowthHalfPoints * 0.5f;
}

public static class DotaHeroSetupUtility
{
	public const int BaseInitialPoints = 45;
	public const float BaseGrowthPoints = 5f;
	public const int MaxInitialAttribute = 35;
	public const float MaxGrowthPerLevel = 5f;

	public static DotaHeroSetupBudget CalculateBudget(Pawn pawn)
	{
		int skillLevelSum = 0;
		int interestValueSum = 0;
		if (pawn?.skills != null)
		{
			foreach (SkillDef skillDef in DefDatabase<SkillDef>.AllDefsListForReading)
			{
				SkillRecord record = pawn.skills.GetSkill(skillDef);
				if (record == null)
				{
					continue;
				}

				skillLevelSum += Mathf.Clamp(record.Level, 0, 20);
				interestValueSum += InterestValue(record.passion);
			}
		}

		int extraInitialPoints = skillLevelSum / 4;
		int growthHalfPoints = 10 + interestValueSum;
		return new DotaHeroSetupBudget(
			skillLevelSum,
			interestValueSum,
			BaseInitialPoints + extraInitialPoints,
			growthHalfPoints);
	}

	private static int InterestValue(Passion passion)
	{
		return passion switch
		{
			Passion.Minor => 1,
			Passion.Major => 2,
			_ => 0,
		};
	}
}
