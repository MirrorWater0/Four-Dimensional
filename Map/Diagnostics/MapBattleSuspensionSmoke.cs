using System;
using Godot;

public partial class MapBattleSuspensionSmoke : Node
{
    public override async void _Ready()
    {
        SetProcess(false);
        bool passed = true;

        PackedScene mapScene = GD.Load<PackedScene>("res://Map/Map.tscn");
        Map map = mapScene?.Instantiate<Map>();
        if (map == null)
        {
            Finish(false, "map instantiate failed");
            return;
        }

        map.Name = "Map";
        var mapEntered = ToSignal(map, Node.SignalName.TreeEntered);
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, map);
        await mapEntered;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var battleLayer = new CanvasLayer { Name = "SmokeBattleLayer", Layer = 4 };
        GetTree().Root.AddChild(battleLayer);
        Battle battle = GD.Load<PackedScene>("res://battle/Battle.tscn")?.Instantiate<Battle>();
        if (battle == null)
        {
            map.QueueFree();
            Finish(false, "battle instantiate failed");
            return;
        }

        battle.Name = "Battle";
        battleLayer.AddChild(battle);
        await battle.WhenPresentationReadyAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        for (int i = 0; i < 60 && !IsStarfieldActive(battle); i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        bool worldSuspended = map.IsBattleWorldSuspended;
        bool levelProgressSuspended = IsHiddenAndDisabled(map.GetNodeOrNull("LevelProgress"));
        bool mapLabelSuspended = IsHiddenAndDisabled(map.GetNodeOrNull("MapLabel"));
        bool uiSuspended = IsHiddenAndDisabled(map.GetNodeOrNull("UI"));
        bool environmentSuspended =
            map.GetNodeOrNull<WorldEnvironment>("WorldEnvironment")?.Environment == null;
        bool resourceStatePreserved = IsPreserved(map.GetNodeOrNull("PlayerResourceState"));
        bool menuLayerPreserved = IsPreserved(map.GetNodeOrNull("MenuLayer"));
        bool resourceInputIdle = !map.PlayerResourceState.IsProcessingUnhandledInput();
        bool itemInputIdle = AreItemInputsIdle(map.PlayerResourceState);
        bool starfieldActive = IsStarfieldActive(battle);
        bool entered =
            worldSuspended
            && levelProgressSuspended
            && mapLabelSuspended
            && uiSuspended
            && environmentSuspended
            && resourceStatePreserved
            && menuLayerPreserved
            && resourceInputIdle
            && itemInputIdle
            && starfieldActive;
        passed &= entered;
        GD.Print(
            "[MapBattleSuspensionSmoke] entered="
                + $"{entered} world={worldSuspended} level={levelProgressSuspended} "
                + $"label={mapLabelSuspended} ui={uiSuspended} environment={environmentSuspended} "
                + $"resources={resourceStatePreserved} menu={menuLayerPreserved} "
                + $"resource_input_idle={resourceInputIdle} item_input_idle={itemInputIdle} "
                + $"starfield={starfieldActive}"
        );

        map.EnterMapPeekMode();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        bool peek =
            map.IsMapPeekModeActive
            && !map.IsBattleWorldSuspended
            && map.PlayerResourceState.IsProcessingUnhandledInput()
            && map.GetNodeOrNull<CanvasItem>("LevelProgress")?.Visible == true
            && map.Camera?.Enabled == true;
        passed &= peek;
        GD.Print($"[MapBattleSuspensionSmoke] peek={peek}");

        map.ExitMapPeekMode();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        bool peekClosed =
            !map.IsMapPeekModeActive
            && map.IsBattleWorldSuspended
            && !map.PlayerResourceState.IsProcessingUnhandledInput()
            && IsHiddenAndDisabled(map.GetNodeOrNull("LevelProgress"));
        passed &= peekClosed;
        GD.Print($"[MapBattleSuspensionSmoke] peek_closed={peekClosed}");

        battleLayer.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        bool exited =
            !map.IsBattleWorldSuspended
            && map.GetNodeOrNull<CanvasItem>("LevelProgress")?.Visible == true
            && map.GetNodeOrNull("LevelProgress")?.ProcessMode != ProcessModeEnum.Disabled
            && map.GetNodeOrNull<CanvasLayer>("MapLabel")?.Visible == true
            && map.Camera?.Enabled == true
            && map.GetNodeOrNull<WorldEnvironment>("WorldEnvironment")?.Environment != null;
        passed &= exited;
        GD.Print($"[MapBattleSuspensionSmoke] exited={exited}");

        map.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Finish(passed, passed ? "ok" : "state assertion failed");
    }

    private static bool IsHiddenAndDisabled(Node node)
    {
        if (node == null || node.ProcessMode != ProcessModeEnum.Disabled)
            return false;
        if (node is CanvasItem canvasItem)
            return !canvasItem.Visible;
        if (node is CanvasLayer canvasLayer)
            return !canvasLayer.Visible;
        return true;
    }

    private static bool IsPreserved(Node node) =>
        node != null && node.ProcessMode != ProcessModeEnum.Disabled;

    private static bool IsStarfieldActive(Battle battle)
    {
        Control starfield = battle?.GetNodeOrNull<Control>("bg/StarfieldBackground3D");
        SubViewport viewport = battle?.GetNodeOrNull<SubViewport>(
            "bg/StarfieldBackground3D/StarfieldViewportContainer/StarfieldViewport"
        );
        return starfield?.Visible == true
            && viewport?.RenderTargetUpdateMode != SubViewport.UpdateMode.Disabled;
    }

    private static bool AreItemInputsIdle(PlayerResourceState playerResourceState)
    {
        if (playerResourceState?.ItemContainer == null)
            return false;

        foreach (Node child in playerResourceState.ItemContainer.GetChildren())
        {
            if (child is ItemContainer item && item.IsProcessingInput())
                return false;
        }
        return true;
    }

    private void Finish(bool passed, string detail)
    {
        GD.Print($"[MapBattleSuspensionSmoke] result passed={passed} detail={detail}");
        GetTree().Quit(passed ? 0 : 1);
    }
}
