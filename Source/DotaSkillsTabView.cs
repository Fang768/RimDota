using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimDota;

/// <summary>
/// Third ITab page. The persistent component is only changed by Confirm;
/// Reset and closing the page operate on the detached edit state.
/// </summary>
[StaticConstructorOnStartup]
internal sealed class DotaSkillsTabView
{
	private static readonly Color PanelColor =
		new Color(0.055f, 0.066f, 0.085f, 0.98f);
	private static readonly Color SlotColor =
		new Color(0.085f, 0.096f, 0.115f, 1f);
	private static readonly Color EmptyPipColor =
		new Color(0.13f, 0.15f, 0.18f, 1f);
	private static readonly Color FilledPipColor =
		new Color(0.84f, 0.65f, 0.24f, 1f);
	private static readonly Color UpgradeGold =
		new Color(0.86f, 0.66f, 0.20f, 1f);
	private static readonly Color UpgradeGoldDisabled =
		new Color(0.34f, 0.29f, 0.16f, 1f);
	private static readonly Color SteelFace =
		new Color(0.61f, 0.65f, 0.69f, 1f);
	private static readonly Color SteelFaceLight =
		new Color(0.73f, 0.77f, 0.81f, 1f);
	private static readonly Color SteelDark =
		new Color(0.22f, 0.25f, 0.29f, 1f);
	private static readonly Color SteelDisabled =
		new Color(0.27f, 0.29f, 0.32f, 1f);
	private static readonly Color DarkText =
		new Color(0.075f, 0.09f, 0.11f, 1f);
	private static readonly Color MutedColor =
		new Color(0.62f, 0.64f, 0.70f, 1f);
	private static Texture2D innateButtonTexture;
	private static Texture2D talentMedallionSilver;
	private static Texture2D talentMedallionGold;

	private Pawn sessionPawn;
	private HediffComp_DotaSkills sessionComp;
	private DotaSkillEditState editState;
	private DotaSkillEditState entryState;
	private bool dirty;

	public void DiscardDraft()
	{
		sessionPawn = null;
		sessionComp = null;
		editState = null;
		entryState = null;
		dirty = false;
	}

	public void Draw(Rect rect, Pawn pawn, HediffComp_DotaSkills comp)
	{
		if (pawn == null || comp == null)
		{
			Widgets.Label(rect, "该英雄没有 Dota 技能组件。");
			return;
		}
		EnsureSession(pawn, comp);
		sessionComp.SyncProgression();
		editState.RefreshProgression();
		entryState.RefreshProgression();
		Widgets.DrawBoxSolid(rect, PanelColor);
		DrawSteelFrame(rect, 1f);

		Rect inner = rect.ContractedBy(10f);
		DrawHeader(inner);
		Rect talentArea = new Rect(inner.x, inner.y + 38f, 220f, 238f);
		DrawTalentGrid(talentArea);
		DrawAttributeButton(
			new Rect(talentArea.x + 28f, talentArea.yMax + 8f, 164f, 64f));

		Rect innateRect = new Rect(
			talentArea.xMax + 26f,
			inner.y + 135f,
			54f,
			54f);
		Rect skillArea = new Rect(
			innateRect.xMax + 24f,
			inner.y + 133f,
			inner.xMax - innateRect.xMax - 24f,
			128f);
		Widgets.DrawBoxSolid(
			new Rect(
				talentArea.xMax,
				innateRect.center.y - 1f,
				skillArea.xMax - talentArea.xMax,
				2f),
			SteelDark);
		DrawInnateSlot(innateRect);

		DrawSkillSlots(skillArea);
		DrawFooter(inner);

		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
	}

	private void EnsureSession(Pawn pawn, HediffComp_DotaSkills comp)
	{
		if (sessionPawn == pawn && sessionComp == comp && editState != null)
		{
			return;
		}
		sessionPawn = pawn;
		sessionComp = comp;
		editState = comp.CreateEditState();
		entryState = editState.Clone();
		dirty = false;
	}

