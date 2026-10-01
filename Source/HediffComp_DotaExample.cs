using Reloader;
using Verse;

namespace RimDota;

/// <summary>
/// 示例 HediffComp（状态效果组件），与 <see cref="DotaDefOf.Dota_ExampleBuff"/> 绑定。
/// Defs/HediffDefs/Hediffs.xml 通过 Class 属性把本组件接到 def 上，
/// 演示了「XML 定义 ↔ C# 代码」的完整接线方式。可安全删除或替换。
/// </summary>
public class HediffComp_DotaExample : HediffComp
{
	public HediffCompProperties_DotaExample Props => (HediffCompProperties_DotaExample)props;

	private int ticksSinceApplied;

	[ReloadMethod] // 标记此方法可被 0Reloader 热重载：改这里重新编译即可，无需重启游戏
	public override void CompPostTick(ref float severityAdjustment)
	{
		base.CompPostTick(ref severityAdjustment);

		// 示例逻辑：buff 生效 60 tick 后打印一次日志。
		ticksSinceApplied++;
		if (ticksSinceApplied == 60)
		{
			Log.Message($"RimDota: 示例 buff 正在作用于 {Pawn.LabelShort}。");
		}
	}
}

public class HediffCompProperties_DotaExample : HediffCompProperties
{
	public HediffCompProperties_DotaExample()
	{
		// 在构造函数里指定对应的组件类，这样 XML 里只需写 Class="…" 即可。
		compClass = typeof(HediffComp_DotaExample);
	}
}
