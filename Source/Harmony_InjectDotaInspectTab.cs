using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace RimDota;

/// <summary>为所有 Humanlike 种族自动注入 Dota ITab，非英雄时页签自动隐藏。</summary>
[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.ResolveReferences))]
public static class Harmony_InjectDotaInspectTab
{
	public static void Prefix(ThingDef __instance)
	{
		if (__instance?.race == null || !__instance.race.Humanlike)
		{
			return;
		}

		__instance.inspectorTabs ??= new List<Type>();
		if (!__instance.inspectorTabs.Contains(typeof(ITab_Pawn_Dota)))
		{
			__instance.inspectorTabs.Add(typeof(ITab_Pawn_Dota));
		}
	}
}
