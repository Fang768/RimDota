using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// Save-scoped common skill pool. Hero-specific skills live on each hero's
/// HediffComp_DotaSkills and are therefore preserved with the pawn.
/// </summary>
public class DotaGameComponent_DotaSkillPools : GameComponent
{
	private List<string> commonSkillDefNames = new List<string>();
	private bool initialized;

	public DotaGameComponent_DotaSkillPools()
	{
	}

	public DotaGameComponent_DotaSkillPools(Game game)
		: this()
	{
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(
			ref commonSkillDefNames,
			"commonSkillDefNames",
			LookMode.Value);
		Scribe_Values.Look(ref initialized, "initialized", false);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			EnsureInitialized();
			Sanitize();
		}
	}

	public override void FinalizeInit()
	{
		base.FinalizeInit();
		EnsureInitialized();
	}

	public IReadOnlyList<AbilityDef> CommonSkills()
	{
		EnsureInitialized();
		Sanitize();
		return commonSkillDefNames
			.Select(DefDatabase<AbilityDef>.GetNamedSilentFail)
			.Where(def => def != null)
			.ToList();
	}

	public bool AddCommonSkill(AbilityDef ability)
	{
		if (ability == null || !DotaSkillPoolUtility.IsDotaSkill(ability))
		{
			return false;
		}
		EnsureInitialized();
		if (commonSkillDefNames.Contains(ability.defName))
		{
			return false;
		}
		commonSkillDefNames.Add(ability.defName);
		return true;
	}

	private void EnsureInitialized()
	{
		commonSkillDefNames ??= new List<string>();
		if (initialized)
		{
			return;
		}

		foreach (AbilityDef ability in DefDatabase<AbilityDef>.AllDefsListForReading)
		{
			DotaSkillExtension extension = DotaSkillExtension.For(ability);
			if (extension?.startsInCommonPool == true
				&& !commonSkillDefNames.Contains(ability.defName))
			{
				commonSkillDefNames.Add(ability.defName);
			}
		}
		initialized = true;
	}

	private void Sanitize()
	{
		HashSet<string> seen = new HashSet<string>();
		commonSkillDefNames.RemoveAll(defName =>
		{
			AbilityDef ability = DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
			return ability == null
				|| !DotaSkillPoolUtility.IsDotaSkill(ability)
				|| !seen.Add(defName);
		});
	}
}

public static class DotaSkillPoolUtility
{
	public static bool IsDotaSkill(AbilityDef ability)
	{
		return DotaSkillExtension.For(ability) != null;
	}

	public static DotaGameComponent_DotaSkillPools CommonPool =>
		Current.Game?.GetComponent<DotaGameComponent_DotaSkillPools>();

	public static IReadOnlyList<AbilityDef> CommonSkills()
	{
		return CommonPool?.CommonSkills() ?? new List<AbilityDef>();
	}

	public static IReadOnlyList<AbilityDef> HeroSpecificSkills(
		HediffComp_DotaSkills comp)
	{
		return comp?.HeroSpecificSkills() ?? new List<AbilityDef>();
	}

	public static bool AddCommonSkill(AbilityDef ability)
	{
		return CommonPool?.AddCommonSkill(ability) ?? false;
	}

	public static bool AddHeroSpecificSkill(
		HediffComp_DotaSkills comp,
		AbilityDef ability)
	{
		return comp?.AddHeroSpecificSkill(ability) ?? false;
	}
}
