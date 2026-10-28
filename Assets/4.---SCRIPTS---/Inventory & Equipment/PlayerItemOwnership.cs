using System;
using System.Collections.Generic;

public sealed class PlayerItemOwnership
{
    private readonly Inventory inventory;
    private readonly EquipmentManager equipment;

    private readonly List<InventoryItemAmount>
        inventoryRemovals = new();

    private readonly List<EquipmentSlotUI>
        equipmentRemovals = new();

    public event Action Changed;

    public Inventory Inventory => inventory;

    public EquipmentManager Equipment => equipment;

    public PlayerItemOwnership(
        Inventory inventory,
        EquipmentManager equipment)
    {
        this.inventory = inventory;
        this.equipment = equipment;

        if (inventory != null)
        {
            inventory.OnInventoryChanged +=
                HandleSourceChanged;
        }

        if (equipment != null)
        {
            equipment.OnEquipmentChanged +=
                HandleSourceChanged;
        }
    }

    public void Dispose()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -=
                HandleSourceChanged;
        }

        if (equipment != null)
        {
            equipment.OnEquipmentChanged -=
                HandleSourceChanged;
        }
    }

    public int GetItemCount(
        ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;

        if (inventory != null)
        {
            total += inventory.GetItemCount(
                item
            );
        }

        if (equipment != null)
        {
            total += equipment.GetEquippedItemCount(
                item
            );
        }

        return total;
    }

    public bool Contains(
        ItemData item,
        int amount = 1)
    {
        if (item == null ||
            amount <= 0)
        {
            return false;
        }

        return GetItemCount(item) >=
               amount;
    }

    public bool CanReceive(
    ItemData item,
    int amount = 1)
    {
        if (item == null ||
            amount <= 0)
        {
            return false;
        }

        if (!item.HasOwnershipLimit)
            return true;

        int currentAmount =
            GetItemCount(
                item
            );

        return currentAmount + amount <=
               item.MaximumOwned;
    }

    public bool TryApplyTransaction(
        IReadOnlyList<InventoryItemAmount> removals,
        IReadOnlyList<InventoryItemAmount> additions,
        bool notifyIfInventoryFull = true)
    {
        inventoryRemovals.Clear();
        equipmentRemovals.Clear();

        if (!BuildRemovalPlan(
                removals))
        {
            return false;
        }

        bool requiresInventory =
            HasValidEntries(
                inventoryRemovals
            ) ||
            HasValidEntries(
                additions
            );

        if (requiresInventory)
        {
            if (inventory == null)
                return false;

            if (!inventory.CanApplyTransaction(
                    inventoryRemovals,
                    additions,
                    notifyIfInventoryFull))
            {
                return false;
            }
        }

        /*
         * Allt är nu validerat.
         *
         * Inventory-transaktionen kan därför committas först.
         * Equipment-removals är redan planerade mot konkreta
         * equipment-slots.
         */
        if (requiresInventory)
        {
            if (!inventory.TryApplyTransaction(
                    inventoryRemovals,
                    additions,
                    notifyIfInventoryFull))
            {
                return false;
            }
        }

        if (equipmentRemovals.Count > 0)
        {
            if (equipment == null)
                return false;

            foreach (EquipmentSlotUI slot
                     in equipmentRemovals)
            {
                equipment
                    .RemoveEquippedItemFromSlot(
                        slot
                    );
            }
        }

        return true;
    }

    private bool BuildRemovalPlan(
        IReadOnlyList<InventoryItemAmount> removals)
    {
        if (removals == null)
            return true;

        foreach (InventoryItemAmount removal
                 in removals)
        {
            if (!removal.IsValid)
                continue;

            int totalAvailable =
                GetItemCount(
                    removal.Item
                );

            if (totalAvailable <
                removal.Amount)
            {
                return false;
            }

            int remaining =
                removal.Amount;

            int inventoryAvailable =
                inventory != null
                    ? inventory.GetItemCount(
                        removal.Item
                    )
                    : 0;

            int removeFromInventory =
                Math.Min(
                    inventoryAvailable,
                    remaining
                );

            if (removeFromInventory > 0)
            {
                inventoryRemovals.Add(
                    new InventoryItemAmount(
                        removal.Item,
                        removeFromInventory
                    )
                );

                remaining -=
                    removeFromInventory;
            }

            if (remaining <= 0)
                continue;

            if (equipment == null)
                return false;

            foreach (EquipmentSlotUI slot
                     in equipment.equipmentSlots)
            {
                if (remaining <= 0)
                    break;

                if (slot == null)
                    continue;

                ItemData equippedItem =
                    slot.GetEquippedItem();

                if (!Inventory.ItemsMatch(
                        equippedItem,
                        removal.Item))
                {
                    continue;
                }

                equipmentRemovals.Add(
                    slot
                );

                remaining--;
            }

            if (remaining > 0)
                return false;
        }

        return true;
    }

    private static bool HasValidEntries(
        IReadOnlyList<InventoryItemAmount> entries)
    {
        if (entries == null)
            return false;

        foreach (InventoryItemAmount entry
                 in entries)
        {
            if (entry.IsValid)
                return true;
        }

        return false;
    }

    private void HandleSourceChanged()
    {
        Changed?.Invoke();
    }
}
