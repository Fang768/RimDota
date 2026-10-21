using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// 示例 Harmony 补丁：对 Game.UpdatePlay 添加一个后置钩子（Postfix）。
/// 该钩子在进入游玩模式后每帧执行一次，这里用静态标志只打印一次日志，
/// 用于验证补丁确实生效。可安全删除或替换为你自己的补丁。
/// </summary>
[HarmonyPatch(typeof(Game), nameof(Game.UpdatePlay))]
internal static class Patch_Game_UpdatePlay
{
	private static bool logged;

	private static void Postfix()
	{
		if (logged)
			return;
		logged = true;
		Log.Message("RimDota: Harmony 补丁已生效（此消息只打印一次）。");
	}
}

/// <summary>
/// Dota 装备门控：只有带 DotaHero 状态的小人才能穿戴 DotaGear 层的装备。
/// </summary>
[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
internal static class Patch_ApparelTracker_Wear
{
	private static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
	{
		if (newApparel?.def != null && DotaEquipmentUtil.IsDotaEquipment(newApparel.def))
		{
			if (!DotaHeroUtil.IsHero(__instance.pawn))
			{
				Messages.Message("只有 Dota 英雄才能装备这件物品。", MessageTypeDefOf.RejectInput);
			}
			else
			{
				Messages.Message("请使用 Dota 装备栏装备这件物品。", MessageTypeDefOf.RejectInput);
			}
			return false;
		}
		return true;
	}
}

/// <summary>
/// Dota 装备使用自定义六格背包，隐藏原版“穿戴/强制穿戴”右键选项，
/// 避免玩家误点后被拒绝。
/// </summary>
[HarmonyPatch(typeof(FloatMenuOptionProvider_Wear), "GetSingleOptionFor")]
internal static class Patch_FloatMenuOptionProvider_Wear
{
	private static void Postfix(Thing clickedThing, ref FloatMenuOption __result)
	{
		if (__result != null && clickedThing != null && DotaEquipmentUtil.IsDotaEquipment(clickedThing.def))
		{
			__result = null;
		}
	}
}

/// <summary>
/// 英雄生命上限：按力量提升部位最大血量。
/// </summary>
[HarmonyPatch(typeof(BodyPartDef), nameof(BodyPartDef.GetMaxHealth))]
internal static class Patch_BodyPartDef_GetMaxHealth
{
	private static void Postfix(Pawn pawn, ref float __result)
	{
		if (pawn == null)
		{
			return;
		}
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(pawn);
		if (comp != null)
		{
			__result = Mathf.CeilToInt(__result * comp.HealthScaleMult);
		}
	}
}