	private void DrawHeader(Rect inner)
	{
		int heroLevel = DotaHeroUtil.GetHeroComp(sessionPawn)?.HeroLevel ?? 1;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(
			new Rect(inner.x, inner.y, inner.width, 26f),
			"英雄等级：" + heroLevel
				+ "    技能点：" + editState.AvailableSkillPoints
				+ " / 已获得 " + editState.EarnedSkillPoints
				+ "（上限 " + editState.Props.skillPointCap + "）"
				+ "    天赋点：" + editState.AvailableTalentPoints
				+ " / 已获得 " + editState.EarnedTalentPoints);
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private void DrawTalentGrid(Rect rect)
	{
		List<DotaTalentNodeDef> nodes =
			DefDatabase<DotaTalentNodeDef>.AllDefsListForReading
				.Where(DotaTalentUtility.IsValidSelection)
				.ToList();
		const float rowHeight = 43f;
		const float rowGap = 7f;
		const float choiceWidth = 72f;
		const float medallionSize = 36f;
		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = new Color(0.83f, 0.85f, 0.87f, 1f);
		Widgets.Label(new Rect(rect.x, rect.y - 2f, rect.width, 24f), "天赋树");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		float rowsY = rect.y + 27f;
		for (int displayRow = 0; displayRow < 4; displayRow++)
		{
			int tier = 4 - displayRow;
			float y = rowsY + displayRow * (rowHeight + rowGap);
			DotaTalentNodeDef leftNode = nodes.FirstOrDefault(candidate =>
				candidate.side == DotaTalentSide.Left
				&& candidate.tier == tier);
			DotaTalentNodeDef rightNode = nodes.FirstOrDefault(candidate =>
				candidate.side == DotaTalentSide.Right
				&& candidate.tier == tier);
			Rect row = new Rect(rect.x, y, rect.width, rowHeight);
			Widgets.DrawBoxSolid(row, new Color(0.09f, 0.105f, 0.13f, 1f));

			if (leftNode != null)
			{
				DrawTalentNode(
					new Rect(rect.x + 4f, y + 2f, choiceWidth, rowHeight - 4f),
					leftNode);
			}
			if (rightNode != null)
			{
				DrawTalentNode(
					new Rect(
						rect.xMax - choiceWidth - 4f,
						y + 2f,
						choiceWidth,
						rowHeight - 4f),
					rightNode);
			}
			DrawTalentMedallion(
				new Rect(
					rect.center.x - medallionSize / 2f,
					y + (rowHeight - medallionSize) / 2f,
					medallionSize,
					medallionSize),
				leftNode?.requiredHeroLevel
					?? rightNode?.requiredHeroLevel
					?? 0,
				(leftNode != null && editState.Talents.Contains(leftNode))
					|| (rightNode != null && editState.Talents.Contains(rightNode)));
		}
	}

	private void DrawTalentNode(Rect rect, DotaTalentNodeDef node)
	{
		bool selected = editState.Talents.Contains(node);
		bool canSelect = editState.CanSelectTalent(node, out string reason);
		DrawDarkButtonFace(rect, selected);
		Widgets.DrawHighlightIfMouseover(rect);
		if (!selected
			&& canSelect
			&& editState.AvailableTalentPoints > 0)
		{
			DrawGoldBorder(rect, 3f);
		}

		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = selected
			? FilledPipColor
			: canSelect
				? new Color(0.91f, 0.93f, 0.95f)
				: new Color(0.74f, 0.77f, 0.80f);
		string sideNumber = node.side == DotaTalentSide.Left ? "1" : "2";
		Widgets.Label(rect, node.tier + "-" + sideNumber);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;

		string side = node.side == DotaTalentSide.Left ? "左" : "右";
		string tooltip = "第 " + node.tier + " 层" + side + "侧天赋"
			+ "\n" + node.description
			+ (selected ? "\n已选择" : "\n点击消耗 1 天赋点")
			+ (!selected && !canSelect ? "\n" + reason : "");
		TooltipHandler.TipRegion(rect, tooltip);
		if (Widgets.ButtonInvisible(rect) && !selected)
		{
			if (editState.SelectTalent(node))
			{
				dirty = true;
			}
			else
			{
				Messages.Message(reason, MessageTypeDefOf.RejectInput);
			}
		}
	}

	private static void DrawTalentMedallion(
		Rect rect,
		int requiredHeroLevel,
		bool selected)
	{
		GUI.DrawTexture(
			rect,
			GetTalentMedallionTexture(selected),
			ScaleMode.ScaleToFit);
		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = selected ? FilledPipColor : new Color(0.78f, 0.81f, 0.84f);
		Widgets.Label(rect, requiredHeroLevel.ToString());
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		TooltipHandler.TipRegion(
			rect,
			"本层等级：" + requiredHeroLevel);
	}

	private static Texture2D GetTalentMedallionTexture(bool selected)
	{
		Texture2D cached = selected ? talentMedallionGold : talentMedallionSilver;
		if (cached != null)
		{
			return cached;
		}
		const int size = 64;
		const float center = (size - 1f) * 0.5f;
		Color32 ring = selected
			? new Color32(216, 167, 60, 255)
			: new Color32(151, 160, 171, 255);
		Color32 inner = new Color32(31, 36, 43, 255);
		Color32[] pixels = new Color32[size * size];
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dx = x - center;
				float dy = y - center;
				float distance = Mathf.Sqrt(dx * dx + dy * dy);
				int index = y * size + x;
				if (distance <= 22f)
				{
					pixels[index] = inner;
				}
				else if (distance <= 30f)
				{
					pixels[index] = ring;
				}
			}
		}
		Texture2D texture = new Texture2D(
			size,
			size,
			TextureFormat.RGBA32,
			mipChain: false);
		texture.name = selected
			? "DotaTalentMedallionGold"
			: "DotaTalentMedallionSilver";
		texture.SetPixels32(pixels);
		texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
		if (selected)
		{
			talentMedallionGold = texture;
		}
		else
		{
			talentMedallionSilver = texture;
		}
		return texture;
	}

	private void DrawAttributeButton(Rect rect)
	{
		bool canSpend = editState.CanSpendAttributePoint();
		DrawDarkButtonFace(rect, selected: false);
		if (editState.AvailableSkillPoints > 0
			&& canSpend)
		{
			DrawGoldBorder(rect);
		}

		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = canSpend
			? new Color(0.91f, 0.93f, 0.95f)
			: MutedColor;
		Widgets.Label(
			new Rect(rect.x, rect.y + 5f, rect.width, 22f),
			"属性点");
		GUI.color = Color.white;

		int capacity = Mathf.Max(0, editState.Props.attributePointCapacity);
		const float pipWidth = 12f;
		const float pipGap = 4f;
		float totalPipWidth = capacity * pipWidth
			+ Mathf.Max(0, capacity - 1) * pipGap;
		float pipX = rect.center.x - totalPipWidth / 2f;
		for (int i = 0; i < capacity; i++)
		{
			Rect pip = new Rect(
				pipX + i * (pipWidth + pipGap),
				rect.y + 35f,
				pipWidth,
				10f);
			Widgets.DrawBoxSolid(
				pip,
				i < editState.AttributePointsSpent
					? FilledPipColor
					: EmptyPipColor);
			DrawSteelFrame(pip, 1f);
		}
		Text.Anchor = TextAnchor.UpperLeft;

		float bonus = editState.AttributePointBonus;
		string tooltip = "属性点：" + editState.AttributePointsSpent
			+ " / " + capacity
			+ "\n当前全属性 +" + bonus.ToString("0")
			+ "\n力量 +" + bonus.ToString("0")
			+ "\n敏捷 +" + bonus.ToString("0")
			+ "\n智力 +" + bonus.ToString("0")
			+ "\n\n左键消耗 " + editState.Props.AttributePointCost + " 技能点";
		TooltipHandler.TipRegion(rect, tooltip);
		if (Widgets.ButtonInvisible(rect))
		{
			if (editState.SpendAttributePoint())
			{
				dirty = true;
			}
		}
	}

	private static void DrawInnateSlot(Rect rect)
	{
		Widgets.DrawBoxSolid(rect, new Color(0.045f, 0.05f, 0.075f, 1f));
		DrawSteelFrame(rect, 1f);
		GUI.DrawTexture(
			rect.ContractedBy(2f),
			InnateButtonTexture,
			ScaleMode.ScaleToFit);
		TooltipHandler.TipRegion(rect, "先天技能\n当前为空");
	}

	private static Texture2D InnateButtonTexture
	{
		get
		{
			if (innateButtonTexture != null)
			{
				return innateButtonTexture;
			}
			const int size = 64;
			const float center = (size - 1f) * 0.5f;
			Color32 silver = new Color32(183, 188, 198, 255);
			Color32 silverDark = new Color32(86, 91, 101, 255);
			Color32 gold = new Color32(224, 176, 54, 255);
			Color32 centerColor = new Color32(22, 24, 31, 255);
			Color32[] pixels = new Color32[size * size];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = x - center;
					float dy = y - center;
					float distance = Mathf.Sqrt(dx * dx + dy * dy);
					int index = y * size + x;
					if (distance > 30f)
					{
						continue;
					}
					if (distance > 25f)
					{
						pixels[index] = silver;
					}
					else if (distance > 21f)
					{
						pixels[index] = silverDark;
					}
					else if (distance > 13f)
					{
						pixels[index] = centerColor;
					}
					else
					{
						pixels[index] = gold;
					}
				}
			}
			innateButtonTexture = new Texture2D(
				size,
				size,
				TextureFormat.RGBA32,
				mipChain: false);
			innateButtonTexture.name = "DotaInnateSkillPlaceholder";
			innateButtonTexture.SetPixels32(pixels);
			innateButtonTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
			return innateButtonTexture;
		}
	}

	private void DrawSkillSlots(Rect rect)
	{
		const float slotSize = 62f;
		const float gap = 8f;
		int count = editState.TotalSlotCount;
		float x = rect.x;
		for (int i = 0; i < count; i++)
		{
			DrawSkillSlot(
				new Rect(x + i * (slotSize + gap), rect.y, slotSize, slotSize),
				i);
		}
	}

	private void DrawSkillSlot(Rect slot, int index)
	{
		AbilityDef ability = editState.SlotSkills[index];
		DotaSkillExtension extension = DotaSkillExtension.For(ability);
		if (ability != null && editState.AvailableSkillPoints > 0)
		{
			DrawUpgradeButton(slot, index);
		}
		Widgets.DrawBoxSolid(
			slot,
			extension?.placeholderColor ?? SlotColor);
		DrawSteelFrame(slot, 2f);
		Widgets.DrawHighlightIfMouseover(slot);

		if (ability == null)
		{
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = new Color(0.62f, 0.64f, 0.70f, 1f);
			Widgets.Label(
				slot,
				editState.IsSlotEditable(index) ? "+" : "锁");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
		}
		else
		{
			int level = editState.SlotLevels[index];
			float mana = extension?.ManaCostAtLevel(Mathf.Max(1, level)) ?? 0f;
			Rect manaRect = new Rect(slot.x + 3f, slot.yMax - 18f, 32f, 16f);
			Widgets.DrawBoxSolid(
				manaRect,
				new Color(0.02f, 0.03f, 0.06f, 0.82f));
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(manaRect, mana.ToString("0"));
			Text.Anchor = TextAnchor.UpperLeft;

			DrawLevelPips(
				new Rect(slot.x, slot.yMax + 5f, slot.width, 9f),
				level,
				extension?.ResolvedMaxLevel ?? 0);
		}

		string tooltip = SkillTooltip(index, ability, extension);
		TooltipHandler.TipRegion(slot, tooltip);

		if (Event.current.type == EventType.MouseDown
			&& Event.current.button == 1
			&& slot.Contains(Event.current.mousePosition)
			&& editState.IsSlotEditable(index))
		{
			if (editState.ClearSkill(index))
			{
				dirty = true;
			}
			Event.current.Use();
			return;
		}

		if (Widgets.ButtonInvisible(slot))
		{
			if (!editState.IsSlotEditable(index))
			{
				Messages.Message(
					"主技能与奥义配置已经锁定。",
					MessageTypeDefOf.RejectInput);
			}
			else
			{
				OpenSkillPicker(index);
			}
		}
	}

	private void DrawUpgradeButton(Rect slot, int index)
	{
		bool canUpgrade = editState.CanUpgradeSkill(index);
		Rect button = new Rect(slot.x, slot.y - 23f, slot.width, 19f);
		Widgets.DrawBoxSolid(
			button,
			canUpgrade ? UpgradeGold : UpgradeGoldDisabled);
		DrawSteelFrame(button, 1f);
		Widgets.DrawHighlightIfMouseover(button);

		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = Color.white;
		Widgets.Label(button, "+");
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;

		string tooltip = canUpgrade
			? "升级 " + editState.SlotSkills[index].LabelCap
			: editState.UpgradeBlockedReason(index);
		TooltipHandler.TipRegion(button, tooltip);
		if (canUpgrade && Widgets.ButtonInvisible(button))
		{
			if (editState.UpgradeSkill(index))
			{
				dirty = true;
			}
		}
	}

	private static void DrawMetalButtonFace(
		Rect rect,
		bool selected,
		bool enabled)
	{
		Color face = selected
			? new Color(0.39f, 0.30f, 0.13f, 1f)
			: enabled
				? SteelFace
				: SteelDisabled;
		Widgets.DrawBoxSolid(rect, face);
		if (enabled || selected)
		{
			Widgets.DrawBoxSolid(
				new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f),
				selected
					? new Color(0.91f, 0.72f, 0.30f, 0.85f)
					: SteelFaceLight);
		}
		DrawSteelFrame(rect, 1f);
	}

	private static void DrawDarkButtonFace(Rect rect, bool selected)
	{
		Widgets.DrawBoxSolid(
			rect,
			selected
				? new Color(0.22f, 0.18f, 0.10f, 1f)
				: SlotColor);
		Widgets.DrawBoxSolid(
			new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f),
			selected
				? new Color(0.91f, 0.72f, 0.30f, 0.85f)
				: new Color(0.34f, 0.38f, 0.43f, 1f));
		DrawSteelFrame(rect, 1f);
	}

	private static bool DrawMetalButton(
		Rect rect,
		string label,
		bool enabled,
		bool emphasized = false)
	{
		DrawMetalButtonFace(rect, emphasized, enabled);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		GUI.color = emphasized
			? Color.white
			: enabled
				? DarkText
				: MutedColor;
		Widgets.Label(rect, label);
		GUI.color = Color.white;
		Text.Anchor = TextAnchor.UpperLeft;
		Widgets.DrawHighlightIfMouseover(rect);
		return Widgets.ButtonInvisible(rect);
	}

	internal static void DrawSteelFrame(Rect rect, float thickness)
	{
		Color outer = SteelDark;
		Color highlight = new Color(0.72f, 0.76f, 0.80f, 0.9f);
		Widgets.DrawBoxSolid(
			new Rect(rect.x, rect.y, rect.width, thickness),
			highlight);
		Widgets.DrawBoxSolid(
			new Rect(rect.x, rect.yMax - thickness, rect.width, thickness),
			outer);
		Widgets.DrawBoxSolid(
			new Rect(rect.x, rect.y, thickness, rect.height),
			outer);
		Widgets.DrawBoxSolid(
			new Rect(rect.xMax - thickness, rect.y, thickness, rect.height),
			outer);
	}

	private static void DrawGoldBorder(Rect rect, float thickness = 2f)
	{
		Color color = UpgradeGold;
		Widgets.DrawBoxSolid(
			new Rect(rect.x, rect.y, rect.width, thickness),
			color);
		Widgets.DrawBoxSolid(
			new Rect(rect.x, rect.yMax - thickness, rect.width, thickness),
			color);
		Widgets.DrawBoxSolid(
			new Rect(rect.x, rect.y, thickness, rect.height),
			color);
		Widgets.DrawBoxSolid(
			new Rect(rect.xMax - thickness, rect.y, thickness, rect.height),
			color);
	}

	private static void DrawLevelPips(
		Rect rect,
		int current,
		int maximum)
	{
		if (maximum <= 0)
		{
			return;
		}
		const float pipWidth = 8f;
		const float pipGap = 4f;
		float totalWidth = maximum * pipWidth
			+ Mathf.Max(0, maximum - 1) * pipGap;
		float x = rect.center.x - totalWidth / 2f;
		for (int i = 0; i < maximum; i++)
		{
			Rect pip = new Rect(
				x + i * (pipWidth + pipGap),
				rect.y,
				pipWidth,
				7f);
			Widgets.DrawBoxSolid(
				pip,
				i < current ? FilledPipColor : EmptyPipColor);
			DrawSteelFrame(pip, 1f);
		}
	}

	private string SkillTooltip(
		int index,
		AbilityDef ability,
		DotaSkillExtension extension)
	{
		string slotLabel = editState.ownerSlotLabel(index);
		if (ability == null)
		{
			return slotLabel + "\n空槽"
				+ (editState.IsSlotEditable(index)
					? "\n左键选择技能"
					: "\n配置已锁定");
		}
		int level = editState.SlotLevels[index];
		int nextLevel = level + 1;
		int requiredLevel = nextLevel <= extension.ResolvedMaxLevel
			? extension.RequiredHeroLevelForLevel(nextLevel)
			: 0;
		return ability.LabelCap + "\n" + ability.description
			+ "\n\n类型：" + slotLabel
			+ "\n等级：" + level + " / " + extension.ResolvedMaxLevel
			+ "\n法力消耗：" + extension.ManaCostAtLevel(Mathf.Max(1, level)).ToString("0")
			+ (requiredLevel > 0
				? "\n下一级需要英雄等级：" + requiredLevel
				: "")
			+ (editState.IsSlotEditable(index)
				? "\n左键更换，右键清空"
				: "\n使用图标上方金色按钮升级");
	}

	private void OpenSkillPicker(int slotIndex)
	{
		DotaAbilityKind kind = sessionComp.SlotKindAt(slotIndex);
		List<AbilityDef> common = DotaSkillPoolUtility.CommonSkills()
			.Where(ability => DotaSkillExtension.For(ability)?.kind == kind)
			.ToList();
		List<AbilityDef> hero = sessionComp.HeroSpecificSkills()
			.Where(ability => DotaSkillExtension.For(ability)?.kind == kind)
			.ToList();
		if (common.Count == 0 && hero.Count == 0)
		{
			Messages.Message(
				kind == DotaAbilityKind.Derived
					? "暂无可用衍生技能。"
					: "当前技能池为空。",
				MessageTypeDefOf.RejectInput);
			return;
		}

		Find.WindowStack.Add(new Dialog_DotaSkillPicker(
			sessionComp.SlotTypeLabel(slotIndex),
			common,
			hero,
			ability =>
			{
				if (!editState.CanAssignSkill(
					slotIndex,
					ability,
					out string reason))
				{
					Messages.Message(reason, MessageTypeDefOf.RejectInput);
					return;
				}
				if (editState.AssignSkill(slotIndex, ability))
				{
					dirty = true;
				}
			}));
	}

	private void DrawFooter(Rect inner)
	{
		Rect footer = new Rect(
			inner.x,
			inner.yMax - 42f,
			inner.width,
			38f);
		if (Prefs.DevMode)
		{
			DrawDeveloperButtons(footer);
		}
		if (!dirty)
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleRight;
			GUI.color = MutedColor;
			Widgets.Label(
				new Rect(
					footer.x + (Prefs.DevMode ? 330f : 0f),
					footer.y,
					footer.width - (Prefs.DevMode ? 330f : 0f),
					footer.height),
				editState.LayoutLocked
					? "主技能与奥义已锁定；衍生槽可调"
					: "可配置技能位置");
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			return;
		}

		if (DrawMetalButton(
			new Rect(footer.xMax - 270f, footer.y, 120f, footer.height),
			"重置",
			enabled: true))
		{
			editState = entryState.Clone();
			dirty = false;
		}
		if (DrawMetalButton(
			new Rect(footer.xMax - 140f, footer.y, 140f, footer.height),
			"确认",
			enabled: true,
			emphasized: true))
		{
			sessionComp.ApplyEditState(editState);
			editState = sessionComp.CreateEditState();
			entryState = editState.Clone();
			dirty = false;
		}
	}

	private void DrawDeveloperButtons(Rect footer)
	{
		HediffComp_DotaHero hero = DotaHeroUtil.GetHeroComp(sessionPawn);
		bool canLevel = hero != null
			&& !hero.AwaitingInitialSetup
			&& hero.HeroLevel < DotaHeroLeveling.MaxLevel;
		string disabledReason = hero == null
			? "找不到 Dota 英雄组件。"
			: hero.AwaitingInitialSetup
				? "请先完成英雄初始化。"
				: hero.HeroLevel >= DotaHeroLeveling.MaxLevel
					? "英雄已经达到最高等级。"
					: null;
		float x = footer.x;
		Rect levelButton = new Rect(x, footer.y, 96f, footer.height);
		if (DrawMetalButton(
			levelButton,
			"升级",
			enabled: canLevel))
		{
			if (!canLevel)
			{
				Messages.Message(
					"升级失败：" + disabledReason,
					MessageTypeDefOf.RejectInput);
			}
			else if (hero.DebugLevelUp())
			{
				RefreshAfterDeveloperChange();
				Messages.Message(
					"调试：Dota 等级提升至 " + hero.HeroLevel + "。",
					sessionPawn,
					MessageTypeDefOf.PositiveEvent);
			}
			else
			{
				Messages.Message(
					"升级失败：" + (disabledReason ?? "等级没有发生变化。"),
					MessageTypeDefOf.RejectInput);
			}
		}
		TooltipHandler.TipRegion(
			levelButton,
			canLevel ? "英雄 Dota 等级 +1" : disabledReason);
		x += 102f;
		Rect maxButton = new Rect(x, footer.y, 112f, footer.height);
		if (DrawMetalButton(
			maxButton,
			"升至满级",
			enabled: canLevel))
		{
			if (!canLevel)
			{
				Messages.Message(
					"升至满级失败：" + disabledReason,
					MessageTypeDefOf.RejectInput);
			}
			else if (hero.DebugSetLevelToMax())
			{
				RefreshAfterDeveloperChange();
				Messages.Message(
					"调试：Dota 等级提升至满级 " + hero.HeroLevel + "。",
					sessionPawn,
					MessageTypeDefOf.PositiveEvent);
			}
			else
			{
				Messages.Message(
					"升至满级失败：" + (disabledReason ?? "等级没有发生变化。"),
					MessageTypeDefOf.RejectInput);
			}
		}
		TooltipHandler.TipRegion(
			maxButton,
			canLevel ? "直接提升至 Dota 等级 30" : disabledReason);
		x += 118f;
		Rect resetSkillsButton = new Rect(x, footer.y, 96f, footer.height);
		if (DrawMetalButton(
			resetSkillsButton,
			"洗点",
			enabled: true))
		{
			sessionComp.ResetSkillProgression();
			editState = sessionComp.CreateEditState();
			entryState = editState.Clone();
			dirty = false;
			Messages.Message(
				"调试：已重置技能、属性点和天赋。",
				MessageTypeDefOf.NeutralEvent);
		}
		TooltipHandler.TipRegion(
			resetSkillsButton,
			"清空技能配置、技能等级、属性点和天赋，并返还对应点数。");
	}

	private void RefreshAfterDeveloperChange()
	{
		sessionComp.SyncProgression();
		editState.RefreshProgression();
		entryState.RefreshProgression();
	}
}

