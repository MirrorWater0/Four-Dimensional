using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public sealed class LevelNodeCompletionRecord
{
    public int CompletionOrder;
    public long CompletedAtUtcTicks;
    public int MapLevel;
    public Vector2I Coordinate;
    public LevelNode.LevelType NodeType;
    public int RandomNum;
    public List<string> EnemyNames = new();
    public int EnemyDefeatCount;
    public int ElectricityCoinChange;
    public int ElectricityCoinGained;
    public int TransitionEnergyChange;
    public List<string> SkillChanges = new();
    public List<string> GainedItems = new();
    public List<string> ConsumedItems = new();
    public List<string> EquipmentChanges = new();
    public int EquipmentGainedCount;
    public List<string> RelicChanges = new();
    public int RelicGainedCount;
    public List<LevelNodePropertyChangeRecord> PermanentPropertyChanges = new();
    public int NextBattleItemDropChance;
    public int NextBattleEquipmentDropChance;
    public List<string> Notes = new();
    public string Summary;
}

public sealed class LevelNodePropertyChangeRecord
{
    public string CharacterName;
    public int PowerChange;
    public int SurvivabilityChange;
    public int SpeedChange;
    public int MaxLifeChange;
}

public sealed class RunHistoryRecord
{
    public int RunIndex;
    public bool Victory;
    public int Seed;
    public int Difficulty;
    public long StartedAtUtcTicks;
    public long EndedAtUtcTicks;
    public long SessionPlaySeconds;
    public int MapLevel;
    public int NodesVisited;
    public int EnemiesDefeated;
    public int EliteDefeated;
    public int BossDefeated;
    public int ElectricityCoinGained;
    public int EquipmentGained;
    public int RelicGained;
    public List<LevelNodeCompletionRecord> NodeRecords = new();
    public List<RunHistoryRelicRecord> RelicRecords = new();
    public List<RunHistoryEquipmentRecord> EquipmentRecords = new();
    public List<RunHistoryCharacterSkillRecord> CharacterSkillRecords = new();
}

public sealed class RunHistoryRelicRecord
{
    public RelicID RelicID;
    public string RelicName;
    public int Count;
}

public sealed class RunHistoryEquipmentRecord
{
    public Equipment.EquipmentName EquipmentName;
    public string DisplayName;
    public string TypeLabel;
    public int Power;
    public int Survivability;
    public int Speed;
    public int MaxLife;
    public string Description;
    public int Count;
}

public sealed class RunHistorySkillTypeRecord
{
    public Skill.SkillTypes SkillType;
    public List<string> SkillNames = new();
    public List<SkillID> SkillIds = new();
}

public sealed class RunHistoryCharacterSkillRecord
{
    public string CharacterName;
    public List<RunHistorySkillTypeRecord> SkillTypeRecords = new();
}

public static partial class GameInfo
{
    public static Dictionary<Vector2I, LevelNodeCompletionRecord> CompletedLevelNodeRecords =
        new();
    public static int CompletedLevelNodeRecordOrder;

    private sealed class ActiveLevelNodeSnapshot
    {
        public Vector2I Coordinate;
        public LevelNode.LevelType NodeType;
        public int RandomNum;
        public List<string> EnemyNames = new();
        public int ElectricityCoin;
        public int TransitionEnergy;
        public Dictionary<RelicID, int> Relics = new();
        public List<ItemID> Items = new();
        public List<string> OwnedEquipmentNames = new();
        public List<HashSet<SkillID>> PlayerGainedSkills = new();
        public PlayerPropertySnapshot[] PlayerProperties = Array.Empty<PlayerPropertySnapshot>();

        public static ActiveLevelNodeSnapshot Capture(LevelNode node)
        {
            return new ActiveLevelNodeSnapshot
            {
                Coordinate = node?.SelfCoordinate ?? Vector2I.Zero,
                NodeType = node?.Type ?? LevelNode.LevelType.Normal,
                RandomNum = node?.RandomNum ?? 0,
                EnemyNames = CaptureEnemyNames(node),
                ElectricityCoin = GameInfo.ElectricityCoin,
                TransitionEnergy = GameInfo.TransitionEnergy,
                Relics = CloneRelicMap(GameInfo.Relics),
                Items = CloneItemList(GameInfo.Items),
                OwnedEquipmentNames = CaptureOwnedEquipmentNames(GameInfo.OwnedEquipments),
                PlayerGainedSkills = CapturePlayerGainedSkills(GameInfo.PlayerCharacters),
                PlayerProperties = CapturePlayerProperties(GameInfo.PlayerCharacters),
            };
        }

