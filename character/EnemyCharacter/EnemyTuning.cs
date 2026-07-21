using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;

public static class EnemyTuning
{
    private const string ProjectTuningDirectory = "res://data/enemy_tuning";
    private const string ProjectLegacyTuningPath = "res://data/enemy_tuning.dev.json";
    private const string UserTuningDirectory = "user://enemy_tuning";
    private const string UserTuningPath = "user://enemy_tuning.dev.json";

    private static readonly Dictionary<string, Dictionary<string, int>> Values =
        new(StringComparer.OrdinalIgnoreCase);

    private static bool _loaded;

    public static int Revision { get; private set; }
    public static string LastLoadedPath { get; private set; } = string.Empty;
    public static string LastError { get; private set; } = string.Empty;

    public static ulong GetTuningFilesFingerprint()
    {
        ulong hash = 1469598103934665603UL;
        AddDirectoryFingerprint(ProjectTuningDirectory, ref hash);
        AddFileFingerprint(ProjectLegacyTuningPath, ref hash);
        AddDirectoryFingerprint(UserTuningDirectory, ref hash);
        AddFileFingerprint(UserTuningPath, ref hash);
        return hash;
    }

    public static bool TryGetInt(EnemyRegedit registry, string key, out int value)
    {
        value = 0;
        if (registry == null || string.IsNullOrWhiteSpace(key))
            return false;

        foreach (string enemyKey in EnumerateRegistryKeys(registry))
        {
            if (TryGetInt(enemyKey, key, out value))
                return true;
        }

        return false;
    }

    public static bool TryGetInt(string enemyKey, string key, out int value)
    {
        EnsureLoaded();
        value = 0;

        if (string.IsNullOrWhiteSpace(enemyKey) || string.IsNullOrWhiteSpace(key))
            return false;

        return Values.TryGetValue(enemyKey, out Dictionary<string, int> enemyValues)
            && enemyValues.TryGetValue(key, out value);
    }

    public static bool Reload(out string message)
    {
        Values.Clear();
        LastError = string.Empty;
        _loaded = true;

        var loadedPaths = new List<string>();
        try
        {
            LoadDirectoryIfExists(ProjectTuningDirectory, loadedPaths);
            LoadPathIfExists(ProjectLegacyTuningPath, loadedPaths);
            LoadDirectoryIfExists(UserTuningDirectory, loadedPaths);
            LoadPathIfExists(UserTuningPath, loadedPaths);

            Revision++;
            LastLoadedPath = loadedPaths.Count > 0 ? string.Join(", ", loadedPaths) : "(none)";
            message =
                loadedPaths.Count > 0
                    ? $"敌人数值表已重载：{LastLoadedPath}，{CountValueEntries()} 项。"
                    : $"未找到敌人数值表，使用代码默认值。期望目录：{ProjectTuningDirectory}";
            return true;
        }
        catch (Exception e)
        {
            LastError = e.Message;
            message = $"敌人数值表重载失败：{e.Message}";
            GD.PushError(message);
            return false;
        }
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
            return;

        Reload(out _);
    }

    private static IEnumerable<string> EnumerateRegistryKeys(EnemyRegedit registry)
    {
        string typeName = registry.GetType().Name;
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            yield return typeName;
            if (typeName.EndsWith("Regedit", StringComparison.Ordinal))
                yield return typeName[..^"Regedit".Length];
        }

        if (!string.IsNullOrWhiteSpace(registry.CharacterName))
            yield return registry.CharacterName;
    }

    private static void LoadPathIfExists(string path, List<string> loadedPaths)
    {
        if (!FileAccess.FileExists(path))
            return;

        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException($"无法打开 {path}");

        string json = file.GetAsText();
        if (string.IsNullOrWhiteSpace(json))
            return;

        using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        });

        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"{path} 根节点必须是 JSON 对象。");

        if (root.TryGetProperty("enemies", out JsonElement enemiesElement))
            root = enemiesElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"{path} 的 enemies 节点必须是 JSON 对象。");

        foreach (JsonProperty enemyProperty in root.EnumerateObject())
        {
            if (enemyProperty.Value.ValueKind != JsonValueKind.Object)
                continue;

            Dictionary<string, int> enemyValues = GetOrCreateEnemyValues(enemyProperty.Name);
            foreach (JsonProperty valueProperty in enemyProperty.Value.EnumerateObject())
            {
                if (TryReadInt(valueProperty.Value, out int value))
                    enemyValues[valueProperty.Name] = value;
            }
        }

        loadedPaths.Add(path);
    }

    private static void LoadDirectoryIfExists(string directoryPath, List<string> loadedPaths)
    {
        using DirAccess directory = DirAccess.Open(directoryPath);
        if (directory == null)
            return;

        foreach (string filePath in EnumerateJsonFiles(directoryPath))
            LoadPathIfExists(filePath, loadedPaths);
    }

    private static void AddDirectoryFingerprint(string directoryPath, ref ulong hash)
    {
        foreach (string filePath in EnumerateJsonFiles(directoryPath).OrderBy(path => path))
            AddFileFingerprint(filePath, ref hash);
    }

    private static void AddFileFingerprint(string path, ref ulong hash)
    {
        if (!FileAccess.FileExists(path))
            return;

        AddStringToHash(path, ref hash);
        AddUlongToHash(FileAccess.GetModifiedTime(path), ref hash);
    }

    private static void AddStringToHash(string value, ref ulong hash)
    {
        foreach (char c in value ?? string.Empty)
            AddUlongToHash(c, ref hash);
    }

    private static void AddUlongToHash(ulong value, ref ulong hash)
    {
        hash ^= value;
        hash *= 1099511628211UL;
    }

    private static IEnumerable<string> EnumerateJsonFiles(string directoryPath)
    {
        using DirAccess directory = DirAccess.Open(directoryPath);
        if (directory == null)
            yield break;

        directory.ListDirBegin();
        while (true)
        {
            string name = directory.GetNext();
            if (string.IsNullOrEmpty(name))
                break;

            if (name == "." || name == "..")
                continue;

            string path = $"{directoryPath}/{name}";
            if (directory.CurrentIsDir())
            {
                foreach (string nestedPath in EnumerateJsonFiles(path))
                    yield return nestedPath;
            }
            else if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
        }

        directory.ListDirEnd();
    }

    private static Dictionary<string, int> GetOrCreateEnemyValues(string enemyKey)
    {
        if (!Values.TryGetValue(enemyKey, out Dictionary<string, int> enemyValues))
        {
            enemyValues = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Values[enemyKey] = enemyValues;
        }

        return enemyValues;
    }

    private static bool TryReadInt(JsonElement element, out int value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                if (element.TryGetInt32(out value))
                    return true;

                if (element.TryGetDouble(out double number))
                {
                    value = Mathf.RoundToInt((float)number);
                    return true;
                }
                break;
            case JsonValueKind.String:
                return int.TryParse(
                    element.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value
                );
        }

        value = 0;
        return false;
    }

    private static int CountValueEntries() => Values.Values.Sum(enemyValues => enemyValues.Count);
}
