using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

// SSOT Step Catalog
// - Target Rule: 未显式指定目标策略时，默认按 Chosetarget1 顺序选取目标，描述中不额外说明。
// - AttackStep: 统一敌方攻击步骤，可覆盖单体、前N名、随机、全体与条件目标。
// - DoubleStrikeStep: 二段攻击（Attack2）。
// - ApplyBuffHostile: 对敌方施加Buff（buffName/stacks/maxTargets；maxTargets=9表示全阵）。
// - SummonStep: 召唤召唤物（slotSelector/packedScene；0最前空位，9最后空位，负数从自身前第N位起向前找空位，正数从自身后第N位起向后找空位）。
// - ApplyBuffSummonsStep: 对自身召唤物施加Buff（buffName/stacks/count；正数前N个，负数后N个，0全部）。
// - ModifySummonPropertyStep: 调整自身召唤物属性（type/value/count；正数前N个，负数后N个，0全部；value正增负减）。
// - BlockSummonsStep: 给予自身召唤物格挡（baseBlock/count/multiplier；count规则同ApplyBuffSummonsStep）。
// - HealSummonsStep: 治疗自身召唤物（baseHeal/count；count规则同ApplyBuffSummonsStep）。
// - LowerTargetPropertyStep: 下降目标属性（target 支持默认目标规则 / 已储存目标；maxTargets 仅默认目标规则生效；value 正数表示下降量）。
// - TargetReference: 固定友方目标选择（自己 / 上一位 / 下一位 / 全体 / 手动选择等）。
// - 已储存目标引用：默认使用内建攻击目标 / 治疗目标槽位。
// - ModifyPropertyStep: 调整友方属性（target 支持相对位 / 绝对位 / 已储存目标；value正增负减）。
// - ApplyBuffFriendly: 对友方施加Buff（target 支持相对位 / 绝对位 / 已储存目标）。
// - HealStep: 对友方治疗（target 支持相对位 / 绝对位 / 已储存目标；可储存目标）。
// - HurtFriendly: 对友方造成伤害（damage/index/all）。
// - EnergyStep: 改变自身或友方目标能量（target 支持相对位 / 绝对位 / 已储存目标）。
// - BlockStep: 相对位友方获得格挡（0自己、-1前一位、+1后一位...；可选对自己不生效）。
// - CarryStep: 连携指定友方目标释放指定技能（target 支持相对位 / 绝对位 / 已存储目标；index:0攻击/1生存/2特殊）。
// - SwapPositionFriendlyStep: 交换两个相对位队友的位置（0自己；交换PositionIndex并同步出手顺序）。
// - AddStatusCardsToDrawPileStep: 向已储存目标所属玩家的抽牌堆塞入状态牌（支持召唤物归属解析）。
// - EnergyTimesGateStep: 能量+次数联合门槛（满足则消耗能量并次数-1，并执行生效体；不再阻断后续step）。
// - EnergyTimesWhileStep: while循环（按EnergyTimesGate条件判定；条件满足时循环执行循环体step）。
// - ConditionStep: 条件执行（condition/steps/conditionDescription）。
// - BranchStep: 条件分支（condition/onPassSteps/onFailSteps/conditionDescription）。
// - TextStep: 仅描述文本（不执行效果）。
// - CustomStep: 自定义执行/描述兜底步骤。
public partial class Skill
{
    private const string BuiltinAttackTargetKey = "__attack_target";
    private const string BuiltinHealTargetKey = "__heal_target";

    public readonly struct PreviewDamageEntry
    {
        public PreviewDamageEntry(Character target, int damage, int hitCount)
        {
            Target = target;
            Damage = damage;
            HitCount = hitCount;
        }

        public Character Target { get; }
        public int Damage { get; }
        public int HitCount { get; }
    }

    protected sealed class PreviewDamageContext
    {
        private readonly Character _attacker;
        private readonly bool _includeTargetVulnerable;
        private readonly AttackBuff.PreviewState _attackBuffState = new();
        private readonly Dictionary<Character, int> _vulnerableStacks = new();

        public PreviewDamageContext(Character attacker, bool includeTargetVulnerable = true)
        {
            _attacker = attacker;
            _includeTargetVulnerable = includeTargetVulnerable;
        }

        public int PredictDamage(Character target, int baseDamage, bool applyAttackBuff = true)
        {
            int damage = baseDamage;
            if (applyAttackBuff)
            {
                damage = Math.Max(
                    AttackBuff.ApplyOutgoingDamageModifiers(
                        _attacker,
                        baseDamage,
                        target,
                        consumeStacks: true,
                        previewState: _attackBuffState
                    ),
                    0
                );
            }

            if (_includeTargetVulnerable && target != null)
            {
                if (!_vulnerableStacks.TryGetValue(target, out int vulnerableStacks))
                {
                    vulnerableStacks =
                        target
                            .HurtBuffs?.FirstOrDefault(x =>
                                x != null
                                && x.ThisBuffName == Buff.BuffName.Vulnerable
                                && x.Stack > 0
                            )
                            ?.Stack ?? 0;
                }

                if (vulnerableStacks > 0)
                {
                    damage = Math.Max((int)MathF.Floor(damage * 1.5f), 0);
                    vulnerableStacks--;
                }

                _vulnerableStacks[target] = vulnerableStacks;
            }

            return damage;
        }
    }

    private SkillPlan _cachedPlan;
    private bool _stopRemainingPlanExecution;
    private readonly Dictionary<string, Character> _storedTargets = new();
    private readonly Dictionary<string, Character[]> _storedTargetArrays = new();
    private Character _manualFriendlyTarget;

    public enum AbsoluteFriendlySelector
    {
        FrontMost,
        BackMost,
        LowestLife,
        All,
    }

    protected enum TargetReference
    {
        DefaultRule,
        Self,
        Previous,
        Next,
        FrontMost,
        BackMost,
        LowestLife,
        All,
        HealKey,
        ManualFriendly,
    }

    private enum TargetSelectionKind
    {
        DefaultRule,
        Relative,
        Absolute,
        Stored,
        StoredArray,
        ManualFriendly,
    }

    private readonly struct TargetSelection
    {
        public TargetSelectionKind Kind { get; }
        public AbsoluteFriendlySelector AbsoluteSelector { get; }
        public int RelativeIndex { get; }
        public string StoredKey { get; }
        public bool UsesStoredTarget =>
            Kind == TargetSelectionKind.Stored || Kind == TargetSelectionKind.StoredArray;

        private TargetSelection(
            TargetSelectionKind kind,
            AbsoluteFriendlySelector absoluteSelector,
            int relativeIndex,
            string storedKey
        )
        {
            Kind = kind;
            AbsoluteSelector = absoluteSelector;
            RelativeIndex = relativeIndex;
            StoredKey = storedKey;
        }

        public static TargetSelection FromRelative(int relativeIndex) =>
            new TargetSelection(TargetSelectionKind.Relative, default, relativeIndex, null);

        public static TargetSelection FromAbsolute(AbsoluteFriendlySelector selector) =>
            new TargetSelection(TargetSelectionKind.Absolute, selector, 0, null);

        public static TargetSelection FromStored(string storedKey) =>
            new TargetSelection(TargetSelectionKind.Stored, default, 0, storedKey);

        public static TargetSelection FromStoredArray(string storedKey) =>
            new TargetSelection(TargetSelectionKind.StoredArray, default, 0, storedKey);

        public static TargetSelection FromManualFriendly() =>
            new TargetSelection(TargetSelectionKind.ManualFriendly, default, 0, null);

        public static TargetSelection FromEnum(TargetReference target) =>
            target switch
            {
                TargetReference.Self => FromRelative(0),
                TargetReference.Previous => FromRelative(-1),
                TargetReference.Next => FromRelative(1),
                TargetReference.FrontMost => FromAbsolute(AbsoluteFriendlySelector.FrontMost),
                TargetReference.BackMost => FromAbsolute(AbsoluteFriendlySelector.BackMost),
                TargetReference.LowestLife => FromAbsolute(AbsoluteFriendlySelector.LowestLife),
                TargetReference.All => FromAbsolute(AbsoluteFriendlySelector.All),
                TargetReference.HealKey => FromStored(BuiltinHealTargetKey),
                TargetReference.ManualFriendly => FromManualFriendly(),
                _ => default,
            };

        public static implicit operator TargetSelection(TargetReference target) => FromEnum(target);
    }

    protected enum HostileTargetReference
    {
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        All,
        Random,
        AttackKey,
        EachRowFirst,
        EachRowLast,
        EachColFirst,
        EachColLast,
    }

    protected readonly struct HostileTargetSelection
    {
        public enum _Kind
        {
            Ordered,
            Random,
            Stored,
            EachRowFirst,
            EachRowLast,
            EachColFirst,
            EachColLast,
        }

        public _Kind Kind { get; }
        public int MaxTargets { get; }
        public bool ByBehindRow { get; }
        public string StoredKey { get; }

        private HostileTargetSelection(_Kind kind, int maxTargets, bool byBehindRow, string storedKey)
        {
            Kind = kind;
            MaxTargets = maxTargets;
            ByBehindRow = byBehindRow;
            StoredKey = storedKey;
        }

        public static HostileTargetSelection Ordered(
            int maxTargets = 0,
            bool byBehindRow = false
        ) =>
            new HostileTargetSelection(
                HostileTargetSelection._Kind.Ordered,
                maxTargets,
                byBehindRow,
                null
            );

        public static HostileTargetSelection Random(int maxTargets = 1, bool byBehindRow = false) =>
            new HostileTargetSelection(
                HostileTargetSelection._Kind.Random,
                maxTargets,
                byBehindRow,
                null
            );

        public static HostileTargetSelection Stored(string storedKey) =>
            new HostileTargetSelection(HostileTargetSelection._Kind.Stored, 0, false, storedKey);

        public static HostileTargetSelection EachRowFirst() =>
            new HostileTargetSelection(HostileTargetSelection._Kind.EachRowFirst, 0, false, null);

        public static HostileTargetSelection EachRowLast() =>
            new HostileTargetSelection(HostileTargetSelection._Kind.EachRowLast, 0, false, null);

        public static HostileTargetSelection EachColFirst() =>
            new HostileTargetSelection(HostileTargetSelection._Kind.EachColFirst, 0, false, null);

        public static HostileTargetSelection EachColLast() =>
            new HostileTargetSelection(HostileTargetSelection._Kind.EachColLast, 0, false, null);

        public static implicit operator HostileTargetSelection(HostileTargetReference target)
        {
            return target switch
            {
                HostileTargetReference.One => Ordered(1),
                HostileTargetReference.Two => Ordered(2),
                HostileTargetReference.Three => Ordered(3),
                HostileTargetReference.Four => Ordered(4),
                HostileTargetReference.Five => Ordered(5),
                HostileTargetReference.Six => Ordered(6),
                HostileTargetReference.Seven => Ordered(7),
                HostileTargetReference.Eight => Ordered(8),
                HostileTargetReference.Nine => Ordered(9),
                HostileTargetReference.All => Ordered(0),
                HostileTargetReference.Random => Random(1),
                HostileTargetReference.AttackKey => Stored(BuiltinAttackTargetKey),
                HostileTargetReference.EachRowFirst => EachRowFirst(),
                HostileTargetReference.EachRowLast => EachRowLast(),
                HostileTargetReference.EachColFirst => EachColFirst(),
                HostileTargetReference.EachColLast => EachColLast(),
                _ => default,
            };
        }
    }

    protected virtual SkillPlan BuildPlan()
    {
        return null;
    }

    protected SkillPlan GetPlan()
    {
        if (_cachedPlan == null)
            _cachedPlan = BuildPlan();

        return _cachedPlan;
    }

    public Character[] GetPreviewHostileTargets()
    {
        var plan = GetPlan();
        return plan?.GetPreviewHostileTargets() ?? Array.Empty<Character>();
    }

    public Character[] GetPreviewFriendlyTargets()
    {
        var plan = GetPlan();
        return plan?.GetPreviewFriendlyTargets() ?? Array.Empty<Character>();
    }

    public PreviewDamageEntry[] GetPreviewHostileDamageEntries(bool includeTargetVulnerable = true)
    {
        var plan = GetPlan();
        return plan?.GetPreviewHostileDamageEntries(includeTargetVulnerable)
            ?? Array.Empty<PreviewDamageEntry>();
    }

    public Character[] GetPreviewHostileDebuffTargets()
    {
        var plan = GetPlan();
        return plan?.GetPreviewHostileDebuffTargets() ?? Array.Empty<Character>();
    }

    public bool RequiresManualFriendlyTarget()
    {
        var plan = GetPlan();
        return plan?.RequiresManualFriendlyTarget() == true;
    }

    public bool ManualFriendlyTargetExcludesSelf()
    {
        var plan = GetPlan();
        return plan?.ManualFriendlyTargetExcludesSelf() == true;
    }

    public bool ManualFriendlyTargetAllowsDying()
    {
        var plan = GetPlan();
        return plan?.ManualFriendlyTargetAllowsDying() == true;
    }

    public void SetManualFriendlyTarget(Character target)
    {
        _manualFriendlyTarget = IsValidManualFriendlyTarget(
            target,
            dyingFilter: !ManualFriendlyTargetAllowsDying()
        )
            ? target
            : null;
    }

    public void ClearManualFriendlyTarget()
    {
        _manualFriendlyTarget = null;
    }

    public bool HasManualFriendlyTarget() =>
        IsValidManualFriendlyTarget(
            _manualFriendlyTarget,
            dyingFilter: !ManualFriendlyTargetAllowsDying()
        );

    private static TargetSelection TargetValue(TargetReference target) =>
        TargetSelection.FromEnum(target);

    private static HostileTargetSelection ResolveHostileTargetSelection(
        HostileTargetReference target,
        bool byBehindRow = false
    ) =>
        target switch
        {
            HostileTargetReference.One => HostileTargets(1, byBehindRow),
            HostileTargetReference.Two => HostileTargets(2, byBehindRow),
            HostileTargetReference.Three => HostileTargets(3, byBehindRow),
            HostileTargetReference.Four => HostileTargets(4, byBehindRow),
            HostileTargetReference.Five => HostileTargets(5, byBehindRow),
            HostileTargetReference.Six => HostileTargets(6, byBehindRow),
            HostileTargetReference.Seven => HostileTargets(7, byBehindRow),
            HostileTargetReference.Eight => HostileTargets(8, byBehindRow),
            HostileTargetReference.Nine => HostileTargets(9, byBehindRow),
            HostileTargetReference.All => HostileTargets(0, byBehindRow),
            HostileTargetReference.Random => HostileRandomTargets(1, byBehindRow),
            HostileTargetReference.AttackKey => HostileTargetSelection.Stored(BuiltinAttackTargetKey),
            HostileTargetReference.EachRowFirst => HostileTargetsEachRowFirst(),
            HostileTargetReference.EachRowLast => HostileTargetsEachRowLast(),
            HostileTargetReference.EachColFirst => HostileTargetsEachColFirst(),
            HostileTargetReference.EachColLast => HostileTargetsEachColLast(),
            _ => HostileTargets(1, byBehindRow),
        };

    protected static HostileTargetSelection HostileTargets(
        int maxTargets = 0,
        bool byBehindRow = false
    ) => HostileTargetSelection.Ordered(maxTargets, byBehindRow);

    protected static HostileTargetSelection HostileRandomTargets(
        int maxTargets = 1,
        bool byBehindRow = false
    ) => HostileTargetSelection.Random(maxTargets, byBehindRow);

    protected static HostileTargetSelection HostileTargetsEachRowFirst() =>
        HostileTargetSelection.EachRowFirst();

    protected static HostileTargetSelection HostileTargetsEachRowLast() =>
        HostileTargetSelection.EachRowLast();

    protected static HostileTargetSelection HostileTargetsEachColFirst() =>
        HostileTargetSelection.EachColFirst();

    protected static HostileTargetSelection HostileTargetsEachColLast() =>
        HostileTargetSelection.EachColLast();

    private void StoreTarget(string storedKey, Character target)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return;

