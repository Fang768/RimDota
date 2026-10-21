using Verse;

namespace RimDota;

/// <summary>
/// RimDota 的持久化 Mod 设置。
/// </summary>
public class DotaModSettings : ModSettings
{
	public const float DefaultRegenBalanceScale = 1f;

	private float regenBalanceScale = DefaultRegenBalanceScale;

	/// <summary>
	/// Dota 每秒回复的平衡系数。1 表示使用 Dota 2 原始速度。
	/// </summary>
	public float RegenBalanceScale
	{
		get => regenBalanceScale;
		set => regenBalanceScale = value < 0f ? 0f : (value > 5f ? 5f : value);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(
			ref regenBalanceScale,
			"regenBalanceScale",
			DefaultRegenBalanceScale);
		RegenBalanceScale = regenBalanceScale;
	}
}
