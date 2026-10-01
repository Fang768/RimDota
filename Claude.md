# 项目概述
这是一个 RimWorld 的 C# Mod，名为 [RimDota]。
- 游戏版本：RimWorld 1.6（1.6.4871 rev590）
- 目标框架：.NET Framework 4.7.2（net48）
- 是否使用 Harmony：是
- 命名空间：`RimDota`
- 程序集名：`RimDota.dll`（输出到 `Assemblies/`）
- packageId：`magicconch.rimdota`

# 项目结构
- `Source/` - C# 源代码（含 `RimDota.csproj`）
- `About/` - Mod 元数据（`About.xml`）
- `Defs/` - XML 定义文件
- `Assemblies/` - 编译输出（构建后生成 `RimDota.dll`）
- `Languages/` - 本地化文本
- `Patches/` - XML 补丁（对原版/其他 Mod 的 def 打补丁）
- `Textures/` / `Sounds/` - 贴图与音效

# 关键参考
- 游戏程序集路径：`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\`
- 已反编译的源码可在 VSCode 的 ILSpy 插件中查看
- RimWorld 引用通过 NuGet 包 `Krafs.Rimworld.Ref`（1.6.4871）提供，与游戏版本精确匹配

# 构建命令
- 构建：`dotnet build Source/RimDota.csproj`
- 输出目录：`Assemblies/`（`OutDir` 已配置为 `..\Assemblies\`）
- 首次构建会自动 `restore` NuGet 包（需要联网）

# 热重载调试（0Reloader）
- `Assemblies/0Reloader.dll` 提供方法级热重载，改动 C# 后重新编译即可生效，无需重启游戏
- 用 `[ReloadMethod]` 特性标注需要热重载的方法（`using Reloader;`）
- 每次构建程序集版本号自动递增（`AssemblyVersion=1.0.*` + `Deterministic=false`），这是热重载能工作的前提
- 默认 `dotnet build`（Debug）已关闭优化，避免方法被内联导致无法替换；发布用 `dotnet build -c Release`
- 注意事项：泛型方法无法热重载；每次热重载会在内存里多保留一份旧程序集，长时间调试后内存会增长，重启游戏即可释放
- ⚠️ 发布前必须移除：`Assemblies/0Reloader.dll`、csproj 的 `<Reference Include="0Reloader">`、代码里的 `[ReloadMethod]` 与 `using Reloader;`

# 开发约定
- 命名空间：`RimDota`
- 使用 Harmony 进行补丁（入口在 `RimDotaMod` 构造函数中 `PatchAll()`）
- XML 定义放在 `Defs/`，代码中通过 `DefOf`（`DotaDefOf`）强类型引用
- 遵循 RimWorld Mod 编码规范
