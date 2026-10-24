using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

public enum DotaAbilityKind
{
	Basic,
	Derived,
	Ultimate,
	Innate
}
public enum DotaTalentSide
{
	Left,
	Right
}

/// <summary>
/// Static metadata for a Dota ability. AbilityDef intentionally carries no
/// execution components in this first pass; the metadata is enough to power
/// the configuration UI and future ability implementations.
/// </summary>
public class DotaSkillExtension : DefModExtension
{
	public DotaAbilityKind kind = DotaAbilityKind.Basic;

	public int maxLevel;

	public List<float> manaCosts = new List<float>();

	public List<int> levelRequiredHeroLevels = new List<int>();

	public int pointCostPerLevel = 1;

	public bool isPlaceholder;

	public Color placeholderColor = new Color(0.28f, 0.52f, 0.72f, 1f);

	public bool startsInCommonPool;

	public List<AbilityDef> parentAbilityDefs = new List<AbilityDef>();

	public int ResolvedMaxLevel
	{
		get => Mathf.Max(0, maxLevel);
	}

	public float ManaCostAtLevel(int level)
	{
		if (manaCosts == null || manaCosts.Count == 0)
		{
			return 0f;
		}
		int index = Mathf.Clamp(level - 1, 0, manaCosts.Count - 1);
		return manaCosts[index];
	}

	public int RequiredHeroLevelForLevel(int level)
	{
		if (levelRequiredHeroLevels == null
			|| level <= 0
			|| level > levelRequiredHeroLevels.Count)
		{
			return 0;
		}
		return Mathf.Max(0, levelRequiredHeroLevels[level - 1]);
	}

	public static DotaSkillExtension For(AbilityDef def)
	{
		return def?.GetModExtension<DotaSkillExtension>();
	}
}
