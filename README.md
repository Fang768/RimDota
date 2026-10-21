# RimDota

面向 RimWorld 1.6 的 C# Mod。当前处于“已配置开发环境的模板”阶段：工程已能编译出 `RimDota.dll` 供游戏加载，但具体玩法功能尚未实现，现有内容均为可替换的示例代码。

## 当前状态
- 游戏版本：RimWorld 1.6（1.6.4871 rev590）
- 目标框架：.NET Framework 4.7.2，使用 Harmony 补丁
- 已具备：示例 Hediff（buff）、示例 Harmony 补丁、示例 Def 与 DefOf 接线
- 尚未实现：实际的 Dota 主题玩法（范围待确认）

## 基本使用
1. 构建：`dotnet build Source/RimDota.csproj`，输出到 `Assemblies/RimDota.dll`。
2. 在 RimWorld Mod 管理器中启用本 Mod（packageId：`magicconch.rimdota`）。

## 许可
MIT（见 [LICENSE](LICENSE)；版权人信息待补充）。
