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
    public Charater OwnerCharater;
    public bool Enable;
    public string Description;

    public Skill(SkillTypes skillType, Charater ownerCharater)
    {
        SkillType = skillType;
        OwnerCharater = ownerCharater;
    }

    public virtual async Task Effect()
    {
        OwnerCharater.DisableSkill();
        OwnerCharater.BattleNode.UsedSkills.Add(this);
    }

    public Charater[] Chosetarget1()
    {
        int index = OwnerCharater.PositionIndex;
        Charater[] targets = (OwnerCharater.IsPlayer) switch
        {
            true => OwnerCharater.BattleNode.Enemies,
            false => OwnerCharater.BattleNode.Players,
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
            .Where(x => x.State == Charater.CharaterState.Normal)
            .ToArray();
        return targets;
    }

    public async Task Attack1(float basis) //顺位一段攻击
    {
        Charater[] targets = Chosetarget1();
        if (targets.Length == 0)
            return;

        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        OwnerCharater.CAplayer.Play("release");
        attack.AnimationPlayer0.Play("Attack1");
        attack.Sprite1.GlobalPosition = targets[0].GlobalPosition;

        await Task.Delay(600);
        targets[0].GetHurt(basis + OwnerCharater.BattlePower);
    }

    public async Task Attack2(float basis) //顺位二段攻击
    {
        Charater[] targets = Chosetarget1();
        if (targets.Length == 0)
            return;

        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        OwnerCharater.CAplayer.Play("release");
        attack.AnimationPlayer0.Play("Attack1");
        attack.Sprite1.GlobalPosition = targets[0].GlobalPosition;

        await Task.Delay(600);
        targets[0].GetHurt(basis + OwnerCharater.BattlePower);
        await Task.Delay(150);
        targets[0].GetHurt(basis + OwnerCharater.BattlePower);
    }

    public async Task Attack3(float basis, Charater target, int num)
    {
        if (target == null)
            return;

        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        OwnerCharater.CAplayer.Play("release");
        attack.AnimationPlayer0.Play("Attack1");
        attack.Sprite1.GlobalPosition = target.GlobalPosition;

        await Task.Delay(600);
        for (int i = 0; i < num; i++)
        {
            target.GetHurt(basis + OwnerCharater.BattlePower);
            await Task.Delay(150);
        }
    }

    public async Task DescendingProperties(Charater target, PropertyType type, int num)
    {
        switch (type)
        {
            case PropertyType.Power:
                target.BattlePower -= num;
                break;
            case PropertyType.Survivalibility:
                target.BattleSurvivability -= num;
                GD.Print("surv", target.BattleSurvivability);
                break;
        }
        target.PowerIconLabel.Text = target.BattlePower.ToString();
        target.SurvivabilityIconLabel.Text = target.BattleSurvivability.ToString();

        Node2D descending = DescendingScene.Instantiate() as Node2D;
        OwnerCharater.BattleNode.AddChild(descending);
        descending.GlobalPosition = target.GlobalPosition + new Vector2(0, -50);
        await Task.Delay(1000);
        descending.QueueFree();
    }

    public async Task IncreaseProperties(Charater target, PropertyType type, int value)
    {
        switch (type)
        {
            case PropertyType.Power:
                target.BattlePower += value;

                break;
            case PropertyType.Survivalibility:
                target.BattleSurvivability += value;

                break;
        }

        
        target.PowerIconLabel.Text = target.BattlePower.ToString();
        target.SurvivabilityIconLabel.Text = target.BattleSurvivability.ToString();
        target.PlayAnimatedSprite(target.absorb);

    }

    public void BuffAdd(Buff.BuffName type, int stack) { }
}
