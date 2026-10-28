using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Announcement Database")]
public class AnnouncementDatabase : ScriptableObject
{
    [Header("Character")]
    public AnnouncementData levelUp;

    [Header("Abilities")]
    public AnnouncementData abilityLearned;

    [Header("Reputation")]
    public AnnouncementData factionDiscovered;
    public AnnouncementData reputationRankChanged;

    [Header("Favours")]
    public AnnouncementData favourFailed;
}