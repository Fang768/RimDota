using Verse;

namespace RimDota;

/// <summary>
/// Dota 装备工具：判断一个物品是否属于 Dota 装备（ThingDef 上的 DotaEquipmentExtension 标记）。
/// </summary>
public static class DotaEquipmentUtil
{
	public static bool IsDotaEquipment(ThingDef def)
	{
		return def != null && def.HasModExtension<DotaEquipmentExtension>();
	}
}
