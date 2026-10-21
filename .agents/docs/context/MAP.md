# 目录 / 版本 / 配置 / 部署映射（MAP）

> 最后更新：2026-10-18。范围：当前工作区（Steam 本地 Mod 目录）。

## 目录

| 路径 | 用途 | 备注 |
| --- | --- | --- |
| `Source/` | C# 源码与 csproj | 游戏不加载，仅供开发 |
| `About/` | Mod 元数据 | `About.xml`、`Preview.png` |
| `Defs/` | XML 定义 | `HediffDefs`、`ApparelLayerDefs`、`ResearchProjectDefs`、`ThingDefs_Buildings/Items/Apparel`、`MapGeneration` |
| `Patches/` | XML xpath 补丁 | 目前为空（仅 README） |
| `Languages/` | 本地化 | `English/Keyed`、`English/DefInjected` |
| `Textures/`、`Sounds/` | 贴图、音效 | `Textures/UI/Icons/Dota/` 三维占位图标；`Sounds/` 为空（仅 README） |
| `Assemblies/` | 编译输出 | `RimDota.dll`、`RimDota.pdb`、`RimDota.deps.json`（不放 `0Harmony.dll`） |
| `.agents/` | Agent 文档体系 | 本项目新增 |

## 版本

| 项 | 值 | 来源 |
| --- | --- | --- |
| 游戏版本 | RimWorld 1.7（1.7.4981 rev136，测试版） | [../../AGENTS.md](../../AGENTS.md) |
| 目标框架 | .NET 10（net10.0） | `Source/RimDota.csproj` |
| RimWorld 引用 | NuGet `Krafs.Rimworld.Ref` 1.7.4979-alpha | `Source/RimDota.csproj` |
| Harmony | NuGet `Lib.Harmony.Ref` 2.4.3（仅引用；运行时由 HarmonyRimWorld 提供） | `Source/RimDota.csproj` + `About/About.xml` |
| packageId / HarmonyId | `magicconch.rimdota` | `About/About.xml` / `Source/RimDotaMod.cs` |
| 程序集 | `RimDota.dll` → `Assemblies/` | csproj `OutDir` |

## 配置（csproj 关键项）
- `AssemblyName=RimDota`、`RootNamespace=RimDota`、`LangVersion=preview`、`PlatformTarget=AnyCPU`。
- 保留 `Deterministic=false` + `AssemblyVersion=1.0.0.*`（自动递增版本号，0Reloader 移除后可按需简化）。
- Debug：`Optimize=false`（默认）；Release：`Optimize=true`。
- Harmony 依赖：`About.xml` 声明 `brrainz.harmony`（HarmonyRimWorld），游戏统一加载一份 Harmony。

## 部署位置
- 本地 Mod 目录：项目根（Steam 本地 mods 路径）。
- Steam 工坊 Mod：`C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\`（文件夹名=工坊 ID；HarmonyRimWorld=`2009463077`，packageId `brrainz.harmony`，含 `1.7/Assemblies/0Harmony.dll`）。
- 游戏程序集参考：`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\`（1.7 起直接放根目录；游戏代码在 `Assembly-CSharp.dll`）。
- NuGet 还原产物：`Source/obj/`（已被 .gitignore 排除）；全局包缓存位置待核实。
- 编译产物 `Assemblies/*.dll`、`Source/obj|bin` 不纳入版本控制（依据：`git status` 未列出这些产物）。
