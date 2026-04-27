using System.Collections.Generic;
using System.Text;

public partial class Equipment
{
    public static bool HasSpecialEffect(Equipment equipment)
    {
        return !string.IsNullOrWhiteSpace(GetSpecialEffectText(equipment));
    }

    public static string GetSpecialEffectText(Equipment equipment)
    {
        if (equipment == null)
            return string.Empty;

        return equipment.Name switch
        {
            EquipmentName.ShockPendant =>
                "战斗开始时，如果敌方有与装备者站位相同的角色，令其获得1层眩晕。",
            EquipmentName.OverloadMark => "战斗开始时，获得2层额外力量。",
            _ => string.Empty,
        };
    }

    public static string BuildSpecialEffectTooltipSection(IEnumerable<Equipment> equipments)
    {
        if (equipments == null)
            return string.Empty;

        StringBuilder sb = null;
        foreach (var equipment in equipments)
        {
            string effectText = GetSpecialEffectText(equipment);
            if (string.IsNullOrWhiteSpace(effectText))
                continue;

            sb ??= new StringBuilder(128);
            if (sb.Length == 0)
                sb.Append("[b]装备特效[/b]\n");
            else
                sb.Append('\n');

            string name = string.IsNullOrWhiteSpace(equipment.DisplayName)
                ? equipment.Name.ToString()
                : equipment.DisplayName;
            sb.Append($"[color=#ffd36b]{name}[/color]\n");
            sb.Append(effectText);
            sb.Append('\n');
        }

        if (sb == null)
            return string.Empty;

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }
}
