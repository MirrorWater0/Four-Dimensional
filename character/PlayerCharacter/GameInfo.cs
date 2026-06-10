using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;

public static partial class GameInfo
{
    public const int CoreEnergyDefaultMax = 100;

    public static PlayerInfoStructure[] PlayerCharacters;
    public static int Seed = 4203;
    public static int ElectricityCoin;
    public static int TransitionEnergy;
    public static int TransitionEnergyMax;
    public static int CurrentLevel;
    public static long SessionPlaySeconds;
    public static long RunStartedAtUtcTicks;
    public static bool RunFinished;
    public static bool PendingBossRelicChoice;
    public static List<RunHistoryRecord> RunHistoryRecords = new();
    public static bool HasSeenBattleTutorial;
    public static int IntentionRandomNum { get; private set; }
    public static int PositionRandomNum { get; private set; }
    public static Dictionary<Vector2I, LevelNode.LevelState> FirstLevelState = new();
    public static List<RelicStack> Relics = new();
    public static int ItemsMaxCount = 3;
    public static List<ItemID> Items = new();
    public static int BattleItemDropChance = BaseBattleItemDropChance;

    public static void InitNewGame()
    {
        ElectricityCoin = 100;
        TransitionEnergy = 0;
        TransitionEnergyMax = 0;
        CurrentLevel = 0;
        SessionPlaySeconds = 0;
        RunStartedAtUtcTicks = DateTime.UtcNow.Ticks;
        RunFinished = false;
        PendingBossRelicChoice = false;
        RunHistoryRecords ??= new List<RunHistoryRecord>();
        FirstLevelState.Clear();
        ResetLevelNodeCompletionRecords();
        ResetBattleRewardDropState();
        Items.Clear();
        GameInfo.Items.Add(ItemID.Explosion);
        Relics.Clear();
        SetRelicCount(RelicID.Blessing, 3);

    }

    public static int GetPartyLife()
    {
        NormalizePlayerCharacters();
        return PlayerCharacters?.Sum(info => Math.Clamp(info.Life, 0, info.LifeMax)) ?? 0;
    }

    public static int GetPartyMaxLife()
    {
        NormalizePlayerCharacters();
        return PlayerCharacters?.Sum(info => Math.Max(info.LifeMax, 0)) ?? 0;
    }

    public static void SetPartyLifeTotal(int totalLife)
    {
        NormalizePlayerCharacters();
        if (PlayerCharacters == null || PlayerCharacters.Length == 0)
            return;

        int currentLife = GetPartyLife();
        int clampedTarget = Math.Clamp(totalLife, 0, GetPartyMaxLife());
        AdjustPartyLife(clampedTarget - currentLife);
    }

    public static void AdjustPartyLife(int delta)
    {
        NormalizePlayerCharacters();
        if (delta == 0 || PlayerCharacters == null || PlayerCharacters.Length == 0)
            return;

        if (delta > 0)
            HealPartyLife(delta);
        else
            DamagePartyLife(-delta);
    }

    public static int HealPartyByMaxLifePercent(float percent)
    {
        NormalizePlayerCharacters();
        if (PlayerCharacters == null || PlayerCharacters.Length == 0 || percent <= 0f)
            return 0;

        int totalHealed = 0;
        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            int maxLife = Math.Max(info.LifeMax, 0);
            if (maxLife <= 0)
                continue;

            int recoverAmount = (int)MathF.Ceiling(maxLife * percent);
            if (recoverAmount <= 0)
                continue;

            int beforeLife = Math.Clamp(info.Life, 0, maxLife);
            int afterLife = Math.Clamp(beforeLife + recoverAmount, 0, maxLife);
            if (afterLife == beforeLife)
                continue;

            info.Life = afterLife;
            info.LifeInitialized = true;
            PlayerCharacters[i] = info;
            totalHealed += afterLife - beforeLife;
        }

