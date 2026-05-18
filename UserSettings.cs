using System;
using Godot;

public static class UserSettings
{
    private const string SettingsPath = "user://settings.cfg";
    private const string SectionName = "Preferences";
    private const string CompactBattleCardDescriptionsKey = "CompactBattleCardDescriptions";
    private const string BattleTurnOrderPreviewKey = "BattleTurnOrderPreview";
    private const string EnemyAttackPreviewKey = "EnemyAttackPreview";
    private const string TextSizeLevelKey = "TextSizeLevel";
    private const string BattleShakeLevelKey = "BattleShakeLevel";

    public const int TextSizeLevelSmall = 0;
    public const int TextSizeLevelStandard = 1;
    public const int TextSizeLevelLarge = 2;
    public const int TextSizeLevelExtraLarge = 3;

    public const int BattleShakeLevelOff = 0;
    public const int BattleShakeLevelSmall = 1;
    public const int BattleShakeLevelStandard = 2;
    public const int BattleShakeLevelLarge = 3;

    private static bool _loaded;

    public static bool UseCompactBattleCardDescriptions { get; private set; }
    public static bool ShowBattleTurnOrderPreview { get; private set; } = true;
    public static bool ShowEnemyAttackPreview { get; private set; }
    public static int TextSizeLevel { get; private set; } = TextSizeLevelStandard;
    public static int BattleShakeLevel { get; private set; } = BattleShakeLevelStandard;

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
            TextSizeLevel = NormalizeTextSizeLevel(
                config.GetValue(SectionName, TextSizeLevelKey, TextSizeLevel).AsInt32()
            );
            BattleShakeLevel = NormalizeBattleShakeLevel(
                config.GetValue(SectionName, BattleShakeLevelKey, BattleShakeLevel).AsInt32()
            );
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

    public static void SetTextSizeLevel(int value)
    {
        EnsureLoaded();
        TextSizeLevel = NormalizeTextSizeLevel(value);
        Save();
    }

    public static void SetBattleShakeLevel(int value)
    {
        EnsureLoaded();
        BattleShakeLevel = NormalizeBattleShakeLevel(value);
        Save();
    }

    public static int NormalizeTextSizeLevel(int value) =>
        Math.Clamp(value, TextSizeLevelSmall, TextSizeLevelExtraLarge);

    public static int NormalizeBattleShakeLevel(int value) =>
        Math.Clamp(value, BattleShakeLevelOff, BattleShakeLevelLarge);

    public static string GetTextSizeLevelLabel(int value)
    {
        return NormalizeTextSizeLevel(value) switch
        {
            TextSizeLevelSmall => "小",
            TextSizeLevelLarge => "大",
            TextSizeLevelExtraLarge => "特大",
            _ => "标准",
        };
    }

    public static string GetBattleShakeLevelLabel(int value)
    {
        return NormalizeBattleShakeLevel(value) switch
        {
            BattleShakeLevelOff => "关闭",
            BattleShakeLevelSmall => "小",
            BattleShakeLevelLarge => "大",
            _ => "标准",
        };
    }

    public static float GetBattleShakeMultiplier()
    {
        EnsureLoaded();
        return BattleShakeLevel switch
        {
            BattleShakeLevelOff => 0.0f,
            BattleShakeLevelSmall => 0.6f,
            BattleShakeLevelLarge => 1.4f,
            _ => 1.0f,
        };
    }

    public static int ScaleTextFontSize(int baseFontSize)
    {
        EnsureLoaded();
        int delta = TextSizeLevel switch
        {
            TextSizeLevelSmall => -2,
            TextSizeLevelLarge => 3,
            TextSizeLevelExtraLarge => 6,
            _ => 0,
        };
        return Math.Max(8, baseFontSize + delta);
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
        config.SetValue(SectionName, TextSizeLevelKey, TextSizeLevel);
        config.SetValue(SectionName, BattleShakeLevelKey, BattleShakeLevel);
        config.Save(SettingsPath);
    }
}
