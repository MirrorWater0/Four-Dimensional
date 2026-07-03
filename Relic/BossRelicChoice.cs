using System.Linq;
using Godot;

public static class BossRelicChoice
{
    private const string NodeName = "BossRelicEvent";

    public static bool ShouldShowPendingChoice()
    {
        if (GameInfo.RunFinished || GameInfo.CurrentLevel != 1)
            return false;

        if (GameInfo.PendingBossRelicChoice)
            return true;

        // Migration for saves made after entering region 2 before this flag existed.
        return HasCompletedRegionOneBoss() && !HasOwnedBossRelic();
    }

    public static EventInterface Show(Node caller)
    {
        var root = caller?.GetTree()?.Root;
        if (root == null)
            return null;

        var siteUi =
            root.GetNodeOrNull<CanvasLayer>("Map/SiteUI")
            ?? root.GetNodeOrNull<CanvasLayer>("/root/Map/SiteUI");
        if (siteUi == null)
            return null;

        var existing = siteUi.GetNodeOrNull<EventInterface>(NodeName);
        if (existing != null && !existing.IsQueuedForDeletion())
            return existing;

        GameEvent gameEvent = GameEvent.BuildBossRelicChoice();
        if (gameEvent == null)
        {
            GameInfo.PendingBossRelicChoice = false;
            return null;
        }

        var eventInterface = LevelNode.EventScene.Instantiate() as EventInterface;
        if (eventInterface == null)
            return null;

        eventInterface.Name = NodeName;
        eventInterface.ThisEvent = gameEvent;
        siteUi.AddChild(eventInterface);
        return eventInterface;
    }

    private static bool HasOwnedBossRelic()
    {
        if (GameInfo.Relics == null || GameInfo.Relics.Count == 0)
            return false;

        return Relic.GetBossRelicOfferPool().Any(GameInfo.HasRelic);
    }

    private static bool HasCompletedRegionOneBoss()
    {
        return GameInfo.CompletedLevelNodeRecords?.Values.Any(record =>
                record != null && record.MapLevel == 0 && record.NodeType == LevelNode.LevelType.Boss
            ) == true;
    }
}
