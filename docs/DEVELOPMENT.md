# Four-Dimensional 开发文档

更新时间：2026-07-09

本文面向接手项目开发的人，用来快速理解仓库结构、运行方式、核心系统边界和常见改动流程。项目当前是 Godot 4.6 C# / .NET 8.0 工程，程序集名为 `tower`。

## 1. 项目定位

`Four-Dimensional` 是一款回合制 RPG / 卡牌构筑 roguelite。玩家选择角色组成队伍，在节点地图上推进，并在战斗中通过抽牌、出牌、技能效果、Buff、遗物和装备形成战术循环。

核心体验参考：

- `Slay the Spire` 式卡牌池、抽牌堆、弃牌堆、消耗牌堆与奖励选择。
- 队伍制角色构筑，每名角色有自身技能池、属性、被动和定位。
- 地图节点推进，包含普通战斗、精英、Boss、事件、商店、休息、宝箱等节点。
- 行动点 / 速度阈值系统，不是严格的传统轮流行动。

## 2. 开发环境

必需环境：

- Godot 4.6 Mono / .NET 版。
- .NET SDK 8.0。
- Windows PowerShell，用于运行项目内脚本。

常用命令：

```powershell
dotnet build tower.sln
dotnet run --project tools/SkillIdGenerator/SkillIdGenerator.csproj -- check
dotnet run --project tools/SkillIdGenerator/SkillIdGenerator.csproj -- generate
```

运行方式：

- Godot 编辑器打开项目根目录。
- 主场景为 `res://PreloadeScene.tscn`。
- 编辑器内按 F5 运行。
- 也可以在安装了 Godot 命令行后执行 `godot --path .`。

注意：

- `tower.csproj` 使用 `Godot.NET.Sdk/4.6.0`，并在构建前自动运行 `tools/SkillIdGenerator`。
- `.vscode` 中可能仍指向旧 Godot 版本路径；以 `tower.csproj` 和实际 Godot 4.6 Mono 为准。
- 目前仓库没有正式测试项目，提交前至少运行 `dotnet build tower.sln`。

## 3. 启动流程

Godot 配置位于 `project.godot`：

- 项目名：`Four-Dimensional`
- 主场景：`res://PreloadeScene.tscn`
- 自动加载：
  - `AudioManager`：`res://Audio/AudioManager.tscn`
  - `MouseTrail`：鼠标拖尾效果

启动相关文件：

- `PreloadeScene.cs` / `PreloadeScene.tscn`：预加载、全局初始化、进入标题界面。
- `SceneTransitionLayer.cs`：场景切换表现。
- `BeginGame/StartInterface.cs`：标题页、继续游戏、新游戏入口。
- `BeginGame/CharacterSelectionOverlay.cs`：新游戏角色选择、种子、难度与开局设置。

典型流程：

1. `PreloadeScene` 设置语言、加载必要资源和 UI 层。
2. 标题界面检查自动存档状态。
3. 新游戏进入角色选择；继续游戏读取 `SaveSystem`。
4. 新游戏初始化 `GameInfo.InitNewGame()`，随后进入地图。

## 4. 目录职责

主要代码目录：

- `battle/`：战斗主循环、手牌 UI、牌堆、目标选择、奖励、战斗 VFX。
- `battle/buff/`：Buff 基类、各类触发时机与状态图标。
- `character/`：角色基类、玩家角色、敌人、召唤物、脚底标记等。
- `character/SkillBase/`：技能基类、技能注册、技能计划 DSL、技能 ID、稀有度、数值读取。
- `Map/`：地图、节点、镜头、路线、调试控制台、地图 UI。
- `Event/`：事件、休息、宝箱、开局奖励、Boss 遗物选择等。
- `Shop/`：空间站商店。
- `Relic/`：遗物系统。
- `Equipment/`：装备与卡槽。
- `ConsumeItems/`：消耗品与目标选择。
- `Menu/`：菜单、设置、百科。
- `Audio/`：全局音频管理器与音效资源。
- `localization/`：本地化加载与辅助函数。
- `data/`：技能与敌人数值 JSON。
- `asset/`：美术资源。
- `shader/`：Godot shader。
- `scripts/`：项目脚本，包含数值导出、图片生成辅助、日志查看等。
- `tools/`：开发工具，目前包含技能 ID 生成器。

根目录关键文件：

- `GameInfo` 位于 `character/PlayerCharacter/GameInfo*.cs`，是单局运行的静态状态中心。
- `SaveSystem.cs` 负责自动存档和读档。
- `UserSettings.cs` 负责用户设置。
- `tower.csproj` 定义构建参数和自动生成目标。

