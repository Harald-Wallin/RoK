using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DeliveryEntry
{
    [SerializeField]
    private ItemData item;

    [SerializeField]
    [Min(1)]
    private int amount = 1;

    [SerializeField]
    [Tooltip(
        "Entity som ska ta emot itemet.\n\n" +
        "Dra ett scene object eller prefab med EntityIdentity hit."
    )]
    private EntityReference target =
        new();

    public ItemData Item =>
        item;

    public int Amount =>
        Mathf.Max(
            1,
            amount
        );

    public EntityReference Target =>
        target;

    public string TargetId =>
        target != null
            ? target.Id
            : string.Empty;

    public string TargetDisplayName =>
        target != null
            ? target.DisplayName
            : string.Empty;

#if UNITY_EDITOR

    public void Validate(
        UnityEngine.Object owner,
        int index)
    {
        amount =
            Mathf.Max(
                1,
                amount
            );

        target ??=
            new EntityReference();

        if (item == null)
        {
            Debug.LogWarning(
                $"DeliverObjective '{owner.name}' har ingen Item " +
                $"på delivery entry {index}.",
                owner
            );
        }

        if (!target.IsValid)
        {
            Debug.LogWarning(
                $"DeliverObjective '{owner.name}' har inget giltigt " +
                $"Target på delivery entry {index}.",
                owner
            );
        }
    }

#endif
}

[CreateAssetMenu(
    menuName =
        "RPG/Favours/Objectives/Deliver Objective"
)]
public sealed class DeliverObjectiveData :
    FavourObjectiveData
{
    [Header("Deliveries")]

    [SerializeField]
    [Tooltip(
        "Items som ges när favourn accepteras och sedan ska " +
        "levereras till respektive target."
    )]
    private List<DeliveryEntry>
        deliveries =
            new();

    public IReadOnlyList<DeliveryEntry>
        Deliveries =>
            deliveries;

    public int RequiredDeliveries
    {
        get
        {
            if (deliveries == null)
                return 0;

            int count = 0;

            foreach (DeliveryEntry delivery
                     in deliveries)
            {
                if (delivery == null ||
                    delivery.Item == null ||
                    string.IsNullOrWhiteSpace(
                        delivery.TargetId))
                {
                    continue;
                }

                count++;
            }

            return count;
        }
    }

    public override FavourObjectiveRuntime
        CreateRuntime(
            FavourRuntime favour)
    {
        return new DeliverObjectiveRuntime(
            this,
            favour
        );
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        deliveries ??=
            new List<DeliveryEntry>();

        if (deliveries.Count == 0)
        {
            Debug.LogWarning(
                $"DeliverObjective '{name}' saknar deliveries.",
                this
            );

            return;
        }

        for (int i = 0;
             i < deliveries.Count;
             i++)
        {
            deliveries[i]?.Validate(
                this,
                i
            );
        }
    }

#endif
}
