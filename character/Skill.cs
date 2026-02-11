using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Skill
{
    public enum PropertyType
    {
        Power,
        Survivalibility,
    }

    public PackedScene AttackScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/AttackEffect.tscn"
    );
    public PackedScene DescendingScene = ResourceLoader.Load<PackedScene>(
        "res://battle/UIScene/Descending.tscn"
    );
    public PackedScene BurnScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/burn.tscn"
    );

    public enum SkillTypes
    {
        Attack,
        Defence,
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

    public async Task Attack1(float basis) //顺位一段攻击
    {
        Character[] targets = Chosetarget1();
        if (targets.Length == 0)
            return;

        await AttackAnimation(targets[0]);

        await targets[0].GetHurt(basis + OwnerCharater.BattlePower);
        await Task.Delay(200);
    }

    public async Task Attack2(float basis) //顺位二段攻击
    {
        Character[] targets = Chosetarget1();
        if (targets.Length == 0)
            return;
        await AttackAnimation(targets[0]);
        await targets[0].GetHurt(basis + OwnerCharater.BattlePower);
        await Task.Delay(100);

        // Only apply second hit if target is still alive
        if (targets[0].State == Character.CharacterState.Normal)
        {
            var attack2 = AttackScene.Instantiate() as AttackEffect;
            targets[0].AddChild(attack2);
            attack2.AnimationPlayer0.Play("Attack1");
            attack2.GlobalPosition = targets[0].GlobalPosition;
            await targets[0].GetHurt(basis + OwnerCharater.BattlePower);
        }
    }

    public async Task Attack3(float basis, Character target, int num)
    {
        if (target == null)
            return;

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
            await target.GetHurt(basis + OwnerCharater.BattlePower);
            await Task.Delay(150);
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

    public async Task DescendingProperties(Character target, PropertyType type, int num)
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

        Node2D descending = DescendingScene.Instantiate() as Node2D;
        OwnerCharater.BattleNode.AddChild(descending);
        descending.GlobalPosition = target.GlobalPosition + new Vector2(0, -50);

        if (icon.PivotOffset == Vector2.Zero)
            icon.PivotOffset = icon.Size / 2;
        icon.Scale = new Vector2(2, 2);
        icon.Modulate = new Color(1, 1, 1, 0.3f);
        icon.CreateTween()
            .TweenProperty(target.PowerIconLabel, "modulate", new Color(1, 1, 1, 1), 0.5f);
        icon.CreateTween().TweenProperty(icon, "scale", new Vector2(1, 1), 0.5f);
        descending.QueueFree();
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

        Buff.GhostExplode(icon, new Vector2(2f, 2f));
    }

    public void BuffAdd(Buff.BuffName type, int stack) { }

    static public Skill GetSkill(SkillID skillID)
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
}