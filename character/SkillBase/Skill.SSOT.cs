using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

// SSOT Step Catalog
// - Target Rule: 未显式指定目标策略时，默认按 Chosetarget1 顺序选取目标，描述中不额外说明。
// - AttackPrimaryStep: 单段攻击（Attack1）。
// - DoubleStrikeStep: 二段攻击（Attack2）。
// - AoeDamageStep: 群体伤害（按 Chosetarget1 顺序命中前N名）。
// - ApplyBuffHostile: 对敌方施加Buff（buffName/stacks/maxTargets；maxTargets=9表示全阵）。
// - SummonStep: 召唤召唤物（slotSelector/packedScene；0最前空位，9最后空位，负数从自身前第N位起向前找空位，正数从自身后第N位起向后找空位）。
// - ApplyBuffSummonsStep: 对自身召唤物施加Buff（buffName/stacks/count；正数前N个，负数后N个，0全部）。
// - ModifySummonPropertyStep: 调整自身召唤物属性（type/value/count；正数前N个，负数后N个，0全部；value正增负减）。
// - BlockSummonsStep: 给予自身召唤物格挡（baseBlock/count/survivabilityMultiplier；count规则同ApplyBuffSummonsStep）。
// - HealSummonsStep: 治疗自身召唤物（baseHeal/count；count规则同ApplyBuffSummonsStep）。
// - LowerTargetPropertyStep: 下降目标属性（target 支持默认目标规则 / 已储存目标；maxTargets 仅默认目标规则生效；value 正数表示下降量）。
// - TargetReference: 统一友方目标选择（相对位 / 绝对位 / 已储存目标）。
// - ModifyPropertyStep: 调整友方属性（target 支持相对位 / 绝对位 / 已储存目标；value正增负减）。
// - ApplyBuffFriendly: 对友方施加Buff（target 支持相对位 / 绝对位 / 已储存目标）。
// - HealStep: 对友方治疗（target 支持相对位 / 绝对位 / 已储存目标；可储存目标）。
// - HurtFriendly: 对友方造成伤害（damage/index/all）。
// - EnergyStep: 改变自身或友方目标能量（target 支持相对位 / 绝对位 / 已储存目标）。
// - BlockStep: 相对位友方获得格挡（0自己、-1前一位、+1后一位...；可选对自己不生效）。
// - CarryRelativeAllyStep: 连携相对位队友释放指定技能（+n下n位，-n上n位；index:0攻击/1生存/2特殊）。
// - SwapPositionFriendlyStep: 交换两个相对位队友的位置（0自己；交换PositionIndex并同步出手顺序）。
// - EnergyTimesGateStep: 能量+次数联合门槛（满足则消耗能量并次数-1，并执行生效体；不再阻断后续step）。
// - EnergyTimesWhileStep: while循环（按EnergyTimesGate条件判定；条件满足时循环执行循环体step）。
// - ConditionStep: 条件执行（condition/steps/conditionDescription）。
// - BranchStep: 条件分支（condition/onPassSteps/onFailSteps/conditionDescription）。
// - TextStep: 仅描述文本（不执行效果）。
// - CustomStep: 自定义执行/描述兜底步骤。
public partial class Skill
{
    private SkillPlan _cachedPlan;
    private bool _stopRemainingPlanExecution;
    private readonly Dictionary<string, Character> _storedTargets = new();

    public enum AbsoluteFriendlySelector
    {
        FrontMost,
        BackMost,
        LowestLife,
        All,
    }

    protected enum TargetReferenceKind
    {
        DefaultRule,
        Relative,
        Absolute,
        Stored,
    }

    protected readonly struct TargetReference
    {
        public TargetReferenceKind Kind { get; }
        public AbsoluteFriendlySelector AbsoluteSelector { get; }
        public int RelativeIndex { get; }
        public string StoredKey { get; }
        public bool UsesStoredTarget => Kind == TargetReferenceKind.Stored;

        private TargetReference(
            TargetReferenceKind kind,
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

        public static TargetReference FromRelative(int relativeIndex) =>
            new TargetReference(TargetReferenceKind.Relative, default, relativeIndex, null);

        public static TargetReference FromAbsolute(AbsoluteFriendlySelector selector) =>
            new TargetReference(TargetReferenceKind.Absolute, selector, 0, null);

        public static TargetReference FromStored(string storedKey) =>
            new TargetReference(TargetReferenceKind.Stored, default, 0, storedKey);
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

    protected static TargetReference RelativeTarget(int relativeIndex = 0) =>
        TargetReference.FromRelative(relativeIndex);

    protected static TargetReference AbsoluteTarget(AbsoluteFriendlySelector selector) =>
        TargetReference.FromAbsolute(selector);

    protected static TargetReference StoredTarget(string storedKey) =>
        TargetReference.FromStored(storedKey);

    private void StoreTarget(string storedKey, Character target)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return;

        if (target == null)
            _storedTargets.Remove(storedKey);
        else
            _storedTargets[storedKey] = target;
    }

    protected Character GetStoredTarget(string storedKey)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return null;

        _storedTargets.TryGetValue(storedKey, out Character cached);
        return cached;
    }

    private Character[] ResolveHostileTargets(
        TargetReference target,
        int maxTargets,
        bool byBehindRow
    )
    {
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

        if (target.Kind != TargetReferenceKind.DefaultRule)
            return Array.Empty<Character>();

        var targets = ChosetargetByOrder(byBehindRow: byBehindRow);
        int count = maxTargets <= 0 ? targets.Length : Math.Min(maxTargets, targets.Length);
        if (count <= 0)
            return Array.Empty<Character>();

        return targets.Take(count).Where(x => x != null).ToArray();
    }

    private Character ResolveHostileTarget(TargetReference target, bool byBehindRow) =>
        ResolveHostileTargets(target, 1, byBehindRow).FirstOrDefault();

    private Character[] ResolveFriendlyTargets(
        TargetReference target,
        bool dyingFilter,
        bool preferNonFull = false,
        bool rebirth = false,
        bool includeSummonsWhenAll = false
    )
    {
        return target.Kind switch
        {
            TargetReferenceKind.Stored => SingleTargetArray(GetStoredTarget(target.StoredKey)),
            TargetReferenceKind.Relative => SingleTargetArray(
                GetAllyByRelative(target.RelativeIndex, dyingFilter: dyingFilter)
            ),
            TargetReferenceKind.Absolute => SelectFriendlyTargets(
                this,
                target.AbsoluteSelector,
                dyingFilter,
                preferNonFull,
                rebirth,
                includeSummonsWhenAll
            ),
            _ => SingleTargetArray(GetAllyByRelative(0, dyingFilter: dyingFilter)),
        };
    }

    private Character ResolveFriendlyTarget(
        TargetReference target,
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

    private static IEnumerable<Character> CollectPreviewTargets(
        Skill skill,
        IEnumerable<SkillStep> steps
    )
    {
        if (steps == null)
            return Array.Empty<Character>();

        return steps.SelectMany(step => step?.PreviewTargets(skill) ?? Array.Empty<Character>());
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
            _skill._storedTargets.Clear();
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_skill._stopRemainingPlanExecution)
                    break;
                await _steps[i].Execute(_skill);
            }
            _skill._stopRemainingPlanExecution = false;
            _skill._storedTargets.Clear();
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
            _skill._storedTargets.Clear();

