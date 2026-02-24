using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSpecialSkill { }

public partial class RebirthPrayer : Skill
{
    private const int EnergyCost = 2;
    private const int BaseRebirthHeal = 12;
    int gainEnergy = 1;

    public RebirthPrayer()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "复苏祷告";

    public override async Task Effect()
    {
        await base.Effect();

        if (OwnerCharater?.BattleNode == null)
            return;

        var target = GetFirstDyingAlly();
        if (target == null)
        {
            HintOwner("[color=yellow]无可复活目标[/color]");
            return;
        }

        if (OwnerCharater.Energy < EnergyCost)
        {
            HintOwner($"[color=#87CEEB]Energy[/color]不足（需要{EnergyCost}）");
            return;
        }
        target.UpdataEnergy(gainEnergy);
        OwnerCharater.UpdataEnergy(-EnergyCost);
        target.Recover(BaseRebirthHeal, rebirth: true);
        await Task.Delay(150);
    }

    private Character GetFirstDyingAlly()
    {
        var allies = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.PlayersList.Cast<Character>()
            : OwnerCharater.BattleNode.EnemiesList.Cast<Character>();

        return allies
            .Where(x =>
                x != null && x != OwnerCharater && x.State == Character.CharacterState.Dying
            )
            .OrderBy(x => x.PositionIndex)
            .FirstOrDefault();
    }

    private void HintOwner(string text)
    {
        if (OwnerCharater == null)
            return;

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = text;
        hint.TargetPosition = OwnerCharater.GlobalPosition + new Vector2(0, -50);
        hint.RandomOffset = true;
        OwnerCharater.AddChild(hint);
    }

    public override void UpdateDescription()
    {
        SetDescriptionLines(
            $"消耗{EnergyCost}点能量，使1名倒下的队友复活。",
            $"并令其获得+{gainEnergy}点能量。",
            $"并回复+{BaseRebirthHeal}点生命（受目标{GetColoredPropertyLabel(PropertyType.Survivalibility)}加成）。"
        );
    }
}

public partial class Sacrifice : Skill
{
    int basisDamage = 30;
    int allyHurt = 20;
    int num => OwnerCharater.BattleNode.EnemiesList.Count;
    public override string SkillName { get; set; } = "献祭";

    public Sacrifice()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        for (int i = 0; i < OwnerCharater.BattleNode.PlayersList.Count; i++)
        {
            DescendingProperties(
                OwnerCharater.BattleNode.PlayersList[i],
                PropertyType.MaxLife,
                allyHurt
            );
        }
        await AOE(basisDamage + OwnerPower, num, 1);
    }
}
