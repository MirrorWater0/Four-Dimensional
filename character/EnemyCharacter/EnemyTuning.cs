using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class EnemyTuning {
    private const string CatalogPath = "res://data/enemy_tuning/catalog.tres";
    private const string UserDirectory = "user://enemy_tuning";
    private static Dictionary<string, EnemyTuningResource> _defaults = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, EnemyTuningResource> _active = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, string> _projectPaths = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static int Revision { get; private set; }
    public static string LastLoadedPath { get; private set; } = string.Empty;
    public static string LastError { get; private set; } = string.Empty;
    public static bool CanSaveProject => OS.HasFeature("editor");
    public static EnemyTuningResource GetEnemy(string key) {
        EnsureLoaded();
        if (!_active.TryGetValue(key, out var enemy))
            throw new InvalidOperationException($"缺少敌人 Resource：{key}。");
        return enemy.Copy();
    }

    public static bool TryGetInt(EnemyRegedit registry, string key, out int value) =>
        TryGetInt(registry?.TuningKey, key, out value);

    public static bool TryGetInt(string enemyKey, string key, out int value) {
        EnsureLoaded();
        value = 0;
        return enemyKey != null && _active.TryGetValue(enemyKey, out var enemy) && enemy.Values.TryGetValue(key, out value);
    }

    public static int GetRequiredInt(string enemyKey, string key) {
        if (string.IsNullOrWhiteSpace(enemyKey) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("敌人 ID 或参数名不能为空。");
        if (TryGetInt(enemyKey, key, out int value))
            return value;
        if (!_active.TryGetValue(enemyKey, out var enemy))
            throw new InvalidOperationException($"缺少敌人 Resource：{enemyKey}。请登记到 {CatalogPath}。");
        var baseline = _defaults[enemyKey];
        if (!baseline.Values.TryGetValue(key, out value)) {
            value = EnemyTuningParameters.GetInitialValue(key);
            baseline.Values[key] = value;
        }
        enemy.Values[key] = value;
        Revision++;
        GD.PushWarning($"[Tuning] 已自动补齐 {enemyKey}.{key} = {value}，请在图鉴检查数值。");
        PersistAddedParameter(enemyKey);
        return value;
    }

    public static bool Apply(EnemyTuningResource draft, out string message) {
        EnsureLoaded();
        try {
            Validate(draft, _defaults[draft.EnemyKey]);
            _active[draft.EnemyKey] = draft.Copy();
            Revision++;
            message = "已应用，尚未保存。";
            return true;
        } catch (Exception e) {
            message = $"应用失败：{e.Message}";
            return false;
        }
    }

    public static bool Save(EnemyTuningResource draft, bool toProject, out string message) {
        EnsureLoaded();
        try {
            Validate(draft, _defaults[draft.EnemyKey]);
            if (toProject && !CanSaveProject)
                throw new InvalidOperationException("请保存为本地设置，当前环境不能写回项目资源。");
            Error error = DirAccess.MakeDirRecursiveAbsolute(UserDirectory);
            if (error != Error.Ok && error != Error.AlreadyExists)
                throw new InvalidOperationException($"无法创建保存目录：{error}");
            string userPath = GetUserPath(draft.EnemyKey);
            string path = toProject ? _projectPaths[draft.EnemyKey] : userPath;
            error = ResourceSaver.Save(draft.Copy(), path);
            if (error != Error.Ok)
                throw new InvalidOperationException($"保存 {path} 失败：{error}");
            if (toProject) {
                // A local override must not hide the freshly saved project definition on restart.
                if (FileAccess.FileExists(userPath) && DirAccess.RemoveAbsolute(userPath) != Error.Ok)
                    throw new InvalidOperationException("项目已保存，但清除本地覆盖失败。");
                _defaults[draft.EnemyKey] = draft.Copy();
            }
            _active[draft.EnemyKey] = draft.Copy();
            Revision++;
            message = toProject ? "已保存到项目 Resource。" : "已保存为本地设置。";
            return true;
        } catch (Exception e) {
            message = $"保存失败：{e.Message}";
            return false;
        }
    }

    public static bool RestoreDefault(string key, out string message) {
        EnsureLoaded();
        string path = GetUserPath(key);
        if (FileAccess.FileExists(path) && DirAccess.RemoveAbsolute(path) != Error.Ok) {
            message = "恢复失败：无法删除本地覆盖。";
            return false;
        }
        _active[key] = _defaults[key].Copy();
        Revision++;
        message = "已恢复项目资源中的数值。";
        return true;
    }

    public static bool Reload(out string message) {
        try {
            var catalog = ResourceLoader.Load<EnemyTuningCatalog>(CatalogPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
            if (catalog == null)
                throw new InvalidOperationException($"无法加载 {CatalogPath}");
            var defaults = new Dictionary<string, EnemyTuningResource>(StringComparer.OrdinalIgnoreCase);
            var active = new Dictionary<string, EnemyTuningResource>(StringComparer.OrdinalIgnoreCase);
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var repairs = new List<(Resource Definition, string Path)>();
            foreach (var enemy in catalog.Enemies) {
                bool projectChanged = Validate(enemy);
                string userPath = GetUserPath(enemy.EnemyKey);
                var selected = enemy;
                bool localChanged = false;
                if (FileAccess.FileExists(userPath)) {
                    selected = ResourceLoader.Load<EnemyTuningResource>(userPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
                    localChanged = Validate(selected, enemy);
                }
                if (!defaults.TryAdd(enemy.EnemyKey, enemy.Copy()))
                    throw new InvalidOperationException($"重复的敌人资源：{enemy.EnemyKey}");
                paths.Add(enemy.EnemyKey, enemy.ResourcePath);
                active.Add(enemy.EnemyKey, selected.Copy());
                if (projectChanged && CanSaveProject)
                    repairs.Add((enemy.Copy(), enemy.ResourcePath));
                if (localChanged || (projectChanged && !CanSaveProject))
                    repairs.Add((selected.Copy(), userPath));
            }
            if (defaults.Count == 0)
                throw new InvalidOperationException("敌人资源目录为空。");
            // Keep the previous complete revision if any file fails to load.
            _defaults = defaults;
            _active = active;
            _projectPaths = paths;
            _loaded = true;
            LastError = string.Empty;
            LastLoadedPath = CatalogPath;
            Revision++;
            // Persist only after the entire catalog has passed validation.
            foreach (var repair in repairs)
                TuningResourceRepair.Persist(repair.Definition, repair.Path);
            message = $"敌人 Resource 已重载：{active.Count} 个敌人。";
            return true;
        } catch (Exception e) {
            LastError = e.Message;
            message = $"敌人 Resource 重载失败：{e.Message}";
            GD.PushError(message);
            return false;
        }
    }

    public static ulong GetTuningFilesFingerprint() {
        EnsureLoaded();
        ulong hash = 1469598103934665603UL;
        foreach (string path in _projectPaths.Values.Append(CatalogPath).Concat(_active.Keys.Select(GetUserPath)).OrderBy(path => path)) {
            foreach (char c in path) {
                hash ^= c;
                hash *= 1099511628211UL;
            }
            hash ^= FileAccess.FileExists(path) ? FileAccess.GetModifiedTime(path) : 0;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    private static string GetUserPath(string key) => $"{UserDirectory}/{key}.tres";
    private static void EnsureLoaded() {
        if (!_loaded && !Reload(out string message))
            throw new InvalidOperationException(message);
    }

    private static void PersistAddedParameter(string key) {
        var baseline = _defaults[key];
        if (CanSaveProject) {
            try {
                // Merge into the latest saved file so external editor changes are not overwritten.
                string projectPath = _projectPaths[key];
                var savedProject = ResourceLoader.Load<EnemyTuningResource>(projectPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
                Validate(savedProject, baseline);
                TuningResourceRepair.Persist(savedProject.Copy(), projectPath);
            } catch (Exception exception) {
                GD.PushWarning($"[Tuning] {key} 的字段已在当前会话补齐，但更新项目 Resource 失败：{exception.Message}。可在图鉴手动保存。");
            }
        }
        string userPath = GetUserPath(key);
        if (CanSaveProject && !FileAccess.FileExists(userPath))
            return;
        try {
            // Use saved definitions only: applying a draft does not authorize persisting its other edits.
            var saved = FileAccess.FileExists(userPath)
                ? ResourceLoader.Load<EnemyTuningResource>(userPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep)
                : baseline.Copy();
            Validate(saved, baseline);
            TuningResourceRepair.Persist(saved.Copy(), userPath);
        } catch (Exception exception) {
            GD.PushWarning($"[Tuning] {key} 的字段已在当前会话补齐，但更新本地 Resource 失败：{exception.Message}。可在图鉴手动保存。");
        }
    }

    private static bool Validate(EnemyTuningResource enemy, EnemyTuningResource baseline = null) {
        if (enemy == null || !EnemyCatalog.Contains(enemy.EnemyKey))
            throw new InvalidOperationException("敌人资源 ID 无效。");
        if (enemy.Values == null)
            throw new InvalidOperationException($"{enemy.EnemyKey} 缺少 Values。");
        if (baseline != null && enemy.EnemyKey != baseline.EnemyKey)
            throw new InvalidOperationException("敌人 ID 与项目定义不一致。");
        bool changed = TuningResourceRepair.Synchronize(enemy.Values, TuningParameterSchema.GetEnemy(enemy.EnemyKey),
            EnemyTuningParameters.GetInitialValue, baseline?.Values);
        foreach (string required in new[] { "MaxLife", "Power", "Survivability", "BasePowerContribution", "BaseSurvivabilityContribution" }) {
            if (enemy.Values.ContainsKey(required))
                continue;
            int value = EnemyTuningParameters.GetInitialValue(required);
            enemy.Values[required] = value;
            changed = true;
            GD.PushWarning($"[Tuning] 已自动补齐 {enemy.EnemyKey}.{required} = {value}，请在图鉴检查数值。");
        }
        foreach (var pair in enemy.Values) {
            if (string.IsNullOrWhiteSpace(pair.Key))
                throw new InvalidOperationException($"{enemy.EnemyKey} 的参数名不能为空。");
            EnemyTuningParameters.Validate(pair.Key, pair.Value);
        }
        return changed;
    }
}
