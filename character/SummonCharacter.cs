using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class SummonCharacter : Character
{
    private const float DeathFadeDurationSeconds = 0.4f;
    public Character Summoner { get; private set; }
    protected virtual float TriggerActionDelaySeconds => 0.1f;

    public override bool IsSummon => true;
    public override bool IsFullCharacter => false;
    public override bool ParticipatesInTurnRotation => false;
    public override bool CountsTowardTeamSpeed => false;
    public override bool TriggersSkillUseEvents => false;
    public override bool ClearsBlockOnActionStart => false;

    internal void BindToSummoner(Character summoner)
    {
        Summoner = summoner;
        BattleNode = summoner?.BattleNode;
        IsPlayer = summoner?.IsPlayer ?? IsPlayer;
    }

    internal void DetachFromSummoner()
    {
        Summoner = null;
    }

    public override void Initialize()
    {
        Skill onlySkill = Skills?.FirstOrDefault(x => x != null);
        Skills = onlySkill == null ? Array.Empty<Skill>() : [onlySkill];
        IsPlayer = Summoner?.IsPlayer ?? IsPlayer;
        base.Initialize();

        if (SpeedIconLabel?.GetParent() is CanvasItem speedIcon)
            speedIcon.Visible = false;
    }

    public override async void StartAction()
    {
        if (State == CharacterState.Dying)
        {
            EmitBattleNext();
            return;
        }

        if (TriggerActionDelaySeconds > 0f)
            await ToSignal(GetTree().CreateTimer(TriggerActionDelaySeconds), "timeout");
        if (State == CharacterState.Dying)
        {
            EmitBattleNext();
            return;
        }

        base.StartAction();

        Skill skill = Skills != null && Skills.Length > 0 ? Skills[0] : null;
        if (skill != null)
            await skill.Effect();

        if (!GodotObject.IsInstanceValid(this) || State == CharacterState.Dying)
        {
            EmitBattleNext();
            return;
        }

        EndAction();
    }

    public override async void EndAction()
    {
        if (EndActionBuffs != null)
        {
            foreach (var buff in EndActionBuffs.Where(x => x != null && x.Stack > 0).ToArray())
            {
                await buff.Trigger();
            }
        }

        EmitBattleNext();
        CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 0), 0.2f);
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        TrailAnimation.Stop();
    }

    public override async Task Dying(Character source = null)
    {
        await base.Dying(source);
        if (State != CharacterState.Dying)
            return;

        if (DeathFadeDurationSeconds > 0f)
            await ToSignal(GetTree().CreateTimer(DeathFadeDurationSeconds), "timeout");

        if (State == CharacterState.Dying)
            BattleNode?.RemoveSummon(this);
    }

    internal async Task DyingFromSummoner()
    {
        if (State == CharacterState.Dying)
            return;

        State = CharacterState.Dying;
        CreateTween().TweenProperty(this, "modulate", new Color(1, 1, 1, 0), DeathFadeDurationSeconds);

        if (DeathFadeDurationSeconds > 0f)
            await ToSignal(GetTree().CreateTimer(DeathFadeDurationSeconds), "timeout");

        if (State == CharacterState.Dying)
            BattleNode?.RemoveSummon(this);
    }

    private void EmitBattleNext()
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        BattleNode.EmitSignal(Battle.SignalName.Next, this);
    }

    public static string BuildPassiveDescription(
        int maxLife,
        int power,
        int survivability,
        int speed,
        params Skill[] skills
    )
    {
        var sb = new StringBuilder(128);
        sb.Append("仅在战斗中被召唤。濒死时若未触发复生则移出战斗。");
        sb.Append("\n不参与正常轮转和全阵速度；召唤者行动后依次出手。");

        var validSkills = skills?.Where(x => x != null).ToArray() ?? Array.Empty<Skill>();
        for (int i = 0; i < validSkills.Length; i++)
        {
            Skill skill = validSkills[i];
            skill.UpdateDescription();
            string desc = string.IsNullOrWhiteSpace(skill.Description) ? "-" : skill.Description;
            sb.Append($"\n技能：{skill.SkillName}。{desc}");
        }

        return sb.ToString();
    }
}
