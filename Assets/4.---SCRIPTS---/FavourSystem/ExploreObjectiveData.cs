using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Objectives/Explore Objective"
)]
public sealed class ExploreObjectiveData :
    FavourObjectiveData
{
    [Header("Zones")]

    [SerializeField]
    [Tooltip(
        "Områden som spelaren måste upptäcka.\n\n" +
        "Dra scene objects med EntityIdentity + ExploreZone hit. " +
        "Endast EntityIdentity-ID:t sparas i objective-assetet."
    )]
    private List<EntityReference>
        zones =
            new();

    public IReadOnlyList<EntityReference>
        Zones =>
            zones;

    public int RequiredExplorations
    {
        get
        {
            if (zones == null)
                return 0;

            HashSet<string> ids =
                new(
                    System.StringComparer.Ordinal
                );

            foreach (EntityReference zone
                     in zones)
            {
                if (zone == null ||
                    !zone.IsValid)
                {
                    continue;
                }

                ids.Add(
                    zone.Id
                );
            }

            return ids.Count;
        }
    }

    public bool ContainsZone(
        string zoneId)
    {
        if (string.IsNullOrWhiteSpace(
                zoneId) ||
            zones == null)
        {
            return false;
        }

        foreach (EntityReference zone
                 in zones)
        {
            if (zone == null ||
                !zone.IsValid)
            {
                continue;
            }

            if (EntityTargetUtility.Matches(
                    zoneId,
                    zone.Id))
            {
                return true;
            }
        }

        return false;
    }

    public override FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour)
    {
        return new ExploreObjectiveRuntime(
            this,
            favour
        );
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        zones ??=
            new List<EntityReference>();

        if (zones.Count == 0)
        {
            Debug.LogWarning(
                $"ExploreObjective '{name}' saknar zones.",
                this
            );

            return;
        }

        HashSet<string> ids =
            new(
                System.StringComparer.Ordinal
            );

        foreach (EntityReference zone
                 in zones)
        {
            if (zone == null ||
                !zone.IsValid)
            {
                Debug.LogWarning(
                    $"ExploreObjective '{name}' har en zone " +
                    $"utan giltigt Entity ID.",
                    this
                );

                continue;
            }

            if (!ids.Add(
                    zone.Id))
            {
                Debug.LogWarning(
                    $"ExploreObjective '{name}' innehåller " +
                    $"Entity ID '{zone.Id}' flera gånger.",
                    this
                );
            }
        }
    }

#endif
}
