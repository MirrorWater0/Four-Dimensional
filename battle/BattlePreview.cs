using System;
using Godot;

public partial class BattlePreview : Control
{
    public GridContainer PlayerFormation =>
        field ??= GetNode<GridContainer>("HBoxContainer/PlayerFormation");
    public GridContainer EnemyFormation =>
        field ??= GetNode<GridContainer>("HBoxContainer/EnemyFormation");
    public Button StartBattleButton => field ??= GetNode<Button>("StartBattle");
    ColorRect tex => field ??= StartBattleButton.GetNode<ColorRect>("BG");
    ExitButton exitButton => field ??= GetNode<ExitButton>("/root/Map/UI/ExitButton");

    public override void _Ready()
    {
        exitButton.PressedActions.Add(Close);
        Modulate = new Color(1, 1, 1, 0);
        CreateTween().TweenProperty(this, "modulate:a", 1, 0.3f);
        SetPortraitPostion();
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

            PlayerFormation.GetChild(BattleReady.remap[positionindex] - 1).AddChild(portrait);
        }

        for (int i = 0; i < LevelNode.EnemiesRegeditList.Count; i++)
        {
            var portrait = BattleReady.PortaitScene.Instantiate() as PortaitFrame;
            portrait.PortaitRect.Texture = GD.Load<Texture2D>(
                LevelNode.EnemiesRegeditList[i].PortaitPath
            );
            portrait.PortaitIndex = i;
            var positionindex = LevelNode.EnemiesRegeditList[i].PositionIndex;
            EnemyFormation.GetChild(BattleReady.remap[positionindex] - 1).AddChild(portrait);
        }
    }

    public void Close()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0, 0.3f);
        tween.TweenCallback(Callable.From(QueueFree));
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
        var mask = GetNode<ColorRect>("/root/Map/UI/Mask");
        mask.Visible = true;
        mask.Modulate = new Color(0, 0, 0, 0);
        Tween tween = CreateTween();
        tween.TweenProperty(mask, "modulate:a", 1, 0.4f);

        GlobalFunction.TweenShader(tex, "cut_x", 1f, 0.3f);
        GlobalFunction.TweenShader(tex, "cut_y", 1f, 0.3f);
        tween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    Close();
                    exitButton.PressedActions.RemoveAt(exitButton.PressedActions.Count - 1);
                    var battle =
                        GD.Load<PackedScene>("res://battle/Battle.tscn").Instantiate() as Battle;
                    var layer = new CanvasLayer();
                    layer.Layer = 10;
                    GetTree().Root.AddChild(layer);
                    layer.AddChild(battle);
                    mask.Visible = false;
                })
            );
    }
}
