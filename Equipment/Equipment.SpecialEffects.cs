using System.Collections.Generic;

public partial class Equipment
{
    public static bool HasSpecialEffect(Equipment equipment) => false;

    public static string GetSpecialEffectText(Equipment equipment) => string.Empty;

    public static string BuildSpecialEffectTooltipSection(IEnumerable<Equipment> equipments) =>
        string.Empty;
}
