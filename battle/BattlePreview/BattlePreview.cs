using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public partial class BattlePreview : Control
{
    [Export]
    public bool WarmupMode { get; set; }

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
    ExitButton exitButton => field ??= GetNode<ExitButton>("ExitButton");
    Map MapNode => field ??= GetNode<Map>("/root/Map");
    public int RandomNum;
    public static Dictionary<int, int> remapEnemy { get; } =
        new Dictionary<int, int>()
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

    private bool _isTransitioning;
    private readonly Dictionary<Control, Vector2> _basePositions = [];
    private readonly List<Tip> _previewPortraitTips = [];
    private CanvasLayer _tipLayer;
    private PackedScene _tipScene;
    private int _previewTipCounter;
    private bool _hidePortraitTipsQueued;
    private PortraitTipPair _activePortraitTips;
    private PortaitFrame _dragTarget;
    private Control _dragOriginalParent;
    private Vector2 _dragMouseOffset;

    private readonly struct AssemblyItem(Control control, Vector2 offset, float delay)
    {
        public Control Control { get; } = control;
        public Vector2 Offset { get; } = offset;
        public float Delay { get; } = delay;
    }

    private sealed class PortraitTipPair
    {
        public string SkillText;
        public string PropertyText;
        public string EquipmentText;
        public Tip SkillTip;
        public Tip PropertyTip;
        public Tip EquipmentTip;
    }

    public override void _Ready()
    {
        if (WarmupMode)
        {
            SetProcess(false);
            SetProcessInput(false);
            SetPhysicsProcess(false);
            return;
        }

        EnsureTipLayer();
        exitButton.PressedActions.Add(Close);
        Modulate = Modulate with { A = 0.0f };
        SetControlAlpha(BackgroundPanel, 0.0f);
        InitializePlayerFormationSlots();
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

    public override void _Process(double delta)
    {
        if (_dragTarget == null || !GodotObject.IsInstanceValid(_dragTarget))
        {
            ResetPlayerFormationSlotHighlights();
            return;
        }

        _dragTarget.GlobalPosition = GetViewport().GetMousePosition() - _dragMouseOffset;
        UpdatePlayerFormationSlotHighlights();
    }

    public override void _ExitTree()
    {
        HidePortraitTooltipsImmediate();
        DisposePreviewPortraitTips();
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
        await BattlePreviewTutorialOverlay.ShowIfNeededAsync(this);
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

    private void InitializePlayerFormationSlots()
    {
        Color accentColor = new(0.69f, 0.75f, 0.80f);
        for (int i = 0; i < PlayerFormation.GetChildCount(); i++)
        {
            if (PlayerFormation.GetChild(i) is not ColorRect slot)
                continue;

            slot.MouseFilter = MouseFilterEnum.Stop;
            if (slot.Material is ShaderMaterial material)
            {
                material.ResourceLocalToScene = true;
                var uniqueMaterial = material.Duplicate() as ShaderMaterial;
                if (uniqueMaterial != null)
                {
                    slot.Material = uniqueMaterial;
                    uniqueMaterial.SetShaderParameter("line_color", accentColor);
                }
            }
        }
    }

    private void UpdatePlayerFormationSlotHighlights()
    {
        Vector2 mousePos = GetViewport().GetMousePosition();
        Color accentColor = new(0.69f, 0.75f, 0.80f);
        Color hoverColor = new(0.9f, 0.95f, 1.0f);

        for (int i = 0; i < PlayerFormation.GetChildCount(); i++)
        {
            if (PlayerFormation.GetChild(i) is not ColorRect slot)
                continue;

            bool hovered = slot.GetGlobalRect().HasPoint(mousePos);
            if (slot.Material is ShaderMaterial material)
                material.SetShaderParameter("line_color", hovered ? hoverColor : accentColor);
        }
    }

    private void ResetPlayerFormationSlotHighlights()
    {
        Color accentColor = new(0.69f, 0.75f, 0.80f);
        for (int i = 0; i < PlayerFormation.GetChildCount(); i++)
        {
            if (
                PlayerFormation.GetChild(i) is ColorRect slot
                && slot.Material is ShaderMaterial material
            )
            {
                material.SetShaderParameter("line_color", accentColor);
            }
        }
    }

    private void EnsureTipLayer()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return;

        _tipLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (_tipLayer == null)
        {
            _tipLayer = new CanvasLayer { Layer = 8, Name = "TipLayer" };
            root.AddChild(_tipLayer);
        }

        _tipScene ??= GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
    }

    private Tip CreatePreviewPortraitTip(string text, Vector2 anchorOffset)
    {
        EnsureTipLayer();
        if (_tipLayer == null || _tipScene == null)
            return null;

        var tip = _tipScene.Instantiate<Tip>();
        tip.Name = $"PreviewTip_{++_previewTipCounter}";
        tip.FollowMouse = true;
        tip.AnchorOffset = anchorOffset;
        tip.FadeOutDuration = 0f;
        _tipLayer.AddChild(tip);
        tip.PreloadText(text ?? string.Empty);
        _previewPortraitTips.Add(tip);
        return tip;
    }

    private PortraitTipPair CreatePortraitTipPair(
        string skillText,
        string propertyText,
        string equipmentText
    )
    {
        return new PortraitTipPair
        {
            SkillText = skillText ?? string.Empty,
            PropertyText = propertyText ?? string.Empty,
            EquipmentText = equipmentText ?? string.Empty,
        };
    }

    private bool EnsurePortraitTipPairReady(PortraitTipPair tips)
    {
        if (tips == null)
            return false;

        bool hasSkillTip = tips.SkillTip != null && GodotObject.IsInstanceValid(tips.SkillTip);
        bool hasPropertyTip =
            tips.PropertyTip != null && GodotObject.IsInstanceValid(tips.PropertyTip);
        bool needsEquipmentTip = !string.IsNullOrWhiteSpace(tips.EquipmentText);
        bool hasEquipmentTip =
            !needsEquipmentTip
            || (tips.EquipmentTip != null && GodotObject.IsInstanceValid(tips.EquipmentTip));
        if (hasSkillTip && hasPropertyTip && hasEquipmentTip)
            return true;

        if (!hasSkillTip)
            tips.SkillTip = CreatePreviewPortraitTip(tips.SkillText, new Vector2(20f, 20f));
        if (!hasPropertyTip)
            tips.PropertyTip = CreatePreviewPortraitTip(tips.PropertyText, new Vector2(-20f, 20f));
        if (needsEquipmentTip && !hasEquipmentTip)
            tips.EquipmentTip = CreatePreviewPortraitTip(
                tips.EquipmentText,
                new Vector2(-20f, -20f)
            );

        bool readySkill = tips.SkillTip != null && GodotObject.IsInstanceValid(tips.SkillTip);
        bool readyProperty =
            tips.PropertyTip != null && GodotObject.IsInstanceValid(tips.PropertyTip);
        bool readyEquipment =
            !needsEquipmentTip
            || (tips.EquipmentTip != null && GodotObject.IsInstanceValid(tips.EquipmentTip));

        if (readySkill && readyProperty && readyEquipment)
            return true;

        if (readySkill)
            tips.SkillTip.QueueFree();
        if (readyProperty)
            tips.PropertyTip.QueueFree();
        if (tips.EquipmentTip != null && GodotObject.IsInstanceValid(tips.EquipmentTip))
            tips.EquipmentTip.QueueFree();
        tips.SkillTip = null;
        tips.PropertyTip = null;
        tips.EquipmentTip = null;
        return false;
    }

    private void DisposePreviewPortraitTips()
    {
        for (int i = 0; i < _previewPortraitTips.Count; i++)
        {
            var tip = _previewPortraitTips[i];
            if (tip != null && GodotObject.IsInstanceValid(tip))
                tip.QueueFree();
        }

        _previewPortraitTips.Clear();
        _activePortraitTips = null;
        _previewTipCounter = 0;
    }

    public void SetPortraitPostion()
    {
        DisposePreviewPortraitTips();
        ClearGrid();
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var portrait = BattleReady.PortaitScene.Instantiate() as PortaitFrame;
            var playerPath = GameInfo.PlayerCharacters[i].PortaitPath;
            portrait.PortaitRect.Texture = PreloadeScene.PreloadedTextures.TryGetValue(playerPath, out var playerTex)
                ? playerTex
                : GD.Load<Texture2D>(playerPath);
            portrait.PortaitIndex = i;
            var positionindex = GameInfo.PlayerCharacters[i].PositionIndex;

            var tips = BuildPlayerPortraitTips(i);
            if (tips != null)
                WirePortraitTooltips(
                    portrait,
                    tips.Value.skillText,
                    tips.Value.propertyText,
                    tips.Value.equipmentText
                );
            WirePlayerPortraitDrag(portrait);

            PlayerFormation.GetChild(BattleReady.remap[positionindex] - 1).AddChild(portrait);
        }

        for (int i = 0; i < WhichNode.EnemiesRegeditList.Count; i++)
        {
            var portrait = BattleReady.PortaitScene.Instantiate() as PortaitFrame;
            var enemyPath = WhichNode.EnemiesRegeditList[i].PortaitPath;
            portrait.PortaitRect.Texture = PreloadeScene.PreloadedTextures.TryGetValue(enemyPath, out var enemyTex)
                ? enemyTex
                : GD.Load<Texture2D>(enemyPath);
            portrait.PortaitIndex = i;
            var positionindex = WhichNode.EnemiesRegeditList[i].PositionIndex;

            var tips = BuildEnemyPortraitTips(i);
            if (tips != null)
                WirePortraitTooltips(
                    portrait,
                    tips.Value.skillText,
                    tips.Value.propertyText,
                    tips.Value.equipmentText
                );

            EnemyFormation.GetChild(remapEnemy[positionindex] - 1).AddChild(portrait);
        }

        UpdateSpeedSummary();
    }

    private void WirePlayerPortraitDrag(PortaitFrame portrait)
    {
        if (portrait?.PortaitButton == null)
            return;

        portrait.PortaitButton.KeepPressedOutside = true;
        portrait.PortaitButton.ButtonDown += () => BeginPlayerPortraitDrag(portrait);
        portrait.PortaitButton.ButtonUp += () => EndPlayerPortraitDrag(portrait);
    }

    private void BeginPlayerPortraitDrag(PortaitFrame portrait)
    {
        if (portrait == null || _isTransitioning)
            return;

        HidePortraitTooltipsImmediate();
        _dragTarget = portrait;
        _dragOriginalParent = portrait.GetParent<Control>();
        _dragMouseOffset = GetViewport().GetMousePosition() - portrait.GlobalPosition;
        portrait.ZIndex = 20;
        UpdatePlayerFormationSlotHighlights();
    }

    private void EndPlayerPortraitDrag(PortaitFrame portrait)
    {
        if (_dragTarget != portrait || portrait == null)
            return;

        Control oldParent = _dragOriginalParent ?? portrait.GetParent<Control>();
        Control newParent = FindPlayerFormationSlotAtMouse();

        _dragTarget = null;
        _dragOriginalParent = null;
        _dragMouseOffset = Vector2.Zero;
        portrait.ZIndex = 0;
        ResetPlayerFormationSlotHighlights();

        if (newParent == null)
        {
            TweenPortraitToSlot(portrait, 0.12f);
            return;
        }

        if (newParent != oldParent && newParent.GetChildCount() > 0)
        {
            var swappedPortrait = newParent.GetChildren().OfType<PortaitFrame>().FirstOrDefault();
            if (swappedPortrait != null)
            {
                swappedPortrait.Reparent(oldParent);
                TweenPortraitToSlot(swappedPortrait, 0.18f);
            }
        }

        if (portrait.GetParent() != newParent)
            portrait.Reparent(newParent);

        TweenPortraitToSlot(portrait, 0.18f);
        CommitPlayerFormationPositions();
    }

    private Control FindPlayerFormationSlotAtMouse()
    {
        Vector2 mousePos = GetViewport().GetMousePosition();
        return PlayerFormation
            .GetChildren()
            .OfType<Control>()
            .FirstOrDefault(slot => slot.GetGlobalRect().HasPoint(mousePos));
    }

    private void TweenPortraitToSlot(PortaitFrame portrait, float duration)
    {
        if (portrait == null || !GodotObject.IsInstanceValid(portrait))
            return;

        CreateTween().TweenProperty(portrait, "position", Vector2.Zero, duration);
        CreateTween()
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    if (GodotObject.IsInstanceValid(portrait))
                        portrait.Animation?.Play("explode");
                })
            );
    }

    private void CommitPlayerFormationPositions()
    {
        if (GameInfo.PlayerCharacters == null)
            return;

        for (int i = 0; i < PlayerFormation.GetChildCount(); i++)
        {
            if (PlayerFormation.GetChild(i) is not Control slot)
                continue;
            if (!int.TryParse(slot.Name, out int positionIndex))
                continue;

            var portrait = slot.GetChildren().OfType<PortaitFrame>().FirstOrDefault();
            if (portrait == null)
                continue;
            if (portrait.PortaitIndex < 0 || portrait.PortaitIndex >= GameInfo.PlayerCharacters.Length)
                continue;

            GameInfo.PlayerCharacters[portrait.PortaitIndex].PositionIndex = positionIndex;
        }

        SaveSystem.SaveAll();
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

    private (string skillText, string propertyText, string equipmentText)? BuildPlayerPortraitTips(
        int characterIndex
    )
    {
        if (characterIndex < 0 || characterIndex >= GameInfo.PlayerCharacters.Length)
            return null;

        var info = GameInfo.PlayerCharacters[characterIndex];
        string name = GuessNameFromScenePath(info.CharacterScenePath);

        string skillText = BuildPlayerSkillText(info, name);
        string propertyText = BuildPlayerPropertyPreviewText(info, name);
        string equipmentText = BuildPlayerEquipmentTipText(info);
        return (skillText, propertyText, equipmentText);
    }

    private (string skillText, string propertyText, string equipmentText)? BuildEnemyPortraitTips(
        int enemyIndex
    )
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
        return (skillText, propertyText, string.Empty);
    }

    private static string BuildPlayerPropertyText(PlayerInfoStructure info, string name)
    {
        var sb = new StringBuilder(128);
        int lifeBonus = SumEquipmentBonus(info, x => x.MaxLife);
        int powerBonus = SumEquipmentBonus(info, x => x.Power);
        int surviveBonus = SumEquipmentBonus(info, x => x.Survivability);
        int speedBonus = SumEquipmentBonus(info, x => x.Speed);

        sb.Append($"[b]{name}[/b]\n");
        sb.Append($"生命：{info.LifeMax + lifeBonus}({FormatSigned(lifeBonus)})\n");
        sb.Append($"力量：{info.Power + powerBonus}({FormatSigned(powerBonus)})\n");
        sb.Append($"生存：{info.Survivability + surviveBonus}({FormatSigned(surviveBonus)})\n");
        sb.Append($"速度：{info.Speed + speedBonus}({FormatSigned(speedBonus)})\n");

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
        var skills = (info.GainedSkills ?? new List<SkillID>())
            .Select(Skill.GetSkill)
            .Where(x => x != null && x.SkillType != Skill.SkillTypes.none)
            .ToArray();

        return BuildOwnedSkillNameTooltipText(name, info.PassiveName, info.PassiveDescription, skills);
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
            skill.SetPreviewStats(regedit.Power, regedit.Survivability, 1, isPlayer: false);
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
            sb.Append($"[color=#87ceeb]耗能[/color] {skill.CardEnergyCostText}\n");

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

    private static string BuildOwnedSkillNameTooltipText(
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

        const string skillNameColor = "#b56bff";
        const int skillNameFontSize = 32;
        sb.Append(
            $"[font_size={skillNameFontSize}][color={skillNameColor}]已拥有技能[/color][/font_size]\n"
        );

        AppendOwnedSkillNameLine(sb, skills, Skill.SkillTypes.Attack);
        AppendOwnedSkillNameLine(sb, skills, Skill.SkillTypes.Survive);
        AppendOwnedSkillNameLine(sb, skills, Skill.SkillTypes.Special);

        return sb.ToString().TrimEnd();
    }

    private static void AppendOwnedSkillNameLine(
        StringBuilder sb,
        Skill[] ownedSkills,
        Skill.SkillTypes skillType
    )
    {
        string[] names = (ownedSkills ?? Array.Empty<Skill>())
            .Where(skill => skill != null && skill.SkillType == skillType)
            .Select(skill => skill.SkillName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        if (names.Length == 0)
            return;

        sb.Append($"[color=#cccccc]({skillType.GetDescription()})[/color] ");
        sb.Append(string.Join("、", names));
        sb.Append('\n');
    }

    private static void AppendPassiveTooltip(
        StringBuilder sb,
        string passiveName,
        string passiveDesc
    )
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

    private void WirePortraitTooltips(
        PortaitFrame portrait,
        string skillText,
        string propertyText,
        string equipmentText
    )
    {
        if (portrait?.PortaitButton == null)
            return;

        var tips = CreatePortraitTipPair(skillText, propertyText, equipmentText);
        if (tips == null)
            return;

        portrait.PortaitButton.MouseEntered += () => ShowPortraitTooltips(tips);
        portrait.PortaitButton.MouseExited += () => QueueHidePortraitTooltips(tips);
    }

    private void ShowPortraitTooltips(PortraitTipPair tips)
    {
        if (tips == null)
            return;
        if (_dragTarget != null)
            return;
        if (!EnsurePortraitTipPairReady(tips))
            return;

        _hidePortraitTipsQueued = false;

        if (_activePortraitTips != null && _activePortraitTips != tips)
        {
            _activePortraitTips.SkillTip?.HideTooltip();
            _activePortraitTips.PropertyTip?.HideTooltip();
            _activePortraitTips.EquipmentTip?.HideTooltip();
        }

        tips.SkillTip?.ShowPreloaded(followMouse: true);
        tips.PropertyTip?.ShowPreloaded(followMouse: true);
        tips.EquipmentTip?.ShowPreloaded(followMouse: true);
        _activePortraitTips = tips;
    }

    private void QueueHidePortraitTooltips(PortraitTipPair tips)
    {
        if (tips == null || _activePortraitTips != tips)
            return;

        if (_hidePortraitTipsQueued)
            return;

        _hidePortraitTipsQueued = true;
        CallDeferred(nameof(HidePortraitTooltipsDeferred));
    }

    private void HidePortraitTooltipsDeferred()
    {
        if (!_hidePortraitTipsQueued)
            return;

        _hidePortraitTipsQueued = false;
        HidePortraitTooltipsImmediate();
    }

    private void HidePortraitTooltipsImmediate()
    {
        _hidePortraitTipsQueued = false;
        if (_activePortraitTips == null)
            return;

        _activePortraitTips.SkillTip?.HideTooltip();
        _activePortraitTips.PropertyTip?.HideTooltip();
        _activePortraitTips.EquipmentTip?.HideTooltip();
        _activePortraitTips = null;
    }

    public void Close()
    {
        HidePortraitTooltipsImmediate();
        _ = CloseAsync();
    }

    private async System.Threading.Tasks.Task CloseAsync()
    {
        await PlayCloseAnimationAsync();
        QueueFree();
        ReleaseMapNodeLock();
    }

    private void ReleaseMapNodeLock()
    {
        var levelProgress = WhichNode?.GetParent()?.GetParent<LevelProgress>();
        levelProgress?.UnlockAllNodes();
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
