using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// Persistent Dota skill layout, point ledger and talent choices. All edits in
/// the UI are staged in DotaSkillEditState and committed here atomically.
/// </summary>
public class HediffComp_DotaSkills : HediffComp
{
	private List<string> slotAbilityDefNames = new List<string>();
	private List<int> slotLevels = new List<int>();
	private List<string> talentNodeDefNames = new List<string>();
	private List<string> heroSkillDefNames = new List<string>();
	private int earnedSkillPoints;
	private int attributePointsSpent;
	private bool layoutLocked;

	public HediffCompProperties_DotaSkills Props =>
		(HediffCompProperties_DotaSkills)props;

	public bool LayoutLocked => layoutLocked;

	public int EarnedSkillPoints => earnedSkillPoints;

	public int SpentSkillPoints
	{
		get
		{
			int spent = Mathf.Max(0, attributePointsSpent) * Props.AttributePointCost;
			for (int i = 0; i < TotalSlotCount; i++)
			{
				AbilityDef ability = GetSkillAt(i);
				DotaSkillExtension extension = DotaSkillExtension.For(ability);
				if (ability != null && extension != null)
				{
					spent += Mathf.Clamp(GetSkillLevelAt(i), 0, extension.ResolvedMaxLevel)
						* Mathf.Max(0, extension.pointCostPerLevel);
				}
			}
			return spent;
		}
	}

	public int AvailableSkillPoints =>
		Mathf.Max(0, earnedSkillPoints - SpentSkillPoints);

	public int AttributePointsSpent => Mathf.Max(0, attributePointsSpent);

	public int EarnedTalentPoints
	{
		get
		{
			int heroLevel = DotaHeroUtil.GetHeroComp(Pawn)?.HeroLevel ?? 1;
			return Props.TalentPointLevels.Distinct().Count(level => level <= heroLevel);
		}
	}

	public int AvailableTalentPoints =>
		Mathf.Max(0, EarnedTalentPoints - talentNodeDefNames.Count);

	public float AttributePointBonus =>
		AttributePointsSpent * Mathf.Max(0f, Props.attributeBonusPerPoint);

	public float TalentBonus
	{
		get
		{
			float total = 0f;
			foreach (DotaTalentNodeDef node in SelectedTalents())
			{
				total += Mathf.Max(0f, node.allAttributeBonus);
			}
			return total;
		}
	}

	public float AllAttributeBonus => AttributePointBonus + TalentBonus;

	public int TotalSlotCount =>
		Mathf.Max(0, Props.normalSlotCount)
		+ Mathf.Max(0, Props.derivedSlotCount)
		+ Mathf.Max(0, Props.ultimateSlotCount);

	public DotaAbilityKind SlotKindAt(int index)
	{
		int normal = Mathf.Max(0, Props.normalSlotCount);
		int derived = Mathf.Max(0, Props.derivedSlotCount);
		if (index < normal)
		{
			return DotaAbilityKind.Basic;
		}
		if (index < normal + derived)
		{
			return DotaAbilityKind.Derived;
		}
		return DotaAbilityKind.Ultimate;
	}

	public string SlotTypeLabel(int index)
	{
		return SlotKindAt(index) switch
		{
			DotaAbilityKind.Basic => "普通技能",
			DotaAbilityKind.Derived => "衍生技能",
			_ => "奥义",
		};
	}

	public AbilityDef GetSkillAt(int index)
	{
		EnsureLists();
		if (index < 0 || index >= TotalSlotCount)
		{
			return null;
		}
		string defName = slotAbilityDefNames[index];
		return defName.NullOrEmpty()
			? null
			: DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
	}

	public int GetSkillLevelAt(int index)
	{
		EnsureLists();
		return index >= 0 && index < TotalSlotCount ? slotLevels[index] : 0;
	}

