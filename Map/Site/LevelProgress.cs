using System;
using System.Collections.Generic;
using Godot;

public partial class LevelProgress : Control
{
    public VBoxContainer VBoxContainer => field ?? GetNode<VBoxContainer>("VBoxContainer");

    public override void _Ready()
    {
        GetNode<ExitButton>("/root/Map/UI/ExitButton").PressedActions.Add(Close);
        InitializeLevelNodes();
        CallDeferred("StartAnimation");
    }

    private LevelNode GetLevelNode(Node node)
    {
        if (node is LevelNode ln)
            return ln;
        if (node.GetChildCount() > 0 && node.GetChild(0) is LevelNode lnChild)
            return lnChild;
        return null;
    }

    public void lockOther(LevelNode currentNode)
    {
        for (int i = 0; i < VBoxContainer.GetChildCount(); i++)
        {
            var row = VBoxContainer.GetChild(i);
            for (int j = 0; j < row.GetChildCount(); j++)
            {
                var levelNode = GetLevelNode(row.GetChild(j));
                if (levelNode.State == LevelNode.LevelState.Unlocked && levelNode != currentNode)
                {
                    levelNode.Color = levelNode.LockColor;
                    levelNode.State = LevelNode.LevelState.Locked;
                    GameInfo.FirstLevelState[levelNode.SelfCoordinate] = LevelNode.LevelState.Locked;
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
            var row = VBoxContainer.GetChild(i);
            for (int j = 0; j < row.GetChildCount(); j++)
            {
                var levelNode = GetLevelNode(row.GetChild(j));
                if (levelNode != null)
                {
                    Vector2 offset = new Vector2(100, (i - 1) * 50);
                    float delay = (i + j) * 0.05f;
                    levelNode.ApplyEntranceMove(offset, 0.5f, delay);
                }
            }
        }
    }

    public void InitializeLevelNodes()
    {
        // Pass 1: Initialize coordinates for all nodes first
        for (int i = 0; i < VBoxContainer.GetChildCount(); i++)
        {
            var row = VBoxContainer.GetChild(i);
            for (int j = 0; j < row.GetChildCount(); j++)
            {
                var ln = GetLevelNode(row.GetChild(j));
                if (ln != null)
                {
                    ln.SelfCoordinate = new Vector2I(j, i);
                }
            }
        }

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

            var row = VBoxContainer.GetChild(i);
            for (int j = 0; j < row.GetChildCount(); j++)
            {
                var child = row.GetChild(j);
                LevelNode levelNode = child as LevelNode;

                if (levelNode != null)
                {
                    // Wrap existing LevelNode in a Control to isolate it from layout during animation
                    Control wrapper = new Control();
                    wrapper.CustomMinimumSize = levelNode.CustomMinimumSize;
                    wrapper.SizeFlagsHorizontal = levelNode.SizeFlagsHorizontal;
                    wrapper.SizeFlagsVertical = levelNode.SizeFlagsVertical;
                    wrapper.Name = levelNode.Name + "_Wrapper";

                    row.AddChild(wrapper);
                    row.MoveChild(wrapper, j);

                    child.GetParent().RemoveChild(child);
                    wrapper.AddChild(child);
                    levelNode.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                }
                else
                {
                    levelNode = GetLevelNode(child);
                }

                if (levelNode == null)
                    continue;

                // Assign generated type
                if (j < 7)
                {
                    levelNode.Type = rowTypes[j];
                }
                levelNode.ColorChose();
                levelNode.State = GameInfo.FirstLevelState[new Vector2I(j, i)];

                // Logic for NextNode connecting
                if (j != row.GetChildCount() - 1)
                {
                    // Next sibling might be LevelNode (not wrapped yet) or Wrapper
                    // Since we loop forward, j+1 is not wrapped yet, so it is LevelNode.
                    var nextChild = row.GetChild(j + 1);
                    levelNode.NextNode = nextChild as LevelNode;
                    levelNode.Button.Pressed += () => lockOther(levelNode.NextNode);
                }

                if (levelNode.State == LevelNode.LevelState.Unlocked)
                {
                    levelNode.Unlock();
                }
                else if (levelNode.State == LevelNode.LevelState.Completed)
                    levelNode.CompletedAnimation();
            }
        }
    }
}
