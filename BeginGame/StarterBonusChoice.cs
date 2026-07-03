using Godot;

public static class StarterBonusChoice
{
    private const string NodeName = "StarterBonusEvent";

    public static bool ShouldShowPendingChoice()
    {
        return !GameInfo.RunFinished && GameInfo.PendingStarterBonusChoice;
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

        var eventInterface = LevelNode.EventScene.Instantiate() as EventInterface;
        if (eventInterface == null)
            return null;

        eventInterface.Name = NodeName;
        eventInterface.ThisEvent = GameEvent.BuildStarterBonus();
        siteUi.AddChild(eventInterface);
        return eventInterface;
    }
}
