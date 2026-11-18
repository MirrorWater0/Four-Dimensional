using System;
using System.Linq;
using Godot;

// A null skill ID denotes one draw from the registry's random intention pool.
public readonly record struct EnemyIntentionStep(SkillID? SkillId) {
    public bool IsRandom => !SkillId.HasValue;
    public static EnemyIntentionStep Random => new(null);
    public static implicit operator EnemyIntentionStep(SkillID id) => new(id);
}

public abstract partial class EnemyRegedit
{
    public string CharacterName;

    public string PortaitPath;

    public PackedScene CharacterScene;

    public int PositionIndex;
    // Random steps select exclusively from this pool.
    public SkillID[] SkillIDs = Array.Empty<SkillID>();
    public EnemyIntentionStep[] IntentionCycle = [EnemyIntentionStep.Random];
    // One-off intentions use 1-based enemy turns and pause the regular cycle.
    public (SkillID SkillId, int Turn)[] IntentionCycleInsertions =
        Array.Empty<(SkillID SkillId, int Turn)>();
    // Choose an initial array position once, then follow the configured cycle order.
    public bool RandomizeIntentionCycleStart = false;
    // Encounter formations enable only their designated Frail carrier.
    public bool CanApplyFrail = true;
    protected static EnemyIntentionStep random => EnemyIntentionStep.Random;

    public (SkillID SkillId, int Turn)[] GetIntentionCycleInsertions() =>
        (IntentionCycleInsertions ?? Array.Empty<(SkillID SkillId, int Turn)>())
            .Where(insertion => insertion.Turn > 0 && insertion.SkillId != SkillID.None)
            .OrderBy(insertion => insertion.Turn).ToArray();

    public SkillID[] GetAllSkillIds() => (SkillIDs ?? Array.Empty<SkillID>())
        .Concat((IntentionCycle ?? Array.Empty<EnemyIntentionStep>())
            .Where(step => !step.IsRandom).Select(step => step.SkillId.Value))
        .Concat(GetIntentionCycleInsertions().Select(insertion => insertion.SkillId))
        .Distinct().ToArray();
    public string TuningKey => GetType().Name[..^"Regedit".Length];
    private float[] _statMultipliers = Array.Empty<float>();
    private float[] _lifeMultipliers = Array.Empty<float>();
    private int? _maxLifeOverride;
    public int Power => ReadStat(nameof(Power), _statMultipliers);
    public int Survivability => ReadStat(nameof(Survivability), _statMultipliers);
    public int BasePowerContribution => ReadStat(nameof(BasePowerContribution), _statMultipliers);
    public int BaseSurvivabilityContribution => ReadStat(nameof(BaseSurvivabilityContribution), _statMultipliers);
    // An explicit instance override is used by existing diagnostic fixtures.
    // Normal enemy definitions always read their value from the Resource.
    public int MaxLife {
        get => _maxLifeOverride ?? ReadStat(nameof(MaxLife), _lifeMultipliers);
        set => _maxLifeOverride = value;
    }
    public int CurrentLife = -1;
    public bool HasAttackVulnerableIntention;

    public EnemyRegedit() { }

    public string PassiveName;
    public string PassiveDescription;

    public EnemyRegedit GetRegedit() => (EnemyRegedit)MemberwiseClone();

    public void ApplyStatMultiplier(float statMultiplier, float lifeMultiplier) {
        _statMultipliers = [.. _statMultipliers, statMultiplier];
        _lifeMultipliers = [.. _lifeMultipliers, lifeMultiplier];
    }

    private int ReadStat(string key, float[] multipliers) {
        int value = EnemyTuning.GetRequiredInt(TuningKey, key);
        foreach (float multiplier in multipliers)
            if (value > 0)
                value = Math.Max(1, Mathf.CeilToInt(value * multiplier));
        return value;
    }
}

public enum EnemiesEnum
{
    Evil,
    FearWorm,
    Armon,
    Arrogance,
    AlienBody,
    RedHusk,
    War,
    Ferociouess,
    Turbine,
    BlackHawk,
    Inexorability,
    GraveWraith,
    VoidAcolyte,
    VoidRotor,
    HollowBulwark,
    MarrowReaver,
    FearElite,
    Death,
    Havoc,
}
