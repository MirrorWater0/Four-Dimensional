using System;
using System.Collections.Generic;
using Godot;

public readonly struct CardPreviewEntity : IEquatable<CardPreviewEntity>
{
    public CardPreviewEntity(ulong id)
    {
        Id = id;
    }

    public ulong Id { get; }
    public bool IsValid => Id != 0;

    public static CardPreviewEntity From(Character character)
    {
        return character != null && GodotObject.IsInstanceValid(character)
            ? new CardPreviewEntity(character.GetInstanceId())
            : default;
    }

    public bool Equals(CardPreviewEntity other) => Id == other.Id;
    public override bool Equals(object obj) => obj is CardPreviewEntity other && Equals(other);
    public override int GetHashCode() => Id.GetHashCode();
}

public readonly struct CardPreviewTargetEffectGroup
{
    public CardPreviewTargetEffectGroup(
        CardPreviewEntity targetEntity,
        Character target,
        Skill.PreviewEffectEntry[] entries
    )
    {
        TargetEntity = targetEntity;
        Target = target;
        Entries = entries ?? Array.Empty<Skill.PreviewEffectEntry>();
    }

    public CardPreviewEntity TargetEntity { get; }
    public Character Target { get; }
    public Skill.PreviewEffectEntry[] Entries { get; }
}

public sealed class CardEffectPreviewSnapshot
{
    public static readonly CardEffectPreviewSnapshot Empty =
        new(
            Array.Empty<Character>(),
            Array.Empty<Character>(),
            Array.Empty<Skill.PreviewDamageEntry>(),
            Array.Empty<Skill.PreviewEffectEntry>(),
            Array.Empty<CardPreviewTargetEffectGroup>()
        );

    public CardEffectPreviewSnapshot(
        Character[] hostileTargets,
        Character[] friendlyTargets,
        Skill.PreviewDamageEntry[] hostileDamageEntries,
        Skill.PreviewEffectEntry[] effectEntries,
        CardPreviewTargetEffectGroup[] effectGroupsByTarget
    )
    {
        HostileTargets = hostileTargets ?? Array.Empty<Character>();
        FriendlyTargets = friendlyTargets ?? Array.Empty<Character>();
        HostileDamageEntries =
            hostileDamageEntries ?? Array.Empty<Skill.PreviewDamageEntry>();
        EffectEntries = effectEntries ?? Array.Empty<Skill.PreviewEffectEntry>();
        EffectGroupsByTarget =
            effectGroupsByTarget ?? Array.Empty<CardPreviewTargetEffectGroup>();
    }

    public Character[] HostileTargets { get; }
    public Character[] FriendlyTargets { get; }
    public Skill.PreviewDamageEntry[] HostileDamageEntries { get; }
    public Skill.PreviewEffectEntry[] EffectEntries { get; }
    public CardPreviewTargetEffectGroup[] EffectGroupsByTarget { get; }
}

public static class CardEffectPreviewEcs
{
    private const int MaxCachedSnapshots = 256;
    private static readonly Dictionary<CardPreviewQuery, CardEffectPreviewSnapshot> Snapshots =
        new();
    private static readonly List<CardPreviewQuery> KeysToRemove = new();

    public static CardEffectPreviewSnapshot GetSnapshot(
        Skill skill,
        bool includeTargetVulnerable = true
    )
    {
        if (skill == null)
            return CardEffectPreviewSnapshot.Empty;

        CardPreviewQuery query = CardPreviewQuery.From(skill, includeTargetVulnerable);
        if (Snapshots.TryGetValue(query, out CardEffectPreviewSnapshot snapshot))
            return snapshot;

        if (Snapshots.Count >= MaxCachedSnapshots)
            Snapshots.Clear();

        snapshot = BuildSnapshot(skill, includeTargetVulnerable);
        Snapshots[query] = snapshot;
        return snapshot;
    }

    public static void InvalidateBattle(Battle battle)
    {
        if (battle == null || !GodotObject.IsInstanceValid(battle) || Snapshots.Count == 0)
            return;

        ulong battleId = battle.GetInstanceId();
        foreach (CardPreviewQuery query in Snapshots.Keys)
        {
            if (query.BattleEntityId == battleId)
                KeysToRemove.Add(query);
        }

        for (int i = 0; i < KeysToRemove.Count; i++)
            Snapshots.Remove(KeysToRemove[i]);

        KeysToRemove.Clear();
    }

    public static void InvalidateSkill(Skill skill)
    {
        if (skill == null || Snapshots.Count == 0)
            return;

        foreach (CardPreviewQuery query in Snapshots.Keys)
        {
            if (ReferenceEquals(query.Skill, skill))
                KeysToRemove.Add(query);
        }

        for (int i = 0; i < KeysToRemove.Count; i++)
            Snapshots.Remove(KeysToRemove[i]);

        KeysToRemove.Clear();
    }

