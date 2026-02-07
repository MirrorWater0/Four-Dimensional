using System;
using Godot;

public partial class LevelProgress : Control
{
    public VBoxContainer VBoxContainer => field ?? GetNode<VBoxContainer>("VBoxContainer");

    public override void _Ready()
    {
        for (int i = 0; i < GetChildCount(); i++)
        {
            for (int j = 0; j < GetChild(i).GetChildCount(); j++)
            {
                var levelNode = VBoxContainer.GetChild(i).GetChild<LevelNode>(j);
                levelNode.State = GameInfo.FirstLevelState[new Vector2I(i, j)];
                if (levelNode.State == LevelNode.LevelState.Unlocked)
                {
                    levelNode.Unlock();
                    GD.Print($"Unlock level {i}, {j}");
                }
                else if (levelNode.State == LevelNode.LevelState.Completed)
                    levelNode.CompletedAnimation();
            }
        }
    }
}
