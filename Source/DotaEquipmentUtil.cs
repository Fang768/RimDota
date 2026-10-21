using Verse;

namespace RimDota;

/// <summary>
/// Dota 装备工具：判断一个物品是否属于 Dota 专属装备（使用 DotaGear 衣物层）。
/// </summary>
public static class DotaEquipmentUtil
{
	public static bool IsDotaEquipment(ThingDef def)
	{
		return def?.apparel?.layers?.Contains(DotaDefOf.DotaGear) ?? false;
	}
}
