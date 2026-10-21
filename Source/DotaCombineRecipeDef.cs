using System.Collections.Generic;
using Verse;

namespace RimDota;

/// <summary>
/// 六格 Dota 装备栏内使用的自动合成配方。
/// </summary>
public class DotaCombineRecipeDef : Def
{
	public ThingDef result;

	public List<ThingDefCountClass> ingredients = new List<ThingDefCountClass>();
}
