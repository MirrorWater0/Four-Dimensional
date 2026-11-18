public enum BattleCardKeyword
{
    Retain,
    ExhaustAfterUse,
    Voidness,
    Immediate,
}

public static class BattleCardKeywordExtensions
{
    public static string GetDisplayName(this BattleCardKeyword keyword) =>
        keyword switch
        {
            BattleCardKeyword.Retain => I18n.Tr("keyword.retain", Skill.RetainKeyword),
            BattleCardKeyword.ExhaustAfterUse => $"{I18n.Tr("keyword.exhaust", Skill.ExhaustKeyword)}1",
            BattleCardKeyword.Voidness => I18n.Tr("keyword.voidness", Skill.VoidnessKeyword),
            BattleCardKeyword.Immediate => I18n.Tr("keyword.immediate", Skill.ImmediateKeyword),
            _ => string.Empty,
        };

    public static string GetEffectText(this BattleCardKeyword keyword) =>
        keyword switch
        {
            BattleCardKeyword.Retain =>
                I18n.Tr("keyword.retain.effect", Skill.RetainKeywordEffectText),
            BattleCardKeyword.ExhaustAfterUse =>
                I18n.Tr("keyword.exhaust_execution.effect", "执行1次后，本场战斗中移出。"),
            BattleCardKeyword.Voidness =>
                I18n.Tr("keyword.voidness.effect", Skill.VoidnessKeywordEffectText),
            BattleCardKeyword.Immediate =>
                I18n.Tr("keyword.immediate.effect", Skill.ImmediateKeywordEffectText),
            _ => string.Empty,
        };
}
