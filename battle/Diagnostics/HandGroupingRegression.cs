using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class HandGroupingDebugRunner
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private async Task RunHandAnimationRegressionAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var registry = new PlayerCharacterRegistry();
        GameInfo.PlayerCharacters = new[] { registry.Echo, registry.Mariya, registry.Kasiya };
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            info.PositionIndex = i + 1;
            info.GainedSkills = new List<SkillID> { SkillID.BasicAttack, SkillID.BasicDefense };
            GameInfo.PlayerCharacters[i] = info;
        }

        var map = GD.Load<PackedScene>("res://Map/Map.tscn").Instantiate<Map>();
        map.Name = "Map";
        map.WarmupMode = true;
        GetTree().Root.AddChild(map);
        Battle battle = GD.Load<PackedScene>("res://battle/Battle.tscn").Instantiate<Battle>();
        battle.WarmupMode = true;
        AddChild(battle);
        try
        {
            var players = new PlayerCharacter[3];
            for (int i = 0; i < players.Length; i++)
            {
                players[i] = GD.Load<PackedScene>(GameInfo.PlayerCharacters[i].CharacterScenePath)
                    .Instantiate<PlayerCharacter>();
                players[i].WarmupMode = true;
                players[i].CharacterIndex = i;
                players[i].BattleNode = battle;
                battle.Left.AddChild(players[i]);
                battle.PlayersList.Add(players[i]);
                players[i].Initialize();
            }
            var enemy = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn")
                .Instantiate<EnemyCharacter>();
            enemy.WarmupMode = true;
            enemy.BattleNode = battle;
            enemy.Registry = new EvilRegedit { MaxLife = 1000, CurrentLife = 1000, PositionIndex = 2 };
            battle.Right.AddChild(enemy);
            battle.EnemiesList.Add(enemy);
            enemy.Initialize();
            SetPrivate(battle, "_isResolvingPlayerTeamActionPhase", true);
            battle.UpdataPlayerEnergySilently(20);
            CharacterControl control = battle.CharacterControl;
            control.ShowPlayerTurn(players[0]);
            await WaitSecondsAsync(0.1);

            // Insert a second batch before the first batch's shuffle delay has expired.
            Skill[] oldHand = battle.GetPlayerTeamBattleHand().ToArray();
            InsertCard(battle, CardFor(players[0]));
            InsertCard(battle, CardFor(players[1]));
            control.PlayBattleDeckShuffleAnimation(4);
            control.QueueBatchDrawMakeRoomAnimation(oldHand, battle.GetPlayerTeamBattleHand());
            control.RefreshCurrentTurnUi();
            await WaitSecondsAsync(0.03);
            Check(GetPrivate<HashSet<int>>(control, "_hiddenPendingDrawEntrySlotIndexes").Count > 0,
                "shuffle holds first batch hidden");
            oldHand = battle.GetPlayerTeamBattleHand().ToArray();
            InsertCard(battle, CardFor(players[2]));
            InsertCard(battle, CardFor(players[1]));
            control.QueueBatchDrawMakeRoomAnimation(oldHand, battle.GetPlayerTeamBattleHand());
            control.RefreshCurrentTurnUi();
            await WaitForHandEntriesAsync(control);
            CheckSettledHand(battle, "insertion during shuffle delay");

            // Insert while entries are already flying; callbacks used to retain stale indexes.
            oldHand = battle.GetPlayerTeamBattleHand().ToArray();
            InsertCard(battle, CardFor(players[0]));
            InsertCard(battle, CardFor(players[1]));
            control.QueueBatchDrawMakeRoomAnimation(oldHand, battle.GetPlayerTeamBattleHand());
            control.RefreshCurrentTurnUi();
            await WaitSecondsAsync(0.06);
            Check(GetPrivate<Dictionary<int, SkillCard>>(control, "_drawEntryPreviewCards").Count > 0,
                "first batch actively flying");
            oldHand = battle.GetPlayerTeamBattleHand().ToArray();
            InsertCard(battle, CardFor(players[2]));
            control.QueueBatchDrawMakeRoomAnimation(oldHand, battle.GetPlayerTeamBattleHand());
            control.RefreshCurrentTurnUi();
            await WaitForHandEntriesAsync(control);
            CheckSettledHand(battle, "insertion during active flight");

            // Exercise the real turn-start path: empty draw pile, shuffle discard, continue drawing.
            foreach (int i in Enumerable.Range(0, PlayerCharacter.MaxBattleHandSize).Reverse())
                battle.RemovePlayerTeamBattleHandCardAt(i, queueReorderAnimation: false);
            control.RefreshCurrentTurnUi();
            object piles = Activator.CreateInstance(typeof(Battle)
                .GetNestedType("PlayerTeamBattleCardPiles", BindingFlags.NonPublic), nonPublic: true);
            SetPrivate(battle, "_playerTeamBattleCardPiles", piles);
            var draw = (List<Battle.BattleCardPileEntry>)piles.GetType().GetField("DrawPile").GetValue(piles);
            var discard = (List<Battle.BattleCardPileEntry>)piles.GetType().GetField("DiscardPile").GetValue(piles);
            draw.Add(new(players[0], SkillID.BasicDefense, 201));
            draw.Add(new(players[1], SkillID.BasicDefense, 202));
            discard.Add(new(players[2], SkillID.BasicDefense, 203));
            discard.Add(new(players[1], SkillID.BasicDefense, 204));
            discard.Add(new(players[0], SkillID.BasicDefense, 205));
            var drawTask = (Task)typeof(Battle)
                .GetMethod("DrawPlayerTeamTurnStartCardsWithShuffleBreaksAsync", PrivateInstance)
                .Invoke(battle, new object[] { 5, CancellationToken.None });
            await drawTask;
            await WaitForHandEntriesAsync(control);
            Check(battle.GetPlayerTeamBattleHand().Count(card => card != null) == 5, "five actual drawn cards");
            Check(discard.Count == 0 && draw.Count == 0, "discard reshuffled and drawn");
            CheckSettledHand(battle, "real turn-start shuffle and draw");

            Skill[] hand = battle.GetPlayerTeamBattleHand();
            Skill frontFirst = hand.First(card => card?.OwnerCharater == players[0]);
            Check(frontFirst.BattleCardInstanceId == 201, "same owner's draw order remains stable across shuffle");
            SetGrouping(false);
            Skill appended = CardFor(players[2]);
            int index = InsertCard(battle, appended);
            Check(index == 5 && ReferenceEquals(battle.GetPlayerTeamBattleHand()[5], appended), "disabled appends");
            SetGrouping(true);
            GD.Print("[HandGroupingDebugRunner] PASS stable owner order and disabled append");
        }
        finally
        {
            battle.QueueFree();
            map.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void CheckSettledHand(Battle battle, string scenario)
    {
        Skill[] hand = battle.GetPlayerTeamBattleHand();
        int[] formation = hand.Where(card => card != null)
            .Select(card => card.OwnerCharater.PositionIndex).ToArray();
        Check(formation.SequenceEqual(formation.OrderByDescending(index => index)), $"{scenario}: right-to-left order");
        var control = battle.CharacterControl;
        var cards = GetPrivate<SkillCard[]>(control, "_cards");
        var slots = GetPrivate<Control[]>(control, "_cardSlots");
        for (int i = 0; i < hand.Length && hand[i] != null; i++)
        {
            Check(cards[i] != null && cards[i].Visible, $"{scenario}: slot {i} visible");
            Check(ReferenceEquals(cards[i].CurrentSkill, hand[i]), $"{scenario}: slot {i} skill identity");
            Check(!cards[i].Button.Disabled, $"{scenario}: slot {i} input enabled");
            if (i > 0)
                Check(slots[i].Position.X > slots[i - 1].Position.X, $"{scenario}: physical left-to-right slots");
        }
        Check(GetPrivate<HashSet<int>>(control, "_hiddenPendingDrawEntrySlotIndexes").Count == 0,
            $"{scenario}: no permanently hidden cards");
        Check(GetPrivate<HashSet<int>>(control, "_pendingDrawEntryAnimations").Count == 0,
            $"{scenario}: no stranded pending animations");
        Check(GetPrivate<HashSet<int>>(control, "_drawEntrySlotIndexes").Count == 0,
            $"{scenario}: all entries finished");
        Check(GetPrivate<Dictionary<int, SkillCard>>(control, "_drawEntryPreviewCards").Count == 0,
            $"{scenario}: no stranded flying cards");
        GD.Print($"[HandGroupingDebugRunner] PASS {scenario}: all cards visible, ordered, interactive, animations finished");
    }

    private async Task WaitSecondsAsync(double seconds) =>
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    private async Task WaitForHandEntriesAsync(CharacterControl control)
    {
        // Shuffle delays use wall-clock ticks. Headless frame time can advance faster
        // than wall time, so a SceneTreeTimer alone can finish before the shuffle does.
        ulong deadline = Time.GetTicksMsec() + 8000;
        int quietFrames = 0;
        while (Time.GetTicksMsec() < deadline)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            bool idle = GetPrivate<HashSet<int>>(control, "_hiddenPendingDrawEntrySlotIndexes").Count == 0
                && GetPrivate<HashSet<int>>(control, "_pendingDrawEntryAnimations").Count == 0
                && GetPrivate<HashSet<int>>(control, "_drawEntrySlotIndexes").Count == 0
                && GetPrivate<Dictionary<int, SkillCard>>(control, "_drawEntryPreviewCards").Count == 0
                && !GetPrivate<bool[]>(control, "_cardSlotLayoutFollowActive").Any(active => active);
            quietFrames = idle ? quietFrames + 1 : 0;
            if (quietFrames >= 3)
                return;
        }
    }

    private static Skill CardFor(PlayerCharacter player)
    {
        Skill card = Skill.GetSkill(SkillID.BasicDefense);
        card.OwnerCharater = player;
        card.UpdateDescription();
        return card;
    }

    private static int InsertCard(Battle battle, Skill card) => (int)typeof(Battle)
        .GetMethod("InsertPlayerTeamBattleHandCard", PrivateInstance).Invoke(battle, new object[] { card });

    private static T GetPrivate<T>(object target, string field) =>
        (T)target.GetType().GetField(field, PrivateInstance).GetValue(target);

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
}
