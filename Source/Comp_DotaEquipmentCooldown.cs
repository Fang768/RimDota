using Verse;

namespace RimDota;

/// <summary>装备实例冷却。相同 cooldownGroup 的装备会同步结束时间。</summary>
public class Comp_DotaEquipmentCooldown : ThingComp
{
	private int cooldownEndTick;

	public int RemainingTicks =>
		cooldownEndTick > Find.TickManager.TicksGame
			? cooldownEndTick - Find.TickManager.TicksGame
			: 0;

	public int CooldownEndTick => cooldownEndTick;

	public bool IsCoolingDown => RemainingTicks > 0;

	public void StartCooldown(int durationTicks)
	{
		cooldownEndTick = Find.TickManager.TicksGame + durationTicks;
	}

	public void SyncCooldownEnd(int endTick)
	{
		if (endTick > cooldownEndTick)
		{
			cooldownEndTick = endTick;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref cooldownEndTick, "cooldownEndTick", 0);
	}
}

public class CompProperties_DotaEquipmentCooldown : CompProperties
{
	public CompProperties_DotaEquipmentCooldown()
	{
		compClass = typeof(Comp_DotaEquipmentCooldown);
	}
}

public static class DotaActiveItemUtility
{
	public static DotaActiveItemExtension ActiveExtension(ThingDef def)
	{
		return def?.GetModExtension<DotaActiveItemExtension>();
	}

	public static Comp_DotaEquipmentCooldown CooldownComp(Thing item)
	{
		return (item as ThingWithComps)?.GetComp<Comp_DotaEquipmentCooldown>();
	}

	public static string CooldownGroup(ThingDef def, DotaActiveItemExtension extension)
	{
		return extension?.cooldownGroup.NullOrEmpty() == false
			? extension.cooldownGroup
			: def?.defName;
	}
}
