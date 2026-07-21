using System;
using System.Globalization;

public partial class Skill
{
    protected int V(string key, int defaultValue)
    {
        if (SkillId is SkillID skillId && SkillTuning.TryGetInt(skillId, key, out int idValue))
            return idValue;

        return SkillTuning.TryGetInt(GetType().Name, key, out int typeValue)
            ? typeValue
            : defaultValue;
    }

    protected static Func<Skill, int> VFromExpression(string expression, int defaultValue)
    {
        if (!TryNormalizeTuningExpression(expression, out string key, out int sign))
            return null;

        int unsignedDefault = sign < 0 ? Math.Abs(defaultValue) : defaultValue;
        return skill => skill != null ? sign * skill.V(key, unsignedDefault) : defaultValue;
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
