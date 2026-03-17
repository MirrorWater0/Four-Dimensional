using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// SSOT Step Catalog
// - Target Rule: 未显式指定目标策略时，默认按 Chosetarget1 顺序选取目标，描述中不额外说明。
// - AttackPrimaryStep: 单段攻击（Attack1）。
// - DoubleStrikeStep: 二段攻击（Attack2）。
// - AoeDamageStep: 群体伤害（按 Chosetarget1 顺序命中前N名）。
// - ApplyBuffHostile: 对敌方施加Buff（buffName/stacks/maxTargets/energyCost）。
// - ApplyBuffAll: 对全阵施加Buff（buffName/stacks/targetCamp/dyingFilter/energyCost）。
// - LowerTargetPropertyStep: 下降前N名目标属性（type/value/maxTargets/permanent）。
// - ModifyPropertyStep: 按相对位调整友方属性（type/value/index；value正增负减）。
// - ModifyPropertyAbsoluteStep: 按绝对位调整友方属性（type/value/selector；支持全阵，value正增负减）。
// - ApplyBuffFriendly: 对友方相对位施加Buff（buffName/stacks/index/dyingFilter/energyCost）。
// - ApplyBuffFriendlyAbsolute: 对友方绝对位施加Buff（buffName/stacks/index/dyingFilter/energyCost）。
// - HealFriendlyRelative: 对友方相对位治疗（baseHeal/survivabilityMultiplier/index/dyingFilter/rebirth）。
// - HurtFriendly: 对友方造成伤害（damage/index/all）。
// - HealFriendlyAbsolute: 对友方绝对位治疗（baseHeal/survivabilityMultiplier/selector/preferNonFull/rebirth）。
// - FriendlyEnergyRelativeStep: 对友方相对位增减能量（delta/relative/dyingFilter）。
// - ModifyPropertyStep: 调整友方属性（可按相对位；value正增负减）。
// - SelfBlockStep: 自身获得格挡。
// - RelativeAllyBlockStep: 相对位队友获得格挡（0自己、-1前一位、+1后一位...；可选非濒死过滤）。
// - CarryRelativeAllyStep: 连携相对位队友释放指定技能（+n下n位，-n上n位；index:0攻击/1生存/2特殊）。
// - SwapPositionFriendlyStep: 交换两个相对位队友的位置（0自己；交换PositionIndex并同步出手顺序）。
// - EnergyStep: 改变自身能量。
// - EnergyTimesGateStep: 能量+次数联合门槛（满足则消耗能量并次数-1，并执行生效体；不再阻断后续step）。
// - EnergyTimesWhileStep: while循环（按EnergyTimesGate条件判定；条件满足时循环执行循环体step）。
// - ConditionStep: 条件执行（condition/steps/conditionDescription）。
// - TextStep: 仅描述文本（不执行效果）。
// - CustomStep: 自定义执行/描述兜底步骤。
public partial class Skill
{
    private SkillPlan _cachedPlan;
    private bool _stopRemainingPlanExecution;
    private readonly Dictionary<string, Character> _absoluteFriendlyTargetBinds = new();
    private readonly Dictionary<string, Character> _hostileTargetBinds = new();

    public enum AbsoluteFriendlySelector
    {
        FrontMost,
        BackMost,
        LowestLife,
    }

    public enum BuffTargetCamp
    {
        Hostile,
        Friendly,
    }

    public enum PropertyAbsoluteSelector
    {
        FrontMost,
        BackMost,
        All,
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

    private Character ResolveAbsoluteFriendlyTarget(
        AbsoluteFriendlySelector selector,
        bool preferNonFull,
        bool rebirth,
        string targetKey
    )
    {
        if (!string.IsNullOrWhiteSpace(targetKey))
        {
            if (_absoluteFriendlyTargetBinds.TryGetValue(targetKey, out Character cached))
            {
                if (cached != null)
                    return cached;
            }
        }

        Character selected = SelectAbsoluteFriendlyTarget(this, selector, preferNonFull, rebirth);
        if (!string.IsNullOrWhiteSpace(targetKey))
        {
            if (selected == null)
                _absoluteFriendlyTargetBinds.Remove(targetKey);
            else
                _absoluteFriendlyTargetBinds[targetKey] = selected;
        }

        return selected;
    }

    private Character ResolveRelativeFriendlyTarget(
        int relative,
        bool dyingFilter,
        string targetKey
    )
    {
        if (!string.IsNullOrWhiteSpace(targetKey))
        {
            if (_absoluteFriendlyTargetBinds.TryGetValue(targetKey, out Character cached))
            {
                if (cached != null)
                    return cached;
            }
        }

        Character selected = GetAllyByRelative(relative, dyingFilter: dyingFilter);
        if (!string.IsNullOrWhiteSpace(targetKey))
        {
            if (selected == null)
                _absoluteFriendlyTargetBinds.Remove(targetKey);
            else
                _absoluteFriendlyTargetBinds[targetKey] = selected;
        }

        return selected;
    }

    private Character ResolveHostileTarget(string targetKey)
    {
        if (!string.IsNullOrWhiteSpace(targetKey))
        {
            if (_hostileTargetBinds.TryGetValue(targetKey, out Character cached))
            {
                if (cached != null)
                    return cached;
            }
        }

        Character selected = null;
        var targets = Chosetarget1();
        if (targets.Length > 0)
            selected = targets[0];

        if (!string.IsNullOrWhiteSpace(targetKey))
        {
            if (selected == null)
                _hostileTargetBinds.Remove(targetKey);
            else
                _hostileTargetBinds[targetKey] = selected;
        }

        return selected;
    }

