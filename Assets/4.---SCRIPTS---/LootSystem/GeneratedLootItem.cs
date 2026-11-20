using UnityEngine;

public sealed class GeneratedLootItem
{
    public ItemData Item { get; }

    public PlayerLootPolicy PlayerLootPolicy { get; }

    public string LootTableId { get; }

    public int EntryIndex { get; }

    public InventoryContainerLoot ContainerLoot { get; }

    public GeneratedLootItem(
        ItemData item,
        PlayerLootPolicy playerLootPolicy,
        string lootTableId,
        int entryIndex,
        InventoryContainerLoot containerLoot = null)
    {
        Item = item;
        PlayerLootPolicy = playerLootPolicy;
        LootTableId = lootTableId ?? string.Empty;
        EntryIndex = Mathf.Max(0, entryIndex);
        ContainerLoot = containerLoot;
    }
}