        public LevelNodeCompletionRecord BuildRecord(LevelNode node)
        {
            var record = new LevelNodeCompletionRecord
            {
                Coordinate = node?.SelfCoordinate ?? Coordinate,
                NodeType = node?.Type ?? NodeType,
                RandomNum = node?.RandomNum ?? RandomNum,
                EnemyNames = CaptureEnemyNames(node),
                ElectricityCoinChange = GameInfo.ElectricityCoin - ElectricityCoin,
                TransitionEnergyChange = GameInfo.TransitionEnergy - TransitionEnergy,
                SkillChanges = BuildSkillChanges(PlayerGainedSkills, GameInfo.PlayerCharacters),
                GainedItems = BuildPositiveItemChanges(Items, GameInfo.Items),
                ConsumedItems = BuildNegativeItemChanges(Items, GameInfo.Items),
                EquipmentChanges = BuildEquipmentChanges(
                    OwnedEquipmentNames,
                    GameInfo.OwnedEquipments
                ),
                RelicChanges = BuildRelicChanges(Relics, GameInfo.Relics),
                PermanentPropertyChanges = BuildPropertyChanges(
                    PlayerProperties,
                    GameInfo.PlayerCharacters
                ),
                EquipmentGainedCount = CountEquipmentGained(
                    OwnedEquipmentNames,
                    GameInfo.OwnedEquipments
                ),
                RelicGainedCount = CountRelicsGained(Relics, GameInfo.Relics),
                NextBattleItemDropChance = GameInfo.BattleItemDropChance,
                NextBattleEquipmentDropChance = GameInfo.BattleEquipmentDropChance,
            };

            if (record.EnemyNames.Count == 0 && EnemyNames.Count > 0)
                record.EnemyNames = new List<string>(EnemyNames);

            record.EnemyDefeatCount = IsBattleNode(record.NodeType)
                ? record.EnemyNames?.Count ?? 0
                : 0;
            record.ElectricityCoinGained = Math.Max(0, record.ElectricityCoinChange);

            return record;
        }
    }

    private sealed class PlayerPropertySnapshot
    {
        public string CharacterName;
        public int Power;
        public int Survivability;
        public int Speed;
        public int MaxLife;
    }

    private static ActiveLevelNodeSnapshot _activeLevelNodeSnapshot;

    public static void ResetLevelNodeCompletionRecords()
    {
        CompletedLevelNodeRecords ??= new Dictionary<Vector2I, LevelNodeCompletionRecord>();
        CompletedLevelNodeRecords.Clear();
        CompletedLevelNodeRecordOrder = 0;
        _activeLevelNodeSnapshot = null;
    }

    public static RunHistoryRecord RecordCurrentRunHistory(bool victory, bool includeCurrentNode = true)
    {
        RunHistoryRecords ??= new List<RunHistoryRecord>();
        if (RunFinished && RunHistoryRecords.Count > 0)
            return RunHistoryRecords[^1];

        var record = BuildCurrentRunHistoryRecord(victory, includeCurrentNode);
        record.RunIndex = RunHistoryRecords.Count + 1;
        RunHistoryRecords.Add(record);
        RunFinished = true;
        return record;
    }

    public static RunHistoryRecord GetLatestRunHistoryRecord()
    {
        RunHistoryRecords ??= new List<RunHistoryRecord>();
        return RunHistoryRecords.Count > 0 ? RunHistoryRecords[^1] : null;
    }

    public static string BuildRunHistoryRecordText(RunHistoryRecord record)
    {
        if (record == null)
            return "暂无本局结算记录。";

        var sb = new StringBuilder(4096);
        AppendRunNodeSection(sb, record);
        AppendRunRelicSection(sb, record);
        AppendRunEquipmentSection(sb, record);
        AppendRunSkillSection(sb, record);
        return sb.ToString();
    }

    public static string BuildRunHistoryStatisticsText(int recentLimit = 8)
    {
        RunHistoryRecords ??= new List<RunHistoryRecord>();
        var records = RunHistoryRecords.Where(record => record != null).ToArray();
        if (records.Length == 0)
            return "暂无历史游戏记录。\n本局结束后会在这里留下结算。";

        var sb = new StringBuilder(8192);
        sb.Append(BuildRunHistoryRecordText(records[^1]));

        sb.Append("\n\n[b]历史汇总[/b]");
        sb.Append($"\n历史局数：{records.Length}");
        sb.Append($"\n战败次数：{records.Count(record => !record.Victory)}");
        sb.Append($"\n累计经历节点：{records.Sum(record => record.NodesVisited)}");
        sb.Append($"\n累计击败敌人：{records.Sum(record => record.EnemiesDefeated)}");
        sb.Append($"\n累计击败精英：{records.Sum(record => record.EliteDefeated)}");
        sb.Append($"\n累计击败Boss：{records.Sum(record => record.BossDefeated)}");
        sb.Append($"\n累计获得电力币：{records.Sum(record => record.ElectricityCoinGained)}");
        sb.Append($"\n累计获得装备：{records.Sum(record => record.EquipmentGained)}");
        sb.Append($"\n累计获得遗物：{records.Sum(record => record.RelicGained)}");
        AppendHistoryNodeTotals(sb, records);
        AppendHistorySkillTotals(sb, records);

        if (records.Length > 1)
        {
            sb.Append("\n\n[b]历史记录[/b]");
            foreach (var record in records.Reverse().Skip(1).Take(Math.Max(0, recentLimit - 1)))
            {
                sb.Append("\n");
                sb.Append(BuildRunHistoryRecordBriefText(record));
            }
        }

        return sb.ToString();
    }

