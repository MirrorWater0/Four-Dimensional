using Godot;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class Skill
{
    public enum PropertyType
    {
        Power,
        Survivalibility
    }

    public PackedScene AttackScene = ResourceLoader.Load<PackedScene>("res://battle/Effect/AttackEffect.tscn");
    public PackedScene DescendingScene = ResourceLoader.Load<PackedScene>("res://battle/UIScene/Descending.tscn");
    public PackedScene BurnScene = ResourceLoader.Load<PackedScene>("res://battle/Effect/burn.tscn");
    public enum SkillTypes
    {
        Attack,
        Defence,
        Special
    }

    virtual public string SkillName { set; get; }
    public SkillTypes SkillType;
    public Charater OwnerCharater;
    public bool Enable;
    
    public Skill(SkillTypes skillType, Charater ownerCharater)
    {
        SkillType = skillType;
        OwnerCharater = ownerCharater;
    }

    public virtual void Effect()
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
            1 => [ 1, 4, 7, 2, 5, 8, 3, 6, 9],
            2 => [ 2, 5, 8, 1, 4, 7, 3, 6, 9],
            0 => [ 3, 6, 9, 2, 5, 8, 1, 4, 7],
        };
        
        targets = targets.OrderBy(x =>
        {
            int iindex = Array.IndexOf(id, x.PositionIndex);
            return iindex;
        }).Where(x => x.State == Charater.CharaterState.Normal).ToArray();
        return targets;
    }
    
    public async void Attack1(float rate) //顺位一段攻击
    {
        Charater[] targets = Chosetarget1();
        if(targets.Length == 0) return;
        
        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        attack.AnimationPlayer0.Play("Attack1");
        attack.Sprite1.GlobalPosition = targets[0].GlobalPosition;
        
        OwnerCharater.APlayer.Play("attack");
        await Task.Delay(600);
        targets[0].GetHurt(rate*OwnerCharater.BattlePower);
        
    }
    
    public async void Attack2(float rate)//顺位二段攻击
    {
        Charater[] targets = Chosetarget1();
        if(targets.Length == 0) return;
        
        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        attack.AnimationPlayer0.Play("Attack1");
        attack.Sprite1.GlobalPosition = targets[0].GlobalPosition;
        
        
        OwnerCharater.APlayer.Play("attack");
        await Task.Delay(600);
        targets[0].GetHurt(rate*OwnerCharater.BattlePower);
        await Task.Delay(150);
        targets[0].GetHurt(rate*OwnerCharater.BattlePower);
    }

    public async void Attack3(float rate,Charater target,int num)
    {
        if(target == null) return;
        
        AttackEffect attack = AttackScene.Instantiate() as AttackEffect;
        OwnerCharater.AddChild(attack);
        attack.AnimationPlayer0.Play("Attack1");
        attack.Sprite1.GlobalPosition = target.GlobalPosition;
        
        OwnerCharater.APlayer.Play("attack");
        await Task.Delay(600);
        for (int i = 0; i < num; i++)
        {
            target.GetHurt(rate*OwnerCharater.BattlePower);
            await Task.Delay(150);
        }
    }

    public async void DescendingProperties(Charater target,PropertyType type,int num,float rate)
    {
        switch (type)
        {
            case PropertyType.Power:;
                target.BattlePower = (int)((target.BattlePower - num)* (1-rate));
                break;
            case PropertyType.Survivalibility:;
                target.BattleSurvivability = (int)((target.BattleSurvivability - num) * (1-rate));
                break;
        }
        target.PowerIconLabel.Text = OwnerCharater.BattlePower.ToString();
        target.SurvivabilityIconLabel.Text = OwnerCharater.BattleSurvivability.ToString();

        Node2D descending = DescendingScene.Instantiate() as Node2D;
        OwnerCharater.BattleNode.AddChild(descending);
        descending.GlobalPosition = target.GlobalPosition+new Vector2(0,-50);
        await Task.Delay(1000);
        descending.QueueFree();
    }

    public async void IncreaseProperties(Charater target,PropertyType type, int value,float rate)
    {
        switch (type)
        {
            case PropertyType.Power :
                target.BattlePower += value;
                target.BattlePower =(int) (target.BattlePower* rate);
                break;
            case PropertyType.Survivalibility:
                target.BattleSurvivability += value;
                target.BattleSurvivability =(int) (target.BattleSurvivability* rate);
                break;
        }


            target.PowerIconLabel.Text = OwnerCharater.BattlePower.ToString();
            target.SurvivabilityIconLabel.Text = OwnerCharater.BattleSurvivability.ToString();
        

        Node2D burn = BurnScene.Instantiate() as Node2D;
        OwnerCharater.AddChild(burn);
        await Task.Delay(1000);
        burn.QueueFree();
    }
}
