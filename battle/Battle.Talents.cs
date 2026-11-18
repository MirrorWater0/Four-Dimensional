using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Battle {
    private readonly Dictionary<Character, int> _talentExhaustRemainders = new();
    private readonly HashSet<Character> _talentFirstPowerShares = new();
    private bool _talentAttackGuardTriggeredThisTurn;
    private bool _playerTalentsApplied;

    private Character[] TalentOwners(Buff.BuffName name) => GetTeamCharacters(true, includeSummons: false)
        .Where(owner => IsTalentCharacterAlive(owner) && owner.SpecialBuffs.Any(buff => buff.ThisBuffName == name && buff.Stack > 0)).ToArray();
    public bool HasActiveTalent(Buff.BuffName name) => TalentOwners(name).Length > 0;
    public int GetTalentHealingBonus() => TalentOwners(Buff.BuffName.TalentHealingPlus).Length;
    public float GetTalentVulnerableMultiplier(Character target) =>
        1.5f + (target?.IsPlayer == false ? 0.2f * TalentOwners(Buff.BuffName.TalentVulnerableAmplify).Length : 0f);
    public float GetTalentDivinityMultiplier(Character attacker) =>
        attacker?.IsPlayer == true && HasActiveTalent(Buff.BuffName.TalentDivinity) ? 2.5f : 2f;

    private static bool IsTalentCharacterAlive(Character character) => character != null
        && GodotObject.IsInstanceValid(character) && IsCharacterAlive(character) && character.Life > 0;
    private Character[] TalentAllies() => GetTeamCharacters(true, includeSummons: true).Where(IsTalentCharacterAlive).ToArray();
    private async Task ApplyPlayerTalentsAtBattleStartAsync() {
        if (_playerTalentsApplied || GameInfo.PlayerCharacters == null)
            return;
        _playerTalentsApplied = true;
        _talentExhaustRemainders.Clear();
        _talentFirstPowerShares.Clear();
        _talentAttackGuardTriggeredThisTurn = false;
        PlayerCharacter[] players = PlayersList.Where(player => player != null && GodotObject.IsInstanceValid(player)).ToArray();
        // Install every marker before one-shot effects, so opening heals include healing talents.
        foreach (PlayerCharacter player in players) {
            if (!IsTalentCharacterAlive(player) || player.CharacterIndex < 0 || player.CharacterIndex >= GameInfo.PlayerCharacters.Length)
                continue;
            var info = GameInfo.PlayerCharacters[player.CharacterIndex];
            foreach (var talent in TalentTree.GetNodes(info).Where(node => TalentTree.HasUnlocked(info, node.Id)))
                SpecialBuff.BuffAdd(talent.BuffName, player, 1, player);
        }
        foreach (PlayerCharacter owner in players.Where(IsTalentCharacterAlive)) {
            foreach (var buff in owner.SpecialBuffs.Where(buff => TalentTree.IsTalentBuff(buff.ThisBuffName)).ToArray()) {
                using var source = owner.BeginEffectSource(Buff.GetBuffDisplayName(buff.ThisBuffName));
                switch (buff.ThisBuffName) {
                    case Buff.BuffName.TalentEnergyReserve:
                        SpecialBuff.BuffAdd(Buff.BuffName.EnergyStorage, owner, 1, owner);
                        break;
                    case Buff.BuffName.TalentTeamSurvivability:
                        foreach (var ally in TalentAllies())
                            await ally.IncreaseProperties(PropertyType.Survivability, 1, owner);
                        break;
                    case Buff.BuffName.TalentOpeningHeal:
                        foreach (var target in GetTeamCharacters(true, true).Concat(GetTeamCharacters(false, true)).Where(IsTalentCharacterAlive).ToArray())
                            target.Recover(1, source: owner);
                        break;
                    case Buff.BuffName.TalentTeamAfterimage:
                        foreach (var ally in TalentAllies())
                            StartActionBuff.BuffAdd(Buff.BuffName.Afterimage, ally, 1, owner);
                        break;
                }
            }
        }
    }

    public void NotifyTalentSkillPlayed(Skill skill) {
        if (skill?.OwnerCharater?.IsPlayer != true || skill.SkillType != Skill.SkillTypes.Attack
            || _talentAttackGuardTriggeredThisTurn)
            return;
        // Reserve the turn's first Attack before queued effects can play another card.
        _talentAttackGuardTriggeredThisTurn = true;
        foreach (var owner in TalentOwners(Buff.BuffName.TalentSpecialGuard))
            GrantTalentTeamBlock(owner, Buff.BuffName.TalentSpecialGuard, 2);
    }
    public void NotifyTalentCardsExhausted(int count) {
        if (count <= 0) return;
        foreach (var owner in TalentOwners(Buff.BuffName.TalentExhaustPower)) {
            int total = _talentExhaustRemainders.GetValueOrDefault(owner) + count;
            _talentExhaustRemainders[owner] = total % 4;
            if (total >= 4)
                QueueBattleEffect(() => GrantTalentPowerAsync(owner, total / 4), "天赋：消耗增力");
        }
        foreach (var owner in TalentOwners(Buff.BuffName.TalentExhaustDamage))
            QueueBattleEffect(() => TriggerTalentExhaustDamageAsync(owner, count),
                "天赋：碎刃风暴", consumeFrameBudget: false);
    }
    private static async Task GrantTalentPowerAsync(Character owner, int amount,
        Buff.BuffName name = Buff.BuffName.TalentExhaustPower) {
        if (!IsTalentCharacterAlive(owner))
            return;
        using var source = owner.BeginEffectSource(Buff.GetBuffDisplayName(name));
        Buff.FlashTriggersOnOwner(owner, name);
        await owner.IncreaseProperties(PropertyType.Power, amount, owner);
    }
    public void NotifyTalentEnteredStealth(Character target) {
        if (!IsTalentCharacterAlive(target) || !TalentOwners(Buff.BuffName.TalentStealthPower).Contains(target))
            return;
        QueueBattleEffect(() => GrantTalentPowerAsync(target, 1, Buff.BuffName.TalentStealthPower), "天赋：隐身增力");
    }
    public void NotifyTalentPowerGained(Character target, int amount) {
        if (amount <= 0 || !IsTalentCharacterAlive(target)
            || !TalentOwners(Buff.BuffName.TalentFirstPowerShare).Contains(target)
            || !_talentFirstPowerShares.Add(target))
            return;
        // Claim the first gain synchronously, before any queued sharing can grant more power.
        QueueBattleEffect(() => ShareTalentFirstPowerAsync(target, amount), "天赋：首次力量共享");
    }
    private async Task ShareTalentFirstPowerAsync(Character owner, int amount) {
        if (!IsTalentCharacterAlive(owner))
            return;
        using var source = owner.BeginEffectSource(Buff.GetBuffDisplayName(Buff.BuffName.TalentFirstPowerShare));
        Buff.FlashTriggersOnOwner(owner, Buff.BuffName.TalentFirstPowerShare);
        foreach (var ally in TalentAllies().Where(ally => ally != owner))
            await ally.IncreaseProperties(PropertyType.Power, amount, owner);
    }
    private async Task TriggerTalentExhaustDamageAsync(Character owner, int count) {
        for (int i = 0; i < count && IsTalentCharacterAlive(owner); i++) {
            await ExecuteBattleEffectAsync(async () => {
                if (!IsTalentCharacterAlive(owner))
                    return;
                Character[] enemies = GetTeamCharacters(false, includeSummons: true)
                    .Where(IsTalentCharacterAlive).ToArray();
                if (enemies.Length == 0)
                    return;

                Character enemy = enemies[GetTargetRandom(owner).Next(enemies.Length)];
                using var source = owner.BeginEffectSource(Buff.GetBuffDisplayName(Buff.BuffName.TalentExhaustDamage));
                Buff.FlashTriggersOnOwner(owner, Buff.BuffName.TalentExhaustDamage);
                await enemy.GetHurt(2, owner);
            }, "天赋：碎刃风暴目标伤害");
        }
    }
    public void NotifyTalentBuffGain(Character target, Buff.BuffName name, int stacks, Character source) {
        if (stacks <= 0 || !IsTalentCharacterAlive(target) || target.IsPlayer) return;
        if (name == Buff.BuffName.Vulnerable) {
            foreach (var owner in TalentOwners(Buff.BuffName.TalentVulnerableShock))
                QueueBattleEffect(() => TriggerTalentVulnerableShockAsync(owner, target), "天赋：易伤震击");
        }
        if (name == Buff.BuffName.Weaken && source?.IsPlayer != false)
            foreach (var owner in TalentOwners(Buff.BuffName.TalentWeakenGuard))
                GrantTalentTeamBlock(owner, Buff.BuffName.TalentWeakenGuard, stacks);
    }
    private static async Task TriggerTalentVulnerableShockAsync(Character owner, Character target) {
        if (!IsTalentCharacterAlive(owner) || !IsTalentCharacterAlive(target))
            return;
        using var source = owner.BeginEffectSource(Buff.GetBuffDisplayName(Buff.BuffName.TalentVulnerableShock));
        Buff.FlashTriggersOnOwner(owner, Buff.BuffName.TalentVulnerableShock);
        await target.GetHurt(5, owner);
    }
    private void GrantTalentTeamBlock(Character owner, Buff.BuffName name, int block) {
        QueueBattleEffect(() => GrantTalentTeamBlockAsync(owner, name, block),
            "天赋：全队格挡", consumeFrameBudget: false);
    }
    private async Task GrantTalentTeamBlockAsync(Character owner, Buff.BuffName name, int block) {
        if (!IsTalentCharacterAlive(owner))
            return;
        bool flashed = false;
        foreach (var ally in TalentAllies())
            await ExecuteBattleEffectAsync(() => {
                if (!IsTalentCharacterAlive(owner) || !IsTalentCharacterAlive(ally))
                    return Task.CompletedTask;
                using var source = owner.BeginEffectSource(Buff.GetBuffDisplayName(name));
                if (!flashed) {
                    Buff.FlashTriggersOnOwner(owner, name);
                    flashed = true;
                }
                ally.UpdataBlock(block, source: owner);
                return Task.CompletedTask;
            }, "天赋：目标格挡");
    }
    private void ResolveTalentPlayerTurnStart() {
        if (PlayerTotalTurnCount == 2)
            foreach (var owner in TalentOwners(Buff.BuffName.TalentSecondEnergy)) {
                Buff.FlashTriggersOnOwner(owner, Buff.BuffName.TalentSecondEnergy);
                UpdataEnergy(owner, 1, owner);
            }
    }
    public Task ResolvePendingTalentEffectsAsync() => ResolvePendingBattleEffectsAsync();
}
