using Godot;

public static class CharacterPlateColors
{
    public static readonly Color EchoColor = new(0.86f, 0.42f, 1.0f, 1f);
    public static readonly Color KasiyaColor = new(0.96f, 0.36f, 0.32f, 1f);
    public static readonly Color MariyaColor = new(0.36f, 0.80f, 0.52f, 1f);
    public static readonly Color NightingaleColor = new(0.10f, 0.28f, 0.72f, 1f);
    public static readonly Color ColorlessColor = new(0.58f, 0.60f, 0.66f, 1f);

    public static bool TryGetColor(string key, out Color color)
    {
        key = NormalizeKey(key);
        color = key switch
        {
            "Echo" => EchoColor,
            "Kasiya" => KasiyaColor,
            "Mariya" => MariyaColor,
            "Nightingale" => NightingaleColor,
            _ => default,
        };

        return key is "Echo" or "Kasiya" or "Mariya" or "Nightingale";
    }

    public static string NormalizeKey(string value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized switch
        {
            "Echo" or "回声" => "Echo",
            "Kasiya" or "卡西亚" => "Kasiya",
            "Mariya" or "玛瑞娅" => "Mariya",
            "Nightingale" or "夜莺" => "Nightingale",
            _ => normalized,
        };
    }
}