	public IReadOnlyList<DotaTalentNodeDef> SelectedTalents()
	{
		EnsureLists();
		return talentNodeDefNames
			.Select(DefDatabase<DotaTalentNodeDef>.GetNamedSilentFail)
			.Where(node => node != null)
			.ToList();
	}

	public IReadOnlyList<AbilityDef> HeroSpecificSkills()
	{
		EnsureLists();
		return heroSkillDefNames
			.Select(DefDatabase<AbilityDef>.GetNamedSilentFail)
			.Where(ability => DotaSkillPoolUtility.IsDotaSkill(ability))
			.ToList();
	}

	public bool AddHeroSpecificSkill(AbilityDef ability)
	{
		if (ability == null || !DotaSkillPoolUtility.IsDotaSkill(ability))
		{
			return false;
		}
		EnsureLists();
		if (heroSkillDefNames.Contains(ability.defName))
		{
			return false;
		}
		heroSkillDefNames.Add(ability.defName);
		return true;
	}

	public DotaSkillEditState CreateEditState()
	{
		EnsureLists();
		return new DotaSkillEditState(this);
	}

	public void ApplyEditState(DotaSkillEditState state)
	{
		if (state == null)
		{
			return;
		}

		EnsureLists();
		slotAbilityDefNames = state.SlotSkills
			.Select(ability => ability?.defName)
			.ToList();
		slotLevels = new List<int>(state.SlotLevels);
		attributePointsSpent = Mathf.Clamp(
			state.AttributePointsSpent,
			0,
			Mathf.Max(0, Props.attributePointCapacity));
		talentNodeDefNames = state.Talents
			.Select(node => node.defName)
			.ToList();
		layoutLocked = true;
		Sanitize();
		NotifyStatsChanged();
	}

	/// <summary>
	/// Used by the respec potion and developer controls. Skill levels, attribute
	/// investments and talent selections are all cleared.
	/// </summary>
	public void ResetSkillProgression()
	{
		EnsureLists();
		for (int i = 0; i < TotalSlotCount; i++)
		{
			slotAbilityDefNames[i] = null;
			slotLevels[i] = 0;
		}
		attributePointsSpent = 0;
		talentNodeDefNames.Clear();
		layoutLocked = false;
		NotifyStatsChanged();
	}

	public void NotifyHeroLevelChanged()
	{
		SyncProgression();
	}

	public void SyncProgression()
	{
		EnsureLists();
		int heroLevel = DotaHeroUtil.GetHeroComp(Pawn)?.HeroLevel ?? 1;
		int pointsEarnedByLevel = Mathf.Max(1, heroLevel)
			* Mathf.Max(0, Props.skillPointsPerLevelUp);
		earnedSkillPoints = Mathf.Max(
			earnedSkillPoints,
			Mathf.Min(
				Mathf.Max(0, Props.skillPointCap),
				pointsEarnedByLevel));
		ClampPointsAndSpending();
	}

	private void EnsureLists()
	{
		slotAbilityDefNames ??= new List<string>();
		slotLevels ??= new List<int>();
		talentNodeDefNames ??= new List<string>();
		heroSkillDefNames ??= new List<string>();
		int count = TotalSlotCount;
		while (slotAbilityDefNames.Count < count)
		{
			slotAbilityDefNames.Add(null);
		}
		while (slotLevels.Count < count)
		{
			slotLevels.Add(0);
		}
		while (slotAbilityDefNames.Count > count)
		{
			slotAbilityDefNames.RemoveAt(slotAbilityDefNames.Count - 1);
		}
		while (slotLevels.Count > count)
		{
			slotLevels.RemoveAt(slotLevels.Count - 1);
		}
	}

