using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Skill
{
    private int _previewPower;
    private int _previewSurvivability;
    private int _previewEnergy = 1;

    protected const string UnfixedPlaceholder = "x";
    protected const int TooltipTotalMax = 999;

    protected enum StatX
    {
        Power,
        Survivability,
        Speed,
        Energy,
        Life,
        MaxLife,
    }

    public enum PropertyType
    {
        [Description("力量")]
        Power,

        [Description("生存")]
        Survivalibility,

        [Description("速度")]
        Speed,

        [Description("生命上限")]
        MaxLife,
    }

    public static PackedScene AttackScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/AttackEffect.tscn"
    );
    public static PackedScene BurnScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/burn.tscn"
    );

    public enum SkillTypes
    {
        [Description("攻击")]
        Attack,

        [Description("生存")]
        Survive,

        [Description("特殊")]
        Special,
    }

    public virtual string SkillName { set; get; }
    public SkillTypes SkillType;
    public Character OwnerCharater;
    public bool Enable;
    public string Description;
    public bool Upgraded = false;

    public Skill(SkillTypes skillType)
    {
        SkillType = skillType;
    }

    public virtual async Task Effect()
    {
        OwnerCharater.DisableSkill();
        OwnerCharater.BattleNode.UsedSkills.Add(this);
    }

    /// <summary>
    /// For non-battle usage (e.g. previews), set preview stats so UpdateDescription can work without a Character instance.
    /// </summary>
    public void SetPreviewStats(int power, int survivability, int energy = 1)
    {
        _previewPower = power;
        _previewSurvivability = survivability;
        _previewEnergy = energy;
    }

    protected int OwnerPower => OwnerCharater != null ? OwnerCharater.BattlePower : _previewPower;
    protected int OwnerSurvivability =>
        OwnerCharater != null ? OwnerCharater.BattleSurvivability : _previewSurvivability;
    protected int OwnerEnergy => OwnerCharater?.Energy ?? _previewEnergy;
    protected bool IsInBattle => OwnerCharater?.BattleNode != null;

    protected static string GetPropertyLabel(PropertyType type) => type.GetDescription();

    protected static string GetColoredPropertyLabel(PropertyType type)
    {
        return $"[color={GetPropertyColor(type)}]{GetPropertyLabel(type)}[/color]";
    }

    private static string GetPropertyColor(PropertyType type)
    {
        return type switch
        {
            PropertyType.Power => "#ff0000",
            PropertyType.Survivalibility => "#89fffd",
            PropertyType.Speed => "#b56bff",
            _ => "white",
        };
    }

    private static string GetStatLabel(StatX stat)
    {
        return stat switch
        {
            StatX.Power => GetPropertyLabel(PropertyType.Power),
            StatX.Survivability => GetPropertyLabel(PropertyType.Survivalibility),
            StatX.Speed => "速度",
            StatX.Energy => "能量",
            StatX.Life => "生命",
            StatX.MaxLife => "最大生命",
            _ => string.Empty,
        };
    }

    private static string GetStatColor(StatX stat)
    {
        return stat switch
        {
            StatX.Power => GetPropertyColor(PropertyType.Power),
            StatX.Survivability => GetPropertyColor(PropertyType.Survivalibility),
            StatX.Speed => "#b56bff",
            StatX.Energy => "#5353ff",
            StatX.Life => "#6bff6b",
            StatX.MaxLife => "#6bff6b",
            _ => "white",
        };
    }

    protected void SetDescriptionText(string text)
    {
        string output = GlobalFunction.ColorizeNumbers(text ?? string.Empty);
        Description = GlobalFunction.ColorizeKeywords(output);
    }

    protected void SetDescriptionLines(params string[] lines)
    {
        var filtered = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        string text = string.Join("\n", filtered);
        SetDescriptionText(text);
    }

    protected static string X(StatX stat)
    {
        string label = GetStatLabel(stat);
        if (string.IsNullOrWhiteSpace(label))
            return UnfixedPlaceholder;

        string color = GetStatColor(stat);
        return $"[color={color}]{UnfixedPlaceholder}({label})[/color]";
    }

    protected static string FormatBasePlusX(int baseValue, StatX stat, int xMultiplier = 1)
    {
        string x = X(stat);
        string xPart = xMultiplier switch
        {
            1 => x,
            -1 => $"-{x}",
            _ => $"{xMultiplier}{x}",
        };

        if (baseValue == 0)
            return xPart;

        if (xPart.StartsWith("-", StringComparison.Ordinal))
            return $"{baseValue}{xPart}";

        return $"{baseValue}+{xPart}";
    }

    protected string WithBattleTotal(string basisText, int total, int clampMax = TooltipTotalMax)
    {
        if (!IsInBattle)
            return basisText;

        int clamped = Math.Clamp(total, 0, clampMax);
        return $"{basisText}（总计：{clamped}）";
    }

    protected string WithBattleTotal(string basisText, string totalText)
    {
        if (!IsInBattle)
            return basisText;

        return $"{basisText}（总计：{totalText}）";
    }

    protected string XWithBattleTotal(StatX stat, int total, int clampMax = TooltipTotalMax) =>
        WithBattleTotal(X(stat), total, clampMax);

    protected string BasePlusXWithBattleTotal(
        int baseValue,
        int total,
        StatX stat,
        int xMultiplier = 1,
        int clampMax = TooltipTotalMax
    ) => WithBattleTotal(FormatBasePlusX(baseValue, stat, xMultiplier), total, clampMax);

    public virtual void UpdateDescription() { }

    public Character[] Chosetarget1()
    {
        int index = OwnerCharater.PositionIndex;
        Character[] targets = OwnerCharater.IsPlayer switch
        {
            true => OwnerCharater.BattleNode.EnemiesList.Cast<Character>().ToArray(),
            false => OwnerCharater.BattleNode.PlayersList.Cast<Character>().ToArray(),
        };

        int[] id = (index % 3) switch
        {
            1 => [1, 4, 7, 2, 5, 8, 3, 6, 9],
            2 => [2, 5, 8, 1, 4, 7, 3, 6, 9],
            0 => [3, 6, 9, 2, 5, 8, 1, 4, 7],
        };

        targets = targets
            .OrderBy(x =>
            {
                int iindex = Array.IndexOf(id, x.PositionIndex);
                return iindex;
            })
            .Where(x => x.State == Character.CharacterState.Normal)
            .ToArray();

        var visibleTargets = targets
            .Where(x =>
                x.StartActionBuffs.Any(b => b.ThisBuffName == Buff.BuffName.Invisible) == false
            )
            .ToArray();

        // If everyone is invisible, still allow targeting to avoid soft-lock.
        targets = visibleTargets.Length > 0 ? visibleTargets : targets;

        if (targets.Any(x => x.HurtBuffs.Any(x => x.ThisBuffName == Buff.BuffName.Taunt)))
        {
            targets = targets
                .OrderByDescending(target =>
                    target.HurtBuffs.Any(buff => buff.ThisBuffName == Buff.BuffName.Taunt)
                )
                .ToArray();
        }
        return targets;
    }

    public Character GetAllyByRelative(int Where, bool dyingFilter = false)
    {
        Character[] Ally = OwnerCharater.IsPlayer switch
        {
            true => OwnerCharater.BattleNode.PlayersList.Cast<Character>().ToArray(),
            false => OwnerCharater.BattleNode.EnemiesList.Cast<Character>().ToArray(),
        };
        Ally = Ally.OrderBy(x => x.PositionIndex).ToArray();
        return Ally[(Array.IndexOf(Ally, OwnerCharater) + Where) % Ally.Length];
    }

    public Character GetAllyByIndex(int index, bool dyingFilter = false)
    {
        var allies = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.PlayersList.Cast<Character>()
            : OwnerCharater.BattleNode.EnemiesList.Cast<Character>();

        allies = allies.OrderBy(x => x.PositionIndex);
        var ally = allies.ElementAtOrDefault(index);
        if (dyingFilter)
        {
            var aliveAlly = allies.Where(x => x.State == Character.CharacterState.Normal);
            ally = aliveAlly.ElementAtOrDefault(index % aliveAlly.Count());
        }

        return ally;
    }

    public async Task Attack1(int damage) //顺位一段攻击
    {
        damage = Math.Clamp(damage, 0, 9999);
        Character[] targets = Chosetarget1();
        if (targets.Length == 0)
            return;

        await AttackAnimation(targets[0]);

        await targets[0].GetHurt(damage);
        await Task.Delay(100);
    }

    public async Task Attack2(int damage) //顺位二段攻击
    {
        damage = Math.Clamp(damage, 0, 9999);
        Character[] targets = Chosetarget1();
        if (targets.Length == 0)
            return;
        await AttackAnimation(targets[0]);
        await targets[0].GetHurt(damage);
        await Task.Delay(100);
        // Only apply second hit if target is still alive
        if (targets[0].State == Character.CharacterState.Normal)
        {
            var attack2 = AttackScene.Instantiate() as AttackEffect;
            targets[0].AddChild(attack2);
            attack2.AnimationPlayer0.Play("Attack1");
            attack2.GlobalPosition = targets[0].GlobalPosition;
            await targets[0].GetHurt(damage);
        }
    }

    public async Task Attack3(int damage, Character target, int times)
    {
        if (target == null)
            return;

        damage = Math.Clamp(damage, 0, 9999);
        await AttackAnimation(target);

        for (int i = 0; i < times; i++)
        {
            // Stop attacking if target has died
            if (target.State != Character.CharacterState.Normal)
                break;
            var attack = AttackScene.Instantiate() as AttackEffect;
            target.AddChild(attack);
            attack.AnimationPlayer0.Play("Attack1");
            attack.GlobalPosition = target.GlobalPosition;
            await target.GetHurt(damage);
            await Task.Delay(100);
        }
    }

    public async Task AOE(int damage, int Num, int times)
    {
        List<Task> tasks = new();
        for (int i = 0; i < Num; i++)
        {
            tasks.Add(Attack3(damage, Chosetarget1()[i], times));
        }
        await Task.WhenAll(tasks);
    }

    public async Task AttackAnimation(Character target)
    {
        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        var effect = OwnerCharater.CharacterEffectScene.Instantiate() as CharacterEffect;
        OwnerCharater.AddChild(effect);
        effect.Animation.Play("explode");
        await effect.ToSignal(effect.Animation, "animation_finished");
        attack.AnimationPlayer0.Play("Attack1");
        attack.GlobalPosition = target.GlobalPosition;
    }

    public void DescendingProperties(Character target, PropertyType type, int value)
    {
        if (target == null)
            return;

        if (value > 0 && SpecialBuff.TryConsumeDebuffImmunity(target))
        {
            var immunityHint = Buff.HintScene.Instantiate<BuffHintLabel>();
            immunityHint.Text =
                $"{Buff.BuffName.DebuffImmunity.GetDescription()} [color=yellow]抵消[/color]";
            immunityHint.TargetPosition = target.GlobalPosition + new Vector2(0, 150);
            immunityHint.RandomOffset = true;
            target.AddChild(immunityHint);
            return;
        }

        ColorRect icon = null;
        switch (type)
        {
            case PropertyType.Power:
                target.BattlePower -= value;
                icon = target.PowerIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Survivalibility:
                target.BattleSurvivability -= value;
                icon = target.SurvivabilityIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Speed:
                target.Speed -= value;
                icon = target.SpeedIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.MaxLife:
                target.BattleMaxLife -= value;
                target.LifeLabel.Text = $"{target.Life}/{target.BattleMaxLife}";
                target
                    .CreateTween()
                    .TweenMethod(
                        Callable.From(
                            (int x) =>
                            {
                                target.LifeBar.MaxValue = x;
                            }
                        ),
                        target.LifeBar.MaxValue,
                        target.BattleMaxLife,
                        0.5f
                    );
                if (target.Life > target.BattleMaxLife)
                    _ = target.GetHurt(target.BattleMaxLife - target.Life);
                break;
        }
        target.PowerIconLabel.Text = target.BattlePower.ToString();
        target.SurvivabilityIconLabel.Text = target.BattleSurvivability.ToString();
        target.SpeedIconLabel.Text = target.Speed.ToString();

        CharacterEffect characterEffect =
            target.CharacterEffectScene.Instantiate<CharacterEffect>();
        target.AddChild(characterEffect);
        characterEffect.Animation.Play("lightning");

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = $"{GetColoredPropertyLabel(type)} -{value}";
        hint.TargetPosition = target.GlobalPosition + new Vector2(0, 150);
        hint.RandomOffset = true;
        target.AddChild(hint);
        Buff.GhostExplode(icon, new Vector2(2f, 2f));
    }

    public void IncreaseProperties(Character target, PropertyType type, int value)
    {
        ColorRect icon = null;
        switch (type)
        {
            case PropertyType.Power:
                target.BattlePower += value;
                icon = target.PowerIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Survivalibility:
                target.BattleSurvivability += value;
                icon = target.SurvivabilityIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Speed:
                target.Speed += value;
                icon = target.SpeedIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.MaxLife:
                target.BattleMaxLife += value;
                target.LifeLabel.Text = $"{target.Life}/{target.BattleMaxLife}";
                target
                    .CreateTween()
                    .TweenMethod(
                        Callable.From(
                            (int x) =>
                            {
                                target.LifeBar.MaxValue = x;
                            }
                        ),
                        target.LifeBar.MaxValue,
                        target.BattleMaxLife,
                        0.5f
                    );
                break;
        }

        CharacterEffect characterEffect =
            target.CharacterEffectScene.Instantiate<CharacterEffect>();
        target.AddChild(characterEffect);
        characterEffect.Animation.Play("absorb");
        target.BattleNode.BattleAnimationPlayer.Play("blue");

        target.PowerIconLabel.Text = target.BattlePower.ToString();
        target.SurvivabilityIconLabel.Text = target.BattleSurvivability.ToString();
        target.SpeedIconLabel.Text = target.Speed.ToString();

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = $"{GetColoredPropertyLabel(type)} +{value}";
        hint.TargetPosition = target.GlobalPosition + new Vector2(0, 150);
        hint.RandomOffset = true;
        target.AddChild(hint);
        Buff.GhostExplode(icon, new Vector2(2f, 2f));
    }

    public async Task Carry(Character target, int skillIndex)
    {
        if (target.State == Character.CharacterState.Dying)
            return;
        await target.Skills[skillIndex].Effect();
    }

    public void BuffAdd(Buff.BuffName type, int stack) { }

    public static Skill GetSkill(SkillID skillID)
    {
        return skillID switch
        {
            SkillID.Determination => new Determination(),
            SkillID.ReNewedSpirit => new ReNewedSpirit(),
            SkillID.TerminateLight => new TerminateLight(),
            SkillID.Smite => new Smite(),
            SkillID.Charge => new Charge(),
            SkillID.DeSurviveSkill => new ShockWave(),
            SkillID.SacredOnslaught => new SacredOnslaught(),
            SkillID.ResonantSlash => new ResonantSlash(),
            SkillID.EchoPuncture => new EchoPuncture(),
            SkillID.EchonicResonance => new EchonicResonance(),
            SkillID.SonicBoom => new SonicBoom(),
            SkillID.PhaseEcho => new PhaseEcho(),
            SkillID.SoundBarrier => new SoundBarrier(),
            SkillID.SonicDeflection => new SonicDeflection(),
            SkillID.TuningStance => new TuningStance(),
            SkillID.ResonantWard => new ResonantWard(),
            SkillID.EvilAttack => new EvilAttack(),
            SkillID.EvilSurvive => new EvilSurvive(),
            SkillID.EvilTermin => new EvilTermin(),
            SkillID.ShockWave => new ShockWave(),
            SkillID.AbsouluteDefense => new AbsouluteDefense(),
            SkillID.TauntingGuard => new TauntingGuard(),
            SkillID.HolySeal => new HolySeal(),
            SkillID.FearWormAttack => new FearWormAttack(),
            SkillID.FearWormSurvive => new FearWormSurvive(),
            SkillID.FearWormTermin => new FearWormTermin(),
            SkillID.MendSlash => new MendSlash(),
            SkillID.FinalGuard => new FinalGuard(),
            SkillID.RebirthPrayer => new RebirthPrayer(),
            SkillID.ShadowAmbush => new ShadowAmbush(),
            SkillID.VeilStep => new VeilStep(),
            SkillID.TempoSurge => new TempoSurge(),
            _ => null,
        };
    }
}

public enum SkillID
{
    Determination,
    ReNewedSpirit,
    TerminateLight,
    Smite,
    Charge,
    SacredOnslaught,
    ResonantSlash,
    EchoPuncture,
    EchonicResonance,
    SonicBoom,
    PhaseEcho,
    SoundBarrier,
    SonicDeflection,
    TuningStance,
    ResonantWard,
    EvilAttack,
    EvilSurvive,
    EvilTermin,
    DeSurviveSkill,
    ShockWave,
    AbsouluteDefense,
    TauntingGuard,
    HolySeal,
    FearWormAttack,
    FearWormSurvive,
    FearWormTermin,
    MendSlash,
    FinalGuard,
    RebirthPrayer,
    ShadowAmbush,
    VeilStep,
    TempoSurge,
}
