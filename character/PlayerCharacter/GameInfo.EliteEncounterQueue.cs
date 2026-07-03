using System.Collections.Generic;

public static partial class GameInfo
{
    private const int MaxEliteBattlesPerRegion = 6;

    public static List<int> EliteEncounterQueue = new();

    public static void ResetEliteEncounterQueueState()
    {
        EliteEncounterQueue.Clear();
        ElitesVisited = 0;
        NodeEliteBattleVisitIndices.Clear();
    }

    public static void InitializeEliteEncounterQueue()
    {
        EliteEncounterQueue = new List<int>();
        int passIndex = 0;
        while (EliteEncounterQueue.Count < MaxEliteBattlesPerRegion)
        {
            foreach (int catalogIndex in EliteBattleEncounter.BuildRunQueue(CurrentLevel, passIndex))
            {
                int indexToAdd = catalogIndex;
                if (
                    EliteEncounterQueue.Count > 0
                    && EliteEncounterQueue[^1] == indexToAdd
                    && EliteBattleEncounter.PoolSize > 1
                )
                {
                    indexToAdd = 1 - indexToAdd;
                }

                EliteEncounterQueue.Add(indexToAdd);
                if (EliteEncounterQueue.Count >= MaxEliteBattlesPerRegion)
                    break;
            }

            passIndex++;
        }
    }

    public static void EnsureEliteEncounterQueue()
    {
        if (EliteEncounterQueue != null && EliteEncounterQueue.Count >= MaxEliteBattlesPerRegion)
            return;

        InitializeEliteEncounterQueue();
    }

    public static int GetEliteEncounterCatalogIndex(int visitIndex)
    {
        EnsureEliteEncounterQueue();
        if (EliteEncounterQueue == null || EliteEncounterQueue.Count == 0)
            return 0;

        int index = visitIndex >= 0 ? visitIndex : 0;
        return EliteEncounterQueue[index % EliteEncounterQueue.Count];
    }

    public static void ResetEliteBattleVisitState()
    {
        ResetEliteEncounterQueueState();
        InitializeEliteEncounterQueue();
    }
}
