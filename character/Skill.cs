using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Skill
{
    public enum PropertyType
    {
        [Description("力量")]
        Power,

        [Description("生存")]
        Survivalibility,
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

    public Skill(SkillTypes skillType)
    {
        SkillType = skillType;
    }

    public virtual async Task Effect()
    {
        OwnerCharater.DisableSkill();
        OwnerCharater.BattleNode.UsedSkills.Add(this);
    }

    protected int OwnerPower => OwnerCharater?.BattlePower ?? 0;
    protected int OwnerSurvivability => OwnerCharater?.BattleSurvivability ?? 0;

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
            _ => "white",
        };
    }

    protected void SetDescriptionText(string text)
    {
        Description = GlobalFunction.ColorizeNumbers(text ?? string.Empty);
    }

    protected void SetDescriptionLines(params string[] lines)
    {
        string text = string.Join("\n", lines.Where(x => !string.IsNullOrWhiteSpace(x)));
        SetDescriptionText(text);
    }

    public virtual void UpdateDescription() { }

    public Character[] Chosetarget1()
    {
        int index = OwnerCharater.PositionIndex;
        Character[] targets = (OwnerCharater.IsPlayer) switch
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
        return targets;
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

    public async Task Attack3(int damage, Character target, int num)
    {
        if (target == null)
            return;

        damage = Math.Clamp(damage, 0, 9999);
        await AttackAnimation(target);

        for (int i = 0; i < num; i++)
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

    public void DescendingProperties(Character target, PropertyType type, int num)
    {
        ColorRect icon = null;
        switch (type)
        {
            case PropertyType.Power:
                target.BattlePower -= num;
                icon = target.PowerIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Survivalibility:
                target.BattleSurvivability -= num;
                icon = target.SurvivabilityIconLabel.GetParent() as ColorRect;
                break;
        }
        target.PowerIconLabel.Text = target.BattlePower.ToString();
        target.SurvivabilityIconLabel.Text = target.BattleSurvivability.ToString();

        CharacterEffect characterEffect =
            target.CharacterEffectScene.Instantiate<CharacterEffect>();
        target.AddChild(characterEffect);
        characterEffect.Animation.Play("lightning");

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = $"{GetColoredPropertyLabel(type)} -{num}";
        hint.TargetPosition = new Vector2(0, 150);
        hint.GlobalPosition = target.GlobalPosition;
        OwnerCharater.BattleNode.AddChild(hint);
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
        }

        CharacterEffect characterEffect =
            target.CharacterEffectScene.Instantiate<CharacterEffect>();
        target.AddChild(characterEffect);
        characterEffect.Animation.Play("absorb");
        target.BattleNode.BattleAnimationPlayer.Play("blue");

        target.PowerIconLabel.Text = target.BattlePower.ToString();
        target.SurvivabilityIconLabel.Text = target.BattleSurvivability.ToString();

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = $"{GetColoredPropertyLabel(type)} +{value}";
        hint.TargetPosition = new Vector2(0, 150);
        hint.TargetPosition = target.GlobalPosition + new Vector2(0, 100);
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
        switch (skillID)
        {
            case SkillID.Determination:
                return new Determination();
            case SkillID.ReNewedSpirit:
                return new ReNewedSpirit();
            case SkillID.TerminateLight:
                return new TerminateLight();
            case SkillID.Smite:
                return new Smite();
            case SkillID.Charge:
                return new Charge();
            case SkillID.SacredOnslaught:
                return new SacredOnslaught();
            case SkillID.EchonicResonance:
                return new EchonicResonance();
            case SkillID.SoundBarrier:
                return new SoundBarrier();
        }
        return null;
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
    EchonicResonance,
    SoundBarrier,
}