## 5. 编码约定

项目采用手工格式化，没有 `.editorconfig` / StyleCop / CSharpier。

遵循现有风格：

- Godot 脚本类使用 `public partial class`。
- 类型、公开成员、方法使用 PascalCase。
- 局部变量使用 camelCase。
- 私有字段使用 `_camelCase`。
- 懒加载节点常用 `field ??= GetNode<T>("Path")`。
- 操作 Godot 节点前经常使用 `GodotObject.IsInstanceValid()`。
- 异步流程使用 `Task`、`await`、`ToSignal`。
- UI 文本、描述和提示大量使用 BBCode。
- 不要把 `Charater` 目录和已有命名改成 `Character`，项目中这个拼写已经成为路径约定。

新增代码时优先贴合附近文件的写法，不要顺手重构无关模块。

## 6. 全局状态与存档

`GameInfo` 是单局状态中心，拆分在多个 partial 文件里：

- `GameInfo.cs`：基础资源、生命、层数、遗物、道具、运行记录。
- `GameInfo.PlayerCharacters.cs`：玩家角色快照与队伍状态。
- `GameInfo.RunRng.cs`：随机数与种子。
- `GameInfo.NormalEncounterQueue.cs` / `GameInfo.EliteEncounterQueue.cs`：遭遇队列。
- `GameInfo.RelicQueue.cs`：遗物队列。
- `GameInfo.Talents.cs`：天赋。
- `GameInfo.StarterBonus.cs`：开局增益。

存档系统：

- 存档路径：`user://autosave.cfg`。
- `SaveSystem.SaveRunCheckpoint()` 用于节点完成、区域切换等检查点。
- `SaveSystem.SaveAllInBackground()` 后台写入。
- `SaveSystem.LoadAll()` 从 Godot `ConfigFile` 读取并恢复 `GameInfo` 静态字段。
- 新增需要持久化的运行状态时，优先放入 `GameInfo` 的 public static 非 readonly 字段，并确认其类型能被 `SaveSystem` 序列化。

注意：

- `SaveSystem.SyncDerivedRunState()` 会同步队伍生命到 `TransitionEnergy`。
- 读档后会调用若干 Normalize / Ensure 方法修复旧档或缺省状态。
- 修改存档结构后，要用新游戏和旧自动存档各跑一次。

## 7. 战斗系统

核心文件：

- `battle/Battle.cs`：战斗节点、角色列表、战斗随机数、战斗结束判定、镜头与打击反馈。
- `battle/Battle.NonGameplay.cs`：非核心战斗逻辑和辅助流程。
- `battle/CharacterControl*.cs`：手牌、出牌区域、牌堆覆盖层、目标选择、手牌布局、抽弃牌动画。
- `battle/UIScene/`：技能按钮、奖励、提示、GameOver、手动目标 UI。
- `character/Character.cs`：角色通用属性、生命、格挡、能量、Buff 列表、受击与表现。
- `character/EnemyCharacter/EnemyScript/EnemyCharacter.cs`：敌人行为基类。
- `character/SummonCharacter.cs`：召唤物。

战斗中的关键概念：

- 玩家队伍和敌方分别维护角色列表与召唤物列表。
- 每个角色有 `Skills = new Skill[3]` 的基础技能槽，同时战斗牌堆使用 `SkillID` 管理具体卡。
- 玩家能量在 `Battle.PlayerEnergy`，角色可通过 `CurrentEnergy` 读取当前能量来源。
- Buff 按触发时机拆成多个列表：`StartActionBuffs`、`EndActionBuffs`、`AttackBuffs`、`HurtBuffs`、`SpecialBuffs`、`SkillBuffs`、`DyingBuffs`。
- Buff 触发时经常需要遍历快照，避免 Buff 在触发中移除自己导致集合修改异常。
- 许多场景有 `[Export] public bool WarmupMode`，用于 UI 预览或资源预热时禁用真实逻辑。

## 8. 技能系统

技能相关文件：

- `character/SkillBase/Skill.cs`：技能基类、费用、类型、描述、效果入口。
- `character/SkillBase/Skill.SSOT.cs`：技能计划 DSL、目标引用、效果步骤和描述单一事实源。
- `character/SkillBase/Skill.Registry.cs`：通过反射将技能实现类绑定到 `SkillID`。
- `character/SkillBase/SkillID.Generated.cs`：自动生成的技能枚举。
- `character/SkillBase/Skill.PlayerCatalog.cs`：玩家角色技能池。
- `character/SkillBase/Skill.ColorlessCatalog.cs`：无色技能池。
- `character/SkillBase/Skill.Rarity.cs`：技能稀有度。
- `character/SkillBase/Skill.Tuning.cs` / `SkillTuning.cs`：技能数值表读取。
- `tools/skill-id-registry.json`：技能 ID 注册源数据。
- `tools/SkillIdGenerator/`：技能 ID 生成器。

