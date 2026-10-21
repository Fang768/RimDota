using HarmonyLib;
using UnityEngine;
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

	private static DotaModSettings settings;

	public static DotaModSettings Settings
	{
		get => settings ??= new DotaModSettings();
		private set => settings = value;
	}

	public RimDotaMod(ModContentPack content) : base(content)
	{
		Settings = GetSettings<DotaModSettings>();

		// 应用本程序集中所有标记了 [HarmonyPatch] 的补丁。
		harmony = new Harmony(HarmonyId);
		harmony.PatchAll();

		Log.Message("RimDota: 已加载。");
	}

	public override string SettingsCategory()
	{
		return "RimDota";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		Listing_Standard listing = new Listing_Standard();
		listing.Begin(inRect);

		Text.Font = GameFont.Medium;
		listing.Label("Dota 回复设置");
		Text.Font = GameFont.Small;
		listing.Gap(6f);

		listing.Label(
			"回复平衡系数：" + Settings.RegenBalanceScale.ToString("0.00"));
		float scale = listing.Slider(
			Settings.RegenBalanceScale,
			0f,
			5f);
		Settings.RegenBalanceScale = Mathf.Round(scale * 100f) / 100f;

		listing.Gap(4f);
		listing.Label(
			"1.00 = 完全使用 Dota 2 原始回复速度。\n"
			+ "所有生命回复和法力回复（属性与装备）都会受到该系数影响。\n"
			+ "RimWorld 一刻为 1/60 秒，一天为 60,000 tick（正常速度下 1,000 秒）。");

		listing.End();
		Text.Font = GameFont.Small;
	}
}
