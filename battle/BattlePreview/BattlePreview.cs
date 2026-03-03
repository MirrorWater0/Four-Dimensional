using System;
using System;
using System.Linq;
using System.Text;
using Godot;

public partial class BattlePreview : Control
{
    public LevelNode WhichNode;
    public GridContainer PlayerFormation =>
        field ??= GetNode<GridContainer>("HBoxContainer/PlayerFormation");
    public GridContainer EnemyFormation =>
        field ??= GetNode<GridContainer>("HBoxContainer/EnemyFormation");
    public Button StartBattleButton => field ??= GetNode<Button>("StartBattle");
    private Control FormationContainer => field ??= GetNode<Control>("HBoxContainer");
    private ColorRect BackgroundPanel => field ??= GetNode<ColorRect>("Panel");
    private Control TitleMain => field ??= GetNode<Control>("TitleMain");
    private Control TitleSub => field ??= GetNode<Control>("TitleSub");
    private Control TitleLineLeft => field ??= GetNode<Control>("TitleLineLeft");
    private Control TitleLineRight => field ??= GetNode<Control>("TitleLineRight");
    private Control PlayerFrame => field ??= GetNode<Control>("PlayerFrame");
    private Control EnemyFrame => field ??= GetNode<Control>("EnemyFrame");
    private Control VsLabel => field ??= GetNode<Control>("VSLabel");
    private Control PlayerSpeedPanel => field ??= GetNode<Control>("PlayerSpeedPanel");
    private Control EnemySpeedPanel => field ??= GetNode<Control>("EnemySpeedPanel");
    private RichTextLabel PlayerSpeedLabel =>
        field ??= GetNode<RichTextLabel>("PlayerSpeedPanel/PlayerSpeedLabel");
    private RichTextLabel EnemySpeedLabel =>
        field ??= GetNode<RichTextLabel>("EnemySpeedPanel/EnemySpeedLabel");
    ColorRect tex => field ??= StartBattleButton.GetNode<ColorRect>("BG");
    ExitButton exitButton => field ??= GetNode<ExitButton>("/root/Map/UI/ExitButton");
    Map MapNode => field ??= GetNode<Map>("/root/Map");
    public int RandomNum;
    public static System.Collections.Generic.Dictionary<int, int> remapEnemy { get; } =
        new System.Collections.Generic.Dictionary<int, int>()
        {
            // 第一行 (子节点 0, 1, 2)
            [7] = 3, // 对应子节点 0
            [8] = 6, // 对应子节点 1
            [9] = 9, // 对应子节点 2

            // 第二行 (子节点 3, 4, 5)
            [4] = 2, // 对应子节点 3
            [5] = 5, // 对应子节点 4
            [6] = 8, // 对应子节点 5

            // 第三行 (子节点 6, 7, 8)
            [1] = 1, // 对应子节点 6
            [2] = 4, // 对应子节点 7
            [3] = 7, // 对应子节点 8
        };

    private Tip SkillTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/Tip");
    private Tip PropertyTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/BuffTip");
    private bool _isTransitioning;
    private readonly System.Collections.Generic.Dictionary<Control, Vector2> _basePositions = [];

    private readonly struct AssemblyItem(Control control, Vector2 offset, float delay)
    {
        public Control Control { get; } = control;
        public Vector2 Offset { get; } = offset;
        public float Delay { get; } = delay;
    }

    public override void _Ready()
    {
        EnsureTipLayer();
        exitButton.PressedActions.Add(Close);
        Modulate = Modulate with { A = 0.0f };
        SetControlAlpha(BackgroundPanel, 0.0f);
        SetPortraitPostion();
        CacheAssemblyBasePositions();
        UpdateBrushButtonMaterialSize();
        tex.Resized += UpdateBrushButtonMaterialSize;
        StartBattleButton.Pressed += StartBattle;
        StartBattleButton.MouseEntered += () =>
        {
            StartBattleButton.Modulate = 2 * new Color(1, 1, 1, 1);

            tex.PivotOffset = tex.Size / 2;
            Tween tween = CreateTween();
            tween.TweenProperty(tex, "scale", new Vector2(1.2f, 1.2f), 0.2f);
            GlobalFunction.TweenShader(tex, "cut_x", 0.4f, 0.2f);
            GlobalFunction.TweenShader(tex, "cut_y", 0.4f, 0.2f);
        };
        StartBattleButton.MouseExited += () =>
        {
            StartBattleButton.Modulate = new Color(1, 1, 1, 1);
            tex.PivotOffset = tex.Size / 2;
            Tween tween = CreateTween();
            tween.TweenProperty(tex, "scale", new Vector2(1f, 1f), 0.2f);
            GlobalFunction.TweenShader(tex, "cut_x", 0.6f, 0.2f);
            GlobalFunction.TweenShader(tex, "cut_y", 0.6f, 0.2f);
        };
    }

