public enum BattleCardKeyword
{
    Retain,
    ExhaustAfterUse,
    Voidness,
}

public static class BattleCardKeywordExtensions
{
    public static string GetDisplayName(this BattleCardKeyword keyword) =>
        keyword switch
        {
            BattleCardKeyword.Retain => I18n.Tr("keyword.retain", Skill.RetainKeyword),
            BattleCardKeyword.ExhaustAfterUse => I18n.Tr("keyword.exhaust", Skill.ExhaustKeyword),
            BattleCardKeyword.Voidness => I18n.Tr("keyword.voidness", Skill.VoidnessKeyword),
            _ => string.Empty,
        };

    public static string GetEffectText(this BattleCardKeyword keyword) =>
        keyword switch
        {
            BattleCardKeyword.Retain =>
                I18n.Tr("keyword.retain.effect", Skill.RetainKeywordEffectText),
            BattleCardKeyword.ExhaustAfterUse =>
                I18n.Tr("keyword.exhaust.effect", Skill.ExhaustKeywordEffectText),
            BattleCardKeyword.Voidness =>
                I18n.Tr("keyword.voidness.effect", Skill.VoidnessKeywordEffectText),
            _ => string.Empty,
        };
}
