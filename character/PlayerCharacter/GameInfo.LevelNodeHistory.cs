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
    public int ElectricityCoinChange;
    public int TransitionEnergyChange;
    public List<string> SkillChanges = new();
    public List<string> GainedItems = new();
    public List<string> ConsumedItems = new();
    public List<string> EquipmentChanges = new();
    public List<string> RelicChanges = new();
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
                NextBattleItemDropChance = GameInfo.BattleItemDropChance,
                NextBattleEquipmentDropChance = GameInfo.BattleEquipmentDropChance,
            };

            if (record.EnemyNames.Count == 0 && EnemyNames.Count > 0)
                record.EnemyNames = new List<string>(EnemyNames);

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

    private static string FormatSigned(int value) => value >= 0 ? $"+{value}" : value.ToString();

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
