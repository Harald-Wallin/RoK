using System.Collections.Generic;
using UnityEngine;

public class FactionAwarenessSystem : MonoBehaviour
{
    public static FactionAwarenessSystem Instance;

    private readonly List<NPCReactionController>
        alertedNPCs =
        new();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterAlertedNPC(
        NPCReactionController npc)
    {
        if (npc == null)
            return;

        if (!alertedNPCs.Contains(npc))
        {
            alertedNPCs.Add(npc);
        }
    }

    public void UnregisterAlertedNPC(
        NPCReactionController npc)
    {
        if (npc == null)
            return;

        alertedNPCs.Remove(npc);
    }

    public bool IsFactionAlerted(Faction faction)
    {
        if (faction == null)
            return false;

        for (int i = alertedNPCs.Count - 1; i >= 0; i--)
        {
            NPCReactionController npc =
                alertedNPCs[i];

            if (npc == null)
            {
                alertedNPCs.RemoveAt(i);
                continue;
            }

            if (!npc.IsAlerted)
                continue;

            if (npc.Faction != faction)
                continue;

            return true;
        }

        return false;
    }
}