    private static RunHistoryRecord BuildCurrentRunHistoryRecord(
        bool victory,
        bool includeCurrentNode
    )
    {
        long now = DateTime.UtcNow.Ticks;
        var records =
            CompletedLevelNodeRecords?.Values
                .Where(record => record != null)
                .OrderBy(record => record.CompletionOrder)
                .ToArray()
            ?? Array.Empty<LevelNodeCompletionRecord>();
        var nodeRecords = BuildRunNodeRecords(records, includeCurrentNode);

        return new RunHistoryRecord
        {
            Victory = victory,
            Seed = Seed,
            Difficulty = GameInfo.Difficulty,
            StartedAtUtcTicks = RunStartedAtUtcTicks == 0 ? now : RunStartedAtUtcTicks,
            EndedAtUtcTicks = now,
            SessionPlaySeconds = Math.Max(0, SessionPlaySeconds),
            MapLevel = CurrentLevel,
            NodesVisited = nodeRecords.Count,
            EnemiesDefeated = records.Sum(record => record.EnemyDefeatCount),
            EliteDefeated = records.Count(record => record.NodeType == LevelNode.LevelType.Elite),
            BossDefeated = records.Count(record => record.NodeType == LevelNode.LevelType.Boss),
            ElectricityCoinGained = records.Sum(record => record.ElectricityCoinGained),
            EquipmentGained = records.Sum(record => record.EquipmentGainedCount),
            RelicGained = records.Sum(record => record.RelicGainedCount),
            NodeRecords = nodeRecords,
            RelicRecords = BuildRunRelicRecords(GameInfo.Relics),
            EquipmentRecords = BuildRunEquipmentRecords(
                GameInfo.OwnedEquipments,
                GameInfo.PlayerCharacters
            ),
            CharacterSkillRecords = BuildRunCharacterSkillRecords(GameInfo.PlayerCharacters),
        };
    }

    private static List<LevelNodeCompletionRecord> BuildRunNodeRecords(
        LevelNodeCompletionRecord[] completedRecords,
        bool includeCurrentNode
    )
    {
        var result = new List<LevelNodeCompletionRecord>();
        if (completedRecords != null)
        {
            foreach (var record in completedRecords)
            {
                var clone = CloneLevelNodeCompletionRecord(record);
                if (clone != null)
                    result.Add(clone);
            }
        }

        if (includeCurrentNode && _activeLevelNodeSnapshot != null)
        {
            var activeRecord = _activeLevelNodeSnapshot.BuildRecord(null);
            activeRecord.CompletionOrder = result.Count + 1;
            activeRecord.CompletedAtUtcTicks = DateTime.UtcNow.Ticks;
            activeRecord.MapLevel = CurrentLevel;
            activeRecord.Notes ??= new List<string>();
            activeRecord.Notes.Add("本节点未完成。");
            activeRecord.Summary = BuildLevelNodeSummary(activeRecord);
            result.Add(activeRecord);
        }

        return result;
    }

    private static LevelNodeCompletionRecord CloneLevelNodeCompletionRecord(
        LevelNodeCompletionRecord source
    )
    {
        if (source == null)
            return null;

        return new LevelNodeCompletionRecord
        {
            CompletionOrder = source.CompletionOrder,
            CompletedAtUtcTicks = source.CompletedAtUtcTicks,
            MapLevel = source.MapLevel,
            Coordinate = source.Coordinate,
            NodeType = source.NodeType,
            RandomNum = source.RandomNum,
            EnemyNames = CopyStringList(source.EnemyNames),
            EnemyDefeatCount = source.EnemyDefeatCount,
            ElectricityCoinChange = source.ElectricityCoinChange,
            ElectricityCoinGained = source.ElectricityCoinGained,
            TransitionEnergyChange = source.TransitionEnergyChange,
            SkillChanges = CopyStringList(source.SkillChanges),
            GainedItems = CopyStringList(source.GainedItems),
            ConsumedItems = CopyStringList(source.ConsumedItems),
            EquipmentChanges = CopyStringList(source.EquipmentChanges),
            EquipmentGainedCount = source.EquipmentGainedCount,
            RelicChanges = CopyStringList(source.RelicChanges),
            RelicGainedCount = source.RelicGainedCount,
            PermanentPropertyChanges = ClonePropertyChangeRecords(source.PermanentPropertyChanges),
            NextBattleItemDropChance = source.NextBattleItemDropChance,
            NextBattleEquipmentDropChance = source.NextBattleEquipmentDropChance,
            Notes = CopyStringList(source.Notes),
            Summary = source.Summary,
        };
    }

    private static List<string> CopyStringList(List<string> source) =>
        source == null ? new List<string>() : new List<string>(source);

    private static List<LevelNodePropertyChangeRecord> ClonePropertyChangeRecords(
        List<LevelNodePropertyChangeRecord> source
    )
    {
        var result = new List<LevelNodePropertyChangeRecord>();
        if (source == null)
            return result;

        foreach (var change in source)
        {
            if (change == null)
                continue;

            result.Add(
                new LevelNodePropertyChangeRecord
                {
                    CharacterName = change.CharacterName,
                    PowerChange = change.PowerChange,
                    SurvivabilityChange = change.SurvivabilityChange,
                    SpeedChange = change.SpeedChange,
                    MaxLifeChange = change.MaxLifeChange,
                }
            );
        }

        return result;
    }

    private static List<RunHistoryRelicRecord> BuildRunRelicRecords(
        Dictionary<RelicID, int> relics
    )
    {
        var result = new List<RunHistoryRelicRecord>();
        if (relics == null || relics.Count == 0)
            return result;

        foreach (var pair in relics.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal))
        {
            var relic = Relic.Create(pair.Key);
            result.Add(
                new RunHistoryRelicRecord
                {
                    RelicID = pair.Key,
                    RelicName = relic == null || string.IsNullOrWhiteSpace(relic.RelicName)
                        ? pair.Key.ToString()
                        : relic.RelicName,
                    Count = pair.Value,
                }
            );
        }

