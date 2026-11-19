using System.Collections.Generic;
using UnityEngine;

public interface IInventoryLootTransfer
{
    bool TryTransferItems(ItemData item, int quantity);
    bool TryTransferCoins(PlayerCurrency currency);
}

public sealed class InventoryContainerLoot :
    ILootSource,
    IInventoryLootTransfer
{
    private readonly ItemData definition;
    private readonly LootContents contents = new();

    private Inventory inventory;
    private bool rolled;
    private bool subscribed;

    public InventoryContainerLoot(ItemData definition)
    {
        this.definition = definition;
    }

    public ItemData Definition => definition;

    public string LootTitle =>
        definition.DisplayName;

    public IReadOnlyList<ItemData> LootItems =>
        contents.Items;

    public int CoinAmount =>
        IsOwned ? contents.CoinAmount : 0;

    public bool HasLoot =>
        IsOwned && contents.HasLoot;

    internal bool ContentsEmpty =>
        !contents.HasLoot;

    public bool IsOwned =>
        FindSlotIndex() >= 0;

    internal int FindSlotIndex()
    {
        if (inventory == null)
            return -1;

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            InventorySlot slot = inventory.slots[i];

            if (slot != null &&
                !slot.IsEmpty() &&
                ReferenceEquals(slot.ContainerLoot, this) &&
                Inventory.ItemsMatch(slot.item, definition))
            {
                return i;
            }
        }

        return -1;
    }

    public void Open(Inventory owner)
    {
        if (owner == null || LootUI.Instance == null)
            return;

        if (inventory != owner)
            Detach();

        inventory = owner;

        if (!IsOwned)
            return;

        if (!rolled)
        {
            contents.Apply(
                LootGenerator.GenerateLootResult(
                    new List<LootTable>(
                        definition.ContainerLootTables),
                    definition.ContainerMinRolls,
                    definition.ContainerMaxRolls));

            rolled = true;
        }

        if (!subscribed)
        {
            inventory.OnInventoryChanged +=
                HandleInventoryChanged;

            subscribed = true;
        }

        if (HasLoot)
            LootUI.Instance.Show(this);
        else
            RefreshLootVisuals();
    }

    public int GetItemQuantity(ItemData item)
    {
        return IsOwned
            ? contents.GetItemQuantity(item)
            : 0;
    }

    public bool TryTransferItems(
        ItemData item,
        int quantity)
    {
        return IsOwned &&
               inventory.TryLootContainerItem(
                   this,
                   item,
                   quantity);
    }

    public bool TryTransferCoins(
        PlayerCurrency currency)
    {
        if (currency == null ||
            !IsOwned ||
            contents.CoinAmount <= 0 ||
            contents.CoinAmount >
                int.MaxValue - currency.Coins)
        {
            return false;
        }

        int amount = contents.TakeAllCoins();

        currency.AddCoins(amount);
        RefreshLootVisuals();

        return true;
    }

    internal bool RemovesContainerWhenTaking(
        ItemData item,
        int quantity)
    {
        return quantity > 0 &&
               contents.CoinAmount == 0 &&
               contents.Items.Count == quantity &&
               contents.GetItemQuantity(item) == quantity;
    }

    internal bool RemoveContents(
        ItemData item,
        int quantity)
    {
        return contents.TryTakeItems(item, quantity);
    }

    public bool TryTakeItems(
        ItemData item,
        int quantity)
    {
        if (!IsOwned ||
            !contents.TryTakeItems(item, quantity))
        {
            return false;
        }

        RefreshLootVisuals();
        return true;
    }

    public int TakeAllCoins()
    {
        if (!IsOwned)
            return 0;

        int amount = contents.TakeAllCoins();
        RefreshLootVisuals();

        return amount;
    }

    public void RefreshLootVisuals()
    {
        if (!IsOwned)
        {
            Detach();
            return;
        }

        if (rolled && ContentsEmpty)
            inventory.RemoveEmptyContainer(this);
    }

    private void HandleInventoryChanged()
    {
        if (!IsOwned)
            Detach();
    }

    private void Detach()
    {
        if (subscribed && inventory != null)
        {
            inventory.OnInventoryChanged -=
                HandleInventoryChanged;
        }

        subscribed = false;

        LootUI.Instance?.CloseIfShowing(this);
    }
}
