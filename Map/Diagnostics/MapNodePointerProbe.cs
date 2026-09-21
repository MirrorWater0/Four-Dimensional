using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Drives map-node hover through Godot's own viewport input path. This is deliberately
/// passive: it only sends mouse-motion events, so it cannot enter a battle or alter run state.
/// </summary>
public static class MapNodePointerProbe
{
    public sealed record Result(
        Vector2I Coordinate,
        LevelNode.LevelState State,
        bool ButtonDisabled,
        Rect2 VisualRect,
        Rect2 ButtonRect,
        string HoveredControlPath,
        bool NodeMouseEntered,
        bool ButtonMouseEntered,
        bool HitExpectedButton
    );

    public static async Task<IReadOnlyList<Result>> RunAsync(
        Node host,
        IEnumerable<LevelNode> nodes
    )
    {
        var results = new List<Result>();
        Viewport viewport = host?.GetViewport();
        if (host == null || viewport == null)
            return results;
        Rect2 visibleRect = viewport.GetVisibleRect();

        // First leave any live hover target. The subsequent per-node booleans then belong
        // solely to the synthetic move that is being measured.
        PushMouseMotion(viewport, new Vector2(-100f, -100f), Vector2.Zero);
        await WaitForProcessFrameAsync(host);

        foreach (LevelNode node in nodes)
        {
            if (node == null || !GodotObject.IsInstanceValid(node) || !node.IsVisibleInTree())
                continue;

            Button button = node.Button;
            if (button == null || !GodotObject.IsInstanceValid(button))
                continue;
            if (!visibleRect.Intersects(button.GetGlobalRect()))
                continue;

            bool nodeMouseEntered = false;
            bool buttonMouseEntered = false;
            Action nodeEnteredHandler = () => nodeMouseEntered = true;
            Action buttonEnteredHandler = () => buttonMouseEntered = true;
            node.MouseEntered += nodeEnteredHandler;
            button.MouseEntered += buttonEnteredHandler;

            try
            {
                Rect2 visualRect = node.GetGlobalRect();
                Rect2 buttonRect = button.GetGlobalRect();
                Vector2 probePosition = buttonRect.GetCenter();
                PushMouseMotion(viewport, probePosition, Vector2.Zero);
                await WaitForProcessFrameAsync(host);

                Control hovered = viewport.GuiGetHoveredControl();
                bool hitExpectedButton = hovered == button || (
                    hovered != null && button.IsAncestorOf(hovered)
                );
                results.Add(
                    new Result(
                        node.SelfCoordinate,
                        node.State,
                        button.Disabled,
                        visualRect,
                        buttonRect,
                        hovered?.GetPath().ToString() ?? "<none>",
                        nodeMouseEntered,
                        buttonMouseEntered,
                        hitExpectedButton
                    )
                );
            }
            finally
            {
                node.MouseEntered -= nodeEnteredHandler;
                button.MouseEntered -= buttonEnteredHandler;
            }

            PushMouseMotion(viewport, new Vector2(-100f, -100f), Vector2.Zero);
            await WaitForProcessFrameAsync(host);
        }

        return results;
    }

    private static void PushMouseMotion(Viewport viewport, Vector2 position, Vector2 relative)
    {
        viewport.PushInput(
            new InputEventMouseMotion
            {
                Position = position,
                GlobalPosition = position,
                Relative = relative,
            }
        );
    }

    private static async Task WaitForProcessFrameAsync(Node host)
    {
        SceneTree tree = host.GetTree();
        if (tree != null)
            await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
