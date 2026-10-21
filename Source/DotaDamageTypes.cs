using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimDota;

/// <summary>Dota 伤害元素，决定吃哪一层减伤。</summary>
public enum DotaDamageElement
{
	Physical,
	Magic,
	True,
	Unprocessed
}

/// <summary>Dota 伤害来源，只用于吸血判定。</summary>
public enum DotaDamageSource
{
	None,
	Attack,
	Skill,
	Splash
}

/// <summary>一次 TakeDamage 的原始分类上下文。</summary>
public struct DotaDamageContext
{
	public DamageDef OriginalDef;
	public DotaDamageElement Element;
	public DotaDamageSource Source;
}

/// <summary>
/// Dota 伤害分类与上下文：
/// - 元素分类用于物理/法术减伤；
/// - 来源分类用于攻击/技能吸血；
/// - 线程静态栈保存 TakeDamage 前的原始 DamageDef，避免原版 Sharp→Blunt 改写丢失来源。
/// </summary>
public static class DotaDamageUtil
{
	[ThreadStatic]
	private static Stack<DotaDamageContext> contextStack;

	[ThreadStatic]
	private static DotaDamageSource pendingSource;

	private static readonly HashSet<string> PhysicalDefs = new HashSet<string>
	{
		"Beam",
		"BeamBypassShields",
		"Burn",
		"Digested"
	};

	private static readonly HashSet<string> MagicDefs = new HashSet<string>
	{
		"ElectricalBurn",
		"Flame",
		"NociosphereVaporize",
		"Vaporize",
		"Psychic"
	};

	private static readonly HashSet<string> SplashDefs = new HashSet<string>
	{
		"Vaporize",
		"NociosphereVaporize"
	};

	public static DotaDamageElement DetermineElement(DamageDef def)
	{
		if (def == null)
		{
			return DotaDamageElement.Unprocessed;
		}

		if (MagicDefs.Contains(def.defName))
		{
			return DotaDamageElement.Magic;
		}

		if (PhysicalDefs.Contains(def.defName))
		{
			return DotaDamageElement.Physical;
		}

		string armorCategory = def.armorCategory?.defName;
		if (armorCategory == "Sharp" || armorCategory == "Blunt")
		{
			return DotaDamageElement.Physical;
		}

		return DotaDamageElement.Unprocessed;
	}

	public static DotaDamageSource DetermineSource(DamageInfo dinfo, DotaDamageSource pending)
	{
		if (pending != DotaDamageSource.None)
		{
			return pending;
		}

		if (dinfo.Def?.isExplosive == true || SplashDefs.Contains(dinfo.Def?.defName ?? string.Empty))
		{
			return DotaDamageSource.Splash;
		}

		if (dinfo.Weapon != null || dinfo.Tool != null)
		{
			return DotaDamageSource.Attack;
		}

		return DotaDamageSource.None;
	}

	public static void PushContext(DamageDef def, DotaDamageSource source)
	{
		contextStack ??= new Stack<DotaDamageContext>();
		contextStack.Push(new DotaDamageContext
		{
			OriginalDef = def,
			Element = DetermineElement(def),
			Source = source
		});
	}

	public static void PopContext()
	{
		if (contextStack != null && contextStack.Count > 0)
		{
			contextStack.Pop();
		}
	}

	public static bool TryPeekContext(out DotaDamageContext context)
	{
		if (contextStack != null && contextStack.Count > 0)
		{
			context = contextStack.Peek();
			return true;
		}

		context = default;
		return false;
	}

	public static void SetPendingSource(DotaDamageSource source)
	{
		pendingSource = source;
	}

	public static DotaDamageSource ConsumePendingSource()
	{
		DotaDamageSource source = pendingSource;
		pendingSource = DotaDamageSource.None;
		return source;
	}

	/// <summary>
	/// 给自定义技能/DOT 使用的统一入口：设置来源标签后调用 TakeDamage，
	/// TakeDamage 前缀会把来源写进伤害上下文。
	/// 注意：自定义 Dota2 DamageDef 默认应设置 externalViolence=true，
	/// 以便 IncomingDamageFactor 的通用减伤生效；例外需显式标注。
	/// </summary>
	public static DamageWorker.DamageResult DealDamage(Thing target, DamageInfo dinfo, DotaDamageSource source)
	{
		if (target == null)
		{
			return new DamageWorker.DamageResult();
		}

		SetPendingSource(source);
		try
		{
			return target.TakeDamage(dinfo);
		}
		finally
		{
			pendingSource = DotaDamageSource.None;
		}
	}
}
