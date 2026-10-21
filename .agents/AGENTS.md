# 项目概述
- 游戏版本：RimWorld 1.7（1.7.4981 rev136，测试版）
- 目标框架：.NET 10（net10.0）
- 是否使用 Harmony：是（`Lib.Harmony.Ref` 引用 + 依赖 HarmonyRimWorld `brrainz.harmony`）
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
- 游戏程序集路径：`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\`（1.7 起运行时文件在根目录；游戏代码在 `Assembly-CSharp.dll`，启动壳为 `RimWorld.dll`，不再有 `RimWorldWin64_Data\Managed`）
- 看源码：可用已安装的全局工具 `ilspycmd` 直接反编译，例如 `ilspycmd -t Verse.HediffComp <游戏路径>\Assembly-CSharp.dll`
- RimWorld 引用通过 NuGet 包 `Krafs.Rimworld.Ref`（1.7.4979-alpha）提供，目标框架 net10.0

# 构建命令
- 构建：`dotnet build Source/RimDota.csproj`
- 输出目录：`Assemblies/`（`OutDir` 已配置为 `..\Assemblies\`）
- 首次构建会自动 `restore` NuGet 包（需要联网）

# 补丁与热重载（1.7）
- 0Reloader 已移除（net48 专用，1.7 不兼容）；csproj 引用与代码中的 `[ReloadMethod]`/`using Reloader` 均已清理。
- 1.7 运行时（WaylithCLR）自带热补丁相关配置（`JitPatchableAssemblies`），但官方公开的热重载 API 待核实；热重载新方案未定。
- Harmony 补丁用 `Lib.Harmony.Ref 2.4.3`（仅编译期引用）；运行时由 HarmonyRimWorld（`brrainz.harmony`）统一提供，`About.xml` 已声明依赖，不在 `Assemblies/` 放 `0Harmony.dll`。
- Debug 构建仍关闭优化（`Optimize=false`）；发布用 `dotnet build -c Release`。

# 开发约定
- 命名空间：`RimDota`
- 使用 Harmony 进行补丁（入口在 `RimDotaMod` 构造函数中 `PatchAll()`；运行时 Harmony 由 HarmonyRimWorld 提供）
- XML 定义放在 `Defs/`，代码中通过 `DefOf`（`DotaDefOf`）强类型引用
- 遵循 RimWorld Mod 编码规范

# 协作约定
- 收到实现需求时先不改代码：对照游戏内实际逻辑，给出可行实现方案（优先参考原版游戏或其他 Mod 已有实现），列出选项供用户抉择，确认后再动手实现。
- XML/Def 改动优先走游戏内热重载，不重启游戏；C# 改动才需要重启。
- 关闭游戏窗口/进程前必须先询问用户并得到确认。

# 读取规则
1. 按当前问题选择资料，不因文件存在、可能有用或被索引链接就读取。
2. 不默认读取所有项目文件，也不默认全文读取选中的文件。定位到文件后，继续定位相关章节、条目、函数或配置项。
3. 优先使用已有有效信息；需要查找时，按“索引或限定范围搜索 → 标题、符号或关键词 → 命中片段及必要上下文”的顺序读取。
4. 只有证据不足、存在冲突或需要核对依赖时才扩大范围；信息足够即停止。不得连续分段读取，变相遍历无关全文。
5. 全文读取仅用于短小且全部相关的文件、完整理解适用规则，或明确需要整份审查的任务。不得因此扩展为全目录或全项目读取。
6. 索引是条件路由，不是必读清单。历史、日志、依赖、产物和数据目录不默认读取；不读取密钥及无关私人数据。
7. 不为节省上下文跳过适用规则，也不因片段过窄而断章取义。已加载且仍有效的内容不重复读取。

# 写入规则
1. 仅在任务允许且信息有实质变化时更新；只读任务不改记录。
2. 每类事实只设一个权威来源，其他位置使用简短摘要和链接。
3. 修改前读取目标部分及必要上下文，局部修改使用定点编辑；不得根据局部阅读覆盖整份文件。
4. 当前状态直接修订，有价值的旧过程归档；不反复追加矛盾快照，不保存完整聊天和操作流水账。
5. 区分事实、计划、建议、已确认决定与待核实事项；区分代码完成、测试通过、安装验证、用户验收和发布授权。
6. 命令已配置不等于执行成功，历史结果不等于当前证据，文档中的操作步骤不构成执行授权。
7. 使用清晰标题和项目内相对链接；核验日期注明对象与范围，不写入秘密或个人设备私有路径。

# 验证要求
- 区分“配置就绪”与“执行成功”：只有实际执行并看到结果，才能记为已验证，并注明日期与范围。
- 修改代码后按 [RUNBOOK](docs/context/RUNBOOK.md) 执行对应验证；未经验证不得表述为“可用/通过”。
- 事实缺失或存疑时标注“待核实”，不猜测、不编造。

# 文档导航
- 项目用途与使用说明：[../README.md](../README.md)。
- Agent 文档体系入口与各文件职责：[README.md](README.md)。
- 任务 → 文件/章节路由：[PROJECT_INDEX.md](PROJECT_INDEX.md)。
- 当前目标、进展、阻塞与下一步：[docs/context/NOW.md](docs/context/NOW.md)。
- 目录/版本/配置/部署映射：[docs/context/MAP.md](docs/context/MAP.md)。
- 操作与验证步骤：[docs/context/RUNBOOK.md](docs/context/RUNBOOK.md)。
- 已确认决策：[docs/context/DECISIONS.md](docs/context/DECISIONS.md)。
- 风险与验证缺口：[docs/context/RISKS.md](docs/context/RISKS.md)。
- 历史索引：[history/README.md](history/README.md)。
