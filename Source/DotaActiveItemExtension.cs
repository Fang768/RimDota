using Verse;

namespace RimDota;

public enum DotaActiveUseMode
{
	Self
}

/// <summary>主动 Dota 装备的使用参数。</summary>
public class DotaActiveItemExtension : DefModExtension
{
	public float manaCost;

	public int cooldownTicks;

	public string cooldownGroup;

	public DotaActiveUseMode useMode = DotaActiveUseMode.Self;

	public HediffDef effectHediff;
}