技能实现位置：

- 通用基础技能：`character/SkillBase/Skill.Basic.cs`
- 无色技能：`character/SkillBase/Colorless/ColorlessSkill.cs`
- Echo：`character/PlayerCharacter/Echo/*Skill.cs`
- Kasiya：`character/PlayerCharacter/Kasiya/*Skill.cs`
- Mariya：`character/PlayerCharacter/Mariya/*Skill.cs`
- Nightingale：`character/PlayerCharacter/Nightingale/*Skill.cs`

新增技能流程：

1. 在对应角色技能文件或无色技能文件中新增 `public class Xxx : Skill` 或 `public class Xxx : ColorlessSkill`。
2. 设置 `SkillType`、`EnergyCost`、稀有度相关信息和描述。
3. 优先用 `BuildPlan()` 返回 `new SkillPlan(...)`，让效果和预览描述共享同一套步骤。
4. 若类名与枚举名一致，`Skill.Registry` 可自动匹配；不一致时使用 `[SkillDefinition(SkillID.Xxx)]` 或更新注册覆盖。
5. 运行 `dotnet run --project tools/SkillIdGenerator/SkillIdGenerator.csproj -- generate`。
6. 检查 `character/SkillBase/SkillID.Generated.cs` 与 `tools/skill-id-registry.json`。
7. 添加或更新 `data/skill_tuning/...json` 中的数值。
8. 添加本地化到 `localization/zh_CN.json` 和 `localization/en.json`。
9. 添加卡图到 `asset/CardPicture/{CharacterName}/{SkillId}.jpg` 或对应目录。
10. 运行 `dotnet build tower.sln`，再进游戏检查奖励、百科、战斗出牌和描述。

技能 ID 注意事项：

- 不要手改 `SkillID.Generated.cs`，它是生成文件。
- 构建默认会自动运行生成器。
- 若只想检查注册一致性，运行生成器的 `check` 命令。
- 旧技能名可以通过 `SkillIdAliases` 或 `TypeNameOverrides` 兼容。

## 9. 数值配置

技能数值：

- 目录：`data/skill_tuning/`
- 项目内默认目录：`res://data/skill_tuning`
- 用户覆盖目录：`user://skill_tuning`
- 兼容旧路径：`res://data/skill_tuning.dev.json` 和 `user://skill_tuning.dev.json`
- 读取入口：`SkillTuning.TryGetInt(...)`

敌人数值：

- 目录：`data/enemy_tuning/`
- 项目内默认目录：`res://data/enemy_tuning`
- 用户覆盖目录：`user://enemy_tuning`
- 兼容旧路径：`res://data/enemy_tuning.dev.json` 和 `user://enemy_tuning.dev.json`
- 读取入口：`EnemyTuning.TryGetInt(...)`

JSON 约定：

- 根节点可以直接是对象，也可以包一层 `skills` 或 `enemies`。
- 数值读取大小写不敏感。
- 支持注释和尾随逗号。
- 项目目录值先加载，用户目录值后加载；后加载的值会覆盖前面的同名 key。

