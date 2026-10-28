using System.Collections.Generic;
using UnityEngine;

public class Vendor : MonoBehaviour, IInteractionOption
{
    [System.Serializable]
    public class BuybackEntry
    {
        public ItemData item;
        public int quantity;
        public int pricePaid;
        public float expiryTime;
    }

    [Header("Buyback Settings")]
    [SerializeField] private int maxBuybackSlots = 5;
    [SerializeField] private float buybackLifetime = 1200f;

    private List<VendorItemRuntime> runtimeItems = new List<VendorItemRuntime>();
    private List<BuybackEntry> buybackItems = new List<BuybackEntry>();

    public List<VendorItemRuntime> GetItems()
    {
        return runtimeItems;
    }
    public List<BuybackEntry> GetBuybackItems()
    {
        return buybackItems;
    }

    [Header("Vendor Settings")]
    [SerializeField] private Faction faction;

    [Header("Items For Sale")]
   
    [SerializeField] private List<VendorItem> itemsForSale = new List<VendorItem>();

    public VendorUI vendorUI;
    public InteractionCategory Category =>
    InteractionCategory.Vendor;

    public InteractionPresentation
        GetPresentation()
    {
        return new InteractionPresentation(
            Category,
            "Trade"
        );
    }

    public string GetStatusText()
    {
        return string.Empty;
    }


    void Start()
    {
        InitializeRuntimeInventory();
    }
    void Update()
    {
        RemoveExpiredBuybacks();
        UpdateRestocks();
    }

    void RemoveExpiredBuybacks()
    {
        for (int i = buybackItems.Count - 1; i >= 0; i--)
        {
            if (Time.time >= buybackItems[i].expiryTime)
            {
                buybackItems.RemoveAt(i);
            }
        }
    }

    void InitializeRuntimeInventory()
    {
        runtimeItems.Clear();

        foreach (var item in itemsForSale)
        {
            runtimeItems.Add(new VendorItemRuntime(item));
        }
    }

    public bool CanInteract(
    in InteractionContext context)
    {
        if (!context.IsValid)
            return false;

        PlayerReputationManager repManager =
            context.Player.GetComponent<
                PlayerReputationManager>();

        return CanTrade(repManager);
    }

    public void Interact(
        in InteractionContext context)
    {
        if (!CanInteract(context))
            return;

        OpenVendor();
    }

    public void OpenVendor()
    {
        VendorUI targetUI =
            vendorUI != null
                ? vendorUI
                : VendorUI.Instance;

        if (targetUI == null)
        {
            Debug.LogWarning(
                $"Vendor '{name}' kunde inte öppnas eftersom " +
                "ingen VendorUI finns tillgänglig.",
                this);

            return;
        }

        targetUI.Open(this);
        InteractionTarget target =
            GetComponentInChildren<
        InteractionTarget>();

        if (target != null)
        {
            GlobalUIManager.Instance?
                .RegisterInteractionWindow(
                    targetUI,
                    target.InteractionTransform,
                    target.WindowCloseDistance);
        }
    }

    public bool CanTrade(PlayerReputationManager repManager)
    {
        if (repManager == null)
            return false;

        NPCReactionController reaction = GetComponent<NPCReactionController>();

        if (reaction != null &&
            reaction.IsTemporarilyHostile)
        {
            return false;
        }

        if (IsTemporarilyHostile())
            return false;

        if (faction == null)
            return true;

        return repManager.GetReputationState(faction)
               >= ReputationState.Indifferent;
    }

    public bool IsTemporarilyHostile()
    {
        NPCReactionController reaction = GetComponent<NPCReactionController>();

        return reaction != null &&
               reaction.IsTemporarilyHostile;
    }