    private void UpdateBrushButtonMaterialSize()
    {
        if (tex?.Material is ShaderMaterial material)
            material.SetShaderParameter("rect_size_px", tex.Size);
    }

    public async void StartAnimation()
    {
        if (_basePositions.Count == 0)
            CacheAssemblyBasePositions();
        await PlayAssembleAnimationAsync();
    }

    public async System.Threading.Tasks.Task PlayCloseAnimationAsync()
    {
        while (_isTransitioning)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _isTransitioning = true;
        try
        {
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.SetEase(Tween.EaseType.In);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(this, "modulate:a", 0.0f, 0.24f);
            tween.TweenProperty(BackgroundPanel, "modulate:a", 0.0f, 0.2f);

            foreach (var item in GetAssemblyItems())
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                tween.TweenProperty(item.Control, "position", basePos + item.Offset * 0.75f, 0.2f);
                tween.TweenProperty(item.Control, "modulate:a", 0.0f, 0.18f);
            }

            await ToSignal(tween, Tween.SignalName.Finished);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async System.Threading.Tasks.Task PlayAssembleAnimationAsync()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        try
        {
            Modulate = Modulate with { A = 0.0f };
            SetControlAlpha(BackgroundPanel, 0.0f);

            var items = GetAssemblyItems();
            foreach (var item in items)
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                item.Control.Position = basePos + item.Offset;
                SetControlAlpha(item.Control, 0.0f);
            }

            var tween = CreateTween();
            tween.SetParallel(true);
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);
            tween.TweenProperty(this, "modulate:a", 1.0f, 0.28f);
            tween.TweenProperty(BackgroundPanel, "modulate:a", 1.0f, 0.34f);

