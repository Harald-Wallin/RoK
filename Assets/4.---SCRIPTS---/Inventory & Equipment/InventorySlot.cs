using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public InventoryContainerLoot ContainerLoot
    {
        get;
        private set;
    }

    public bool IsEmpty()
    {
        return item == null || amount <= 0;
    }

    public void SetItem(
        ItemData newItem,
        int newAmount = 1,
        InventoryContainerLoot container = null)
    {
        if (newItem == null || newAmount <= 0)
        {
            Clear();
            return;
        }

        item = newItem;

        amount = newItem.IsContainer || !newItem.stackable
            ? 1
            : Mathf.Clamp(
                newAmount,
                1,
                Mathf.Max(1, newItem.maxStack));

        ContainerLoot = newItem.IsContainer
            ? container ?? new InventoryContainerLoot(newItem)
            : null;
    }

    public InventoryContainerLoot EnsureContainerLoot()
    {
        if (IsEmpty() || !item.IsContainer)
            return null;

        return ContainerLoot ??=
            new InventoryContainerLoot(item);
    }

    public void CopyFrom(InventorySlot source)
    {
        if (source == null || source.IsEmpty())
        {
            Clear();
            return;
        }

        SetItem(
            source.item,
            source.amount,
            source.EnsureContainerLoot());
    }

    public void AddAmount(int value)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        amount += value;

        if (amount <= 0)
        {
            Clear();
            return;
        }

        amount = item.IsContainer || !item.stackable
            ? 1
            : Mathf.Min(
                amount,
                Mathf.Max(1, item.maxStack));
    }

    public void Clear()
    {
        item = null;
        amount = 0;
        ContainerLoot = null;
    }
}