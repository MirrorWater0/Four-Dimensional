using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class SkillTuning {
    private const string CatalogPath = "res://data/skill_tuning/catalog.tres";
    private const string UserDirectory = "user://skill_tuning";
    private static Dictionary<string, CardTuningResource> _defaults = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, CardTuningResource> _active = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, string> _projectPaths = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static int Revision { get; private set; }
    public static string LastLoadedPath { get; private set; } = string.Empty;
    public static string LastError { get; private set; } = string.Empty;
    public static bool CanSaveProject => OS.HasFeature("editor");
    public static string ResolveKey(SkillID id) => Skill.ResolveTuningId(id).ToString();

    public static IReadOnlyList<CardTuningResource> GetCards() {
        EnsureLoaded();
        return _active.Values.OrderBy(card => card.DisplayName).ThenBy(card => card.SkillKey)
            .Select(card => card.Copy()).ToArray();
    }

    public static CardTuningResource GetCard(SkillID id) => GetCard(ResolveKey(id));
    public static CardTuningResource GetCard(string key) {
        EnsureLoaded();
        if (!_active.TryGetValue(key, out var card))
            throw new InvalidOperationException($"缺少卡牌 Resource：{key}。请登记到 {CatalogPath}。");
        return card.Copy();
    }

    public static bool TryGetInt(SkillID id, string key, out int value) => TryGetInt(ResolveKey(id), key, out value);
    public static bool TryGetInt(string skillKey, string key, out int value) {
        EnsureLoaded();
        value = 0;
        return _active.TryGetValue(skillKey, out var card) && card.Values.TryGetValue(key, out value);
    }

    public static int GetRequiredInt(string skillKey, string key) {
        if (string.IsNullOrWhiteSpace(skillKey) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("卡牌 ID 或参数名不能为空。");
        if (TryGetInt(skillKey, key, out int value))
            return value;
        if (!_active.TryGetValue(skillKey, out var card))
            throw new InvalidOperationException($"缺少卡牌 Resource：{skillKey}。请登记到 {CatalogPath}。");
        var baseline = _defaults[skillKey];
        if (!baseline.Values.TryGetValue(key, out value)) {
            value = CardTuningParameters.GetInitialValue(key);
            baseline.Values[key] = value;
        }
        card.Values[key] = value;
        Revision++;
        GD.PushWarning($"[Tuning] 已自动补齐 {skillKey}.{key} = {value}，请在图鉴检查数值。");
        PersistAddedParameter(skillKey);
        return value;
    }

    public static bool Apply(CardTuningResource draft, out string message) {
        EnsureLoaded();
        try {
            Validate(draft, _defaults[draft.SkillKey]);
            _active[draft.SkillKey] = draft.Copy();
            Revision++;
            message = "已应用，尚未保存。";
            return true;
        } catch (Exception e) {
            message = $"应用失败：{e.Message}";
            return false;
        }
    }

    public static bool Save(CardTuningResource draft, bool toProject, out string message) {
        EnsureLoaded();
        try {
            Validate(draft, _defaults[draft.SkillKey]);
            if (toProject && !CanSaveProject)
                throw new InvalidOperationException("请保存为本地设置，当前环境不能写回项目资源。");
            Error error = DirAccess.MakeDirRecursiveAbsolute(UserDirectory);
            if (error != Error.Ok && error != Error.AlreadyExists)
                throw new InvalidOperationException($"无法创建保存目录：{error}");
            string userPath = GetUserPath(draft.SkillKey);
            string path = toProject ? _projectPaths[draft.SkillKey] : userPath;
            error = ResourceSaver.Save(draft.Copy(), path);
            if (error != Error.Ok)
                throw new InvalidOperationException($"保存 {path} 失败：{error}");
            if (toProject) {
                // A local override must not hide the freshly saved project definition on restart.
                if (FileAccess.FileExists(userPath) && DirAccess.RemoveAbsolute(userPath) != Error.Ok)
                    throw new InvalidOperationException("项目已保存，但清除本地覆盖失败。");
                _defaults[draft.SkillKey] = draft.Copy();
            }
            _active[draft.SkillKey] = draft.Copy();
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
            var catalog = ResourceLoader.Load<CardTuningCatalog>(CatalogPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
            if (catalog == null)
                throw new InvalidOperationException($"无法加载 {CatalogPath}");
            var defaults = new Dictionary<string, CardTuningResource>(StringComparer.OrdinalIgnoreCase);
            var active = new Dictionary<string, CardTuningResource>(StringComparer.OrdinalIgnoreCase);
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var repairs = new List<(Resource Definition, string Path)>();
            foreach (var card in catalog.Cards) {
                bool projectChanged = Validate(card);
                string userPath = GetUserPath(card.SkillKey);
                var selected = card;
                bool localChanged = false;
                if (FileAccess.FileExists(userPath)) {
                    selected = ResourceLoader.Load<CardTuningResource>(userPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
                    localChanged = Validate(selected, card);
                }
                if (!defaults.TryAdd(card.SkillKey, card.Copy()))
                    throw new InvalidOperationException($"重复的卡牌资源：{card.SkillKey}");
                paths.Add(card.SkillKey, card.ResourcePath);
                active.Add(card.SkillKey, selected.Copy());
                if (projectChanged && CanSaveProject)
                    repairs.Add((card.Copy(), card.ResourcePath));
                if (localChanged || (projectChanged && !CanSaveProject))
                    repairs.Add((selected.Copy(), userPath));
            }
            if (defaults.Count == 0)
                throw new InvalidOperationException("卡牌资源目录为空。");
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
            message = $"卡牌 Resource 已重载：{active.Count} 张卡牌。";
            return true;
        } catch (Exception e) {
            LastError = e.Message;
            message = $"卡牌 Resource 重载失败：{e.Message}";
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
                var savedProject = ResourceLoader.Load<CardTuningResource>(projectPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
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
                ? ResourceLoader.Load<CardTuningResource>(userPath, cacheMode: ResourceLoader.CacheMode.IgnoreDeep)
                : baseline.Copy();
            Validate(saved, baseline);
            TuningResourceRepair.Persist(saved.Copy(), userPath);
        } catch (Exception exception) {
            GD.PushWarning($"[Tuning] {key} 的字段已在当前会话补齐，但更新本地 Resource 失败：{exception.Message}。可在图鉴手动保存。");
        }
    }

    private static bool Validate(CardTuningResource card, CardTuningResource baseline = null) {
        if (card == null || !Enum.TryParse<SkillID>(card.SkillKey, out var id) || id == SkillID.None || !Enum.IsDefined(id))
            throw new InvalidOperationException("卡牌资源 ID 无效。");
        if (card.Values == null)
            throw new InvalidOperationException($"{card.SkillKey} 缺少 Values。");
        if (baseline != null && card.SkillKey != baseline.SkillKey)
            throw new InvalidOperationException("卡牌 ID 与项目定义不一致。");
        bool changed = TuningResourceRepair.Synchronize(card.Values, TuningParameterSchema.GetCard(card.SkillKey),
            CardTuningParameters.GetInitialValue, baseline?.Values);
        // Remove the retired field after merging old local overrides and editor drafts.
        changed |= card.Values.Remove("EnemySpecialIntentionCooldown");
        foreach (string required in new[] { "EnergyCost", "EnemyEnergyCost", "ExhaustAfterExecutions" }) {
            if (card.Values.ContainsKey(required))
                continue;
            int value = CardTuningParameters.GetInitialValue(required);
            card.Values[required] = value;
            changed = true;
            GD.PushWarning($"[Tuning] 已自动补齐 {card.SkillKey}.{required} = {value}，请在图鉴检查数值。");
        }
        foreach (var pair in card.Values) {
            if (string.IsNullOrWhiteSpace(pair.Key))
                throw new InvalidOperationException($"{card.SkillKey} 的参数名不能为空。");
            CardTuningParameters.Validate(pair.Key, pair.Value);
        }
        return changed;
    }
}
