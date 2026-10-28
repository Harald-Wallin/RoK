using System.Collections.Generic;
using UnityEngine;

public sealed class LootContents
{
    private readonly List<ItemData> items =
        new();

    private int coinAmount;

    private readonly List<GeneratedLootItem>
    generatedItems =
        new();

    public IReadOnlyList<ItemData> Items =>
        items;

    public List<ItemData> MutableItems =>
        items;

    public int CoinAmount =>
        Mathf.Max(
            0,
            coinAmount
        );

    public bool HasLoot =>
        items.Count > 0 ||
        CoinAmount > 0;

    public void Clear()
    {
        items.Clear();
        coinAmount = 0;
    }

    public IReadOnlyList<GeneratedLootItem>
    GeneratedItems =>
        generatedItems;

    public void AddItem(
        ItemData item)
    {
        if (item == null)
            return;

        items.Add(
            item
        );
    }

    public void AddItems(
        IEnumerable<ItemData> newItems)
    {
        if (newItems == null)
            return;

        foreach (ItemData item
                 in newItems)
        {
            AddItem(
                item
            );
        }
    }

    public void AddGeneratedItem(
    GeneratedLootItem generatedItem)
    {
        if (generatedItem == null ||
            generatedItem.Item == null)
        {
            return;
        }

        generatedItems.Add(
            generatedItem
        );

        items.Add(
            generatedItem.Item
        );
    }

    public void AddGeneratedItems(
    IEnumerable<GeneratedLootItem> newItems)
    {
        if (newItems == null)
            return;

        foreach (GeneratedLootItem item
                 in newItems)
        {
            AddGeneratedItem(
                item
            );
        }
    }

    public int GetItemQuantity(
        ItemData item)
    {
        if (item == null)
            return 0;

        int quantity = 0;

        foreach (ItemData containedItem
                 in items)
        {
            if (containedItem != null &&
                Inventory.ItemsMatch(
                    containedItem,
                    item))
            {
                quantity++;
            }
        }

        return quantity;
    }

    public bool TryTakeItems(
        ItemData item,
        int quantity)
    {
        if (item == null ||
            quantity <= 0)
        {
            return false;
        }

        if (GetItemQuantity(item) <
            quantity)
        {
            return false;
        }

        int remaining =
            quantity;

        for (int i =
                 items.Count - 1;
             i >= 0 &&
             remaining > 0;
             i--)
        {
            ItemData containedItem =
                items[i];

            if (containedItem == null ||
                !Inventory.ItemsMatch(
                    containedItem,
                    item))
            {
                continue;
            }

            ItemData removedItem =
    items[i];

            items.RemoveAt(
                i
            );

            RemoveOneGeneratedItem(
                removedItem
            );

            remaining--;
        }

        return remaining == 0;
    }

    private void RemoveOneGeneratedItem(
    ItemData item)
    {
        if (item == null)
            return;

        for (int i =
                 generatedItems.Count - 1;
             i >= 0;
             i--)
        {
            GeneratedLootItem generatedItem =
                generatedItems[i];

            if (generatedItem == null ||
                generatedItem.Item == null)
            {
                continue;
            }

            if (!Inventory.ItemsMatch(
                    generatedItem.Item,
                    item))
            {
                continue;
            }

            generatedItems.RemoveAt(
                i
            );

            return;
        }
    }

    public void SetCoins(
        int amount)
    {
        coinAmount =
            Mathf.Max(
                0,
                amount
            );
    }

    public void AddCoins(
        int amount)
    {
        if (amount <= 0)
            return;

        coinAmount +=
            amount;
    }

    public int TakeAllCoins()
    {
        int taken =
            CoinAmount;

        coinAmount = 0;

        return taken;
    }

    public void Apply(
    LootGenerationResult result)
    {
        Clear();

        if (result == null)
            return;

        AddGeneratedItems(
            result.GeneratedItems
        );

        SetCoins(
            result.Coins
        );
    }
}