    public void BuyItem(VendorItemRuntime entry)
    {
        if (entry == null || entry.data == null || entry.data.item == null)
            return;

        PlayerStats player = PlayerReference.Player;

        PlayerItemOwnership ownership =
    PlayerFavourManager.Instance?
        .PlayerItems;

        if (ownership != null &&
            !ownership.CanReceive(
                entry.data.item,
                1
            ))
        {
            Debug.Log(
                $"Cannot buy '{entry.data.item.DisplayName}': " +
                "ownership limit reached."
            );

            return;
        }

        // Kontrollera level-krav på VendorItem (VendorItem.requiredLevel)
        if (entry.data.useLevelRequirement && entry.data.requiredLevel > player.level)
        {
            Debug.Log("Level too low to buy this item.");
            return;
        }

        // Kontrollera reputation-krav om item kräver det
        if (entry.data.useReputationRequirement)
        {
            PlayerReputationManager repManager = FindFirstObjectByType<PlayerReputationManager>();

            if (repManager == null || faction == null)
            {
                Debug.Log("Cannot determine reputation for this vendor. Purchase denied.");
                return;
            }

            var state = repManager.GetReputationState(faction);
            if (state < entry.data.requiredReputation)
            {
                Debug.Log("Your reputation is too low to buy this item.");
                return;
            }
        }

        // 🛑 STOPPA KÖP OM STOCK = 0
        if (entry.data.stockType == VendorStockType.Limited && entry.currentStock <= 0)
        {
            Debug.Log("Item out of stock.");
            return;
        }

        int price = GetModifiedPrice(entry.data);

        if (price < 0)
        {
            Debug.Log("Vendor refuses to trade with you.");
            return;
        }

        if (!PlayerCurrency.Instance.TrySpendCoins(price))
        {
            Debug.Log("Not enough coins.");
            return;
        }

        bool added = Inventory.Instance.AddItem(entry.data.item, 1);

        if (!added)
        {
            Debug.Log("Inventory full.");
            PlayerCurrency.Instance.AddCoins(price);
            return;
        }

        // 🔻 Minska lager om item inte är infinite
        if (entry.data.stockType == VendorStockType.Limited)
        {
            entry.currentStock--;

            if (entry.currentStock <= 0)
            {
                entry.currentStock = 0;

                if (entry.data.restockTime > 0)
                    entry.restockTimer = entry.data.restockTime;
            }
        }

        //Debug.Log($"Bought {entry.data.item.itemName} for {price} coins.");
    }

    public int GetModifiedPrice(VendorItem entry)
    {

        int basePrice = entry.vendorPrice;

        PlayerReputationManager rep =
            FindFirstObjectByType<PlayerReputationManager>();

        if (rep == null || faction == null)
            return basePrice;

        ReputationState state = rep.GetReputationState(faction);

        float modifier = 1f;

        switch (state)
        {
            case ReputationState.Hated:
                return -1;

            case ReputationState.Untrusted:
                modifier = 1.15f;
                break;

            case ReputationState.Indifferent:
                modifier = 1f;
                break;

            case ReputationState.Favoured:
                modifier = 0.97f;
                break;

            case ReputationState.Renowned:
                modifier = 0.94f;
                break;

            case ReputationState.Praised:
                modifier = 0.91f;
                break;

            case ReputationState.Revered:
                modifier = 0.88f;
                break;
        }

        return Mathf.RoundToInt(basePrice * modifier);
    }

    public bool TryBuyFromPlayer(
    ItemData item,
    int inventorySlot,
    int quantity)
    {
        if (item == null ||
            quantity <= 0)
        {
            return false;
        }

        Inventory inventory =
            Inventory.Instance;

        PlayerCurrency currency =
            PlayerCurrency.Instance;

        if (inventory == null ||
            currency == null)
        {
            Debug.LogError(
                $"Vendor '{name}' kunde inte genomföra försäljningen " +
                $"eftersom Inventory eller PlayerCurrency saknas.",
                this
            );

            return false;
        }

        bool removed =
            inventory.TryRemoveItemAt(
                inventorySlot,
                item,
                quantity
            );

        if (!removed)
        {
            return false;
        }

        int totalPrice =
            Mathf.Max(
                0,
                item.SellPrice * quantity
            );

        currency.AddCoins(
            totalPrice
        );

        AddBuybackItem(
            item,
            quantity,
            totalPrice
        );

        return true;
    }

