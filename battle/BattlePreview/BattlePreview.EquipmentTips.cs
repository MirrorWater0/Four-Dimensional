using System.Text;

public partial class BattlePreview
{
    private static string BuildPlayerPropertyPreviewText(PlayerInfoStructure info, string name)
    {
        var sb = new StringBuilder(128);
        sb.Append($"[b]{name}[/b]\n");
        sb.Append($"{I18n.Tr("ui.common.life", "生命")}：{info.LifeMax}\n");
        sb.Append($"{I18n.Tr("property.power", "力量")}：{TalentTree.GetEffectivePower(info)}\n");
        sb.Append(
            $"{I18n.Tr("property.survivability", "生存")}：{TalentTree.GetEffectiveSurvivability(info)}\n"
        );

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }
}
