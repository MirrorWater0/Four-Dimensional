using System.Collections.Generic;

public partial class Battle {
    // Counts follow physical card IDs, including when a partially used card is discarded and drawn again.
    private readonly Dictionary<ulong, int> _battleCardExecutionCounts = new();

    public int GetBattleCardExecutionCount(Skill skill) => skill?.BattleCardInstanceId is > 0
        && _battleCardExecutionCounts.TryGetValue(skill.BattleCardInstanceId, out int count) ? count : 0;

    internal void RecordBattleCardExecution(Skill skill) {
        if (skill?.OwnerCharater is not PlayerCharacter || skill.ExhaustExecutionLimit <= 0)
            return;
        ulong id = GetOrCreateBattleCardInstanceId(skill);
        _battleCardExecutionCounts[id] = GetBattleCardExecutionCount(skill) + 1;
        skill.UpdateDescription();
        NotifyHandPreviewContextChanged();
    }
}
