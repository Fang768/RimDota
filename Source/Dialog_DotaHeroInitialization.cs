using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>殖民地小人获得 Dota 英雄身份后的初始属性分配窗口。</summary>
public class Dialog_DotaHeroInitialization : Window
{
	private readonly Pawn pawn;
	private readonly HediffComp_DotaHero heroComp;
	private readonly DotaHeroSetupBudget budget;

	private DotaPrimaryAttribute primaryAttribute = DotaPrimaryAttribute.Universal;
	private int strength;
	private int agility;
	private int intelligence;
	private float strengthGrowth;
	private float agilityGrowth;
	private float intelligenceGrowth;
	private string strengthBuffer;
	private string agilityBuffer;
	private string intelligenceBuffer;
	private string strengthGrowthBuffer;
	private string agilityGrowthBuffer;
	private string intelligenceGrowthBuffer;

	public override Vector2 InitialSize => new Vector2(570f, 575f);

	public Dialog_DotaHeroInitialization(Pawn pawn, HediffComp_DotaHero heroComp)
	{
		this.pawn = pawn;
		this.heroComp = heroComp;
		budget = DotaHeroSetupUtility.CalculateBudget(pawn);
		primaryAttribute = heroComp.PrimaryAttribute;
		strengthBuffer = strength.ToString();
		agilityBuffer = agility.ToString();
		intelligenceBuffer = intelligence.ToString();
		strengthGrowthBuffer = strengthGrowth.ToString("0.0");
		agilityGrowthBuffer = agilityGrowth.ToString("0.0");
		intelligenceGrowthBuffer = intelligenceGrowth.ToString("0.0");

		doCloseX = true;
		doCloseButton = false;
		forcePause = true;
		absorbInputAroundWindow = true;
		closeOnClickedOutside = false;
		closeOnCancel = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (pawn == null || heroComp == null || !heroComp.AwaitingInitialSetup)
		{
			Close();
			return;
		}

		Listing_Standard listing = new Listing_Standard();
		listing.Begin(inRect.ContractedBy(18f));

		Text.Font = GameFont.Medium;
		listing.Label("Dota 英雄初始化");
		Text.Font = GameFont.Small;
		listing.GapLine();

		listing.Label(
			$"{pawn.LabelShortCap} 获得了 Dota 英雄身份。\n"
			+ $"原版技能等级总和：{budget.SkillLevelSum}（每 4 点额外 +1 初始属性点）\n"
			+ $"原版兴趣值总和：{budget.InterestValueSum}（每点额外 +0.5 成长点）");
		listing.GapLine();

		listing.Label("主属性");
		DrawPrimaryAttributeButtons(listing.GetRect(30f));
		listing.Gap(4f);

		int spentInitial = strength + agility + intelligence;
		int remainingInitial = budget.InitialPoints - spentInitial;
		listing.Label($"初始属性：剩余 {remainingInitial} / 总计 {budget.InitialPoints}");
		DrawIntegerAllocation(
			listing,
			"力量",
			ref strength,
			ref strengthBuffer,
			remainingInitial,
			DotaHeroSetupUtility.MaxInitialAttribute);
		remainingInitial = budget.InitialPoints - (strength + agility + intelligence);
		DrawIntegerAllocation(
			listing,
			"敏捷",
			ref agility,
			ref agilityBuffer,
			remainingInitial,
			DotaHeroSetupUtility.MaxInitialAttribute);
		remainingInitial = budget.InitialPoints - (strength + agility + intelligence);
		DrawIntegerAllocation(
			listing,
			"智力",
			ref intelligence,
			ref intelligenceBuffer,
			remainingInitial,
			DotaHeroSetupUtility.MaxInitialAttribute);

		listing.GapLine();
		float remainingGrowth = budget.GrowthPoints - TotalGrowth();
		listing.Label(
			$"属性成长：剩余 {remainingGrowth:0.0} / 总计 {budget.GrowthPoints:0.0}");
		DrawGrowthAllocation(
			listing,
			"力量成长",
			ref strengthGrowth,
			ref strengthGrowthBuffer,
			remainingGrowth);
		remainingGrowth = budget.GrowthPoints - TotalGrowth();
		DrawGrowthAllocation(
			listing,
			"敏捷成长",
			ref agilityGrowth,
			ref agilityGrowthBuffer,
			remainingGrowth);
		remainingGrowth = budget.GrowthPoints - TotalGrowth();
		DrawGrowthAllocation(
			listing,
			"智力成长",
			ref intelligenceGrowth,
			ref intelligenceGrowthBuffer,
			remainingGrowth);

		listing.Gap(8f);
		float normalizedStrengthGrowth = NormalizeGrowth(strengthGrowth);
		float normalizedAgilityGrowth = NormalizeGrowth(agilityGrowth);
		float normalizedIntelligenceGrowth = NormalizeGrowth(intelligenceGrowth);
		float spentGrowth = normalizedStrengthGrowth
			+ normalizedAgilityGrowth
			+ normalizedIntelligenceGrowth;
		remainingGrowth = budget.GrowthPoints - spentGrowth;
		bool growthWithinBudget = spentGrowth <= budget.GrowthPoints + 0.001f;
		bool growthFullyAllocated = Mathf.Abs(remainingGrowth) < 0.001f;
		bool growthAtAllCaps =
			normalizedStrengthGrowth >= 5f
			&& normalizedAgilityGrowth >= 5f
			&& normalizedIntelligenceGrowth >= 5f;
		bool initialAtAllCaps =
			strength >= DotaHeroSetupUtility.MaxInitialAttribute
			&& agility >= DotaHeroSetupUtility.MaxInitialAttribute
			&& intelligence >= DotaHeroSetupUtility.MaxInitialAttribute;
		bool canConfirm = (remainingInitial == 0 || initialAtAllCaps)
			&& growthWithinBudget
			&& (growthFullyAllocated || growthAtAllCaps);

		Rect buttonRow = listing.GetRect(36f);
		Rect cancelRect = new Rect(buttonRow.x, buttonRow.y, 150f, buttonRow.height);
		Rect confirmRect = new Rect(buttonRow.xMax - 190f, buttonRow.y, 190f, buttonRow.height);
		if (Widgets.ButtonText(cancelRect, "稍后分配"))
		{
			Close();
		}
		if (Widgets.ButtonText(
			confirmRect,
			"确认英雄属性",
			active: canConfirm)
			&& canConfirm)
		{
			heroComp.ApplyInitialSetup(
				primaryAttribute,
				strength,
				agility,
				intelligence,
				normalizedStrengthGrowth,
				normalizedAgilityGrowth,
				normalizedIntelligenceGrowth);
			Messages.Message(
				pawn.LabelShortCap + " 的 Dota 英雄属性已确定。",
				pawn,
				MessageTypeDefOf.PositiveEvent);
			Close();
		}

		Text.Font = GameFont.Small;
		listing.End();
	}

