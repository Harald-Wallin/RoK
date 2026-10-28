using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public bool IsEmpty()
    {
        return item == null ||
               amount <= 0;
    }

    public void SetItem(
        ItemData newItem,
        int newAmount = 1)
    {
        if (newItem == null ||
            newAmount <= 0)
        {
            Clear();
            return;
        }

        item =
            newItem;

        if (!newItem.stackable)
        {
            amount = 1;
            return;
        }

        amount =
            Mathf.Clamp(
                newAmount,
                1,
                Mathf.Max(
                    1,
                    newItem.maxStack
                )
            );
    }

    public void AddAmount(
        int value)
    {
        if (item == null)
        {
            amount = 0;
            return;
        }

        amount +=
            value;

        if (amount <= 0)
        {
            Clear();
            return;
        }

        if (!item.stackable)
        {
            amount = 1;
            return;
        }

        amount =
            Mathf.Min(
                amount,
                Mathf.Max(
                    1,
                    item.maxStack
                )
            );
    }

    public void Clear()
    {
        item =
            null;

        amount =
            0;
    }
}