    private static CardEffectPreviewSnapshot BuildSnapshot(
        Skill skill,
        bool includeTargetVulnerable
    )
    {
        Skill.PreviewEffectEntry[] effectEntries =
            skill.BuildPreviewEffectEntriesRaw(includeTargetVulnerable)
            ?? Array.Empty<Skill.PreviewEffectEntry>();

        return new CardEffectPreviewSnapshot(
            skill.BuildPreviewHostileTargetsRaw() ?? Array.Empty<Character>(),
            skill.BuildPreviewFriendlyTargetsRaw() ?? Array.Empty<Character>(),
            skill.BuildPreviewHostileDamageEntriesRaw(includeTargetVulnerable)
                ?? Array.Empty<Skill.PreviewDamageEntry>(),
            effectEntries,
            BuildEffectGroupsByTarget(effectEntries)
        );
    }

    private static CardPreviewTargetEffectGroup[] BuildEffectGroupsByTarget(
        Skill.PreviewEffectEntry[] entries
    )
    {
        if (entries == null || entries.Length == 0)
            return Array.Empty<CardPreviewTargetEffectGroup>();

        var targetOrder = new List<Character>(entries.Length);
        var entriesByTarget = new Dictionary<ulong, List<Skill.PreviewEffectEntry>>();
        var targetByEntityId = new Dictionary<ulong, Character>();

        for (int i = 0; i < entries.Length; i++)
        {
            Skill.PreviewEffectEntry entry = entries[i];
            Character target = entry.Target;
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            ulong entityId = target.GetInstanceId();
            if (!entriesByTarget.TryGetValue(entityId, out var targetEntries))
            {
                targetEntries = new List<Skill.PreviewEffectEntry>(4);
                entriesByTarget[entityId] = targetEntries;
                targetByEntityId[entityId] = target;
                targetOrder.Add(target);
            }

            targetEntries.Add(entry);
        }

        if (targetOrder.Count == 0)
            return Array.Empty<CardPreviewTargetEffectGroup>();

        var groups = new CardPreviewTargetEffectGroup[targetOrder.Count];
        for (int i = 0; i < targetOrder.Count; i++)
        {
            Character target = targetOrder[i];
            ulong entityId = target.GetInstanceId();
            groups[i] = new CardPreviewTargetEffectGroup(
                new CardPreviewEntity(entityId),
                targetByEntityId[entityId],
                entriesByTarget[entityId].ToArray()
            );
        }

        return groups;
    }

    private readonly struct CardPreviewQuery : IEquatable<CardPreviewQuery>
    {
        private CardPreviewQuery(
            Skill skill,
            int revision,
            ulong battleEntityId,
            ulong ownerEntityId,
            ulong manualTargetEntityId,
            bool includeTargetVulnerable
        )
        {
            Skill = skill;
            Revision = revision;
            BattleEntityId = battleEntityId;
            OwnerEntityId = ownerEntityId;
            ManualTargetEntityId = manualTargetEntityId;
            IncludeTargetVulnerable = includeTargetVulnerable;
        }

        public Skill Skill { get; }
        public int Revision { get; }
        public ulong BattleEntityId { get; }
        public ulong OwnerEntityId { get; }
        public ulong ManualTargetEntityId { get; }
        public bool IncludeTargetVulnerable { get; }

        public static CardPreviewQuery From(Skill skill, bool includeTargetVulnerable)
        {
            Character owner = skill.OwnerCharater;
            Battle battle =
                owner != null && GodotObject.IsInstanceValid(owner) ? owner.BattleNode : null;
            Character manualTarget = skill.GetManualFriendlyTarget();
            return new CardPreviewQuery(
                skill,
                skill.ComputePreviewCacheRevision(),
                battle != null && GodotObject.IsInstanceValid(battle)
                    ? battle.GetInstanceId()
                    : 0,
                owner != null && GodotObject.IsInstanceValid(owner)
                    ? owner.GetInstanceId()
                    : 0,
                manualTarget != null && GodotObject.IsInstanceValid(manualTarget)
                    ? manualTarget.GetInstanceId()
                    : 0,
                includeTargetVulnerable
            );
        }

        public bool Equals(CardPreviewQuery other)
        {
            return ReferenceEquals(Skill, other.Skill)
                && Revision == other.Revision
                && BattleEntityId == other.BattleEntityId
                && OwnerEntityId == other.OwnerEntityId
                && ManualTargetEntityId == other.ManualTargetEntityId
                && IncludeTargetVulnerable == other.IncludeTargetVulnerable;
        }

        public override bool Equals(object obj) =>
            obj is CardPreviewQuery other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Skill);
            hash.Add(Revision);
            hash.Add(BattleEntityId);
            hash.Add(OwnerEntityId);
            hash.Add(ManualTargetEntityId);
            hash.Add(IncludeTargetVulnerable);
            return hash.ToHashCode();
        }
    }
}
