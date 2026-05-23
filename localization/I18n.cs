using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Godot;

public static class I18n
{
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    public static string Tr(string key, string fallback = null)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(key))
            return fallback ?? string.Empty;

        string translated = TranslationServer.Translate(key);
        if (translated == key && !string.IsNullOrWhiteSpace(fallback))
            return fallback;

        return translated;
    }

    public static bool HasTranslation(string key)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return TranslationServer.Translate(key) != key;
    }

    public static bool IsEnglishLocale()
    {
        EnsureInitialized();
        return TranslationServer.GetLocale().StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    public static string Format(
        string key,
        string fallback = null,
        params (string Name, object Value)[] args
    )
    {
        string text = Tr(key, fallback);
        if (args == null || args.Length == 0)
            return text;

        for (int i = 0; i < args.Length; i++)
        {
            string token = "{" + args[i].Name + "}";
            text = text.Replace(token, args[i].Value?.ToString() ?? string.Empty);
        }

        return text;
    }

    public static void SetLocale(string locale)
    {
        EnsureInitialized();
        if (!string.IsNullOrWhiteSpace(locale))
            TranslationServer.SetLocale(locale);
    }

    public static string GetEnumKey(Enum value)
    {
        return value switch
        {
            Skill.SkillTypes skillType => $"skill_type.{ToSnakeCase(skillType.ToString())}",
            PropertyType propertyType => $"property.{ToSnakeCase(propertyType.ToString())}",
            Buff.BuffName buffName => $"buff.{ToSnakeCase(buffName.ToString())}.name",
            _ => null,
        };
    }

    public static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsUpper(current) && i > 0)
                builder.Append('_');

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }

    public static string HumanizeSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string[] parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return value;

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
        }

        return string.Join(" ", parts);
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (SyncRoot)
        {
            if (_initialized)
                return;

            LoadLocaleFile("zh_CN");
            LoadLocaleFile("en");
            _initialized = true;
        }
    }

    private static void LoadLocaleFile(string locale)
    {
        string path = $"res://localization/{locale}.json";
        if (!FileAccess.FileExists(path))
            return;

        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        Dictionary<string, string> entries = JsonSerializer.Deserialize<Dictionary<string, string>>(
            json
        );
        if (entries == null || entries.Count == 0)
            return;

        var translation = new Translation { Locale = locale };
        foreach (var pair in entries)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                translation.AddMessage(pair.Key, pair.Value);
        }

        TranslationServer.AddTranslation(translation);
    }
}