internal static class DotaSkillEditStateExtensions
{
	public static string ownerSlotLabel(
		this DotaSkillEditState state,
		int index)
	{
		return index < state.Props.normalSlotCount
			? "普通技能 " + (index + 1)
			: index < state.Props.normalSlotCount + state.Props.derivedSlotCount
				? "衍生技能 " + (index - state.Props.normalSlotCount + 1)
				: "奥义";
	}
}

internal sealed class Dialog_DotaSkillPicker : Window
{
	private const int Columns = 4;
	private const float CardWidth = 128f;
	private const float CardHeight = 78f;
	private const float CardGap = 8f;

	private readonly string slotLabel;
	private readonly List<AbilityDef> commonSkills;
	private readonly List<AbilityDef> heroSkills;
	private readonly Action<AbilityDef> onSelect;
	private Vector2 scrollPosition;

	public override Vector2 InitialSize => new Vector2(620f, 540f);

	public Dialog_DotaSkillPicker(
		string slotLabel,
		List<AbilityDef> commonSkills,
		List<AbilityDef> heroSkills,
		Action<AbilityDef> onSelect)
	{
		this.slotLabel = slotLabel;
		this.commonSkills = commonSkills;
		this.heroSkills = heroSkills;
		this.onSelect = onSelect;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnClickedOutside = true;
	}

