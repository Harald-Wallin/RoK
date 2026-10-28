using UnityEngine;

public enum LootEntryType
{
    Item,
    Coins
}

public enum PlayerLootPolicy
{
    Normal,
    OncePerPlayer
}

[System.Serializable]
public sealed class LootEntry
{
    [SerializeField]
    private LootEntryType type =
        LootEntryType.Item;

    [SerializeField]
    private ItemData item;

    [Header("Player Loot")]

    [SerializeField]
    private PlayerLootPolicy playerLootPolicy =
    PlayerLootPolicy.Normal;

    [SerializeField]
    [Range(0f, 1f)]
    private float dropChance = 1f;

    [SerializeField]
    [Min(1)]
    private int minQuantity = 1;

    [SerializeField]
    [Min(1)]
    private int maxQuantity = 1;

    public PlayerLootPolicy PlayerLootPolicy =>
    playerLootPolicy;

    public LootEntryType Type =>
        type;

    public ItemData Item =>
        item;

    public float DropChance =>
        Mathf.Clamp01(
            dropChance
        );

    public int MinQuantity =>
        Mathf.Max(
            1,
            minQuantity
        );

    public int MaxQuantity =>
        Mathf.Max(
            MinQuantity,
            maxQuantity
        );

    public bool IsValid
    {
        get
        {
            switch (type)
            {
                case LootEntryType.Item:
                    return item != null;

                case LootEntryType.Coins:
                    return true;

                default:
                    return false;
            }
        }
    }

#if UNITY_EDITOR
    public void Normalize()
    {
        dropChance =
            Mathf.Clamp01(
                dropChance
            );

        minQuantity =
            Mathf.Max(
                1,
                minQuantity
            );

        maxQuantity =
            Mathf.Max(
                minQuantity,
                maxQuantity
            );

        if (type ==
            LootEntryType.Coins)
        {
            item = null;
        }
    }
#endif
}