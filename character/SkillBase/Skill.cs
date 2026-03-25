using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public enum PropertyType
{
    [Description("力量")]
    Power,

    [Description("生存")]
    Survivability,

    [Description("速度")]
    Speed,

    [Description("生命上限")]
    MaxLife,
}

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

        [Description("无")]
        none,
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
        if (OwnerCharater?.SkillBuffs != null)
        {
            var stun = OwnerCharater.SkillBuffs.FirstOrDefault(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Stun && x.Stack > 0
            );
            if (stun != null)
            {
                await stun.Trigger(this);
                return;
            }
        }

        OwnerCharater.DisableSkill();
        OwnerCharater.BattleNode.UsedSkills.Add(this);
        foreach (var buff in OwnerCharater.SkillBuffs)
        {
            await buff.Trigger(this);
        }
        var plan = GetPlan();
        if (plan != null)
        {
            await plan.Execute();
        }
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

    public static string GetPropertyLabel(PropertyType type) => type.GetDescription();

    public static string GetColoredPropertyLabel(PropertyType type)
    {
        return $"[color={GetPropertyColor(type)}]{GetPropertyLabel(type)}[/color]";
    }

    private static string GetPropertyColor(PropertyType type)
    {
        return type switch
        {
            PropertyType.Power => "#ff0000",
            PropertyType.Survivability => "#89fffd",
            PropertyType.Speed => "#b56bff",
            _ => "white",
        };
    }

    private static string GetStatLabel(StatX stat)
    {
        return stat switch
        {
            StatX.Power => GetPropertyLabel(PropertyType.Power),
            StatX.Survivability => GetPropertyLabel(PropertyType.Survivability),
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
            StatX.Survivability => GetPropertyColor(PropertyType.Survivability),
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

    public virtual void UpdateDescription()
    {
        var plan = GetPlan();
        if (plan != null)
        {
            SetDescriptionLines(plan.DescribeLines());
        }
    }

    public Character[] ChosetargetByOrder(bool byBehindRow = false)
    {
        if (OwnerCharater?.BattleNode == null)
            return [];

        static int Row(int pos) => pos > 0 ? (pos - 1) % 3 : 0;
        static int Col(int pos) => pos > 0 ? (pos - 1) / 3 : 0;

        int attackerRow = Row(OwnerCharater.PositionIndex);

        IEnumerable<Character> source = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.EnemiesList.Cast<Character>()
            : OwnerCharater.BattleNode.PlayersList.Cast<Character>();

        var ordered = byBehindRow
            ? source
                .Where(x => x != null)
                .OrderBy(x => Math.Abs(Row(x.PositionIndex) - attackerRow))
                .ThenBy(x => Row(x.PositionIndex))
                .ThenByDescending(x => Col(x.PositionIndex))
                .Where(x => x.State == Character.CharacterState.Normal)
                .ToArray()
            : source
                .Where(x => x != null)
                .OrderBy(x => Math.Abs(Row(x.PositionIndex) - attackerRow))
                .ThenBy(x => Row(x.PositionIndex))
                .ThenBy(x => Col(x.PositionIndex))
                .Where(x => x.State == Character.CharacterState.Normal)
                .ToArray();

        var visible = ordered
            .Where(x =>
                x.StartActionBuffs.Any(b => b.ThisBuffName == Buff.BuffName.Invisible) == false
            )
            .ToArray();

        // If everyone is invisible, still allow targeting to avoid soft-lock.
        var targets = visible.Length > 0 ? visible : ordered;

        if (targets.Any(x => x.HurtBuffs.Any(b => b.ThisBuffName == Buff.BuffName.Taunt)))
        {
            // Stable sort: taunt targets first, keep prior ordering within groups.
            targets = targets
                .OrderByDescending(target =>
                    target.HurtBuffs.Any(buff => buff.ThisBuffName == Buff.BuffName.Taunt)
                )
                .ToArray();
        }

        return targets.Length > 0 ? targets : [OwnerCharater.BattleNode.dummy];
    }

    public Character GetAllyByRelative(int Where, bool dyingFilter = false)
    {
        // 1. 获取基础列表并排序
        var query = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.PlayersList.Cast<Character>()
            : OwnerCharater.BattleNode.EnemiesList.Cast<Character>();

        if (dyingFilter)
        {
            query = query.Where(x => x.State != Character.CharacterState.Dying);
        }

        Character[] Ally = query.OrderBy(x => x.PositionIndex).ToArray();

        // 2. 安全检查：如果列表为空，直接返回 null
        if (Ally.Length == 0)
            return null;

        // 3. 获取当前角色的位置
        int currentIndex = Array.IndexOf(Ally, OwnerCharater);

        // 如果当前角色不在列表里，默认从0开始算偏移
        if (currentIndex == -1)
            currentIndex = 0;

        // 4. 【核心逻辑】处理负数：不管 Where 是多小的负数，都能转为正索引
        // 逻辑：(基础计算 % 长度 + 长度) % 长度
        int totalOffset = currentIndex + Where;
        int targetIndex = (totalOffset % Ally.Length + Ally.Length) % Ally.Length;

        return Ally[targetIndex];
    }

    public Character[] GetAllAllyWithOrder(bool dyingFilter = false)
    {
        IEnumerable<Character> query = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.PlayersList.Cast<Character>()
            : OwnerCharater.BattleNode.EnemiesList.Cast<Character>();

        if (dyingFilter)
            query = query.Where(x => x.State != Character.CharacterState.Dying);

        return query.OrderBy(x => x.PositionIndex).ToArray();
    }

    public Character GetAllyByIndex(int index, bool dyingFilter = false)
    {
        var allies = (
            OwnerCharater.IsPlayer
                ? OwnerCharater.BattleNode.PlayersList.Cast<Character>()
                : OwnerCharater.BattleNode.EnemiesList.Cast<Character>()
        )
            .OrderBy(x => x.PositionIndex)
            .ToList();

        int safeIndex = (index % allies.Count + allies.Count) % allies.Count;
        // 2. 如果开启了死亡过滤，更新这个 List
        if (dyingFilter)
        {
            while (allies[safeIndex].State == Character.CharacterState.Dying)
            {
                if (safeIndex + 1 >= allies.Count / 2.0)
                {
                    safeIndex = (safeIndex - 1) % allies.Count;
                }
                else
                {
                    safeIndex = (safeIndex + 1) % allies.Count;
                }
            }
        }

        // 4. 核心逻辑：循环取模公式，确保负数和超界都能正确指向
        // 使用 Count 属性（List已生成，Count是瞬间读取，性能极高）

        return allies[safeIndex];
    }

    public async Task SwapPositionIndex(
        Character first,
        Character second,
        float disappearDuration = 0.28f,
        float moveDuration = 0.22f,
        float appearDuration = 0.28f
    )
    {
        if (first == null || second == null || first == second)
            return;

        var battle = first.BattleNode;
        if (battle == null || second.BattleNode == null)
            return;
        if (!GodotObject.IsInstanceValid(battle) || battle != second.BattleNode)
            return;
        if (first.IsPlayer != second.IsPlayer)
            return;

        await Task.WhenAll(
            TweenSpriteProgress(first, 1f, disappearDuration),
            TweenSpriteProgress(second, 1f, disappearDuration)
        );

        int tempIndex = first.PositionIndex;
        first.PositionIndex = second.PositionIndex;
        second.PositionIndex = tempIndex;

        SwapBattleOrder(battle, first, second);
        Vector2 firstTarget = ComputeBattlePosition(first.PositionIndex, first.IsPlayer);
        Vector2 secondTarget = ComputeBattlePosition(second.PositionIndex, second.IsPlayer);
        UpdateZIndexByPosition(first);
        UpdateZIndexByPosition(second);

        await Task.WhenAll(
            TweenCharacterPosition(first, firstTarget, moveDuration),
            TweenCharacterPosition(second, secondTarget, moveDuration)
        );
        first.Position = firstTarget;
        second.Position = secondTarget;
        first.OriginalPosition = firstTarget;
        second.OriginalPosition = secondTarget;

        await Task.WhenAll(
            TweenSpriteProgress(first, 0f, appearDuration),
            TweenSpriteProgress(second, 0f, appearDuration)
        );
    }

    private static async Task TweenSpriteProgress(Character character, float target, float duration)
    {
        if (character?.Sprite == null || !GodotObject.IsInstanceValid(character.Sprite))
            return;

        if (!TryGetProgressMaterial(character.Sprite, out ShaderMaterial material))
            return;

        Tween tween = character.CreateTween();
        tween.TweenMethod(
            Callable.From<float>(value => material.SetShaderParameter("progress", value)),
            (float)material.GetShaderParameter("progress"),
            target,
            Math.Max(0f, duration)
        );
        await character.ToSignal(tween, "finished");
    }

    private static async Task TweenCharacterPosition(
        Character character,
        Vector2 target,
        float duration
    )
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return;

        Tween tween = character.CreateTween();
        tween.TweenProperty(character, "position", target, Math.Max(0f, duration));
        await character.ToSignal(tween, "finished");
    }

    private static Vector2 ComputeBattlePosition(int positionIndex, bool isPlayer)
    {
        float bGapY = 140f;
        float bGapX = 280f;
        float bSkew = 10f;
        int row = positionIndex > 0 ? (positionIndex - 1) % 3 : 0;
        int col = positionIndex > 0 ? (positionIndex - 1) / 3 : 0;
        int side = isPlayer ? -1 : 1;
        float xPos = col * bGapX * side - (row * bSkew - 100 * (row - 1));
        float yPos = row * bGapY;
        return new Vector2(xPos, yPos);
    }

    private static void UpdateZIndexByPosition(Character character)
    {
        if (character == null)
            return;

        int row = character.PositionIndex > 0 ? (character.PositionIndex - 1) % 3 : 0;
        character.ZIndex = row;
    }

    private static bool TryGetProgressMaterial(Node sprite, out ShaderMaterial material)
    {
        material = null;

        if (sprite is CanvasItem canvas && canvas.Material is ShaderMaterial canvasMaterial)
        {
            material = canvasMaterial;
            return true;
        }

        if (sprite.GetClass() == "SpineSprite")
        {
            Variant normalVariant = sprite.Get("normal_material");
            if (normalVariant.VariantType == Variant.Type.Object)
            {
                material = normalVariant.As<ShaderMaterial>();
                if (material != null)
                    return true;
            }
        }

        return false;
    }

    private static void SwapBattleOrder(Battle battle, Character first, Character second)
    {
        if (battle == null || first == null || second == null)
            return;

        if (first.IsPlayer)
        {
            var list = battle.PlayersList;
            if (first is not PlayerCharacter p1 || second is not PlayerCharacter p2)
                return;

            int firstIndex = list.IndexOf(p1);
            int secondIndex = list.IndexOf(p2);
            if (firstIndex < 0 || secondIndex < 0 || firstIndex == secondIndex)
                return;

            (list[firstIndex], list[secondIndex]) = (list[secondIndex], list[firstIndex]);
            return;
        }

        var enemyList = battle.EnemiesList;
        if (first is not EnemyCharacter e1 || second is not EnemyCharacter e2)
            return;

        int enemyFirstIndex = enemyList.IndexOf(e1);
        int enemySecondIndex = enemyList.IndexOf(e2);
        if (enemyFirstIndex < 0 || enemySecondIndex < 0 || enemyFirstIndex == enemySecondIndex)
            return;

        (enemyList[enemyFirstIndex], enemyList[enemySecondIndex]) = (
            enemyList[enemySecondIndex],
            enemyList[enemyFirstIndex]
        );
    }

    public async Task Attack1(int damage, bool byBehindRow = false) //顺位一段攻击
    {
        damage = Math.Clamp(damage, 0, 9999);
        Character[] targets = ChosetargetByOrder(byBehindRow: byBehindRow);
        if (targets.Length == 0)
            return;

        await AttackAnimation(targets[0]);

        await targets[0].GetHurt(damage);
        await Task.Delay(100);
    }

    public async Task Attack2(int damage, bool byBehindRow = false) //顺位二段攻击
    {
        damage = Math.Clamp(damage, 0, 9999);
        Character[] targets = ChosetargetByOrder(byBehindRow: byBehindRow);
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

    public async Task AOE(int damage, int Num, int times, bool byBehindRow = false)
    {
        var targets = ChosetargetByOrder(byBehindRow: byBehindRow);
        if (targets.Length == 0)
            return;

        int count = Math.Min(Num, targets.Length);
        List<Task> tasks = new();
        for (int i = 0; i < count; i++)
        {
            tasks.Add(Attack3(damage, targets[i], times));
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

    public async Task Carry(Character target, int skillIndex)
    {
        if (target.State == Character.CharacterState.Dying)
            return;
        await target.Skills[skillIndex].Effect();
    }

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
            SkillID.BreakStrike => new BreakStrike(),
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
            SkillID.SwapSlash => new SwapSlash(),
            SkillID.FinalGuard => new FinalGuard(),
            SkillID.RebirthPrayer => new RebirthPrayer(),
            SkillID.Sacrifice => new Sacrifice(),
            SkillID.ShadowAmbush => new ShadowAmbush(),
            SkillID.ShadowExecution => new ShadowExecution(),
            SkillID.VeilStep => new VeilStep(),
            SkillID.TempoSurge => new TempoSurge(),
            SkillID.LongNight => new LongNight(),
            SkillID.Vower => new Vower(),
            SkillID.FlashOfLight => new FlashOfLight(),
            SkillID.CrystalGuard => new CrystalGuard(),
            SkillID.Swift => new Swift(),
            SkillID.StarWard => new StarWard(),
            SkillID.ArmonAttack => new ArmonAttack(),
            SkillID.ArmonSurvive => new ArmonSurvive(),
            SkillID.ArmonSpecial => new ArmonSpecial(),
            SkillID.ArroganceAttack => new ArroganceAttack(),
            SkillID.ArroganceSurvive => new ArroganceSurvive(),
            SkillID.ArroganceSpecial => new ArroganceSpecial(),
            SkillID.AlienBodyAttack => new AlienBodyAttack(),
            SkillID.AlienBodySurvive => new AlienBodySurvive(),
            SkillID.AlienBodySpecial => new AlienBodySpecial(),
            SkillID.RedHuskAttack => new RedHuskAttack(),
            SkillID.RedHuskSurvive => new RedHuskSurvive(),
            SkillID.RedHuskSpecial => new RedHuskSpecial(),
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
    BreakStrike,
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
    ShadowExecution,
    VeilStep,
    TempoSurge,
    Sacrifice,
    LongNight,
    Vower,
    FlashOfLight,
    CrystalGuard,
    Swift,
    StarWard,
    ArmonAttack,
    ArmonSurvive,
    ArmonSpecial,
    ArroganceAttack,
    ArroganceSurvive,
    ArroganceSpecial,
    AlienBodyAttack,
    AlienBodySurvive,
    AlienBodySpecial,
    RedHuskAttack,
    RedHuskSurvive,
    RedHuskSpecial,
    SwapSlash,
}
