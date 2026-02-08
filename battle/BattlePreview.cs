using System;
using Godot;

public partial class BattlePreview : Control
{
    public GridContainer PlayerFormation => field ??= GetNode<GridContainer>("HBoxContainer/PlayerFormation");
    public GridContainer EnemyFormation => field ??= GetNode<GridContainer>("HBoxContainer/EnemyFormation");
    public Button StartBattleButton => field ??= GetNode<Button>("StartBattle");
    public override void _Ready()
    {
        SetPortraitPostion();
        StartBattleButton.Pressed += StartBattle;
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
        var battle = GD.Load<PackedScene>("res://battle/Battle.tscn");
        GetTree().ChangeSceneToPacked(battle);
    }
}
