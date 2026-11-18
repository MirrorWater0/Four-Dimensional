using System;
using System.Globalization;

public partial class Skill
{
    private string TuningKey => SkillId is SkillID id ? SkillTuning.ResolveKey(id)
        : TryResolveSkillId(GetType(), out var resolved) ? SkillTuning.ResolveKey(resolved)
        : throw new InvalidOperationException($"技能没有数值资源 ID：{GetType().Name}");

    protected int V(string key)
    {
        return SkillTuning.GetRequiredInt(TuningKey, key);
    }

    protected Func<Skill, int> TuningProviderFromExpression(string expression)
    {
        if (!TryNormalizeTuningExpression(expression, out string key, out int sign))
            return null;

        // Local variables and calculated values are not resource keys. Only bind actual parameters.
        if (!SkillTuning.TryGetInt(TuningKey, key, out _))
            return null;
        return skill => sign * skill.V(key);
    }

    public void RefreshTuning()
    {
        _cachedPlan = null;
        UpdateDescription();
    }

    private static bool TryNormalizeTuningExpression(
        string expression,
        out string key,
        out int sign
    )
    {
        key = null;
        sign = 1;

        if (string.IsNullOrWhiteSpace(expression))
            return false;

        string value = expression.Trim();
        if (value.StartsWith("-", StringComparison.Ordinal))
        {
            sign = -1;
            value = value[1..].Trim();
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return false;

        int memberSeparator = value.LastIndexOf('.');
        if (memberSeparator >= 0 && memberSeparator < value.Length - 1)
            value = value[(memberSeparator + 1)..];

        if (!IsSimpleIdentifier(value))
            return false;

        key = value;
        return true;
    }

    private static bool IsSimpleIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!char.IsLetter(value[0]) && value[0] != '_')
            return false;

        for (int i = 1; i < value.Length; i++)
        {
            char c = value[i];
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }
}
