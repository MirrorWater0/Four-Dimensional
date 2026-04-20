using System;
using System.Linq;
using Godot;

public partial class Mariya : PlayerCharacter
{
    private const int PassiveHealBase = 4;

    public const string PassiveNameText = "治愈";
    public static string PassiveDescriptionText =>
        $"自己回合结束时：回复最低生命队友{PassiveHealBase}点基础生命。";

    public override PackedScene CharaterScene { get; set; } = StartInterface._Mariya;
    public override string CharacterName { get; set; } = "Mariya";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnEnd()
    {
        TryHealLowestAlly();
        base.OnTurnEnd();
    }

    private void TryHealLowestAlly()
    {
        if (BattleNode == null)
            return;

        using var _ = BeginEffectSource("被动");

        var allies = IsPlayer
            ? BattleNode.PlayersList.Cast<Character>()
            : BattleNode.EnemiesList.Cast<Character>();

        var target = allies
            .Where(x => x.Life < x.BattleMaxLife && x.State == CharacterState.Normal)
            .OrderBy(x => x.Life)
            .FirstOrDefault();

        if (target == null)
            return;

        target.Recover(PassiveHealBase, source: this);
    }
}
