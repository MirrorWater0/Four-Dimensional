using System;

public static class LocalizationHelper
{
    public static string GetEnemyKey(EnemyRegedit regedit)
    {
        if (regedit == null)
            return string.Empty;

        string typeName = regedit.GetType().Name;
        if (typeName.EndsWith("Regedit", StringComparison.Ordinal))
            typeName = typeName[..^"Regedit".Length];

        return I18n.ToSnakeCase(typeName);
    }

    public static string GetEnemyDisplayName(EnemyRegedit regedit)
    {
        if (regedit == null)
            return I18n.Tr("ui.common.enemy", "Enemy");

        string fallback = string.IsNullOrWhiteSpace(regedit.CharacterName)
            ? GetTypeDisplayName(regedit)
            : regedit.CharacterName;
        return I18n.Tr($"character.{GetEnemyKey(regedit)}.name", fallback);
    }

    public static string GetEnemyPassiveName(EnemyRegedit regedit)
    {
        if (regedit == null)
            return string.Empty;

        return I18n.Tr(
            $"character.{GetEnemyKey(regedit)}.passive.name",
            regedit.PassiveName
        );
    }

    public static string GetEnemyPassiveDescription(EnemyRegedit regedit)
    {
        if (regedit == null)
            return string.Empty;

        return I18n.Tr(
            $"character.{GetEnemyKey(regedit)}.passive.description",
            regedit.PassiveDescription
        );
    }

    private static string GetTypeDisplayName(EnemyRegedit regedit)
    {
        string typeName = regedit?.GetType().Name ?? string.Empty;
        if (typeName.EndsWith("Regedit", StringComparison.Ordinal))
            typeName = typeName[..^"Regedit".Length];

        return string.IsNullOrWhiteSpace(typeName)
            ? I18n.Tr("ui.common.enemy", "Enemy")
            : typeName;
    }
}
