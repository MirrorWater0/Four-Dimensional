using Godot;

// Shared by the map and the observation terminal: thumbnails match the actual road.
public static class WorldLineVisuals {
    private static readonly Color[] Colors =
    [
        new("#AACFEC"), new("#B6ABE6"), new("#DCC496"),
        new("#DFA6AC"), new("#91CCC3"),
    ];

    public static Color GetColor(int worldLine) => Colors[GameInfo.NormalizeWorldLine(worldLine)];

    public static Vector2 GetPoint(float progress, int worldLine) {
        float t = Mathf.Clamp(progress, 0f, 1f);
        float phase = t * Mathf.Tau;
        float wave = GameInfo.NormalizeWorldLine(worldLine) switch {
            1 => 0.68f * Mathf.Sin(phase * 2f) + 0.22f * Mathf.Sin(phase),
            2 => -0.82f * Mathf.Sin(phase * 0.5f) + 0.18f * Mathf.Sin(phase * 1.5f),
            3 => 0.58f * Mathf.Sin(phase * 3f) + 0.28f * Mathf.Sin(phase),
            4 => -0.72f * Mathf.Sin(phase) + 0.26f * Mathf.Sin(phase * 2f),
            _ => Mathf.Sin(phase),
        };
        return new Vector2(t, 0.5f - wave * 0.42f);
    }
}
