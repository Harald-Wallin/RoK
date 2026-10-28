using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueTable", menuName = "Dialogue/NPC Dialogue Table")]
public class NPCDialogueTable : ScriptableObject
{
    [Header("Faction Restriction (Optional)")]
    public Faction faction; // kan vara null
    public int minReputationLevel = 0;

    [Header("Dialogue Lines")]
    public List<DialogueLine> lines = new List<DialogueLine>();
}
