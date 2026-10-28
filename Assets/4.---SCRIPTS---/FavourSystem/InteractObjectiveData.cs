using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Objectives/Interact Objective"
)]
public sealed class InteractObjectiveData :
    FavourObjectiveData
{
    [Header("Targets")]

    [SerializeField]
    [Tooltip(
        "Entities som spelaren måste interagera med.\n\n" +
        "Du kan dra både scene objects och prefabs hit. " +
        "Endast EntityIdentity-ID:t sparas i objective-assetet."
    )]
    private List<EntityReference>
        targets =
            new();

    public IReadOnlyList<EntityReference>
        Targets =>
            targets;

    public int RequiredInteractions
    {
        get
        {
            if (targets == null)
                return 0;

            HashSet<string> ids =
                new(
                    System.StringComparer.Ordinal
                );

            foreach (EntityReference target
                     in targets)
            {
                if (target == null ||
                    !target.IsValid)
                {
                    continue;
                }

                ids.Add(
                    target.Id
                );
            }

            return ids.Count;
        }
    }

    public bool ContainsTarget(
        string entityId)
    {
        if (string.IsNullOrWhiteSpace(
                entityId) ||
            targets == null)
        {
            return false;
        }

        foreach (EntityReference target
                 in targets)
        {
            if (target == null ||
                !target.IsValid)
            {
                continue;
            }

            if (EntityTargetUtility.Matches(
                    entityId,
                    target.Id))
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
        return new InteractObjectiveRuntime(
            this,
            favour
        );
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        targets ??=
            new List<EntityReference>();

        if (targets.Count == 0)
        {
            Debug.LogWarning(
                $"InteractObjective '{name}' saknar targets.",
                this
            );

            return;
        }

        HashSet<string> ids =
            new(
                System.StringComparer.Ordinal
            );

        foreach (EntityReference target
                 in targets)
        {
            if (target == null ||
                !target.IsValid)
            {
                Debug.LogWarning(
                    $"InteractObjective '{name}' har ett target " +
                    $"utan giltigt Entity ID.",
                    this
                );

                continue;
            }

            if (!ids.Add(
                    target.Id))
            {
                Debug.LogWarning(
                    $"InteractObjective '{name}' innehåller " +
                    $"Entity ID '{target.Id}' flera gånger.",
                    this
                );
            }
        }
    }

#endif
}