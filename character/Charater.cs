using Godot;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;

[Serializable]
public partial class Charater : Node2D
{
	public enum CharaterState
	{
		Normal,
		Dying
	}
	public virtual PackedScene CharaterScene{set; get;}
	public CharaterState State = CharaterState.Normal;
	//charater basic properties
	[Export]
	public Texture2D Portrait;
	public virtual string CharaterName{get;set;}
	public int BattleLifemax;
	public int Lifemax;
	public int Life;
	public int BattlePower;
	public int Power;
	public int BattleSurvivability;
	public int Survivability;
	public int Speed;

	public int Block;
	//properties label
	public Label LifeLabel => field ??= GetNode("Life") as Label;
	public Label PowerLabel;
	public Label DefenseLabel;
	public Label SpeedLabel;
	public Label BlockLabel => field??=GetNode<Label>("Block");
	public TextureProgressBar BlockBar => field??=GetNode<TextureProgressBar>("Block/BlockBar");
	public TextureProgressBar LifeBar  => field??= GetNode<TextureProgressBar>("Life/LifeBar");
	public TextureProgressBar LifeBar2 => field??=GetNode<TextureProgressBar>("Life/LifeBar2");
	public Label PowerIconLabel => field??=GetNode<Label>("PowerIcon/Label");
	public Label SurvivabilityIconLabel => field??=GetNode<Label>("SurvivabilityIcon/Label");
	public TextureRect Hoverframe => field??=GetNode<TextureRect>("Hoverframe");
	// public Control SkillControl => field??=GetNode<Control>("SkillControl");
	//action and skill
	public Skill[] Skills;
	public int DoubleHitLayer = 1;
	
	public AnimatedSprite2D Animate1 => field??=GetNode("Effect/Effect1") as AnimatedSprite2D;
	public AnimationPlayer APlayer => field??=GetNode("Player") as AnimationPlayer;
	public Battle BattleNode ;
	
	
	public int PositionIndex;
	
	public PackedScene Number = ResourceLoader.Load<PackedScene>("res://EffectNode/Number.tscn");

	public bool IsPlayer;
	
	//buff
	public int Energe = 1;
	public List<Buff> DyingBuffs = new List<Buff>();
	public List<Buff> HurtBuffs = new List<Buff>();
	
	public virtual void Initialize()
	{
		APlayer.Play("RESET");
		//初始化清理buff
		Block = 0;
		Energe = 1;
		DyingBuffs.Clear();
		HurtBuffs.Clear();
		
		//初始化数值
		State = CharaterState.Normal;
		BattleLifemax = Lifemax;
		Life = Lifemax;
		BattlePower = Power;
		BattleSurvivability = Survivability;
		
		BlockLabel.Text = Block.ToString();
		Life = BattleLifemax;
		LifeLabel.Text = Life.ToString();
		
		LifeBar.MaxValue = BattleLifemax;
		LifeBar2.MaxValue = BattleLifemax;
		LifeBar.MinValue = 0;
		LifeBar2.MinValue = 0;
		LifeBar.Value = Life;
		LifeBar2.Value = Life;
		PowerIconLabel.Text = BattlePower.ToString();
		SurvivabilityIconLabel.Text = BattleSurvivability.ToString();

		Block = 0;
		BlockLabel.Text = Block.ToString();
		BlockBar.MaxValue = BattleLifemax;
		BlockBar.Value = 0;
		
		Hoverframe.SelfModulate = new Color(1,1,1,0);
		Hoverframe.PivotOffset = Hoverframe.Size / 2;
	}

    public override void _Ready()
    {
        Hoverframe.MouseEntered += Hover;
		Hoverframe.MouseExited += () => { Hoverframe.SelfModulate = new Color(1,1,1,0);};
    }

	public virtual void StartAction()
	{
		Block = 0;
		UpdataBlock(0);
		Energe += 1;
	}