            Character dummy = _skill.OwnerCharater?.BattleNode?.dummy;
            Character[] targets = CollectPreviewTargets(_skill, _steps)
                .Where(target =>
                    target != null
                    && target != dummy
                    && target.State == Character.CharacterState.Normal
                )
                .Distinct()
                .ToArray();

            _skill._storedTargets.Clear();
            return targets;
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
    }

    protected SkillStep AttackPrimaryStep(
        int baseDamage = 0,
        int powerMultiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        bool byBehindRow = false,
        TargetReference target = default,
        string storeAs = null
    ) =>
        AttackPrimaryStepCore(
            baseDamage,
            powerMultiplier,
            prefix,
            suffix,
            clampMax,
            byBehindRow,
            target,
            storeAs,
            null
        );

    protected SkillStep AttackPrimaryStep(
        Func<Skill, int> baseDamage,
        int powerMultiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        bool byBehindRow = false,
        TargetReference target = default,
        string storeAs = null
    ) =>
        AttackPrimaryStepCore(
            0,
            powerMultiplier,
            prefix,
            suffix,
            clampMax,
            byBehindRow,
            target,
            storeAs,
            baseDamage
        );

    private SkillStep AttackPrimaryStepCore(
        int baseDamage,
        int powerMultiplier,
        string prefix,
        string suffix,
        int clampMax,
        bool byBehindRow,
        TargetReference target,
        string storeAs,
        Func<Skill, int> baseDamageProvider
    ) =>
        new AttackPrimarySkillStep(
            baseDamage,
            powerMultiplier,
            prefix,
            suffix,
            clampMax,
            byBehindRow,
            target,
            storeAs,
            baseDamageProvider
        );

    protected SkillStep DoubleStrikeStep(
        int baseDamage = 0,
        int powerMultiplier = 1,
        string prefix = "每段造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        bool includeTwoHitText = true,
        bool byBehindRow = false,
        TargetReference target = default,
        string storeAs = null
    ) =>
        DoubleStrikeStepCore(
            baseDamage,
            powerMultiplier,
            prefix,
            suffix,
            clampMax,
            includeTwoHitText,
            byBehindRow,
            target,
            storeAs,
            null
        );

    protected SkillStep DoubleStrikeStep(
        Func<Skill, int> baseDamage,
        int powerMultiplier = 1,
        string prefix = "每段造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        bool includeTwoHitText = true,
        bool byBehindRow = false,
        TargetReference target = default,
        string storeAs = null
    ) =>
        DoubleStrikeStepCore(
            0,
            powerMultiplier,
            prefix,
            suffix,
            clampMax,
            includeTwoHitText,
            byBehindRow,
            target,
            storeAs,
            baseDamage
        );

    private SkillStep DoubleStrikeStepCore(
        int baseDamage,
        int powerMultiplier,
        string prefix,
        string suffix,
        int clampMax,
        bool includeTwoHitText,
        bool byBehindRow,
        TargetReference target,
        string storeAs,
        Func<Skill, int> baseDamageProvider
    ) =>
        new DoubleStrikeSkillStep(
            baseDamage,
            powerMultiplier,
            prefix,
            suffix,
            clampMax,
            includeTwoHitText,
            byBehindRow,
            target,
            storeAs,
            baseDamageProvider
        );

    protected SkillStep AoeDamageStep(
        int baseDamage = 0,
        int powerMultiplier = 1,
        int maxTargets = 0,
        int times = 1,
        int clampMax = 9999,
        bool byBehindRow = false
    ) =>
        AoeDamageStepCore(
            baseDamage,
            powerMultiplier,
            maxTargets,
            times,
            clampMax,
            byBehindRow,
            null
        );

    protected SkillStep AoeDamageStep(
        Func<Skill, int> baseDamage,
        int powerMultiplier = 1,
        int maxTargets = 0,
        int times = 1,
        int clampMax = 9999,
        bool byBehindRow = false
    ) =>
        AoeDamageStepCore(
            0,
            powerMultiplier,
            maxTargets,
            times,
            clampMax,
            byBehindRow,
            baseDamage
        );

    private SkillStep AoeDamageStepCore(
        int baseDamage,
        int powerMultiplier,
        int maxTargets,
        int times,
        int clampMax,
        bool byBehindRow,
        Func<Skill, int> baseDamageProvider
    ) =>
        new AoeDamageSkillStep(
            baseDamage,
            powerMultiplier,
            maxTargets,
            times,
            clampMax,
            byBehindRow,
            baseDamageProvider
        );

    protected SkillStep ApplyBuffHostile(
        Buff.BuffName buffName,
        int stacks,
        int maxTargets = 1,
        bool byBehindRow = false
    ) => new ApplyBuffHostileSkillStep(buffName, stacks, maxTargets, byBehindRow);

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

    protected SkillStep ModifySummonPropertyStep(PropertyType type, int value, int count = 0) =>
        new ModifySummonPropertySkillStep(type, value, count);

    protected SkillStep BlockSummonsStep(
        int baseBlock = 0,
        int count = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999
    ) =>
        BlockSummonsStepCore(baseBlock, count, survivabilityMultiplier, clampMax, null);

    protected SkillStep BlockSummonsStep(
        Func<Skill, int> baseBlock,
        int count = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999
    ) => BlockSummonsStepCore(0, count, survivabilityMultiplier, clampMax, baseBlock);

    private SkillStep BlockSummonsStepCore(
        int baseBlock,
        int count,
        int survivabilityMultiplier,
        int clampMax,
        Func<Skill, int> baseBlockProvider
    ) =>
        new BlockSummonsSkillStep(
            baseBlock,
            survivabilityMultiplier,
            count,
            clampMax,
            baseBlockProvider
        );

    protected SkillStep HealSummonsStep(
        int baseHeal = 0,
        int count = 0,
        int clampMax = 999
    ) => HealSummonsStepCore(baseHeal, count, clampMax, null);

    protected SkillStep HealSummonsStep(
        Func<Skill, int> baseHeal,
        int count = 0,
        int clampMax = 999
    ) => HealSummonsStepCore(0, count, clampMax, baseHeal);

    private SkillStep HealSummonsStepCore(
        int baseHeal,
        int count,
        int clampMax,
        Func<Skill, int> baseHealProvider
    ) => new HealSummonsSkillStep(baseHeal, count, clampMax, baseHealProvider);

    protected SkillStep LowerTargetPropertyStep(
        PropertyType type,
        int value,
        int maxTargets = 1,
        bool permanent = false,
        bool byBehindRow = false
    ) => LowerTargetPropertyStep(type, value, default, maxTargets, permanent, byBehindRow);

    protected SkillStep LowerTargetPropertyStep(
        PropertyType type,
        int value,
        TargetReference target,
        bool permanent = false,
        bool byBehindRow = false
    ) => LowerTargetPropertyStep(type, value, target, 1, permanent, byBehindRow);

    protected SkillStep LowerTargetPropertyStep(
        PropertyType type,
        int value,
        TargetReference target,
        int maxTargets,
        bool permanent = false,
        bool byBehindRow = false
    ) => new LowerTargetPropertySkillStep(type, value, target, maxTargets, permanent, byBehindRow);

    protected SkillStep ApplyBuffFriendly(
        Buff.BuffName buffName,
        int stacks,
        TargetReference target,
        bool includeSummonsWhenAll = false
    ) => new ApplyBuffFriendlySkillStep(buffName, stacks, target, includeSummonsWhenAll);

    protected SkillStep HealStep(
        int baseHeal = 0,
        int survivabilityMultiplier = 1,
        TargetReference target = default,
        bool dyingFilter = true,
        bool preferNonFull = false,
        bool rebirth = false,
        int clampMax = 999,
        string descriptionOverride = null,
        string storeAs = null,
        bool includeSummonsWhenAll = false
    ) =>
        HealFriendlyCore(
            baseHeal,
            survivabilityMultiplier,
            target,
            dyingFilter,
            preferNonFull,
            rebirth,
            clampMax,
            null,
            descriptionOverride,
            storeAs,
            includeSummonsWhenAll
        );

    protected SkillStep HealStep(
        Func<Skill, int> baseHeal,
        int survivabilityMultiplier = 1,
        TargetReference target = default,
        bool dyingFilter = true,
        bool preferNonFull = false,
        bool rebirth = false,
        int clampMax = 999,
        string descriptionOverride = null,
        string storeAs = null,
        bool includeSummonsWhenAll = false
    ) =>
        HealFriendlyCore(
            0,
            survivabilityMultiplier,
            target,
            dyingFilter,
            preferNonFull,
            rebirth,
            clampMax,
            baseHeal,
            descriptionOverride,
            storeAs,
            includeSummonsWhenAll
        );

    protected SkillStep HealFriendlyRelative(
        int baseHeal = 0,
        int survivabilityMultiplier = 1,
        int index = 0,
        bool dyingFilter = true,
        bool rebirth = false,
        int clampMax = 999,
        string descriptionOverride = null
    ) =>
        HealStep(
            baseHeal,
            survivabilityMultiplier,
            RelativeTarget(index),
            dyingFilter,
            preferNonFull: false,
            rebirth,
            clampMax,
            descriptionOverride
        );

    protected SkillStep HealFriendlyRelative(
        Func<Skill, int> baseHeal,
        int survivabilityMultiplier = 1,
        int index = 0,
        bool dyingFilter = true,
        bool rebirth = false,
        int clampMax = 999,
        string descriptionOverride = null
    ) =>
        HealStep(
            baseHeal,
            survivabilityMultiplier,
            RelativeTarget(index),
            dyingFilter,
            preferNonFull: false,
            rebirth,
            clampMax,
            descriptionOverride
        );

    protected SkillStep HealFriendlyAbsolute(
        int baseHeal = 0,
        int survivabilityMultiplier = 1,
        AbsoluteFriendlySelector selector = AbsoluteFriendlySelector.FrontMost,
        bool preferNonFull = true,
        bool rebirth = false,
        int clampMax = 999,
        string storeAs = null
    ) =>
        HealStep(
            baseHeal,
            survivabilityMultiplier,
            AbsoluteTarget(selector),
            dyingFilter: false,
            preferNonFull,
            rebirth,
            clampMax,
            storeAs: storeAs,
            includeSummonsWhenAll: selector == AbsoluteFriendlySelector.All
        );

    protected SkillStep HealFriendlyAbsolute(
        Func<Skill, int> baseHeal,
        int survivabilityMultiplier = 1,
        AbsoluteFriendlySelector selector = AbsoluteFriendlySelector.FrontMost,
        bool preferNonFull = true,
        bool rebirth = false,
        int clampMax = 999,
        string storeAs = null
    ) =>
        HealStep(
            baseHeal,
            survivabilityMultiplier,
            AbsoluteTarget(selector),
            dyingFilter: false,
            preferNonFull,
            rebirth,
            clampMax,
            storeAs: storeAs,
            includeSummonsWhenAll: selector == AbsoluteFriendlySelector.All
        );

    private SkillStep HealFriendlyCore(
        int baseHeal,
        int survivabilityMultiplier,
        TargetReference target,
        bool dyingFilter,
        bool preferNonFull,
        bool rebirth,
        int clampMax,
        Func<Skill, int> baseHealProvider,
        string descriptionOverride,
        string storeAs,
        bool includeSummonsWhenAll
    ) =>
        new HealFriendlySkillStep(
            baseHeal,
            survivabilityMultiplier,
            target,
            dyingFilter,
            preferNonFull,
            rebirth,
            clampMax,
            baseHealProvider,
            descriptionOverride,
            storeAs,
            includeSummonsWhenAll
        );

    protected SkillStep HurtFriendly(int damage, int index = 0, bool all = false) =>
        new HurtFriendlySkillStep(damage, index, all);

    protected SkillStep EnergyStep(int delta) => new EnergySkillStep(delta);

    protected SkillStep EnergyStep(
        int delta,
        TargetReference target
    ) => new EnergySkillStep(delta, target);

    protected SkillStep ModifyPropertyStep(
        PropertyType type,
        int value,
        int index = 0
    ) => ModifyPropertyStep(type, value, RelativeTarget(index));

    protected SkillStep ModifyPropertyStep(
        PropertyType type,
        int value,
        TargetReference target,
        bool includeSummonsWhenAll = false
    ) => new ModifyFriendlyPropertySkillStep(type, value, target, includeSummonsWhenAll);

    protected SkillStep BlockStep(
        int relativeIndex = 0,
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999,
        bool excludeSelf = false,
        bool describe = true,
        string descriptionPrefix = null
    ) =>
        BlockStepCore(
            relativeIndex,
            baseBlock,
            survivabilityMultiplier,
            clampMax,
            excludeSelf,
            describe,
            descriptionPrefix,
            null
        );

    protected SkillStep BlockStep(
        int relativeIndex,
        Func<Skill, int> baseBlock,
        int survivabilityMultiplier = 1,
        int clampMax = 999,
        bool excludeSelf = false,
        bool describe = true,
        string descriptionPrefix = null
    ) =>
        BlockStepCore(
            relativeIndex,
            0,
            survivabilityMultiplier,
            clampMax,
            excludeSelf,
            describe,
            descriptionPrefix,
            baseBlock
        );

    private SkillStep BlockStepCore(
        int relativeIndex,
        int baseBlock,
        int survivabilityMultiplier,
        int clampMax,
        bool excludeSelf,
        bool describe,
        string descriptionPrefix,
        Func<Skill, int> baseBlockProvider
    ) =>
        new BlockFriendlyByRelativeSkillStep(
            relativeIndex,
            baseBlock,
            survivabilityMultiplier,
            clampMax,
            excludeSelf,
            describe,
            descriptionPrefix,
            baseBlockProvider
        );

    protected SkillStep CarryRelativeAllyStep(
        int relativeIndex,
        int skillIndex,
        bool describe = true,
        string descriptionLine = null
    ) =>
        new CarryRelativeAllySkillStepImpl(
            relativeIndex,
            skillIndex,
            describe,
            descriptionLine
        );

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

    protected SkillStep EnergyTimesGateStep(
        int energyCost,
        Func<int> times = null,
        Action<int> setTimes = null,
        params SkillStep[] onPassSteps
    ) => new EnergyTimesGateSkillStep(energyCost, times, setTimes, onPassSteps);

    protected SkillStep EnergyTimesGateStep(
        int energyCost,
        int times,
        SkillStep firstOnPassStep,
        [CallerArgumentExpression("times")] string timesMemberExpression = null,
        params SkillStep[] additionalOnPassSteps
    )
    {
        var (getTimes, setTimes) = ResolveEnergyTimesMember(timesMemberExpression);
        SkillStep[] onPassSteps = firstOnPassStep == null
            ? additionalOnPassSteps ?? Array.Empty<SkillStep>()
            : [firstOnPassStep, .. (additionalOnPassSteps ?? Array.Empty<SkillStep>())];
        return new EnergyTimesGateSkillStep(energyCost, getTimes, setTimes, onPassSteps);
    }

    protected SkillStep EnergyTimesWhileStep(
        int energyCost,
        Func<int> times = null,
        Action<int> setTimes = null,
        params SkillStep[] loopSteps
    ) => new EnergyTimesWhileSkillStep(energyCost, times, setTimes, loopSteps);

    private (Func<int> GetTimes, Action<int> SetTimes) ResolveEnergyTimesMember(
        string memberExpression
    )
    {
        string memberName = NormalizeEnergyTimesMemberName(memberExpression);
        if (string.IsNullOrWhiteSpace(memberName))
        {
            throw new InvalidOperationException("次数成员名不能为空。");
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
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

    private sealed class AttackPrimarySkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly Func<Skill, int> _baseDamageProvider;
        private readonly int _powerMultiplier;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _clampMax;
        private readonly bool _byBehindRow;
        private readonly TargetReference _target;
        private readonly string _storeAs;

        public AttackPrimarySkillStep(
            int baseDamage,
            int powerMultiplier,
            string prefix,
            string suffix,
            int clampMax,
            bool byBehindRow,
            TargetReference target,
            string storeAs,
            Func<Skill, int> baseDamageProvider
        )
        {
            _baseDamage = baseDamage;
            _baseDamageProvider = baseDamageProvider;
            _powerMultiplier = powerMultiplier;
            _prefix = prefix;
            _suffix = suffix;
            _clampMax = clampMax;
            _byBehindRow = byBehindRow;
            _target = target;
            _storeAs = storeAs;
        }

        public override async Task Execute(Skill skill)
        {
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageProvider);
            int damage = skill.DamageFromPower(baseDamage, _powerMultiplier, _clampMax);
            if (_target.Kind == TargetReferenceKind.DefaultRule && string.IsNullOrWhiteSpace(_storeAs))
            {
                await skill.Attack1(damage, _byBehindRow);
                return;
            }

            Character target = skill.ResolveHostileTarget(_target, _byBehindRow);
            skill.StoreTarget(_storeAs, target);
            if (target == null)
                return;
            await AttackTargetOnce(skill, target, damage);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageProvider);
            string line = skill.DamageLine(
                baseDamage,
                _powerMultiplier,
                _prefix,
                _suffix,
                _clampMax
            );
            if (_byBehindRow && !line.Contains("后排", StringComparison.Ordinal))
                line = $"对后排目标{line}";

            yield return line;
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (_target.Kind == TargetReferenceKind.DefaultRule && string.IsNullOrWhiteSpace(_storeAs))
                return PreviewHostileTargets(skill, maxTargets: 1, byBehindRow: _byBehindRow);

            Character target = skill.ResolveHostileTarget(_target, _byBehindRow);
            skill.StoreTarget(_storeAs, target);
            return SingleTargetArray(target);
        }
    }

    private sealed class DoubleStrikeSkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly Func<Skill, int> _baseDamageProvider;
        private readonly int _powerMultiplier;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _clampMax;
        private readonly bool _includeTwoHitText;
        private readonly bool _byBehindRow;
        private readonly TargetReference _target;
        private readonly string _storeAs;

        public DoubleStrikeSkillStep(
            int baseDamage,
            int powerMultiplier,
            string prefix,
            string suffix,
            int clampMax,
            bool includeTwoHitText,
            bool byBehindRow,
            TargetReference target,
            string storeAs,
            Func<Skill, int> baseDamageProvider
        )
        {
            _baseDamage = baseDamage;
            _baseDamageProvider = baseDamageProvider;
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
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageProvider);
            int damage = skill.DamageFromPower(baseDamage, _powerMultiplier, _clampMax);
            if (_target.Kind == TargetReferenceKind.DefaultRule && string.IsNullOrWhiteSpace(_storeAs))
            {
                await skill.Attack2(damage, _byBehindRow);
                return;
            }

            Character target = skill.ResolveHostileTarget(_target, _byBehindRow);
            skill.StoreTarget(_storeAs, target);
            if (target == null)
                return;
            await AttackTargetDoubleStrike(skill, target, damage);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_includeTwoHitText)
                yield return "二段攻击。";

            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageProvider);
            string line = skill.DamageLine(
                baseDamage,
                _powerMultiplier,
                _prefix,
                _suffix,
                _clampMax
            );
            if (_byBehindRow && !line.Contains("后排", StringComparison.Ordinal))
                line = $"对后排目标{line}";

            yield return line;
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (_target.Kind == TargetReferenceKind.DefaultRule && string.IsNullOrWhiteSpace(_storeAs))
                return PreviewHostileTargets(skill, maxTargets: 1, byBehindRow: _byBehindRow);

            Character target = skill.ResolveHostileTarget(_target, _byBehindRow);
            skill.StoreTarget(_storeAs, target);
            return SingleTargetArray(target);
        }
    }

    private sealed class AoeDamageSkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly Func<Skill, int> _baseDamageProvider;
        private readonly int _powerMultiplier;
        private readonly int _maxTargets;
        private readonly int _times;
        private readonly int _clampMax;
        private readonly bool _byBehindRow;

        public AoeDamageSkillStep(
            int baseDamage,
            int powerMultiplier,
            int maxTargets,
            int times,
            int clampMax,
            bool byBehindRow,
            Func<Skill, int> baseDamageProvider
        )
        {
            _baseDamage = baseDamage;
            _baseDamageProvider = baseDamageProvider;
            _powerMultiplier = powerMultiplier;
            _maxTargets = maxTargets;
            _times = times;
            _clampMax = clampMax;
            _byBehindRow = byBehindRow;
        }

        public override async Task Execute(Skill skill)
        {
            var targets = skill.ChosetargetByOrder(byBehindRow: _byBehindRow);
            int count = _maxTargets <= 0 ? targets.Length : Math.Min(_maxTargets, targets.Length);
            if (count <= 0)
                return;

            int times = Math.Max(1, _times);
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageProvider);
            int damage = skill.DamageFromPower(baseDamage, _powerMultiplier, _clampMax);
            await skill.AOE(damage, count, times, _byBehindRow);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int baseDamage = ResolveStepBaseValue(skill, _baseDamage, _baseDamageProvider);
            string damageText = skill.DamageFromPowerText(baseDamage, _powerMultiplier, _clampMax);
            if (_maxTargets <= 0)
                yield return $"对所有目标造成{damageText}点伤害。";
            else
            {
                string targetText = _byBehindRow ? "后排目标" : "目标";
                yield return $"对至多{_maxTargets}名{targetText}造成{damageText}点伤害。";
            }

            if (_times > 1)
                yield return $"共{_times}段。";
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return PreviewHostileTargets(skill, _maxTargets, _byBehindRow);
        }
    }

    private sealed class LowerTargetPropertySkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly TargetReference _target;
        private readonly int _maxTargets;
        private readonly bool _permanent;
        private readonly bool _byBehindRow;

        public LowerTargetPropertySkillStep(
            PropertyType type,
            int value,
            TargetReference target,
            int maxTargets,
            bool permanent,
            bool byBehindRow
        )
        {
            _type = type;
            _value = value;
            _target = target;
            _maxTargets = maxTargets;
            _permanent = permanent;
            _byBehindRow = byBehindRow;
        }

        public override async Task Execute(Skill skill)
        {
            int loss = Math.Abs(_value);
            if (loss == 0)
                return;

            var targets = skill.ResolveHostileTargets(_target, _maxTargets, _byBehindRow);
            if (targets.Length == 0)
                return;

            for (int i = 0; i < targets.Length; i++)
            {
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
            string targetText = HostileTargetText(_target, _maxTargets, _byBehindRow);

            yield return $"下降{targetText}{loseText}{PermanentTag()}。";
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (Math.Abs(_value) == 0)
                return Array.Empty<Character>();

            return skill.ResolveHostileTargets(_target, _maxTargets, _byBehindRow);
        }

        private string PermanentTag()
        {
            return _permanent ? "（永久）" : string.Empty;
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
            case Buff.BuffName.Stun:
                SkillBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.Pursuit:
                EndActionBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.Invisible:
                StartActionBuff.BuffAdd(buffName, target, stacks, source);
                return true;
            case Buff.BuffName.DebuffImmunity:
            case Buff.BuffName.ExtraPower:
            case Buff.BuffName.ExtraSurvivability:
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

    private static string FriendlyTargetText(TargetReference target)
    {
        return target.Kind switch
        {
            TargetReferenceKind.Stored => target.StoredKey,
            TargetReferenceKind.Relative => RelativeFriendlyTargetText(target.RelativeIndex),
            TargetReferenceKind.Absolute => AbsoluteFriendlySelectorText(target.AbsoluteSelector),
            _ => RelativeFriendlyTargetText(0),
        };
    }

    private static Character[] GetAllHostileWithOrder(Skill skill, bool dyingFilter)
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return Array.Empty<Character>();

        Character[] ordered = skill.OwnerCharater.BattleNode.GetOrderedTeamCharacters(
            !skill.OwnerCharater.IsPlayer,
            includeSummons: true,
            dyingFilter: dyingFilter
        );

        Character[] visible = ordered
            .Where(x =>
                x != null
                && x.StartActionBuffs.Any(b => b.ThisBuffName == Buff.BuffName.Invisible) == false
            )
            .ToArray();

        // Match hostile targeting rules: if any non-invisible targets exist, full-team hostile buffs
        // should not include invisible ones. If everyone is invisible, still allow targeting all.
        return visible.Length > 0 ? visible : ordered;
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

    private static int ResolveStepBaseValue(
        Skill skill,
        int fixedValue,
        Func<Skill, int> valueProvider
    ) => valueProvider?.Invoke(skill) ?? fixedValue;

    private static int ResolveFriendlyHealAmount(
        Skill skill,
        int baseHeal,
        int survivabilityMultiplier,
        int clampMax
    )
    {
        return Math.Clamp(baseHeal, 0, clampMax);
    }

    private static string FriendlyHealAmountText(
        Skill skill,
        int baseHeal,
        int survivabilityMultiplier,
        int clampMax
    ) => ResolveFriendlyHealAmount(skill, baseHeal, survivabilityMultiplier, clampMax).ToString();

    private static string PropertyDeltaActionText(PropertyType type, int value)
    {
        int amount = Math.Abs(value);
        string propertyText = $"{amount}{GetColoredPropertyLabel(type)}";
        return value >= 0 ? $"增加{propertyText}" : $"减少{propertyText}";
    }

    private static string HostileTargetText(
        TargetReference target,
        int maxTargets,
        bool byBehindRow
    )
    {
        if (target.UsesStoredTarget)
            return target.StoredKey;

        if (maxTargets == 1)
            return byBehindRow ? "后排目标" : "目标";
        if (maxTargets <= 0)
            return "所有命中目标";

        return byBehindRow ? $"至多{maxTargets}名后排目标" : $"至多{maxTargets}名目标";
    }

    private static string AbsoluteFriendlySelectorText(AbsoluteFriendlySelector selector)
    {
        return selector switch
        {
            AbsoluteFriendlySelector.FrontMost => "站位最前队友",
            AbsoluteFriendlySelector.BackMost => "站位最后队友",
            AbsoluteFriendlySelector.LowestLife => "生命最少队友",
            AbsoluteFriendlySelector.All => "友方全阵",
            _ => "队友",
        };
    }

    private static string AbsoluteFriendlyPriorityText(bool preferNonFull, bool rebirth)
    {
        if (rebirth && preferNonFull)
            return "优先濒死目标，其次非满血目标";
        if (rebirth)
            return "优先濒死目标";
        if (preferNonFull)
            return "优先非满血目标";
        return null;
    }

    private static Character SelectAbsoluteFriendlyTarget(
        Skill skill,
        AbsoluteFriendlySelector selector,
        bool preferNonFull,
        bool rebirth
    )
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return null;

        Character[] allies = skill.GetAllAllyWithOrder(false).Where(x => x != null).ToArray();
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
        {
            var dying = allies.Where(x => x.State == Character.CharacterState.Dying);
            var dyingTarget = PickBySelector(dying);
            if (dyingTarget != null)
                return dyingTarget;
        }

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
            return "全部召唤物";
        if (count > 0)
            return $"最前{count}个召唤物";

        return $"最后{-count}个召唤物";
    }

    private static string SummonSlotSelectorText(int slotSelector)
    {
        if (slotSelector == 0)
            return "最前空位";
        if (slotSelector == 9)
            return "最后空位";
        if (slotSelector > 0)
            return $"从自身后第{slotSelector}位起向后寻找空位";

        return $"从自身前第{-slotSelector}位起向前寻找空位";
    }

    private sealed class ApplyBuffHostileSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly int _maxTargets;
        private readonly bool _byBehindRow;

        public ApplyBuffHostileSkillStep(
            Buff.BuffName buffName,
            int stacks,
            int maxTargets,
            bool byBehindRow
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _maxTargets = maxTargets;
            _byBehindRow = byBehindRow;
        }

        public override Task Execute(Skill skill)
        {
            Character[] targets;
            int count;
            if (_maxTargets >= 9)
            {
                targets = GetAllHostileWithOrder(skill, dyingFilter: true);
                count = targets.Length;
            }
            else
            {
                targets = skill.ChosetargetByOrder(byBehindRow: _byBehindRow);
                count = _maxTargets <= 0 ? targets.Length : Math.Min(_maxTargets, targets.Length);
            }

            for (int i = 0; i < count; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], _stacks, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string stacksText = BuffStacksText(_buffName, _stacks);
            if (_maxTargets == 1)
            {
                string targetText = _byBehindRow ? "后排目标" : "目标";
                yield return $"使{targetText}获得{stacksText}。";
            }
            else if (_maxTargets >= 9)
                yield return $"使敌方全阵获得{stacksText}。";
            else if (_maxTargets <= 0)
                yield return $"使所有命中目标获得{stacksText}。";
            else
            {
                string targetText = _byBehindRow ? "后排目标" : "目标";
                yield return $"使{targetText}各获得{stacksText}。";
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (_maxTargets >= 9)
                return GetAllHostileWithOrder(skill, dyingFilter: true);

            return PreviewHostileTargets(skill, _maxTargets, _byBehindRow);
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
            yield return $"在{SummonSlotSelectorText(_slotSelector)}召唤1个召唤物。";
        }
    }

    private sealed class ApplyBuffSummonsSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly int _count;

        public ApplyBuffSummonsSkillStep(Buff.BuffName buffName, int stacks, int count)
        {
            _buffName = buffName;
            _stacks = stacks;
            _count = count;
        }

        public override Task Execute(Skill skill)
        {
            SummonCharacter[] targets = SelectOwnedSummons(skill, _count);
            for (int i = 0; i < targets.Length; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], _stacks, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = SummonSelectionText(_count);
            string stacksText = BuffStacksText(_buffName, _stacks);
            yield return $"使{targetText}获得{stacksText}。";
        }
    }

    private sealed class ApplyBuffFriendlySkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly TargetReference _target;
        private readonly bool _includeSummonsWhenAll;

        public ApplyBuffFriendlySkillStep(
            Buff.BuffName buffName,
            int stacks,
            TargetReference target,
            bool includeSummonsWhenAll
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _target = target;
            _includeSummonsWhenAll = includeSummonsWhenAll;
        }

        public override Task Execute(Skill skill)
        {
            Character[] targets = skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
            for (int i = 0; i < targets.Length; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], _stacks, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = FriendlyTargetText(_target);
            string stacksText = BuffStacksText(_buffName, _stacks);
            yield return $"使{targetText}获得{stacksText}。";
        }
    }

    private sealed class HurtFriendlySkillStep : SkillStep
    {
        private readonly int _damage;
        private readonly int _index;
        private readonly bool _all;

        public HurtFriendlySkillStep(int damage, int index, bool all)
        {
            _damage = Math.Max(0, damage);
            _index = index;
            _all = all;
        }

        public override async Task Execute(Skill skill)
        {
            if (_damage <= 0)
                return;

            if (_all)
            {
                var allies = skill
                    .GetAllAllyWithOrder(dyingFilter: true, includeSummons: true)
                    .Where(x => x != null)
                    .ToArray();
                if (allies.Length == 0)
                    return;

                List<Task> tasks = new(allies.Length);
                for (int i = 0; i < allies.Length; i++)
                {
                    tasks.Add(allies[i].GetHurt(_damage, skill?.OwnerCharater));
                }

                await Task.WhenAll(tasks);
                return;
            }

            var target = skill.GetAllyByRelative(_index, dyingFilter: true);
            if (target != null)
                await target.GetHurt(_damage, skill?.OwnerCharater);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_damage <= 0)
                yield break;

            if (_all)
            {
                yield return $"对友方全阵造成{_damage}点伤害。";
                yield break;
            }

            string targetText = RelativeFriendlyTargetText(_index);
            yield return $"对{targetText}造成{_damage}点伤害。";
        }
    }

    private sealed class HealFriendlySkillStep : SkillStep
    {
        private readonly int _baseHeal;
        private readonly Func<Skill, int> _baseHealProvider;
        private readonly int _survivabilityMultiplier;
        private readonly TargetReference _target;
        private readonly bool _dyingFilter;
        private readonly bool _preferNonFull;
        private readonly bool _rebirth;
        private readonly int _clampMax;
        private readonly string _descriptionOverride;
        private readonly string _storeAs;
        private readonly bool _includeSummonsWhenAll;

        public HealFriendlySkillStep(
            int baseHeal,
            int survivabilityMultiplier,
            TargetReference target,
            bool dyingFilter,
            bool preferNonFull,
            bool rebirth,
            int clampMax,
            Func<Skill, int> baseHealProvider,
            string descriptionOverride,
            string storeAs,
            bool includeSummonsWhenAll
        )
        {
            _baseHeal = baseHeal;
            _baseHealProvider = baseHealProvider;
            _survivabilityMultiplier = survivabilityMultiplier;
            _target = target;
            _dyingFilter = dyingFilter;
            _preferNonFull = preferNonFull;
            _rebirth = rebirth;
            _clampMax = clampMax;
            _descriptionOverride = descriptionOverride;
            _storeAs = storeAs;
            _includeSummonsWhenAll = includeSummonsWhenAll;
        }

        public override Task Execute(Skill skill)
        {
            Character[] targets = skill.ResolveFriendlyTargets(
                _target,
                _dyingFilter,
                _preferNonFull,
                _rebirth,
                _includeSummonsWhenAll
            );
            skill.StoreTarget(_storeAs, targets.FirstOrDefault());
            if (targets.Length == 0)
                return Task.CompletedTask;

            int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealProvider);
            int heal = ResolveFriendlyHealAmount(
                skill,
                baseHeal,
                _survivabilityMultiplier,
                _clampMax
            );
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].Recover(heal, rebirth: _rebirth, source: skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!string.IsNullOrWhiteSpace(_descriptionOverride))
            {
                yield return _descriptionOverride;
                yield break;
            }

            string targetText = FriendlyTargetText(_target);
            int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealProvider);
            string healText = FriendlyHealAmountText(
                skill,
                baseHeal,
                _survivabilityMultiplier,
                _clampMax
            );
            string line = $"使{targetText}回复{healText}点生命。";
            string priorityText =
                _target.Kind == TargetReferenceKind.Absolute
                && _target.AbsoluteSelector != AbsoluteFriendlySelector.All
                    ? AbsoluteFriendlyPriorityText(_preferNonFull, _rebirth)
                    : null;
            if (!string.IsNullOrWhiteSpace(priorityText))
                line += $"（{priorityText}）";
            if (_rebirth && string.IsNullOrWhiteSpace(priorityText))
                line += "（可对濒死目标生效）";
            yield return line;
        }
    }

    private sealed class ModifyFriendlyPropertySkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly TargetReference _target;
        private readonly bool _includeSummonsWhenAll;

        public ModifyFriendlyPropertySkillStep(
            PropertyType type,
            int value,
            TargetReference target,
            bool includeSummonsWhenAll
        )
        {
            _type = type;
            _value = value;
            _target = target;
            _includeSummonsWhenAll = includeSummonsWhenAll;
        }

        public override async Task Execute(Skill skill)
        {
            Character[] targets = skill.ResolveFriendlyTargets(
                _target,
                dyingFilter: true,
                includeSummonsWhenAll: _includeSummonsWhenAll
            );
            for (int i = 0; i < targets.Length; i++)
            {
                await ApplyPropertyDelta(targets[i], _type, _value, skill?.OwnerCharater);
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_value == 0)
                yield break;

            string targetText = FriendlyTargetText(_target);
            string deltaText = PropertyDeltaActionText(_type, _value);
            yield return $"使{targetText}{deltaText}。";
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
            SummonCharacter[] targets = SelectOwnedSummons(skill, _count);
            for (int i = 0; i < targets.Length; i++)
            {
                await ApplyPropertyDelta(targets[i], _type, _value, skill?.OwnerCharater);
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_value == 0)
                yield break;

            string targetText = SummonSelectionText(_count);
            string deltaText = PropertyDeltaActionText(_type, _value);
            yield return $"使{targetText}{deltaText}。";
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
            yield return $"令{targetText}获得{blockText}点格挡。";
        }
    }

    private sealed class HealSummonsSkillStep : SkillStep
    {
        private readonly int _baseHeal;
        private readonly Func<Skill, int> _baseHealProvider;
        private readonly int _count;
        private readonly int _clampMax;

        public HealSummonsSkillStep(
            int baseHeal,
            int count,
            int clampMax,
            Func<Skill, int> baseHealProvider
        )
        {
            _baseHeal = baseHeal;
            _baseHealProvider = baseHealProvider;
            _count = count;
            _clampMax = clampMax;
        }

        public override Task Execute(Skill skill)
        {
            SummonCharacter[] targets = SelectOwnedSummons(skill, _count, dyingFilter: true);
            int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealProvider);
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
            int baseHeal = ResolveStepBaseValue(skill, _baseHeal, _baseHealProvider);
            int heal = Math.Clamp(baseHeal, 0, _clampMax);
            yield return $"使{targetText}回复{heal}点生命。";
        }
    }

    private sealed class BlockFriendlyByRelativeSkillStep : SkillStep
    {
        private readonly int _relativeIndex;
        private readonly int _baseBlock;
        private readonly Func<Skill, int> _baseBlockProvider;
        private readonly int _survivabilityMultiplier;
        private readonly int _clampMax;
        private readonly bool _excludeSelf;
        private readonly bool _describe;
        private readonly string _descriptionPrefix;

        public BlockFriendlyByRelativeSkillStep(
            int relativeIndex,
            int baseBlock,
            int survivabilityMultiplier,
            int clampMax,
            bool excludeSelf,
            bool describe,
            string descriptionPrefix,
            Func<Skill, int> baseBlockProvider
        )
        {
            _relativeIndex = relativeIndex;
            _baseBlock = baseBlock;
            _baseBlockProvider = baseBlockProvider;
            _survivabilityMultiplier = survivabilityMultiplier;
            _clampMax = clampMax;
            _excludeSelf = excludeSelf;
            _describe = describe;
            _descriptionPrefix = descriptionPrefix;
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

            var target = skill.GetAllyByRelative(_relativeIndex, dyingFilter: true);
            if (_excludeSelf && target == skill.OwnerCharater)
                return Task.CompletedTask;

            if (target != null)
                target.UpdataBlock(block, source: skill?.OwnerCharater);

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
                yield return _excludeSelf
                    ? $"{_descriptionPrefix}{blockText}点格挡（对自己不生效）。"
                    : $"{_descriptionPrefix}{blockText}点格挡。";
                yield break;
            }

            string targetText = _relativeIndex switch
            {
                0 => "自己",
                -1 => "前一位队友",
                1 => "后一位队友",
                > 1 => $"后{_relativeIndex}位队友",
                _ => $"前{-_relativeIndex}位队友",
            };
            yield return _excludeSelf
                ? $"令{targetText}获得{blockText}点格挡（对自己不生效）。"
                : $"令{targetText}获得{blockText}点格挡。";
        }
    }

    private sealed class CarryRelativeAllySkillStepImpl : SkillStep
    {
        private readonly int _relativeIndex;
        private readonly int _skillIndex;
        private readonly bool _describe;
        private readonly string _descriptionLine;

        public CarryRelativeAllySkillStepImpl(
            int relativeIndex,
            int skillIndex,
            bool describe,
            string descriptionLine
        )
        {
            _relativeIndex = relativeIndex;
            _skillIndex = skillIndex;
            _describe = describe;
            _descriptionLine = descriptionLine;
        }

        public override async Task Execute(Skill skill)
        {
            if (skill.OwnerCharater?.BattleNode == null)
                return;

            var target = skill.GetAllyByRelative(_relativeIndex, dyingFilter: true);
            if (target == null || target.Skills == null)
                return;
            if (_skillIndex < 0 || _skillIndex >= target.Skills.Length)
                return;
            if (target.Skills[_skillIndex] == null)
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

            string relativeText = _relativeIndex switch
            {
                0 => "自己",
                1 => "下一位",
                -1 => "上一位",
                > 1 => $"下{_relativeIndex}位",
                _ => $"上{-_relativeIndex}位",
            };
            string skillText = _skillIndex switch
            {
                0 => "攻击技能",
                1 => "生存技能",
                2 => "特殊技能",
                _ => $"第{_skillIndex + 1}个技能",
            };
            yield return $"连携{relativeText}角色使用{skillText}。";
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

            string firstText = _relativeIndexA switch
            {
                0 => "自己",
                1 => "下一位",
                -1 => "上一位",
                > 1 => $"下{_relativeIndexA}位",
                _ => $"上{-_relativeIndexA}位",
            };
            string secondText = _relativeIndexB switch
            {
                0 => "自己",
                1 => "下一位",
                -1 => "上一位",
                > 1 => $"下{_relativeIndexB}位",
                _ => $"上{-_relativeIndexB}位",
            };
            yield return $"交换{firstText}与{secondText}的位置。";
        }
    }

    private sealed class EnergySkillStep : SkillStep
    {
        private readonly int _delta;
        private readonly TargetReference _target;
        private readonly bool _useFriendlyTarget;

        public EnergySkillStep(int delta)
        {
            _delta = delta;
            _target = default;
            _useFriendlyTarget = false;
        }

        public EnergySkillStep(int delta, TargetReference target)
        {
            _delta = delta;
            _target = target;
            _useFriendlyTarget = true;
        }

        public override Task Execute(Skill skill)
        {
            if (_delta == 0)
                return Task.CompletedTask;

            if (_useFriendlyTarget)
            {
                var target = skill.ResolveFriendlyTarget(_target, dyingFilter: true);
                if (target == null)
                    return Task.CompletedTask;

                target.UpdataEnergy(_delta, skill?.OwnerCharater);
                return Task.CompletedTask;
            }

            if (skill.OwnerCharater == null)
                return Task.CompletedTask;

            skill.OwnerCharater.UpdataEnergy(_delta, skill.OwnerCharater);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_delta == 0)
                yield break;

            string energyText = _delta > 0 ? $"恢复{_delta}点能量" : $"消耗{-_delta}点能量";
            if (_useFriendlyTarget)
            {
                string targetText = FriendlyTargetText(_target);
                yield return $"使{targetText}{energyText}。";
                yield break;
            }

            if (_delta > 0)
                yield return $"恢复{_delta}点能量。";
            else
                yield return $"消耗{-_delta}点能量。";
        }
    }

    private sealed class EnergyTimesGateSkillStep : SkillStep
    {
        private readonly int _energyCost;
        private readonly Func<int> _times;
        private readonly Action<int> _setTimes;
        private readonly SkillStep[] _onPassSteps;

        public EnergyTimesGateSkillStep(
            int energyCost,
            Func<int> times,
            Action<int> setTimes,
            SkillStep[] onPassSteps
        )
        {
            _energyCost = Math.Max(0, energyCost);
            _times = times;
            _setTimes = setTimes;
            _onPassSteps = onPassSteps ?? Array.Empty<SkillStep>();
        }

        public override async Task Execute(Skill skill)
        {
            if (!TryPassEnergyTimesGate(skill, _energyCost, _times, _setTimes))
                return;

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                await _onPassSteps[i].Execute(skill);
                if (skill._stopRemainingPlanExecution)
                    return;
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            bool hasTimes = _times != null;
            int currentTimes = hasTimes ? Math.Max(0, _times?.Invoke() ?? 0) : 0;

            if (hasTimes)
            {
                if (_energyCost > 0)
                    yield return $"消耗{_energyCost}点能量并次数-1（当前次数：{currentTimes}）：";
                else
                    yield return $"次数-1（当前次数：{currentTimes}）：";
            }
            else if (_energyCost > 0)
                yield return $"消耗{_energyCost}点能量：";

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return $"生效：{line}";
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (!CanPassEnergyTimesGate(skill, _energyCost, _times))
                return Array.Empty<Character>();

            return CollectPreviewTargets(skill, _onPassSteps);
        }
    }

    private sealed class EnergyTimesWhileSkillStep : SkillStep
    {
        private readonly int _energyCost;
        private readonly Func<int> _times;
        private readonly Action<int> _setTimes;
        private readonly SkillStep[] _loopSteps;

        public EnergyTimesWhileSkillStep(
            int energyCost,
            Func<int> times,
            Action<int> setTimes,
            SkillStep[] loopSteps
        )
        {
            _energyCost = Math.Max(0, energyCost);
            _times = times;
            _setTimes = setTimes;
            _loopSteps = loopSteps ?? Array.Empty<SkillStep>();
        }

        public override async Task Execute(Skill skill)
        {
            if (_loopSteps.Length == 0)
                return;

            if (_energyCost <= 0 && _times == null)
                return;

            while (TryPassEnergyTimesGate(skill, _energyCost, _times, _setTimes))
            {
                for (int i = 0; i < _loopSteps.Length; i++)
                {
                    await _loopSteps[i].Execute(skill);
                    if (skill._stopRemainingPlanExecution)
                        return;
                }
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            bool hasTimes = _times != null;
            int currentTimes = hasTimes ? Math.Max(0, _times?.Invoke() ?? 0) : 0;
            if (_energyCost <= 0 && !hasTimes)
            {
                yield return "循环条件无效，不执行。";
                yield break;
            }

            if (hasTimes)
            {
                if (_energyCost > 0)
                    yield return $"循环每轮消耗{_energyCost}点能量并次数{-1}（当前次数：{currentTimes}）：";
                else
                    yield return $"循环每轮次数{-1}（当前次数：{currentTimes}）。";
            }
            else
            {
                yield return $"循环每轮消耗{_energyCost}点能量：";
            }

            for (int i = 0; i < _loopSteps.Length; i++)
            {
                IEnumerable<string> lines = _loopSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    yield return $"循环：{line}";
                }
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (!CanPassEnergyTimesGate(skill, _energyCost, _times))
                return Array.Empty<Character>();

            return CollectPreviewTargets(skill, _loopSteps);
        }
    }

    private static bool CanPassEnergyTimesGate(Skill skill, int energyCost, Func<int> times)
    {
        if (skill?.OwnerCharater == null)
            return false;

        bool hasTimes = times != null;
        int currentTimes = hasTimes ? Math.Max(0, times()) : 0;
        if (hasTimes && currentTimes <= 0)
            return false;

        int safeEnergyCost = Math.Max(0, energyCost);
        return safeEnergyCost <= 0 || skill.OwnerCharater.Energy >= safeEnergyCost;
    }

    private static bool TryPassEnergyTimesGate(
        Skill skill,
        int energyCost,
        Func<int> times,
        Action<int> setTimes
    )
    {
        if (skill?.OwnerCharater == null)
            return false;

        bool hasTimes = times != null;
        int currentTimes = hasTimes ? Math.Max(0, times()) : 0;
        if (hasTimes && currentTimes <= 0)
            return false;

        int safeEnergyCost = Math.Max(0, energyCost);
        if (safeEnergyCost > 0 && !skill.TrySpendEnergy(safeEnergyCost))
            return false;

        if (hasTimes)
        {
            if (setTimes == null)
                return false;

            setTimes(Math.Max(0, currentTimes - 1));
        }

        return true;
    }

    private static async Task AttackTargetOnce(Skill skill, Character target, int damage)
    {
        if (skill == null || target == null || IsDummyTarget(skill, target))
            return;

        int clamped = Math.Clamp(damage, 0, 9999);
        await skill.AttackAnimation(target);
        await target.GetHurt(clamped, skill?.OwnerCharater);
        await Task.Delay(100);
    }

    private static async Task AttackTargetDoubleStrike(Skill skill, Character target, int damage)
    {
        if (skill == null || target == null || IsDummyTarget(skill, target))
            return;

        int clamped = Math.Clamp(damage, 0, 9999);
        await skill.AttackAnimation(target);
        await target.GetHurt(clamped, skill?.OwnerCharater);
        await Task.Delay(100);
        if (target.State != Character.CharacterState.Normal)
            return;

        var attack2 = AttackScene.Instantiate() as AttackEffect;
        target.AddChild(attack2);
        attack2.AnimationPlayer0.Play("Attack1");
        attack2.GlobalPosition = target.GlobalPosition;
        await target.GetHurt(clamped, skill?.OwnerCharater);
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
            if (_onPassSteps.Length == 0)
                return;

            bool pass = _condition?.Invoke() == true;
            if (!pass)
                return;

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                await _onPassSteps[i].Execute(skill);
                if (skill._stopRemainingPlanExecution)
                    return;
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_onPassSteps.Length == 0)
                yield break;

            if (!string.IsNullOrWhiteSpace(_conditionDescription))
                yield return $"若{_conditionDescription}：";

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return $"生效：{line}";
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            if (_condition?.Invoke() != true)
                return Array.Empty<Character>();

            return CollectPreviewTargets(skill, _onPassSteps);
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
            SkillStep[] activeSteps = _condition?.Invoke() == true ? _onPassSteps : _onFailSteps;
            if (activeSteps.Length == 0)
                return;

            for (int i = 0; i < activeSteps.Length; i++)
            {
                await activeSteps[i].Execute(skill);
                if (skill._stopRemainingPlanExecution)
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
                yield return $"若{_conditionDescription}：";

            for (int i = 0; i < _onPassSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return $"生效：{line}";
            }

            if (!hasFail)
                yield break;

            yield return "否则：";
            for (int i = 0; i < _onFailSteps.Length; i++)
            {
                IEnumerable<string> lines =
                    _onFailSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return $"生效：{line}";
            }
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            SkillStep[] activeSteps = _condition?.Invoke() == true ? _onPassSteps : _onFailSteps;
            return CollectPreviewTargets(skill, activeSteps);
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