        if (target == null)
            _storedTargets.Remove(storedKey);
        else
            _storedTargets[storedKey] = target;
    }

    private void StoreTarget(string storedKey, Character[] targets)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return;

        if (targets == null || targets.Length == 0)
            _storedTargetArrays.Remove(storedKey);
        else
            _storedTargetArrays[storedKey] = targets.Where(x => x != null).ToArray();
    }

    private void StoreAutoAttackTarget(Character target)
    {
        StoreTarget(BuiltinAttackTargetKey, target);
        StoreTarget(BuiltinAttackTargetKey, SingleTargetArray(target));
    }

    private void StoreAutoAttackTargets(Character[] targets)
    {
        Character[] filteredTargets = targets?.Where(x => x != null).ToArray() ?? Array.Empty<Character>();
        StoreTarget(BuiltinAttackTargetKey, filteredTargets.FirstOrDefault());
        StoreTarget(BuiltinAttackTargetKey, filteredTargets);
    }

    private void StoreAutoHealTargets(Character[] targets)
    {
        Character[] filteredTargets = targets?.Where(x => x != null).ToArray() ?? Array.Empty<Character>();
        StoreTarget(BuiltinHealTargetKey, filteredTargets.FirstOrDefault());
        StoreTarget(BuiltinHealTargetKey, filteredTargets);
    }

    private Character GetStoredTarget(string storedKey)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return null;

        _storedTargets.TryGetValue(storedKey, out Character cached);
        return cached;
    }

    private Character[] GetStoredTargetArray(string storedKey)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return Array.Empty<Character>();

        _storedTargetArrays.TryGetValue(storedKey, out Character[] cached);
        return cached ?? Array.Empty<Character>();
    }

    protected Character GetAttackTarget() => GetStoredTarget(BuiltinAttackTargetKey);

    protected Character[] GetAttackTargets() => GetStoredTargetArray(BuiltinAttackTargetKey);

    protected Character GetHealTarget() => GetStoredTarget(BuiltinHealTargetKey);

    protected Character[] GetHealTargets() => GetStoredTargetArray(BuiltinHealTargetKey);

    private Character[] ResolveHostileTargets(
        TargetSelection target,
        int maxTargets,
        bool byBehindRow
    )
    {
        if (target.Kind == TargetSelectionKind.StoredArray)
        {
            Character[] stored = GetStoredTargetArray(target.StoredKey);
            Character dummy = OwnerCharater?.BattleNode?.dummy;
            return stored
                .Where(x => x != null && x != dummy && x.State != Character.CharacterState.Dying)
                .ToArray();
        }

        if (target.UsesStoredTarget)
        {
            Character stored = GetStoredTarget(target.StoredKey);
            if (
                stored == null
                || stored == OwnerCharater?.BattleNode?.dummy
                || stored.State == Character.CharacterState.Dying
            )
            {
                return Array.Empty<Character>();
            }

            return [stored];
        }

        if (target.Kind != TargetSelectionKind.DefaultRule)
            return Array.Empty<Character>();

        var targets = ChosetargetByOrder(byBehindRow: byBehindRow);
        int count = maxTargets <= 0 ? targets.Length : Math.Min(maxTargets, targets.Length);
        if (count <= 0)
            return Array.Empty<Character>();

        return targets.Take(count).Where(x => x != null).ToArray();
    }

    private Character ResolveHostileTarget(TargetSelection target, bool byBehindRow) =>
        ResolveHostileTargets(target, 1, byBehindRow).FirstOrDefault();

    private Character[] ResolveHostileTargets(
        HostileTargetSelection target,
        Func<Character, bool> targetCondition = null
    )
    {
        HostileTargetSelection value = target;
        if (value.Kind == HostileTargetSelection._Kind.Stored)
        {
            Character[] storedTargets = GetStoredTargetArray(value.StoredKey);
            if (storedTargets.Length > 0)
                return storedTargets.Where(x => x != null && x.State != Character.CharacterState.Dying).ToArray();

            Character storedTarget = GetStoredTarget(value.StoredKey);
            if (storedTarget == null || storedTarget.State == Character.CharacterState.Dying)
                return Array.Empty<Character>();

            return [storedTarget];
        }

        Character[] ordered =
            value.Kind == HostileTargetSelection._Kind.Ordered
            || value.Kind == HostileTargetSelection._Kind.Random
                ? ChosetargetByOrder(byBehindRow: value.ByBehindRow)
                : GetAllHostileWithOrder(this, dyingFilter: true);

        if (ordered.Length == 0)
            return Array.Empty<Character>();

        Character[] matched =
            targetCondition == null ? ordered : ordered.Where(targetCondition).ToArray();

        return SelectHostileTargets(
            matched,
            target,
            OwnerCharater?.BattleNode?.BattleIntentionRandom
        );
    }

    private Character[] ResolveFriendlyTargets(
        TargetSelection target,
        bool dyingFilter,
        bool preferNonFull = false,
        bool rebirth = false,
        bool includeSummonsWhenAll = false
    )
    {
        return target.Kind switch
        {
            TargetSelectionKind.StoredArray => GetStoredTargetArray(target.StoredKey),
            TargetSelectionKind.Stored => SingleTargetArray(GetStoredTarget(target.StoredKey)),
            TargetSelectionKind.Relative => SingleTargetArray(
                GetAllyByRelative(target.RelativeIndex, dyingFilter: dyingFilter)
            ),
            TargetSelectionKind.Absolute => SelectFriendlyTargets(
                this,
                target.AbsoluteSelector,
                dyingFilter,
                preferNonFull,
                rebirth,
                includeSummonsWhenAll
            ),
            TargetSelectionKind.ManualFriendly => SingleTargetArray(
                IsValidManualFriendlyTarget(_manualFriendlyTarget, dyingFilter)
                    ? _manualFriendlyTarget
                    : null
            ),
            _ => SingleTargetArray(GetAllyByRelative(0, dyingFilter: dyingFilter)),
        };
    }

    private bool IsValidManualFriendlyTarget(Character target, bool dyingFilter)
    {
        if (
            target == null
            || OwnerCharater?.BattleNode == null
            || target.IsPlayer != OwnerCharater.IsPlayer
            || !GodotObject.IsInstanceValid(target)
        )
        {
            return false;
        }

        if (dyingFilter && target.State == Character.CharacterState.Dying)
            return false;

        return OwnerCharater
            .BattleNode.GetTeamCharacters(OwnerCharater.IsPlayer, includeSummons: true)
            .Any(character => character == target);
    }

    private Character ResolveFriendlyTarget(
        TargetSelection target,
        bool dyingFilter,
        bool preferNonFull = false,
        bool rebirth = false
    )
    {
        Character[] targets = ResolveFriendlyTargets(target, dyingFilter, preferNonFull, rebirth);
        return targets.Length > 0 ? targets[0] : null;
    }

    private static IEnumerable<Character> PreviewHostileTargets(
        Skill skill,
        int maxTargets,
        bool byBehindRow
    )
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return Array.Empty<Character>();

        Character[] targets = skill.ChosetargetByOrder(byBehindRow: byBehindRow);
        int count = maxTargets <= 0 ? targets.Length : Math.Min(maxTargets, targets.Length);
        if (count <= 0)
            return Array.Empty<Character>();

        return targets.Take(count);
    }

    private static IEnumerable<Character> PreviewHostileTargets(
        Skill skill,
        HostileTargetSelection target,
        Func<Character, bool> targetCondition = null
    )
    {
        if (skill == null)
            return Array.Empty<Character>();

        HostileTargetSelection value = target;
        if (value.Kind == HostileTargetSelection._Kind.Stored)
        {
            Character[] storedTargets = skill.GetStoredTargetArray(value.StoredKey);
            if (storedTargets.Length > 0)
                return storedTargets.Where(x => x != null && x.State != Character.CharacterState.Dying).ToArray();

            Character storedTarget = skill.GetStoredTarget(value.StoredKey);
            return storedTarget != null && storedTarget.State != Character.CharacterState.Dying
                ? [storedTarget]
                : Array.Empty<Character>();
        }

        Character[] ordered =
            value.Kind == HostileTargetSelection._Kind.Ordered
            || value.Kind == HostileTargetSelection._Kind.Random
                ? skill.ChosetargetByOrder(byBehindRow: value.ByBehindRow)
                : GetAllHostileWithOrder(skill, dyingFilter: true);

        if (ordered.Length == 0)
            return Array.Empty<Character>();

        Character[] matched =
            targetCondition == null ? ordered : ordered.Where(targetCondition).ToArray();

        return SelectHostileTargets(matched, target, previewRandomAsAll: true);
    }

    private static Character[] SelectHostileTargets(
        Character[] ordered,
        HostileTargetSelection target,
        Random random = null,
        bool previewRandomAsAll = false
    )
    {
        if (ordered == null || ordered.Length == 0)
            return Array.Empty<Character>();

        HostileTargetSelection value = target;
        return value.Kind switch
        {
            HostileTargetSelection._Kind.Stored => Array.Empty<Character>(),
            HostileTargetSelection._Kind.Ordered => SelectOrderedHostileTargets(
                ordered,
                value.MaxTargets
            ),
            HostileTargetSelection._Kind.Random => previewRandomAsAll
                ? ordered.Where(x => x != null).ToArray()
                : SelectRandomHostileTargets(ordered, value.MaxTargets, random),
            HostileTargetSelection._Kind.EachRowFirst => SelectGroupedHostileTargets(
                ordered,
                groupKeySelector: x => x.PositionIndex > 0 ? (x.PositionIndex - 1) % 3 : 0,
                pickLast: false
            ),
            HostileTargetSelection._Kind.EachRowLast => SelectGroupedHostileTargets(
                ordered,
                groupKeySelector: x => x.PositionIndex > 0 ? (x.PositionIndex - 1) % 3 : 0,
                pickLast: true
            ),
            HostileTargetSelection._Kind.EachColFirst => SelectGroupedHostileTargets(
                ordered,
                groupKeySelector: x => x.PositionIndex > 0 ? (x.PositionIndex - 1) / 3 : 0,
                pickLast: false
            ),
            HostileTargetSelection._Kind.EachColLast => SelectGroupedHostileTargets(
                ordered,
                groupKeySelector: x => x.PositionIndex > 0 ? (x.PositionIndex - 1) / 3 : 0,
                pickLast: true
            ),
            _ => Array.Empty<Character>(),
        };
    }

    private static Character[] SelectOrderedHostileTargets(Character[] ordered, int maxTargets)
    {
        if (ordered == null || ordered.Length == 0)
            return Array.Empty<Character>();

        int count = maxTargets <= 0 ? ordered.Length : Math.Min(maxTargets, ordered.Length);
        if (count <= 0)
            return Array.Empty<Character>();

        return ordered.Take(count).Where(x => x != null).ToArray();
    }

    private static Character[] SelectRandomHostileTargets(
        Character[] ordered,
        int maxTargets,
        Random random
    )
    {
        List<Character> candidates = ordered.Where(x => x != null).ToList();
        if (candidates.Count == 0)
            return Array.Empty<Character>();

        int count = maxTargets <= 0 ? candidates.Count : Math.Min(maxTargets, candidates.Count);
        if (count <= 0)
            return Array.Empty<Character>();

        random ??= new Random();
        for (int i = 0; i < count; i++)
        {
            int swapIndex = random.Next(i, candidates.Count);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        return candidates.Take(count).ToArray();
    }

    private static Character[] SelectGroupedHostileTargets(
        Character[] ordered,
        Func<Character, int> groupKeySelector,
        bool pickLast
    )
    {
        if (ordered == null || ordered.Length == 0)
            return Array.Empty<Character>();

        return ordered
            .Where(x => x != null)
            .GroupBy(groupKeySelector)
            .OrderBy(group => group.Key)
            .Select(group =>
                pickLast
                    ? group.OrderByDescending(x => x.PositionIndex).FirstOrDefault()
                    : group.OrderBy(x => x.PositionIndex).FirstOrDefault()
            )
            .Where(x => x != null)
            .ToArray();
    }

    private static IEnumerable<Character> CollectPreviewTargets(
        Skill skill,
        IEnumerable<SkillStep> steps
    )
    {
        if (steps == null)
            return Array.Empty<Character>();

        return steps.SelectMany(step => step?.PreviewTargets(skill) ?? Array.Empty<Character>());
    }

    private static IEnumerable<PreviewDamageEntry> CollectPreviewDamage(
        Skill skill,
        IEnumerable<SkillStep> steps,
        PreviewDamageContext context
    )
    {
        if (steps == null)
            return Array.Empty<PreviewDamageEntry>();

        return steps.SelectMany(step =>
            step?.PreviewDamage(skill, context) ?? Array.Empty<PreviewDamageEntry>()
        );
    }

    private static IEnumerable<Character> CollectPreviewHostileDebuffTargets(
        Skill skill,
        IEnumerable<SkillStep> steps
    )
    {
        if (steps == null)
            return Array.Empty<Character>();

        return steps.SelectMany(step =>
            step?.PreviewHostileDebuffTargets(skill) ?? Array.Empty<Character>()
        );
    }

    protected sealed class SkillPlan
    {
        private readonly Skill _skill;
        private readonly SkillStep[] _steps;

        public SkillPlan(Skill skill, params SkillStep[] steps)
        {
            _skill = skill;
            _steps = steps ?? Array.Empty<SkillStep>();
        }

        public async Task Execute()
        {
            _skill._stopRemainingPlanExecution = false;
            ClearStoredTargets();
            for (int i = 0; i < _steps.Length; i++)
            {
                if (ShouldAbortStepExecution(_skill))
                    break;
                await _steps[i].Execute(_skill);
                if (ShouldAbortStepExecution(_skill))
                    break;
            }
            _skill._stopRemainingPlanExecution = false;
            ClearStoredTargets();
        }

        public string[] DescribeLines()
        {
            var lines = new List<string>();
            for (int i = 0; i < _steps.Length; i++)
            {
                lines.AddRange(
                    _steps[i].Describe(_skill).Where(line => !string.IsNullOrWhiteSpace(line))
                );
            }
            return lines.ToArray();
        }

        public Character[] GetPreviewHostileTargets()
        {
            Dictionary<string, Character> storedTargetSnapshot = new(_skill._storedTargets);
            Dictionary<string, Character[]> storedTargetArraySnapshot = new(
                _skill._storedTargetArrays
            );
            try
            {
                ClearStoredTargets();

                Character dummy = _skill.OwnerCharater?.BattleNode?.dummy;
                return CollectPreviewTargets(_skill, _steps)
                    .Where(target =>
                        target != null
                        && target != dummy
                        && target.IsPlayer != _skill.OwnerCharater?.IsPlayer
                        && target.State == Character.CharacterState.Normal
                    )
                    .Distinct()
                    .ToArray();
            }
            finally
            {
                RestoreStoredTargets(storedTargetSnapshot, storedTargetArraySnapshot);
            }
        }

        public Character[] GetPreviewFriendlyTargets()
        {
            Dictionary<string, Character> storedTargetSnapshot = new(_skill._storedTargets);
            Dictionary<string, Character[]> storedTargetArraySnapshot = new(
                _skill._storedTargetArrays
            );
            try
            {
                ClearStoredTargets();

                Character dummy = _skill.OwnerCharater?.BattleNode?.dummy;
                return CollectPreviewTargets(_skill, _steps)
                    .Where(target =>
                        target != null
                        && target != dummy
                        && target.IsPlayer == _skill.OwnerCharater?.IsPlayer
                        && target.State == Character.CharacterState.Normal
                    )
                    .Distinct()
                    .ToArray();
            }
            finally
            {
                RestoreStoredTargets(storedTargetSnapshot, storedTargetArraySnapshot);
            }
        }

        public PreviewDamageEntry[] GetPreviewHostileDamageEntries(
            bool includeTargetVulnerable = true
        )
        {
            Dictionary<string, Character> storedTargetSnapshot = new(_skill._storedTargets);
            Dictionary<string, Character[]> storedTargetArraySnapshot = new(
                _skill._storedTargetArrays
            );
            try
            {
                ClearStoredTargets();

                Character dummy = _skill.OwnerCharater?.BattleNode?.dummy;
                var context = new PreviewDamageContext(
                    _skill.OwnerCharater,
                    includeTargetVulnerable
                );
                var aggregated = new Dictionary<Character, (int damage, int hits)>();
                var orderedTargets = new List<Character>();

                for (int i = 0; i < _steps.Length; i++)
                {
                    IEnumerable<PreviewDamageEntry> entries =
                        _steps[i]?.PreviewDamage(_skill, context)
                        ?? Array.Empty<PreviewDamageEntry>();
                    foreach (PreviewDamageEntry entry in entries)
                    {
                        if (
                            entry.Target == null
                            || entry.Target == dummy
                            || entry.Target.State != Character.CharacterState.Normal
                        )
                        {
                            continue;
                        }

                        if (!aggregated.TryGetValue(entry.Target, out var current))
                        {
                            aggregated[entry.Target] = (entry.Damage, entry.HitCount);
                            orderedTargets.Add(entry.Target);
                            continue;
                        }

                        aggregated[entry.Target] = (
                            current.damage + entry.Damage,
                            current.hits + entry.HitCount
                        );
                    }
                }

                return orderedTargets
                    .Select(target =>
                    {
                        var data = aggregated[target];
                        return new PreviewDamageEntry(target, data.damage, data.hits);
                    })
                    .ToArray();
            }
            finally
            {
                RestoreStoredTargets(storedTargetSnapshot, storedTargetArraySnapshot);
            }
        }

        public Character[] GetPreviewHostileDebuffTargets()
        {
            Dictionary<string, Character> storedTargetSnapshot = new(_skill._storedTargets);
            Dictionary<string, Character[]> storedTargetArraySnapshot = new(
                _skill._storedTargetArrays
            );
            try
            {
                ClearStoredTargets();

                Character dummy = _skill.OwnerCharater?.BattleNode?.dummy;
                return CollectPreviewHostileDebuffTargets(_skill, _steps)
                    .Where(target =>
                        target != null
                        && target != dummy
                        && target.State == Character.CharacterState.Normal
                    )
                    .Distinct()
                    .ToArray();
            }
            finally
            {
                RestoreStoredTargets(storedTargetSnapshot, storedTargetArraySnapshot);
            }
        }

        public bool RequiresManualFriendlyTarget() =>
            _steps.Any(step => StepRequiresManualFriendlyTarget(step));

        public bool ManualFriendlyTargetExcludesSelf() =>
            _steps.Any(step => StepManualFriendlyTargetExcludesSelf(step));

        public bool ManualFriendlyTargetAllowsDying() =>
            _steps.Any(step => StepManualFriendlyTargetAllowsDying(step));

        private void ClearStoredTargets()
        {
            _skill._storedTargets.Clear();
            _skill._storedTargetArrays.Clear();
        }

        private void RestoreStoredTargets(
            Dictionary<string, Character> storedTargetSnapshot,
            Dictionary<string, Character[]> storedTargetArraySnapshot
        )
        {
            ClearStoredTargets();
            foreach (var pair in storedTargetSnapshot)
                _skill._storedTargets[pair.Key] = pair.Value;
            foreach (var pair in storedTargetArraySnapshot)
                _skill._storedTargetArrays[pair.Key] = pair.Value;
        }
    }

    protected abstract class SkillStep
    {
        public abstract Task Execute(Skill skill);

        public abstract IEnumerable<string> Describe(Skill skill);

        public virtual IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return Array.Empty<Character>();
        }

        public virtual IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            return Array.Empty<PreviewDamageEntry>();
        }

        public virtual IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            return Array.Empty<Character>();
        }
    }

    private static bool StepRequiresManualFriendlyTarget(SkillStep step)
    {
        if (step == null)
            return false;

        var fields = step.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            object value = fields[i].GetValue(step);
            if (
                value is TargetSelection target
                && target.Kind == TargetSelectionKind.ManualFriendly
            )
                return true;

            if (value is SkillStep nestedStep && StepRequiresManualFriendlyTarget(nestedStep))
                return true;

            if (value is IEnumerable<SkillStep> nestedSteps)
            {
                foreach (SkillStep item in nestedSteps)
                {
                    if (StepRequiresManualFriendlyTarget(item))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool StepManualFriendlyTargetExcludesSelf(SkillStep step)
    {
        if (step == null)
            return false;

        if (step is CarrySkillStepImpl carryStep && carryStep.TargetIsManualFriendly)
            return true;

        var fields = step.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            object value = fields[i].GetValue(step);
            if (value is SkillStep nestedStep && StepManualFriendlyTargetExcludesSelf(nestedStep))
                return true;

            if (value is IEnumerable<SkillStep> nestedSteps)
            {
                foreach (SkillStep item in nestedSteps)
                {
                    if (StepManualFriendlyTargetExcludesSelf(item))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool StepManualFriendlyTargetAllowsDying(SkillStep step)
    {
        if (step == null)
            return false;

        if (step is HealFriendlySkillStep healStep && healStep.AllowsDyingManualFriendlyTarget)
            return true;

        var fields = step.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < fields.Length; i++)
        {
            object value = fields[i].GetValue(step);
            if (value is SkillStep nestedStep && StepManualFriendlyTargetAllowsDying(nestedStep))
                return true;

            if (value is IEnumerable<SkillStep> nestedSteps)
            {
                foreach (SkillStep item in nestedSteps)
                {
                    if (StepManualFriendlyTargetAllowsDying(item))
                        return true;
                }
            }
        }

        return false;
    }

    // Offensive hostile steps
    protected SkillStep AttackStep(
        int baseDamage = 0,
        int multiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int times = 1,
        int clampMax = 9999,
        bool byBehindRow = false,
        HostileTargetSelection? target = null,
        Func<Character, bool> targetCondition = null,
        string conditionText = null,
        string storeAs = null
    ) =>
        AttackStepCore(
            baseDamage,
            multiplier,
            prefix,
            suffix,
            times,
            clampMax,
            target ?? HostileTargets(1, byBehindRow),
            storeAs,
            null,
            targetCondition,
            conditionText
        );

    protected SkillStep AttackStep(
        Func<Skill, int> baseDamage,
        int multiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int times = 1,
        int clampMax = 9999,
        bool byBehindRow = false,
        HostileTargetSelection? target = null,
        Func<Character, bool> targetCondition = null,
        string conditionText = null,
        string storeAs = null
    ) =>
        AttackStepCore(
            0,
            multiplier,
            prefix,
            suffix,
            times,
            clampMax,
            target ?? HostileTargets(1, byBehindRow),
            storeAs,
            baseDamage,
            targetCondition,
            conditionText
        );

    protected SkillStep AttackStep(
        int baseDamage,
        HostileTargetReference target,
        int multiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int times = 1,
        int clampMax = 9999,
        bool byBehindRow = false,
        Func<Character, bool> targetCondition = null,
        string conditionText = null,
        string storeAs = null
    ) =>
        AttackStepCore(
            baseDamage,
            multiplier,
            prefix,
            suffix,
            times,
            clampMax,
            ResolveHostileTargetSelection(target, byBehindRow),
            storeAs,
            null,
            targetCondition,
            conditionText
        );

    protected SkillStep AttackStep(
        HostileTargetReference target,
        Func<Skill, int> baseDamage,
        int multiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int times = 1,
        int clampMax = 9999,
        bool byBehindRow = false,
        Func<Character, bool> targetCondition = null,
        string conditionText = null,
        string storeAs = null
    ) =>
        AttackStepCore(
            0,
            multiplier,
            prefix,
            suffix,
            times,
            clampMax,
            ResolveHostileTargetSelection(target, byBehindRow),
            storeAs,
            baseDamage,
            targetCondition,
            conditionText
        );

    private SkillStep AttackStepCore(
        int baseDamage,
        int multiplier,
        string prefix,
        string suffix,
        int times,
        int clampMax,
        HostileTargetSelection target,
        string storeAs,
        Func<Skill, int> baseDamageFunc,
        Func<Character, bool> targetCondition,
        string conditionText
    ) =>
        new HostileAttackSkillStep(
            baseDamage,
            multiplier,
            prefix,
            suffix,
            times,
            clampMax,
            target,
            storeAs,
            baseDamageFunc,
            targetCondition,
            conditionText
        );

    protected SkillStep DoubleStrikeStep(
        int baseDamage = 0,
        int multiplier = 1,
        string prefix = "每段造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        bool includeTwoHitText = true,
        bool byBehindRow = false,
        TargetReference target = TargetReference.DefaultRule,
        string storeAs = null
    ) =>
        DoubleStrikeStepCore(
            baseDamage,
            multiplier,
            prefix,
            suffix,
            clampMax,
            includeTwoHitText,
            byBehindRow,
            TargetValue(target),
            storeAs,
            null
        );

    protected SkillStep DoubleStrikeStep(
        Func<Skill, int> baseDamage,
        int multiplier = 1,
        string prefix = "每段造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        bool includeTwoHitText = true,
        bool byBehindRow = false,
        TargetReference target = TargetReference.DefaultRule,
        string storeAs = null
    ) =>
        DoubleStrikeStepCore(
            0,
            multiplier,
            prefix,
            suffix,
            clampMax,
            includeTwoHitText,
            byBehindRow,
            TargetValue(target),
            storeAs,
            baseDamage
        );

    private SkillStep DoubleStrikeStepCore(
        int baseDamage,
        int multiplier,
        string prefix,
        string suffix,
        int clampMax,
        bool includeTwoHitText,
        bool byBehindRow,
        TargetSelection target,
        string storeAs,
        Func<Skill, int> baseDamageFunc
    ) =>
        new DoubleStrikeSkillStep(
            baseDamage,
            multiplier,
            prefix,
            suffix,
            clampMax,
            includeTwoHitText,
            byBehindRow,
            target,
            storeAs,
            baseDamageFunc
        );

    protected SkillStep ApplyBuffHostile(
        Buff.BuffName buffName,
        int stacks,
        HostileTargetSelection target = default
    ) => new ApplyBuffHostileSkillStep(buffName, stacks, target);

    protected SkillStep ApplyBuffHostile(
        Buff.BuffName buffName,
        Func<Skill, int> stacks,
        HostileTargetSelection target = default
    ) => new ApplyBuffHostileSkillStep(buffName, 0, target, stacks);

    // Summon and summon-support steps
    /// <param name="slotSelector">
    /// 召唤位置选择器。
    /// 0 表示最前空位，9 表示最后空位。
    /// 负数表示从自身前第 N 位起继续向前寻找空位，正数表示从自身后第 N 位起继续向后寻找空位。
    /// 例如 -1 表示优先上一个位置，1 表示优先下一个位置。
    /// </param>
    /// <param name="summonScene">用于实例化召唤物的 PackedScene，实例必须继承 SummonCharacter。</param>
    protected SkillStep SummonStep(int slotSelector, PackedScene summonScene) =>
        new SummonSkillStep(slotSelector, summonScene);

    protected SkillStep ApplyBuffSummonsStep(Buff.BuffName buffName, int stacks, int count = 0) =>
        new ApplyBuffSummonsSkillStep(buffName, stacks, count);

    protected SkillStep ApplyBuffSummonsStep(
        Buff.BuffName buffName,
        Func<Skill, int> stacks,
        int count = 0
    ) => new ApplyBuffSummonsSkillStep(buffName, 0, count, stacks);

    protected SkillStep ModifySummonPropertyStep(PropertyType type, int value, int count = 0) =>
        new ModifySummonPropertySkillStep(type, value, count);

    protected SkillStep BlockSummonsStep(
        int baseBlock = 0,
        int count = 0,
        int multiplier = 1,
        int clampMax = 999
    ) => BlockSummonsStepCore(baseBlock, count, multiplier, clampMax, null);

    protected SkillStep BlockSummonsStep(
        Func<Skill, int> baseBlock,
        int count = 0,
        int multiplier = 1,
        int clampMax = 999
    ) => BlockSummonsStepCore(0, count, multiplier, clampMax, baseBlock);

    private SkillStep BlockSummonsStepCore(
        int baseBlock,
        int count,
        int multiplier,
        int clampMax,
        Func<Skill, int> baseBlockProvider
    ) =>
        new BlockSummonsSkillStep(
            baseBlock,
            multiplier,
            count,
            clampMax,
            baseBlockProvider
        );

    protected SkillStep HealSummonsStep(int baseHeal = 0, int count = 0, int clampMax = 999) =>
        HealSummonsStepCore(baseHeal, count, clampMax, null);

    protected SkillStep HealSummonsStep(
        Func<Skill, int> baseHeal,
        int count = 0,
        int clampMax = 999
    ) => HealSummonsStepCore(0, count, clampMax, baseHeal);

    private SkillStep HealSummonsStepCore(
        int baseHeal,
        int count,
        int clampMax,
        Func<Skill, int> baseHealFunc
    ) => new HealSummonsSkillStep(baseHeal, count, clampMax, baseHealFunc);

    protected SkillStep LowerTargetPropertyStep(
        PropertyType type,
        int value,
        HostileTargetReference target = HostileTargetReference.One,
        bool permanent = false
    ) => new LowerTargetPropertySkillStep(type, value, target, permanent);

    protected SkillStep LowerTargetPropertyStep(
        PropertyType type,
        int value,
        HostileTargetSelection target,
        bool permanent = false
    ) => new LowerTargetPropertySkillStep(type, value, target, permanent);

    // Friendly support steps
    protected SkillStep ApplyBuffFriendly(
        Buff.BuffName buffName,
        int stacks,
        TargetReference target = TargetReference.Self,
        bool includeSummonsWhenAll = false
    ) =>
        new ApplyBuffFriendlySkillStep(
            buffName,
            stacks,
            TargetValue(target),
            includeSummonsWhenAll
        );

    protected SkillStep ApplyBuffFriendly(
        Buff.BuffName buffName,
        Func<Skill, int> stacks,
        TargetReference target = TargetReference.Self,
        bool includeSummonsWhenAll = false
    ) =>
        new ApplyBuffFriendlySkillStep(
            buffName,
            0,
            TargetValue(target),
            includeSummonsWhenAll,
            stacks
        );

    protected SkillStep HealStep(
        int baseHeal = 0,
        TargetReference target = TargetReference.Self,
        bool preferNonFull = false,
        bool rebirth = false,
        int clampMax = 999,
        string descriptionOverride = null,
        string storeAs = null,
        bool includeSummonsWhenAll = false,
        int repeatCount = 1
    ) =>
        HealFriendlyCore(
            baseHeal,
            TargetValue(target),
            preferNonFull,
            rebirth,
            clampMax,
            null,
            descriptionOverride,
            storeAs,
            includeSummonsWhenAll,
            repeatCount
        );

    protected SkillStep HealStep(
        Func<Skill, int> baseHeal,
        TargetReference target = TargetReference.Self,
        bool preferNonFull = false,
        bool rebirth = false,
        int clampMax = 999,
        string descriptionOverride = null,
        string storeAs = null,
        bool includeSummonsWhenAll = false,
        int repeatCount = 1
    ) =>
        HealFriendlyCore(
            0,
            TargetValue(target),
            preferNonFull,
            rebirth,
            clampMax,
            baseHeal,
            descriptionOverride,
            storeAs,
            includeSummonsWhenAll,
            repeatCount
        );

    private SkillStep HealFriendlyCore(
        int baseHeal,
        TargetSelection target,
        bool preferNonFull,
        bool rebirth,
        int clampMax,
        Func<Skill, int> baseHealFunc,
        string descriptionOverride,
        string storeAs,
        bool includeSummonsWhenAll,
        int repeatCount
    ) =>
        new HealFriendlySkillStep(
            baseHeal,
            target,
            preferNonFull,
            rebirth,
            clampMax,
            baseHealFunc,
            descriptionOverride,
            storeAs,
            includeSummonsWhenAll,
            repeatCount
        );

    protected SkillStep HurtFriendly(
        int damage,
        TargetReference target = TargetReference.Self,
        bool includeSummonsWhenAll = true
    ) => new HurtFriendlySkillStep(damage, TargetValue(target), includeSummonsWhenAll);

    protected SkillStep EnergyStep(int delta, TargetReference target = TargetReference.Self) =>
        new EnergySkillStep(delta, TargetValue(target));

    protected SkillStep EnergyStep(
        Func<Skill, int> delta,
        TargetReference target = TargetReference.Self
    ) => new EnergySkillStep(delta, TargetValue(target));

    protected SkillStep DrawCardsStep(int count) => new DrawCardsSkillStep(count);

    protected SkillStep DrawCardsStep(Func<Skill, int> count, string description = null) =>
        new DrawCardsSkillStep(count, description);

    protected SkillStep AddStatusCardsToDrawPileStep(
        SkillID statusSkillId,
        int count,
        HostileTargetSelection target,
        string targetText = null
    ) => new AddStatusCardsToDrawPileSkillStep(
        statusSkillId,
        count,
        target,
        targetText ?? HostileTargetText(target)
    );

    // Friendly property / defense / utility steps
    protected SkillStep ModifyPropertyStep(
        PropertyType type,
        int value,
        TargetReference target = TargetReference.Self,
        bool includeSummonsWhenAll = false
    ) =>
        new ModifyFriendlyPropertySkillStep(
            type,
            value,
            TargetValue(target),
            includeSummonsWhenAll
        );

    protected SkillStep ModifyPropertyStep(
        PropertyType type,
        Func<Skill, int> value,
        TargetReference target = TargetReference.Self,
        bool includeSummonsWhenAll = false
    ) =>
        new ModifyFriendlyPropertySkillStep(
            type,
            0,
            TargetValue(target),
            includeSummonsWhenAll,
            value
        );

    protected SkillStep BlockStep(
        int baseBlock = 0,
        TargetReference target = TargetReference.Self,
        int multiplier = 1,
        int clampMax = 999,
        bool describe = true,
        string descriptionPrefix = null,
        bool includeSummonsWhenAll = false
    ) =>
        BlockStepCore(
            TargetValue(target),
            baseBlock,
            multiplier,
            clampMax,
            describe,
            descriptionPrefix,
            includeSummonsWhenAll,
            null
        );

    protected SkillStep BlockStep(
        Func<Skill, int> baseBlock,
        TargetReference target = TargetReference.Self,
        int multiplier = 1,
        int clampMax = 999,
        bool describe = true,
        string descriptionPrefix = null,
        bool includeSummonsWhenAll = false
    ) =>
        BlockStepCore(
            TargetValue(target),
            0,
            multiplier,
            clampMax,
            describe,
            descriptionPrefix,
            includeSummonsWhenAll,
            baseBlock
        );

    private SkillStep BlockStepCore(
        TargetSelection target,
        int baseBlock,
        int multiplier,
        int clampMax,
        bool describe,
        string descriptionPrefix,
        bool includeSummonsWhenAll,
        Func<Skill, int> baseBlockProvider
    ) =>
        new BlockFriendlySkillStep(
            target,
            baseBlock,
            multiplier,
            clampMax,
            describe,
            descriptionPrefix,
            includeSummonsWhenAll,
            baseBlockProvider
        );

    private SkillStep CarryStep(
        TargetSelection target,
        int skillIndex,
        bool describe = true,
        string descriptionLine = null
    ) => new CarrySkillStepImpl(target, skillIndex, describe, descriptionLine);

    protected SkillStep CarryStep(
        TargetReference target,
        int skillIndex,
        bool describe = true,
        string descriptionLine = null
    ) => CarryStep(TargetValue(target), skillIndex, describe, descriptionLine);

    protected SkillStep SwapPositionFriendlyStep(
        int relativeIndexA,
        int relativeIndexB,
        bool describe = true,
        string descriptionLine = null
    ) =>
        new SwapPositionFriendlySkillStep(
            relativeIndexA,
            relativeIndexB,
            describe,
            descriptionLine
        );

    protected SkillStep EnergyTimesWhileStep(
        int paidEnergyPerLoop = 0,
        Func<int> times = null,
        Action<int> setTimes = null,
        params SkillStep[] loopSteps
    ) => new EnergyTimesWhileSkillStep(paidEnergyPerLoop, times, setTimes, loopSteps);

    private (Func<int> GetTimes, Action<int> SetTimes) ResolveEnergyTimesMember(
        string memberExpression
    )
    {
        string memberName = NormalizeEnergyTimesMemberName(memberExpression);
        if (string.IsNullOrWhiteSpace(memberName))
        {
            throw new InvalidOperationException("次数成员名不能为空。");
        }

        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (Type current = GetType(); current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(memberName, flags);
            if (field != null)
            {
                if (field.FieldType != typeof(int))
                {
                    throw new InvalidOperationException(
                        $"次数成员 `{memberName}` 必须是 int 字段。"
                    );
                }

                return (
                    () => (int)(field.GetValue(this) ?? 0),
                    value => field.SetValue(this, value)
                );
            }

            PropertyInfo property = current.GetProperty(memberName, flags);
            if (property != null)
            {
                if (property.PropertyType != typeof(int) || !property.CanRead || !property.CanWrite)
                {
                    throw new InvalidOperationException(
                        $"次数成员 `{memberName}` 必须是可读写的 int 属性。"
                    );
                }

                return (
                    () => (int)(property.GetValue(this) ?? 0),
                    value => property.SetValue(this, value)
                );
            }
        }

        throw new InvalidOperationException(
            $"未在 `{GetType().Name}` 上找到次数成员 `{memberExpression}`。简写仅支持当前技能实例上的 int 字段或属性。"
        );
    }

    private static string NormalizeEnergyTimesMemberName(string memberExpression)
    {
        if (string.IsNullOrWhiteSpace(memberExpression))
            return null;

        string memberName = memberExpression.Trim();
        if (memberName.StartsWith("this.", StringComparison.Ordinal))
            memberName = memberName["this.".Length..];

        return memberName;
    }

    protected SkillStep ConditionStep(
        Func<bool> condition,
        string conditionDescription,
        params SkillStep[] onPassSteps
    ) => new ConditionSkillStep(condition, conditionDescription, onPassSteps);

    protected SkillStep BranchStep(
        Func<bool> condition,
        string conditionDescription,
        SkillStep[] onPassSteps,
        SkillStep[] onFailSteps
    ) => new BranchSkillStep(condition, conditionDescription, onPassSteps, onFailSteps);

    protected SkillStep TextStep(string line) => new TextSkillStep(line);

    protected SkillStep CustomStep(
        Func<Skill, Task> execute,
        Func<Skill, IEnumerable<string>> describe
    ) => new DelegateSkillStep(execute, describe);

    private sealed class HostileAttackSkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly Func<Skill, int> _baseDamageFunc;
        private readonly int _powerMultiplier;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _times;
        private readonly int _clampMax;
        private readonly HostileTargetSelection _target;
        private readonly string _storeAs;
        private readonly Func<Character, bool> _targetCondition;
        private readonly string _conditionText;

        public HostileAttackSkillStep(
            int baseDamage,
            int powerMultiplier,
            string prefix,
            string suffix,
            int times,
            int clampMax,
            HostileTargetSelection target,
            string storeAs,
            Func<Skill, int> baseDamageFunc,
            Func<Character, bool> targetCondition,
            string conditionText
        )
        {
            _baseDamage = baseDamage;
            _baseDamageFunc = baseDamageFunc;
            _powerMultiplier = powerMultiplier;
            _prefix = prefix;
            _suffix = suffix;
            _times = Math.Max(1, times);
            _clampMax = clampMax;
            _target = target;
            _storeAs = storeAs;
            _targetCondition = targetCondition;
            _conditionText = conditionText;
        }

        public override async Task Execute(Skill skill)
        {
            Character[] targets = SelectTargets(skill);
            StoreResolvedTargets(skill, targets);
            if (targets.Length == 0)
                return;

            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageFunc);
            int damage = skill.DamageFromPower(baseDamage, _powerMultiplier, _clampMax);
            if (targets.Length == 1)
            {
                await AttackTargetTimes(skill, targets[0], damage, _times);
                return;
            }

            int adjustedDamage = Math.Clamp(
                AttackBuff.ApplyOutgoingDamageModifiers(
                    skill.OwnerCharater,
                    damage,
                    null,
                    consumeStacks: true
                ),
                0,
                9999
            );

            List<Task> tasks = new(targets.Length);
            for (int hit = 0; hit < _times; hit++)
            {
                if (ShouldAbortStepExecution(skill))
                    break;

                for (int i = 0; i < targets.Length; i++)
                {
                    if (ShouldAbortStepExecution(skill))
                        break;

                    tasks.Add(
                        skill.Attack(
                            adjustedDamage,
                            times: 1,
                            target: targets[i],
                            playHitEffectForFirstHit: true,
                            delayAfterLastHit: true,
                            applyAttackBuff: false
                        )
                    );

                    if (i < targets.Length - 1)
                        await skill.YieldBatchedCombatFrameAsync();
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageFunc);
            if (!ShouldDescribeHostileAttackTarget(_target, _targetCondition))
            {
                yield return skill.DamageLine(
                    baseDamage,
                    _powerMultiplier,
                    _prefix,
                    _suffix,
                    _clampMax,
                    _times
                );
                yield break;
            }

            string damageText = skill.DamageFromPowerText(
                baseDamage,
                _powerMultiplier,
                _clampMax,
                _times
            );

            if (_targetCondition != null)
            {
                string conditionText = string.IsNullOrWhiteSpace(_conditionText)
                    ? I18n.Tr("skill.step.condition.matching", "符合条件")
                    : _conditionText;
                string targetText = HostileTargetText(_target);
                yield return I18n.Format(
                    "skill.step.hostile_damage.condition",
                    "对{condition}的{target}造成{damage}点伤害。",
                    ("condition", conditionText),
                    ("target", targetText),
                    ("damage", damageText)
                );
                yield break;
            }

            yield return I18n.Format(
                "skill.step.hostile_damage",
                "对{target}造成{damage}点伤害。",
                ("target", HostileTargetText(_target)),
                ("damage", damageText)
            );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            Character[] targets = SelectTargets(skill);
            StoreResolvedTargets(skill, targets);
            return targets;
        }

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageFunc);
            int damage = skill.DamageFromPower(baseDamage, _powerMultiplier, _clampMax);
            Character[] targets = PreviewTargets(skill).Where(x => x != null).ToArray();
            if (targets.Length == 0)
                yield break;

            if (targets.Length == 1)
            {
                int modifiedDamage = context.PredictDamage(targets[0], damage);
                yield return new PreviewDamageEntry(targets[0], modifiedDamage * _times, _times);
                yield break;
            }

            int adjustedDamage = context.PredictDamage(null, damage);
            for (int i = 0; i < targets.Length; i++)
            {
                int totalDamage = 0;
                for (int hit = 0; hit < _times; hit++)
                    totalDamage += context.PredictDamage(
                        targets[i],
                        adjustedDamage,
                        applyAttackBuff: false
                    );

                yield return new PreviewDamageEntry(targets[i], totalDamage, _times);
            }
        }

        private Character[] SelectTargets(Skill skill)
        {
            return skill?.ResolveHostileTargets(_target, _targetCondition) ?? Array.Empty<Character>();
        }

        private void StoreResolvedTargets(Skill skill, Character[] targets)
        {
            Character[] resolvedTargets = targets?.Where(x => x != null).ToArray() ?? Array.Empty<Character>();
            skill.StoreAutoAttackTargets(resolvedTargets);
            skill.StoreTarget(_storeAs, resolvedTargets.FirstOrDefault());
            skill.StoreTarget(_storeAs, resolvedTargets);
        }
    }

    private sealed class DoubleStrikeSkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly Func<Skill, int> _baseDamageFunc;
        private readonly int _powerMultiplier;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _clampMax;
        private readonly bool _includeTwoHitText;
        private readonly bool _byBehindRow;
        private readonly TargetSelection _target;
        private readonly string _storeAs;

        public DoubleStrikeSkillStep(
            int baseDamage,
            int powerMultiplier,
            string prefix,
            string suffix,
            int clampMax,
            bool includeTwoHitText,
            bool byBehindRow,
            TargetSelection target,
            string storeAs,
            Func<Skill, int> baseDamageFunc
        )
        {
            _baseDamage = baseDamage;
            _baseDamageFunc = baseDamageFunc;
            _powerMultiplier = powerMultiplier;
            _prefix = prefix;
            _suffix = suffix;
            _clampMax = clampMax;
            _includeTwoHitText = includeTwoHitText;
            _byBehindRow = byBehindRow;
            _target = target;
            _storeAs = storeAs;
        }

        public override async Task Execute(Skill skill)
        {
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageFunc);
            int damage = skill.DamageFromPower(baseDamage, _powerMultiplier, _clampMax);
            if (
                _target.Kind == TargetSelectionKind.DefaultRule
                && string.IsNullOrWhiteSpace(_storeAs)
            )
            {
                skill.StoreAutoAttackTarget(skill.ResolveHostileTarget(_target, _byBehindRow));
                await skill.Attack(
                    damage,
                    times: 2,
                    byBehindRow: _byBehindRow,
                    delayAfterLastHit: false
                );
                return;
            }

            Character target = skill.ResolveHostileTarget(_target, _byBehindRow);
            skill.StoreAutoAttackTarget(target);
            skill.StoreTarget(_storeAs, target);
            if (target == null)
                return;
            await AttackTargetTimes(skill, target, damage, 2);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_includeTwoHitText)
                yield return I18n.Tr("skill.step.double_strike", "二段攻击。");

            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageFunc);
            string line = skill.DamageLine(
                baseDamage,
                _powerMultiplier,
                _prefix,
                _suffix,
                _clampMax,
                2
            );
            if (_byBehindRow && !line.Contains("后排", StringComparison.Ordinal))
                line = I18n.Format(
                    "skill.step.hostile_back_prefix",
                    "对后排目标{line}",
                    ("line", line)
                );

            yield return line;
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (
                _target.Kind == TargetSelectionKind.DefaultRule
                && string.IsNullOrWhiteSpace(_storeAs)
            )
            {
                Character[] targets = PreviewHostileTargets(
                    skill,
                    maxTargets: 1,
                    byBehindRow: _byBehindRow
                ).Where(x => x != null).ToArray();
                skill.StoreAutoAttackTargets(targets);
                return targets;
            }

            Character target = skill.ResolveHostileTarget(_target, _byBehindRow);
            skill.StoreAutoAttackTarget(target);
            skill.StoreTarget(_storeAs, target);
            return SingleTargetArray(target);
        }

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageFunc);
            int damage = skill.DamageFromPower(baseDamage, _powerMultiplier, _clampMax);
            Character[] targets = PreviewTargets(skill).Where(x => x != null).ToArray();
            if (targets.Length == 0)
                yield break;

            int totalDamage = context.PredictDamage(targets[0], damage) * 2;

            yield return new PreviewDamageEntry(targets[0], totalDamage, 2);
        }
    }

    private sealed class LowerTargetPropertySkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly TargetSelection _singleTarget;
        private readonly HostileTargetSelection _multiTarget;
        private readonly bool _useSingleTarget;
        private readonly bool _permanent;
        private readonly bool _byBehindRow;

        public LowerTargetPropertySkillStep(
            PropertyType type,
            int value,
            TargetSelection target,
            bool permanent,
            bool byBehindRow
        )
        {
            _type = type;
            _value = value;
            _singleTarget = target;
            _multiTarget = default;
            _useSingleTarget = true;
            _permanent = permanent;
            _byBehindRow = byBehindRow;
        }

        public LowerTargetPropertySkillStep(
            PropertyType type,
            int value,
            HostileTargetSelection target,
            bool permanent
        )
        {
            _type = type;
            _value = value;
            _singleTarget = default;
            _multiTarget = target;
            _useSingleTarget = false;
            _permanent = permanent;
            _byBehindRow = target.ByBehindRow;
        }

        public override async Task Execute(Skill skill)
        {
            if (ShouldAbortStepExecution(skill))
                return;

            int loss = Math.Abs(_value);
            if (loss == 0)
                return;

            Character[] targets = _useSingleTarget
                ? skill.ResolveHostileTargets(_singleTarget, 1, _byBehindRow)
                : skill.ResolveHostileTargets(_multiTarget);
            if (targets.Length == 0)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
                if (ShouldAbortStepExecution(skill))
                    return;
                if (targets[i] == null)
                    continue;
                bool tryPermanent = _permanent && targets[i] is PlayerCharacter;
                int before = 0;
                if (tryPermanent)
                    before = GetPropertyValue(targets[i], _type);
                await targets[i].DescendingProperties(_type, loss, skill?.OwnerCharater);
                if (tryPermanent)
                {
                    int after = GetPropertyValue(targets[i], _type);
                    if (after != before)
                        ApplyPermanentPropertyLoss(targets[i], _type, loss);
                }
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int loss = Math.Abs(_value);
            if (loss == 0)
                yield break;

            string loseText = LosePropertyText(_type, loss);
            string targetText = _useSingleTarget
                ? HostileTargetText(_singleTarget, 1, _byBehindRow)
                : HostileTargetText(_multiTarget);

            yield return I18n.Format(
                "skill.step.lower_target_property",
                "下降{target}{loss}{permanent}。",
                ("target", targetText),
                ("loss", loseText),
                ("permanent", PermanentTag())
            );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (Math.Abs(_value) == 0)
                return Array.Empty<Character>();

            return _useSingleTarget
                ? skill.ResolveHostileTargets(_singleTarget, 1, _byBehindRow)
                : skill.ResolveHostileTargets(_multiTarget);
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            return PreviewTargets(skill);
        }

        private string PermanentTag()
        {
            return _permanent ? I18n.Tr("skill.step.permanent_tag", "(永久)") : string.Empty;
        }
    }

    private static void ApplyPermanentPropertyLoss(Character target, PropertyType type, int loss)
    {
        if (loss <= 0)
            return;

        if (target is not PlayerCharacter player)
            return;

        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
            return;

        int index = player.CharacterIndex;
        if (index < 0 || index >= GameInfo.PlayerCharacters.Length)
            return;

        PlayerInfoStructure info = GameInfo.PlayerCharacters[index];
        switch (type)
        {
            case PropertyType.Power:
                info.Power -= loss;
                break;
            case PropertyType.Survivability:
                info.Survivability -= loss;
                break;
            case PropertyType.Speed:
                info.Speed -= loss;
                break;
            case PropertyType.MaxLife:
                info.LifeMax -= loss;
                break;
            default:
                return;
        }
        GameInfo.PlayerCharacters[index] = info;
    }

    private static int GetPropertyValue(Character target, PropertyType type)
    {
        if (target == null)
            return 0;

        return type switch
        {
            PropertyType.Power => target.BattlePower,
            PropertyType.Survivability => target.BattleSurvivability,
            PropertyType.Speed => target.Speed,
            PropertyType.MaxLife => target.BattleMaxLife,
            _ => 0,
        };
    }

    private static bool TryApplyBuffToTarget(
        Buff.BuffName buffName,
        Character target,
        int stacks,
        Character source = null
    )
    {
        if (target == null || stacks == 0)
            return false;

        switch (buffName)
        {
            case Buff.BuffName.RebirthI:
                DyingBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.DamageImmune:
            case Buff.BuffName.Vulnerable:
            case Buff.BuffName.Taunt:
            case Buff.BuffName.Thorn:
            case Buff.BuffName.AutoArmor:
                HurtBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.Weaken:
            case Buff.BuffName.Shadow:
            case Buff.BuffName.CursePower:
                AttackBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.Stun:
            case Buff.BuffName.Echo:
            case Buff.BuffName.Fear:
                SkillBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.Pursuit:
            case Buff.BuffName.ExtraTurn:
            case Buff.BuffName.Disaster:
            case Buff.BuffName.Demon:
            case Buff.BuffName.Void:
            case Buff.BuffName.Sanctuary:
                EndActionBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.Invisible:
            case Buff.BuffName.EternalDark:
            case Buff.BuffName.Swift:
            case Buff.BuffName.Source:
            case Buff.BuffName.Barricade:
            case Buff.BuffName.Afterimage:
            case Buff.BuffName.Divinity:
                StartActionBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.DebuffImmunity:
            case Buff.BuffName.ExtraPower:
            case Buff.BuffName.ExtraSurvivability:
            case Buff.BuffName.ExtraDraw:
            case Buff.BuffName.Beacon:
            case Buff.BuffName.WeakeningField:
                SpecialBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            default:
                return false;
        }
    }

    private static string RelativeFriendlyTargetText(int index)
    {
        return index switch
        {
            0 => "自己",
            -1 => "上一位队友",
            1 => "下一位队友",
            > 1 => $"下{index}位队友",
            _ => $"上{-index}位队友",
        };
    }

    private static string FriendlyTargetText(TargetSelection target)
    {
        return target.Kind switch
        {
            TargetSelectionKind.Stored => StoredTargetText(target.StoredKey),
            TargetSelectionKind.Relative => RelativeFriendlyTargetText(target.RelativeIndex),
            TargetSelectionKind.Absolute => AbsoluteFriendlySelectorText(target.AbsoluteSelector),
            TargetSelectionKind.ManualFriendly => "选择一名己方角色",
            _ => RelativeFriendlyTargetText(0),
        };
    }

    private static bool IsSelfFriendlyTarget(TargetSelection target) =>
        target.Kind == TargetSelectionKind.Relative && target.RelativeIndex == 0;

    private static bool IsManualFriendlyTarget(TargetSelection target) =>
        target.Kind == TargetSelectionKind.ManualFriendly;

    private static bool IsImplicitSelfFriendlyTarget(TargetSelection target) =>
        target.Kind == TargetSelectionKind.DefaultRule || IsSelfFriendlyTarget(target);

    private static string FriendlyTargetTextForDescription(TargetSelection target) =>
        IsSelfFriendlyTarget(target)
            ? I18n.Tr("skill.step.target.current_character", "当前角色")
            : FriendlyTargetText(target);

    private static string RelativeFriendlyTargetTextForDescription(int index)
    {
        return index switch
        {
            0 => I18n.Tr("skill.step.target.current_character", "当前角色"),
            -1 => I18n.Tr("skill.step.target.previous_ally", "上一位队友"),
            1 => I18n.Tr("skill.step.target.next_ally", "下一位队友"),
            > 1 => I18n.Format(
                "skill.step.target.next_ally_n",
                "下{count}位队友",
                ("count", index)
            ),
            _ => I18n.Format(
                "skill.step.target.previous_ally_n",
                "上{count}位队友",
                ("count", -index)
            ),
        };
    }

    private static Character[] GetAllHostileWithOrder(Skill skill, bool dyingFilter)
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return Array.Empty<Character>();

        return skill.GetHostileTargetsInTeamOrder(dyingFilter, returnDummyWhenEmpty: false);
    }

    private static async Task ApplyPropertyDelta(
        Character target,
        PropertyType type,
        int value,
        Character source = null
    )
    {
        if (target == null || value == 0)
            return;

        if (value > 0)
            await target.IncreaseProperties(type, value, source);
        else
            await target.DescendingProperties(type, -value, source);
    }

    private static bool ShouldAbortStepExecution(Skill skill)
    {
        if (skill == null)
            return true;

        if (skill._stopRemainingPlanExecution)
            return true;

        if (skill.OwnerCharater?.State == Character.CharacterState.Dying)
        {
            skill._stopRemainingPlanExecution = true;
            return true;
        }

        if (skill.OwnerCharater?.BattleNode?.ShouldAbortSkillResolution() == true)
        {
            skill._stopRemainingPlanExecution = true;
            return true;
        }

        return false;
    }

    private static int ResolveStepBaseValue(
        Skill skill,
        int fixedValue,
        Func<Skill, int> valueProvider
    ) => valueProvider?.Invoke(skill) ?? fixedValue;

    private static int ResolveFriendlyHealAmount(Skill skill, int baseHeal, int clampMax)
    {
        return Math.Clamp(baseHeal, 0, clampMax);
    }

    private static string FriendlyHealAmountText(Skill skill, int baseHeal, int clampMax) =>
        ResolveFriendlyHealAmount(skill, baseHeal, clampMax).ToString();

    private static string AppendRepeatSuffix(string line, int repeatCount)
    {
        if (repeatCount <= 1)
            return line;

        return line
            + I18n.Format(
                "skill.step.repeat_suffix",
                "(重复{count}次)",
                ("count", repeatCount)
            );
    }

    private static PlayerCharacter ResolveCardPileOwner(Character target)
    {
        if (target is PlayerCharacter player)
            return player;

        if (target is SummonCharacter summon)
        {
            if (summon.Summoner is PlayerCharacter summoner)
                return summoner;

            if (summon.LastSummoner is PlayerCharacter lastSummoner)
                return lastSummoner;
        }

        return null;
    }

    private static string PropertyDeltaActionText(PropertyType type, int value)
    {
        int amount = Math.Abs(value);
        string propertyText = $"{amount}{GetColoredPropertyLabel(type)}";
        return value >= 0
            ? I18n.Format(
                "skill.step.property.increase",
                "增加{property}",
                ("property", propertyText)
            )
            : I18n.Format(
                "skill.step.property.decrease",
                "减少{property}",
                ("property", propertyText)
            );
    }

    private static string HostileTargetText(
        TargetSelection target,
        int maxTargets,
        bool byBehindRow
    )
    {
        if (target.UsesStoredTarget)
            return StoredTargetText(target.StoredKey);

        if (maxTargets == 1)
            return byBehindRow
                ? I18n.Tr("skill.step.target.hostile_back", "后排目标")
                : I18n.Tr("skill.step.target.hostile", "目标");
        if (maxTargets <= 0)
            return I18n.Tr("skill.step.target.all_hit", "所有命中目标");

        return byBehindRow
            ? I18n.Format(
                "skill.step.target.max_back_hostile",
                "至多{count}名后排目标",
                ("count", maxTargets)
            )
            : I18n.Format(
                "skill.step.target.max_hostile",
                "至多{count}名目标",
                ("count", maxTargets)
            );
    }

    private static bool ShouldDescribeHostileAttackTarget(
        HostileTargetSelection target,
        Func<Character, bool> targetCondition
    )
    {
        if (targetCondition != null)
            return true;

        return target.Kind switch
        {
            HostileTargetSelection._Kind.Ordered =>
                target.MaxTargets != 1 || target.ByBehindRow,
            _ => true,
        };
    }

    private static string HostileTargetText(HostileTargetSelection target)
    {
        return target.Kind switch
        {
            HostileTargetSelection._Kind.Stored => StoredTargetText(target.StoredKey),
            HostileTargetSelection._Kind.Ordered => target.MaxTargets switch
            {
                1 => target.ByBehindRow
                    ? I18n.Tr("skill.step.target.hostile_back", "后排目标")
                    : I18n.Tr("skill.step.target.hostile", "目标"),
                <= 0 => target.ByBehindRow
                    ? I18n.Tr("skill.step.target.all_back_hostile", "所有后排目标")
                    : I18n.Tr("skill.step.target.all_hostile", "所有目标"),
                _ => target.ByBehindRow
                    ? I18n.Format(
                        "skill.step.target.max_back_hostile",
                        "至多{count}名后排目标",
                        ("count", target.MaxTargets)
                    )
                    : I18n.Format(
                        "skill.step.target.max_hostile",
                        "至多{count}名目标",
                        ("count", target.MaxTargets)
                    ),
            },
            HostileTargetSelection._Kind.Random => target.MaxTargets switch
            {
                1 => target.ByBehindRow
                    ? I18n.Tr("skill.step.target.random_hostile_back", "随机1名后排目标")
                    : I18n.Tr("skill.step.target.random_hostile", "随机1名目标"),
                <= 0 => target.ByBehindRow
                    ? I18n.Tr("skill.step.target.random_all_back_hostile", "随机后排目标")
                    : I18n.Tr("skill.step.target.random_all_hostile", "随机目标"),
                _ => target.ByBehindRow
                    ? I18n.Format(
                        "skill.step.target.random_max_back_hostile",
                        "随机{count}名后排目标",
                        ("count", target.MaxTargets)
                    )
                    : I18n.Format(
                        "skill.step.target.random_max_hostile",
                        "随机{count}名目标",
                        ("count", target.MaxTargets)
                    ),
            },
            HostileTargetSelection._Kind.EachRowFirst => I18n.Tr(
                "skill.step.target.each_row_first",
                "各横排第一名角色"
            ),
            HostileTargetSelection._Kind.EachRowLast => I18n.Tr(
                "skill.step.target.each_row_last",
                "各横排最后一名角色"
            ),
            HostileTargetSelection._Kind.EachColFirst => I18n.Tr(
                "skill.step.target.each_col_first",
                "各列第一名角色"
            ),
            HostileTargetSelection._Kind.EachColLast => I18n.Tr(
                "skill.step.target.each_col_last",
                "各列最后一名角色"
            ),
            _ => I18n.Tr("skill.step.target.hostile", "目标"),
        };
    }

    private static string StoredTargetText(string storedKey)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return I18n.Tr("skill.step.target.hostile", "目标");

        if (string.Equals(storedKey, BuiltinAttackTargetKey, StringComparison.Ordinal))
            return I18n.Tr("skill.target_ref.attack", "攻击目标");
        if (string.Equals(storedKey, BuiltinHealTargetKey, StringComparison.Ordinal))
            return I18n.Tr("skill.target_ref.heal", "治疗目标");

        return I18n.Tr($"skill.target_ref.{storedKey}", storedKey);
    }

    private static string AbsoluteFriendlySelectorText(AbsoluteFriendlySelector selector)
    {
        return selector switch
        {
            AbsoluteFriendlySelector.FrontMost => I18n.Tr(
                "skill.step.target.frontmost_ally",
                "站位最前队友"
            ),
            AbsoluteFriendlySelector.BackMost => I18n.Tr(
                "skill.step.target.backmost_ally",
                "站位最后队友"
            ),
            AbsoluteFriendlySelector.LowestLife => I18n.Tr(
                "skill.step.target.lowest_life_ally",
                "生命最少队友"
            ),
            AbsoluteFriendlySelector.All => I18n.Tr("skill.step.target.all_allies", "友方全阵"),
            _ => I18n.Tr("skill.step.target.ally", "队友"),
        };
    }

    private static Character SelectAbsoluteFriendlyTarget(
        Skill skill,
        AbsoluteFriendlySelector selector,
        bool preferNonFull,
        bool rebirth,
        ISet<Character> excludedTargets = null
    )
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return null;

        Character[] allies = skill
            .GetAllAllyWithOrder(false)
            .Where(x => x != null && (excludedTargets == null || !excludedTargets.Contains(x)))
            .ToArray();
        if (allies.Length == 0)
            return null;

        Character PickBySelector(IEnumerable<Character> source)
        {
            var list = source?.Where(x => x != null).ToArray() ?? Array.Empty<Character>();
            if (list.Length == 0)
                return null;

            return selector switch
            {
                AbsoluteFriendlySelector.FrontMost => list.OrderBy(x => x.PositionIndex)
                    .FirstOrDefault(),
                AbsoluteFriendlySelector.BackMost => list.OrderByDescending(x => x.PositionIndex)
                    .FirstOrDefault(),
                AbsoluteFriendlySelector.LowestLife => list.OrderBy(x => x.Life)
                    .ThenBy(x => x.PositionIndex)
                    .FirstOrDefault(),
                AbsoluteFriendlySelector.All => list.OrderBy(x => x.PositionIndex).FirstOrDefault(),
                _ => list.OrderBy(x => x.PositionIndex).FirstOrDefault(),
            };
        }

        if (rebirth)
            return PickBySelector(allies);

        var normal = allies.Where(x => x.State != Character.CharacterState.Dying);

        if (preferNonFull)
        {
            var nonFull = normal.Where(x => x.Life < x.BattleMaxLife);
            var nonFullTarget = PickBySelector(nonFull);
            if (nonFullTarget != null)
                return nonFullTarget;
        }

        var fallback = rebirth ? allies : normal;
        var fallbackTarget = PickBySelector(fallback);
        if (fallbackTarget != null)
            return fallbackTarget;

        return PickBySelector(allies);
    }

    private static Character[] SingleTargetArray(Character target)
    {
        return target == null ? Array.Empty<Character>() : [target];
    }

    private static Character[] SelectFriendlyTargets(
        Skill skill,
        AbsoluteFriendlySelector selector,
        bool dyingFilter,
        bool preferNonFull,
        bool rebirth,
        bool includeSummonsWhenAll
    )
    {
        if (selector == AbsoluteFriendlySelector.All)
        {
            if (skill?.OwnerCharater?.BattleNode == null)
                return Array.Empty<Character>();

            return skill
                .GetAllAllyWithOrder(dyingFilter, includeSummons: includeSummonsWhenAll)
                .Where(x => x != null)
                .ToArray();
        }

        return SingleTargetArray(
            SelectAbsoluteFriendlyTarget(skill, selector, preferNonFull, rebirth)
        );
    }

    private static SummonCharacter[] GetOwnedSummons(Skill skill, bool dyingFilter = true)
    {
        if (skill?.OwnerCharater?.Summons == null)
            return Array.Empty<SummonCharacter>();

        return skill
            .OwnerCharater.Summons.Where(x =>
                x != null
                && GodotObject.IsInstanceValid(x)
                && (!dyingFilter || x.State != Character.CharacterState.Dying)
            )
            .OrderBy(x => x.PositionIndex)
            .ToArray();
    }

    private static SummonCharacter[] SelectOwnedSummons(
        Skill skill,
        int count,
        bool dyingFilter = true
    )
    {
        SummonCharacter[] summons = GetOwnedSummons(skill, dyingFilter);
        if (summons.Length == 0)
            return Array.Empty<SummonCharacter>();

        if (count == 0)
            return summons;

        int safeCount = Math.Min(Math.Abs(count), summons.Length);
        if (count > 0)
            return summons.Take(safeCount).ToArray();

        return summons.Skip(summons.Length - safeCount).ToArray();
    }

    private static string SummonSelectionText(int count)
    {
        if (count == 0)
            return I18n.Tr("skill.step.target.all_summons", "全部召唤物");
        if (count > 0)
            return I18n.Format(
                "skill.step.target.front_summons",
                "最前{count}个召唤物",
                ("count", count)
            );

        return I18n.Format(
            "skill.step.target.back_summons",
            "最后{count}个召唤物",
            ("count", -count)
        );
    }

    private static string SummonSlotSelectorText(int slotSelector)
    {
        if (slotSelector == 0)
            return I18n.Tr("skill.step.target.front_empty_slot", "最前空位");
        if (slotSelector == 9)
            return I18n.Tr("skill.step.target.back_empty_slot", "最后空位");
        if (slotSelector > 0)
            return I18n.Format(
                "skill.step.target.find_empty_slot_after",
                "从自身后第{count}位起向后寻找空位",
                ("count", slotSelector)
            );

        return I18n.Format(
            "skill.step.target.find_empty_slot_before",
            "从自身前第{count}位起向前寻找空位",
            ("count", -slotSelector)
        );
    }

    private sealed class ApplyBuffHostileSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly HostileTargetSelection _hostileTarget;
        private readonly TargetSelection _targetReference;
        private readonly Func<Skill, int> _stacksFunc;

        public ApplyBuffHostileSkillStep(
            Buff.BuffName buffName,
            int stacks,
            HostileTargetSelection target,
            Func<Skill, int> stacksFunc = null
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _hostileTarget = target;
            _targetReference = default;
            _stacksFunc = stacksFunc;
        }

        public ApplyBuffHostileSkillStep(
            Buff.BuffName buffName,
            int stacks,
            TargetSelection target,
            Func<Skill, int> stacksFunc = null
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _hostileTarget = default;
            _targetReference = target;
            _stacksFunc = stacksFunc;
        }

        public override Task Execute(Skill skill)
        {
            int stacks = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            Character[] targets = ResolveTargets(skill);
            for (int i = 0; i < targets.Length; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], stacks, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int stacks = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            string stacksText = BuffStacksText(_buffName, stacks);
            string targetText = _targetReference.UsesStoredTarget
                ? StoredTargetText(_targetReference.StoredKey)
                : HostileTargetText(_hostileTarget);
            yield return I18n.Format(
                "skill.step.apply_buff_hostile",
                "使{target}获得{stacks}。",
                ("target", targetText),
                ("stacks", stacksText)
            );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return ResolveTargets(skill);
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            return PreviewTargets(skill);
        }

        private Character[] ResolveTargets(Skill skill)
        {
            if (_targetReference.Kind == TargetSelectionKind.StoredArray)
            {
                Character dummy = skill?.OwnerCharater?.BattleNode?.dummy;
                return skill
                    .GetStoredTargetArray(_targetReference.StoredKey)
                    .Where(x =>
                        x != null && x != dummy && x.State == Character.CharacterState.Normal
                    )
                    .ToArray();
            }

            if (_targetReference.Kind == TargetSelectionKind.Stored)
            {
                Character stored = skill.GetStoredTarget(_targetReference.StoredKey);
                if (
                    stored == null
                    || stored == skill?.OwnerCharater?.BattleNode?.dummy
                    || stored.State == Character.CharacterState.Dying
                )
                {
                    return Array.Empty<Character>();
                }

                return [stored];
            }

            return skill.ResolveHostileTargets(_hostileTarget);
        }
    }

    private sealed class SummonSkillStep : SkillStep
    {
        private readonly int _slotSelector;
        private readonly PackedScene _summonScene;

        /// <param name="slotSelector">
        /// 召唤位置选择器。
        /// 0 表示最前空位，9 表示最后空位。
        /// 负数表示从自身前第 N 位起继续向前寻找空位，正数表示从自身后第 N 位起继续向后寻找空位。
        /// </param>
        /// <param name="summonScene">用于生成召唤物实例的 PackedScene，要求实例类型为 SummonCharacter。</param>
        public SummonSkillStep(int slotSelector, PackedScene summonScene)
        {
            _slotSelector = slotSelector;
            _summonScene = summonScene;
        }

        public override Task Execute(Skill skill)
        {
            if (skill?.OwnerCharater?.BattleNode == null || _summonScene == null)
                return Task.CompletedTask;

            Node instance = _summonScene.Instantiate();
            if (instance is not SummonCharacter summon)
            {
                instance?.Free();
                return Task.CompletedTask;
            }

            skill.OwnerCharater.BattleNode.AddSummon(summon, skill.OwnerCharater, _slotSelector);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            yield return I18n.Format(
                "skill.step.summon",
                "在{slot}召唤1个召唤物。",
                ("slot", SummonSlotSelectorText(_slotSelector))
            );
        }
    }

    private sealed class ApplyBuffSummonsSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly int _count;
        private readonly Func<Skill, int> _stacksFunc;

        public ApplyBuffSummonsSkillStep(
            Buff.BuffName buffName,
            int stacks,
            int count,
            Func<Skill, int> stacksFunc = null
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _count = count;
            _stacksFunc = stacksFunc;
        }

        public override Task Execute(Skill skill)
        {
            int stacks = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            SummonCharacter[] targets = SelectOwnedSummons(skill, _count);
            for (int i = 0; i < targets.Length; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], stacks, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = SummonSelectionText(_count);
            int stacks = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            string stacksText = BuffStacksText(_buffName, stacks);
            yield return I18n.Format(
                "skill.step.apply_buff_target",
                "使{target}获得{stacks}。",
                ("target", targetText),
                ("stacks", stacksText)
            );
        }
    }

    private sealed class ApplyBuffFriendlySkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly TargetSelection _target;
        private readonly bool _includeSummonsWhenAll;
        private readonly Func<Skill, int> _stacksFunc;

        public ApplyBuffFriendlySkillStep(
            Buff.BuffName buffName,
            int stacks,
            TargetSelection target,
            bool includeSummonsWhenAll,
            Func<Skill, int> stacksFunc = null
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _target = target;
            _includeSummonsWhenAll = includeSummonsWhenAll;
            _stacksFunc = stacksFunc;
        }

        public override Task Execute(Skill skill)
        {
            int stacks = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            Character[] targets = skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
            for (int i = 0; i < targets.Length; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], stacks, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = FriendlyTargetTextForDescription(_target);
            int stacks = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            string stacksText = BuffStacksText(_buffName, stacks);
            if (IsSelfFriendlyTarget(_target))
                yield return I18n.Format(
                    "skill.step.gain_buff_self",
                    "获得{stacks}。",
                    ("stacks", stacksText)
                );
            else
                yield return I18n.Format(
                    "skill.step.apply_buff_target",
                    "使{target}获得{stacks}。",
                    ("target", targetText),
                    ("stacks", stacksText)
                );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
        }
    }

    private sealed class HurtFriendlySkillStep : SkillStep
    {
        private readonly int _damage;
        private readonly TargetSelection _target;
        private readonly bool _includeSummonsWhenAll;

        public HurtFriendlySkillStep(int damage, TargetSelection target, bool includeSummonsWhenAll)
        {
            _damage = Math.Max(0, damage);
            _target = target;
            _includeSummonsWhenAll = includeSummonsWhenAll;
        }

        public override async Task Execute(Skill skill)
        {
            if (_damage <= 0)
                return;

            Character[] targets = ResolveTargets(skill);
            if (targets.Length == 0)
                return;

            List<Task> tasks = new(targets.Length);
            for (int i = 0; i < targets.Length; i++)
            {
                if (ShouldAbortStepExecution(skill))
                    break;

                tasks.Add(targets[i].GetHurt(_damage, skill?.OwnerCharater));

                if (i < targets.Length - 1)
                    await skill.YieldBatchedCombatFrameAsync();
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_damage <= 0)
                yield break;

            string targetText = FriendlyTargetTextForDescription(_target);
            if (IsSelfFriendlyTarget(_target))
                yield return I18n.Format(
                    "skill.step.hurt.self",
                    "受到{damage}点伤害。",
                    ("damage", _damage)
                );
            else
                yield return I18n.Format(
                    "skill.step.hurt.target",
                    "对{target}造成{damage}点伤害。",
                    ("target", targetText),
                    ("damage", _damage)
                );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (_damage <= 0)
                return Array.Empty<Character>();

            return ResolveTargets(skill);
        }

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            if (_damage <= 0)
                return Array.Empty<PreviewDamageEntry>();

            var targets = PreviewTargets(skill).Where(x => x != null).ToArray();
            if (targets.Length == 0)
                return Array.Empty<PreviewDamageEntry>();

            return targets.Select(target => new PreviewDamageEntry(target, _damage, 1));
        }

        private Character[] ResolveTargets(Skill skill)
        {
            return skill
                .ResolveFriendlyTargets(
                    _target,
                    dyingFilter: true,
                    includeSummonsWhenAll: _includeSummonsWhenAll
                )
                .Where(target => target != null)
                .ToArray();
        }
    }

    private sealed class HealFriendlySkillStep : SkillStep
    {
        private readonly int _baseHeal;
        private readonly Func<Skill, int> _baseHealFunc;
        private readonly TargetSelection _target;
        private readonly bool _preferNonFull;
        private readonly bool _rebirth;
        private readonly int _clampMax;
        private readonly string _descriptionOverride;
        private readonly string _storeAs;
        private readonly bool _includeSummonsWhenAll;
        private readonly int _repeatCount;
        public bool AllowsDyingManualFriendlyTarget => _rebirth && IsManualFriendlyTarget(_target);

        public HealFriendlySkillStep(
            int baseHeal,
            TargetSelection target,
            bool preferNonFull,
            bool rebirth,
            int clampMax,
            Func<Skill, int> baseHealFunc,
            string descriptionOverride,
            string storeAs,
            bool includeSummonsWhenAll,
            int repeatCount
        )
        {
            _baseHeal = baseHeal;
            _baseHealFunc = baseHealFunc;
            _target = target;
            _preferNonFull = preferNonFull;
            _rebirth = rebirth;
            _clampMax = clampMax;
            _descriptionOverride = descriptionOverride;
            _storeAs = storeAs;
            _includeSummonsWhenAll = includeSummonsWhenAll;
            _repeatCount = Math.Max(1, repeatCount);
        }

        public override Task Execute(Skill skill)
        {
            List<Character> allTargets = new();
            HashSet<Character> selectedTargets = _repeatCount > 1 ? new() : null;
            for (int repeat = 0; repeat < _repeatCount; repeat++)
            {
                Character[] targets;
                bool useUniqueAbsoluteTarget =
                    selectedTargets != null
                    && _target.Kind == TargetSelectionKind.Absolute
                    && _target.AbsoluteSelector != AbsoluteFriendlySelector.All;
                if (useUniqueAbsoluteTarget)
                {
                    Character uniqueTarget = SelectAbsoluteFriendlyTarget(
                        skill,
                        _target.AbsoluteSelector,
                        _preferNonFull,
                        _rebirth,
                        selectedTargets
                    );
                    if (uniqueTarget == null)
                        break;

                    selectedTargets.Add(uniqueTarget);
                    targets = SingleTargetArray(uniqueTarget);
                }
                else
                {
                    targets = skill.ResolveFriendlyTargets(
                        _target,
                        !_rebirth,
                        _preferNonFull,
                        _rebirth,
                        _includeSummonsWhenAll
                    );
                }
                allTargets.AddRange(targets.Where(x => x != null));
                skill.StoreTarget(_storeAs, targets.FirstOrDefault());
                if (targets.Length == 0)
                    continue;

                int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealFunc);
                int heal = ResolveFriendlyHealAmount(skill, baseHeal, _clampMax);
                for (int i = 0; i < targets.Length; i++)
                    targets[i].Recover(heal, rebirth: _rebirth, source: skill?.OwnerCharater);
            }

            skill.StoreAutoHealTargets(allTargets.ToArray());
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!string.IsNullOrWhiteSpace(_descriptionOverride))
            {
                yield return _descriptionOverride;
                yield break;
            }

            string targetText = FriendlyTargetTextForDescription(_target);
            int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealFunc);
            string healText = FriendlyHealAmountText(skill, baseHeal, _clampMax);

            if (_rebirth)
            {
                if (IsImplicitSelfFriendlyTarget(_target))
                {
                    yield return AppendRepeatSuffix(
                        I18n.Format(
                            "skill.step.rebirth.self",
                            "复生{heal}点生命。",
                            ("heal", healText)
                        ),
                        _repeatCount
                    );
                    yield break;
                }

                yield return AppendRepeatSuffix(
                    I18n.Format(
                        "skill.step.rebirth.target",
                        "使{target}复生{heal}点生命。",
                        ("target", targetText),
                        ("heal", healText)
                    ),
                    _repeatCount
                );
                yield break;
            }

            yield return AppendRepeatSuffix(
                IsImplicitSelfFriendlyTarget(_target)
                    ? I18n.Format(
                        "skill.step.heal.self",
                        "回复{heal}点生命。",
                        ("heal", healText)
                    )
                    : I18n.Format(
                        "skill.step.heal.target",
                        "使{target}回复{heal}点生命。",
                        ("target", targetText),
                        ("heal", healText)
                    ),
                _repeatCount
            );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            Character[] targets = skill.ResolveFriendlyTargets(
                _target,
                !_rebirth,
                _preferNonFull,
                _rebirth,
                _includeSummonsWhenAll
            );
            skill.StoreAutoHealTargets(targets);
            return targets;
        }
    }

    private sealed class ModifyFriendlyPropertySkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly Func<Skill, int> _valueProvider;
        private readonly TargetSelection _target;
        private readonly bool _includeSummonsWhenAll;

        public ModifyFriendlyPropertySkillStep(
            PropertyType type,
            int value,
            TargetSelection target,
            bool includeSummonsWhenAll,
            Func<Skill, int> valueProvider = null
        )
        {
            _type = type;
            _value = value;
            _valueProvider = valueProvider;
            _target = target;
            _includeSummonsWhenAll = includeSummonsWhenAll;
        }

        public override async Task Execute(Skill skill)
        {
            if (ShouldAbortStepExecution(skill))
                return;

            int value = ResolveStepBaseValue(skill, _value, _valueProvider);
            if (value == 0)
                return;

            Character[] targets = skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
            for (int i = 0; i < targets.Length; i++)
            {
                if (ShouldAbortStepExecution(skill))
                    return;
                await ApplyPropertyDelta(targets[i], _type, value, skill?.OwnerCharater);
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int value = ResolveStepBaseValue(skill, _value, _valueProvider);
            if (value == 0)
                yield break;

            string targetText = FriendlyTargetTextForDescription(_target);
            string deltaText = PropertyDeltaActionText(_type, value);
            if (IsSelfFriendlyTarget(_target))
                yield return I18n.Format(
                    "skill.step.modify_property.self",
                    "{delta}。",
                    ("delta", deltaText)
                );
            else
                yield return I18n.Format(
                    "skill.step.modify_property.target",
                    "使{target}{delta}。",
                    ("target", targetText),
                    ("delta", deltaText)
                );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            int value = ResolveStepBaseValue(skill, _value, _valueProvider);
            if (value == 0)
                return Array.Empty<Character>();

            return skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
        }
    }

    private sealed class ModifySummonPropertySkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly int _count;

        public ModifySummonPropertySkillStep(PropertyType type, int value, int count)
        {
            _type = type;
            _value = value;
            _count = count;
        }

        public override async Task Execute(Skill skill)
        {
            if (ShouldAbortStepExecution(skill))
                return;

            SummonCharacter[] targets = SelectOwnedSummons(skill, _count);
            for (int i = 0; i < targets.Length; i++)
            {
                if (ShouldAbortStepExecution(skill))
                    return;
                await ApplyPropertyDelta(targets[i], _type, _value, skill?.OwnerCharater);
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_value == 0)
                yield break;

            string targetText = SummonSelectionText(_count);
            string deltaText = PropertyDeltaActionText(_type, _value);
            yield return I18n.Format(
                "skill.step.modify_property.target",
                "使{target}{delta}。",
                ("target", targetText),
                ("delta", deltaText)
            );
        }
    }

    private sealed class BlockSummonsSkillStep : SkillStep
    {
        private readonly int _baseBlock;
        private readonly Func<Skill, int> _baseBlockProvider;
        private readonly int _survivabilityMultiplier;
        private readonly int _count;
        private readonly int _clampMax;

        public BlockSummonsSkillStep(
            int baseBlock,
            int survivabilityMultiplier,
            int count,
            int clampMax,
            Func<Skill, int> baseBlockProvider
        )
        {
            _baseBlock = baseBlock;
            _baseBlockProvider = baseBlockProvider;
            _survivabilityMultiplier = survivabilityMultiplier;
            _count = count;
            _clampMax = clampMax;
        }

        public override Task Execute(Skill skill)
        {
            if (skill?.OwnerCharater?.BattleNode == null)
                return Task.CompletedTask;

            int baseBlock = ResolveStepBaseValue(skill, _baseBlock, _baseBlockProvider);
            int block = skill.BlockFromSurvivability(
                baseBlock,
                _survivabilityMultiplier,
                _clampMax
            );
            SummonCharacter[] targets = SelectOwnedSummons(skill, _count);
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].UpdataBlock(block, source: skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = SummonSelectionText(_count);
            int baseBlock = ResolveStepBaseValue(skill, _baseBlock, _baseBlockProvider);
            string blockText = skill.BlockFromSurvivabilityText(
                baseBlock,
                _survivabilityMultiplier,
                _clampMax
            );
            yield return I18n.Format(
                "skill.step.block.target",
                "令{target}获得{block}点格挡。",
                ("target", targetText),
                ("block", blockText)
            );
        }
    }

    private sealed class HealSummonsSkillStep : SkillStep
    {
        private readonly int _baseHeal;
        private readonly Func<Skill, int> _baseHealFunc;
        private readonly int _count;
        private readonly int _clampMax;

        public HealSummonsSkillStep(
            int baseHeal,
            int count,
            int clampMax,
            Func<Skill, int> baseHealFunc
        )
        {
            _baseHeal = baseHeal;
            _baseHealFunc = baseHealFunc;
            _count = count;
            _clampMax = clampMax;
        }

        public override Task Execute(Skill skill)
        {
            SummonCharacter[] targets = SelectOwnedSummons(skill, _count, dyingFilter: true);
            int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealFunc);
            int heal = Math.Clamp(baseHeal, 0, _clampMax);
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].Recover(heal, rebirth: false, source: skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = SummonSelectionText(_count);
            int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealFunc);
            int heal = Math.Clamp(baseHeal, 0, _clampMax);
            yield return I18n.Format(
                "skill.step.heal.target",
                "使{target}回复{heal}点生命。",
                ("target", targetText),
                ("heal", heal)
            );
        }
    }

    private sealed class BlockFriendlySkillStep : SkillStep
    {
        private readonly int _relativeIndex;
        private readonly TargetSelection _target;
        private readonly int _baseBlock;
        private readonly Func<Skill, int> _baseBlockProvider;
        private readonly int _survivabilityMultiplier;
        private readonly int _clampMax;
        private readonly bool _describe;
        private readonly string _descriptionPrefix;
        private readonly bool _includeSummonsWhenAll;

        public BlockFriendlySkillStep(
            TargetSelection target,
            int baseBlock,
            int survivabilityMultiplier,
            int clampMax,
            bool describe,
            string descriptionPrefix,
            bool includeSummonsWhenAll,
            Func<Skill, int> baseBlockProvider
        )
        {
            _relativeIndex = target.RelativeIndex;
            _target = target;
            _baseBlock = baseBlock;
            _baseBlockProvider = baseBlockProvider;
            _survivabilityMultiplier = survivabilityMultiplier;
            _clampMax = clampMax;
            _describe = describe;
            _descriptionPrefix = descriptionPrefix;
            _includeSummonsWhenAll = includeSummonsWhenAll;
        }

        public override Task Execute(Skill skill)
        {
            if (skill.OwnerCharater?.BattleNode == null)
                return Task.CompletedTask;

            int baseBlock = ResolveStepBaseValue(skill, _baseBlock, _baseBlockProvider);
            int block = skill.BlockFromSurvivability(
                baseBlock,
                _survivabilityMultiplier,
                _clampMax
            );

            Character[] targets = skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
            for (int i = 0; i < targets.Length; i++)
            {
                int previousBlock = targets[i].Block;
                targets[i].UpdataBlock(block, source: skill?.OwnerCharater);
                int gainedBlock = Math.Max(0, targets[i].Block - previousBlock);
                SpecialBuff.TriggerBeaconBlockShare(targets[i], gainedBlock, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!_describe)
                yield break;

            int baseBlock = ResolveStepBaseValue(skill, _baseBlock, _baseBlockProvider);
            string blockText = skill.BlockFromSurvivabilityText(
                baseBlock,
                _survivabilityMultiplier,
                _clampMax
            );

            if (!string.IsNullOrWhiteSpace(_descriptionPrefix))
            {
                yield return I18n.Format(
                    "skill.step.block.prefix",
                    "{prefix}{block}点格挡。",
                    ("prefix", _descriptionPrefix),
                    ("block", blockText)
                );
                yield break;
            }

            if (_target.Kind != TargetSelectionKind.Relative)
            {
                string directTargetText = FriendlyTargetTextForDescription(_target);
                if (IsSelfFriendlyTarget(_target))
                    yield return I18n.Format(
                        "skill.step.block.self",
                        "获得{block}点格挡。",
                        ("block", blockText)
                    );
                else
                    yield return I18n.Format(
                        "skill.step.block.target",
                        "令{target}获得{block}点格挡。",
                        ("target", directTargetText),
                        ("block", blockText)
                    );
                yield break;
            }
            string targetText = RelativeFriendlyTargetTextForDescription(_relativeIndex);
            if (_relativeIndex == 0)
                yield return I18n.Format(
                    "skill.step.block.self",
                    "获得{block}点格挡。",
                    ("block", blockText)
                );
            else
                yield return I18n.Format(
                    "skill.step.block.target",
                    "令{target}获得{block}点格挡。",
                    ("target", targetText),
                    ("block", blockText)
                );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
        }
    }

    private sealed class CarrySkillStepImpl : SkillStep
    {
        private readonly TargetSelection _target;
        private readonly int _relativeIndex;
        private readonly int _skillIndex;
        private readonly bool _describe;
        private readonly string _descriptionLine;
        public bool TargetIsManualFriendly => _target.Kind == TargetSelectionKind.ManualFriendly;

        public CarrySkillStepImpl(
            TargetSelection target,
            int skillIndex,
            bool describe,
            string descriptionLine
        )
        {
            _target = target;
            _relativeIndex = target.Kind == TargetSelectionKind.Relative ? target.RelativeIndex : 0;
            _skillIndex = skillIndex;
            _describe = describe;
            if (
                string.IsNullOrWhiteSpace(descriptionLine)
                && target.Kind != TargetSelectionKind.Relative
            )
            {
                string skillText = GetCarrySkillText(skillIndex);
                _descriptionLine = I18n.Format(
                    "skill.step.carry.target",
                    "连携{target}角色随机打出1张{skill}。",
                    ("target", FriendlyTargetText(target)),
                    ("skill", skillText)
                );
            }
            else
            {
                _descriptionLine = descriptionLine;
            }
        }

        public override async Task Execute(Skill skill)
        {
            if (skill.OwnerCharater?.BattleNode == null)
                return;

            var target = skill.ResolveFriendlyTarget(_target, dyingFilter: true);
            if (target == null)
                return;
            if (!TryGetCarrySkillType(_skillIndex, out _))
                return;

            await skill.Carry(target, _skillIndex);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!_describe)
                yield break;

            if (!string.IsNullOrWhiteSpace(_descriptionLine))
            {
                yield return _descriptionLine;
                yield break;
            }

            string relativeText = RelativeFriendlyTargetTextForDescription(_relativeIndex);
            yield return I18n.Format(
                "skill.step.carry.target",
                "连携{target}角色随机打出1张{skill}。",
                ("target", relativeText),
                ("skill", GetCarrySkillText(_skillIndex))
            );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return SingleTargetArray(skill.ResolveFriendlyTarget(_target, dyingFilter: true));
        }

        private static string GetCarrySkillText(int skillIndex)
        {
            return skillIndex switch
            {
                0 => I18n.Tr("skill.step.skill_type.any", "任意技能"),
                1 => I18n.Tr("skill.step.skill_type.attack", "攻击技能"),
                2 => I18n.Tr("skill.step.skill_type.survive", "生存技能"),
                3 => I18n.Tr("skill.step.skill_type.special", "特殊技能"),
                _ => I18n.Format(
                    "skill.step.skill_type.indexed",
                    "第{index}个技能",
                    ("index", skillIndex + 1)
                ),
            };
        }
    }

    private sealed class SwapPositionFriendlySkillStep : SkillStep
    {
        private readonly int _relativeIndexA;
        private readonly int _relativeIndexB;
        private readonly bool _describe;
        private readonly string _descriptionLine;

        public SwapPositionFriendlySkillStep(
            int relativeIndexA,
            int relativeIndexB,
            bool describe,
            string descriptionLine
        )
        {
            _relativeIndexA = relativeIndexA;
            _relativeIndexB = relativeIndexB;
            _describe = describe;
            _descriptionLine = descriptionLine;
        }

        public override async Task Execute(Skill skill)
        {
            if (skill?.OwnerCharater?.BattleNode == null)
                return;

            var first = skill.GetAllyByRelative(_relativeIndexA, dyingFilter: true);
            var second = skill.GetAllyByRelative(_relativeIndexB, dyingFilter: true);
            if (first == null || second == null || first == second)
                return;

            await skill.SwapPositionIndex(first, second);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!_describe)
                yield break;

            if (!string.IsNullOrWhiteSpace(_descriptionLine))
            {
                yield return _descriptionLine;
                yield break;
            }

            string firstText = RelativeFriendlyTargetTextForDescription(_relativeIndexA);
            string secondText = RelativeFriendlyTargetTextForDescription(_relativeIndexB);
            yield return I18n.Format(
                "skill.step.swap_position",
                "交换{first}与{second}的位置。",
                ("first", firstText),
                ("second", secondText)
            );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return new[]
            {
                skill.GetAllyByRelative(_relativeIndexA, dyingFilter: true),
                skill.GetAllyByRelative(_relativeIndexB, dyingFilter: true),
            }.Where(target => target != null);
        }
    }

    private sealed class EnergySkillStep : SkillStep
    {
        private readonly int _delta;
        private readonly Func<Skill, int> _deltaProvider;
        private readonly TargetSelection _target;
        private readonly bool _useFriendlyTarget;

        public EnergySkillStep(int delta)
        {
            _delta = delta;
            _deltaProvider = null;
            _target = default;
            _useFriendlyTarget = false;
        }

        public EnergySkillStep(Func<Skill, int> delta)
        {
            _delta = 0;
            _deltaProvider = delta;
            _target = default;
            _useFriendlyTarget = false;
        }

        public EnergySkillStep(int delta, TargetSelection target)
        {
            _delta = delta;
            _deltaProvider = null;
            _target = target;
            _useFriendlyTarget = true;
        }

        public EnergySkillStep(Func<Skill, int> delta, TargetSelection target)
        {
            _delta = 0;
            _deltaProvider = delta;
            _target = target;
            _useFriendlyTarget = true;
        }

        public override Task Execute(Skill skill)
        {
            int delta = ResolveStepBaseValue(skill, _delta, _deltaProvider);
            if (delta == 0)
                return Task.CompletedTask;

            if (_useFriendlyTarget)
            {
                Character[] targets = skill.ResolveFriendlyTargets(_target, dyingFilter: true);
                if (targets.Length == 0)
                    return Task.CompletedTask;

                for (int i = 0; i < targets.Length; i++)
                {
                    if (ShouldAbortStepExecution(skill))
                        return Task.CompletedTask;

                    targets[i].UpdataEnergy(delta, skill?.OwnerCharater);
                }
                return Task.CompletedTask;
            }

            if (skill.OwnerCharater == null)
                return Task.CompletedTask;

            skill.OwnerCharater.UpdataEnergy(delta, skill.OwnerCharater);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int delta = ResolveStepBaseValue(skill, _delta, _deltaProvider);
            if (delta == 0)
                yield break;

            string energyText = delta > 0
                ? I18n.Format(
                    "skill.step.energy.gain_phrase",
                    "获得{amount}点能量",
                    ("amount", delta)
                )
                : I18n.Format(
                    "skill.step.energy.lose_phrase",
                    "失去{amount}点能量",
                    ("amount", -delta)
                );
            if (_useFriendlyTarget)
            {
                string targetText = FriendlyTargetTextForDescription(_target);
                if (IsSelfFriendlyTarget(_target))
                    yield return delta > 0
                        ? I18n.Format(
                            "skill.step.energy.gain_self",
                            "获得{amount}点能量。",
                            ("amount", delta)
                        )
                        : I18n.Format(
                            "skill.step.energy.lose_self",
                            "失去{amount}点能量。",
                            ("amount", -delta)
                        );
                else if (IsManualFriendlyTarget(_target))
                    yield return delta > 0
                        ? I18n.Format(
                            "skill.step.energy.manual_gain",
                            "{target}，使其获得{amount}点能量。",
                            ("target", targetText),
                            ("amount", delta)
                        )
                        : I18n.Format(
                            "skill.step.energy.manual_lose",
                            "{target}，使其失去{amount}点能量。",
                            ("target", targetText),
                            ("amount", -delta)
                        );
                else
                    yield return I18n.Format(
                        "skill.step.energy.target",
                        "使{target}{energy}。",
                        ("target", targetText),
                        ("energy", energyText)
                    );
                yield break;
            }

            if (delta > 0)
                yield return I18n.Format(
                    "skill.step.energy.gain_self",
                    "获得{amount}点能量。",
                    ("amount", delta)
                );
            else
                yield return I18n.Format(
                    "skill.step.energy.lose_self",
                    "失去{amount}点能量。",
                    ("amount", -delta)
                );
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (!_useFriendlyTarget)
                return Array.Empty<Character>();

            int delta = ResolveStepBaseValue(skill, _delta, _deltaProvider);
            if (delta == 0)
                return Array.Empty<Character>();

            return skill.ResolveFriendlyTargets(_target, dyingFilter: true);
        }
    }

    private sealed class DrawCardsSkillStep : SkillStep
    {
        private readonly int _count;
        private readonly Func<Skill, int> _countProvider;
        private readonly string _description;

        public DrawCardsSkillStep(int count)
        {
            _count = count;
            _countProvider = null;
        }

        public DrawCardsSkillStep(Func<Skill, int> count, string description = null)
        {
            _count = 0;
            _countProvider = count;
            _description = description;
        }

        public override Task Execute(Skill skill)
        {
            int count = ResolveStepBaseValue(skill, _count, _countProvider);
            if (count <= 0 || skill?.OwnerCharater is not PlayerCharacter player)
                return Task.CompletedTask;

            player.TryDrawBattleCards(count);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!string.IsNullOrWhiteSpace(_description))
            {
                yield return _description.EndsWith("。")
                    ? _description
                    : I18n.Format("skill.step.with_period", "{text}。", ("text", _description));
                yield break;
            }

            int count = ResolveStepBaseValue(skill, _count, _countProvider);
            if (count <= 0)
                yield break;

            yield return I18n.Format(
                "skill.step.draw_cards",
                "抽{count}张牌。",
                ("count", count)
            );
        }
    }

    private sealed class AddStatusCardsToDrawPileSkillStep : SkillStep
    {
        private readonly SkillID _statusSkillId;
        private readonly int _count;
        private readonly HostileTargetSelection _hostileTarget;
        private readonly string _targetText;

        public AddStatusCardsToDrawPileSkillStep(
            SkillID statusSkillId,
            int count,
            HostileTargetSelection hostileTarget,
            string targetText
        )
        {
            _statusSkillId = statusSkillId;
            _count = count;
            _hostileTarget = hostileTarget;
            _targetText = string.IsNullOrWhiteSpace(targetText) ? "目标" : targetText;
        }

        public override async Task Execute(Skill skill)
        {
            if (skill == null || _count <= 0)
                return;

            Character[] targets = ResolveStatusInsertTargets(skill);
            if (targets.Length == 0)
                return;

            var affectedTargets = targets
                .Select(target => new { Target = target, Player = ResolveCardPileOwner(target) })
                .Where(x => x.Player != null && x.Player.BattleNode != null)
                .GroupBy(x => x.Target)
                .Select(group => group.First())
                .ToArray();

            foreach (var animationGroup in affectedTargets
                .Where(entry => entry.Player.BattleNode.CharacterControl != null)
                .GroupBy(entry => entry.Player.BattleNode.CharacterControl))
            {
                await animationGroup.Key.PlayStatusCardInsertAnimationAsync(
                    animationGroup.Select(
                            entry =>
                                new CharacterControl.StatusCardInsertAnimationEntry(
                                    entry.Target,
                                    _statusSkillId,
                                    _count,
                                    skill.OwnerCharater
                                )
                        )
                        .ToArray()
                );
            }

            foreach (var playerGroup in affectedTargets.GroupBy(entry => entry.Player))
            {
                PlayerCharacter player = playerGroup.Key;
                player.BattleNode.AddPlayerBattleStatusCardsToDrawPile(
                    player,
                    _statusSkillId,
                    _count * playerGroup.Count(),
                    skill.OwnerCharater
                );
            }
        }

        private Character[] ResolveStatusInsertTargets(Skill skill)
        {
            if (skill?.OwnerCharater?.BattleNode == null)
                return Array.Empty<Character>();

            if (
                _hostileTarget.Kind == HostileTargetSelection._Kind.Ordered
                && _hostileTarget.MaxTargets <= 0
            )
            {
                return skill.OwnerCharater.BattleNode.GetOrderedTeamCharacters(
                    !skill.OwnerCharater.IsPlayer,
                    includeSummons: true,
                    dyingFilter: true
                );
            }

            return skill.ResolveHostileTargets(_hostileTarget);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_count <= 0)
                yield break;

            string statusName =
                Skill.GetSkill(_statusSkillId)?.SkillName ?? _statusSkillId.ToString();
            yield return I18n.Format(
                "skill.step.add_status_to_draw_pile",
                "向{target}抽牌堆塞入{count}张{status}。",
                ("target", _targetText),
                ("count", _count),
                ("status", statusName)
            );
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            return PreviewHostileTargets(skill, _hostileTarget);
        }
    }

    private sealed class EnergyTimesGateSkillStep : SkillStep
    {
        private readonly Func<int> _times;
        private readonly Action<int> _setTimes;
        private readonly SkillStep[] _onPassSteps;

        public EnergyTimesGateSkillStep(
            Func<int> times,
            Action<int> setTimes,
            SkillStep[] onPassSteps
        )
        {
            _times = times;
            _setTimes = setTimes;
            _onPassSteps = onPassSteps ?? Array.Empty<SkillStep>();
        }

        public override async Task Execute(Skill skill)
        {
            if (ShouldAbortStepExecution(skill))
                return;

            if (!TryPassTimesGate(skill, _times, _setTimes))
                return;

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                if (ShouldAbortStepExecution(skill))
                    return;
                await _onPassSteps[i].Execute(skill);
                if (ShouldAbortStepExecution(skill))
                    return;
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            bool hasTimes = _times != null;
            bool consumesTimes = hasTimes && _setTimes != null;
            int currentTimes = hasTimes ? Math.Max(0, _times?.Invoke() ?? 0) : 0;

            if (hasTimes)
            {
                if (consumesTimes)
                    yield return I18n.Format(
                        "skill.step.times_gate.consume_header",
                        "次数-1(当前次数：{count})：",
                        ("count", currentTimes)
                    );
                else
                    yield return I18n.Format(
                        "skill.step.times_gate.current_header",
                        "当前次数：{count}：",
                        ("count", currentTimes)
                    );
            }

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return I18n.Format(
                        "skill.step.effect_prefix",
                        "生效：{line}",
                        ("line", line)
                    );
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (!CanPassTimesGate(skill, _times))
                return Array.Empty<Character>();

            return CollectPreviewTargets(skill, _onPassSteps);
        }

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            if (!CanPassTimesGate(skill, _times))
                return Array.Empty<PreviewDamageEntry>();

            return CollectPreviewDamage(skill, _onPassSteps, context);
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            if (!CanPassTimesGate(skill, _times))
                return Array.Empty<Character>();

            return CollectPreviewHostileDebuffTargets(skill, _onPassSteps);
        }
    }

    private sealed class EnergyTimesWhileSkillStep : SkillStep
    {
        private readonly int _paidEnergyPerLoop;
        private readonly Func<int> _times;
        private readonly Action<int> _setTimes;
        private readonly SkillStep[] _loopSteps;

        public EnergyTimesWhileSkillStep(
            int paidEnergyPerLoop,
            Func<int> times,
            Action<int> setTimes,
            SkillStep[] loopSteps
        )
        {
            _paidEnergyPerLoop = Math.Max(0, paidEnergyPerLoop);
            _times = times;
            _setTimes = setTimes;
            _loopSteps = loopSteps ?? Array.Empty<SkillStep>();
        }

        public override async Task Execute(Skill skill)
        {
            if (ShouldAbortStepExecution(skill))
                return;

            if (_loopSteps.Length == 0)
                return;

            if (_paidEnergyPerLoop <= 0 && _times == null)
                return;

            if (_paidEnergyPerLoop > 0 && _times == null)
            {
                int loopCount = skill.GetXEnergyLoopCount(_paidEnergyPerLoop);
                for (int loopIndex = 0; loopIndex < loopCount; loopIndex++)
                {
                    if (ShouldAbortStepExecution(skill))
                        return;

                    for (int i = 0; i < _loopSteps.Length; i++)
                    {
                        if (ShouldAbortStepExecution(skill))
                            return;
                        await _loopSteps[i].Execute(skill);
                        if (ShouldAbortStepExecution(skill))
                            return;
                    }
                }

                return;
            }

            // Delegate-only times mode: snapshot once, then consume locally.
            // This keeps WhileStep compatible with read-only Func<int> providers.
            if (_times != null && _setTimes == null)
            {
                int remainingTimes = Math.Max(0, _times());
                while (remainingTimes > 0)
                {
                    if (ShouldAbortStepExecution(skill))
                        return;

                    remainingTimes--;
                    for (int i = 0; i < _loopSteps.Length; i++)
                    {
                        if (ShouldAbortStepExecution(skill))
                            return;
                        await _loopSteps[i].Execute(skill);
                        if (ShouldAbortStepExecution(skill))
                            return;
                    }
                }

                return;
            }

            while (TryPassTimesGate(skill, _times, _setTimes))
            {
                if (ShouldAbortStepExecution(skill))
                    return;

                for (int i = 0; i < _loopSteps.Length; i++)
                {
                    if (ShouldAbortStepExecution(skill))
                        return;
                    await _loopSteps[i].Execute(skill);
                    if (ShouldAbortStepExecution(skill))
                        return;
                }
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            bool hasTimes = _times != null;
            bool consumesTimes = hasTimes && _setTimes != null;
            int currentTimes = hasTimes ? Math.Max(0, _times?.Invoke() ?? 0) : 0;
            if (_paidEnergyPerLoop <= 0 && !hasTimes)
            {
                yield return I18n.Tr("skill.step.loop.invalid", "循环条件无效，不执行。");
                yield break;
            }

            if (hasTimes)
            {
                if (consumesTimes)
                {
                    yield return I18n.Format(
                        "skill.step.loop.consume_times",
                        "循环每轮次数-1(当前次数：{count})。",
                        ("count", currentTimes)
                    );
                }
                else
                {
                    yield return I18n.Format(
                        "skill.step.loop.current_times",
                        "循环当前次数：{count}。",
                        ("count", currentTimes)
                    );
                }
            }
            else
            {
                if (skill?.UsesXEnergyCost == true && _paidEnergyPerLoop == 1)
                    yield return I18n.Tr("skill.step.loop.x_times", "循环x次：");
                else
                    yield return I18n.Format(
                        "skill.step.loop.energy_cost",
                        "按本技能支付能量每{cost}点循环1次：",
                        ("cost", _paidEnergyPerLoop)
                    );
            }

            for (int i = 0; i < _loopSteps.Length; i++)
            {
                IEnumerable<string> lines = _loopSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    yield return I18n.Format(
                        "skill.step.loop.prefix",
                        "循环：{line}",
                        ("line", line)
                    );
                }
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (!CanPassEnergyTimesLoop(skill, _paidEnergyPerLoop, _times))
                return Array.Empty<Character>();

            return CollectPreviewTargets(skill, _loopSteps);
        }

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            if (!CanPassEnergyTimesLoop(skill, _paidEnergyPerLoop, _times))
                return Array.Empty<PreviewDamageEntry>();

            IEnumerable<PreviewDamageEntry> entries = CollectPreviewDamage(
                skill,
                _loopSteps,
                context
            );
            if (_times != null || _paidEnergyPerLoop <= 0)
                return entries;

            int loopCount = skill.GetXEnergyLoopCount(_paidEnergyPerLoop);
            if (loopCount <= 1)
                return entries;

            return entries.Select(entry => new PreviewDamageEntry(
                entry.Target,
                entry.Damage * loopCount,
                entry.HitCount * loopCount
            ));
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            if (!CanPassEnergyTimesLoop(skill, _paidEnergyPerLoop, _times))
                return Array.Empty<Character>();

            return CollectPreviewHostileDebuffTargets(skill, _loopSteps);
        }
    }

    private static bool CanPassTimesGate(Skill skill, Func<int> times)
    {
        if (ShouldAbortStepExecution(skill) || skill?.OwnerCharater == null)
            return false;

        bool hasTimes = times != null;
        int currentTimes = hasTimes ? Math.Max(0, times()) : 0;
        if (hasTimes && currentTimes <= 0)
            return false;

        return true;
    }

    private static bool CanPassEnergyTimesLoop(Skill skill, int paidEnergyPerLoop, Func<int> times)
    {
        if (!CanPassTimesGate(skill, times))
            return false;

        if (times == null && paidEnergyPerLoop > 0)
            return skill.GetXEnergyLoopCount(paidEnergyPerLoop) > 0;

        return true;
    }

    private static bool TryPassTimesGate(Skill skill, Func<int> times, Action<int> setTimes)
    {
        if (ShouldAbortStepExecution(skill) || skill?.OwnerCharater == null)
            return false;

        bool hasTimes = times != null;
        int currentTimes = hasTimes ? Math.Max(0, times()) : 0;
        if (hasTimes && currentTimes <= 0)
            return false;

        if (hasTimes)
        {
            if (setTimes == null)
                return false;

            setTimes(Math.Max(0, currentTimes - 1));
        }

        return true;
    }

    private static async Task AttackTargetTimes(
        Skill skill,
        Character target,
        int damage,
        int times
    )
    {
        if (ShouldAbortStepExecution(skill) || target == null || IsDummyTarget(skill, target))
            return;

        int totalHits = Math.Max(1, times);
        int clamped = Math.Clamp(
            AttackBuff.ApplyOutgoingDamageModifiers(
                skill.OwnerCharater,
                damage,
                target,
                consumeStacks: true
            ),
            0,
            9999
        );

        for (int i = 0; i < totalHits; i++)
        {
            if (ShouldAbortStepExecution(skill) || target.State != Character.CharacterState.Normal)
                return;

            if (i == 0)
            {
                await skill.AttackAnimation(target);
            }
            else
            {
                var attackFx = AttackScene.Instantiate() as AttackEffect;
                target.AddChild(attackFx);
                attackFx.AnimationPlayer0.Play("Attack1");
                attackFx.GlobalPosition = target.GlobalPosition;
            }

            if (ShouldAbortStepExecution(skill))
                return;

            await target.GetHurt(
                clamped,
                skill?.OwnerCharater,
                damageKind: Character.DamageKind.Attack
            );

            if (ShouldAbortStepExecution(skill))
                return;

            if (i < totalHits - 1)
                await Task.Delay(100);
        }
    }

    private sealed class ConditionSkillStep : SkillStep
    {
        private readonly Func<bool> _condition;
        private readonly string _conditionDescription;
        private readonly SkillStep[] _onPassSteps;

        public ConditionSkillStep(
            Func<bool> condition,
            string conditionDescription,
            SkillStep[] onPassSteps
        )
        {
            _condition = condition;
            _conditionDescription = conditionDescription;
            _onPassSteps = onPassSteps ?? Array.Empty<SkillStep>();
        }

        public override async Task Execute(Skill skill)
        {
            if (ShouldAbortStepExecution(skill))
                return;

            if (_onPassSteps.Length == 0)
                return;

            bool pass = _condition?.Invoke() == true;
            if (!pass)
                return;

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                if (ShouldAbortStepExecution(skill))
                    return;
                await _onPassSteps[i].Execute(skill);
                if (ShouldAbortStepExecution(skill))
                    return;
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_onPassSteps.Length == 0)
                yield break;

            if (!string.IsNullOrWhiteSpace(_conditionDescription))
                yield return I18n.Format(
                    "skill.step.condition.if",
                    "若{condition}：",
                    ("condition", _conditionDescription)
                );

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return I18n.Format(
                        "skill.step.effect_prefix",
                        "生效：{line}",
                        ("line", line)
                    );
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (_condition?.Invoke() != true)
                return Array.Empty<Character>();

            return CollectPreviewTargets(skill, _onPassSteps);
        }

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            if (_condition?.Invoke() != true)
                return Array.Empty<PreviewDamageEntry>();

            return CollectPreviewDamage(skill, _onPassSteps, context);
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            if (_condition?.Invoke() != true)
                return Array.Empty<Character>();

            return CollectPreviewHostileDebuffTargets(skill, _onPassSteps);
        }
    }

    private sealed class BranchSkillStep : SkillStep
    {
        private readonly Func<bool> _condition;
        private readonly string _conditionDescription;
        private readonly SkillStep[] _onPassSteps;
        private readonly SkillStep[] _onFailSteps;

        public BranchSkillStep(
            Func<bool> condition,
            string conditionDescription,
            SkillStep[] onPassSteps,
            SkillStep[] onFailSteps
        )
        {
            _condition = condition;
            _conditionDescription = conditionDescription;
            _onPassSteps = onPassSteps ?? Array.Empty<SkillStep>();
            _onFailSteps = onFailSteps ?? Array.Empty<SkillStep>();
        }

        public override async Task Execute(Skill skill)
        {
            if (ShouldAbortStepExecution(skill))
                return;

            SkillStep[] activeSteps = _condition?.Invoke() == true ? _onPassSteps : _onFailSteps;
            if (activeSteps.Length == 0)
                return;

            for (int i = 0; i < activeSteps.Length; i++)
            {
                if (ShouldAbortStepExecution(skill))
                    return;
                await activeSteps[i].Execute(skill);
                if (ShouldAbortStepExecution(skill))
                    return;
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            bool hasPass = _onPassSteps.Length > 0;
            bool hasFail = _onFailSteps.Length > 0;
            if (!hasPass && !hasFail)
                yield break;

            if (!string.IsNullOrWhiteSpace(_conditionDescription))
                yield return I18n.Format(
                    "skill.step.condition.if",
                    "若{condition}：",
                    ("condition", _conditionDescription)
                );

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return I18n.Format(
                        "skill.step.effect_prefix",
                        "生效：{line}",
                        ("line", line)
                    );
            }

            if (!hasFail)
                yield break;

            yield return I18n.Tr("skill.step.condition.else", "否则：");
            for (int i = 0; i < _onFailSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onFailSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return I18n.Format(
                        "skill.step.effect_prefix",
                        "生效：{line}",
                        ("line", line)
                    );
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            SkillStep[] activeSteps = _condition?.Invoke() == true ? _onPassSteps : _onFailSteps;
            return CollectPreviewTargets(skill, activeSteps);
        }

        public override IEnumerable<PreviewDamageEntry> PreviewDamage(
            Skill skill,
            PreviewDamageContext context
        )
        {
            SkillStep[] activeSteps = _condition?.Invoke() == true ? _onPassSteps : _onFailSteps;
            return CollectPreviewDamage(skill, activeSteps, context);
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            SkillStep[] activeSteps = _condition?.Invoke() == true ? _onPassSteps : _onFailSteps;
            return CollectPreviewHostileDebuffTargets(skill, activeSteps);
        }
    }

    private sealed class TextSkillStep : SkillStep
    {
        private readonly string _line;

        public TextSkillStep(string line)
        {
            _line = line;
        }

        public override Task Execute(Skill skill)
        {
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!string.IsNullOrWhiteSpace(_line))
                yield return _line;
        }
    }

    private sealed class DelegateSkillStep : SkillStep
    {
        private readonly Func<Skill, Task> _execute;
        private readonly Func<Skill, IEnumerable<string>> _describe;

        public DelegateSkillStep(
            Func<Skill, Task> execute,
            Func<Skill, IEnumerable<string>> describe
        )
        {
            _execute = execute;
            _describe = describe;
        }

        public override Task Execute(Skill skill)
        {
            if (_execute == null)
                return Task.CompletedTask;

            return _execute(skill) ?? Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            return _describe?.Invoke(skill) ?? Array.Empty<string>();
        }
    }
}