        return result;
    }

    private static List<RunHistoryEquipmentRecord> BuildRunEquipmentRecords(
        List<Equipment> ownedEquipments,
        PlayerInfoStructure[] players
    )
    {
        var equipmentCounts = new Dictionary<Equipment.EquipmentName, (Equipment Equipment, int Count)>();
        AddEquipmentRecords(equipmentCounts, ownedEquipments);

        if (players != null)
        {
            foreach (var player in players)
                AddEquipmentRecords(equipmentCounts, player.Equipments);
        }

        return equipmentCounts
            .OrderBy(pair => GetEquipmentDisplayName(pair.Value.Equipment), StringComparer.Ordinal)
            .Select(pair =>
            {
                var equipment = pair.Value.Equipment;
                return new RunHistoryEquipmentRecord
                {
                    EquipmentName = pair.Key,
                    DisplayName = GetEquipmentDisplayName(equipment),
                    TypeLabel = equipment?.TypeLabel ?? string.Empty,
                    Power = equipment?.Power ?? 0,
                    Survivability = equipment?.Survivability ?? 0,
                    Speed = equipment?.Speed ?? 0,
                    MaxLife = equipment?.MaxLife ?? 0,
                    Description = equipment?.Description ?? string.Empty,
                    Count = pair.Value.Count,
                };
            })
            .ToList();
    }

    private static void AddEquipmentRecords(
        Dictionary<Equipment.EquipmentName, (Equipment Equipment, int Count)> equipmentCounts,
        IEnumerable<Equipment> equipments
    )
    {
        if (equipmentCounts == null || equipments == null)
            return;

        foreach (var equipment in equipments)
        {
            if (equipment == null)
                continue;

            if (!equipmentCounts.TryGetValue(equipment.Name, out var entry))
            {
                equipmentCounts[equipment.Name] = (Equipment.Clone(equipment), 1);
                continue;
            }

            equipmentCounts[equipment.Name] = (entry.Equipment, entry.Count + 1);
        }
    }

    private static List<RunHistoryCharacterSkillRecord> BuildRunCharacterSkillRecords(
        PlayerInfoStructure[] players
    )
    {
        var result = new List<RunHistoryCharacterSkillRecord>();
        if (players == null || players.Length == 0)
            return result;

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            var grouped = new Dictionary<Skill.SkillTypes, List<SkillID>>();
            foreach (var skillId in (player.GainedSkills ?? new List<SkillID>()).Distinct())
            {
                var skill = Skill.GetSkill(skillId);
                if (skill == null || skill.SkillType == Skill.SkillTypes.none)
                    continue;

                if (!grouped.TryGetValue(skill.SkillType, out var skillIds))
                {
                    skillIds = new List<SkillID>();
                    grouped[skill.SkillType] = skillIds;
                }

                if (!skillIds.Contains(skillId))
                    skillIds.Add(skillId);
            }

            var characterRecord = new RunHistoryCharacterSkillRecord
            {
                CharacterName = GetPlayerName(player, i),
            };

            foreach (var skillType in GetDisplaySkillTypes())
            {
                if (!grouped.TryGetValue(skillType, out var skillIds) || skillIds.Count == 0)
                    continue;

                characterRecord.SkillTypeRecords.Add(
                    new RunHistorySkillTypeRecord
                    {
                        SkillType = skillType,
                        SkillNames = skillIds.Select(GetSkillDisplayName).ToList(),
                        SkillIds = new List<SkillID>(skillIds),
                    }
                );
            }

            result.Add(characterRecord);
        }

        return result;
    }

    private static Skill.SkillTypes[] GetDisplaySkillTypes() =>
        [Skill.SkillTypes.Attack, Skill.SkillTypes.Survive, Skill.SkillTypes.Special];

    private static void AppendRunNodeSection(StringBuilder sb, RunHistoryRecord record)
    {
        var nodes = record.NodeRecords ?? new List<LevelNodeCompletionRecord>();
        string result = record.Victory ? "胜利" : "战败";

        sb.Append("[b]节点路线[/b]");
        sb.Append($"\n本局结果：{result}");
        sb.Append($"\n游戏时长：{FormatRunDuration(record.SessionPlaySeconds)}");
        sb.Append($"\n种子：{record.Seed}");
        sb.Append($"\n难度：{record.Difficulty}");
        sb.Append($"\n经历节点：{record.NodesVisited}");
        sb.Append(
            $"\n击败：敌人 {record.EnemiesDefeated} / 精英 {record.EliteDefeated} / Boss {record.BossDefeated}"
        );
        sb.Append(
            $"\n获得：电力币 {record.ElectricityCoinGained} / 装备 {record.EquipmentGained} / 遗物 {record.RelicGained}"
        );

        sb.Append($"\n节点统计：{BuildNodeTypeSummary(nodes)}");
    }

    private static void AppendRunRelicSection(StringBuilder sb, RunHistoryRecord record)
    {
        var relics = record.RelicRecords ?? new List<RunHistoryRelicRecord>();
        sb.Append("\n\n[b]遗物[/b]");
        sb.Append($"\n总数：{CountRunRelics(relics)}");
    }

    private static void AppendRunEquipmentSection(StringBuilder sb, RunHistoryRecord record)
    {
        var equipments = record.EquipmentRecords ?? new List<RunHistoryEquipmentRecord>();
        sb.Append("\n\n[b]装备[/b]");
        sb.Append($"\n总数：{equipments.Sum(equipment => equipment.Count > 0 ? equipment.Count : 1)}");
    }

    private static void AppendRunSkillSection(StringBuilder sb, RunHistoryRecord record)
    {
        var characters = record.CharacterSkillRecords ?? new List<RunHistoryCharacterSkillRecord>();
        sb.Append("\n\n[b]角色技能[/b]");
        sb.Append($"\n总数：{CountRunSkills(characters)}");
    }

    private static void AppendHistoryNodeTotals(StringBuilder sb, RunHistoryRecord[] records)
    {
        var nodes = records
            .SelectMany(record => record.NodeRecords ?? new List<LevelNodeCompletionRecord>())
            .Where(node => node != null)
            .ToList();
        if (nodes.Count == 0)
            return;

        sb.Append($"\n累计节点记录：{nodes.Count}");
        sb.Append($"\n累计节点类型：{BuildNodeTypeSummary(nodes)}");
    }

    private static void AppendHistorySkillTotals(StringBuilder sb, RunHistoryRecord[] records)
    {
        int skillCount = records.Sum(record => CountRunSkills(record.CharacterSkillRecords));
        if (skillCount <= 0)
            return;

        sb.Append($"\n累计技能记录：{skillCount}");
    }

    private static string BuildRunHistoryRecordBriefText(RunHistoryRecord record)
    {
        if (record == null)
            return string.Empty;

        string index = record.RunIndex > 0 ? record.RunIndex.ToString() : "?";
        string result = record.Victory ? "胜利" : "战败";
        int relicCount = CountRunRelics(record.RelicRecords);
        int skillCount = CountRunSkills(record.CharacterSkillRecords);
        return $"#{index} {result}  难度 {record.Difficulty}  节点 {record.NodesVisited}  遗物 {relicCount}  技能 {skillCount}  {FormatRunDuration(record.SessionPlaySeconds)}";
    }

    private static string BuildNodeTypeSummary(IEnumerable<LevelNodeCompletionRecord> nodes)
    {
        var nodeList = nodes?.Where(node => node != null).ToList() ?? new List<LevelNodeCompletionRecord>();
        if (nodeList.Count == 0)
            return "无";

        LevelNode.LevelType[] order =
        [
            LevelNode.LevelType.Normal,
            LevelNode.LevelType.Elite,
            LevelNode.LevelType.Boss,
            LevelNode.LevelType.Event,
            LevelNode.LevelType.Shop,
        ];

        return string.Join(
            " / ",
            order.Select(type => $"{GetLevelTypeShortLabel(type)} {nodeList.Count(node => node.NodeType == type)}")
        );
    }

    private static string BuildCompactNodeRecordText(LevelNodeCompletionRecord node)
    {
        if (node == null)
            return string.Empty;

        var parts = new List<string>();
        if (node.ElectricityCoinChange != 0)
            parts.Add($"电力币 {FormatSigned(node.ElectricityCoinChange)}");
        if (node.TransitionEnergyChange != 0)
            parts.Add($"跃迁能量 {FormatSigned(node.TransitionEnergyChange)}");

        AddNodeRecordPart(parts, "技能", node.SkillChanges);
        AddNodeRecordPart(parts, "物品", node.GainedItems);
        AddNodeRecordPart(parts, "消耗", node.ConsumedItems);
        AddNodeRecordPart(parts, "装备", node.EquipmentChanges);
        AddNodeRecordPart(parts, "遗物", node.RelicChanges);

        if (node.PermanentPropertyChanges != null && node.PermanentPropertyChanges.Count > 0)
        {
            var propertyLines = node
                .PermanentPropertyChanges.Select(BuildPropertyChangeLine)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            AddNodeRecordPart(parts, "属性", propertyLines);
        }

        AddNodeRecordPart(parts, "备注", node.Notes);
        return parts.Count == 0 ? "无额外记录" : string.Join("；", parts);
    }

    private static void AddNodeRecordPart(List<string> parts, string label, List<string> values)
    {
        if (parts == null || values == null || values.Count == 0)
            return;

        string text = BuildJoinedText(values, "、");
        if (!string.IsNullOrWhiteSpace(text))
            parts.Add($"{label}：{text}");
    }

    private static string BuildJoinedText(List<string> values, string separator)
    {
        if (values == null || values.Count == 0)
            return string.Empty;

        return string.Join(separator, values.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static int CountRunRelics(List<RunHistoryRelicRecord> relics)
    {
        if (relics == null || relics.Count == 0)
            return 0;

        return relics.Where(relic => relic != null).Sum(relic => relic.Count > 0 ? relic.Count : 1);
    }

    private static int CountRunSkills(List<RunHistoryCharacterSkillRecord> characters)
    {
        if (characters == null || characters.Count == 0)
            return 0;

        return characters
            .Where(character => character != null)
            .Sum(character =>
                (character.SkillTypeRecords ?? new List<RunHistorySkillTypeRecord>())
                    .Where(typeRecord => typeRecord != null)
                    .Sum(typeRecord => typeRecord.SkillNames?.Count ?? 0)
            );
    }

    private static string GetLevelTypeShortLabel(LevelNode.LevelType type)
    {
        return type switch
        {
            LevelNode.LevelType.Normal => "普通",
            LevelNode.LevelType.Elite => "精英",
            LevelNode.LevelType.Boss => "Boss",
            LevelNode.LevelType.Event => "事件",
            LevelNode.LevelType.Shop => "商店",
            _ => "未知",
        };
    }

    private static string GetSkillTypeLabel(Skill.SkillTypes type)
    {
        return type switch
        {
            Skill.SkillTypes.Attack => "攻击",
            Skill.SkillTypes.Survive => "生存",
            Skill.SkillTypes.Special => "特殊",
            _ => "其它",
        };
    }

    public static void BeginLevelNodeTracking(LevelNode node)
    {
        if (node == null)
            return;

        if (_activeLevelNodeSnapshot?.Coordinate == node.SelfCoordinate)
            return;

        _activeLevelNodeSnapshot = ActiveLevelNodeSnapshot.Capture(node);
    }

    public static void CompleteLevelNodeTracking(LevelNode node)
    {
        if (node == null)
            return;

        var snapshot = _activeLevelNodeSnapshot;
        bool missingSnapshot = snapshot == null || snapshot.Coordinate != node.SelfCoordinate;
        if (missingSnapshot)
            snapshot = ActiveLevelNodeSnapshot.Capture(node);

        var record = snapshot.BuildRecord(node);
        record.CompletionOrder = ++CompletedLevelNodeRecordOrder;
        record.CompletedAtUtcTicks = DateTime.UtcNow.Ticks;
        record.MapLevel = CurrentLevel;
        if (missingSnapshot)
        {
            record.Notes.Add("完成时缺少进入节点前快照，本次记录仅保留最终状态可推导出的变化。");
        }

        record.Summary = BuildLevelNodeSummary(record);

        CompletedLevelNodeRecords ??= new Dictionary<Vector2I, LevelNodeCompletionRecord>();
        CompletedLevelNodeRecords[node.SelfCoordinate] = record;

        if (_activeLevelNodeSnapshot?.Coordinate == node.SelfCoordinate)
            _activeLevelNodeSnapshot = null;
    }

    public static bool IsRegionTwoUnlocked()
    {
        if (CurrentLevel > 0)
            return true;

        return CompletedLevelNodeRecords?.Values.Any(record => record?.NodeType == LevelNode.LevelType.Boss) == true;
    }

    public static string GetLevelNodeCompletionSummary(Vector2I coordinate)
    {
        if (
            CompletedLevelNodeRecords == null
            || !CompletedLevelNodeRecords.TryGetValue(coordinate, out var record)
            || record == null
        )
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(record.Summary))
            return record.Summary;

        record.Summary = BuildLevelNodeSummary(record);
        CompletedLevelNodeRecords[coordinate] = record;
        return record.Summary;
    }

    private static string BuildLevelNodeSummary(LevelNodeCompletionRecord record)
    {
        if (record == null)
            return string.Empty;

        var sb = new StringBuilder(256);
        sb.Append($"类型：{GetLevelTypeLabel(record.NodeType)}");

        if (record.EnemyNames != null && record.EnemyNames.Count > 0)
            sb.Append($"\n敌人：{string.Join("，", record.EnemyNames)}");

        if (record.ElectricityCoinChange != 0)
            sb.Append($"\n电力币：{FormatSigned(record.ElectricityCoinChange)}");

        if (record.TransitionEnergyChange != 0)
            sb.Append($"\n跃迁能量：{FormatSigned(record.TransitionEnergyChange)}");

        sb.Append(
            $"\n下一场战斗掉率：道具 {record.NextBattleItemDropChance}%；普通装备 {record.NextBattleEquipmentDropChance}%"
        );

        AppendJoinedLines(sb, "技能", record.SkillChanges);
        AppendJoinedLines(sb, "获得物品", record.GainedItems);
        AppendJoinedLines(sb, "消耗物品", record.ConsumedItems);
        AppendJoinedLines(sb, "装备变化", record.EquipmentChanges);
        AppendJoinedLines(sb, "遗物变化", record.RelicChanges);

        if (record.PermanentPropertyChanges != null && record.PermanentPropertyChanges.Count > 0)
        {
            var propertyLines = record
                .PermanentPropertyChanges.Select(BuildPropertyChangeLine)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            AppendJoinedLines(sb, "永久属性变化", propertyLines);
        }

        AppendJoinedLines(sb, "备注", record.Notes);

        if (
            record.ElectricityCoinChange == 0
            && record.TransitionEnergyChange == 0
            && (record.SkillChanges == null || record.SkillChanges.Count == 0)
            && (record.GainedItems == null || record.GainedItems.Count == 0)
            && (record.ConsumedItems == null || record.ConsumedItems.Count == 0)
            && (record.EquipmentChanges == null || record.EquipmentChanges.Count == 0)
            && (record.RelicChanges == null || record.RelicChanges.Count == 0)
            && (record.PermanentPropertyChanges == null || record.PermanentPropertyChanges.Count == 0)
            && (record.Notes == null || record.Notes.Count == 0)
        )
        {
            sb.Append("\n无额外变化");
        }

        return sb.ToString();
    }

    private static void AppendJoinedLines(StringBuilder sb, string label, List<string> values)
    {
        if (values == null || values.Count == 0)
            return;

        sb.Append($"\n{label}：{string.Join("；", values)}");
    }

    private static string BuildPropertyChangeLine(LevelNodePropertyChangeRecord change)
    {
        if (change == null)
            return string.Empty;

        var segments = new List<string>(4);
        AppendStatSegment(segments, PropertyType.Power, change.PowerChange);
        AppendStatSegment(segments, PropertyType.Survivability, change.SurvivabilityChange);
        AppendStatSegment(segments, PropertyType.Speed, change.SpeedChange);
        AppendStatSegment(segments, PropertyType.MaxLife, change.MaxLifeChange);

        if (segments.Count == 0)
            return string.Empty;

        string characterName = string.IsNullOrWhiteSpace(change.CharacterName)
            ? "未知角色"
            : change.CharacterName;
        return $"{characterName} {string.Join("，", segments)}";
    }

    private static void AppendStatSegment(List<string> segments, PropertyType type, int delta)
    {
        if (delta == 0)
            return;
        segments.Add($"{type.GetDescription()}{FormatSigned(delta)}");
    }

    private static string GetLevelTypeLabel(LevelNode.LevelType type)
    {
        return type switch
        {
            LevelNode.LevelType.Normal => "普通战斗",
            LevelNode.LevelType.Elite => "精英战斗",
            LevelNode.LevelType.Boss => "首领战斗",
            LevelNode.LevelType.Event => "事件",
            LevelNode.LevelType.Shop => "商店",
            _ => "未知节点",
        };
    }

    private static bool IsBattleNode(LevelNode.LevelType type) =>
        type is LevelNode.LevelType.Normal or LevelNode.LevelType.Elite or LevelNode.LevelType.Boss;

    private static string FormatSigned(int value) => value >= 0 ? $"+{value}" : value.ToString();

    private static string FormatRunDuration(long totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        long hours = totalSeconds / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        if (hours > 0)
            return $"{hours}小时{minutes}分{seconds}秒";
        if (minutes > 0)
            return $"{minutes}分{seconds}秒";
        return $"{seconds}秒";
    }

    private static List<string> CaptureEnemyNames(LevelNode node)
    {
        if (node == null)
            return new List<string>();

        var source =
            node.EnemiesRegeditList
            ?? (
                node.Type == LevelNode.LevelType.Normal
                || node.Type == LevelNode.LevelType.Elite
                || node.Type == LevelNode.LevelType.Boss
                    ? node.ProduceEnemies()
                    : null
            );

        if (source == null || source.Count == 0)
            return new List<string>();

        return source
            .Where(enemy => enemy != null)
            .Select(enemy => string.IsNullOrWhiteSpace(enemy.CharacterName) ? "未知敌人" : enemy.CharacterName)
            .ToList();
    }

    private static Dictionary<RelicID, int> CloneRelicMap(Dictionary<RelicID, int> source)
    {
        return source == null ? new Dictionary<RelicID, int>() : new Dictionary<RelicID, int>(source);
    }

    private static List<ItemID> CloneItemList(List<ItemID> source)
    {
        return source == null ? new List<ItemID>() : new List<ItemID>(source);
    }

    private static List<string> CaptureOwnedEquipmentNames(List<Equipment> source)
    {
        if (source == null || source.Count == 0)
            return new List<string>();

        return source.Select(GetEquipmentDisplayName).ToList();
    }

    private static List<HashSet<SkillID>> CapturePlayerGainedSkills(PlayerInfoStructure[] players)
    {
        var result = new List<HashSet<SkillID>>();
        if (players == null || players.Length == 0)
            return result;

        for (int i = 0; i < players.Length; i++)
        {
            result.Add(new HashSet<SkillID>(players[i].GainedSkills ?? new List<SkillID>()));
        }

        return result;
    }

    private static PlayerPropertySnapshot[] CapturePlayerProperties(PlayerInfoStructure[] players)
    {
        if (players == null || players.Length == 0)
            return Array.Empty<PlayerPropertySnapshot>();

        var result = new PlayerPropertySnapshot[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            result[i] = new PlayerPropertySnapshot
            {
                CharacterName = GetPlayerName(player, i),
                Power = player.Power,
                Survivability = player.Survivability,
                Speed = player.Speed,
                MaxLife = player.LifeMax,
            };
        }

        return result;
    }

    private static List<string> BuildSkillChanges(
        List<HashSet<SkillID>> before,
        PlayerInfoStructure[] currentPlayers
    )
    {
        var result = new List<string>();
        if (currentPlayers == null || currentPlayers.Length == 0)
            return result;

        for (int i = 0; i < currentPlayers.Length; i++)
        {
            var current = currentPlayers[i];
            var previous = i < before.Count ? before[i] : new HashSet<SkillID>();
            foreach (var skillId in current.GainedSkills ?? new List<SkillID>())
            {
                if (previous.Contains(skillId))
                    continue;

                result.Add($"{GetPlayerName(current, i)} · {GetSkillDisplayName(skillId)}");
            }
        }

        return result;
    }

    private static List<string> BuildPositiveItemChanges(List<ItemID> before, List<ItemID> after)
    {
        return BuildCountedChanges(
            CountItems(after),
            CountItems(before),
            itemId => ConsumeItem.GetItemName(itemId)
        );
    }

    private static List<string> BuildNegativeItemChanges(List<ItemID> before, List<ItemID> after)
    {
        return BuildCountedChanges(
            CountItems(before),
            CountItems(after),
            itemId => ConsumeItem.GetItemName(itemId)
        );
    }

    private static Dictionary<ItemID, int> CountItems(List<ItemID> items)
    {
        var result = new Dictionary<ItemID, int>();
        if (items == null)
            return result;

        for (int i = 0; i < items.Count; i++)
        {
            ItemID itemId = items[i];
            result.TryGetValue(itemId, out int count);
            result[itemId] = count + 1;
        }

        return result;
    }

    private static List<string> BuildEquipmentChanges(
        List<string> beforeEquipmentNames,
        List<Equipment> currentEquipments
    )
    {
        var before = CountStrings(beforeEquipmentNames);
        var after = CountStrings(CaptureOwnedEquipmentNames(currentEquipments));
        return BuildCountedChanges(after, before, value => value);
    }

    private static int CountEquipmentGained(
        List<string> beforeEquipmentNames,
        List<Equipment> currentEquipments
    )
    {
        var before = CountStrings(beforeEquipmentNames);
        var after = CountStrings(CaptureOwnedEquipmentNames(currentEquipments));
        return CountPositiveCountDifference(after, before);
    }

    private static Dictionary<string, int> CountStrings(List<string> values)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        if (values == null)
            return result;

        for (int i = 0; i < values.Count; i++)
        {
            string value = values[i];
            if (string.IsNullOrWhiteSpace(value))
                continue;

            result.TryGetValue(value, out int count);
            result[value] = count + 1;
        }

        return result;
    }

    private static List<string> BuildRelicChanges(
        Dictionary<RelicID, int> beforeRelics,
        Dictionary<RelicID, int> afterRelics
    )
    {
        var result = new List<string>();
        beforeRelics ??= new Dictionary<RelicID, int>();
        afterRelics ??= new Dictionary<RelicID, int>();

        HashSet<RelicID> allIds = new(beforeRelics.Keys);
        allIds.UnionWith(afterRelics.Keys);

        foreach (var relicId in allIds.OrderBy(id => id.ToString(), StringComparer.Ordinal))
        {
            int acquireAmount = Relic.GetAcquireAmount(relicId);
            string relicName = Relic.Create(relicId).RelicName;

            bool hadBefore = beforeRelics.TryGetValue(relicId, out int beforeValue);
            bool hasAfter = afterRelics.TryGetValue(relicId, out int afterValue);

            if (acquireAmount < 0)
            {
                if (hadBefore == hasAfter)
                    continue;

                result.Add(hasAfter ? $"{relicName} +1" : $"{relicName} -1");
                continue;
            }

            int diff = (hasAfter ? afterValue : 0) - (hadBefore ? beforeValue : 0);
            if (diff == 0)
                continue;

            result.Add($"{relicName} {FormatSigned(diff)}");
        }

        return result;
    }

    private static int CountRelicsGained(
        Dictionary<RelicID, int> beforeRelics,
        Dictionary<RelicID, int> afterRelics
    )
    {
        beforeRelics ??= new Dictionary<RelicID, int>();
        afterRelics ??= new Dictionary<RelicID, int>();
        return CountPositiveCountDifference(afterRelics, beforeRelics);
    }

    private static List<LevelNodePropertyChangeRecord> BuildPropertyChanges(
        PlayerPropertySnapshot[] before,
        PlayerInfoStructure[] currentPlayers
    )
    {
        var result = new List<LevelNodePropertyChangeRecord>();
        if (before == null || before.Length == 0 || currentPlayers == null || currentPlayers.Length == 0)
            return result;

        int count = Math.Min(before.Length, currentPlayers.Length);
        for (int i = 0; i < count; i++)
        {
            var previous = before[i];
            var current = currentPlayers[i];
            var change = new LevelNodePropertyChangeRecord
            {
                CharacterName = GetPlayerName(current, i),
                PowerChange = current.Power - previous.Power,
                SurvivabilityChange = current.Survivability - previous.Survivability,
                SpeedChange = current.Speed - previous.Speed,
                MaxLifeChange = current.LifeMax - previous.MaxLife,
            };

            if (
                change.PowerChange == 0
                && change.SurvivabilityChange == 0
                && change.SpeedChange == 0
                && change.MaxLifeChange == 0
            )
            {
                continue;
            }

            result.Add(change);
        }

        return result;
    }

    private static List<string> BuildCountedChanges<T>(
        Dictionary<T, int> larger,
        Dictionary<T, int> smaller,
        Func<T, string> labelSelector
    )
    {
        var result = new List<string>();
        foreach (var pair in larger.OrderBy(pair => pair.Key?.ToString(), StringComparer.Ordinal))
        {
            smaller.TryGetValue(pair.Key, out int baseline);
            int diff = pair.Value - baseline;
            if (diff <= 0)
                continue;

            string label = labelSelector(pair.Key);
            result.Add(diff > 1 ? $"{label} x{diff}" : label);
        }

        return result;
    }

    private static int CountPositiveCountDifference<T>(
        Dictionary<T, int> larger,
        Dictionary<T, int> smaller
    )
    {
        int result = 0;
        larger ??= new Dictionary<T, int>();
        smaller ??= new Dictionary<T, int>();

        foreach (var pair in larger)
        {
            smaller.TryGetValue(pair.Key, out int baseline);
            int diff = pair.Value - baseline;
            if (diff > 0)
                result += diff;
        }

        return result;
    }

    private static string GetPlayerName(PlayerInfoStructure info, int index)
    {
        return string.IsNullOrWhiteSpace(info.CharacterName) ? $"角色{index + 1}" : info.CharacterName;
    }

    private static string GetSkillDisplayName(SkillID skillId)
    {
        var skill = Skill.GetSkill(skillId);
        return skill == null || string.IsNullOrWhiteSpace(skill.SkillName)
            ? skillId.ToString()
            : skill.SkillName;
    }

    private static string GetEquipmentDisplayName(Equipment equipment)
    {
        if (equipment == null)
            return "未知装备";
        return string.IsNullOrWhiteSpace(equipment.DisplayName)
            ? equipment.Name.ToString()
            : equipment.DisplayName;
    }
}