	private void Sanitize()
	{
		EnsureLists();
		HashSet<string> usedSkills = new HashSet<string>();
		for (int i = 0; i < TotalSlotCount; i++)
		{
			AbilityDef ability = GetSkillAt(i);
			DotaSkillExtension extension = DotaSkillExtension.For(ability);
			bool validKind = extension != null && extension.kind == SlotKindAt(i);
			if (ability == null
				|| !validKind
				|| !usedSkills.Add(ability.defName))
			{
				slotAbilityDefNames[i] = null;
				slotLevels[i] = 0;
				continue;
			}
			slotLevels[i] = Mathf.Clamp(
				slotLevels[i],
				0,
				extension.ResolvedMaxLevel);
		}
		for (int i = 0; i < TotalSlotCount; i++)
		{
			DotaSkillExtension extension = DotaSkillExtension.For(GetSkillAt(i));
			if (extension?.kind != DotaAbilityKind.Derived
				|| extension.parentAbilityDefs.NullOrEmpty()
				|| extension.parentAbilityDefs.Any(parent => usedSkills.Contains(parent.defName)))
			{
				continue;
			}
			slotAbilityDefNames[i] = null;
			slotLevels[i] = 0;
		}

		HashSet<string> selectedNodes = new HashSet<string>();
		HashSet<string> selectedTierSides = new HashSet<string>();
		talentNodeDefNames.RemoveAll(defName =>
		{
			DotaTalentNodeDef node =
				DefDatabase<DotaTalentNodeDef>.GetNamedSilentFail(defName);
			string tierSide = node == null
				? null
				: node.tier + ":" + node.side;
			return !DotaTalentUtility.IsValidSelection(node)
				|| !selectedNodes.Add(defName)
				|| !selectedTierSides.Add(tierSide);
		});
		int heroLevel = DotaHeroUtil.GetHeroComp(Pawn)?.HeroLevel ?? 1;
		HashSet<int> seenTalentTiers = new HashSet<int>();
		talentNodeDefNames.RemoveAll(defName =>
		{
			DotaTalentNodeDef node =
				DefDatabase<DotaTalentNodeDef>.GetNamedSilentFail(defName);
			return node != null
				&& !seenTalentTiers.Add(node.tier)
				&& heroLevel < Props.secondaryTalentUnlockLevel;
		});

		heroSkillDefNames.RemoveAll(defName =>
		{
			AbilityDef ability = DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
			return !DotaSkillPoolUtility.IsDotaSkill(ability);
		});
		heroSkillDefNames = heroSkillDefNames.Distinct().ToList();
		attributePointsSpent = Mathf.Clamp(
			attributePointsSpent,
			0,
			Mathf.Max(0, Props.attributePointCapacity));
		earnedSkillPoints = Mathf.Clamp(
			earnedSkillPoints,
			0,
			Mathf.Max(0, Props.skillPointCap));
		ClampPointsAndSpending();
	}

	private void ClampPointsAndSpending()
	{
		EnsureLists();
		earnedSkillPoints = Mathf.Clamp(
			earnedSkillPoints,
			0,
			Mathf.Max(0, Props.skillPointCap));
		while (SpentSkillPoints > earnedSkillPoints && attributePointsSpent > 0)
		{
			attributePointsSpent--;
		}
		for (int i = TotalSlotCount - 1;
			i >= 0 && SpentSkillPoints > earnedSkillPoints;
			i--)
		{
			while (slotLevels[i] > 0 && SpentSkillPoints > earnedSkillPoints)
			{
				slotLevels[i]--;
			}
		}
	}

	private void NotifyStatsChanged()
	{
		if (Pawn?.health != null && parent != null)
		{
			Pawn.health.Notify_HediffChanged(parent);
		}
	}

	public override void CompPostMake()
	{
		base.CompPostMake();
		EnsureLists();
		SyncProgression();
	}

