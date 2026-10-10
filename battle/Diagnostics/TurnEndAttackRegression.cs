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

    public override async void _Ready()
    {
        bool passed = false;
        try
        {
            GD.Print("[TurnEndAttackRegression] START");
            CheckRegistry();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Setup();
            await CheckCountsAndCardAsync();
            await CheckTurnEndDamagePreviewAsync();
            await CheckCombatAsync();
            await CheckEndTurnButtonAsync();
            passed = true;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[TurnEndAttackRegression] {exception}");
        }
        GD.Print($"[TurnEndAttackRegression] {(passed ? "PASS" : "FAIL")}");
        _battle?.Free();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(passed ? 0 : 1);
    }

    private static void CheckRegistry()
    {
        foreach (PlayerCharacterKey key in Enum.GetValues<PlayerCharacterKey>())
            Require(Skill.GetPlayerSkillPool(key).All(id => Skill.GetSkill(id)?.SkillType != Skill.SkillTypes.Attack),
                $"no attack cards in {key} pool");
        Require(Skill.GetColorlessSkillPool().All(id => Skill.GetSkill(id)?.SkillType != Skill.SkillTypes.Attack),
            "no attack cards in colorless pool");
        Require(Skill.GetSkill(SkillID.SacredOnslaught) == null && Skill.GetSkill(SkillID.Blade) == null,
            "attack factories removed");
        Require(Skill.GetSkill(SkillID.EvilAttack)?.SkillType == Skill.SkillTypes.Attack,
            "enemy attack factory retained");
        Require(Skill.GetSkill(SkillID.BasicAttack)?.SkillType == Skill.SkillTypes.Special,
            "starter card now special");
        var registry = new PlayerCharacterRegistry();
        GameInfo.PlayerCharacters = [registry.Echo, registry.Kasiya, registry.Mariya, registry.Nightingale];
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
        Require(GameInfo.PlayerCharacters.All(info => info.GainedSkills.Count(id => id == SkillID.BasicAttack) == 1),
            "each character starts with one basic attack card");
        GD.Print("[TurnEndAttackRegression] registry PASS");
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

    private async Task CheckCountsAndCardAsync()
    {
        foreach (PlayerCharacter player in _players)
        {
            Require(AttackCountBuff.GetCount(player) == 1, "initial attack count is one");
            Require(player.SpecialBuffs.Any(buff => buff.ThisBuffName == Buff.BuffName.AttackCount
                && buff.BuffIcon?.GetNodeOrNull<TextureRect>("TextureIcon") != null), "normal count icon exists");
        }
        var card = Skill.GetSkill(SkillID.BasicAttack);
        card.OwnerCharater = _players[0];
        card.UpdateDescription();
        var preview = card.GetPreviewEffectEntries();
        Require(preview.Any(entry => entry.BuffName == Buff.BuffName.TemporaryAttackCount && entry.Value == 1),
            "starter card previews temporary attack");
        Require(AttackCountBuff.GetCount(_players[0], true) == 0, "preview is side-effect free");
        int energy = _battle.PlayerEnergy;
        Require(energy == 0 && card.CardEnergyCost == 0, "starter card is playable at zero energy");
        int life = _enemy.Life;
        await card.Effect();
        Require(_enemy.Life == life, "starter card deals no immediate damage");
        Require(_battle.PlayerEnergy == energy, "starter card costs zero energy");
        Require(AttackCountBuff.GetCount(_players[0], true) == 1, "starter card grants one temporary attack");
        Require(_players[0].SpecialBuffs.Any(buff => buff.ThisBuffName == Buff.BuffName.TemporaryAttackCount
            && buff.BuffIcon?.GetNodeOrNull<TextureRect>("TextureIcon") != null), "temporary count icon exists");
        await new AttackCountProbeSkill { OwnerCharater = _players[1] }.Effect();
        Require(AttackCountBuff.GetCount(_players[1]) == 3, "normal count Step persists");
        AttackCountBuff.Modify(_players[2], -50);
        Require(AttackCountBuff.GetCount(_players[2]) == 0, "normal count clamps to zero");
        Require(_players[2].SpecialBuffs.Any(buff => buff.ThisBuffName == Buff.BuffName.AttackCount
            && buff.BuffIcon != null && buff.Stack == 0), "zero count keeps its icon");
        GD.Print("[TurnEndAttackRegression] Step/card/preview/icons PASS");
    }

    private async Task CheckTurnEndDamagePreviewAsync()
    {
        var initial = _battle.BuildTurnEndAttackPreview().Single();
        Require(initial.Damage == 3 && initial.Hits == 5 && initial.Blocked == 0,
            "preview includes temporary attacks and has no base damage");
        _players[0].ConfigureCombatStats(4, _players[0].BattleSurvivability, _players[0].BattleMaxLife);
        var weaken = new AttackBuff(_players[0], Buff.BuffName.Weaken, 1);
        var vulnerable = new HurtBuff(_enemy, Buff.BuffName.Vulnerable, 1);
        var immune = new HurtBuff(_enemy, Buff.BuffName.DamageImmune, 1);
        _players[0].AttackBuffs.Add(weaken);
        _enemy.HurtBuffs.Add(vulnerable);
        _enemy.HurtBuffs.Add(immune);
        _enemy.UpdataBlock(5);
        int before = _enemy.Life;
        int records = DamageSources().Length;
        var mitigated = _battle.BuildTurnEndAttackPreview().Single();
        Require(mitigated.Damage == 2 && mitigated.Blocked == 5 && mitigated.Hits == 5,
            "preview simulates weaken, vulnerable, shared block and one-shot immunity");
        Require(_enemy.Life == before && _enemy.Block == 5 && immune.Stack == 1 && weaken.Stack == 1
            && AttackCountBuff.GetCount(_players[0], true) == 1 && DamageSources().Length == records,
            "preview never changes health, block, buff stacks, attack counts or records");
        await _battle.ResolvePlayerTurnEndAttacksAsync(CancellationToken.None);
        Require(before - _enemy.Life == mitigated.Damage && _enemy.Block == 0,
            "mitigated preview matches actual sequential end-turn damage");
        _players[0].AttackBuffs.Remove(weaken);
        _enemy.HurtBuffs.Remove(vulnerable);
        _enemy.HurtBuffs.Remove(immune);
        AttackCountBuff.Modify(_players[0], 1, temporary: true);

        var rear = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn").Instantiate<EnemyCharacter>();
        rear.WarmupMode = true;
        rear.BattleNode = _battle;
        rear.Registry = new EvilRegedit { MaxLife = 100, CurrentLife = 100, PositionIndex = 3 };
        _battle.Right.AddChild(rear);
        _battle.EnemiesList.Add(rear);
        rear.Initialize();
        rear.ConfigureCombatStats(0, 0, 100);
        rear.Life = 100;
        _enemy.Life = 3;
        var retargeted = _battle.BuildTurnEndAttackPreview();
        Require(retargeted.Single(entry => entry.Target == _enemy).Damage == 3
            && retargeted.Single(entry => entry.Target == rear).Damage == 7,
            "simulated kill retargets remaining attacks without overkill");
        var taunt = new HurtBuff(rear, Buff.BuffName.Taunt, 1);
        rear.HurtBuffs.Add(taunt);
        Require(_battle.BuildTurnEndAttackPreview().Single().Target == rear,
            "preview respects taunt");
        rear.StartActionBuffs.Add(new StartActionBuff(rear, Buff.BuffName.Invisible, 1));
        Require(_battle.BuildTurnEndAttackPreview().Single(entry => entry.Target == _enemy).Damage == 3,
            "preview respects invisibility before taunt and falls back after visible target dies");
        var rebirth = new DyingBuff(_enemy, Buff.BuffName.RebirthI, 1);
        _enemy.DyingBuffs.Add(rebirth);
        Require(_battle.BuildTurnEndAttackPreview().Single().Damage == 10 && rebirth.Stack == 1,
            "preview keeps attacking reborn target without consuming real rebirth");
        _enemy.DyingBuffs.Remove(rebirth);
        _enemy.Life = 1000;
        int playerLife = _players[0].Life;
        _players[0].Life = 1;
        var thorn = new HurtBuff(_enemy, Buff.BuffName.Thorn, 1);
        _enemy.HurtBuffs.Add(thorn);
        var reflected = _battle.BuildTurnEndAttackPreview().Single();
        Require(reflected.Damage == 7 && reflected.Hits == 4 && _players[0].Life == 1,
            "preview stops further attacks from an actor killed by reflection");
        _enemy.HurtBuffs.Remove(thorn);
        _players[0].Life = playerLife;
        _battle.EnemiesList.Remove(rear);
        rear.Free();
        _enemy.Life = 1000;
        _players[0].ConfigureCombatStats(0, _players[0].BattleSurvivability, _players[0].BattleMaxLife);
        GD.Print("[TurnEndAttackRegression] end-turn damage preview/purity/mitigation/retarget PASS");
    }

    private async Task CheckCombatAsync()
    {
        int energy = _battle.PlayerEnergy;
        int usedCards = _battle.UsedSkills.Count;
        int before = _enemy.Life;
        await _battle.ResolvePlayerTurnEndAttacksAsync(CancellationToken.None);
        Require(before - _enemy.Life == 3, "zero-power actor deals zero damage and one-power actor deals one per attack");
        Character[] order = DamageSources().TakeLast(5).ToArray();
        Require(order.SequenceEqual(new Character[] { _players[0], _players[0], _players[1], _players[1], _players[1] }),
            "attacks are sequential by formation");
        Require(_battle.PlayerEnergy == energy && _battle.UsedSkills.Count == usedCards,
            "automatic attacks cost no energy and emit no card-use events");
        Require(AttackCountBuff.GetCount(_players[0], true) == 0, "temporary count cleared");
        Require(!_players[0].SpecialBuffs.Any(buff => buff.ThisBuffName == Buff.BuffName.TemporaryAttackCount),
            "temporary icon removed");
        before = _enemy.Life;
        await _battle.ResolvePlayerTurnEndAttacksAsync(CancellationToken.None);
        Require(before - _enemy.Life == 3, "next turn only normal attacks persist");

        // A dying ally must neither attack nor keep temporary attacks for later revival.
        _players[1].State = Character.CharacterState.Dying;
        AttackCountBuff.Modify(_players[1], 4, true);
        before = _enemy.Life;
        await _battle.ResolvePlayerTurnEndAttacksAsync(CancellationToken.None);
        Require(before == _enemy.Life && AttackCountBuff.GetCount(_players[1], true) == 0,
            "dying actor skipped and temporary count cleared");
        _players[1].State = Character.CharacterState.Normal;

        // Keep victory handling deferred while checking last-target termination.
        using (_battle.PushEffectSource(_players[0], "regression"))
        {
            _players[0].ConfigureCombatStats(1, _players[0].BattleSurvivability, _players[0].BattleMaxLife);
            _enemy.Life = 1;
            int records = DamageSources().Length;
            AttackCountBuff.Modify(_players[0], 2, true);
            await _battle.ResolvePlayerTurnEndAttacksAsync(CancellationToken.None);
            Require(DamageSources().Length == records + 1,
                $"last enemy kill terminates attack queue: before={records} after={DamageSources().Length} state={_enemy.State} life={_enemy.Life}");
            Require(_players.All(player => AttackCountBuff.GetCount(player, true) == 0),
                "last enemy kill clears every temporary count");
            _enemy.Life = 1000;
            _enemy.State = Character.CharacterState.Normal;
            _players[0].ConfigureCombatStats(0, _players[0].BattleSurvivability, _players[0].BattleMaxLife);
        }
        GD.Print("[TurnEndAttackRegression] combat/order/death/lifetime PASS");
    }

    private Character[] DamageSources()
    {
        var records = (IEnumerable)typeof(Battle).GetField("_damageRecords", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_battle);
        return records.Cast<object>().Select(record => (Character)record.GetType().GetProperty("SourceCharacter")
            .GetValue(record)).ToArray();
    }

    private async Task CheckEndTurnButtonAsync()
    {
        _battle.CharacterControl.Connect();
        MethodInfo phaseMethod = typeof(Battle).GetMethod("PlayerActionPhase", BindingFlags.NonPublic | BindingFlags.Instance);
        Task phase = (Task)phaseMethod.Invoke(_battle, new object[] { CancellationToken.None });
        ulong deadline = Time.GetTicksMsec() + 10000;
        while (!_battle.GetPlayerTeamBattleHand().Any(skill => skill != null)
            || GetControlFlag("_isResolvingCard"))
        {
            Require(Time.GetTicksMsec() < deadline && !phase.IsCompleted, "player phase becomes ready");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Require(_battle.GetPlayerTeamBattleHand().All(skill => skill == null || skill.SkillType != Skill.SkillTypes.Attack),
            "old saved attack cards never enter battle hand");
        int before = _enemy.Life;
        _battle.SetProcess(true); // Warmup setup disables the normal UI polling loop.
        _battle.RefreshTurnEndAttackPreview();
        var previewPanel = _enemy.GetNodeOrNull<VBoxContainer>("TurnEndAttackPreview");
        Require(previewPanel?.Visible == true, "end-turn damage panel appears during player phase");
        AttackCountBuff.Modify(_players[1], 1, true);
        await ToSignal(GetTree().CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
        Require(previewPanel.GetChildren().OfType<PanelContainer>()
            .Any(row => row.GetNode<Label>("Content/Value").Text.Contains("4(5次)")),
            "panel refreshes its damage and hit count after temporary count changes");
        AttackCountBuff.Modify(_players[1], -1, true);
        AttackCountBuff.Modify(_players[0], 1, true);
        _battle.CharacterControl.EndTurnButton.EmitSignal(Button.SignalName.Pressed);
        Require(!previewPanel.Visible, "end-turn damage panel hides immediately on execution");
        Require(await Task.WhenAny(phase, Task.Delay(15000)) == phase, "end-turn button completes player phase");
        await phase;
        Require(before - _enemy.Life == 2 * _players[0].BattlePower + 3 * _players[1].BattlePower,
            "end-turn button resolves automatic attacks");
        Require(!_battle.GetPlayerTeamBattleHand().Any(skill => skill != null), "hand discarded after automatic attacks");
        Require(_players.All(player => AttackCountBuff.GetCount(player, true) == 0), "button path clears temporary attacks");
        GD.Print("[TurnEndAttackRegression] end-turn button/phase/discard PASS");
    }

    private bool GetControlFlag(string name) => (bool)typeof(CharacterControl)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_battle.CharacterControl);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private partial class AttackCountProbeSkill : Skill
    {
        public override SkillTypes SkillType => SkillTypes.Special;
        public override int EnergyCost => 0;
        protected override SkillPlan BuildPlan() => new(this, ModifyAttackCountStep(2));
    }
}
