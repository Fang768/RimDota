using UnityEngine;

namespace RimDota;

/// <summary>Dota 2 当前 1-30 级累计经验表。</summary>
public static class DotaHeroLeveling
{
	public const int MaxLevel = 30;

	private static readonly float[] TotalExperienceByLevel =
	{
		0f, 240f, 640f, 1160f, 1760f, 2440f, 3200f, 4000f, 4900f, 5900f,
		7000f, 8200f, 9500f, 10900f, 12400f, 14000f, 15700f, 17500f,
		19400f, 21400f, 23600f, 26000f, 28600f, 31400f, 34400f, 38400f,
		43400f, 49400f, 56400f, 63900f
	};

	public static float TotalExperienceForLevel(int level)
	{
		return TotalExperienceByLevel[Mathf.Clamp(level, 1, MaxLevel) - 1];
	}
}
