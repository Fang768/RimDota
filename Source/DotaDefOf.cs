using RimWorld;
using Verse;

namespace RimDota;

/// <summary>
/// DefOf：把 XML 里定义的 def 通过反射绑定到强类型字段，
/// 方便在 C# 代码里引用。字段名必须与 defName 完全一致。
/// </summary>
[DefOf]
public static class DotaDefOf
{
	public static HediffDef Dota_ExampleBuff;
}
