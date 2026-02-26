using System;
using System.Linq;
using Godot;

public partial class Mariya : PlayerCharacter
{
    public override PackedScene CharaterScene { get; set; } = StartInterface._Mariya;
    public override string CharacterName { get; set; } = "Mariya";

    private const int PassiveHealBase = 4;

    public override void EndAction()
    {
        TryHealLowestAlly();
        base.EndAction();
    }

    private void TryHealLowestAlly()
    {
        if (BattleNode == null)
            return;

        var allies = IsPlayer
            ? BattleNode.PlayersList.Cast<Character>()
            : BattleNode.EnemiesList.Cast<Character>();

        var target = allies
            .Where(x => x.Life < x.BattleMaxLife && x.State == CharacterState.Normal)
            .OrderBy(x => x.Life)
            .FirstOrDefault();

        if (target == null)
            return;

        target.Recover(PassiveHealBase);
    }
}