        return totalHealed;
    }

    public static int RefillPartyLife()
    {
        NormalizePlayerCharacters();
        if (PlayerCharacters == null || PlayerCharacters.Length == 0)
            return 0;

        int totalHealed = 0;
        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            int maxLife = Math.Max(info.LifeMax, 1);
            int beforeLife = Math.Clamp(info.Life, 0, maxLife);
            if (beforeLife >= maxLife)
                continue;

            info.Life = maxLife;
            info.LifeInitialized = true;
            PlayerCharacters[i] = info;
            totalHealed += maxLife - beforeLife;
        }

        return totalHealed;
    }

    private static void HealPartyLife(int amount)
    {
        while (amount > 0)
        {
            int targetIndex = Array.FindIndex(
                PlayerCharacters,
                info => info.Life < info.LifeMax
            );
            if (targetIndex < 0)
                return;

            var info = PlayerCharacters[targetIndex];
            int applied = Math.Min(amount, info.LifeMax - info.Life);
            info.Life += applied;
            info.LifeInitialized = true;
            PlayerCharacters[targetIndex] = info;
            amount -= applied;
        }
    }

    private static void DamagePartyLife(int amount)
    {
        while (amount > 0)
        {
            int targetIndex = Array.FindIndex(PlayerCharacters, info => info.Life > 0);
            if (targetIndex < 0)
                return;

            var info = PlayerCharacters[targetIndex];
            int applied = Math.Min(amount, info.Life);
            info.Life -= applied;
            info.LifeInitialized = true;
            PlayerCharacters[targetIndex] = info;
            amount -= applied;
        }
    }

    public static bool HasRelic(RelicID relicID)
    {
        NormalizeRelics();
        return Relics.Any(stack => stack.ID == relicID);
    }

    public static int GetRelicCount(RelicID relicID, int defaultValue = 0)
    {
        NormalizeRelics();
        RelicStack stack = Relics.FirstOrDefault(stack => stack.ID == relicID);
        return stack.ID == relicID ? stack.Count : defaultValue;
    }

    public static void SetRelicCount(RelicID relicID, int count)
    {
        Relics ??= new List<RelicStack>();
        for (int i = 0; i < Relics.Count; i++)
        {
            if (Relics[i].ID != relicID)
                continue;

            Relics[i] = new RelicStack(relicID, count);
            return;
        }

        Relics.Add(new RelicStack(relicID, count));
    }

    public static int AddRelicCount(RelicID relicID, int amount)
    {
        int count = GetRelicCount(relicID, 0) + amount;
        SetRelicCount(relicID, count);
        return count;
    }

    public static void NormalizeRelics()
    {
        Relics ??= new List<RelicStack>();

        var seen = new HashSet<RelicID>();
        var normalized = new List<RelicStack>();
        foreach (RelicStack stack in Relics)
        {
            if (!seen.Add(stack.ID))
            {
                int existingIndex = normalized.FindIndex(existing => existing.ID == stack.ID);
                if (existingIndex >= 0)
                    normalized[existingIndex] = stack;
                continue;
            }

            normalized.Add(stack);
        }

        Relics = normalized;
    }

    public static Dictionary<RelicID, int> ToRelicDictionary()
    {
        NormalizeRelics();
        return Relics.ToDictionary(stack => stack.ID, stack => stack.Count);
    }

    public static void RefreshRandomNum(ref int num)
    {
        num = new Random(num).Next();
    }

}

public struct RelicStack
{
    public RelicStack() { }

    public RelicStack(RelicID id, int count)
    {
        ID = id;
        Count = count;
    }

    public RelicID ID;
    public int Count;
}

public struct PlayerInfoStructure
{
    public PlayerInfoStructure() { }

    public string CharacterScenePath;
    public int Life;
    public int LifeMax;
    public bool LifeInitialized;
    public int Power;
    public int Survivability;
    public int Speed;
    public int TalentPoints;
    public List<string> UnlockedTalents = new();
    public List<SkillID> GainedSkills = new();
    public SkillID[] TakenSkills = new SkillID[3];
    public SkillID[] AllSkills;
    public int PositionIndex;
    public string PortaitPath;
    public string CharacterName;
    public string PassiveName;
    public string PassiveDescription;
}

