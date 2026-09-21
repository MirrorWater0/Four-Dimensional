using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Standalone, in-engine verification for <see cref="MapNodePointerProbe"/>.
/// Launch this scene directly; it never uses the operating system mouse and exits when done.
/// </summary>
public partial class MapNodePointerProbeRunner : Node
{
    public override async void _Ready()
    {
        try
        {
            GetTree().Root.Size = new Vector2I(1920, 1080);
            GameInfo.Seed = 377500226;
            GameInfo.InitNewGame();
            GameInfo.PendingStarterBonusChoice = false;
            GameInfo.PendingBossRelicChoice = false;

            PackedScene scene = GD.Load<PackedScene>("res://Map/Map.tscn");
            if (scene == null)
                throw new InvalidOperationException("Map.tscn failed to load.");

            Map map = scene.Instantiate<Map>();
            CallDeferred(Node.MethodName.AddChild, map);
            await WaitFramesAsync(6);

            LevelProgress levelProgress = map.GetNodeOrNull<LevelProgress>("LevelProgress");
            if (levelProgress == null)
                throw new InvalidOperationException("Map has no LevelProgress node.");

            // The probe verifies input routing, not map-progression policy. Enabling every
            // generated node ensures disabled buttons do not mask a hit-test regression.
            foreach (LevelNode node in FindNodes(levelProgress))
                node.Unlock();

            await WaitFramesAsync(1);
            IReadOnlyList<MapNodePointerProbe.Result> results = await MapNodePointerProbe.RunAsync(
                levelProgress,
                FindNodes(levelProgress).Where(node => node.SelfCoordinate.X == 0)
            );
            List<MapNodePointerProbe.Result> failures = results
                .Where(result =>
                    !result.ButtonDisabled
                    && (!result.HitExpectedButton || !result.ButtonMouseEntered)
                )
                .ToList();

            foreach (MapNodePointerProbe.Result result in results)
            {
                GD.Print(
                    "[MapNodePointerProbeRunner] "
                        + $"node={result.Coordinate} disabled={result.ButtonDisabled} "
                        + $"hit={result.HitExpectedButton} button_enter={result.ButtonMouseEntered} "
                        + $"hovered={result.HoveredControlPath}"
                );
            }

            if (failures.Count > 0)
            {
                GD.PrintErr(
                    $"[MapNodePointerProbeRunner] FAILED: {failures.Count}/{results.Count} enabled nodes missed."
                );
                GetTree().Quit(1);
                return;
            }

            GD.Print($"[MapNodePointerProbeRunner] PASS: {results.Count} nodes hit their buttons.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PrintErr($"[MapNodePointerProbeRunner] FAILED: {exception}");
            GetTree().Quit(1);
        }
    }

    private static IEnumerable<LevelNode> FindNodes(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is LevelNode levelNode)
                yield return levelNode;

            foreach (LevelNode descendant in FindNodes(child))
                yield return descendant;
        }
    }

    private async Task WaitFramesAsync(int frames)
    {
        SceneTree tree = GetTree();
        for (int index = 0; index < frames; index++)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