	public override void DoWindowContents(Rect inRect)
	{
		Rect inner = inRect.ContractedBy(16f);
		Text.Font = GameFont.Medium;
		Widgets.Label(
			new Rect(inner.x, inner.y, inner.width, 32f),
			"选择" + slotLabel);
		Text.Font = GameFont.Small;

		Rect viewRect = new Rect(
			inner.x,
			inner.y + 38f,
			inner.width,
			inner.height - 38f);
		float contentHeight = GroupHeight(commonSkills)
			+ GroupHeight(heroSkills)
			+ 8f;
		Rect content = new Rect(
			0f,
			0f,
			viewRect.width - 18f,
			Mathf.Max(viewRect.height, contentHeight));
		Widgets.BeginScrollView(viewRect, ref scrollPosition, content);
		float y = DrawGroup(content, 0f, "通用技能池", commonSkills);
		y += 8f;
		DrawGroup(content, y, "小人专属技能池", heroSkills);
		Widgets.EndScrollView();
	}

	private static float GroupHeight(List<AbilityDef> abilities)
	{
		if (abilities.Count == 0)
		{
			return 57f;
		}
		int rows = Mathf.CeilToInt(abilities.Count / (float)Columns);
		return 27f + rows * CardHeight + Mathf.Max(0, rows - 1) * CardGap;
	}

