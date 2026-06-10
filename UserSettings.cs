using System;
using Godot;

public static class UserSettings
{
    private const string SettingsPath = "user://settings.cfg";
    private const string SectionName = "Preferences";
    private const string CompactBattleCardDescriptionsKey = "CompactBattleCardDescriptions";
    private const string BattleTurnOrderPreviewKey = "BattleTurnOrderPreview";
    private const string IncomingDamagePreviewKey = "IncomingDamagePreview";
    private const string ShowIntentionTargetNamesKey = "ShowIntentionTargetNames";
    private const string ShowSingleTargetDamageIntentionArrowsKey =
        "ShowSingleTargetDamageIntentionArrows";
    private const string HideEnemySkillsKey = "HideEnemySkills";
    private const string GroupBattlePilesByCharacterKey = "GroupBattlePilesByCharacter";
    private const string KeepManualTargetCardVisibleWhenHiddenKey =
        "KeepManualTargetCardVisibleWhenHidden";
    private const string UseArrowManualTargetSelectionKey = "UseArrowManualTargetSelection";
    private const string TextSizeLevelKey = "TextSizeLevel";
    private const string BattleShakeLevelKey = "BattleShakeLevel";
    private const string LastSelectedDifficultyKey = "LastSelectedDifficulty";
    private const string MasterVolumePercentKey = "MasterVolumePercent";
    private const string SfxVolumePercentKey = "SfxVolumePercent";
    private const string LocaleKey = "Locale";
    private const string WindowWidthKey = "WindowWidth";
    private const string WindowHeightKey = "WindowHeight";

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
    public static bool ShowIncomingDamagePreview { get; private set; }
    public static bool ShowIntentionTargetNames { get; private set; }
    public static bool ShowSingleTargetDamageIntentionArrows { get; private set; } = true;
    public static bool HideEnemySkills { get; private set; } = true;
    public static bool GroupBattlePilesByCharacter { get; private set; }
    public static bool KeepManualTargetCardVisibleWhenHidden { get; private set; } = true;
    public static bool UseArrowManualTargetSelection { get; private set; } = true;
    public static int TextSizeLevel { get; private set; } = TextSizeLevelStandard;
    public static int BattleShakeLevel { get; private set; } = BattleShakeLevelStandard;
    public static int LastSelectedDifficulty { get; private set; }
    public static int MasterVolumePercent { get; private set; } = 100;
    public static int SfxVolumePercent { get; private set; } = 100;
    public static string Locale { get; private set; } = "zh_CN";
    public static int WindowWidth { get; private set; }
    public static int WindowHeight { get; private set; }
    public static bool HasCustomWindowResolution => WindowWidth > 0 && WindowHeight > 0;

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
            ShowIncomingDamagePreview = config
                .GetValue(SectionName, IncomingDamagePreviewKey, ShowIncomingDamagePreview)
                .AsBool();
            ShowIntentionTargetNames = config
                .GetValue(SectionName, ShowIntentionTargetNamesKey, ShowIntentionTargetNames)
                .AsBool();
            ShowSingleTargetDamageIntentionArrows = config
                .GetValue(
                    SectionName,
                    ShowSingleTargetDamageIntentionArrowsKey,
                    ShowSingleTargetDamageIntentionArrows
                )
                .AsBool();
            HideEnemySkills = config
                .GetValue(SectionName, HideEnemySkillsKey, HideEnemySkills)
                .AsBool();
            GroupBattlePilesByCharacter = config
                .GetValue(
                    SectionName,
                    GroupBattlePilesByCharacterKey,
                    GroupBattlePilesByCharacter
                )
                .AsBool();
            KeepManualTargetCardVisibleWhenHidden = config
                .GetValue(
                    SectionName,
                    KeepManualTargetCardVisibleWhenHiddenKey,
                    KeepManualTargetCardVisibleWhenHidden
                )
                .AsBool();
            UseArrowManualTargetSelection = config
                .GetValue(
                    SectionName,
                    UseArrowManualTargetSelectionKey,
                    UseArrowManualTargetSelection
                )
                .AsBool();
            TextSizeLevel = NormalizeTextSizeLevel(
                config.GetValue(SectionName, TextSizeLevelKey, TextSizeLevel).AsInt32()
            );
            BattleShakeLevel = NormalizeBattleShakeLevel(
                config.GetValue(SectionName, BattleShakeLevelKey, BattleShakeLevel).AsInt32()
            );
            LastSelectedDifficulty = NormalizeDifficulty(
                config
                    .GetValue(SectionName, LastSelectedDifficultyKey, LastSelectedDifficulty)
                    .AsInt32()
            );
            MasterVolumePercent = NormalizeVolumePercent(
                config.GetValue(SectionName, MasterVolumePercentKey, MasterVolumePercent).AsInt32()
            );
            SfxVolumePercent = NormalizeVolumePercent(
                config.GetValue(SectionName, SfxVolumePercentKey, SfxVolumePercent).AsInt32()
            );
            Locale = NormalizeLocale(
                config.GetValue(SectionName, LocaleKey, Locale).AsString()
            );
            WindowWidth = NormalizeWindowDimension(
                config.GetValue(SectionName, WindowWidthKey, WindowWidth).AsInt32()
            );
            WindowHeight = NormalizeWindowDimension(
                config.GetValue(SectionName, WindowHeightKey, WindowHeight).AsInt32()
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

    public static void SetIncomingDamagePreview(bool value)
    {
        EnsureLoaded();
        ShowIncomingDamagePreview = value;
        Save();
    }

    public static void SetShowIntentionTargetNames(bool value)
    {
        EnsureLoaded();
        ShowIntentionTargetNames = value;
        Save();
    }

    public static void SetShowSingleTargetDamageIntentionArrows(bool value)
    {
        EnsureLoaded();
        ShowSingleTargetDamageIntentionArrows = value;
        Save();
    }

    public static void SetHideEnemySkills(bool value)
    {
        EnsureLoaded();
        HideEnemySkills = value;
        Save();
    }

    public static void SetGroupBattlePilesByCharacter(bool value)
    {
        EnsureLoaded();
        GroupBattlePilesByCharacter = value;
        Save();
    }

    public static void SetKeepManualTargetCardVisibleWhenHidden(bool value)
    {
        EnsureLoaded();
        KeepManualTargetCardVisibleWhenHidden = value;
        Save();
    }

    public static void SetUseArrowManualTargetSelection(bool value)
    {
        EnsureLoaded();
        UseArrowManualTargetSelection = value;
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

    public static void SetLastSelectedDifficulty(int value)
    {
        EnsureLoaded();
        LastSelectedDifficulty = NormalizeDifficulty(value);
        Save();
    }

    public static void SetMasterVolumePercent(int value)
    {
        EnsureLoaded();
        MasterVolumePercent = NormalizeVolumePercent(value);
        Save();
        AudioManager.RefreshSettings();
    }

    public static void SetSfxVolumePercent(int value)
    {
        EnsureLoaded();
        SfxVolumePercent = NormalizeVolumePercent(value);
        Save();
        AudioManager.RefreshSettings();
    }

    public static void SetLocale(string value)
    {
        EnsureLoaded();
        Locale = NormalizeLocale(value);
        Save();
    }

    public static void SetWindowResolution(int width, int height)
    {
        EnsureLoaded();
        WindowWidth = NormalizeWindowDimension(width);
        WindowHeight = NormalizeWindowDimension(height);
        Save();
    }

    public static void ClearWindowResolution()
    {
        EnsureLoaded();
        WindowWidth = 0;
        WindowHeight = 0;
        Save();
    }

    public static int NormalizeTextSizeLevel(int value) =>
        Math.Clamp(value, TextSizeLevelSmall, TextSizeLevelExtraLarge);

    public static int NormalizeBattleShakeLevel(int value) =>
        Math.Clamp(value, BattleShakeLevelOff, BattleShakeLevelLarge);

    public static int NormalizeDifficulty(int value) =>
        Math.Clamp(value, GameInfo.MinDifficulty, GameInfo.MaxDifficulty);

    public static int NormalizeVolumePercent(int value) => Math.Clamp(value, 0, 100);

    public static int NormalizeWindowDimension(int value) => value > 0 ? value : 0;

    public static string NormalizeLocale(string value)
    {
        return string.Equals(value, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "zh_CN";
    }

    public static string GetTextSizeLevelLabel(int value)
    {
        return NormalizeTextSizeLevel(value) switch
        {
            TextSizeLevelSmall => I18n.Tr("ui.settings.option.small", "小"),
            TextSizeLevelLarge => I18n.Tr("ui.settings.option.large", "大"),
            TextSizeLevelExtraLarge => I18n.Tr("ui.settings.option.extra_large", "特大"),
            _ => I18n.Tr("ui.settings.option.standard", "标准"),
        };
    }

    public static string GetBattleShakeLevelLabel(int value)
    {
        return NormalizeBattleShakeLevel(value) switch
        {
            BattleShakeLevelOff => I18n.Tr("ui.settings.option.off", "关闭"),
            BattleShakeLevelSmall => I18n.Tr("ui.settings.option.small", "小"),
            BattleShakeLevelLarge => I18n.Tr("ui.settings.option.large", "大"),
            _ => I18n.Tr("ui.settings.option.standard", "标准"),
        };
    }

    public static string GetVolumePercentLabel(int value)
    {
        int normalized = NormalizeVolumePercent(value);
        return normalized == 0
            ? I18n.Tr("ui.settings.option.muted", "静音")
            : $"{normalized}%";
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

    public static void ApplyWindowSettings(Window window = null)
    {
        EnsureLoaded();
        window ??= (Engine.GetMainLoop() as SceneTree)?.Root;
        if (window == null)
            return;

        if (!HasCustomWindowResolution)
        {
            window.Mode = Window.ModeEnum.Fullscreen;
            return;
        }

        window.Mode = Window.ModeEnum.Windowed;
        window.Size = new Vector2I(WindowWidth, WindowHeight);
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
        config.SetValue(SectionName, IncomingDamagePreviewKey, ShowIncomingDamagePreview);
        config.SetValue(SectionName, ShowIntentionTargetNamesKey, ShowIntentionTargetNames);
        config.SetValue(
            SectionName,
            ShowSingleTargetDamageIntentionArrowsKey,
            ShowSingleTargetDamageIntentionArrows
        );
        config.SetValue(SectionName, HideEnemySkillsKey, HideEnemySkills);
        config.SetValue(
            SectionName,
            GroupBattlePilesByCharacterKey,
            GroupBattlePilesByCharacter
        );
        config.SetValue(
            SectionName,
            KeepManualTargetCardVisibleWhenHiddenKey,
            KeepManualTargetCardVisibleWhenHidden
        );
        config.SetValue(
            SectionName,
            UseArrowManualTargetSelectionKey,
            UseArrowManualTargetSelection
        );
        config.SetValue(SectionName, TextSizeLevelKey, TextSizeLevel);
        config.SetValue(SectionName, BattleShakeLevelKey, BattleShakeLevel);
        config.SetValue(SectionName, LastSelectedDifficultyKey, LastSelectedDifficulty);
        config.SetValue(SectionName, MasterVolumePercentKey, MasterVolumePercent);
        config.SetValue(SectionName, SfxVolumePercentKey, SfxVolumePercent);
        config.SetValue(SectionName, LocaleKey, Locale);
        config.SetValue(SectionName, WindowWidthKey, WindowWidth);
        config.SetValue(SectionName, WindowHeightKey, WindowHeight);
        config.Save(SettingsPath);
    }
}
