using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle
{
    internal async Task ResolvePlayerTurnEndAttacksAsync(CancellationToken token)
    {
        // Snapshot both order and budgets: attack-triggered changes cannot extend this queue indefinitely.
        var attacks = GetPlayerPhaseActionOrder()
            .Select(player => (Player: player, Count: (long)AttackCountBuff.GetCount(player)
                + AttackCountBuff.GetCount(player, temporary: true)))
            .ToArray();
        try
        {
            foreach (var entry in attacks)
            {
                for (long hit = 0; hit < entry.Count; hit++)
                {
                    if (!CanContinue(token) || HasBattleEnded())
                        return;
                    if (!IsCharacterAlive(entry.Player))
                        break;

                    using (PushEffectSource(entry.Player, "回合结束攻击"))
                        await Skill.ExecuteTurnEndAttackAsync(entry.Player);
                }
            }
        }
        finally
        {
            // Clear dead/skipped characters as well, so reviving cannot carry this-turn attacks forward.
            foreach (PlayerCharacter player in PlayersList.ToArray())
                if (player != null && GodotObject.IsInstanceValid(player))
                    AttackCountBuff.ClearTemporary(player);
        }
    }
}
