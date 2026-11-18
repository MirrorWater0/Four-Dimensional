using System;
using System.Collections.Generic;
using System.Linq;

public readonly struct TalentNodeDefinition(string id, string name, string effect, Buff.BuffName buff) {
    public string Id { get; } = id;
    public string DisplayName => I18n.Tr($"talent.{Id}.name", name);
    public string Description => EffectDescription;
    public string EffectDescription => I18n.Tr($"talent.{Id}.effect", effect);
    public Buff.BuffName BuffName { get; } = buff;
}

public static class TalentTree {
    private static readonly Dictionary<string, TalentNodeDefinition[]> Pools = new(StringComparer.OrdinalIgnoreCase) {
        ["Kasiya"] = [
            new("Kasiya.SpecialGuard", "战术屏障", "每回合第一次打出攻击牌时，己方全阵获得2点格挡。", Buff.BuffName.TalentSpecialGuard),
            new("Kasiya.ExhaustPower", "熔炼战意", "每消耗4张卡牌，自身获得1点力量；计数跨回合保留。", Buff.BuffName.TalentExhaustPower),
            new("Kasiya.VulnerableShock", "破绽冲击", "敌人获得易伤时，受到5点伤害。", Buff.BuffName.TalentVulnerableShock),
            new("Kasiya.VulnerableAmplify", "弱点放大", "敌方易伤伤害加成额外提高20个百分点（50%提升至70%）。", Buff.BuffName.TalentVulnerableAmplify),
        ],
        ["Echo"] = [
            new("Echo.SecondEnergy", "延迟回声", "己方第二回合开始时，额外获得1点能量。", Buff.BuffName.TalentSecondEnergy),
            new("Echo.EnergyReserve", "余音储能", "战斗开始时获得1层能量储存。", Buff.BuffName.TalentEnergyReserve),
            new("Echo.WeakenGuard", "抑振护盾", "每给予敌人1层虚弱，己方全阵获得1点格挡。", Buff.BuffName.TalentWeakenGuard),
            new("Echo.TeamSurvivability", "共振防线", "战斗开始时，己方全阵获得1点生存。", Buff.BuffName.TalentTeamSurvivability),
        ],
        ["Mariya"] = [
            new("Mariya.HealingPlus", "温柔祈愿", "所有角色恢复生命时，额外恢复1点生命。", Buff.BuffName.TalentHealingPlus),
            new("Mariya.MapHealing", "旅途祝福", "战斗外恢复生命时，恢复量额外提高10%（向上取整）。", Buff.BuffName.TalentMapHealing),
            new("Mariya.OpeningHeal", "晨光祈祷", "战斗开始时，所有角色恢复1点生命。", Buff.BuffName.TalentOpeningHeal),
            new("Mariya.Divinity", "至高神格", "神格的攻击伤害倍率由2倍提升至2.5倍。", Buff.BuffName.TalentDivinity),
        ],
        ["Nightingale"] = [
            new("Nightingale.StealthPower", "暮色锋芒", "每次进入隐身时，自身获得1点力量。", Buff.BuffName.TalentStealthPower),
            new("Nightingale.ExhaustDamage", "碎刃风暴", "每消耗1张卡牌，随机对一名敌人造成2点伤害。", Buff.BuffName.TalentExhaustDamage),
            new("Nightingale.FirstPowerShare", "月相共鸣", "每场战斗第一次获得力量时，其他己方角色获得等额力量。", Buff.BuffName.TalentFirstPowerShare),
            new("Nightingale.TeamAfterimage", "夜幕残影", "战斗开始时，己方全阵获得1层残影。", Buff.BuffName.TalentTeamAfterimage),
        ],
    };
    public static IReadOnlyList<TalentNodeDefinition> GetNodes(string name) => Pools.TryGetValue(ResolveCharacterKey(name), out var nodes) ? nodes : [];
    public static IReadOnlyList<TalentNodeDefinition> GetNodes(PlayerInfoStructure info) => GetNodes(ResolveCharacterKey(info));
    public static IEnumerable<TalentNodeDefinition> AllTalents => Pools.Values.SelectMany(nodes => nodes);
    public static bool IsTalentBuff(Buff.BuffName name) => AllTalents.Any(node => node.BuffName == name);
    public static TalentNodeDefinition GetByBuff(Buff.BuffName name) => AllTalents.FirstOrDefault(node => node.BuffName == name);
    public static string ResolveCharacterKey(PlayerInfoStructure info) {
        string[] parts = (info.CharacterScenePath ?? "").Split('/');
        return ResolveCharacterKey(parts.Length >= 2 ? parts[^2] : info.CharacterName);
    }
    private static string ResolveCharacterKey(string name) => Pools.Keys.FirstOrDefault(key => string.Equals(key, name?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? name?.Trim() ?? "";
    public static bool HasUnlocked(PlayerInfoStructure info, string id) => info.UnlockedTalents?.Contains(id) == true;
    public static bool CanGainChoice(PlayerInfoStructure info) => GetNodes(info).Count >
        GetNodes(info).Count(node => HasUnlocked(info, node.Id)) + Math.Max(0, info.TalentPoints);
    public static void Normalize(ref PlayerInfoStructure info) {
        if (ResolveCharacterKey(info) == "Nightingale") {
            info.UnlockedTalents = info.UnlockedTalents?.Select(MigrateNightingaleTalentId).ToList();
            info.TalentOffer = info.TalentOffer?.Select(MigrateNightingaleTalentId).ToList();
        }
        var valid = GetNodes(info).Select(node => node.Id).ToHashSet();
        int legacyCount = info.UnlockedTalents?.Count(id => !valid.Contains(id)) ?? 0;
        info.LifeMax = Math.Max(1, info.LifeMax - Math.Max(0, info.AppliedTalentMaxLifeBonus));
        info.Life = Math.Min(info.Life, info.LifeMax);
        info.AppliedTalentMaxLifeBonus = 0;
        info.UnlockedTalents = (info.UnlockedTalents ?? []).Where(valid.Contains).Distinct().ToList();
        info.TalentPoints += legacyCount;
        info.TalentPoints = Math.Clamp(info.TalentPoints, 0, valid.Count - info.UnlockedTalents.Count);
        var unlocked = info.UnlockedTalents;
        info.TalentOffer = (info.TalentOffer ?? []).Where(id => valid.Contains(id) && !unlocked.Contains(id)).Distinct().Take(2).ToList();
        if (info.TalentPoints == 0) info.TalentOffer.Clear();
    }
    private static string MigrateNightingaleTalentId(string id) => id switch {
        "Nightingale.OpeningStealth" => "Nightingale.StealthPower",
        "Nightingale.OpeningPower" => "Nightingale.ExhaustDamage",
        "Nightingale.OpeningBlock" => "Nightingale.FirstPowerShare",
        "Nightingale.FirstDraw" => "Nightingale.TeamAfterimage",
        _ => id,
    };
    public static IReadOnlyList<TalentNodeDefinition> GetChoices(ref PlayerInfoStructure info) {
        Normalize(ref info);
        if (info.TalentPoints <= 0) return [];
        var unlocked = info.UnlockedTalents;
        var available = GetNodes(info).Where(node => !unlocked.Contains(node.Id)).ToList();
        if (info.TalentOffer.Count == 0) {
            int salt = ResolveCharacterKey(info).Aggregate(7919, (hash, c) => unchecked(hash * 31 + c));
            var rng = GameInfo.CreateRunRng(null, unchecked(salt + unlocked.Count * 104729));
            while (info.TalentOffer.Count < 2 && available.Count > 0) {
                int index = rng.Next(available.Count);
                info.TalentOffer.Add(available[index].Id);
                available.RemoveAt(index);
            }
        }
        var pool = GetNodes(info);
        return info.TalentOffer.Select(id => pool.First(node => node.Id == id)).ToArray();
    }
    public static bool CanUnlock(PlayerInfoStructure info, TalentNodeDefinition node, out string reason) {
        bool allowed = info.TalentPoints > 0 && info.TalentOffer?.Contains(node.Id) == true && !HasUnlocked(info, node.Id);
        reason = allowed ? "选择此天赋" : HasUnlocked(info, node.Id) ? "已获得" : "不在本次候选中";
        return allowed;
    }
    public static bool TryUnlock(ref PlayerInfoStructure info, string id, out string message) {
        GetChoices(ref info);
        var node = GetNodes(info).FirstOrDefault(node => node.Id == id);
        if (string.IsNullOrEmpty(node.Id) || !CanUnlock(info, node, out message)) { message = "无法选择此天赋"; return false; }
        info.UnlockedTalents.Add(id);
        info.TalentPoints--;
        info.TalentOffer.Clear();
        message = $"获得天赋：{node.DisplayName}";
        return true;
    }
    // TalentPoints now stores pending selections, preserving save/reward serialization.
    public static void AddTalentPoints(ref PlayerInfoStructure info, int amount) {
        Normalize(ref info);
        info.TalentPoints = Math.Clamp(info.TalentPoints + amount, 0, GetNodes(info).Count - info.UnlockedTalents.Count);
        GetChoices(ref info);
    }
    public static void ApplyMaxLifeBonus(ref PlayerInfoStructure info) => Normalize(ref info);
    public static int GetEffectivePower(PlayerInfoStructure info) => info.Power;
    public static int GetEffectiveSurvivability(PlayerInfoStructure info) => info.Survivability;
    public static bool HasPassiveUpgrade(PlayerInfoStructure info) => false;
    public static string GetPassiveDescription(PlayerInfoStructure info) => ResolveCharacterKey(info) switch {
        "Mariya" => Mariya.PassiveDescriptionText,
        "Nightingale" => Nightingale.PassiveDescriptionText,
        _ => info.PassiveDescription ?? "-",
    };
    public static string AppendPassiveUpgradeDescription(string key, string description, bool upgraded) => description;
}