	public virtual void GetHurt(float damage)
	{
		if (HurtBuffs != null)
			for (int i = 0; i < HurtBuffs.Count; i++)
			{
				HurtBuffs[i].HurtEffect(ref damage);
				HurtBuffs[i].Stack--;
				if(HurtBuffs[i].Stack == 0) HurtBuffs.RemoveAt(i);
			}
		
		var attacknum = Number.Instantiate<Number>();
		AddChild(attacknum);
		attacknum.Position = Position + new Vector2(0, -50f);
		attacknum.NumberLabel.Text = (-(int)damage).ToString();
		
		BattleNode.BattlePlayer.Play("hit");
		APlayer.Play("BeHurt");
		
		Life -= Math.Clamp((int)damage - Block,0,Life);
		Block = Math.Clamp(Block - (int)damage,0,99999);
		UpdataBlock(0);

		CreateTween().TweenProperty(LifeBar2, "value", Life, 0.2f);
		LifeBar.Value = Life;
		LifeLabel.Text = Life.ToString();
		
		
		if (Life <= 0)
		{
			Dying();
		}
	}

	public virtual void Recovery(int num)
	{
		Life = Math.Clamp(Life + num, 0, BattleLifemax);
		CreateTween().TweenProperty(LifeBar2, "value", Life, 0.2f);
		CreateTween().TweenProperty(LifeBar, "value", Life, 0.2f);
		LifeLabel.Text = Life.ToString();
		var numlabel = Number.Instantiate<Number>();
		AddChild(numlabel);
		numlabel.NumberLabel.Text = "+"+ num.ToString();
		numlabel.NumberLabel.AddThemeColorOverride("font_color",Colors.Green);
	}

	public virtual void Dying()
	{
		State = CharaterState.Dying;
		
		GD.Print("Dying");
		CreateTween().TweenProperty(this, "modulate", new Color(1,1,1,0), 0.5f);
		
		if(DyingBuffs != null) for (int i = 0; i < DyingBuffs.Count; i++)
		{
			DyingBuffs[i].Stack--;
			DyingBuffs[i].DyingEffect();
			if (DyingBuffs[i].Stack == 0) DyingBuffs.RemoveAt(i);
		}
	}

	public async virtual void EndAction()
	{
		await ToSignal(GetTree().CreateTimer(1), "timeout");
		BattleNode.EmitS();
	}

	public virtual void DisableSkill(){}
	public virtual void UpdateEnerge(int num)
	{
		Energe += num;
	}

	public void UpdataBlock(int num)
	{
		Block += num;
		BlockLabel.Text = Block.ToString();
		CreateTween().TweenProperty(BlockBar, "value", Block, 0.5f);
		if (num > 0)
		{
			Number number = Number.Instantiate<Number>();
			AddChild(number);
			number.NumberLabel.Text = "+" + num.ToString();
			number.NumberLabel.AddThemeColorOverride("font_color",new Color(180, 220, 255,255)/255);
		}
		
	}
	
	public virtual void Passive(Skill skill){}

	public void Hover()
	{
		Hoverframe.SelfModulate = new Color(1,1,1,1);
		Hoverframe.Size = new Vector2(0.9f, 0.9f);
		Tween tween = CreateTween();
		tween.TweenProperty(Hoverframe, "scale", new Vector2(1.1f, 1.1f), 0.1f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		
		tween.TweenProperty(Hoverframe, "scale", new Vector2(1f, 1f), 0.2f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		
		// SkillControl.Visible = true;
		// for(int i = 0; i < SkillControl.GetChildCount(); i++)
		// {
		// 	Control skillbutton = SkillControl.GetChild<Control>(i);
		// 	//skillbutton.Scale = Vector2.Zero;
		// 	skillbutton.Position = Vector2.Zero;
			
		// 	Tween skilltween = CreateTween();
		// 	float r = 75f;
		// 	skilltween.TweenProperty(skillbutton, "position", r*new Vector2((float)Math.Cos(Math.PI*2/3 * i), (float)Math.Sin(Math.PI*2/3 * i)), 0.3f)
		// 		.SetDelay(0.1f * i)
		// 		.SetTrans(Tween.TransitionType.Sine)
		// 		.SetEase(Tween.EaseType.Out);
		// }
	}
}
