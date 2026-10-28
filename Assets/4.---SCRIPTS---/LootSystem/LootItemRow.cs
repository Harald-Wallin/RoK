using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LootItemRow :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Visuals")]

    [SerializeField]
    private TMP_Text itemNameText;

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text amountText;

    [SerializeField]
    private Image border;

    private ItemData item;
    private CurrencyData currency;

    private ILootSource source;
    private LootUI lootUI;

    private Color originalBorderColor;

    private bool IsCurrency =>
        currency != null;

    public void SetupItem(
        ItemData newItem,
        ILootSource lootSource,
        LootUI ui)
    {
        item = newItem;
        currency = null;

        source =
            lootSource;

        lootUI =
            ui;

        if (item == null ||
            source == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        int quantity =
            source.GetItemQuantity(
                item
            );

        itemNameText.text =
            item.DisplayName;

        Color rarityColor =
            ItemRarityColors.GetColor(
                item.rarity
            );

        SetBorderColor(
            rarityColor
        );

        itemNameText.color =
            rarityColor;

        if (iconImage != null)
        {
            iconImage.sprite =
                item.icon;

            iconImage.enabled =
                item.icon != null;
        }

        SetAmountText(
            quantity
        );
    }

    public void SetupCoins(
        CurrencyData currencyData,
        ILootSource lootSource,
        LootUI ui)
    {
        item = null;
        currency = currencyData;

        source =
            lootSource;

        lootUI =
            ui;

        if (currency == null ||
            source == null ||
            source.CoinAmount <= 0)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        itemNameText.text =
            currency.DisplayName;

        itemNameText.color =
            Color.white;

        SetBorderColor(
            Color.white
        );

        if (iconImage != null)
        {
            iconImage.sprite =
                currency.Icon;

            iconImage.enabled =
                currency.Icon != null;
        }

        SetAmountText(
            source.CoinAmount
        );
    }

    public void TakeItem()
    {
        if (IsCurrency)
        {
            TakeCoins();
            return;
        }

        TakeInventoryItem();
    }

    private void TakeInventoryItem()
    {
        if (item == null ||
            source == null ||
            Inventory.Instance == null)
        {
            return;
        }

        int quantity =
            source.GetItemQuantity(
                item
            );

        if (quantity <= 0)
            return;

        /*
         * Inventory modifieras först.
         *
         * Loot-source töms bara om inventory faktiskt
         * kunde ta emot hela mängden.
         */
        bool added =
            Inventory.Instance.AddItem(
                item,
                quantity
            );

        if (!added)
            return;

        bool removed =
            source.TryTakeItems(
                item,
                quantity
            );

        if (!removed)
        {
            Debug.LogError(
                "LootItemRow: Item lades till i inventory men " +
                "kunde inte tas bort från loot source.",
                this
            );

            return;
        }

        FinishTakingLoot();
    }

    private void TakeCoins()
    {
        if (source == null)
            return;

        PlayerCurrency playerCurrency =
            PlayerCurrency.Instance;

        if (playerCurrency == null)
        {
            Debug.LogError(
                "Kan inte loota coins: PlayerCurrency saknas.",
                this
            );

            return;
        }

        int amount =
            source.CoinAmount;

        if (amount <= 0)
            return;

        if (!playerCurrency.AddCoins(
                amount))
        {
            return;
        }

        int taken =
            source.TakeAllCoins();

        if (taken <= 0)
        {
            Debug.LogError(
                "LootItemRow: Coins lades till hos spelaren men " +
                "kunde inte tas bort från loot source.",
                this
            );

            return;
        }

        FinishTakingLoot();
    }

    private void FinishTakingLoot()
    {
        ItemTooltip.Instance?.Hide();

        source?.RefreshLootVisuals();

        lootUI?.Refresh();

        Destroy(
            gameObject
        );
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        ITooltipProvider provider =
            IsCurrency
                ? currency
                : item;

        if (provider == null ||
            ItemTooltip.Instance == null ||
            iconImage == null)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            provider,
            iconImage.rectTransform,
            PlayerReference.Player
        );

        if (border != null)
        {
            border.color =
                Color.Lerp(
                    originalBorderColor,
                    Color.white,
                    0.5f
                );
        }
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();

        if (border != null)
        {
            border.color =
                originalBorderColor;
        }
    }

    private void SetAmountText(
        int amount)
    {
        if (amountText == null)
            return;

        bool show =
            amount > 1;

        amountText.gameObject.SetActive(
            show
        );

        amountText.text =
            show
                ? amount.ToString()
                : string.Empty;
    }

    private void SetBorderColor(
        Color color)
    {
        if (border == null)
            return;

        border.enabled =
            true;

        border.color =
            color;

        originalBorderColor =
            color;
    }

    private void OnDisable()
    {
        ItemTooltip.Instance?.Hide();
    }
}