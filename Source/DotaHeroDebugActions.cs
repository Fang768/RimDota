using LudeonTK;
using Verse;

namespace RimDota;

/// <summary>
/// 开发用调试入口：开启开发者模式后，右键小人可在调试菜单中“设为/移除 Dota 英雄”。
/// 触发时会打印日志，便于通过 Player.log 核对。
/// </summary>
public static class DotaHeroDebugActions
{
	[DebugAction("Pawns", "设为 Dota 英雄（选中）", allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void MakeDotaHero()
	{
		Pawn p = Find.Selector.SingleSelectedThing as Pawn;
		if (p == null)
		{
			Log.Message("RimDota: 请先选中一个小人，再使用该调试操作。");
			return;
		}
		if (!p.health.hediffSet.HasHediff(DotaDefOf.DotaHero))
		{
			p.health.AddHediff(DotaDefOf.DotaHero);
			Log.Message($"RimDota: {p.LabelShort} 已设为 Dota 英雄。");
		}
	}

	[DebugAction("Pawns", "移除 Dota 英雄（选中）", allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void RemoveDotaHero()
	{
		Pawn p = Find.Selector.SingleSelectedThing as Pawn;
		if (p == null)
		{
			Log.Message("RimDota: 请先选中一个小人，再使用该调试操作。");
			return;
		}
		Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(DotaDefOf.DotaHero);
		if (hediff != null)
		{
			p.health.RemoveHediff(hediff);
			Log.Message($"RimDota: {p.LabelShort} 已移除 Dota 英雄。");
		}
	}

	[DebugAction("Pawns", "查看 Dota 英雄属性", allowedGameStates = AllowedGameStates.PlayingOnMap)]
	public static void ShowHeroInfo()
	{
		Pawn p = Find.Selector.SingleSelectedThing as Pawn;
		if (p == null)
		{
			Log.Message("RimDota: 请先选中一个小人。");
			return;
		}
		HediffComp_DotaHero comp = DotaHeroUtil.GetHeroComp(p);
		if (comp == null)
		{
			Log.Message("RimDota: 该小人不是 Dota 英雄。");
			return;
		}
		float totalMaxHp = 0f;
		foreach (BodyPartRecord part in p.health.hediffSet.GetNotMissingParts())
		{
			totalMaxHp += part.def.GetMaxHealth(p);
		}
		Log.Message($"RimDota: {p.LabelShort} 力量={comp.Strength:F0} 敏捷={comp.Agility:F0} 智力={comp.Intelligence:F0} 生命倍率={comp.HealthScaleMult:F2} 总最大生命={totalMaxHp:F0} 回血={comp.HealthRegenPerSecond:F2}/秒 回蓝={comp.ManaRegenPerSecond:F2}/秒 回复系数={RimDotaMod.Settings.RegenBalanceScale:F2} 护甲={comp.ArmorPoints:F2} 物理减伤={(1f - comp.PhysicalDamageMultiplier) * 100f:F1}% 魔抗={comp.MagicResistance * 100f:F1}% 法力上限={comp.MaxMana:F0} 攻击吸血={comp.AttackLifesteal * 100f:F0}% 技能吸血={comp.SpellLifesteal * 100f:F0}%");
	}
}
