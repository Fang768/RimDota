using Verse;

namespace RimDota;

/// <summary>
/// 每件 Dota 装备自己的锁定状态。锁定后不参与六格装备栏的自动合成。
/// </summary>
public class Comp_DotaEquipmentLock : ThingComp
{
	private bool locked;

	public bool Locked
	{
		get => locked;
		set => locked = value;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref locked, "locked", false);
	}
}

public class CompProperties_DotaEquipmentLock : CompProperties
{
	public CompProperties_DotaEquipmentLock()
	{
		compClass = typeof(Comp_DotaEquipmentLock);
	}
}

public static class DotaEquipmentLockUtility
{
	public static Comp_DotaEquipmentLock GetLockComp(Thing item)
	{
		return (item as ThingWithComps)?.GetComp<Comp_DotaEquipmentLock>();
	}

	public static bool IsLocked(Thing item)
	{
		return GetLockComp(item)?.Locked ?? false;
	}
}
