using System.Collections.Generic;

public partial class Battle
{
    private readonly HashSet<(ulong InstanceId, BattleCardKeyword Keyword)> _battleCardKeywords = new();

    public void AddBattleCardKeyword(ulong instanceId, BattleCardKeyword keyword)
    {
        if (instanceId == 0)
            return;

        if (!_battleCardKeywords.Add((instanceId, keyword)))
            return;

        InvalidatePlayerTeamSkillTooltips();
    }

    public void AddBattleCardKeyword(Skill skill, BattleCardKeyword keyword)
    {
        if (skill == null)
            return;

        AddBattleCardKeyword(GetOrCreateBattleCardInstanceId(skill), keyword);
    }

    public bool HasBattleCardKeyword(Skill skill, BattleCardKeyword keyword) =>
        skill != null
        && skill.BattleCardInstanceId != 0
        && _battleCardKeywords.Contains((skill.BattleCardInstanceId, keyword));
}
