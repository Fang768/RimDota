using HarmonyLib;
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