    protected Character GetHostileTargetBind(string targetKey)
    {
        if (string.IsNullOrWhiteSpace(targetKey))
            return null;

        _hostileTargetBinds.TryGetValue(targetKey, out Character cached);
        return cached;
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
            _skill._absoluteFriendlyTargetBinds.Clear();
            _skill._hostileTargetBinds.Clear();
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_skill._stopRemainingPlanExecution)
                    break;
                await _steps[i].Execute(_skill);
            }
            _skill._stopRemainingPlanExecution = false;
            _skill._absoluteFriendlyTargetBinds.Clear();
            _skill._hostileTargetBinds.Clear();
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
    }

    protected abstract class SkillStep
    {
        public abstract Task Execute(Skill skill);

        public abstract IEnumerable<string> Describe(Skill skill);
    }

    protected SkillStep AttackPrimaryStep(
        int baseDamage = 0,
        int powerMultiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        string targetKey = null
    ) => new AttackPrimarySkillStep(baseDamage, powerMultiplier, prefix, suffix, clampMax, targetKey);

    protected SkillStep DoubleStrikeStep(
        int baseDamage = 0,
        int powerMultiplier = 1,
        string prefix = "每段造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        bool includeTwoHitText = true,
        string targetKey = null
    ) =>
        new DoubleStrikeSkillStep(
            baseDamage,
            powerMultiplier,
            prefix,
            suffix,
            clampMax,
            includeTwoHitText,
            targetKey
        );

    protected SkillStep AoeDamageStep(
        int baseDamage = 0,
        int powerMultiplier = 1,
        int maxTargets = 3,
        int times = 1,
        int clampMax = 9999
    ) => new AoeDamageSkillStep(baseDamage, powerMultiplier, maxTargets, times, clampMax);

    protected SkillStep ApplyBuffHostile(
        Buff.BuffName buffName,
        int stacks,
        int maxTargets = 1,
        int energyCost = 0
    ) => new ApplyBuffHostileSkillStep(buffName, stacks, maxTargets, energyCost);

    protected SkillStep ApplyBuffAll(
        Buff.BuffName buffName,
        int stacks,
        BuffTargetCamp targetCamp = BuffTargetCamp.Hostile,
        bool dyingFilter = true,
        int energyCost = 0
    ) => new ApplyBuffAllSkillStep(buffName, stacks, targetCamp, dyingFilter, energyCost);

    protected SkillStep LowerTargetPropertyStep(
        PropertyType type,
        int value,
        int maxTargets = 1,
        bool permanent = false
    ) => new LowerTargetPropertySkillStep(type, value, maxTargets, permanent);

    protected SkillStep ApplyBuffFriendly(
        Buff.BuffName buffName,
        int stacks,
        int index = 0,
        bool dyingFilter = true,
        int energyCost = 0
    ) => new ApplyBuffFriendlySkillStep(buffName, stacks, index, dyingFilter, energyCost);

    protected SkillStep ApplyBuffFriendlyAbsolute(
        Buff.BuffName buffName,
        int stacks,
        int index = 0,
        bool dyingFilter = true,
        int energyCost = 0
    ) => new ApplyBuffFriendlyAbsoluteSkillStep(buffName, stacks, index, dyingFilter, energyCost);

    protected SkillStep HealFriendlyRelative(
        int baseHeal = 0,
        int survivabilityMultiplier = 1,
        int index = 0,
        bool dyingFilter = true,
        bool rebirth = false,
        int clampMax = 999
    ) =>
        new HealFriendlyRelativeSkillStep(
            baseHeal,
            survivabilityMultiplier,
            index,
            dyingFilter,
            rebirth,
            clampMax
        );

    protected SkillStep HurtFriendly(int damage, int index = 0, bool all = false) =>
        new HurtFriendlySkillStep(damage, index, all);

    protected SkillStep HealFriendlyAbsolute(
        int baseHeal = 0,
        int survivabilityMultiplier = 1,
        AbsoluteFriendlySelector selector = AbsoluteFriendlySelector.FrontMost,
        bool preferNonFull = true,
        bool rebirth = false,
        int clampMax = 999,
        string targetKey = null
    ) =>
        new HealFriendlyAbsoluteSkillStep(
            baseHeal,
            survivabilityMultiplier,
            selector,
            preferNonFull,
            rebirth,
            clampMax,
            targetKey
        );

    protected SkillStep FriendlyEnergyRelativeStep(
        int delta,
        int relative = 0,
        bool dyingFilter = true,
        string targetKey = null
    ) => new FriendlyEnergyRelativeSkillStep(delta, relative, dyingFilter, targetKey);

    protected SkillStep ModifyPropertyStep(
        PropertyType type,
        int value,
        int index = 0,
        bool dyingFilter = false
    ) => new ModifyPropertyRelativeSkillStep(type, value, index, dyingFilter);

    protected SkillStep ModifyPropertyAbsoluteStep(
        PropertyType type,
        int value,
        PropertyAbsoluteSelector selector = PropertyAbsoluteSelector.FrontMost,
        bool dyingFilter = false
    ) => new ModifyPropertyAbsoluteSkillStep(type, value, selector, dyingFilter);

    protected SkillStep SelfBlockStep(
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        string prefix = "获得",
        string suffix = "点格挡。",
        int clampMax = 999
    ) => new SelfBlockSkillStep(baseBlock, survivabilityMultiplier, prefix, suffix, clampMax);

    protected SkillStep RelativeAllyBlockStep(
        int relativeIndex,
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999,
        bool dyingFilter = true,
        bool describe = true,
        string descriptionPrefix = null
    ) =>
        new RelativeAllyBlockSkillStep(
            relativeIndex,
            baseBlock,
            survivabilityMultiplier,
            clampMax,
            dyingFilter,
            describe,
            descriptionPrefix
        );

    protected SkillStep CarryRelativeAllyStep(
        int relativeIndex,
        int skillIndex,
        bool dyingFilter = true,
        bool describe = true,
        string descriptionLine = null
    ) =>
        new CarryRelativeAllySkillStepImpl(
            relativeIndex,
            skillIndex,
            dyingFilter,
            describe,
            descriptionLine
        );

    protected SkillStep SwapPositionFriendlyStep(
        int relativeIndexA,
        int relativeIndexB,
        bool dyingFilter = true,
        bool describe = true,
        string descriptionLine = null
    ) =>
        new SwapPositionFriendlySkillStep(
            relativeIndexA,
            relativeIndexB,
            dyingFilter,
            describe,
            descriptionLine
        );

    protected SkillStep EnergyStep(int delta) => new EnergySkillStep(delta);

    protected SkillStep EnergyTimesGateStep(
        int energyCost,
        Func<int> times = null,
        Action<int> setTimes = null,
        params SkillStep[] onPassSteps
    ) => new EnergyTimesGateSkillStep(energyCost, times, setTimes, onPassSteps);

    protected SkillStep EnergyTimesWhileStep(
        int energyCost,
        Func<int> times = null,
        Action<int> setTimes = null,
        params SkillStep[] loopSteps
    ) => new EnergyTimesWhileSkillStep(energyCost, times, setTimes, loopSteps);

    protected SkillStep ConditionStep(
        Func<bool> condition,
        string conditionDescription,
        params SkillStep[] onPassSteps
    ) => new ConditionSkillStep(condition, conditionDescription, onPassSteps);

    protected SkillStep TextStep(string line) => new TextSkillStep(line);

    protected SkillStep CustomStep(
        Func<Skill, Task> execute,
        Func<Skill, IEnumerable<string>> describe
    ) => new DelegateSkillStep(execute, describe);

    private sealed class AttackPrimarySkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly int _powerMultiplier;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _clampMax;
        private readonly string _targetKey;

        public AttackPrimarySkillStep(
            int baseDamage,
            int powerMultiplier,
            string prefix,
            string suffix,
            int clampMax,
            string targetKey
        )
        {
            _baseDamage = baseDamage;
            _powerMultiplier = powerMultiplier;
            _prefix = prefix;
            _suffix = suffix;
            _clampMax = clampMax;
            _targetKey = targetKey;
        }

        public override async Task Execute(Skill skill)
        {
            int damage = skill.DamageFromPower(_baseDamage, _powerMultiplier, _clampMax);
            if (!string.IsNullOrWhiteSpace(_targetKey))
            {
                var target = skill.ResolveHostileTarget(_targetKey);
                if (target == null)
                    return;
                await AttackTargetOnce(skill, target, damage);
                return;
            }

            await skill.Attack1(damage);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            yield return skill.DamageLine(
                _baseDamage,
                _powerMultiplier,
                _prefix,
                _suffix,
                _clampMax
            );
        }
    }

    private sealed class DoubleStrikeSkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly int _powerMultiplier;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _clampMax;
        private readonly bool _includeTwoHitText;
        private readonly string _targetKey;

        public DoubleStrikeSkillStep(
            int baseDamage,
            int powerMultiplier,
            string prefix,
            string suffix,
            int clampMax,
            bool includeTwoHitText,
            string targetKey
        )
        {
            _baseDamage = baseDamage;
            _powerMultiplier = powerMultiplier;
            _prefix = prefix;
            _suffix = suffix;
            _clampMax = clampMax;
            _includeTwoHitText = includeTwoHitText;
            _targetKey = targetKey;
        }

        public override async Task Execute(Skill skill)
        {
            int damage = skill.DamageFromPower(_baseDamage, _powerMultiplier, _clampMax);
            if (!string.IsNullOrWhiteSpace(_targetKey))
            {
                var target = skill.ResolveHostileTarget(_targetKey);
                if (target == null)
                    return;
                await AttackTargetDoubleStrike(skill, target, damage);
                return;
            }

            await skill.Attack2(damage);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_includeTwoHitText)
                yield return "二段攻击。";

            yield return skill.DamageLine(
                _baseDamage,
                _powerMultiplier,
                _prefix,
                _suffix,
                _clampMax
            );
        }
    }

    private sealed class AoeDamageSkillStep : SkillStep
    {
        private readonly int _baseDamage;
        private readonly int _powerMultiplier;
        private readonly int _maxTargets;
        private readonly int _times;
        private readonly int _clampMax;

        public AoeDamageSkillStep(
            int baseDamage,
            int powerMultiplier,
            int maxTargets,
            int times,
            int clampMax
        )
        {
            _baseDamage = baseDamage;
            _powerMultiplier = powerMultiplier;
            _maxTargets = maxTargets;
            _times = times;
            _clampMax = clampMax;
        }

        public override async Task Execute(Skill skill)
        {
            var targets = skill.Chosetarget1();
            int count = _maxTargets <= 0 ? targets.Length : Math.Min(_maxTargets, targets.Length);
            if (count <= 0)
                return;

            int times = Math.Max(1, _times);
            int damage = skill.DamageFromPower(_baseDamage, _powerMultiplier, _clampMax);
            await skill.AOE(damage, count, times);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string damageText = skill.DamageFromPowerText(_baseDamage, _powerMultiplier, _clampMax);
            if (_maxTargets <= 0)
                yield return $"对所有目标造成{damageText}点伤害。";
            else
                yield return $"对至多{_maxTargets}名目标造成{damageText}点伤害。";

            if (_times > 1)
                yield return $"共{_times}段。";
        }
    }

    private sealed class LowerTargetPropertySkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly int _maxTargets;
        private readonly bool _permanent;

        public LowerTargetPropertySkillStep(
            PropertyType type,
            int value,
            int maxTargets,
            bool permanent
        )
        {
            _type = type;
            _value = value;
            _maxTargets = maxTargets;
            _permanent = permanent;
        }

        public override async Task Execute(Skill skill)
        {
            int loss = Math.Abs(_value);
            if (loss == 0)
                return;

            var targets = skill.Chosetarget1();
            int count = _maxTargets <= 0 ? targets.Length : Math.Min(_maxTargets, targets.Length);
            if (count <= 0)
                return;

            for (int i = 0; i < count; i++)
            {
                if (targets[i] == null)
                    continue;
                bool tryPermanent = _permanent && targets[i] is PlayerCharacter;
                int before = 0;
                if (tryPermanent)
                    before = GetPropertyValue(targets[i], _type);
                await targets[i].DescendingProperties(_type, loss);
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
            if (_maxTargets == 1)
                yield return $"下降目标{loseText}{PermanentTag()}。";
            else if (_maxTargets <= 0)
                yield return $"下降所有命中目标{loseText}{PermanentTag()}。";
            else
                yield return $"下降至多{_maxTargets}名目标{loseText}{PermanentTag()}。";
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

    private static bool TryApplyBuffToTarget(Buff.BuffName buffName, Character target, int stacks)
    {
        if (target == null || stacks == 0)
            return false;

        switch (buffName)
        {
            case Buff.BuffName.RebirthI:
                DyingBuff.BuffAdd(buffName, target, stacks);
                return true;
            case Buff.BuffName.DamageImmune:
            case Buff.BuffName.Vulnerable:
            case Buff.BuffName.Taunt:
                HurtBuff.BuffAdd(buffName, target, stacks);
                return true;
            case Buff.BuffName.Stun:
                SkillBuff.BuffAdd(buffName, target, stacks);
                return true;
            case Buff.BuffName.Pursuit:
                EndActionBuff.BuffAdd(buffName, target, stacks);
                return true;
            case Buff.BuffName.Invisible:
                StartActionBuff.BuffAdd(buffName, target, stacks);
                return true;
            case Buff.BuffName.DebuffImmunity:
            case Buff.BuffName.ExtraPower:
            case Buff.BuffName.ExtraSurvivability:
                SpecialBuff.BuffAdd(buffName, target, stacks);
                return true;
            default:
                return false;
        }
    }

    private static string RelativeFriendlyTargetText(int index, bool dyingFilter)
    {
        string relativeText = index switch
        {
            0 => "自己",
            -1 => "上一位队友",
            1 => "下一位队友",
            > 1 => $"下{index}位队友",
            _ => $"上{-index}位队友",
        };
        return dyingFilter ? $"非濒死{relativeText}" : relativeText;
    }

    private static string AbsoluteFriendlyTargetText(int index, bool dyingFilter)
    {
        string orderText = index switch
        {
            0 => "顺位第1位队友",
            > 0 => $"顺位第{index + 1}位队友",
            _ => $"顺位倒数第{-index}位队友",
        };
        return dyingFilter ? $"非濒死{orderText}" : orderText;
    }

    private static Character GetAllyByAbsoluteOrder(Skill skill, int index, bool dyingFilter)
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return null;

        var allies = skill.GetAllAllyWithOrder(dyingFilter);
        if (allies == null || allies.Length == 0)
            return null;

        int safeIndex = (index % allies.Length + allies.Length) % allies.Length;
        return allies[safeIndex];
    }

    private static Character[] GetAllHostileWithOrder(Skill skill, bool dyingFilter)
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return Array.Empty<Character>();

        IEnumerable<Character> query = skill.OwnerCharater.IsPlayer
            ? skill.OwnerCharater.BattleNode.EnemiesList.Cast<Character>()
            : skill.OwnerCharater.BattleNode.PlayersList.Cast<Character>();

        query = query.Where(x => x != null);
        if (dyingFilter)
            query = query.Where(x => x.State != Character.CharacterState.Dying);

        return query.OrderBy(x => x.PositionIndex).ToArray();
    }

    private static async Task ApplyPropertyDelta(Character target, PropertyType type, int value)
    {
        if (target == null || value == 0)
            return;

        if (value > 0)
            await target.IncreaseProperties(type, value);
        else
            await target.DescendingProperties(type, -value);
    }

    private static string PropertyDeltaActionText(PropertyType type, int value)
    {
        int amount = Math.Abs(value);
        string propertyText = $"{amount}{GetColoredPropertyLabel(type)}";
        return value >= 0 ? $"增加{propertyText}" : $"减少{propertyText}";
    }

    private static string PropertyAbsoluteSelectorText(
        PropertyAbsoluteSelector selector,
        bool dyingFilter
    )
    {
        string targetText = selector switch
        {
            PropertyAbsoluteSelector.FrontMost => "站位最前队友",
            PropertyAbsoluteSelector.BackMost => "站位最后队友",
            PropertyAbsoluteSelector.All => "友方全阵",
            _ => "队友",
        };

        if (selector == PropertyAbsoluteSelector.All)
            return dyingFilter ? $"{targetText}（非濒死）" : targetText;

        return dyingFilter ? $"非濒死{targetText}" : targetText;
    }

    private static Character[] SelectPropertyAbsoluteTargets(
        Skill skill,
        PropertyAbsoluteSelector selector,
        bool dyingFilter
    )
    {
        if (skill?.OwnerCharater?.BattleNode == null)
            return Array.Empty<Character>();

        Character[] allies = skill.GetAllAllyWithOrder(dyingFilter).Where(x => x != null).ToArray();
        if (allies.Length == 0)
            return Array.Empty<Character>();

        return selector switch
        {
            PropertyAbsoluteSelector.FrontMost => [allies[0]],
            PropertyAbsoluteSelector.BackMost => [allies[^1]],
            PropertyAbsoluteSelector.All => allies,
            _ => [allies[0]],
        };
    }

    private static string AbsoluteFriendlySelectorText(AbsoluteFriendlySelector selector)
    {
        return selector switch
        {
            AbsoluteFriendlySelector.FrontMost => "站位最前队友",
            AbsoluteFriendlySelector.BackMost => "站位最后队友",
            AbsoluteFriendlySelector.LowestLife => "生命最少队友",
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

    private sealed class ApplyBuffHostileSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly int _maxTargets;
        private readonly int _energyCost;

        public ApplyBuffHostileSkillStep(
            Buff.BuffName buffName,
            int stacks,
            int maxTargets,
            int energyCost
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _maxTargets = maxTargets;
            _energyCost = Math.Max(0, energyCost);
        }

        public override Task Execute(Skill skill)
        {
            if (_energyCost > 0 && !skill.TrySpendEnergy(_energyCost))
                return Task.CompletedTask;

            var targets = skill.Chosetarget1();
            int count = _maxTargets <= 0 ? targets.Length : Math.Min(_maxTargets, targets.Length);
            for (int i = 0; i < count; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], _stacks);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_energyCost > 0)
                yield return $"消耗{_energyCost}点能量:";

            string stacksText = BuffStacksText(_buffName, _stacks);
            if (_maxTargets == 1)
                yield return $"使目标获得{stacksText}。";
            else if (_maxTargets <= 0)
                yield return $"使所有命中目标获得{stacksText}。";
            else
                yield return $"使其各获得{stacksText}。";
        }
    }

    private sealed class ApplyBuffAllSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly BuffTargetCamp _targetCamp;
        private readonly bool _dyingFilter;
        private readonly int _energyCost;

        public ApplyBuffAllSkillStep(
            Buff.BuffName buffName,
            int stacks,
            BuffTargetCamp targetCamp,
            bool dyingFilter,
            int energyCost
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _targetCamp = targetCamp;
            _dyingFilter = dyingFilter;
            _energyCost = Math.Max(0, energyCost);
        }

        public override Task Execute(Skill skill)
        {
            if (_energyCost > 0 && !skill.TrySpendEnergy(_energyCost))
                return Task.CompletedTask;

            Character[] targets =
                _targetCamp == BuffTargetCamp.Hostile
                    ? GetAllHostileWithOrder(skill, _dyingFilter)
                    : skill.GetAllAllyWithOrder(_dyingFilter).Where(x => x != null).ToArray();

            for (int i = 0; i < targets.Length; i++)
            {
                TryApplyBuffToTarget(_buffName, targets[i], _stacks);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_energyCost > 0)
                yield return $"消耗{_energyCost}点能量:";

            string campText = _targetCamp == BuffTargetCamp.Hostile ? "敌方全阵" : "友方全阵";
            if (_dyingFilter)
                campText += "（非濒死）";

            string stacksText = BuffStacksText(_buffName, _stacks);
            yield return $"使{campText}获得{stacksText}。";
        }
    }

    private sealed class ApplyBuffFriendlySkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly int _index;
        private readonly bool _dyingFilter;
        private readonly int _energyCost;

        public ApplyBuffFriendlySkillStep(
            Buff.BuffName buffName,
            int stacks,
            int index,
            bool dyingFilter,
            int energyCost
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _index = index;
            _dyingFilter = dyingFilter;
            _energyCost = Math.Max(0, energyCost);
        }

        public override Task Execute(Skill skill)
        {
            if (_energyCost > 0 && !skill.TrySpendEnergy(_energyCost))
                return Task.CompletedTask;

            var target = skill.GetAllyByRelative(_index, dyingFilter: _dyingFilter);
            TryApplyBuffToTarget(_buffName, target, _stacks);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_energyCost > 0)
                yield return $"消耗{_energyCost}点能量:";

            string targetText = RelativeFriendlyTargetText(_index, _dyingFilter);
            string stacksText = BuffStacksText(_buffName, _stacks);
            yield return $"使{targetText}获得{stacksText}。";
        }
    }

    private sealed class ApplyBuffFriendlyAbsoluteSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly int _index;
        private readonly bool _dyingFilter;
        private readonly int _energyCost;

        public ApplyBuffFriendlyAbsoluteSkillStep(
            Buff.BuffName buffName,
            int stacks,
            int index,
            bool dyingFilter,
            int energyCost
        )
        {
            _buffName = buffName;
            _stacks = stacks;
            _index = index;
            _dyingFilter = dyingFilter;
            _energyCost = Math.Max(0, energyCost);
        }

        public override Task Execute(Skill skill)
        {
            if (_energyCost > 0 && !skill.TrySpendEnergy(_energyCost))
                return Task.CompletedTask;

            var target = GetAllyByAbsoluteOrder(skill, _index, _dyingFilter);
            TryApplyBuffToTarget(_buffName, target, _stacks);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_energyCost > 0)
                yield return $"消耗{_energyCost}点能量:";

            string targetText = AbsoluteFriendlyTargetText(_index, _dyingFilter);
            string stacksText = BuffStacksText(_buffName, _stacks);
            yield return $"使{targetText}获得{stacksText}。";
        }
    }

    private sealed class HealFriendlyRelativeSkillStep : SkillStep
    {
        private readonly int _baseHeal;
        private readonly int _survivabilityMultiplier;
        private readonly int _index;
        private readonly bool _dyingFilter;
        private readonly bool _rebirth;
        private readonly int _clampMax;

        public HealFriendlyRelativeSkillStep(
            int baseHeal,
            int survivabilityMultiplier,
            int index,
            bool dyingFilter,
            bool rebirth,
            int clampMax
        )
        {
            _baseHeal = baseHeal;
            _survivabilityMultiplier = survivabilityMultiplier;
            _index = index;
            _dyingFilter = dyingFilter;
            _rebirth = rebirth;
            _clampMax = clampMax;
        }

        public override Task Execute(Skill skill)
        {
            var target = skill.GetAllyByRelative(_index, dyingFilter: _dyingFilter);
            if (target == null)
                return Task.CompletedTask;

            int heal = Math.Clamp(_baseHeal, 0, _clampMax);
            target.Recover(heal, rebirth: _rebirth);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = RelativeFriendlyTargetText(_index, _dyingFilter);
            int heal = Math.Clamp(_baseHeal, 0, _clampMax);
            string healText = heal.ToString();
            string line = $"使{targetText}回复{healText}点生命。";
            if (_rebirth)
                line += "（可对濒死目标生效）";
            yield return line;
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
                    .GetAllAllyWithOrder(dyingFilter: true)
                    .Where(x => x != null)
                    .ToArray();
                if (allies.Length == 0)
                    return;

                List<Task> tasks = new(allies.Length);
                for (int i = 0; i < allies.Length; i++)
                {
                    tasks.Add(allies[i].GetHurt(_damage));
                }

                await Task.WhenAll(tasks);
                return;
            }

            var target = skill.GetAllyByRelative(_index, dyingFilter: true);
            if (target != null)
                await target.GetHurt(_damage);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_damage <= 0)
                yield break;

            if (_all)
            {
                yield return $"对友方全阵（非濒死）造成{_damage}点伤害。";
                yield break;
            }

            string targetText = RelativeFriendlyTargetText(_index, dyingFilter: true);
            yield return $"对{targetText}造成{_damage}点伤害。";
        }
    }

    private sealed class HealFriendlyAbsoluteSkillStep : SkillStep
    {
        private readonly int _baseHeal;
        private readonly int _survivabilityMultiplier;
        private readonly AbsoluteFriendlySelector _selector;
        private readonly bool _preferNonFull;
        private readonly bool _rebirth;
        private readonly int _clampMax;
        private readonly string _targetKey;

        public HealFriendlyAbsoluteSkillStep(
            int baseHeal,
            int survivabilityMultiplier,
            AbsoluteFriendlySelector selector,
            bool preferNonFull,
            bool rebirth,
            int clampMax,
            string targetKey
        )
        {
            _baseHeal = baseHeal;
            _survivabilityMultiplier = survivabilityMultiplier;
            _selector = selector;
            _preferNonFull = preferNonFull;
            _rebirth = rebirth;
            _clampMax = clampMax;
            _targetKey = targetKey;
        }

        public override Task Execute(Skill skill)
        {
            var target = skill.ResolveAbsoluteFriendlyTarget(
                _selector,
                _preferNonFull,
                _rebirth,
                _targetKey
            );
            if (target == null)
                return Task.CompletedTask;

            int heal = Math.Clamp(_baseHeal, 0, _clampMax);
            target.Recover(heal, rebirth: _rebirth);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            string targetText = AbsoluteFriendlySelectorText(_selector);
            int heal = Math.Clamp(_baseHeal, 0, _clampMax);
            string healText = heal.ToString();
            string line = $"使{targetText}回复{healText}点生命。";
            string priorityText = AbsoluteFriendlyPriorityText(_preferNonFull, _rebirth);
            if (!string.IsNullOrWhiteSpace(priorityText))
                line += $"（{priorityText}）";
            if (!string.IsNullOrWhiteSpace(_targetKey))
                line += $"（目标为{_targetKey}）";
            yield return line;
        }
    }

    private sealed class FriendlyEnergyRelativeSkillStep : SkillStep
    {
        private readonly int _delta;
        private readonly int _relative;
        private readonly bool _dyingFilter;
        private readonly string _targetKey;

        public FriendlyEnergyRelativeSkillStep(
            int delta,
            int relative,
            bool dyingFilter,
            string targetKey
        )
        {
            _delta = delta;
            _relative = relative;
            _dyingFilter = dyingFilter;
            _targetKey = targetKey;
        }

        public override Task Execute(Skill skill)
        {
            if (_delta == 0)
                return Task.CompletedTask;

            var target = skill.ResolveRelativeFriendlyTarget(_relative, _dyingFilter, _targetKey);
            if (target == null)
                return Task.CompletedTask;

            target.UpdataEnergy(_delta);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_delta == 0)
                yield break;

            string targetText = RelativeFriendlyTargetText(_relative, _dyingFilter);
            string energyText = _delta > 0 ? $"恢复{_delta}点能量" : $"消耗{-_delta}点能量";
            string line = $"使{targetText}{energyText}。";
            if (!string.IsNullOrWhiteSpace(_targetKey))
                line += $"（目标为{_targetKey}）";
            yield return line;
        }
    }

    private sealed class ModifyPropertyRelativeSkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly int _index;
        private readonly bool _dyingFilter;

        public ModifyPropertyRelativeSkillStep(
            PropertyType type,
            int value,
            int index,
            bool dyingFilter
        )
        {
            _type = type;
            _value = value;
            _index = index;
            _dyingFilter = dyingFilter;
        }

        public override async Task Execute(Skill skill)
        {
            var target = skill.GetAllyByRelative(_index, dyingFilter: _dyingFilter);
            await ApplyPropertyDelta(target, _type, _value);
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_value == 0)
                yield break;

            string targetText = RelativeFriendlyTargetText(_index, _dyingFilter);
            string deltaText = PropertyDeltaActionText(_type, _value);
            yield return $"使{targetText}{deltaText}。";
        }
    }

    private sealed class ModifyPropertyAbsoluteSkillStep : SkillStep
    {
        private readonly PropertyType _type;
        private readonly int _value;
        private readonly PropertyAbsoluteSelector _selector;
        private readonly bool _dyingFilter;

        public ModifyPropertyAbsoluteSkillStep(
            PropertyType type,
            int value,
            PropertyAbsoluteSelector selector,
            bool dyingFilter
        )
        {
            _type = type;
            _value = value;
            _selector = selector;
            _dyingFilter = dyingFilter;
        }

        public override async Task Execute(Skill skill)
        {
            Character[] targets = SelectPropertyAbsoluteTargets(skill, _selector, _dyingFilter);
            for (int i = 0; i < targets.Length; i++)
            {
                await ApplyPropertyDelta(targets[i], _type, _value);
            }
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_value == 0)
                yield break;

            string targetText = PropertyAbsoluteSelectorText(_selector, _dyingFilter);
            string deltaText = PropertyDeltaActionText(_type, _value);
            yield return $"使{targetText}{deltaText}。";
        }
    }

    private sealed class SelfBlockSkillStep : SkillStep
    {
        private readonly int _baseBlock;
        private readonly int _survivabilityMultiplier;
        private readonly string _prefix;
        private readonly string _suffix;
        private readonly int _clampMax;

        public SelfBlockSkillStep(
            int baseBlock,
            int survivabilityMultiplier,
            string prefix,
            string suffix,
            int clampMax
        )
        {
            _baseBlock = baseBlock;
            _survivabilityMultiplier = survivabilityMultiplier;
            _prefix = prefix;
            _suffix = suffix;
            _clampMax = clampMax;
        }

        public override Task Execute(Skill skill)
        {
            if (skill.OwnerCharater == null)
                return Task.CompletedTask;

            int block = skill.BlockFromSurvivability(
                _baseBlock,
                _survivabilityMultiplier,
                _clampMax
            );
            skill.OwnerCharater.UpdataBlock(block);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            yield return skill.BlockLine(
                _baseBlock,
                _survivabilityMultiplier,
                _prefix,
                _suffix,
                _clampMax
            );
        }
    }

    private sealed class RelativeAllyBlockSkillStep : SkillStep
    {
        private readonly int _relativeIndex;
        private readonly int _baseBlock;
        private readonly int _survivabilityMultiplier;
        private readonly int _clampMax;
        private readonly bool _dyingFilter;
        private readonly bool _describe;
        private readonly string _descriptionPrefix;

        public RelativeAllyBlockSkillStep(
            int relativeIndex,
            int baseBlock,
            int survivabilityMultiplier,
            int clampMax,
            bool dyingFilter,
            bool describe,
            string descriptionPrefix
        )
        {
            _relativeIndex = relativeIndex;
            _baseBlock = baseBlock;
            _survivabilityMultiplier = survivabilityMultiplier;
            _clampMax = clampMax;
            _dyingFilter = dyingFilter;
            _describe = describe;
            _descriptionPrefix = descriptionPrefix;
        }

        public override Task Execute(Skill skill)
        {
            if (skill.OwnerCharater?.BattleNode == null)
                return Task.CompletedTask;

            int block = skill.BlockFromSurvivability(
                _baseBlock,
                _survivabilityMultiplier,
                _clampMax
            );

            var target = skill.GetAllyByRelative(_relativeIndex, dyingFilter: _dyingFilter);
            if (target != null)
                target.UpdataBlock(block);

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (!_describe)
                yield break;

            string blockText = skill.BlockFromSurvivabilityText(
                _baseBlock,
                _survivabilityMultiplier,
                _clampMax
            );

            if (!string.IsNullOrWhiteSpace(_descriptionPrefix))
            {
                string line = $"{_descriptionPrefix}{blockText}点格挡。";
                if (_dyingFilter && line.Contains("非濒死", StringComparison.Ordinal) == false)
                {
                    line = $"{line}（目标为非濒死）";
                }
                yield return line;
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
            if (_dyingFilter)
                targetText = $"非濒死{targetText}";
            yield return $"令{targetText}获得{blockText}点格挡。";
        }
    }

    private sealed class CarryRelativeAllySkillStepImpl : SkillStep
    {
        private readonly int _relativeIndex;
        private readonly int _skillIndex;
        private readonly bool _dyingFilter;
        private readonly bool _describe;
        private readonly string _descriptionLine;

        public CarryRelativeAllySkillStepImpl(
            int relativeIndex,
            int skillIndex,
            bool dyingFilter,
            bool describe,
            string descriptionLine
        )
        {
            _relativeIndex = relativeIndex;
            _skillIndex = skillIndex;
            _dyingFilter = dyingFilter;
            _describe = describe;
            _descriptionLine = descriptionLine;
        }

        public override async Task Execute(Skill skill)
        {
            if (skill.OwnerCharater?.BattleNode == null)
                return;

            var target = skill.GetAllyByRelative(_relativeIndex, dyingFilter: _dyingFilter);
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
                string line = _descriptionLine;
                if (_dyingFilter && line.Contains("非濒死", StringComparison.Ordinal) == false)
                {
                    line = $"{line}（目标为非濒死）";
                }
                yield return line;
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
            if (_dyingFilter)
                relativeText = $"非濒死{relativeText}";
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
        private readonly bool _dyingFilter;
        private readonly bool _describe;
        private readonly string _descriptionLine;

        public SwapPositionFriendlySkillStep(
            int relativeIndexA,
            int relativeIndexB,
            bool dyingFilter,
            bool describe,
            string descriptionLine
        )
        {
            _relativeIndexA = relativeIndexA;
            _relativeIndexB = relativeIndexB;
            _dyingFilter = dyingFilter;
            _describe = describe;
            _descriptionLine = descriptionLine;
        }

        public override async Task Execute(Skill skill)
        {
            if (skill?.OwnerCharater?.BattleNode == null)
                return;

            var first = skill.GetAllyByRelative(_relativeIndexA, dyingFilter: _dyingFilter);
            var second = skill.GetAllyByRelative(_relativeIndexB, dyingFilter: _dyingFilter);
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
                string line = _descriptionLine;
                if (_dyingFilter && line.Contains("非濒死", StringComparison.Ordinal) == false)
                {
                    line = $"{line}（目标为非濒死）";
                }
                yield return line;
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
            if (_dyingFilter)
                firstText = $"非濒死{firstText}";

            string secondText = _relativeIndexB switch
            {
                0 => "自己",
                1 => "下一位",
                -1 => "上一位",
                > 1 => $"下{_relativeIndexB}位",
                _ => $"上{-_relativeIndexB}位",
            };
            if (_dyingFilter)
                secondText = $"非濒死{secondText}";

            yield return $"交换{firstText}与{secondText}的位置。";
        }
    }

    private sealed class EnergySkillStep : SkillStep
    {
        private readonly int _delta;

        public EnergySkillStep(int delta)
        {
            _delta = delta;
        }

        public override Task Execute(Skill skill)
        {
            if (skill.OwnerCharater == null || _delta == 0)
                return Task.CompletedTask;

            skill.OwnerCharater.UpdataEnergy(_delta);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (_delta > 0)
                yield return $"恢复{_delta}点能量。";
            else if (_delta < 0)
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
                IEnumerable<string> lines = _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return $"生效：{line}";
            }
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
        if (skill == null || target == null)
            return;

        int clamped = Math.Clamp(damage, 0, 9999);
        await skill.AttackAnimation(target);
        await target.GetHurt(clamped);
        await Task.Delay(100);
    }

    private static async Task AttackTargetDoubleStrike(Skill skill, Character target, int damage)
    {
        if (skill == null || target == null)
            return;

        int clamped = Math.Clamp(damage, 0, 9999);
        await skill.AttackAnimation(target);
        await target.GetHurt(clamped);
        await Task.Delay(100);
        if (target.State != Character.CharacterState.Normal)
            return;

        var attack2 = AttackScene.Instantiate() as AttackEffect;
        target.AddChild(attack2);
        attack2.AnimationPlayer0.Play("Attack1");
        attack2.GlobalPosition = target.GlobalPosition;
        await target.GetHurt(clamped);
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
                IEnumerable<string> lines = _onPassSteps[i].Describe(skill) ?? Array.Empty<string>();
                foreach (string line in lines.Where(x => !string.IsNullOrWhiteSpace(x)))
                    yield return $"生效：{line}";
            }
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
