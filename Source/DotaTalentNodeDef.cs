using UnityEngine;
using Verse;

namespace RimDota;

public class DotaTalentNodeDef : Def
{
	public int tier;

	public DotaTalentSide side;

	public int requiredHeroLevel;

	public float allAttributeBonus;

	public Color color = new Color(0.66f, 0.70f, 0.76f, 1f);
}

public static class DotaTalentUtility
{
	public static bool IsValidSelection(DotaTalentNodeDef node)
	{
		return node != null
			&& node.tier >= 1
			&& node.tier <= 4
			&& node.requiredHeroLevel > 0
			&& node.allAttributeBonus > 0f;
	}
}
