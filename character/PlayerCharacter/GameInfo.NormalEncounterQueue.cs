using System.Collections.Generic;

public static partial class GameInfo
{
    private const int MaxNormalBattlesPerRegion = 24;

    public static List<int> NormalEncounterQueue = new();

    public static void ResetNormalEncounterQueueState()
    {
        NormalEncounterQueue.Clear();
        NormalBattlesVisited = 0;
        NodeNormalBattleVisitIndices.Clear();
    }

    public static void InitializeNormalEncounterQueue()
    {
        NormalEncounterQueue = new List<int>();
        int passIndex = 0;
        while (NormalEncounterQueue.Count < MaxNormalBattlesPerRegion)
        {
            foreach (int formationIndex in NormalBattleEncounter.BuildRunQueue(CurrentLevel, passIndex))
            {
                NormalEncounterQueue.Add(formationIndex);
                if (NormalEncounterQueue.Count >= MaxNormalBattlesPerRegion)
                    break;
            }

            passIndex++;
        }
    }

    public static void EnsureNormalEncounterQueue()
    {
        if (NormalEncounterQueue != null && NormalEncounterQueue.Count >= MaxNormalBattlesPerRegion)
            return;

        InitializeNormalEncounterQueue();
    }

    public static int GetNormalEncounterFormationIndex(int visitIndex)
    {
        EnsureNormalEncounterQueue();
        if (NormalEncounterQueue == null || NormalEncounterQueue.Count == 0)
            return 0;

        int index = visitIndex >= 0 ? visitIndex : 0;
        return NormalEncounterQueue[index % NormalEncounterQueue.Count];
    }

    public static void ResetNormalBattleVisitState()
    {
        ResetNormalEncounterQueueState();
        InitializeNormalEncounterQueue();
    }
}
