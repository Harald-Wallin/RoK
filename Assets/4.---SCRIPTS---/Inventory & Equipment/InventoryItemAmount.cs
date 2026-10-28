using System;
using UnityEngine;

[Serializable]
public readonly struct InventoryItemAmount
{
    public InventoryItemAmount(
        ItemData item,
        int amount)
    {
        Item = item;
        Amount = amount;
    }

    public ItemData Item
    {
        get;
    }

    public int Amount
    {
        get;
    }

    public bool IsValid =>
        Item != null &&
        Amount > 0;
}
