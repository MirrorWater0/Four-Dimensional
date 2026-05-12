using System.Text;

public partial class BattlePreview
{
    private static string BuildPlayerPropertyPreviewText(PlayerInfoStructure info, string name)
    {
        var sb = new StringBuilder(128);
        sb.Append($"[b]{name}[/b]\n");
        sb.Append($"生命：{info.LifeMax}\n");
        sb.Append($"力量：{info.Power}\n");
        sb.Append($"生存：{info.Survivability}\n");
        sb.Append($"速度：{info.Speed}\n");

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }
}
