using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class SummonCharacter : EnemyCharacter
{
    private const float DeathFadeDurationSeconds = 0.4f;
    public Character Summoner { get; private set; }
    public Character LastSummoner { get; private set; }

    public override bool IsSummon => true;
    public override bool IsFullCharacter => false;
    public override bool ParticipatesInTurnRotation => false;
    public override bool TriggersSkillUseEvents => false;
    public override bool ClearsBlockOnActionStart => false;
    protected override bool ResolvesTurnStartOnActionStart => true;

    public override void _Ready()
    {
        base._Ready();
        IsPlayer = Summoner?.IsPlayer ?? IsPlayer;
    }

    internal void BindToSummoner(Character summoner)
    {
        Summoner = summoner;
        LastSummoner = summoner;
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
        IntentionIndex = Skills.Length > 0 ? 0 : -1;
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

        await DisappearIntention();
        State = CharacterState.Dying;
        CreateTween().TweenProperty(this, "modulate", new Color(1, 1, 1, 0), DeathFadeDurationSeconds);

        if (DeathFadeDurationSeconds > 0f)
            await ToSignal(GetTree().CreateTimer(DeathFadeDurationSeconds), "timeout");

        if (State == CharacterState.Dying)
            BattleNode?.RemoveSummon(this);
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
        sb.Append("不参与正常轮转；召唤者行动后依次出手。");

        return sb.ToString();
    }
}