常用脚本：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/export_skill_tuning_from_source.ps1
```

## 10. 敌人系统

关键文件：

- `character/EnemyRegedit.cs`：敌人注册 / 配置基类。
- `character/EnemyCharacter/EnemyScript/`：敌人行为脚本。
- `character/EnemyCharacter/`：敌人场景与配置。
- `data/enemy_tuning/enemy/*.json`：敌人数值覆盖。
- `data/skill_tuning/enemy/*.json`：敌方技能数值。

新增敌人建议流程：

1. 创建敌人脚本，继承项目现有敌人基类或参考同类敌人。
2. 创建 / 复制敌人场景，保持节点路径与脚本期望一致。
3. 创建或更新 `EnemyRegedit` 配置。
4. 把敌人加入普通、精英或 Boss 遭遇队列。
5. 添加 `data/enemy_tuning/enemy/{enemy_key}.json`。
6. 若敌人技能走技能系统，添加 `data/skill_tuning/enemy/{enemy_key}.json`。
7. 添加敌人图像到 `asset/EnemyCharater/`。
8. 添加本地化名称、百科描述和必要图标。
9. 从地图节点进入战斗验证意图、行动、奖励和死亡结算。

## 11. 地图与节点

关键文件：

- `Map/Map.cs` / `Map/Map.tscn`：地图主界面。
- `Map/DynamicCamera.cs`：地图相机。
- `Map/Site/LevelNode.cs`：节点逻辑。
- `Map/Site/NormalBattleEncounter.cs`：普通战斗遭遇。
- `Map/Site/EliteBattleEncounter.cs`：精英战斗遭遇。
- `Map/Site/Practice.cs`：练习或测试节点。
- `Map/UI/PlayerResourceState.cs`：地图资源状态。
- `Map/DebugConsole.cs`：地图调试控制台。

节点类型通常包括：

- 普通战斗
- 精英战斗
- Boss
- 事件
- 商店
- 休息
- 宝箱

新增节点或改地图推进时，要同步考虑：

- 节点显示、可达性和完成状态。
- 是否写入 `GameInfo.LevelNodeHistory`。
- 是否触发 `SaveSystem.SaveRunCheckpoint()`。
- 是否影响区域推进、Boss 遗物、奖励或地图 UI。
- 是否需要百科 / 统计界面显示。

## 12. 事件、奖励、商店

事件系统：

- `Event/GameEvent.cs`：事件数据和标准事件入口。
- `Event/EventInterface.cs`：事件 UI、选项、结算。
- `Event/TargetSelectOverlay.cs`：事件目标选择。

奖励系统：

- `battle/UIScene/Reward/Reward.cs`：战斗奖励主逻辑。
- `battle/UIScene/Reward/SkillCard.cs`：技能卡展示。
- 奖励掉落状态在 `GameInfo.BattleRewardDrops.cs`。

商店：

- `Shop/SpaceStationShop.cs`：商店界面和购买流程。
- `Shop/StatCharacterPanel.cs`：角色状态展示。

新增奖励或事件时，检查：

- 是否影响 `GameInfo`。
- 是否需要自动存档。
- 是否需要历史记录统计。
- 是否需要本地化。
- 是否能在 UI 中重复进入 / 返回而不丢状态。

## 13. 遗物、装备、道具

遗物：

- `Relic/RelicBase.cs`：遗物定义和触发。
- `Relic/RelicIcon.cs`：遗物图标。
- `Relic/BossRelicChoice.cs`：Boss 遗物选择。
- `GameInfo.RelicQueue.cs`：遗物队列与抽取状态。

装备：

- `Equipment/Equipment.cs`：装备数据。
- `Equipment/Equipment.SpecialEffects.cs`：装备特殊效果。
- `Equipment/InventoryGrid.cs`：装备格子。
- `Equipment/CardSlot.cs`：装备卡槽。

道具：

- `ConsumeItems/ConsumeItem.cs`：消耗品定义与使用。
- `ConsumeItems/AimTarget.cs`：目标选择。

新增可获得物时，检查：

- 是否要进入奖励池 / 商店池。
- 是否需要存档序列化。
- 是否需要百科展示。
- 是否需要图标、名称、描述、本地化。
- 是否在战斗、地图、事件三个上下文都能正确刷新 UI。

## 14. UI 与本地化

本地化文件：

- `localization/I18n.cs`
- `localization/zh_CN.json`
- `localization/en.json`
- `localization/LocalizationHelper.cs`

使用方式：

```csharp
I18n.Tr("ui.common.confirm", "确认");
I18n.Format("ui.map.seed", "Seed: {value}", ("value", GameInfo.Seed));
```

约定：

- 中文是主要开发语言，但新增 UI 文本应同时补 `zh_CN.json` 和 `en.json`。
- key 使用点分层，例如 `ui.map.seed`、`skill.echo_form.name`。
- 技能名默认 key 为 `skill.{snake_case_class_name}.name`。
- 角色名默认 key 为 `character.{snake_case_class_name}.name`。
- Buff 名默认 key 为 `buff.{snake_case_buff_name}.name`。
- fallback 文本只用于兜底，不能替代本地化文件。

UI 开发注意：

- Godot 节点路径变化会直接影响 `GetNode<T>("...")`，改场景结构后同步改脚本。
- 大量 tooltip 使用 BBCode，修改描述时要检查标签闭合。
- 设置项变更后调用相关刷新方法，例如 `AudioManager.RefreshSettings()`。

## 15. 美术、卡图和音频

卡图路径：

- 玩家技能：`asset/CardPicture/{CharacterName}/{SkillId}.jpg`
- 无色技能：`asset/CardPicture/Colorless/{SkillId}.jpg`
- 状态牌：`asset/CardPicture/Status/{SkillId}.jpg`

项目曾使用 png，现在大量卡图改为 jpg。新增资源时先看同目录现状，保持一致。

AI 图片流程：

- 参考 `docs/IMAGE2_USAGE.md`。
- 角色、敌人、Boss 立绘必须先查看项目内参考图。
- 生成后的正式资源必须复制到项目目录，不要只留在 Codex 生成目录。
- 不要覆盖已有正式图，除非明确要求。

音频：

- `Audio/AudioManager.cs` 是全局音效入口。
- 音效枚举为 `AudioCue`。
- 常用调用如 `AudioManager.PlayUiClick()`、`AudioManager.PlayAttack()`。
- 新增 cue 时要同时更新枚举、场景里的播放器节点绑定、资源和设置音量逻辑。

## 16. 调试

常用方式：

- Godot 编辑器运行主场景。
- 使用地图调试控制台 `Map/DebugConsole.cs`。
- 使用 `scripts/show_godot_log.ps1` 查看 Godot 日志。
- 特定手牌预览可看 `tools/HandPreviewDebugRunner.tscn` 和 `battle/HandPreviewDebugRunner.cs`。

常用检查：

```powershell
dotnet build tower.sln
dotnet run --project tools/SkillIdGenerator/SkillIdGenerator.csproj -- check
powershell -ExecutionPolicy Bypass -File scripts/show_godot_log.ps1
```

因为没有自动测试，复杂改动至少手动验证：

- 新游戏能进入角色选择。
- 能从地图进入战斗。
- 出牌、目标选择、回合推进正常。
- 战斗胜利奖励正常。
- 地图节点完成后能继续推进。
- 自动存档能继续游戏。
- 设置、百科、统计界面不报错。

## 17. 提交前清单

代码类改动：

- `dotnet build tower.sln` 通过。
- 没有手改生成文件，除非生成器刚刚更新。
- 没有误改 `Charater` 路径拼写。
- 新增 public static 运行状态已考虑存档兼容。
- Godot 场景节点路径和 C# `GetNode` 路径一致。

技能 / 卡牌改动：

- 技能 ID 已生成并检查。
- 技能数值 JSON 已更新。
- 中文和英文文案已补齐。
- 卡图路径正确。
- 奖励池、百科、战斗预览都能看到。

敌人 / 遭遇改动：

- 敌人数值和技能数值已配置。
- 遭遇队列能抽到目标敌人。
- 意图、行动、死亡、奖励结算正常。
- 百科和本地化已更新。

资源改动：

- `.import` 文件已由 Godot 正常生成。
- 第三方资源归属写入文档，例如 `docs/GAME_ICONS_ATTRIBUTION.md`。
- 没有把临时生成素材误接到正式场景。

## 18. 常见坑

- `SkillID.Generated.cs` 是生成物；真正的来源是技能类和 `tools/skill-id-registry.json`。
- `SaveSystem` 会序列化 `GameInfo` 的 public static 字段，新增复杂类型前要确认可序列化。
- 很多 Buff 触发时会修改 Buff 列表，遍历时优先用快照。
- `WarmupMode` 场景不应执行真实战斗、奖励或存档逻辑。
- 修改场景节点名后，C# 的懒加载节点路径很容易失效。
- 本地化 fallback 不能保证英文环境质量，新增 UI 时要补 JSON。
- 资源目录中 `Charater` 是历史拼写，不要批量改名。
- 卡图和立绘的生成流程已有约束，尤其角色身份和敌人绿幕抠图不要临时发挥。

## 19. 推荐阅读顺序

第一次接手建议按这个顺序看代码：

1. `project.godot`
2. `PreloadeScene.cs`
3. `BeginGame/StartInterface.cs`
4. `BeginGame/CharacterSelectionOverlay.cs`
5. `character/PlayerCharacter/GameInfo.cs`
6. `Map/Map.cs`
7. `Map/Site/LevelNode.cs`
8. `battle/Battle.cs`
9. `battle/CharacterControl.cs`
10. `character/Character.cs`
11. `character/SkillBase/Skill.cs`
12. `character/SkillBase/Skill.SSOT.cs`
13. 一个具体角色技能文件，例如 `character/PlayerCharacter/Echo/EchoAttackSkill.cs`
14. `SaveSystem.cs`
15. `localization/I18n.cs`

读完这些文件，基本可以定位一次完整流程：启动、选人、进地图、进战斗、出牌、结算、保存。
