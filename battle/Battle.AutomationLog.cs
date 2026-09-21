using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Godot;

public partial class Battle
{
    [Export]
    public bool AutomationBattleLogEnabled { get; set; } = false;
    public bool IsAutomationBattleLogActive => AutomationBattleLogEnabled && !WarmupMode;

    private const string AutomationBattleLogPath = "user://battle_automation_log.jsonl";
    private const string AutomationBattleLatestPath = "user://battle_automation_latest.json";
    private const string AutomationBattleMetaPath = "user://battle_automation_meta.json";
    private static readonly JsonSerializerOptions AutomationJsonOptions =
        new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
        };

    private string _automationBattleLogGlobalPath;
    private string _automationBattleLatestGlobalPath;
    private string _automationBattleMetaGlobalPath;
    private int _automationEventIndex;

    private void InitializeBattleAutomationLog()
    {
        _automationEventIndex = 0;
        _automationBattleLogGlobalPath = ProjectSettings.GlobalizePath(AutomationBattleLogPath);
        _automationBattleLatestGlobalPath = ProjectSettings.GlobalizePath(AutomationBattleLatestPath);
        _automationBattleMetaGlobalPath = ProjectSettings.GlobalizePath(AutomationBattleMetaPath);
        EnsureAutomationLogDirectory(_automationBattleLogGlobalPath);
        EnsureAutomationLogDirectory(_automationBattleLatestGlobalPath);
        EnsureAutomationLogDirectory(_automationBattleMetaGlobalPath);

        if (!IsAutomationBattleLogActive)
            return;

        WriteAutomationFile(_automationBattleLogGlobalPath, string.Empty);
        WriteAutomationEvent("battle_log_initialized");
        WriteAutomationFile(
            _automationBattleMetaGlobalPath,
            JsonSerializer.Serialize(
                new Dictionary<string, object>
                {
                    ["logPath"] = _automationBattleLogGlobalPath,
                    ["latestPath"] = _automationBattleLatestGlobalPath,
                    ["schema"] = "battle-automation-v1",
                },
                AutomationJsonOptions
            )
        );
    }

    public void RecordAutomationSnapshot(string reason)
    {
        WriteAutomationEvent(reason);
    }

    public void RecordAutomationEvent(string reason, Dictionary<string, object> details = null)
    {
        WriteAutomationEvent(reason, details);
    }

    private void WriteAutomationEvent(string reason, Dictionary<string, object> details = null)
    {
        if (!IsAutomationBattleLogActive)
            return;

        try
        {
            Dictionary<string, object> payload = BuildAutomationSnapshot(reason, details);
            string json = JsonSerializer.Serialize(payload, AutomationJsonOptions);
            File.AppendAllText(_automationBattleLogGlobalPath, json + System.Environment.NewLine);
            WriteAutomationFile(_automationBattleLatestGlobalPath, json);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Battle automation log write failed: {ex.Message}");
        }
    }

    private Dictionary<string, object> BuildAutomationSnapshot(
        string reason,
        Dictionary<string, object> details
    )
    {
        PlayerTeamBattleCardPiles piles = _playerTeamBattleCardPiles;
        Skill[] hand = _playerTeamBattleHand ?? Array.Empty<Skill>();
        CharacterControl control =
            CharacterControl != null && GodotObject.IsInstanceValid(CharacterControl)
                ? CharacterControl
                : null;

        return new Dictionary<string, object>
        {
            ["schema"] = "battle-automation-v1",
            ["eventIndex"] = ++_automationEventIndex,
            ["reason"] = reason ?? "snapshot",
            ["timeMsec"] = Time.GetTicksMsec(),
            ["battleInstanceId"] = _battleInstanceId,
            ["turn"] = new Dictionary<string, object>
            {
                ["elapsedTurnCount"] = _elapsedTurnCount,
                ["playerEnergy"] = _playerEnergy,
                ["currentActor"] = CharacterAutomationId(CurrentActionCharacter),
                ["isPlayerPhase"] = _isResolvingPlayerTeamActionPhase,
                ["isEnemyPhase"] = _isResolvingEnemyTeamActionPhase,
                ["canEndTurn"] = control?.EndTurnButton?.Disabled == false,
                ["manualTargetSelection"] = control?.IsManualTargetArrowSelectionActive == true,
                ["ui"] = control?.GetAutomationUiState() ?? new Dictionary<string, object>(),
            },
            ["battleEnded"] = HasBattleEnded(),
            ["details"] = details ?? new Dictionary<string, object>(),
            ["players"] = GetOrderedTeamCharacters(isPlayer: true, includeSummons: true, dyingFilter: true)
                .Select(BuildCharacterAutomationState)
                .ToArray(),
            ["enemies"] = GetOrderedTeamCharacters(isPlayer: false, includeSummons: true, dyingFilter: true)
                .Select(BuildCharacterAutomationState)
                .ToArray(),
            ["incomingDamage"] = BuildIncomingDamageAutomationState(),
            ["hand"] = hand.Select((skill, index) => BuildHandCardAutomationState(skill, index)).ToArray(),
            ["piles"] = new Dictionary<string, object>
            {
                ["drawCount"] = piles?.DrawPile.Count ?? 0,
                ["discardCount"] = piles?.DiscardPile.Count ?? 0,
                ["exhaustedCount"] = piles?.Exhausted.Count ?? 0,
                ["drawTopKnown"] = BuildPileAutomationState(piles?.DrawPile),
                ["discard"] = BuildPileAutomationState(piles?.DiscardPile),
                ["exhausted"] = BuildPileAutomationState(piles?.Exhausted),
            },
        };
    }

    private Dictionary<string, object> BuildCharacterAutomationState(Character character)
    {
        var state = new Dictionary<string, object>
        {
            ["id"] = CharacterAutomationId(character),
            ["name"] = GetRecordCharacterName(character),
            ["type"] = character.GetType().Name,
            ["isPlayer"] = character.IsPlayer,
            ["isSummon"] = character.IsSummon,
            ["positionIndex"] = character.PositionIndex,
            ["state"] = character.State.ToString(),
            ["life"] = character.Life,
            ["maxLife"] = character.BattleMaxLife,
            ["block"] = character.Block,
            ["power"] = character.BattlePower,
            ["survivability"] = character.BattleSurvivability,
            ["sourceStacks"] = SpecialBuff.GetSourceEnergyBonus(character),
            ["currentEnergy"] = character.CurrentEnergy,
            ["buffs"] = BuildBuffAutomationState(character),
        };

        if (character is IIntentionPreviewSource intentionSource)
            state["intention"] = BuildIntentionAutomationState(intentionSource);

        return state;
    }

    private Dictionary<string, object> BuildHandCardAutomationState(Skill skill, int index)
    {
        if (skill == null)
        {
            return new Dictionary<string, object>
            {
                ["slot"] = index + 1,
                ["empty"] = true,
            };
        }

        return new Dictionary<string, object>
        {
            ["slot"] = index + 1,
            ["empty"] = false,
            ["skillId"] = skill.SkillId?.ToString() ?? skill.GetType().Name,
            ["name"] = skill.SkillName,
            ["type"] = skill.SkillType.ToString(),
            ["owner"] = CharacterAutomationId(skill.OwnerCharater),
            ["ownerName"] = SafeCharacterName(skill.OwnerCharater),
            ["cost"] = skill.CardEnergyCostText,
            ["rawEnergyCost"] = skill.RequiredEnergyCost,
            ["canBePlayed"] = skill.CanBePlayed,
            ["canUseCurrentEnergy"] = skill.CanUseCurrentEnergy(),
            ["requiresManualFriendlyTarget"] = skill.RequiresManualFriendlyTarget(),
            ["manualFriendlyExcludesSelf"] = skill.ManualFriendlyTargetExcludesSelf(),
            ["manualFriendlyAllowsDying"] = skill.ManualFriendlyTargetAllowsDying(),
            ["exhaustsAfterUse"] = skill.ExhaustsAfterUse,
            ["retainsAtTurnEnd"] = ShouldShowRetainKeyword(skill),
            ["preview"] = BuildSkillPreviewAutomationState(skill),
        };
    }

    private Dictionary<string, object> BuildSkillPreviewAutomationState(Skill skill)
    {
        return new Dictionary<string, object>
        {
            ["hostileTargets"] = skill.GetPreviewHostileTargets()
                .Select(CharacterAutomationId)
                .ToArray(),
            ["friendlyTargets"] = skill.GetPreviewFriendlyTargets()
                .Select(CharacterAutomationId)
                .ToArray(),
            ["effects"] = skill.GetPreviewEffectEntries()
                .Select(BuildPreviewEffectAutomationState)
                .ToArray(),
        };
    }

    private Dictionary<string, object> BuildIntentionAutomationState(IIntentionPreviewSource source)
    {
        Skill skill = source?.CurrentIntentionSkill;
        if (source == null || skill == null || source.HasActiveStun())
        {
            return new Dictionary<string, object>
            {
                ["stunned"] = source?.HasActiveStun() == true,
                ["skillId"] = null,
                ["name"] = null,
            };
        }

        skill.OwnerCharater = source.SourceCharacter;
        return new Dictionary<string, object>
        {
            ["stunned"] = false,
            ["skillId"] = skill.SkillId?.ToString() ?? skill.GetType().Name,
            ["name"] = skill.SkillName,
            ["type"] = skill.SkillType.ToString(),
            ["effects"] = skill.GetPreviewEffectEntries()
                .Select(BuildPreviewEffectAutomationState)
                .ToArray(),
        };
    }

    private Dictionary<string, object> BuildPreviewEffectAutomationState(Skill.PreviewEffectEntry entry)
    {
        return new Dictionary<string, object>
        {
            ["kind"] = entry.Kind.ToString(),
            ["target"] = CharacterAutomationId(entry.Target),
            ["targetName"] = SafeCharacterName(entry.Target),
            ["value"] = entry.Value,
            ["hitCount"] = entry.HitCount,
            ["powerMultiplier"] = entry.PowerMultiplier,
            ["property"] = entry.PropertyType?.ToString(),
            ["buff"] = entry.BuffName?.ToString(),
            ["text"] = entry.Text,
        };
    }

    private object[] BuildIncomingDamageAutomationState()
    {
        return BuildIncomingDamagePreviewEntries()
            .Select(entry => new Dictionary<string, object>
            {
                ["target"] = CharacterAutomationId(entry.Target),
                ["targetName"] = SafeCharacterName(entry.Target),
                ["damage"] = entry.Value,
            })
            .ToArray();
    }

    private object[] BuildBuffAutomationState(Character character)
    {
        return CollectCharacterBuffs(character)
            .Where(buff => buff != null && buff.Stack > 0)
            .Select(buff => new Dictionary<string, object>
            {
                ["name"] = buff.ThisBuffName.ToString(),
                ["description"] = buff.ThisBuffName.GetDescription(),
                ["stack"] = buff.Stack,
            })
            .ToArray();
    }

    private static IEnumerable<Buff> CollectCharacterBuffs(Character character)
    {
        if (character == null)
            return Enumerable.Empty<Buff>();

        return (character.StartActionBuffs ?? Enumerable.Empty<StartActionBuff>()).Cast<Buff>()
            .Concat((character.EndActionBuffs ?? Enumerable.Empty<EndActionBuff>()).Cast<Buff>())
            .Concat((character.AttackBuffs ?? Enumerable.Empty<AttackBuff>()).Cast<Buff>())
            .Concat((character.HurtBuffs ?? Enumerable.Empty<HurtBuff>()).Cast<Buff>())
            .Concat((character.SpecialBuffs ?? Enumerable.Empty<SpecialBuff>()).Cast<Buff>())
            .Concat((character.SkillBuffs ?? Enumerable.Empty<SkillBuff>()).Cast<Buff>())
            .Concat((character.DyingBuffs ?? Enumerable.Empty<DyingBuff>()).Cast<Buff>());
    }

    private object[] BuildPileAutomationState(List<BattleCardPileEntry> entries)
    {
        if (entries == null || entries.Count == 0)
            return Array.Empty<object>();

        return entries
            .Select(entry => new Dictionary<string, object>
            {
                ["owner"] = CharacterAutomationId(entry.Owner),
                ["ownerName"] = SafeCharacterName(entry.Owner),
                ["skillId"] = entry.SkillId.ToString(),
                ["name"] = Skill.GetSkill(entry.SkillId)?.SkillName ?? entry.SkillId.ToString(),
            })
            .ToArray();
    }

    internal static string CharacterAutomationId(Character character)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return null;

        return $"{(character.IsPlayer ? "player" : "enemy")}:{character.GetInstanceId()}";
    }

    private static string SafeCharacterName(Character character)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return null;

        return character.CharacterName;
    }

    private static void EnsureAutomationLogDirectory(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    private static void WriteAutomationFile(string path, string contents)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        File.WriteAllText(path, contents ?? string.Empty);
    }
}
