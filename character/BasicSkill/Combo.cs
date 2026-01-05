using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;

public partial class Combo : Skill
{
	public Combo(Charater owner):base(Skill.SkillTypes.Attack,owner){}
	public override string SkillName { set; get; } = "回响时刻";
	public async override void Effect()
	{
		base.Effect();
		
		int i = 1;
		Attack1(0.9f + i*0.1f);
		if (OwnerCharater.Energe > 0)
		{
			await Task.Delay(1500);
			OwnerCharater.UpdataEnerge(-1);
			Effect();
		}
		else
		{
			OwnerCharater.EndAction();
		}
		
	}
}