	public override void CompExposeData()
	{
		base.CompExposeData();
		Scribe_Collections.Look(
			ref slotAbilityDefNames,
			"slotAbilityDefNames",
			LookMode.Value);
		Scribe_Collections.Look(
			ref slotLevels,
			"slotLevels",
			LookMode.Value);
		Scribe_Collections.Look(
			ref talentNodeDefNames,
			"talentNodeDefNames",
			LookMode.Value);
		Scribe_Collections.Look(
			ref heroSkillDefNames,
			"heroSkillDefNames",
			LookMode.Value);
		Scribe_Values.Look(
			ref earnedSkillPoints,
			"earnedSkillPoints",
			0);
		Scribe_Values.Look(
			ref attributePointsSpent,
			"attributePointsSpent",
			0);
		Scribe_Values.Look(
			ref layoutLocked,
			"skillsLayoutLocked",
			false);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			EnsureLists();
			Sanitize();
			SyncProgression();
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmos()
	{
		for (int i = 0; i < TotalSlotCount; i++)
		{
			AbilityDef ability = GetSkillAt(i);
			if (ability != null && GetSkillLevelAt(i) > 0)
			{
				yield return new Gizmo_DotaSkill(this, i);
			}
		}
	}
}

/// <summary>
/// A detached copy of all values that can be edited on the skill page.
/// Closing without confirmation simply drops this object.
/// </summary>
public class DotaSkillEditState
{
	private readonly HediffComp_DotaSkills owner;
	private int heroLevel;

	public readonly List<AbilityDef> SlotSkills;
	public readonly List<int> SlotLevels;
	public readonly List<DotaTalentNodeDef> Talents;

	public int EarnedSkillPoints { get; private set; }
	public int AttributePointsSpent { get; private set; }
	public bool LayoutLocked { get; }

	public DotaSkillEditState(HediffComp_DotaSkills owner)
	{
		this.owner = owner;
		heroLevel = DotaHeroUtil.GetHeroComp(owner.Pawn)?.HeroLevel ?? 1;
		EarnedSkillPoints = owner.EarnedSkillPoints;
		AttributePointsSpent = owner.AttributePointsSpent;
		LayoutLocked = owner.LayoutLocked;
		SlotSkills = new List<AbilityDef>();
		SlotLevels = new List<int>();
		for (int i = 0; i < owner.TotalSlotCount; i++)
		{
			SlotSkills.Add(owner.GetSkillAt(i));
			SlotLevels.Add(owner.GetSkillLevelAt(i));
		}
		Talents = new List<DotaTalentNodeDef>(owner.SelectedTalents());
	}

	private DotaSkillEditState(
		HediffComp_DotaSkills owner,
		int heroLevel,
		int earnedSkillPoints,
		int attributePointsSpent,
		bool layoutLocked,
		IEnumerable<AbilityDef> slotSkills,
		IEnumerable<int> slotLevels,
		IEnumerable<DotaTalentNodeDef> talents)
	{
		this.owner = owner;
		this.heroLevel = heroLevel;
		EarnedSkillPoints = earnedSkillPoints;
		AttributePointsSpent = attributePointsSpent;
		LayoutLocked = layoutLocked;
		SlotSkills = slotSkills.ToList();
		SlotLevels = slotLevels.ToList();
		Talents = talents.ToList();
	}

	public HediffCompProperties_DotaSkills Props => owner.Props;

	public int TotalSlotCount => owner.TotalSlotCount;

	public int HeroLevel => heroLevel;

	public bool IsSlotEditable(int slotIndex)
	{
		return !LayoutLocked || owner.SlotKindAt(slotIndex) == DotaAbilityKind.Derived;
	}

	public int SpentSkillPoints
	{
		get
		{
			int spent = AttributePointsSpent * Props.AttributePointCost;
			for (int i = 0; i < SlotSkills.Count; i++)
			{
				DotaSkillExtension extension =
					DotaSkillExtension.For(SlotSkills[i]);
				if (extension != null)
				{
					spent += Mathf.Max(0, SlotLevels[i])
						* Mathf.Max(0, extension.pointCostPerLevel);
				}
			}
			return spent;
		}
	}

