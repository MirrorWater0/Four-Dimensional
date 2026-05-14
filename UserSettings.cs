using Godot;

public static class UserSettings
{
    private const string SettingsPath = "user://settings.cfg";
    private const string SectionName = "Preferences";
    private const string CompactBattleCardDescriptionsKey = "CompactBattleCardDescriptions";
    private const string BattleTurnOrderPreviewKey = "BattleTurnOrderPreview";
    private const string EnemyAttackPreviewKey = "EnemyAttackPreview";

    private static bool _loaded;

    public static bool UseCompactBattleCardDescriptions { get; private set; }
    public static bool ShowBattleTurnOrderPreview { get; private set; } = true;
    public static bool ShowEnemyAttackPreview { get; private set; }

    public static void EnsureLoaded()
    {
        if (!_loaded)
            Load();
    }

    public static void Load()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) == Error.Ok)
        {
            UseCompactBattleCardDescriptions = config
                .GetValue(
                    SectionName,
                    CompactBattleCardDescriptionsKey,
                    UseCompactBattleCardDescriptions
                )
                .AsBool();
            ShowBattleTurnOrderPreview = config
                .GetValue(SectionName, BattleTurnOrderPreviewKey, ShowBattleTurnOrderPreview)
                .AsBool();
            ShowEnemyAttackPreview = config
                .GetValue(SectionName, EnemyAttackPreviewKey, ShowEnemyAttackPreview)
                .AsBool();
        }

        _loaded = true;
    }

    public static void SetCompactBattleCardDescriptions(bool value)
    {
        EnsureLoaded();
        UseCompactBattleCardDescriptions = value;
        Save();
    }

    public static void SetBattleTurnOrderPreview(bool value)
    {
        EnsureLoaded();
        ShowBattleTurnOrderPreview = value;
        Save();
    }

    public static void SetEnemyAttackPreview(bool value)
    {
        EnsureLoaded();
        ShowEnemyAttackPreview = value;
        Save();
    }

    public static void Save()
    {
        var config = new ConfigFile();
        config.SetValue(
            SectionName,
            CompactBattleCardDescriptionsKey,
            UseCompactBattleCardDescriptions
        );
        config.SetValue(SectionName, BattleTurnOrderPreviewKey, ShowBattleTurnOrderPreview);
        config.SetValue(SectionName, EnemyAttackPreviewKey, ShowEnemyAttackPreview);
        config.Save(SettingsPath);
    }
}
