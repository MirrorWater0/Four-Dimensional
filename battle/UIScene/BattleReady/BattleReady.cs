using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BattleReady : Control
{
    public PackedScene SkillButtonScene = GD.Load<PackedScene>(
        "res://battle/UIScene/SkillButton.tscn"
    );
    public static PackedScene PortaitScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/PortaitFrame.tscn"
    );
    private List<SkillButton> readyChange = new List<SkillButton>();
    Control FrameContainer => field ??= GetNode("frame") as Control;
    public Control Grid => field ??= GetNode("GridContainer") as Control;
    HBoxContainer SkillContainer => field ??= GetNode("SkillContainer") as HBoxContainer;

    PackedScene SekectButtonScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleReady/SelectButton.tscn"
    );
    private PortaitFrame _dragTarget;
    ShaderMaterial BGmaterial;

    public override void _Ready()
    {
        Modulate = new Color(1, 1, 1, 0);
        BGmaterial = GetNode<ColorRect>("BG").Material as ShaderMaterial;
        BGmaterial.SetShaderParameter("appearance", 0f);
        Initialize();
    }

    public async void StartAnimation()
    {
        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            await ToSignal(GetTree().CreateTimer(0.0001f), "timeout");
            var child = Grid.GetChild<ColorRect>(i);
            Vector2 offset = new Vector2(50 + i * 10, 0);
            child.Position -= offset;
            CreateTween()
                .TweenProperty(
                    child,
                    "position",
                    child.Position + offset,
                    0.2f + 0.01f * (i + 1) % 3
                )
                .SetEase(Tween.EaseType.Out);
        }

        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.3f);
        tween
            .TweenMethod(
                Callable.From<float>(value => BGmaterial.SetShaderParameter("appearance", value)),
                0.0f,
                1f,
                0.4f
            )
            .SetEase(Tween.EaseType.Out);
        FrameContainer.Position += new Vector2(0, 200);
        tween
            .TweenProperty(
                FrameContainer,
                "position",
                FrameContainer.Position - new Vector2(0, 200),
                0.3f
            )
            .SetEase(Tween.EaseType.Out);
    }

    public override async void _Process(double delta)
    {
        if (_dragTarget != null)
        {
            _dragTarget.GlobalPosition =
                GetViewport().GetMousePosition() - _dragTarget.PortaitRect.Size;
        }

        // Manually check for mouse over TextureRects to ensure highlighting works during drag
        var mousePos = GetViewport().GetMousePosition();
        for (int i = 0; i < Grid.GetChildCount(); i++)
        {
            var tex = Grid.GetChild<ColorRect>(i);
            bool isOver = tex.GetGlobalRect().HasPoint(mousePos);

            Color accent_color = new Color(0.69f, 0.75f, 0.80f);
            Color targetColor = isOver
                ? accent_color + 5 * new Color(0.2f, 0.2f, 0.2f)
                : accent_color;

            ((ShaderMaterial)(tex.Material)).SetShaderParameter("line_color", targetColor);
        }
    }

    public void Initialize()
    {
        InitializePostion();
        //give each frame a click event to select
        for (int i = 0; i < FrameContainer.GetChildCount(); i++)
        {
            var frame = FrameContainer.GetChild<Frame>(i);
            frame.IDindex = i;
            frame.ClickButton.Visible = true;

            frame.ClickButton.Pressed += async () =>
            {
                if (frame.Selected.Visible)
                    return;

                frame.Selected.Visible = true;
                foreach (
                    var other in FrameContainer.GetChildren().Where(x => x != frame).OfType<Frame>()
                )
                {
                    other.Selected.Visible = false;
                }

                await ClearSkillContainer();

                var character = GameInfo.PlayerCharacters[frame.IDindex];

                for (int j = 0; j < character.GainedSkills.Count; j++)
                {
                    var selectbutton = SekectButtonScene.Instantiate() as SelectButton;
                    var skill = character.GainedSkills[j];
                    selectbutton.MySkill = skill;
                    selectbutton.ThisLabel.Text = skill.SkillName;

                    int skillIndex = -1;
                    switch (skill.SkillType)
                    {
                        case Skill.SkillTypes.Attack:
                            skillIndex = 0;
                            SkillContainer.GetChild<VBoxContainer>(0).AddChild(selectbutton);
                            break;
                        case Skill.SkillTypes.Defence:
                            skillIndex = 1;
                            SkillContainer.GetChild<VBoxContainer>(1).AddChild(selectbutton);
                            break;
                        case Skill.SkillTypes.Special:
                            skillIndex = 2;
                            SkillContainer.GetChild<VBoxContainer>(2).AddChild(selectbutton);
                            break;
                    }
                    selectbutton.StartAnimation(0.05f * (j - 1));
                    // If the character has already taken this skill, mark the button as pressed
                    if (character.TakenSkills.Contains(skill))
                    {
                        selectbutton.Button.ButtonPressed = true;
                        selectbutton.animation.Play("explode");
                    }

                    selectbutton.Button.Pressed += () =>
                    {
                        GameInfo.PlayerCharacters[frame.IDindex].TakenSkills[skillIndex] = skill;
                        selectbutton.Button.ButtonPressed = true;
                        for (int i = 0; i < selectbutton.GetParent().GetChildCount(); i++)
                        {
                            SelectButton button = SkillContainer
                                .GetChild<VBoxContainer>(skillIndex)
                                .GetChild<SelectButton>(i);
                            if (button != selectbutton)
                                button.Button.ButtonPressed = false;
                        }
                    };
                }
                for (int i = 0; i < SkillContainer.GetChildCount(); i++)
                {
                    SkillContainer.GetChild<VBoxContainer>(i).QueueSort();
                }
            };

            for (int j = 0; j < frame.SkillButtonContainer.GetChildCount(); j++)
            {
                frame.SkillButtonContainer.GetChild<Button>(j).Visible = false;
            }
        }
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
            Color accent_color = new Color(0.69f, 0.75f, 0.80f);
            var tex = Grid.GetChild<ColorRect>(i);
            tex.Material.ResourceLocalToScene = true;
            tex.Material = tex.Material.Duplicate() as ShaderMaterial;
            ((ShaderMaterial)(tex.Material)).SetShaderParameter("line_color", accent_color);
        }

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var portrait = PortaitScene.Instantiate() as PortaitFrame;
            portrait.PortaitRect.Texture = GD.Load<Texture2D>(
                GameInfo.PlayerCharacters[i].PortaitPath
            );
            portrait.PortaitIndex = i;
            var positionindex = GameInfo.PlayerCharacters[i].PositionIndex;

            if (remap.ContainsKey(positionindex))
            {
                Grid.GetChild(remap[positionindex] - 1).AddChild(portrait);
            }
            else
            {
                GD.PrintErr($"Invalid PositionIndex: {positionindex}. Valid values are 1-9.");
                continue;
            }

            portrait.PortaitButton.ButtonDown += () =>
            {
                _dragTarget = portrait;
            };
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
                    CreateTween().TweenProperty(p, "position", new Vector2(0, 0), time);
                    Tween tween = CreateTween();
                    tween
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

    public async Task ClearSkillContainer()
    {
        int buttonsCount = 0;
        int cumulativeIndex = 0;
        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
        {
            buttonsCount += SkillContainer.GetChild<VBoxContainer>(i).GetChildCount();
            for (int j = 0; j < SkillContainer.GetChild<VBoxContainer>(i).GetChildCount(); j++)
            {
                var button = SkillContainer.GetChild<VBoxContainer>(i).GetChild<SelectButton>(j);
                button.FadeAnimation(0.05f * cumulativeIndex);
                cumulativeIndex++;
            }
        }
        await ToSignal(GetTree().CreateTimer(0.2f + 0.05f * (buttonsCount - 1)), "timeout");
        for (int i = 0; i < SkillContainer.GetChildCount(); i++)
        {
            for (int j = 0; j < SkillContainer.GetChild<VBoxContainer>(i).GetChildCount(); j++)
            {
                var button = SkillContainer.GetChild<VBoxContainer>(i).GetChild<SelectButton>(j);
                button.QueueFree();
            }
        }
    }

    public void ComfirmTactics()
    {
        System.Collections.Generic.Dictionary<int, int> map =
            new System.Collections.Generic.Dictionary<int, int>()
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
            if (texture.GetChildCount() > 0)
            {
                int gridIndex = i + 1;
                if (map.ContainsKey(gridIndex))
                {
                    var portrait = texture.GetChild<PortaitFrame>(0);
                    if (portrait != null)
                    {
                        // Update both the data structure and the character instance
                        GameInfo.PlayerCharacters[portrait.PortaitIndex].PositionIndex = map[
                            gridIndex
                        ];
                    }
                    else
                    {
                        GD.PrintErr($"Portrait or Charater is null at grid index {gridIndex}");
                    }
                }
            }
        }

        var preview = GetTree().Root.GetNodeOrNull<BattlePreview>("Map/SiteUI/BattlePreview");
        if (preview != null)
        {
            preview.SetPortraitPostion();
        }
    }
}