	public int AvailableSkillPoints =>
		Mathf.Max(0, EarnedSkillPoints - SpentSkillPoints);

	public int EarnedTalentPoints =>
		Props.TalentPointLevels.Distinct().Count(level => level <= heroLevel);

	public int AvailableTalentPoints =>
		Mathf.Max(0, EarnedTalentPoints - Talents.Count);

	public float AttributePointBonus =>
		AttributePointsSpent * Mathf.Max(0f, Props.attributeBonusPerPoint);

	public float TalentBonus =>
		Talents.Sum(node => Mathf.Max(0f, node.allAttributeBonus));

	public float AllAttributeBonus => AttributePointBonus + TalentBonus;

	public DotaSkillEditState Clone()
	{
		return new DotaSkillEditState(
			owner,
			heroLevel,
			EarnedSkillPoints,
			AttributePointsSpent,
			LayoutLocked,
			SlotSkills,
			SlotLevels,
			Talents);
	}

	public void RefreshProgression()
	{
		heroLevel = DotaHeroUtil.GetHeroComp(owner.Pawn)?.HeroLevel ?? 1;
		EarnedSkillPoints = owner.EarnedSkillPoints;
	}

	public bool CanAssignSkill(
		int slotIndex,
		AbilityDef ability,
		out string reason)
	{
		reason = null;
		if (!IsSlotEditable(slotIndex))
		{
			reason = "主技能与奥义配置已经锁定。";
			return false;
		}
		DotaSkillExtension extension = DotaSkillExtension.For(ability);
		if (extension == null || extension.kind != owner.SlotKindAt(slotIndex))
		{
			reason = "该技能不能放入这个槽位。";
			return false;
		}
		if (SlotSkills[slotIndex] == ability)
		{
			reason = "该技能已经在这个槽位。";
			return false;
		}
		for (int i = 0; i < SlotSkills.Count; i++)
		{
			if (i != slotIndex && SlotSkills[i] == ability)
			{
				reason = "同一技能不能重复配置。";
				return false;
			}
		}
		if (extension.kind == DotaAbilityKind.Derived
			&& extension.parentAbilityDefs != null
			&& extension.parentAbilityDefs.Count > 0
			&& !extension.parentAbilityDefs.Any(SlotSkills.Contains))
		{
			reason = "需要先配置对应的前置技能。";
			return false;
		}
		return true;
	}

	public bool AssignSkill(int slotIndex, AbilityDef ability)
	{
		if (!CanAssignSkill(slotIndex, ability, out _))
		{
			return false;
		}
		SlotSkills[slotIndex] = ability;
		SlotLevels[slotIndex] = 0;
		PruneInvalidDerivedSlots();
		return true;
	}

	public bool ClearSkill(int slotIndex)
	{
		if (!IsSlotEditable(slotIndex)
			|| slotIndex < 0
			|| slotIndex >= SlotSkills.Count)
		{
			return false;
		}
		SlotSkills[slotIndex] = null;
		SlotLevels[slotIndex] = 0;
		PruneInvalidDerivedSlots();
		return true;
	}

	private void PruneInvalidDerivedSlots()
	{
		for (int i = 0; i < TotalSlotCount; i++)
		{
			if (owner.SlotKindAt(i) != DotaAbilityKind.Derived)
			{
				continue;
			}
			DotaSkillExtension extension = DotaSkillExtension.For(SlotSkills[i]);
			if (extension == null
				|| extension.parentAbilityDefs.NullOrEmpty()
				|| extension.parentAbilityDefs.Any(SlotSkills.Contains))
			{
				continue;
			}
			SlotSkills[i] = null;
			SlotLevels[i] = 0;
		}
	}

	public bool CanUpgradeSkill(int slotIndex)
	{
		AbilityDef ability = SlotSkills[slotIndex];
		DotaSkillExtension extension = DotaSkillExtension.For(ability);
		return extension != null
			&& SlotLevels[slotIndex] < extension.ResolvedMaxLevel
			&& heroLevel >= extension.RequiredHeroLevelForLevel(SlotLevels[slotIndex] + 1)
			&& AvailableSkillPoints >= Mathf.Max(0, extension.pointCostPerLevel);
	}

