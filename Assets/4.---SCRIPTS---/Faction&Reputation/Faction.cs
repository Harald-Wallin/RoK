using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RPG/Faction")]
public class Faction : ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    public string factionName;


    [Header("Presentation")]

    [SerializeField]
    [Tooltip(
        "Ikonen som används när denna faction presenteras i UI, " +
        "exempelvis på NPC-nameplates."
    )]
    private Sprite icon;


    [Header("Can be discovered")]

    public bool showInReputationWindow = true;


    [Header("Description")]

    [TextArea(3, 6)]
    public string loreDescription;

    public string factionCategory;


    public string Id =>
        PersistentIdUtility
            .FromDisplayName(
                DisplayName
            );


    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            factionName
        )
            ? name
            : factionName;


    public Sprite Icon =>
        icon;


    [System.Serializable]
    public class FactionRelation
    {
        public Faction otherFaction;

        public ReputationState standing =
            ReputationState.Indifferent;
    }


    public List<FactionRelation> relations =
        new List<FactionRelation>();


    public ReputationState GetStanding(
        Faction other)
    {
        if (other == null)
        {
            return
                ReputationState.Indifferent;
        }

        foreach (
            FactionRelation relation
            in relations)
        {
            if (relation.otherFaction ==
                other)
            {
                return
                    relation.standing;
            }
        }

        return
            ReputationState.Indifferent;
    }
}