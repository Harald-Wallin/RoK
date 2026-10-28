using System;
using System.Collections.Generic;

public sealed class DeliverObjectiveRuntime :
    FavourObjectiveRuntime
{
    private readonly DeliverObjectiveData
        deliverData;

    private readonly HashSet<int>
        completedDeliveries =
            new();

    private bool
        processingInventoryChange;

    public DeliverObjectiveRuntime(
        DeliverObjectiveData data,
        FavourRuntime favour)
        : base(
            data,
            favour)
    {
        deliverData =
            data;
    }

    public override bool IsComplete =>
        RequiredProgress > 0 &&
        CurrentProgress >=
        RequiredProgress;

    public override int CurrentProgress =>
        completedDeliveries.Count;

    public override int RequiredProgress =>
        deliverData != null
            ? deliverData.RequiredDeliveries
            : 0;

    protected override void OnActivated()
    {
        Inventory inventory =
            ResolveInventory();

        if (inventory != null)
        {
            inventory.OnInventoryChanged +=
                HandleInventoryChanged;
        }
    }

    protected override void OnDeactivated()
    {
        Inventory inventory =
            ResolveInventory();

        if (inventory != null)
        {
            inventory.OnInventoryChanged -=
                HandleInventoryChanged;
        }
    }

    // =========================================================
    // RECIPIENT
    // =========================================================

    public bool RequiresTarget(
        string entityId)
    {
        if (deliverData?.Deliveries == null ||
            string.IsNullOrWhiteSpace(
                entityId))
        {
            return false;
        }

        for (int i = 0;
             i < deliverData.Deliveries.Count;
             i++)
        {
            if (completedDeliveries.Contains(i))
                continue;

            DeliveryEntry delivery =
                deliverData.Deliveries[i];

            if (!IsValidDelivery(
                    delivery))
            {
                continue;
            }

            if (EntityTargetUtility.Matches(
                    entityId,
                    delivery.TargetId))
            {
                return true;
            }
        }

        return false;
    }

    public bool CanDeliverTo(
        string entityId)
    {
        if (!IsActive ||
            IsComplete ||
            string.IsNullOrWhiteSpace(
                entityId))
        {
            return false;
        }

        Inventory inventory =
            ResolveInventory();

        if (inventory == null ||
            deliverData?.Deliveries == null)
        {
            return false;
        }

        for (int i = 0;
             i < deliverData.Deliveries.Count;
             i++)
        {
            if (completedDeliveries.Contains(i))
                continue;

            DeliveryEntry delivery =
                deliverData.Deliveries[i];

            if (!IsValidDelivery(
                    delivery))
            {
                continue;
            }

            if (!EntityTargetUtility.Matches(
                    entityId,
                    delivery.TargetId))
            {
                continue;
            }

            if (inventory.GetItemCount(
                    delivery.Item) >=
                delivery.Amount)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryDeliverTo(
        string entityId)
    {
        if (!CanDeliverTo(
                entityId))
        {
            return false;
        }

        Inventory inventory =
            ResolveInventory();

        if (inventory == null)
            return false;

        /*
         * En recipient kan ha flera deliveries.
         *
         * Exempel:
         * Hammer + Letter → Fanarik.
         *
         * Vi bygger därför en enda atomisk inventory-
         * transaction för samtliga deliveries till targetet.
         */
        List<InventoryItemAmount> removals =
            new();

        List<int> deliveryIndices =
            new();

        for (int i = 0;
             i < deliverData.Deliveries.Count;
             i++)
        {
            if (completedDeliveries.Contains(i))
                continue;

            DeliveryEntry delivery =
                deliverData.Deliveries[i];

            if (!IsValidDelivery(
                    delivery))
            {
                continue;
            }

            if (!EntityTargetUtility.Matches(
                    entityId,
                    delivery.TargetId))
            {
                continue;
            }

            AddOrMergeAmount(
                removals,
                delivery.Item,
                delivery.Amount
            );

            deliveryIndices.Add(i);
        }

        if (deliveryIndices.Count == 0)
            return false;

        foreach (InventoryItemAmount removal
                 in removals)
        {
            if (inventory.GetItemCount(
                    removal.Item) <
                removal.Amount)
            {
                return false;
            }
        }

        processingInventoryChange =
            true;

        try
        {
            bool succeeded =
                inventory.TryApplyTransaction(
                    removals,
                    Array.Empty<
                        InventoryItemAmount>(),
                    notifyIfInventoryFull: false
                );

            if (!succeeded)
                return false;

            foreach (int index
                     in deliveryIndices)
            {
                completedDeliveries.Add(
                    index
                );
            }
        }
        finally
        {
            processingInventoryChange =
                false;
        }

        RaiseProgressChanged();

        return true;
    }

    // =========================================================
    // ACCEPT ITEMS
    // =========================================================

    internal void CollectMissingStartingItems(
        List<InventoryItemAmount> additions,
        Inventory inventory)
    {
        if (additions == null ||
            inventory == null ||
            deliverData?.Deliveries == null)
        {
            return;
        }

        List<InventoryItemAmount> required =
            new();

        foreach (DeliveryEntry delivery
                 in deliverData.Deliveries)
        {
            if (!IsValidDelivery(
                    delivery))
            {
                continue;
            }

            AddOrMergeAmount(
                required,
                delivery.Item,
                delivery.Amount
            );
        }

        foreach (InventoryItemAmount requirement
                 in required)
        {
            int current =
                inventory.GetItemCount(
                    requirement.Item
                );

            int missing =
                Math.Max(
                    0,
                    requirement.Amount -
                    current
                );

            if (missing <= 0)
                continue;

            AddOrMergeAmount(
                additions,
                requirement.Item,
                missing
            );
        }
    }

    // =========================================================
    // LOST ITEM WATCH
    // =========================================================

    private void HandleInventoryChanged()
    {
        if (processingInventoryChange ||
            !IsActive ||
            IsComplete)
        {
            return;
        }

        if (HasAllRequiredRemainingItems())
            return;

        Favour?.ResetForReaccept();
    }

    private bool HasAllRequiredRemainingItems()
    {
        Inventory inventory =
            ResolveInventory();

        if (inventory == null ||
            deliverData?.Deliveries == null)
        {
            return false;
        }

        List<InventoryItemAmount> required =
            new();

        for (int i = 0;
             i < deliverData.Deliveries.Count;
             i++)
        {
            if (completedDeliveries.Contains(i))
                continue;

            DeliveryEntry delivery =
                deliverData.Deliveries[i];

            if (!IsValidDelivery(
                    delivery))
            {
                continue;
            }

            AddOrMergeAmount(
                required,
                delivery.Item,
                delivery.Amount
            );
        }

        foreach (InventoryItemAmount requirement
                 in required)
        {
            if (inventory.GetItemCount(
                    requirement.Item) <
                requirement.Amount)
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // PRESENTATION
    // =========================================================

    public override bool IsRelevantToEntity(
        string entityId)
    {
        if (deliverData == null ||
            string.IsNullOrWhiteSpace(
                entityId))
        {
            return false;
        }

        /*
         * När hela objective't är färdigt behåller vi
         * presentation-relevansen fram till favour turn-in.
         */
        if (IsComplete)
        {
            foreach (DeliveryEntry delivery
                     in deliverData.Deliveries)
            {
                if (IsValidDelivery(delivery) &&
                    EntityTargetUtility.Matches(
                        entityId,
                        delivery.TargetId))
                {
                    return true;
                }
            }

            return false;
        }

        return RequiresTarget(
            entityId
        );
    }

    // =========================================================
    // RESET
    // =========================================================

    public override void ResetProgress()
    {
        if (completedDeliveries.Count == 0)
            return;

        completedDeliveries.Clear();

        RaiseProgressChanged();
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private Inventory ResolveInventory()
    {
        Inventory inventory =
            Favour?.Manager?.PlayerInventory;

        return inventory != null
            ? inventory
            : Inventory.Instance;
    }

    private static bool IsValidDelivery(
        DeliveryEntry delivery)
    {
        return
            delivery != null &&
            delivery.Item != null &&
            delivery.Amount > 0 &&
            !string.IsNullOrWhiteSpace(
                delivery.TargetId
            );
    }

    private static void AddOrMergeAmount(
        List<InventoryItemAmount> amounts,
        ItemData item,
        int amount)
    {
        if (amounts == null ||
            item == null ||
            amount <= 0)
        {
            return;
        }

        for (int i = 0;
             i < amounts.Count;
             i++)
        {
            InventoryItemAmount existing =
                amounts[i];

            if (!Inventory.ItemsMatch(
                    existing.Item,
                    item))
            {
                continue;
            }

            amounts[i] =
                new InventoryItemAmount(
                    existing.Item,
                    existing.Amount +
                    amount
                );

            return;
        }

        amounts.Add(
            new InventoryItemAmount(
                item,
                amount
            )
        );
    }
}