public static class GlobalFunction
{
    public static void TweenShader(Control node, string var, float val, float duration)
    {
        ShaderMaterial material = node?.Material as ShaderMaterial;
        if (material == null)
            return;

        node.CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                    SetShaderParameterIfValid(material, var, value)
                ),
                GetShaderParameterFloat(material, var),
                val,
                duration
            );
    }

    public static void TweenShader(Node2D node, string var, float val, float duration)
    {
        ShaderMaterial material = node?.Material as ShaderMaterial;
        if (material == null)
            return;

        node.CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                    SetShaderParameterIfValid(material, var, value)
                ),
                GetShaderParameterFloat(material, var),
                val,
                duration
            );
    }

    private static void SetShaderParameterIfValid(
        ShaderMaterial material,
        string parameterName,
        float value
    )
    {
        try
        {
            if (GodotObject.IsInstanceValid(material))
                material.SetShaderParameter(parameterName, value);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static float GetShaderParameterFloat(ShaderMaterial material, string parameterName)
    {
        Variant value = material.GetShaderParameter(parameterName);
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt64(),
            _ => 0f,
        };
    }

    private const string NumberColor = "#ffff00";

    public static string CompactDescriptionPunctuation(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder builder = new(input.Length);
        bool inTag = false;

        foreach (char ch in input)
        {
            if (ch == '[')
            {
                inTag = true;
                builder.Append(ch);
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                builder.Append(ch);
                continue;
            }

            builder.Append(inTag ? ch : ToCompactPunctuation(ch));
        }

        return builder.ToString();
    }

    private static char ToCompactPunctuation(char ch) =>
        ch switch
        {
            '，' => ',',
            '。' => '.',
            '：' => ':',
            '；' => ';',
            '！' => '!',
            '？' => '?',
            '（' => '(',
            '）' => ')',
            '【' => '(',
            '】' => ')',
            '、' => ',',
            '“' => '"',
            '”' => '"',
            '‘' => '\'',
            '’' => '\'',
            '《' => '<',
            '》' => '>',
            _ => ch,
        };

    public static string ColorizeNumbers(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        input = CompactDescriptionPunctuation(input);

        // 仅高亮 BBCode 标签外的数字，避免破坏像 [color=#87CEEB] 这样的标签参数。
        StringBuilder builder = new StringBuilder(input.Length * 2);
        bool inTag = false;

        for (int i = 0; i < input.Length; i++)
        {
            char ch = input[i];

            if (ch == '[')
            {
                inTag = true;
                builder.Append(ch);
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                builder.Append(ch);
                continue;
            }

            if (!inTag && char.IsDigit(ch))
            {
                int start = i;
                while (i < input.Length && char.IsDigit(input[i]))
                    i++;

                builder.Append($"[color={NumberColor}]");
                builder.Append(input, start, i - start);
                builder.Append("[/color]");
                i--;
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    public static string ColorizeKeywords(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        input = CompactDescriptionPunctuation(input);

        var keywords = BuildLocalizedKeywordEntries();
        if (keywords.Count == 0)
            return input;

        StringBuilder builder = new(input.Length * 2);
        bool inTag = false;

        for (int i = 0; i < input.Length; i++)
        {
            char ch = input[i];

            if (ch == '[')
            {
                inTag = true;
                builder.Append(ch);
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                builder.Append(ch);
                continue;
            }

            if (!inTag && TryMatchKeyword(input, i, keywords, out var match))
            {
                builder.Append($"[color={match.Color}]");
                builder.Append(input, i, match.Text.Length);
                builder.Append("[/color]");
                i += match.Text.Length - 1;
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    private readonly record struct KeywordColorEntry(
        string Text,
        string Color,
        bool UseWordBoundary = false,
        bool UseRebirthValidation = false
    );

    private static List<KeywordColorEntry> BuildLocalizedKeywordEntries()
    {
        const string cambridgeBlue = "#9cdacf";
        List<KeywordColorEntry> entries = new();

        AddKeywordVariants(entries, "keyword.damage", "伤害", "#ffc9c9");
        AddKeywordVariants(entries, "keyword.block", "格挡", cambridgeBlue);
        AddKeywordVariants(entries, "keyword.energy", "能量", "#c9cdff");
        AddKeywordVariants(entries, "keyword.exhaust", "消耗", "#ffb86b");
        AddKeywordVariants(entries, "keyword.voidness", "虚无", "#b9a6ff");
        AddKeywordVariants(
            entries,
            "keyword.rebirth",
            "复生",
            "#a8f0ad",
            useRebirthValidation: true
        );
        AddKeywordVariants(entries, "keyword.carry", "连携", "#a8f0ad");

        foreach (Buff.BuffName buffName in Enum.GetValues(typeof(Buff.BuffName)))
        {
            AddKeyword(entries, Buff.GetBuffDisplayName(buffName), "#a8f0ad");
            AddKeyword(entries, buffName.GetDescription(), "#a8f0ad");
        }

        return entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
            .Distinct()
            .OrderByDescending(entry => entry.Text.Length)
            .ToList();
    }

    private static void AddKeywordVariants(
        ICollection<KeywordColorEntry> entries,
        string key,
        string fallback,
        string color,
        bool useRebirthValidation = false
    )
    {
        AddKeyword(
            entries,
            I18n.Tr(key, fallback),
            color,
            useRebirthValidation: useRebirthValidation
        );
        AddKeyword(entries, fallback, color, useRebirthValidation: useRebirthValidation);
    }

    private static void AddKeyword(
        ICollection<KeywordColorEntry> entries,
        string text,
        string color,
        bool useRebirthValidation = false
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        entries.Add(
            new KeywordColorEntry(
                text,
                color,
                UseWordBoundary: ContainsLatinOrDigit(text),
                UseRebirthValidation: useRebirthValidation
            )
        );
    }

    private static bool TryMatchKeyword(
        string input,
        int index,
        IEnumerable<KeywordColorEntry> keywords,
        out KeywordColorEntry match
    )
    {
        foreach (var entry in keywords)
        {
            if (index + entry.Text.Length > input.Length)
                continue;

            if (
                !string.Equals(
                    input.Substring(index, entry.Text.Length),
                    entry.Text,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            if (
                entry.UseRebirthValidation
                && entry.Text == "复生"
                && !IsValidRebirthKeywordMatch(input, index)
            )
            {
                continue;
            }

            if (entry.UseWordBoundary && !HasWordBoundaryAround(input, index, entry.Text.Length))
                continue;

            match = entry;
            return true;
        }

        match = default;
        return false;
    }

    private static bool HasWordBoundaryAround(string input, int index, int length)
    {
        bool leftOk = index == 0 || !IsWordChar(input[index - 1]);
        int endIndex = index + length;
        bool rightOk = endIndex >= input.Length || !IsWordChar(input[endIndex]);
        return leftOk && rightOk;
    }

    private static bool ContainsLatinOrDigit(string text) => text.Any(IsWordChar);

    private static bool IsWordChar(char ch) =>
        (ch >= 'a' && ch <= 'z')
        || (ch >= 'A' && ch <= 'Z')
        || char.IsDigit(ch)
        || ch == '_';

    public static bool IsValidRebirthKeywordMatch(string input, int index)
    {
        if (string.IsNullOrEmpty(input) || index < 0 || index + 2 > input.Length)
            return false;

        if (!string.Equals(input.Substring(index, 2), "复生", StringComparison.Ordinal))
            return false;

        char? previous = FindPreviousPlainChar(input, index);
        char? next = FindNextPlainChar(input, index + 2);

        return previous is not ('回' or '恢') && next != '命';
    }

    private static char? FindPreviousPlainChar(string input, int startIndex)
    {
        bool inTag = false;
        for (int i = startIndex - 1; i >= 0; i--)
        {
            char ch = input[i];
            if (ch == ']')
            {
                inTag = true;
                continue;
            }

            if (ch == '[')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                return ch;
        }

        return null;
    }

    private static char? FindNextPlainChar(string input, int startIndex)
    {
        bool inTag = false;
        for (int i = startIndex; i < input.Length; i++)
        {
            char ch = input[i];
            if (ch == '[')
            {
                inTag = true;
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                return ch;
        }

        return null;
    }
}

public class ObservableList<T> : List<T>
{
    public event Action<T> ItemAdded; // 新增元素事件
    public event Action<T> ItemRemoved; // 移除元素事件

    public new void Add(T item)
    {
        base.Add(item);
        ItemAdded?.Invoke(item); // 触发新增回调
    }

    public new void Remove(T item)
    {
        base.Remove(item);
        ItemRemoved?.Invoke(item); // 触发移除回调
    }

    public new void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
            return;
        T item = this[index];
        base.RemoveAt(index);
        ItemRemoved?.Invoke(item);
    }

    // 可扩展其他方法（如 Insert、Clear 等）
}

public static class EnumExtensions
{
    private const string BuffColor = "#87eb91";

    public static string GetDescription(this Enum value)
    {
        if (value is Buff.BuffName.Void)
            return $"[color={BuffColor}]虚空[/color]";

        FieldInfo fi = value.GetType().GetField(value.ToString());
        DescriptionAttribute[] attributes =
            fi == null
                ? Array.Empty<DescriptionAttribute>()
                : (DescriptionAttribute[])
                    fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

        // 获取特性中写的字符串（作为翻译 Key）
        string fallback = attributes.Length > 0 ? attributes[0].Description : value.ToString();
        string key = I18n.GetEnumKey(value) ?? fallback;

        string translated = I18n.Tr(key, fallback);

        // Buff 名称默认蓝色高亮，便于在技能描述和提示中快速识别。
        if (value.GetType() == typeof(Buff.BuffName))
            return $"[color={BuffColor}]{translated}[/color]";

        return translated;
    }
}