    private void AddBuybackItem(
    ItemData item,
    int quantity,
    int totalPrice)
    {
        if (item == null ||
            quantity <= 0)
        {
            return;
        }

        int safeQuantity =
            Mathf.Max(
                1,
                quantity
            );

        int pricePerItem =
            Mathf.Max(
                0,
                totalPrice / safeQuantity
            );

        int remaining =
            safeQuantity;

        // =========================================================
        // STACKABLE
        // =========================================================

        if (item.stackable)
        {
            int maxStack =
                Mathf.Max(
                    1,
                    item.maxStack
                );

            /*
             * Fyll först befintliga buyback-stacks
             * av samma item.
             */
            for (int i = 0;
                 i < buybackItems.Count &&
                 remaining > 0;
                 i++)
            {
                BuybackEntry entry =
                    buybackItems[i];

                if (entry == null ||
                    entry.item != item ||
                    entry.quantity >= maxStack)
                {
                    continue;
                }

                int availableSpace =
                    maxStack -
                    entry.quantity;

                int amountToAdd =
                    Mathf.Min(
                        availableSpace,
                        remaining
                    );

                entry.quantity +=
                    amountToAdd;

                entry.pricePaid +=
                    pricePerItem *
                    amountToAdd;

                entry.expiryTime =
                    Time.time +
                    buybackLifetime;

                remaining -=
                    amountToAdd;
            }

            /*
             * Skapa nya buyback-stacks för
             * eventuell återstående mängd.
             */
            while (remaining > 0)
            {
                int amountToAdd =
                    Mathf.Min(
                        maxStack,
                        remaining
                    );

                BuybackEntry newEntry =
                    new BuybackEntry
                    {
                        item =
                            item,

                        quantity =
                            amountToAdd,

                        pricePaid =
                            pricePerItem *
                            amountToAdd,

                        expiryTime =
                            Time.time +
                            buybackLifetime
                    };

                buybackItems.Insert(
                    0,
                    newEntry
                );

                remaining -=
                    amountToAdd;
            }
        }

        // =========================================================
        // NON-STACKABLE
        // =========================================================

        else
        {
            for (int i = 0;
                 i < safeQuantity;
                 i++)
            {
                BuybackEntry newEntry =
                    new BuybackEntry
                    {
                        item =
                            item,

                        quantity =
                            1,

                        pricePaid =
                            pricePerItem,

                        expiryTime =
                            Time.time +
                            buybackLifetime
                    };

                buybackItems.Insert(
                    0,
                    newEntry
                );
            }
        }

        // =========================================================
        // BUYBACK LIMIT
        // =========================================================

        int safeMaxSlots =
            Mathf.Max(
                1,
                maxBuybackSlots
            );

        while (buybackItems.Count >
            safeMaxSlots)
        {
            buybackItems.RemoveAt(
                buybackItems.Count - 1
            );
        }
    }

    void UpdateRestocks()
    {
        foreach (var item in runtimeItems)
        {
            if (item.data.stockType == VendorStockType.Infinite)
                continue;

            if (item.currentStock > 0)
                continue;

            if (item.data.restockTime <= 0)
                continue;

            item.restockTimer -= Time.deltaTime;

            if (item.restockTimer <= 0f)
            {
                item.currentStock = item.data.maxStock;
                item.restockTimer = 0f;

                if (VendorUI.Instance != null && VendorUI.Instance.ActiveVendor == this)
                {
                    VendorUI.Instance.RefreshVendorUI();
                }
            }
        }
    }
    public Faction GetFaction()
    {
        return faction;
    }
}