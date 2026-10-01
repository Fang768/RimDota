using HarmonyLib;
using Verse;

namespace RimDota;

/// <summary>
/// Mod 入口点。RimWorld 在启用本 Mod 时会实例化这个类，
/// 因此它同时承担了初始化（注册 Harmony 补丁）的职责。
/// </summary>
public class RimDotaMod : Mod
{
	/// <summary>
	/// Harmony 的唯一标识，约定为 "author.modname"。
	/// 请保持与 About.xml 中的 packageId 一致。
	/// </summary>
	public const string HarmonyId = "magicconch.rimdota";

	private readonly Harmony harmony;

	public RimDotaMod(ModContentPack content) : base(content)
	{
		// 应用本程序集中所有标记了 [HarmonyPatch] 的补丁。
		harmony = new Harmony(HarmonyId);
		harmony.PatchAll();

		Log.Message("RimDota: 已加载。");
	}
}
