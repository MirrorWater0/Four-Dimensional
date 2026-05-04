using System.Text;

public partial class BattlePreview
{
    private static string BuildPlayerEquipmentTipText(PlayerInfoStructure info)
    {
        return Equipment.BuildSpecialEffectTooltipSection(info.Equipments);
    }

    private static string BuildPlayerPropertyPreviewText(PlayerInfoStructure info, string name)
    {
        var sb = new StringBuilder(128);
        int lifeBonus = SumEquipmentBonus(info, x => x.MaxLife);
        int powerBonus = SumEquipmentBonus(info, x => x.Power);
        int surviveBonus = SumEquipmentBonus(info, x => x.Survivability);
        int speedBonus = SumEquipmentBonus(info, x => x.Speed);

        sb.Append($"[b]{name}[/b]\n");
        sb.Append($"生命：{info.LifeMax + lifeBonus}({FormatPreviewBonus(lifeBonus)})\n");
        sb.Append($"力量：{info.Power + powerBonus}({FormatPreviewBonus(powerBonus)})\n");
        sb.Append($"生存：{info.Survivability + surviveBonus}({FormatPreviewBonus(surviveBonus)})\n");
        sb.Append($"速度：{info.Speed + speedBonus}({FormatPreviewBonus(speedBonus)})\n");

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private static string FormatPreviewBonus(int value)
    {
        return value.ToString("+0;-0;0");
    }
}
