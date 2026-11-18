using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class TurnEndAttackRegression : Node
{
    private Battle _battle;
    private EnemyCharacter _enemy;
    private PlayerCharacter[] _players;

    public override async void _Ready() {
        bool passed = false;
        try {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Setup();
            _battle.ActionCardTriggerDelay = 0;
            await CheckPreview();
            passed = true;
        }
        catch (Exception ex) { GD.PrintErr($"[TurnEndAttackRegression] {ex}"); }
        GD.Print($"[TurnEndAttackRegression] {(passed ? "PASS" : "FAIL")}");
        _battle?.QueueFree();
        GetTree().Root.GetNodeOrNull<Map>("Map")?.QueueFree();
        for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(passed ? 0 : 1);
    }

    private void Setup()
    {
        var registry = new PlayerCharacterRegistry();
        GameInfo.PlayerCharacters = Enumerable.Range(0, 3).Select(index =>
        {
            PlayerInfoStructure info = registry.Echo;
            info.PositionIndex = index + 1;
            info.Power = index;
            info.GainedSkills = new List<SkillID> { SkillID.BasicAttack, SkillID.BasicDefense, SkillID.SacredOnslaught };
            info.TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.EchoBasicSpecial];
            info.AllSkills = Skill.GetPlayerSkillPool(PlayerCharacterKey.Echo);
            return info;
        }).ToArray();
        var map = GD.Load<PackedScene>("res://Map/Map.tscn").Instantiate<Map>();
        map.Name = "Map";
        map.WarmupMode = true;
        GetTree().Root.AddChild(map);
        _battle = GD.Load<PackedScene>("res://battle/Battle.tscn").Instantiate<Battle>();
        _battle.WarmupMode = true;
        _battle.Name = "RegressionBattle";
        AddChild(_battle);
        _players = new PlayerCharacter[3];
        for (int index = 0; index < 3; index++)
        {
            var player = GD.Load<PackedScene>("res://character/PlayerCharacter/Echo/Echo.tscn")
                .Instantiate<PlayerCharacter>();
            player.WarmupMode = true;
            player.CharacterIndex = index;
            player.BattleNode = _battle;
            _battle.Left.AddChild(player);
            _battle.PlayersList.Add(player);
            player.Initialize();
            _players[index] = player;
        }
        // Reverse the list to prove order is taken from formation rather than list insertion.
        _battle.PlayersList.Reverse();
        _enemy = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn").Instantiate<EnemyCharacter>();
        _enemy.WarmupMode = true;
        _enemy.BattleNode = _battle;
        _enemy.Registry = new EvilRegedit { MaxLife = 1000, CurrentLife = 1000, PositionIndex = 2 };
        _battle.Right.AddChild(_enemy);
        _battle.EnemiesList.Add(_enemy);
        _enemy.Initialize();
        _enemy.ConfigureCombatStats(0, 0, 1000);
        _enemy.Life = 1000;
    }

    private void ClearActions() => typeof(Battle).GetMethod("ClearActionCards", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_battle, null);
    private async Task Install(Skill skill, int owner = 0, Character friendly = null) {
        skill.OwnerCharater = _players[owner];
        skill.SetManualFriendlyTarget(friendly);
        _battle.UpdataPlayerEnergySilently(30 - _battle.PlayerEnergy);
        await skill.Effect();
        Require(_battle.IsActionCardInstalled(skill), "action installed");
    }
    private async Task CheckPreview() {
        Require(_players.All(player => !player.SpecialBuffs.Any(buff => buff.ThisBuffName is Buff.BuffName.AttackCount
            or Buff.BuffName.TemporaryAttackCount)), "legacy automatic attack buffs absent");
        Require(_battle.BuildTurnEndAttackPreview().Length == 0, "no installed cards means no automatic attacks");
        foreach (var player in _players) player.ConfigureCombatStats(4, 1, player.BattleMaxLife);
        await Install(new BasicAttack());
        await Install(new BasicAttack(), 1);
        await Install(new BasicAttack(), 2);
        var immune = new HurtBuff(_enemy, Buff.BuffName.DamageImmune, 1);
        var vulnerable = new HurtBuff(_enemy, Buff.BuffName.Vulnerable, 1);
        _enemy.HurtBuffs.Add(immune);
        _enemy.HurtBuffs.Add(vulnerable);
        _enemy.UpdataBlock(5);
        int life = _enemy.Life;
        int energy = _battle.PlayerEnergy;
        int uses = _battle.UsedSkills.Count;
        var preview = _battle.BuildTurnEndAttackPreview().Single();
        Require(preview.Damage == 7 && preview.Blocked == 5 && preview.Hits == 3, "shared block and one-shot immunity");
        Require(_enemy.Life == life && _enemy.Block == 5 && immune.Stack == 1 && vulnerable.Stack == 1
            && _battle.PlayerEnergy == energy && _battle.UsedSkills.Count == uses, "preview is pure");
        Require(_battle.BuildTurnEndAttackPreview().Single().Damage == preview.Damage, "repeat preview is stable");
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(life - _enemy.Life == preview.Damage, "mitigated preview matches actual actions");
        _enemy.HurtBuffs.Remove(immune); _enemy.HurtBuffs.Remove(vulnerable);
        ClearActions();
        _enemy.Life = 1000; _enemy.UpdataBlock(-99999);
        await Install(new GrowthAttack());
        await Install(new BasicAttack());
        preview = _battle.BuildTurnEndAttackPreview().Single();
        Require(preview.Damage == 12, "power gained by preceding plan feeds subsequent action");
        life = _enemy.Life;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(life - _enemy.Life == preview.Damage, "growth preview matches actual actions");
        ClearActions();
        _players[0].ConfigureCombatStats(4, 1, _players[0].BattleMaxLife);
        await Install(new MultiAttack());
        var armor = new HurtBuff(_enemy, Buff.BuffName.AutoArmor, 2);
        _enemy.HurtBuffs.Add(armor);
        preview = _battle.BuildTurnEndAttackPreview().Single();
        Require(preview.Damage == 8 && preview.Blocked == 4 && preview.Hits == 3, "multi-hit armor is applied between hits");
        life = _enemy.Life;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(life - _enemy.Life == preview.Damage, "multi-hit preview matches actual actions");
        _enemy.HurtBuffs.Remove(armor); _enemy.UpdataBlock(-99999);
        ClearActions();
        await Install(new EchonicResonance()); // installed with 30 energy; X value must stay frozen in preview
        _battle.UpdataPlayerEnergySilently(-_battle.PlayerEnergy);
        preview = _battle.BuildTurnEndAttackPreview().Single();
        Require(preview.Hits == 30, "X preview uses installation energy");
        ClearActions();
        await Install(new BasicAttack());
        await Install(new BasicAttack(), 1);
        var rear = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn").Instantiate<EnemyCharacter>();
        rear.WarmupMode = true; rear.BattleNode = _battle;
        rear.Registry = new EvilRegedit { MaxLife = 100, CurrentLife = 100, PositionIndex = 3 };
        _battle.Right.AddChild(rear); _battle.EnemiesList.Add(rear); rear.Initialize();
        rear.ConfigureCombatStats(0, 0, 100); rear.Life = 100; _enemy.Life = 3;
        var entries = _battle.BuildTurnEndAttackPreview();
        Require(entries.Single(entry => entry.Target == _enemy).Damage == 3
            && entries.Single(entry => entry.Target == rear).Damage == 4, "later action retargets after simulated kill");
        var taunt = new HurtBuff(rear, Buff.BuffName.Taunt, 1); rear.HurtBuffs.Add(taunt);
        Require(_battle.BuildTurnEndAttackPreview().Single().Target == rear, "taunt respected");
        rear.StartActionBuffs.Add(new StartActionBuff(rear, Buff.BuffName.Invisible, 1));
        Require(_battle.BuildTurnEndAttackPreview().Any(entry => entry.Target == _enemy), "invisibility before taunt");
        var rebirth = new DyingBuff(_enemy, Buff.BuffName.RebirthI, 1); _enemy.DyingBuffs.Add(rebirth);
        Require(_battle.BuildTurnEndAttackPreview().Single().Damage == 7 && rebirth.Stack == 1, "rebirth shadow state");
        _enemy.DyingBuffs.Remove(rebirth);
        _enemy.Life = 1000;
        _players[0].Life = 1;
        var thorn = new HurtBuff(_enemy, Buff.BuffName.Thorn, 1); _enemy.HurtBuffs.Add(thorn);
        ClearActions();
        await Install(new MultiAttack()); await Install(new BasicAttack()); await Install(new BasicAttack(), 1);
        preview = _battle.BuildTurnEndAttackPreview().Single();
        Require(preview.Damage == 8 && preview.Hits == 2 && _players[0].Life == 1, "reflection skips killed actor's remaining hits/cards");
        _enemy.HurtBuffs.Remove(thorn); _players[0].Life = _players[0].BattleMaxLife;
        ClearActions();
        await Install(new BasicAttack()); await Install(new BasicAttack());
        var stun = new SkillBuff(_players[0], Buff.BuffName.Stun, 1); _players[0].SkillBuffs.Add(stun);
        Require(_battle.BuildTurnEndAttackPreview().Single().Hits == 1 && stun.Stack == 1, "stun consumes only a shadow stack");
        _players[0].SkillBuffs.Remove(stun);
        GD.Print("[TurnEndAttackRegression] action totals/purity/growth/multihit/X/retarget/taunt/rebirth/reflection/stun PASS");
    }
    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
    private partial class GrowthAttack : Skill {
        public override SkillTypes SkillType => SkillTypes.Attack;
        public override int EnergyCost => 0;
        protected override SkillPlan BuildPlan() => new(this, ModifyPropertyStep(PropertyType.Power, 2), AttackStep(0, multiplier: 1));
    }
    private partial class MultiAttack : Skill {
        public override SkillTypes SkillType => SkillTypes.Attack;
        public override int EnergyCost => 0;
        protected override SkillPlan BuildPlan() => new(this, AttackStep(0, multiplier: 1, times: 3));
    }
}
