using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public enum PlayerCharacterKey
{
    Echo,
    Kasiya,
    Mariya,
    Nightingale,
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class PlayerSkillAttribute : Attribute
{
    public PlayerSkillAttribute(PlayerCharacterKey characterKey)
    {
        CharacterKey = characterKey;
    }

    public PlayerCharacterKey CharacterKey { get; }
}

public partial class Skill
{
    private static readonly Dictionary<string, PlayerCharacterKey> PlayerCharacterKeyMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Echo"] = PlayerCharacterKey.Echo,
            ["Kasiya"] = PlayerCharacterKey.Kasiya,
            ["Mariya"] = PlayerCharacterKey.Mariya,
            ["Nightingale"] = PlayerCharacterKey.Nightingale,
        };

    private static readonly Dictionary<PlayerCharacterKey, SkillID[]> PlayerSkillPools =
        BuildPlayerSkillPools();

    public static SkillID[] GetPlayerSkillPool(string characterName)
    {
        if (
            string.IsNullOrWhiteSpace(characterName)
            || !PlayerCharacterKeyMap.TryGetValue(characterName, out var characterKey)
        )
        {
            return Array.Empty<SkillID>();
        }

        return GetPlayerSkillPool(characterKey);
    }

    public static SkillID[] GetPlayerSkillPool(PlayerCharacterKey characterKey)
    {
        return PlayerSkillPools.TryGetValue(characterKey, out var pool)
            ? pool
            : Array.Empty<SkillID>();
    }

    private static Dictionary<PlayerCharacterKey, SkillID[]> BuildPlayerSkillPools()
    {
        var pools = new Dictionary<PlayerCharacterKey, List<SkillID>>();
        foreach (var skillId in Enum.GetValues<SkillID>())
        {
            var field = typeof(SkillID).GetField(skillId.ToString());
            var attribute = field?.GetCustomAttribute<PlayerSkillAttribute>();
            if (attribute == null)
            {
                continue;
            }

            if (!pools.TryGetValue(attribute.CharacterKey, out var list))
            {
                list = new List<SkillID>();
                pools[attribute.CharacterKey] = list;
            }

            list.Add(skillId);
        }

        return pools.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Distinct().ToArray()
        );
    }
}
