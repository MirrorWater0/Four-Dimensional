using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class BattleReady : Control
{
    public static PackedScene PortaitScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/PortaitFrame.tscn"
    );
    public Control Grid => field ??= GetNode("GridContainer") as Control;
    private HBoxContainer SkillContainer => field ??= GetNode("SkillContainer") as HBoxContainer;
    private Control CharacterSelectRoot => field ??= GetNode<Control>("CharacterSelectRoot");
    private Control FormationFrame => field ??= GetNode<Control>("FormationFrame");
    private Control FormationHeaderFrame => field ??= GetNode<Control>("FormationHeaderFrame");
    private Control SkillAreaFrame => field ??= GetNode<Control>("SkillAreaFrame");
    private Control SkillAreaHeaderFrame => field ??= GetNode<Control>("SkillAreaHeaderFrame");
    private Control SkillAreaHeader => field ??= GetNode<Control>("SkillAreaHeader");
    private Control SkillTypeFrame => field ??= GetNode<Control>("SkillTypeFrame");
    private Control SkillTypeIcons => field ??= GetNode<Control>("HBoxContainer");
    private Control SkillDividers => field ??= GetNode<Control>("HBoxContainer2");
    private Control TopAccent => field ??= GetNode<Control>("ColorRect");
    public ColorRect BG => field ??= GetNode<ColorRect>("BG");
    private Control CharacterSelectPanel =>
        field ??= GetNode<Control>("CharacterSelectRoot/CharacterSelectPanel");
    private Button[] CharacterButtons =>
        field ??= [
            GetNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/EchoButton"
            ),
            GetNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/KasiyaButton"
            ),
            GetNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/MariyaButton"
            ),
            GetNode<Button>(
                "CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/NightingaleButton"
            ),
        ];

    private static PackedScene SelectButtonScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/SelectButton.tscn"
    );

    private PortaitFrame _dragTarget;
    private int _selectedCharacterIndex;
    private ShaderMaterial BGmaterial;
    private bool _isTransitioning;
    private readonly Dictionary<Control, Vector2> _basePositions = [];

    private readonly struct AssemblyItem(Control control, Vector2 offset, float delay)
    {
        public Control Control { get; } = control;
        public Vector2 Offset { get; } = offset;
        public float Delay { get; } = delay;
    }

    public override void _Ready()
    {
        Modulate = new Color(1, 1, 1, 0);
        BGmaterial = GetNode<ColorRect>("BG").Material as ShaderMaterial;
        BGmaterial.SetShaderParameter("appearance", 0f);
        SetControlAlpha(BG, 0.0f);
        Initialize();
        CacheAssemblyBasePositions();
    }

    public async void StartAnimation()
    {
        await PlayAssembleAnimationAsync();
    }

    public async Task PlayCloseAnimationAsync()
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
            tween.TweenProperty(this, "modulate:a", 0.0f, 0.28f);
            tween.TweenProperty(BG, "modulate:a", 0.0f, 0.24f);

            if (BGmaterial != null)
            {
                tween.TweenMethod(
                    Callable.From<float>(value =>
                        BGmaterial.SetShaderParameter("appearance", value)
                    ),
                    1f,
                    0f,
                    0.24f
                );
            }

            foreach (var item in GetAssemblyItems())
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                tween.TweenProperty(item.Control, "position", basePos + item.Offset * 0.75f, 0.22f);
                tween.TweenProperty(item.Control, "modulate:a", 0.0f, 0.2f);
            }

            await ToSignal(tween, Tween.SignalName.Finished);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Task PlayAssembleAnimationAsync()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        try
        {
            Modulate = Modulate with { A = 0.0f };
            SetControlAlpha(BG, 0.0f);
            if (BGmaterial != null)
                BGmaterial.SetShaderParameter("appearance", 0f);

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
            tween.TweenProperty(this, "modulate:a", 1.0f, 0.3f);
            tween.TweenProperty(BG, "modulate:a", 1.0f, 0.36f);

            if (BGmaterial != null)
            {
                tween.TweenMethod(
                    Callable.From<float>(value =>
                        BGmaterial.SetShaderParameter("appearance", value)
                    ),
                    0f,
                    1f,
                    0.36f
                );
            }

            foreach (var item in items)
            {
                if (!_basePositions.TryGetValue(item.Control, out var basePos))
                    continue;

                tween.TweenProperty(item.Control, "position", basePos, 0.32f).SetDelay(item.Delay);
                tween.TweenProperty(item.Control, "modulate:a", 1.0f, 0.28f).SetDelay(item.Delay);
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
        foreach (var item in GetAssemblyItems())
            _basePositions[item.Control] = item.Control.Position;
    }

    private AssemblyItem[] GetAssemblyItems()
    {
        return
        [
            new AssemblyItem(CharacterSelectRoot, new Vector2(-88f, 24f), 0.00f),
            new AssemblyItem(FormationFrame, new Vector2(-74f, 22f), 0.04f),
            new AssemblyItem(FormationHeaderFrame, new Vector2(-92f, 0f), 0.08f),
            new AssemblyItem(Grid, new Vector2(-60f, 28f), 0.12f),
            new AssemblyItem(SkillAreaFrame, new Vector2(96f, 18f), 0.00f),
            new AssemblyItem(SkillAreaHeaderFrame, new Vector2(78f, 0f), 0.06f),
            new AssemblyItem(SkillAreaHeader, new Vector2(78f, 0f), 0.1f),
            new AssemblyItem(SkillTypeFrame, new Vector2(66f, 0f), 0.12f),
            new AssemblyItem(SkillTypeIcons, new Vector2(54f, -18f), 0.16f),
            new AssemblyItem(SkillDividers, new Vector2(60f, 24f), 0.2f),
            new AssemblyItem(SkillContainer, new Vector2(88f, 14f), 0.24f),
            new AssemblyItem(TopAccent, new Vector2(0f, -40f), 0.2f),
        ];
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        control.Modulate = control.Modulate with { A = alpha };
    }

    public override void _Process(double delta)
    {
        if (_dragTarget != null)
            _dragTarget.GlobalPosition =
                GetViewport().GetMousePosition() - _dragTarget.PortaitRect.Size;

        var mousePos = GetViewport().GetMousePosition();
        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            var tex = Grid.GetChild<ColorRect>(i);
            bool isOver = tex.GetGlobalRect().HasPoint(mousePos);
            Color accentColor = new Color(0.69f, 0.75f, 0.80f);
            Color targetColor = isOver
                ? accentColor + 5 * new Color(0.2f, 0.2f, 0.2f)
                : accentColor;
            ((ShaderMaterial)tex.Material).SetShaderParameter("line_color", targetColor);
        }
    }

    public void Initialize()
    {
        InitializePostion();
        InitializeCharacterButtons();
        _ = SelectCharacter(_selectedCharacterIndex);
    }

    public static System.Collections.Generic.Dictionary<int, int> remap { get; } =
        new System.Collections.Generic.Dictionary<int, int>()
        {
            [7] = 1,
            [4] = 2,
            [1] = 3,
            [8] = 4,
            [5] = 5,
            [2] = 6,
            [9] = 7,
            [6] = 8,
            [3] = 9,
        };

    public void InitializePostion()
    {
        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            Color accentColor = new Color(0.69f, 0.75f, 0.80f);
            var tex = Grid.GetChild<ColorRect>(i);
            tex.Material.ResourceLocalToScene = true;
            tex.Material = tex.Material.Duplicate() as ShaderMaterial;
            ((ShaderMaterial)tex.Material).SetShaderParameter("line_color", accentColor);
        }

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var portrait = PortaitScene.Instantiate() as PortaitFrame;
            portrait.PortaitRect.Texture = GD.Load<Texture2D>(
                GameInfo.PlayerCharacters[i].PortaitPath
            );
            portrait.PortaitIndex = i;
            int positionindex = GameInfo.PlayerCharacters[i].PositionIndex;

            if (!remap.ContainsKey(positionindex))
            {
                GD.PrintErr($"Invalid PositionIndex: {positionindex}. Valid values are 1-9.");
                continue;
            }

            Grid.GetChild(remap[positionindex] - 1).AddChild(portrait);

            portrait.PortaitButton.ButtonDown += () => _dragTarget = portrait;
            portrait.PortaitButton.ButtonUp += () =>
            {
                _dragTarget = null;
                var olderParent = portrait.GetParent();
                var newParent = Grid.GetChildren()
                    .OfType<ColorRect>()
                    .FirstOrDefault(x =>
                        x.GetGlobalRect().HasPoint(GetViewport().GetMousePosition())
                    );

                if (newParent != null)
                {
                    if (newParent.GetChildCount() > 0 && olderParent != newParent)
                    {
                        var overPortait = newParent.GetChild<PortaitFrame>(0);
                        overPortait.Reparent(olderParent);
                        TweenSetAnimation(overPortait, 0.2f);
                    }
                    portrait.Reparent(newParent);
                    TweenSetAnimation(portrait, 0.2f);
                    _dragTarget = null;
                }
                else
                {
                    TweenSetAnimation(portrait, 0.1f);
                }

                void TweenSetAnimation(PortaitFrame p, float time)
                {
                    CreateTween().TweenProperty(p, "position", Vector2.Zero, time);
                    CreateTween()
                        .Chain()
                        .TweenCallback(
                            Callable.From(() =>
                            {
                                p.Animation.Play("explode");
                            })
                        );
                }
            };
        }
    }

    private void InitializeCharacterButtons()
    {
        var players = GameInfo.PlayerCharacters ?? Array.Empty<PlayerInfoStructure>();
        _selectedCharacterIndex = Math.Clamp(
            _selectedCharacterIndex,
            0,
            Math.Max(players.Length - 1, 0)
        );

        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            var button = CharacterButtons[i];
            bool exists = i < players.Length;
            button.Visible = exists;
            if (!exists)
                continue;

            var info = players[i];
            button.ToggleMode = true;
            button.Text = string.IsNullOrWhiteSpace(info.CharacterName)
                ? $"角色 {i + 1}"
                : info.CharacterName;

            int capturedIndex = i;
            button.Pressed += () => OnCharacterButtonPressed(capturedIndex);
        }

        UpdateCharacterButtonState();
    }

    private async void OnCharacterButtonPressed(int characterIndex)
    {
        await SelectCharacter(characterIndex);
    }

    private async Task SelectCharacter(int characterIndex)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || characterIndex < 0 || characterIndex >= players.Length)
            return;

        _selectedCharacterIndex = characterIndex;
        UpdateCharacterButtonState();
        await ClearSkillContainer();
        PopulateSkillButtons(characterIndex);
    }

    private void PopulateSkillButtons(int characterIndex)
    {
        var character = GameInfo.PlayerCharacters[characterIndex];
        for (int j = 0; j < character.GainedSkills.Count; j++)
        {
            var selectbutton = SelectButtonScene.Instantiate() as SelectButton;
            var skillID = character.GainedSkills[j];
            selectbutton.MySkill = Skill.GetSkill(skillID);
            if (selectbutton.MySkill == null)
            {
                selectbutton.QueueFree();
                continue;
            }

            selectbutton.ThisLabel.Text = selectbutton.MySkill.SkillName;

            int skillIndex = selectbutton.MySkill.SkillType switch
            {
                Skill.SkillTypes.Attack => 0,
                Skill.SkillTypes.Survive => 1,
                Skill.SkillTypes.Special => 2,
                _ => -1,
            };
            if (skillIndex < 0)
            {
                selectbutton.QueueFree();
                continue;
            }

            SkillContainer.GetChild<VBoxContainer>(skillIndex).AddChild(selectbutton);
            selectbutton.StartAnimation(0.05f * (j - 1));

            if (character.TakenSkills.Contains(skillID))
            {
                selectbutton.Button.ButtonPressed = true;
                selectbutton.animation.Play("explode");
            }

            int capturedCharacterIndex = characterIndex;
            int capturedSkillIndex = skillIndex;
            selectbutton.Button.Pressed += () =>
            {
                GameInfo.PlayerCharacters[capturedCharacterIndex].TakenSkills[capturedSkillIndex] =
                    skillID;
                selectbutton.Button.ButtonPressed = true;
                for (int i = 0; i < selectbutton.GetParent().GetChildCount(); i++)
                {
                    SelectButton button = SkillContainer
                        .GetChild<VBoxContainer>(capturedSkillIndex)
                        .GetChild<SelectButton>(i);
                    if (button != selectbutton)
                        button.Button.ButtonPressed = false;
                }
            };
        }

        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
            SkillContainer.GetChild<VBoxContainer>(i).QueueSort();
    }

    private void UpdateCharacterButtonState()
    {
        for (int i = 0; i < CharacterButtons.Length; i++)
            CharacterButtons[i].ButtonPressed =
                CharacterButtons[i].Visible && i == _selectedCharacterIndex;
    }

    public async Task ClearSkillContainer()
    {
        int buttonsCount = 0;
        int cumulativeIndex = 0;
        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
        {
            var fence = SkillContainer.GetChild<VBoxContainer>(i);
            buttonsCount += fence.GetChildCount();
            for (int j = 0; j < fence.GetChildCount(); j++)
            {
                var button = fence.GetChild<SelectButton>(j);
                button.FadeAnimation(0.05f * cumulativeIndex);
                cumulativeIndex++;
            }
        }

        if (buttonsCount == 0)
            return;

        await ToSignal(GetTree().CreateTimer(0.2f + 0.05f * (buttonsCount - 1)), "timeout");
        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
        {
            var fence = SkillContainer.GetChild<VBoxContainer>(i);
            for (int j = 0; j < fence.GetChildCount(); j++)
                fence.GetChild<SelectButton>(j).QueueFree();
        }
    }

    public void ComfirmTactics()
    {
        var map = new System.Collections.Generic.Dictionary<int, int>()
        {
            [1] = 7,
            [2] = 4,
            [3] = 1,
            [4] = 8,
            [5] = 5,
            [6] = 2,
            [7] = 9,
            [8] = 6,
            [9] = 3,
        };

        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            var texture = Grid.GetChild<ColorRect>(i);
            if (texture.GetChildCount() <= 0)
                continue;

            int gridIndex = i + 1;
            if (!map.ContainsKey(gridIndex))
                continue;

            var portrait = texture.GetChild<PortaitFrame>(0);
            if (portrait != null)
                GameInfo.PlayerCharacters[portrait.PortaitIndex].PositionIndex = map[gridIndex];
            else
                GD.PrintErr($"Portrait or Charater is null at grid index {gridIndex}");
        }

        SaveSystem.SaveAll();
        var preview = GetTree().Root.GetNodeOrNull<BattlePreview>("Map/SiteUI/BattlePreview");
        if (preview != null)
            preview.SetPortraitPostion();
    }
}