	private void DrawPrimaryAttributeButtons(Rect rect)
	{
		float gap = 6f;
		float width = (rect.width - gap * 3f) / 4f;
		DrawPrimaryAttributeButton(
			new Rect(rect.x, rect.y, width, rect.height),
			DotaPrimaryAttribute.Strength,
			"力量");
		DrawPrimaryAttributeButton(
			new Rect(rect.x + (width + gap), rect.y, width, rect.height),
			DotaPrimaryAttribute.Agility,
			"敏捷");
		DrawPrimaryAttributeButton(
			new Rect(rect.x + (width + gap) * 2f, rect.y, width, rect.height),
			DotaPrimaryAttribute.Intelligence,
			"智力");
		DrawPrimaryAttributeButton(
			new Rect(rect.x + (width + gap) * 3f, rect.y, width, rect.height),
			DotaPrimaryAttribute.Universal,
			"全才");
	}

	private void DrawPrimaryAttributeButton(
		Rect rect,
		DotaPrimaryAttribute attribute,
		string label)
	{
		string text = primaryAttribute == attribute ? "✓ " + label : label;
		if (Widgets.ButtonText(rect, text))
		{
			primaryAttribute = attribute;
		}
	}

	private static void DrawIntegerAllocation(
		Listing_Standard listing,
		string label,
		ref int value,
		ref string buffer,
		int remaining,
		int max)
	{
		Rect row = listing.GetRect(30f);
		Widgets.Label(row.LeftPart(0.38f), label);
		Rect controls = row.RightPart(0.58f);
		Rect minus = new Rect(controls.x, controls.y, 42f, controls.height);
		Rect plus = new Rect(controls.xMax - 42f, controls.y, 42f, controls.height);
		Rect field = new Rect(
			controls.center.x - 55f,
			controls.y,
			110f,
			controls.height);
		int allowedMax = Mathf.Clamp(value + remaining, 0, max);

		if (Widgets.ButtonText(minus, "−", active: value > 0))
		{
			value--;
			buffer = value.ToString();
		}
		if (Widgets.ButtonText(plus, "+", active: value < allowedMax))
		{
			value++;
			buffer = value.ToString();
		}
		Widgets.TextFieldNumeric(field, ref value, ref buffer, 0f, allowedMax);
		if (value > allowedMax)
		{
			value = allowedMax;
			buffer = value.ToString();
		}
	}

	private static void DrawGrowthAllocation(
		Listing_Standard listing,
		string label,
		ref float value,
		ref string buffer,
		float remaining)
	{
		Rect row = listing.GetRect(30f);
		Widgets.Label(row.LeftPart(0.38f), label);
		Rect controls = row.RightPart(0.58f);
		Rect minus = new Rect(controls.x, controls.y, 42f, controls.height);
		Rect plus = new Rect(controls.xMax - 42f, controls.y, 42f, controls.height);
		Rect field = new Rect(
			controls.center.x - 55f,
			controls.y,
			110f,
			controls.height);
		float allowedMax = NormalizeGrowth(Mathf.Clamp(
			value + remaining,
			0f,
			DotaHeroSetupUtility.MaxGrowthPerLevel));

		if (Widgets.ButtonText(minus, "−", active: value > 0f))
		{
			value = NormalizeGrowth(value - 0.1f);
			buffer = value.ToString("0.0");
		}
		if (Widgets.ButtonText(
			plus,
			"+",
			active: value + 0.099f < allowedMax))
		{
			value = NormalizeGrowth(value + 0.1f);
			buffer = value.ToString("0.0");
		}
		Widgets.TextFieldNumeric(
			field,
			ref value,
			ref buffer,
			0f,
			allowedMax);
		if (value > allowedMax)
		{
			value = allowedMax;
			buffer = value.ToString("0.0");
		}
	}

	private static float NormalizeGrowth(float value)
	{
		return Mathf.Clamp(Mathf.Round(value * 10f) / 10f, 0f, 5f);
	}

	private float TotalGrowth()
	{
		return NormalizeGrowth(strengthGrowth)
			+ NormalizeGrowth(agilityGrowth)
			+ NormalizeGrowth(intelligenceGrowth);
	}
}