	public string UpgradeBlockedReason(int slotIndex)
	{
		AbilityDef ability = SlotSkills[slotIndex];
		DotaSkillExtension extension = DotaSkillExtension.For(ability);
		if (extension == null)
		{
			return "没有可升级的技能。";
		}
		if (SlotLevels[slotIndex] >= extension.ResolvedMaxLevel)
		{
			return "技能已达到最高等级。";
		}
		int nextLevel = SlotLevels[slotIndex] + 1;
		int requiredLevel = extension.RequiredHeroLevelForLevel(nextLevel);
		if (heroLevel < requiredLevel)
		{
			return "英雄达到 " + requiredLevel + " 级后才能升级到 " + nextLevel + " 级。";
		}
		return "没有可用技能点。";
	}

	public bool UpgradeSkill(int slotIndex)
	{
		if (!CanUpgradeSkill(slotIndex))
		{
			return false;
		}
		SlotLevels[slotIndex]++;
		return true;
	}

	public bool CanSpendAttributePoint()
	{
		return AttributePointsSpent < Mathf.Max(0, Props.attributePointCapacity)
			&& AvailableSkillPoints >= Props.AttributePointCost;
	}

	public bool SpendAttributePoint()
	{
		if (!CanSpendAttributePoint())
		{
			return false;
		}
		AttributePointsSpent++;
		return true;
	}

	public bool CanSelectTalent(
		DotaTalentNodeDef node,
		out string reason)
	{
		reason = null;
		if (!DotaTalentUtility.IsValidSelection(node))
		{
			reason = "无效天赋。";
			return false;
		}
		if (Talents.Contains(node))
		{
			reason = "该天赋已经选择。";
			return false;
		}
		if (heroLevel < node.requiredHeroLevel)
		{
			reason = "达到 " + node.requiredHeroLevel + " 级后才能选择这一层天赋。";
			return false;
		}
		if (AvailableTalentPoints <= 0)
		{
			reason = "没有可用天赋点。";
			return false;
		}
		List<DotaTalentNodeDef> sameTier =
			Talents.Where(selected => selected.tier == node.tier).ToList();
		if (sameTier.Count >= 2)
		{
			reason = "这一层天赋已经全部选择。";
			return false;
		}
		if (sameTier.Count == 1)
		{
			if (sameTier[0].side == node.side)
			{
				reason = "同一侧天赋不能重复选择。";
				return false;
			}
			if (heroLevel < Props.secondaryTalentUnlockLevel)
			{
				reason = "达到 " + Props.secondaryTalentUnlockLevel
					+ " 级后才能选择另一侧天赋。";
				return false;
			}
		}
		return true;
	}

	public bool SelectTalent(DotaTalentNodeDef node)
	{
		if (!CanSelectTalent(node, out _))
		{
			return false;
		}
		Talents.Add(node);
		return true;
	}
}

public class HediffCompProperties_DotaSkills : HediffCompProperties
{
	public int skillPointCap;

	public int skillPointsPerLevelUp = 1;

	public List<int> talentPointLevels = new List<int>();

	public int secondaryTalentUnlockLevel = 27;

	public int attributePointCapacity = 7;

	public int attributePointCost = 1;

	public float attributeBonusPerPoint = 2f;

	public int normalSlotCount = 3;

	public int derivedSlotCount = 2;

	public int ultimateSlotCount = 1;

	public List<int> TalentPointLevels =>
		talentPointLevels ?? new List<int>();

	public int AttributePointCost => Mathf.Max(0, attributePointCost);

	public HediffCompProperties_DotaSkills()
	{
		compClass = typeof(HediffComp_DotaSkills);
	}
}
