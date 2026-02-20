using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Reward : CanvasLayer
{
    private static readonly PackedScene RewardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/Reward.tscn"
    );
    private static readonly PackedScene RewardCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/RewardCard.tscn"
    );

    // Call from anywhere (e.g. Battle) to show the reward UI.
    public static Reward Show(Node caller)
    {
        var tree = caller.GetTree();
        var root = tree.Root;

        var existing = root.GetNodeOrNull<Reward>("Reward");
        if (existing != null)
        {
            existing.CallDeferred(nameof(Open));
            return existing;
        }

        var reward = RewardScene.Instantiate<Reward>();
        reward.Name = "Reward";
        reward.Layer = 1;
        root.AddChild(reward);
        reward.CallDeferred(nameof(Open));
        return reward;
    }

    // Reward UI:
    // - Shows one RewardCard per PlayerCharacter (max 4).
    // - Each card offers a skill picked from that player's AllSkills (prefers not yet in GainedSkills).
    // - Clicking one card adds the SkillID to that player's GainedSkills and hides the other cards.
    private HBoxContainer RewardsContainer => field ??= GetNode<HBoxContainer>("HBoxContainer");
    ColorRect BG => field ??= GetNode<ColorRect>("BG");
    private readonly List<RewardCard> _rewardSlots = new();

    // Parallel arrays used to map a UI slot -> (playerIndex, skillId).
    private SkillID?[] _offeredSkillIds = Array.Empty<SkillID?>();
    private int[] _offeredPlayerIndexes = Array.Empty<int>();
    private bool _picked;

    public override void _Ready()
    {
        CacheRewardSlots();
        WireSlotButtons();
        Visible = false;
    }

    /// <summary>Show reward UI and (re)roll offers for each player.</summary>
    public void Open()
    {
        _picked = false;
        Visible = true;
        BuildRewards();

        for (int i = 0; i < _rewardSlots.Count; i++)
        {
            _rewardSlots[i].ResetState();
            if (_rewardSlots[i].Visible)
                _rewardSlots[i].StartAnimation(0.05f * i + 0.3f);
        }
    }

    private void CacheRewardSlots()
    {
        _rewardSlots.Clear();
        _rewardSlots.AddRange(RewardsContainer.GetChildren().OfType<RewardCard>());

        const int expectedSlots = 4;
        if (_rewardSlots.Count < expectedSlots)
        {
            for (int i = _rewardSlots.Count; i < expectedSlots; i++)
            {
                var slot = RewardCardScene.Instantiate<RewardCard>();
                slot.Name = $"RewardCard{i + 1}";
                RewardsContainer.AddChild(slot);
                _rewardSlots.Add(slot);
            }
        }
    }

    /// <summary>Connect each card's button once; clicking a slot picks that reward.</summary>
    private void WireSlotButtons()
    {
        for (int i = 0; i < _rewardSlots.Count; i++)
        {
            int slotIndex = i;
            var slot = _rewardSlots[slotIndex];
            slot.Button.Pressed += () => PickReward(slotIndex);
        }
    }

    /// <summary>Populate each visible slot with its offered skill (one offer per player).</summary>
    private void BuildRewards()
    {
        var players =
            GameInfo.PlayerCharacters
            ?? throw new InvalidOperationException("GameInfo.PlayerCharacters is null.");

        int count = Math.Min(players.Length, _rewardSlots.Count);
        _offeredSkillIds = new SkillID?[count];
        _offeredPlayerIndexes = new int[count];

        var rng = new Random(GameInfo.Seed);

        for (int i = 0; i < _rewardSlots.Count; i++)
        {
            _rewardSlots[i].Visible = i < count;
            _rewardSlots[i].Button.Disabled = i >= count;
        }

        for (int i = 0; i < count; i++)
        {
            int playerIndex = i;
            var info = players[playerIndex];

            var pickedId = PickSkillId(info, rng);
            _offeredSkillIds[i] = pickedId;
            _offeredPlayerIndexes[i] = playerIndex;

            var slot = _rewardSlots[i];
            if (pickedId == null)
            {
                slot.SetSkill(null);
                slot.Button.Disabled = true;
                continue;
            }

            var skill = Skill.GetSkill(pickedId.Value);
            if (skill == null)
            {
                slot.SetSkill(null);
                slot.Button.Disabled = true;
                continue;
            }

            slot.CharacterName.Text = GameInfo.PlayerCharacters[playerIndex].CharacterName;
            skill.SetPreviewStats(info.Power, info.Survivability, 1);
            slot.SetSkill(skill);
        }
    }

    private static SkillID? PickSkillId(PlayerInfoStructure info, Random rng)
    {
        var pool = info.AllSkills;
        var gained = info.GainedSkills ?? new List<SkillID>();
        var candidates = pool.Where(id => !gained.Contains(id)).ToArray();
        if (candidates.Length == 0)
            candidates = pool;

        return candidates[rng.Next(0, candidates.Length)];
    }

    /// <summary>Pick the reward from a slot, add it to the corresponding player, and hide the other slots.</summary>
    private void PickReward(int slotIndex)
    {
        if (_picked)
            return;
        _picked = true;

        var skillId = _offeredSkillIds[slotIndex]!.Value;
        int playerIndex = _offeredPlayerIndexes[slotIndex];

        var players =
            GameInfo.PlayerCharacters
            ?? throw new InvalidOperationException("GameInfo.PlayerCharacters is null.");

        var info = players[playerIndex];
        info.GainedSkills ??= new List<SkillID>();
        if (!info.GainedSkills.Contains(skillId))
            info.GainedSkills.Add(skillId);
        players[playerIndex] = info;
        GameInfo.PlayerCharacters = players;

        for (int i = 0; i < _rewardSlots.Count; i++)
        {
            _rewardSlots[i].Button.Disabled = true;
        }

        for (int i = 0; i < _rewardSlots.Count; i++)
        {
            var slot = _rewardSlots[i];
            if (i == slotIndex)
                continue;
            if (!slot.Visible)
                continue;
            slot.Vanish();
        }

        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(BG, "modulate", new Color(1, 1, 1, 0), 0.4f);
        tween.SetParallel(false);
        tween.TweenInterval(0.3f);
        tween.TweenCallback(
            Callable.From(() =>
            {
                BG.Modulate = new Color(1, 1, 1, 1);
                Visible = false;
            })
        );
    }
}
