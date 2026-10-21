# 操作手册（RUNBOOK）

> 最后更新：2026-10-18。依据：[../../AGENTS.md](../../AGENTS.md)、`Source/RimDota.csproj`、本次构建验证。

## 构建（net10.0）
- 命令：`dotnet build Source/RimDota.csproj`（Debug，输出到 `Assemblies/RimDota.dll`）。
- 前置：.NET SDK（本机 10.0.401）；首次构建联网还原 NuGet 包（`Krafs.Rimworld.Ref` 1.7.4979-alpha、`Lib.Harmony.Ref` 2.4.3）。
- 验证（2026-10-17 已执行）：`dotnet build Source/RimDota.csproj` → 0 警告 0 错误；`Assemblies/` 产物含 `RimDota.dll`、`RimDota.pdb`、`RimDota.deps.json`（不放 `0Harmony.dll`，运行时由 HarmonyRimWorld 提供）。

## 发布构建
- 命令：`dotnet build Source/RimDota.csproj -c Release`（开启优化）。
- 状态：未执行（发布时执行并验证）。

## 补丁与热重载（1.7）
- C# 改动需重启：游戏运行时会锁定 `Assemblies/RimDota.dll`，编译无法覆盖；关游戏前先询问用户。
- XML/Def 热重载：开发者模式 → 调试操作 → General →「Hot reload Defs」（内部调用 `PlayDataLoader.HotReloadDefs()` → `LoadedModManager.LoadAllActiveMods(hotReload: true)`）。改 XML 后走这个，不重启。
- Harmony 补丁：`Lib.Harmony.Ref` 引用 + `About.xml` 声明依赖 `brrainz.harmony`；不要在 `Assemblies/` 放 `0Harmony.dll`。

## 调试入口（已实现）
- 开发者模式 → 右键选中小人 → 调试操作 → Pawns → 「设为 Dota 英雄（选中）」/「移除 Dota 英雄（选中）」/「查看 Dota 英雄属性」。
- 「查看 Dota 英雄属性」会把力量/敏捷/智力、生命倍率、总最大生命、法力上限打印到 Player.log（`Log.Message`）。

## 发布前检查（未执行）
1. 替换 `About/About.xml` 的模板描述。
2. `dotnet build Source/RimDota.csproj -c Release` 并验证。
3. 游戏内验证 Harmony 补丁、DotaHero 标记与法力条生效。

## 游戏内验证（未执行）
- 在 RimWorld Mod 管理器启用本 Mod，确认无报错，日志输出“RimDota: 已加载。”与“Harmony 补丁已生效”。
- 用「设为 Dota 英雄（选中）」给小人加 DotaHero，确认：底部命令栏出现法力条（“法力”标签 + 蓝条 + 三维“图标/值/成长”）、DotaHero 不出现在健康面板、非英雄无此条。
- 用「查看 Dota 英雄属性」核对日志中生命倍率/法力上限随三维变化。
- 观察 Player.log 无红字报错。
