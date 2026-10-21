using HarmonyLib;
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