            foreach (var item in items)
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                tween.TweenProperty(item.Control, "position", basePos, 0.3f).SetDelay(item.Delay);
                tween.TweenProperty(item.Control, "modulate:a", 1.0f, 0.26f).SetDelay(item.Delay);
            }

            await ToSignal(tween, Tween.SignalName.Finished);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private void CacheAssemblyBasePositions()
    {
        _basePositions.Clear();
        foreach (var item in GetAssemblyItems())
        {
            if (item.Control == null)
                continue;
            _basePositions[item.Control] = item.Control.Position;
        }
    }

    private AssemblyItem[] GetAssemblyItems()
    {
        return
        [
            new AssemblyItem(TitleMain, new Vector2(0f, -28f), 0.00f),
            new AssemblyItem(TitleSub, new Vector2(0f, -22f), 0.03f),
            new AssemblyItem(TitleLineLeft, new Vector2(-60f, 0f), 0.06f),
            new AssemblyItem(TitleLineRight, new Vector2(60f, 0f), 0.06f),
            new AssemblyItem(PlayerFrame, new Vector2(-90f, 20f), 0.08f),
            new AssemblyItem(EnemyFrame, new Vector2(90f, 20f), 0.08f),
            new AssemblyItem(FormationContainer, new Vector2(0f, 32f), 0.12f),
            new AssemblyItem(VsLabel, new Vector2(0f, 24f), 0.16f),
            new AssemblyItem(PlayerSpeedPanel, new Vector2(-70f, 18f), 0.2f),
            new AssemblyItem(EnemySpeedPanel, new Vector2(70f, 18f), 0.2f),
            new AssemblyItem(StartBattleButton, new Vector2(0f, 40f), 0.24f),
        ];
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        if (control == null)
            return;
        control.Modulate = control.Modulate with { A = alpha };
    }

    private void EnsureTipLayer()
    {
        var root = GetTree().Root;
        var existingLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");

        if (existingLayer == null)
        {
            existingLayer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.CallDeferred(Node.MethodName.AddChild, existingLayer);
        }

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return;

        if (!existingLayer.HasNode("Tip"))
        {
            var tip = tipScene.Instantiate<Tip>();
            tip.Name = "Tip";
            tip.FollowMouse = true;
            tip.AnchorOffset = new Vector2(20f, 20f);
            existingLayer.AddChild(tip);
        }

        if (!existingLayer.HasNode("BuffTip"))
        {
            var buffTip = tipScene.Instantiate<Tip>();
            buffTip.Name = "BuffTip";
            buffTip.FollowMouse = true;
            buffTip.AnchorOffset = new Vector2(-20f, 20f);
            existingLayer.AddChild(buffTip);
        }
    }

    public void SetPortraitPostion()
    {
        ClearGrid();
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var portrait = BattleReady.PortaitScene.Instantiate() as PortaitFrame;
            portrait.PortaitRect.Texture = GD.Load<Texture2D>(
                GameInfo.PlayerCharacters[i].PortaitPath
            );
            portrait.PortaitIndex = i;
            var positionindex = GameInfo.PlayerCharacters[i].PositionIndex;

            var tips = BuildPlayerPortraitTips(i);
            if (tips != null)
                WirePortraitTooltips(portrait, tips.Value.skillText, tips.Value.propertyText);

            PlayerFormation.GetChild(BattleReady.remap[positionindex] - 1).AddChild(portrait);
        }

        for (int i = 0; i < WhichNode.EnemiesRegeditList.Count; i++)
        {
            var portrait = BattleReady.PortaitScene.Instantiate() as PortaitFrame;
            portrait.PortaitRect.Texture = GD.Load<Texture2D>(
                WhichNode.EnemiesRegeditList[i].PortaitPath
            );
            portrait.PortaitIndex = i;
            var positionindex = WhichNode.EnemiesRegeditList[i].PositionIndex;

            var tips = BuildEnemyPortraitTips(i);
            if (tips != null)
                WirePortraitTooltips(portrait, tips.Value.skillText, tips.Value.propertyText);

            EnemyFormation.GetChild(remapEnemy[positionindex] - 1).AddChild(portrait);
        }

        UpdateSpeedSummary();
    }

    private void UpdateSpeedSummary()
    {
        int playerTotal = CalculatePlayerTotalSpeed();
        int enemyTotal = CalculateEnemyTotalSpeed();

        SetSpeedLabel(PlayerSpeedLabel, "我方总速度", playerTotal);
        SetSpeedLabel(EnemySpeedLabel, "敌方总速度", enemyTotal);
    }

    private static void SetSpeedLabel(RichTextLabel label, string title, int total)
    {
        if (label == null)
            return;

        string text = $"{title} {total}";
        text = GlobalFunction.ColorizeNumbers(text);
        label.Text = text;
    }

    private static int CalculatePlayerTotalSpeed()
    {
        if (GameInfo.PlayerCharacters == null)
            return 0;

        int sum = 0;
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            int bonus = SumEquipmentBonus(info, x => x.Speed);
            sum += info.Speed + bonus;
        }

        return sum;
    }

    private int CalculateEnemyTotalSpeed()
    {
        if (WhichNode?.EnemiesRegeditList == null)
            return 0;

        int sum = 0;
        for (int i = 0; i < WhichNode.EnemiesRegeditList.Count; i++)
        {
            var regedit = WhichNode.EnemiesRegeditList[i];
            if (regedit == null)
                continue;
            sum += regedit.Speed;
        }

        return sum;
    }

    private (string skillText, string propertyText)? BuildPlayerPortraitTips(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= GameInfo.PlayerCharacters.Length)
            return null;

        var info = GameInfo.PlayerCharacters[characterIndex];
        string name = GuessNameFromScenePath(info.CharacterScenePath);

        string skillText = BuildPlayerSkillText(info, name);
        string propertyText = BuildPlayerPropertyText(info, name);
        return (skillText, propertyText);
    }

    private (string skillText, string propertyText)? BuildEnemyPortraitTips(int enemyIndex)
    {
        if (WhichNode?.EnemiesRegeditList == null)
            return null;
        if (enemyIndex < 0 || enemyIndex >= WhichNode.EnemiesRegeditList.Count)
            return null;

        var regedit = WhichNode.EnemiesRegeditList[enemyIndex];
        if (regedit == null)
            return null;

        string skillText = BuildEnemySkillText(regedit);
        string propertyText = BuildEnemyPropertyText(regedit);
        return (skillText, propertyText);
    }

    private static string BuildPlayerPropertyText(PlayerInfoStructure info, string name)
    {
        var sb = new StringBuilder(128);
        int lifeBonus = SumEquipmentBonus(info, x => x.MaxLife);
        int powerBonus = SumEquipmentBonus(info, x => x.Power);
        int surviveBonus = SumEquipmentBonus(info, x => x.Survivability);
        int speedBonus = SumEquipmentBonus(info, x => x.Speed);

        sb.Append($"[b]{name}[/b]\n");
        sb.Append($"生命：{info.LifeMax + lifeBonus}（{FormatSigned(lifeBonus)}）\n");
        sb.Append($"力量：{info.Power + powerBonus}（{FormatSigned(powerBonus)}）\n");
        sb.Append($"生存：{info.Survivability + surviveBonus}（{FormatSigned(surviveBonus)}）\n");
        sb.Append($"速度：{info.Speed + speedBonus}（{FormatSigned(speedBonus)}）\n");

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private static int SumEquipmentBonus(PlayerInfoStructure info, Func<Equipment, int> selector)
    {
        if (info.Equipments == null || info.Equipments.Length == 0)
            return 0;

        int sum = 0;
        foreach (var equipment in info.Equipments)
        {
            if (equipment == null)
                continue;
            sum += selector(equipment);
        }
        return sum;
    }

    private static string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static string BuildEnemyPropertyText(EnemyRegedit regedit)
    {
        var sb = new StringBuilder(128);
        string name = string.IsNullOrWhiteSpace(regedit.CharacterName)
            ? "Enemy"
            : regedit.CharacterName;

        sb.Append($"[b]{name}[/b]\n");
        sb.Append($"生命 {regedit.MaxLife}\n");
        sb.Append($"力量 {regedit.Power}\n");
        sb.Append($"生存 {regedit.Survivability}\n");
        sb.Append($"速度 {regedit.Speed}\n");

        string text = sb.ToString().TrimEnd();
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private static string BuildPlayerSkillText(PlayerInfoStructure info, string name)
    {
        var skills = info.TakenSkills.Select(Skill.GetSkill).Where(x => x != null).ToArray();
        foreach (var skill in skills)
        {
            skill.SetPreviewStats(info.Power, info.Survivability, 1);
            skill.UpdateDescription();
        }

        return BuildSkillTooltipText(name, info.PassiveName, info.PassiveDescription, skills);
    }

    private static string BuildEnemySkillText(EnemyRegedit regedit)
    {
        string name = string.IsNullOrWhiteSpace(regedit.CharacterName)
            ? "Enemy"
            : regedit.CharacterName;

        var ids = regedit.SkillIDs ?? Array.Empty<SkillID>();
        var skills = ids.Select(Skill.GetSkill).Where(x => x != null).ToArray();
        foreach (var skill in skills)
        {
            skill.SetPreviewStats(regedit.Power, regedit.Survivability, 1);
            skill.UpdateDescription();
        }

        return BuildSkillTooltipText(name, regedit.PassiveName, regedit.PassiveDescription, skills);
    }

    private static string BuildSkillTooltipText(
        string characterName,
        string passiveName,
        string passiveDesc,
        Skill[] skills
    )
    {
        var sb = new StringBuilder(256);
        sb.Append($"[b]{characterName}[/b]\n");

        AppendPassiveTooltip(sb, passiveName, passiveDesc);

        if (skills == null || skills.Length == 0)
            return sb.ToString().TrimEnd();

        const string separator = "[hr]\n";
        const string skillNameColor = "#b56bff";
        const int skillNameFontSize = 32;

        for (int i = 0; i < skills.Length; i++)
        {
            var skill = skills[i];
            if (skill == null)
                continue;

            if (i > 0)
                sb.Append('\n');

            sb.Append(
                $"[font_size={skillNameFontSize}][color={skillNameColor}]{skill.SkillName}[/color][/font_size]  [color=#cccccc]({skill.SkillType.GetDescription()})[/color]\n"
            );

            if (!string.IsNullOrWhiteSpace(skill.Description))
                sb.Append(skill.Description);
            else
                sb.Append("-");
            sb.Append('\n');

            if (i < skills.Length - 1)
                sb.Append(separator);
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendPassiveTooltip(StringBuilder sb, string passiveName, string passiveDesc)
    {
        if (string.IsNullOrWhiteSpace(passiveName) && string.IsNullOrWhiteSpace(passiveDesc))
            return;

        const string passiveColor = "#ffd36b";
        const int titleFontSize = 30;

        string title = string.IsNullOrWhiteSpace(passiveName) ? "Passive" : passiveName;
        sb.Append(
            $"[font_size={titleFontSize}][color={passiveColor}]{title}[/color][/font_size]  [color=#cccccc](被动)[/color]\n"
        );

        if (!string.IsNullOrWhiteSpace(passiveDesc))
        {
            string text = GlobalFunction.ColorizeNumbers(passiveDesc);
            text = GlobalFunction.ColorizeKeywords(text);
            sb.Append(text);
        }
        else
        {
            sb.Append("-");
        }

        sb.Append("\n[hr]\n");
    }

    private static string GuessNameFromScenePath(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return "Character";

        string normalized = scenePath.Replace('\\', '/');
        int slash = normalized.LastIndexOf('/');
        string last = slash >= 0 ? normalized[(slash + 1)..] : normalized;
        int dot = last.LastIndexOf('.');
        return dot > 0 ? last[..dot] : last;
    }

    private void WirePortraitTooltips(PortaitFrame portrait, string skillText, string propertyText)
    {
        if (portrait?.PortaitButton == null)
            return;

        portrait.PortaitButton.MouseEntered += () => ShowPortraitTooltips(skillText, propertyText);
        portrait.PortaitButton.MouseExited += HidePortraitTooltips;
    }

    private void ShowPortraitTooltips(string skillText, string propertyText)
    {
        if (SkillTooltip != null)
        {
            SkillTooltip.FollowMouse = true;
            SkillTooltip.AnchorOffset = new Vector2(20f, 20f);
            SkillTooltip.Description.Text = skillText ?? string.Empty;
            SkillTooltip.Visible = true;
        }

        if (PropertyTooltip != null)
        {
            PropertyTooltip.FollowMouse = true;
            PropertyTooltip.AnchorOffset = new Vector2(-20f, 20f);
            PropertyTooltip.Description.Text = propertyText ?? string.Empty;
            PropertyTooltip.Visible = true;
        }
    }

    private void HidePortraitTooltips()
    {
        if (SkillTooltip != null)
            SkillTooltip.Visible = false;
        if (PropertyTooltip != null)
            PropertyTooltip.Visible = false;
    }

    public void Close()
    {
        HidePortraitTooltips();
        _ = CloseAsync();
    }

    private async System.Threading.Tasks.Task CloseAsync()
    {
        await PlayCloseAnimationAsync();
        QueueFree();
    }

    public void ClearGrid()
    {
        for (int i = 0; i < PlayerFormation.GetChildCount(); i++)
        {
            foreach (var child in PlayerFormation.GetChild<Control>(i).GetChildren())
            {
                child.QueueFree();
            }
        }

        for (int i = 0; i < EnemyFormation.GetChildCount(); i++)
        {
            foreach (var child in EnemyFormation.GetChild<Control>(i).GetChildren())
            {
                child.QueueFree();
            }
        }
    }

    public void StartBattle()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(tex, "scale", new Vector2(1f, 1f), 0.4f);
        MapNode.BlackMaskAnimation(0.4f);
        GlobalFunction.TweenShader(tex, "cut_x", 1f, 0.3f);
        GlobalFunction.TweenShader(tex, "cut_y", 1f, 0.3f);
        tween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    var layer = new CanvasLayer();
                    layer.Layer = 4;
                    GetTree().Root.AddChild(layer);
                    exitButton.PressedActions.RemoveAt(exitButton.PressedActions.Count - 1);

                    var battle =
                        GD.Load<PackedScene>("res://battle/Battle.tscn").Instantiate() as Battle;
                    battle.BattleIntentionRandom = new Random(RandomNum);
                    battle.CurrentLevelNode = WhichNode;
                    layer.AddChild(battle);
                    Close();
                })
            );
    }
}
