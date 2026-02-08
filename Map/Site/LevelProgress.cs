using System;
using System.Collections.Generic;
using Godot;

public partial class LevelProgress : Control
{
    public VBoxContainer VBoxContainer => field ?? GetNode<VBoxContainer>("VBoxContainer");

    public override void _Ready()
    {
        Random rng = new Random(GameInfo.Seed);

        for (int i = 0; i < VBoxContainer.GetChildCount(); i++)
        {
            // Initialize all as Normal
            LevelNode.LevelType[] rowTypes = new LevelNode.LevelType[7];
            for (int k = 0; k < 7; k++)
                rowTypes[k] = LevelNode.LevelType.Normal;

            // Last node (index 6) is Boss
            rowTypes[6] = LevelNode.LevelType.Boss;

            // Generate 1 Elite between 3rd (index 2) and 6th (index 5) node
            int eliteIndex = rng.Next(2, 6);
            rowTypes[eliteIndex] = LevelNode.LevelType.Elite;

            // Generate 1-2 Events between 2nd (index 1) and 6th (index 5) node
            int eventCount = rng.Next(1, 3);
            List<int> eventCandidates = new List<int>();
            for (int k = 1; k <= 5; k++)
            {
                // Avoid overwriting Elite
                if (k != eliteIndex)
                {
                    eventCandidates.Add(k);
                }
            }

            for (int k = 0; k < eventCount; k++)
            {
                if (eventCandidates.Count == 0)
                    break;
                int randIndex = rng.Next(eventCandidates.Count);
                int eventIndex = eventCandidates[randIndex];
                rowTypes[eventIndex] = LevelNode.LevelType.Event;
                eventCandidates.RemoveAt(randIndex);
            }

            for (int j = 0; j < VBoxContainer.GetChild(i).GetChildCount(); j++)
            {
                var levelNode = VBoxContainer.GetChild(i).GetChild<LevelNode>(j);

                // Assign generated type
                if (j < 7)
                {
                    levelNode.Type = rowTypes[j];
                }
                levelNode.ColorChose();
                levelNode.State = GameInfo.FirstLevelState[new Vector2I(j, i)];
                if (j != VBoxContainer.GetChild(i).GetChildCount() - 1)
                {
                    levelNode.NextNode = VBoxContainer.GetChild(i).GetChild<LevelNode>(j + 1);
                    levelNode.Button.Pressed += () => lockOther(levelNode.NextNode);
                }
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

    public void lockOther(LevelNode currentNode)
    {
        for (int i = 0; i < VBoxContainer.GetChildCount(); i++)
        {
            for (int j = 0; j < VBoxContainer.GetChild(i).GetChildCount(); j++)
            {
                var levelNode = VBoxContainer.GetChild(i).GetChild<LevelNode>(j);
                if (levelNode != currentNode)
                {
                    levelNode.Color = levelNode.LockColor;
                    levelNode.Button.Disabled = true;
                }
            }
        }
    }

    public void Close()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0, 0.3f);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }

    public void StartAnimation()
    {
        for (int i = 0; i < VBoxContainer.GetChildCount(); i++)
        {
            for (int j = 0; j < VBoxContainer.GetChild(i).GetChildCount(); j++)
            {
                
            }
        }
    }
}
