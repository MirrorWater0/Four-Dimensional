using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class ActionCardRegression : Node {
    private Battle _battle;
    private EnemyCharacter _enemy;
    private PlayerCharacter[] _players;
    public override async void _Ready() {
        bool passed = false;
        try {
            GD.Print("[ActionCardRegression] START");
            await Frames(1);
            Setup();
            _battle.ActionCardTriggerDelay = 0;
            foreach (var player in _players) {
                player.ConfigureCombatStats(2, 1, player.BattleMaxLife);
                player.Position = _battle.GetFormationPosition(player.PositionIndex, -1);
            }
            _enemy.Position = _battle.GetFormationPosition(2, 1);
            if (!OS.GetCmdlineUserArgs().Contains("--action-layout-only")) {
                await CheckColorless();
                await CheckActions();
                await CheckReplacement();
                await CheckExecutionCounts();
                await CheckAutomaticSelection();
                await CheckSnapshotAndLastKill();
            }
            await CheckQueuedPlayAndTurns();
            passed = true;
        }
        catch (Exception ex) { GD.PrintErr($"[ActionCardRegression] {ex}"); }
        GD.Print($"[ActionCardRegression] {(passed ? "PASS" : "FAIL")}");
        _battle?.QueueFree();
        GetTree().Root.GetNodeOrNull<Map>("Map")?.QueueFree();
        await Frames(3);
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

    private Skill Card(SkillID id, int owner = 0) {
        Skill skill = Skill.GetSkill(id);
        Require(skill != null, $"registered {id}");
        skill.OwnerCharater = _players[owner];
        skill.UpdateDescription();
        return skill;
    }
    private void ClearActions() => typeof(Battle).GetMethod("ClearActionCards", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_battle, null);
    private async Task CheckColorless() {
        var blade = Card(SkillID.Blade);
        var defense = Card(SkillID.DefenceFocus);
        Require(!blade.IsActionCard && !defense.IsActionCard && !Skill.GetSkill(SkillID.Calmness).IsActionCard,
            "all colorless cards stay immediate in catalog and with an owner");
        int life = _enemy.Life;
        await blade.Effect();
        Require(_enemy.Life < life && !_battle.IsActionCardInstalled(blade) && blade.RemainingExecutions == 0,
            "colorless blade deals damage immediately and exhausts once");
        _battle.DiscardBattleSkill(_players[0], blade);
        Require(_battle.GetExhaustedBattleCardPileEntries(_players[0]).Any(entry => entry.InstanceId == blade.BattleCardInstanceId),
            "colorless blade reaches exhausted pile");
        int block = _players[0].Block;
        await defense.Effect();
        Require(_players[0].Block > block && !_battle.IsActionCardInstalled(defense), "colorless survival grants block immediately");
        GD.Print("[ActionCardRegression] colorless immediate damage/block/exhaust PASS");
    }
    private async Task CheckActions() {
        Require(Skill.GetSkill(SkillID.SacredOnslaught).IsActionCard && !Skill.GetSkill(SkillID.Blade).IsActionCard
            && Skill.GetSkill(SkillID.BasicDefense).IsActionCard, "attack and survival restored as actions");
        Require(!Skill.GetSkill(SkillID.EvilAttack).IsActionCard && !Skill.GetSkill(SkillID.EchoBasicSpecial).IsActionCard,
            "enemy intentions and special cards remain immediate");
        _battle.UpdataPlayerEnergySilently(20);
        var attack = Card(SkillID.BasicAttack);
        var defense = Card(SkillID.BasicDefense);
        var guard = Card(SkillID.BasicGuard, 1);
        guard.SetManualFriendlyTarget(_players[2]);
        int life = _enemy.Life, block = _players[0].Block, energy = _battle.PlayerEnergy, uses = _battle.UsedSkills.Count;
        await attack.Effect(); await defense.Effect(); await guard.Effect();
        Require(_enemy.Life == life && _players[0].Block == block, "installation never executes damage or block");
        Require(_battle.PlayerEnergy == energy - attack.CardEnergyCost - defense.CardEnergyCost - guard.CardEnergyCost,
            "installation pays energy exactly once");
        Require(_battle.UsedSkills.Count == uses + 3, "installation records card-use events");
        ulong instance = attack.BattleCardInstanceId;
        _battle.DiscardBattleSkill(_players[0], attack);
        Require(!_battle.GetDiscardBattleCardPileEntries(_players[0]).Any(entry => entry.InstanceId == instance),
            "installed instance cannot enter discard");
        var order = new List<(PlayerCharacter, int)>();
        void Trigger(PlayerCharacter owner, int index) => order.Add((owner, index));
        _battle.ActionCardTriggered += Trigger;
        uses = _battle.UsedSkills.Count; energy = _battle.PlayerEnergy;
        for (int round = 0; round < 2; round++) {
            int before = _enemy.Life, beforeBlock = _players[0].Block, guardBlock = _players[2].Block;
            await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
            Require(before - _enemy.Life == 2 && _players[0].Block - beforeBlock == 5
                && _players[2].Block - guardBlock == 3, "recurrent damage, block and chosen guard target");
        }
        Require(order.SequenceEqual(new[] { (_players[0], 0), (_players[0], 1), (_players[1], 0),
            (_players[0], 0), (_players[0], 1), (_players[1], 0) }), "formation and slot order");
        _battle.ActionCardTriggered -= Trigger;
        Require(_battle.PlayerEnergy == energy && _battle.UsedSkills.Count == uses, "recurrent execution has no payment or use events");
        _players[2].State = Character.CharacterState.Dying;
        _players[0].Life = 1;
        int fallbackBlock = _players[0].Block;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(_players[0].Block == fallbackBlock + 8, "invalid guard target falls back to weakest living ally");
        _players[2].State = Character.CharacterState.Normal;
        _players[0].Life = _players[0].BattleMaxLife;
        _players[0].State = Character.CharacterState.Dying;
        life = _enemy.Life;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(_enemy.Life == life, "dying actor skips installed actions");
        _players[0].State = Character.CharacterState.Normal;
        using (var cancellation = new CancellationTokenSource()) {
            cancellation.Cancel();
            await _battle.ResolveInstalledActionCardsAsync(cancellation.Token);
            Require(_enemy.Life == life && !_battle.IsResolvingActionCards, "cancellation clears execution flag");
        }
        ClearActions();
        // An X card repeats the originally invested amount, despite later energy changes.
        _battle.UpdataPlayerEnergySilently(3 - _battle.PlayerEnergy);
        Skill x = new TwoUseXProbe { SkillId = SkillID.EchonicResonance, OwnerCharater = _players[0] };
        await x.Effect();
        Require(_battle.GetInstalledActionCards(_players[0]).Single().PaidEnergy == 3, "X investment captured");
        _battle.UpdataPlayerEnergySilently(1);
        int power = _players[0].BattlePower;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(_players[0].BattlePower == power + 6 && _battle.PlayerEnergy == 1, "X investment persists across rounds");
        Require(!_battle.IsActionCardInstalled(x)
            && _battle.GetExhaustedBattleCardPileEntries(_players[0]).Count(entry => entry.InstanceId == x.BattleCardInstanceId) == 1,
            "exhaust 2 removes exactly after the second full plan, not inner X loops");
        _battle.UpdataPlayerEnergySilently(1);
        x = Card(SkillID.EchonicResonance);
        await x.Effect();
        Require(_battle.IsActionCardInstalled(x) && x.RemainingExecutions == 1, "legacy exhaust card installs as exhaust 1");
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(!_battle.IsActionCardInstalled(x) && x.RemainingExecutions == 0, "legacy exhaust 1 removed after first automatic execution");
        GD.Print("[ActionCardRegression] classification/install/identity/repeat/targets/X/cancel PASS");
    }
    private async Task CheckReplacement() {
        _battle.ActionCardSlotsPerCharacter = 1;
        _battle.UpdataPlayerEnergySilently(20);
        await Card(SkillID.BasicAttack).Effect();
        Skill old = _battle.GetInstalledActionCards(_players[0]).Single().Skill;
        Skill incoming = Card(SkillID.BasicDefense);
        Task<bool> preparation = _battle.PrepareActionCardInstallationAsync(incoming);
        await Frames(3);
        Require(_battle.ActionCardsUi.IsChoosingReplacement, "full slot requires choice");
        PressEscape(); await Frames(2);
        Require(preparation.IsCompleted && !await preparation && _battle.IsActionCardInstalled(old), "cancel keeps installed card");
        preparation = _battle.PrepareActionCardInstallationAsync(incoming);
        await Frames(3);
        ActionCardRack rack = _battle.ActionCardsUi.GetRack(_players[0]);
        await Click(rack.GetSlotGlobalCenter(0));
        Require(preparation.IsCompleted && await preparation, $"real UI click chooses replacement (hover={rack.HoveredIndex}, position={rack.Position})");
        await incoming.Effect();
        Require(_battle.GetInstalledActionCards(_players[0]).Single().Skill == incoming
            && _battle.GetDiscardBattleCardPileEntries(_players[0]).Count(entry => entry.InstanceId == old.BattleCardInstanceId) == 1,
            "replaced action returns to discard once without counting an execution");
        rack.CardScale = 0.4f; // Scene-wide exports drive all racks.
        _battle.ActionCardsUi.CardScale = 0.35f;
        _battle.ActionCardsUi.CardStep = 42;
        await Frames(4);
        Require(rack.GetNode<SkillCard>("Cards/Card0/Card").Scale.IsEqualApprox(new Vector2(0.35f, 0.35f)), "exported layout updates existing card UI");
        _battle.ActionCardsUi.CardScale = 0.29f;
        _battle.ActionCardsUi.CardStep = 48;
        _battle.ActionCardSlotsPerCharacter = 3;
        GD.Print("[ActionCardRegression] replacement/cancel/exhaust/layout PASS");
    }
    private async Task CheckAutomaticSelection() {
        MovePointer(new Vector2(1800, 100));
        await Frames(3);
        ClearActions();
        Skill search = Card(SkillID.ShatterSlash);
        await search.Effect();
        int before = _battle.GetPlayerTeamBattleHand().Count(card => card != null);
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(_battle.GetPlayerTeamBattleHand().Count(card => card != null) == before + 1,
            "action search automatically draws without modal selection");
        Require(!_battle.CharacterControl.IsInspectingBattlePresentation, "automatic search opens no overlay");
        GD.Print("[ActionCardRegression] automatic pile selection PASS");
        ClearActions();
    }
    private async Task CheckExecutionCounts() {
        ClearActions();
        Skill original = new TwoUseImmediateProbe { SkillId = SkillID.BasicSpecial, OwnerCharater = _players[0] };
        await original.Effect();
        Require(original.RemainingExecutions == 1 && !original.ResolvesExhaustsAfterUse, "immediate exhaust 2 survives its first execution");
        _battle.DiscardBattleSkill(_players[0], original);
        Require(_battle.GetDiscardBattleCardPileEntries(_players[0]).Any(entry => entry.InstanceId == original.BattleCardInstanceId),
            "partially used immediate card enters discard");
        Skill redrawn = new TwoUseImmediateProbe {
            SkillId = SkillID.BasicSpecial, OwnerCharater = _players[0], BattleCardInstanceId = original.BattleCardInstanceId,
        };
        Require(redrawn.RemainingExecutions == 1, "execution count survives card re-instantiation");
        await redrawn.Effect();
        Require(redrawn.ResolvesExhaustsAfterUse && redrawn.RemainingExecutions == 0, "second immediate execution exhausts");
        Skill ordinary = Card(SkillID.BasicAttack);
        _battle.AddBattleCardKeyword(ordinary, BattleCardKeyword.ExhaustAfterUse);
        await ordinary.Effect();
        Require(_battle.IsActionCardInstalled(ordinary) && ordinary.RemainingExecutions == 1, "added exhaust keyword means exhaust 1");
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(!_battle.IsActionCardInstalled(ordinary) && ordinary.RemainingExecutions == 0, "added exhaust 1 applies to automatic action");
        var echo = Card(SkillID.BasicAttack);
        echo.QueueExtraSkillExecutions(1);
        await echo.Effect();
        int life = _enemy.Life;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(life - _enemy.Life == 2 * _players[0].BattlePower, "installation-time extra execution runs on first trigger");
        life = _enemy.Life;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(life - _enemy.Life == _players[0].BattlePower, "installation-time extra execution is not repeated every round");
        ClearActions();
        Skill playedFromDrawPile = new OneUseImmediateProbe { SkillId = SkillID.MariyaBasicSpecial, OwnerCharater = _players[1] };
        Task drawPilePlay = _battle.CharacterControl.QueueDrawPileCardAsync(_players[1], playedFromDrawPile);
        Require(await Task.WhenAny(drawPilePlay, Task.Delay(10000)) == drawPilePlay, "free draw-pile play completes its queue");
        await drawPilePlay;
        await Until(() => !ControlFlag("_isProcessingCardQueue"));
        Require(_battle.GetExhaustedBattleCardPileEntries(_players[1]).Count(entry => entry.InstanceId == playedFromDrawPile.BattleCardInstanceId) == 1
            && !_battle.GetDiscardBattleCardPileEntries(_players[1]).Any(entry => entry.InstanceId == playedFromDrawPile.BattleCardInstanceId),
            "draw-pile play exhaust 1 resolves its destination after execution without duplicating into discard");
        GD.Print("[ActionCardRegression] exhaust 1/2/immediate/instance/keyword PASS");
    }
    private async Task CheckQueuedPlayAndTurns() {
        int installedAttackDamage = Card(SkillID.BasicAttack).GetPreviewHostileDamageEntries().Sum(entry => entry.Damage);
        foreach (var player in _players) {
            player.ConfigureCombatStats(2, 1, player.BattleMaxLife);
        }
        _battle.CharacterControl.Connect();
        MethodInfo phaseMethod = typeof(Battle).GetMethod("PlayerActionPhase", BindingFlags.NonPublic | BindingFlags.Instance);
        for (int round = 0; round < 2; round++) {
            Task phase = (Task)phaseMethod.Invoke(_battle, new object[] { CancellationToken.None });
            await Until(() => _battle.GetPlayerTeamBattleHand().Any(card => card != null)
                && !ControlFlag("_isResolvingCard") && !ControlFlag("_handLayoutSyncPending"));
            _battle.UpdataPlayerEnergySilently(20);
            if (round == 0) {
                int slot = Array.FindIndex(_battle.GetPlayerTeamBattleHand(), skill => skill?.SkillId == SkillID.BasicAttack);
                if (slot < 0 && OS.GetCmdlineUserArgs().Contains("--action-layout-only")) {
                    // Layout checks must not depend on a shuffled opening hand containing one specific card.
                    _battle.RemovePlayerTeamBattleHandCardAt(0);
                    Require(_battle.TryRestorePlayerTeamBattleHandCardAt(0, Card(SkillID.BasicAttack)), "prepare queued layout card");
                    _battle.CharacterControl.RefreshCurrentTurnUi();
                    slot = 0;
                    await Frames(3);
                }
                Require(slot >= 0, "restored attack appears in real hand");
                Skill action = _battle.GetPlayerTeamBattleHand()[slot];
                await Until(() => !(bool)typeof(CharacterControl).GetMethod("IsCardDrawEntryInputBlocked", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(_battle.CharacterControl, new object[] { slot }));
                int before = _enemy.Life;
                typeof(CharacterControl).GetMethod("QueueCardPlay", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(_battle.CharacterControl, new object[] { slot, action, false });
                await Until(() => _battle.IsActionCardInstalled(action) && !ControlFlag("_isProcessingCardQueue"));
                await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
                var installedCard = _battle.ActionCardsUi.GetRack((PlayerCharacter)action.OwnerCharater)
                    .GetNode<SkillCard>("Cards/Card0/Card");
                Require(installedCard.Scale.IsEqualApprox(Vector2.One * _battle.ActionCardsUi.CardScale)
                    && installedCard.Position.IsEqualApprox(Vector2.Zero), "installation grows to resting scale and position");
                Require(_enemy.Life == before, "actual queue installs without immediate attack");
                Require(!_battle.GetPlayerTeamBattleHand().Contains(action), "actual queue removes installed physical card from hand");
                // Fill the remaining slots for real-scene visual QA.
                for (int owner = 0; owner < 3; owner++) {
                    while (_battle.GetInstalledActionCards(_players[owner]).Length < 3) {
                        var ownerRack = _battle.ActionCardsUi.GetRack(_players[owner]);
                        int oldCount = ownerRack?.CardCount ?? 0;
                        Vector2 oldPosition = oldCount > 0 ? ownerRack.GetNode<Control>("Cards/Card0").Position : Vector2.Zero;
                        await Card(_battle.GetInstalledActionCards(_players[owner]).Length == 1 ? SkillID.BasicDefense : SkillID.BasicGuard, owner)
                            .Effect();
                        if (oldCount > 0)
                            Require(ownerRack.GetNode<Control>("Cards/Card0").Position.IsEqualApprox(oldPosition),
                                "real installation preserves existing card position when scheduling its slide");
                    }
                }
                _battle.CharacterControl.SetPlayerInputsEnabled(_players[0], true);
                await ToSignal(GetTree().CreateTimer(_battle.ActionCardsUi.LayoutMoveDuration + 0.05), SceneTreeTimer.SignalName.Timeout);
                await Capture("01_installed.png");
                var rack = _battle.ActionCardsUi.GetRack(_players[0]);
                Vector2 point = rack.GetNode<Control>("Cards/Card0").GlobalPosition + new Vector2(12, 55);
                MovePointer(point); await Frames(4);
                Require(rack.HoveredIndex == 0, "real battle rack hover");
                await Capture("02_hover.png");
                MovePointer(new Vector2(1800, 100)); await Frames(2);
                // Exercise prepaid cancellation through the actual queue and restore its hand instance.
                _battle.ActionCardSlotsPerCharacter = 3;
                int incomingSlot = Array.FindIndex(_battle.GetPlayerTeamBattleHand(), skill => skill?.IsActionCard == true);
                Require(incomingSlot >= 0, "remaining action in hand for cancellation");
                Skill incoming = _battle.GetPlayerTeamBattleHand()[incomingSlot];
                await Until(() => !(bool)typeof(CharacterControl).GetMethod("IsCardDrawEntryInputBlocked", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(_battle.CharacterControl, new object[] { incomingSlot }));
                int energy = _battle.PlayerEnergy;
                typeof(CharacterControl).GetMethod("QueueCardPlay", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(_battle.CharacterControl, new object[] { incomingSlot, incoming, false });
                await Until(() => _battle.ActionCardsUi.IsChoosingReplacement);
                await Capture("03_replace.png");
                PressEscape();
                await Until(() => !ControlFlag("_isProcessingCardQueue"));
                Require(_battle.PlayerEnergy == energy && _battle.GetPlayerTeamBattleHand().Contains(incoming),
                    "queued replacement cancellation refunds prepaid energy and restores physical card");
                Require(!ControlFlag("_isResolvingCard"), "cancelling replacement keeps end-turn input available");
            }
            int life = _enemy.Life;
            _battle.RefreshTurnEndAttackPreview();
            var totalsPanel = _enemy.GetNodeOrNull<VBoxContainer>("TurnEndAttackPreview");
            Require(totalsPanel?.Visible == true, "installed action totals appear during the real player phase");
            _battle.CharacterControl.EndTurnButton.EmitSignal(Button.SignalName.Pressed);
            Require(!totalsPanel.Visible, "action totals hide immediately when ending the turn");
            Require(await Task.WhenAny(phase, Task.Delay(20000)) == phase, "actual phase completes");
            await phase;
            Require(_enemy.Life == life - installedAttackDamage, "end-turn path triggers installed attack on installation and following round");
            Require(!_battle.GetPlayerTeamBattleHand().Any(card => card != null), "end-turn discards hand after installed effects");
            Require(_battle.GetInstalledActionCards(_players[0]).Length == 3, "installed cards persist across actual rounds");
        }
        GD.Print("[ActionCardRegression] real queue/refund/end-turn/persistence/visual PASS");
    }
    private async Task CheckSnapshotAndLastKill() {
        ClearActions();
        var attack = Card(SkillID.BasicAttack);
        var defense = Card(SkillID.BasicDefense);
        await attack.Effect();
        void AddDuringTrigger(PlayerCharacter owner, int index) {
            if (!_battle.IsActionCardInstalled(defense))
                _battle.InstallActionCard(defense, 0);
        }
        _battle.ActionCardTriggered += AddDuringTrigger;
        int block = _players[0].Block;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(_players[0].Block == block, "cards installed during execution wait until the next snapshot");
        _battle.ActionCardTriggered -= AddDuringTrigger;
        await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
        Require(_players[0].Block == block + 5, "new snapshot includes newly installed card");
        ClearActions();
        var lethal = new LethalProbe { SkillId = SkillID.SacredOnslaught, OwnerCharater = _players[0] };
        await lethal.Effect();
        await Card(SkillID.BasicDefense).Effect();
        using (_battle.PushEffectSource(_players[0], "last-kill regression")) {
            _enemy.Life = 1;
            block = _players[0].Block;
            await _battle.ResolveInstalledActionCardsAsync(CancellationToken.None);
            Require(_players[0].Block == block + 3, "last kill completes same-card block and skips later action");
            Require(!_battle.IsActionCardInstalled(lethal) && lethal.RemainingExecutions == 0, "lethal action exhausts after full plan");
            _enemy.Life = 1000;
            _enemy.State = Character.CharacterState.Normal;
        }
        ClearActions();
        GD.Print("[ActionCardRegression] snapshot/last-kill/remaining-effects PASS");
    }
    private bool ControlFlag(string name) => (bool)typeof(CharacterControl).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_battle.CharacterControl);
    private async Task Until(Func<bool> condition) {
        ulong deadline = Time.GetTicksMsec() + 12000;
        while (!condition()) { Require(Time.GetTicksMsec() < deadline, "wait timeout"); await Frames(1); }
        await Frames(2);
    }
    private async Task Frames(int count = 8) {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private static void MovePointer(Vector2 position) => Input.ParseInputEvent(new InputEventMouseMotion { Position = position, GlobalPosition = position });
    private async Task Click(Vector2 position) {
        MovePointer(position); await Frames(3);
        Input.ParseInputEvent(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private static void PressEscape() {
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = false });
    }
    private async Task Capture(string file) {
        await Frames(6);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        string directory = ProjectSettings.GlobalizePath("res://tmp/action_cards_battle");
        Directory.CreateDirectory(directory);
        Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(directory, file)) == Error.Ok, "screenshot saved");
    }
    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
    private partial class TwoUseXProbe : EchonicResonance {
        public override int ExhaustAfterExecutions => 2;
    }
    private partial class TwoUseImmediateProbe : Skill {
        public override SkillTypes SkillType => SkillTypes.Special;
        public override int EnergyCost => 0;
        public override int ExhaustAfterExecutions => 2;
        protected override SkillPlan BuildPlan() => new(this, BlockStep(baseBlock: 1, multiplier: 0));
    }
    private partial class OneUseImmediateProbe : TwoUseImmediateProbe {
        public override int ExhaustAfterExecutions => 1;
    }
    private partial class LethalProbe : Skill {
        public override SkillTypes SkillType => SkillTypes.Attack;
        public override int EnergyCost => 0;
        public override int ExhaustAfterExecutions => 1;
        protected override SkillPlan BuildPlan() => new(this, AttackStep(baseDamage: 0, multiplier: 1), BlockStep(baseBlock: 3, multiplier: 0));
    }
}
