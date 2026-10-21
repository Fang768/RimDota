# RimDota

RimDota 是一个面向 RimWorld 1.7 的 Dota 2 主题框架 Mod。它提供英雄隐藏标记、三维属性（力量/敏捷/智力）、Dota 属性栏、装备属性通道、物理/法术减伤、攻击/技能吸血和统一的 Dota 回血池；后续内容 Mod 可以在此基础上扩展英雄、技能和装备。

## 兼容性

- RimWorld 1.7（开发环境：1.7.4981 rev136 测试版）
- .NET 10（`net10.0`）
- 依赖 HarmonyRimWorld（`brrainz.harmony`）

## 当前功能

- 英雄框架：`DotaHero` 隐藏 Hediff 标记、`HediffComp_DotaHero`、`DotaHeroUtil` API。
- 三维属性：力量/敏捷/智力，初始 20/20/20；主属性枚举（力量/敏捷/智力/全才）。
- 派生数值：
  - 力量：生命上限 +2%/点、回血 +0.1/点/秒
  - 敏捷：Dota 护甲 1/6 点/敏捷、瞄准/攻速 -0.5%/点
  - 智力：法力上限 +5/点、回蓝 +0.05/点/秒、魔抗 +0.1%/点、心灵敏感度 +2%/点
- Dota 属性栏：法力蓝条、三维图标/数值/成长、主属性着色、悬停 tooltip。
- 装备：六格 Dota 装备栏（独立于 RimWorld 衣物系统），支持锁定装备并将其排除在自动合成之外；力量手套/敏捷便鞋/智力斗篷/力量腰带、32 件 Dota 2 基础组件，以及吸血面具 + 阔剑合成的疯狂面具。
- 自动合成：装备、解锁和读档后按槽位 1→6 选择最早完成的配方；同格完成时优先使用靠前的实际材料槽位，不依赖配方排序。
- 伤害系统：
  - 物理：Dota 护甲公式（不扣原版护甲穿透）
  - 法术：Dota 2 魔抗（基础 25% + 智力 0.1%/点 + 装备，正魔抗乘算）
  - 之后仍走原版护甲；通用减伤走 `IncomingDamageFactor`（加算、可为负）
  - 攻击/技能吸血按实际伤害结算，溅射不吸血
- 回血池：所有 Dota 回血统一入池，每 15 tick 结算，按伤口严重度降序分配。
- Mod 设置：“Dota 回复设置”中的生命回复平衡系数默认 `0.2`，只缩放实际生命回复结算，不改变装备描述或属性栏显示值，也不影响法力回复。
- 基础内容：月亮碎片建筑、遗迹碎片/翡翠掉落、遗迹科技线。
- 调试入口：Dev 模式 → 右键小人 → Pawns → 设为/移除 Dota 英雄、查看属性。

## 构建

```powershell
dotnet build Source/RimDota.csproj
```

输出到 `Assemblies/RimDota.dll`（构建产物不入库）。

## 使用

1. 启用本 Mod 和 HarmonyRimWorld。
2. 开发者模式下用“设为 Dota 英雄”给小人加英雄标记。
3. 选中 Dota 英雄后右键地面上的 Dota 装备，装备会进入六格装备栏；非英雄不能装备。
4. “查看 Dota 英雄属性”会在 Player.log 输出属性、护甲、物理减伤、魔抗、吸血等。

## 已知限制

- 等级/经验/属性成长系统未实现（growth 字段暂为 0）。
- 主动技能、攻击力、移动速度、攻击特效尚未实现；对应装备的属性留空并标注“待实现”。
- 条件型通用减伤（刚被兽被动、幽鬼折射等）只留接口。
- 真实伤害目前没有实际 DamageDef 映射。
- 属性图标仍为红/绿/蓝占位图；Dota 2 VTex 真图标尚未正式接入。
- 最新伤害/减伤/吸血系统尚未完成游戏内验证。

## 许可与第三方素材

- 本项目原创代码与文档采用 MIT License，见 [LICENSE](LICENSE)。
- Dota 2 及其装备图标、英雄/物品名称、游戏数据归 Valve Corporation 所有。本项目是非商业同人项目，MIT 许可不覆盖 Valve 的素材；本项目与 Valve 无关联，也未获 Valve 背书。
- RimWorld 是 Ludeon Studios 的商标；本项目与 Ludeon Studios 无关联。
- 第三方声明见 [NOTICE.md](NOTICE.md)。

## 致谢

- RimWorld / Ludeon Studios
- Dota 2 / Valve Corporation
- Harmony / Andreas Pardeike (Brrainz)
