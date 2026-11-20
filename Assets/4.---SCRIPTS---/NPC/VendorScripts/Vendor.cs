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
        public InventoryContainerLoot containerLoot;
    }

    [Header("Buyback Settings")]
    [SerializeField] private int maxBuybackSlots = 5;
    [SerializeField] private float buybackLifetime = 1200f;

    [Header("Vendor Settings")]
    [SerializeField] private Faction faction;

    [Tooltip(
        "Empty = coins. Assign an ItemType.Trophy to charge " +
        "that trophy for purchases. Selling and buyback always use coins.")]
    [SerializeField]
    private ItemData paymentTrophy;

    [Header("Items For Sale")]
    [SerializeField]
    private List<VendorItem> itemsForSale = new();

    public VendorUI vendorUI;

    private bool trading;

    private readonly List<VendorItemRuntime> runtimeItems = new();
    private readonly List<BuybackEntry> buybackItems = new();

    public bool UsesTrophies => paymentTrophy != null;
    public ItemData PaymentTrophy => paymentTrophy;

    public InteractionCategory Category =>
        InteractionCategory.Vendor;

    public List<VendorItemRuntime> GetItems() =>
        runtimeItems;

    public List<BuybackEntry> GetBuybackItems() =>
        buybackItems;

    public Faction GetFaction() => faction;

    public InteractionPresentation GetPresentation() =>
        new(Category, "Trade");

    public string GetStatusText() =>
        string.Empty;

    private void Start()
    {
        runtimeItems.Clear();

        foreach (VendorItem item in itemsForSale)
        {
            if (item != null && item.item != null)
                runtimeItems.Add(new VendorItemRuntime(item));
        }
    }

    private void Update()
    {
        bool expired = false;

        for (int i = buybackItems.Count - 1; i >= 0; i--)
        {
            if (Time.time < buybackItems[i].expiryTime)
                continue;

            buybackItems.RemoveAt(i);
            expired = true;
        }

        if (expired &&
            VendorUI.Instance != null &&
            VendorUI.Instance.ActiveVendor == this)
        {
            VendorUI.Instance.RefreshBuybackUI();
        }

        UpdateRestocks();
    }

    public bool CanInteract(
        in InteractionContext context)
    {
        return context.IsValid &&
               CanTrade(
                   context.Player.GetComponent<PlayerReputationManager>());
    }

    public void Interact(
        in InteractionContext context)
    {
        if (CanInteract(context))
            OpenVendor();
    }

    public void OpenVendor()
    {
        VendorUI ui =
            vendorUI != null ? vendorUI : VendorUI.Instance;

        if (ui == null)
            return;

        ui.Open(this);

        InteractionTarget target =
            GetComponentInChildren<InteractionTarget>();

        if (target != null)
        {
            GlobalUIManager.Instance?.RegisterInteractionWindow(
                ui,
                target.InteractionTransform,
                target.WindowCloseDistance);
        }
    }

    public bool CanTrade(
        PlayerReputationManager reputation)
    {
        return reputation != null &&
               !IsTemporarilyHostile() &&
               (faction == null ||
                reputation.GetReputationState(faction) >=
                    ReputationState.Indifferent);
    }

    public bool IsTemporarilyHostile()
    {
        NPCReactionController reaction =
            GetComponent<NPCReactionController>();

        return reaction != null &&
               reaction.IsTemporarilyHostile;
    }

    public int GetModifiedPrice(VendorItem entry)
    {
        if (entry == null || entry.vendorPrice < 0)
            return -1;

        PlayerStats player =
            PlayerReference.Player;

        PlayerReputationManager reputation =
            player != null
                ? player.GetComponent<PlayerReputationManager>()
                : null;

        if (reputation == null || faction == null)
            return entry.vendorPrice;

        ReputationState state =
            reputation.GetReputationState(faction);

        if (state == ReputationState.Hated)
            return -1;

        float multiplier = state switch
        {
            ReputationState.Untrusted => 1.15f,
            ReputationState.Favoured => 0.97f,
            ReputationState.Renowned => 0.94f,
            ReputationState.Praised => 0.91f,
            ReputationState.Revered => 0.88f,
            _ => 1f
        };

        return Mathf.Max(
            0,
            Mathf.RoundToInt(entry.vendorPrice * multiplier));
    }

    public string GetPriceText(VendorItem entry)
    {
        int price = GetModifiedPrice(entry);

        return price >= 0
            ? price.ToString()
            : "Unavailable";
    }

    public bool CanAfford(VendorItem entry)
    {
        int price = GetModifiedPrice(entry);

        if (price < 0)
            return false;

        if (UsesTrophies)
        {
            return paymentTrophy.IsTrophy &&
                   Inventory.Instance != null &&
                   Inventory.Instance.GetItemCount(paymentTrophy) >= price;
        }

        return price == 0 ||
               (PlayerCurrency.Instance != null &&
                PlayerCurrency.Instance.HasCoins(price));
    }

    public bool CanPurchase(
    VendorItemRuntime entry)
    {
        PlayerStats player =
            PlayerReference.Player;

        if (Inventory.Instance == null ||
            player == null ||
            entry?.data?.item == null ||
            !runtimeItems.Contains(entry) ||
            !MeetsPurchaseRequirements(entry.data) ||
            !CanTrade(
                player.GetComponent<PlayerReputationManager>()) ||
            (entry.data.stockType == VendorStockType.Limited &&
             entry.currentStock <= 0))
        {
            return false;
        }

        PlayerItemOwnership ownership =
            PlayerFavourManager.Instance?.PlayerItems;

        if (ownership != null &&
            !ownership.CanReceive(entry.data.item, 1))
        {
            return false;
        }

        return CanAfford(entry.data);
    }

    public bool MeetsPurchaseRequirements(
    VendorItem offer)
    {
        PlayerStats player =
            PlayerReference.Player;

        if (player == null ||
            offer == null ||
            offer.item == null ||
            !offer.item.MeetsRequirements(player))
        {
            return false;
        }

        if (offer.useLevelRequirement &&
            player.level < offer.requiredLevel)
        {
            return false;
        }

        if (offer.useReputationRequirement)
        {
            PlayerReputationManager reputation =
                player.GetComponent<PlayerReputationManager>();

            if (reputation == null ||
                faction == null ||
                reputation.GetReputationState(faction) <
                    offer.requiredReputation)
            {
                return false;
            }
        }

        return true;
    }

    public void BuyItem(VendorItemRuntime entry)
    {
        if (trading)
            return;

        trading = true;

        try
        {
            if (!CanPurchase(entry))
                return;

            Inventory inventory = Inventory.Instance;
            VendorItem offer = entry.data;
            ItemData item = offer.item;

            int price = GetModifiedPrice(offer);
            bool added;

            if (UsesTrophies)
            {
                added = inventory.TryApplyTransaction(
                    price > 0
                        ? new[]
                        {
                            new InventoryItemAmount(
                                paymentTrophy,
                                price)
                        }
                        : null,
                    new[]
                    {
                        new InventoryItemAmount(item, 1)
                    });
            }
            else
            {
                PlayerCurrency currency =
                    PlayerCurrency.Instance;

                if (price > 0 &&
                    (currency == null ||
                     !currency.TrySpendCoins(price)))
                {
                    return;
                }

                added = inventory.AddItem(item, 1);

                if (!added && price > 0)
                    currency.AddCoins(price);
            }

            if (!added)
                return;

            if (offer.stockType == VendorStockType.Limited)
            {
                entry.currentStock =
                    Mathf.Max(0, entry.currentStock - 1);

                if (entry.currentStock == 0 &&
                    offer.restockTime > 0f)
                {
                    entry.restockTimer = offer.restockTime;
                }
            }
        }
        finally
        {
            trading = false;
        }
    }

    public bool TryBuyFromPlayer(
        ItemData item,
        int inventorySlot,
        int quantity)
    {
        if (trading)
            return false;

        trading = true;

        try
        {
            Inventory inventory = Inventory.Instance;
            PlayerCurrency currency = PlayerCurrency.Instance;
            PlayerStats player = PlayerReference.Player;

            if (item == null ||
                quantity <= 0 ||
                inventory == null ||
                currency == null ||
                player == null ||
                !CanTrade(player.GetComponent<PlayerReputationManager>()) ||
                inventorySlot < 0 ||
                inventorySlot >= inventory.slots.Count)
            {
                return false;
            }

            InventorySlot slot =
                inventory.slots[inventorySlot];

            if (slot == null ||
                slot.IsEmpty() ||
                !Inventory.ItemsMatch(slot.item, item) ||
                slot.amount < quantity ||
                (item.IsContainer && quantity != 1))
            {
                return false;
            }

            long total =
                (long)Mathf.Max(0, item.SellPrice) * quantity;

            if (total > int.MaxValue - currency.Coins)
                return false;

            InventoryContainerLoot state =
                slot.EnsureContainerLoot();

            if (!inventory.TryRemoveItemAt(
                    inventorySlot,
                    item,
                    quantity))
            {
                return false;
            }

            if (total > 0)
                currency.AddCoins((int)total);

            AddBuybackItem(
                item,
                quantity,
                (int)total,
                state);

            return true;
        }
        finally
        {
            trading = false;
        }
    }

    private void AddBuybackItem(
        ItemData item,
        int quantity,
        int totalPrice,
        InventoryContainerLoot container = null)
    {
        int unitPrice = totalPrice / quantity;
        int remaining = quantity;

        int maxStack = item.stackable && !item.IsContainer
            ? Mathf.Max(1, item.maxStack)
            : 1;

        if (maxStack > 1)
        {
            foreach (BuybackEntry entry in buybackItems)
            {
                if (remaining == 0)
                    break;

                if (entry.item != item ||
                    entry.quantity >= maxStack ||
                    entry.pricePaid / entry.quantity != unitPrice)
                {
                    continue;
                }

                int added = Mathf.Min(
                    remaining,
                    maxStack - entry.quantity);

                entry.quantity += added;
                entry.pricePaid += unitPrice * added;
                entry.expiryTime = Time.time + buybackLifetime;

                remaining -= added;
            }
        }

        while (remaining > 0)
        {
            int added = Mathf.Min(
                remaining,
                maxStack);

            buybackItems.Insert(
                0,
                new BuybackEntry
                {
                    item = item,
                    quantity = added,
                    pricePaid = unitPrice * added,
                    expiryTime = Time.time + buybackLifetime,
                    containerLoot = container
                });

            remaining -= added;
        }

        while (buybackItems.Count >
               Mathf.Max(1, maxBuybackSlots))
        {
            buybackItems.RemoveAt(
                buybackItems.Count - 1);
        }
    }

    public bool TryBuyback(BuybackEntry entry)
    {
        if (trading)
            return false;

        trading = true;

        try
        {
            Inventory inventory = Inventory.Instance;
            PlayerCurrency currency = PlayerCurrency.Instance;
            PlayerStats player = PlayerReference.Player;

            if (entry == null ||
                entry.item == null ||
                entry.quantity <= 0 ||
                inventory == null ||
                currency == null ||
                player == null ||
                !CanTrade(player.GetComponent<PlayerReputationManager>()) ||
                !buybackItems.Contains(entry) ||
                Time.time >= entry.expiryTime ||
                entry.pricePaid < 0)
            {
                return false;
            }

            PlayerItemOwnership ownership =
                PlayerFavourManager.Instance?.PlayerItems;

            if (ownership != null &&
                !ownership.CanReceive(
                    entry.item,
                    entry.quantity))
            {
                return false;
            }

            if (entry.pricePaid > 0 &&
                !currency.TrySpendCoins(entry.pricePaid))
            {
                return false;
            }

            bool added = entry.item.IsContainer
                ? inventory.TryRestoreContainer(
                    entry.item,
                    entry.containerLoot)
                : inventory.AddItem(
                    entry.item,
                    entry.quantity);

            if (!added)
            {
                if (entry.pricePaid > 0)
                    currency.AddCoins(entry.pricePaid);

                return false;
            }

            buybackItems.Remove(entry);
            return true;
        }
        finally
        {
            trading = false;
        }
    }

    private void UpdateRestocks()
    {
        foreach (VendorItemRuntime item in runtimeItems)
        {
            if (item.data.stockType == VendorStockType.Infinite ||
                item.currentStock > 0 ||
                item.data.restockTime <= 0f)
            {
                continue;
            }

            item.restockTimer -= Time.deltaTime;

            if (item.restockTimer > 0f)
                continue;

            item.currentStock = item.data.maxStock;
            item.restockTimer = 0f;

            if (VendorUI.Instance != null &&
                VendorUI.Instance.ActiveVendor == this)
            {
                VendorUI.Instance.RefreshVendorUI();
            }
        }
    }
}