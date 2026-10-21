using Verse;

namespace RimDota;

/// <summary>
/// RimDota 的持久化 Mod 设置。
/// </summary>
public class DotaModSettings : ModSettings
{
	public const float DefaultHealthRegenBalanceScale = 0.2f;

	private float healthRegenBalanceScale = DefaultHealthRegenBalanceScale;

	/// <summary>
	/// Dota 生命回复的平衡系数，仅影响实际结算，不影响 UI 和装备描述。
	/// </summary>
	public float HealthRegenBalanceScale
	{
		get => healthRegenBalanceScale;
		set => healthRegenBalanceScale = value < 0f ? 0f : (value > 5f ? 5f : value);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(
			ref healthRegenBalanceScale,
			"healthRegenBalanceScale",
			DefaultHealthRegenBalanceScale);
		HealthRegenBalanceScale = healthRegenBalanceScale;
	}
}
