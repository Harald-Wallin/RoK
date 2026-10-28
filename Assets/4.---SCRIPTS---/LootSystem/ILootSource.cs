using System.Collections.Generic;

public interface ILootSource
{
    string LootTitle { get; }

    IReadOnlyList<ItemData> LootItems { get; }

    int CoinAmount { get; }

    bool HasLoot { get; }

    int GetItemQuantity(
        ItemData item);

    bool TryTakeItems(
        ItemData item,
        int quantity);

    int TakeAllCoins();

    void RefreshLootVisuals();
}