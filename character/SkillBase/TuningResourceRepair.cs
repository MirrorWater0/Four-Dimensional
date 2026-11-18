using System;
using System.Linq;
using Godot;

// Synchronizes parameter names without persisting unrelated, unsaved session edits.
public static class TuningResourceRepair {
    public static bool Synchronize(Godot.Collections.Dictionary<string, int> values,
        TuningParameterSchema.Parameters schema, Func<string, int> getInitialValue,
        Godot.Collections.Dictionary<string, int> baseline = null) {
        bool changed = false;
        if (schema != null) {
            if (schema.CanRemoveObsolete) {
                foreach (string key in values.Keys.Where(key => !schema.Keys.Contains(key)).ToArray()) {
                    values.Remove(key);
                    changed = true;
                }
            }
            foreach (string key in schema.Keys) {
                if (values.ContainsKey(key))
                    continue;
                values[key] = baseline != null && baseline.TryGetValue(key, out int value)
                    ? value : getInitialValue(key);
                changed = true;
            }
        }
        if (baseline != null) {
            foreach (var pair in baseline) {
                if (values.ContainsKey(pair.Key)
                    || (schema?.CanRemoveObsolete == true && !schema.Keys.Contains(pair.Key)))
                    continue;
                values[pair.Key] = pair.Value;
                changed = true;
            }
        }
        return changed;
    }

    public static void Persist(Resource definition, string path) {
        try {
            Error error = DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());
            if (error != Error.Ok && error != Error.AlreadyExists)
                throw new InvalidOperationException($"无法创建目录：{error}");
            error = ResourceSaver.Save(definition, path);
            if (error != Error.Ok)
                throw new InvalidOperationException(error.ToString());
            GD.Print($"[Tuning] 已保存同步后的字段：{path}");
        } catch (Exception e) {
            GD.PushWarning($"[Tuning] 字段已在当前会话同步，但写入 {path} 失败：{e.Message}。可在图鉴手动保存。");
        }
    }
}