	private float DrawGroup(
		Rect inner,
		float y,
		string title,
		List<AbilityDef> abilities)
	{
		Widgets.Label(new Rect(inner.x, y, inner.width, 24f), title);
		y += 27f;
		if (abilities.Count == 0)
		{
			GUI.color = new Color(0.62f, 0.64f, 0.70f, 1f);
			Widgets.Label(
				new Rect(inner.x + 8f, y, inner.width - 16f, 24f),
				"暂无可用技能");
			GUI.color = Color.white;
			return y + 30f;
		}

		for (int i = 0; i < abilities.Count; i++)
		{
			AbilityDef ability = abilities[i];
			int column = i % Columns;
			int rowIndex = i / Columns;
			Rect card = new Rect(
				inner.x + column * (CardWidth + CardGap),
				y + rowIndex * (CardHeight + CardGap),
				CardWidth,
				CardHeight);
			Widgets.DrawBoxSolid(
				card,
				new Color(0.055f, 0.065f, 0.09f, 1f));
			DotaSkillsTabView.DrawSteelFrame(card, 1f);
			Widgets.DrawHighlightIfMouseover(card);

			Rect icon = new Rect(card.x + 6f, card.y + 7f, 42f, 42f);
			Widgets.DrawBoxSolid(
				icon,
				DotaSkillExtension.For(ability).placeholderColor);
			DotaSkillsTabView.DrawSteelFrame(icon, 1f);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Text.WordWrap = true;
			Widgets.Label(
				new Rect(
					icon.xMax + 7f,
					card.y + 6f,
					card.width - 60f,
					43f),
				ability.LabelCap);

			DotaSkillExtension extension = DotaSkillExtension.For(ability);
			Text.Font = GameFont.Tiny;
			GUI.color = new Color(0.62f, 0.64f, 0.70f, 1f);
			Widgets.Label(
				new Rect(
					card.x + 5f,
					card.yMax - 22f,
					card.width - 10f,
					17f),
				"最高 " + extension.ResolvedMaxLevel
					+ " 级 · " + extension.ManaCostAtLevel(1).ToString("0") + " 法力");
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			string tooltip = ability.LabelCap + "\n" + ability.description
				+ "\n\n最高等级：" + extension.ResolvedMaxLevel
				+ "\n每级法力消耗：" + extension.ManaCostAtLevel(1).ToString("0");
			TooltipHandler.TipRegion(card, tooltip);
			if (Widgets.ButtonInvisible(card))
			{
				onSelect(ability);
				Close();
				return y;
			}
		}
		int rows = Mathf.CeilToInt(abilities.Count / (float)Columns);
		return y + rows * CardHeight + Mathf.Max(0, rows - 1) * CardGap;
	}
}
