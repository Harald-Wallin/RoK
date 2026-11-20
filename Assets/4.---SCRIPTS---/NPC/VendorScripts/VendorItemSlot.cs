using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VendorItemSlot : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]

    [SerializeField] public Image border;
    [SerializeField] public Image icon;
    [SerializeField] public TMP_Text nameText;
    [SerializeField] public TMP_Text priceText;

    [SerializeField]
    private Image priceCurrencyIcon;

    [SerializeField] private TMP_Text stackText;
    [SerializeField] private TMP_Text stockText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image buttonImage;

    private VendorItemRuntime entry;
    private VendorUI vendorUI;

    private Vendor.BuybackEntry buybackEntry;
    private Vendor buybackVendor;

    private bool isBuybackSlot;

    private Color originalButtonColor;
    private Sprite originalCurrencySprite;

    private void Awake()
    {
        originalButtonColor = buttonImage != null
            ? buttonImage.color
            : Color.white;

        originalCurrencySprite = priceCurrencyIcon != null
            ? priceCurrencyIcon.sprite
            : null;

        if (priceCurrencyIcon != null)
        {
            priceCurrencyIcon.preserveAspect = true;
            priceCurrencyIcon.raycastTarget = false;
        }
    }

    public void Initialize(
        VendorItemRuntime newEntry,
        VendorUI ui)
    {
        entry = newEntry;
        vendorUI = ui;

        isBuybackSlot = false;
        buybackEntry = null;

        ConfigureButton();

        if (entry?.data?.item == null)
            return;

        SetItemVisuals(entry.data.item);

        if (stackText != null)
            stackText.gameObject.SetActive(false);

        RefreshOffer();
    }

    public void SetupBuyback(
        Vendor.BuybackEntry entry,
        Vendor vendor)
    {
        this.entry = null;

        buybackEntry = entry;
        buybackVendor = vendor;
        isBuybackSlot = true;

        ConfigureButton();

        if (entry?.item == null)
            return;

        SetItemVisuals(entry.item);

        SetPrice(
            entry.pricePaid,
            GetCurrencyIcon(true));

        if (stackText != null)
        {
            stackText.text =
                entry.quantity.ToString();

            stackText.gameObject.SetActive(
                entry.quantity > 1);
        }

        if (stockText != null)
            stockText.gameObject.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);
    }

    private void ConfigureButton()
    {
        Button button = GetComponent<Button>();

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void SetItemVisuals(ItemData item)
    {
        Color rarity =
            ItemRarityColors.GetColor(item.rarity);

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.color = Color.white;
        }

        if (border != null)
            border.color = rarity;

        if (nameText != null)
        {
            nameText.text = item.DisplayName;
            nameText.color = rarity;
        }
    }

    private void Update()
    {
        if (!isBuybackSlot)
            RefreshOffer();
    }

    private Sprite GetCurrencyIcon(
        bool forceCoins = false)
    {
        Vendor vendor = isBuybackSlot
            ? buybackVendor
            : vendorUI != null
                ? vendorUI.ActiveVendor
                : null;

        if (!forceCoins &&
            vendor != null &&
            vendor.UsesTrophies)
        {
            return vendor.PaymentTrophy.icon;
        }

        CurrencyData currency =
            PlayerCurrency.Instance != null
                ? PlayerCurrency.Instance.CurrencyDefinition
                : null;

        return currency != null && currency.Icon != null
            ? currency.Icon
            : originalCurrencySprite;
    }

    private void SetPrice(
        int amount,
        Sprite currencySprite)
    {
        if (priceText != null)
        {
            priceText.text = amount >= 0
                ? amount.ToString()
                : "Unavailable";

            priceText.color = Color.white;
        }

        if (priceCurrencyIcon != null)
        {
            priceCurrencyIcon.sprite =
                currencySprite;

            priceCurrencyIcon.gameObject.SetActive(
                amount >= 0 &&
                currencySprite != null);
        }
    }

    private void RefreshOffer()
    {
        Vendor vendor = vendorUI != null
            ? vendorUI.ActiveVendor
            : null;

        if (vendor == null ||
            entry?.data?.item == null)
        {
            return;
        }

        SetPrice(
            vendor.GetModifiedPrice(entry.data),
            GetCurrencyIcon());

        bool meetsRequirements =
            vendor.MeetsPurchaseRequirements(entry.data);

        if (icon != null)
        {
            icon.color = meetsRequirements
                ? Color.white
                : new Color(1f, 0.4f, 0.4f, 1f);
        }

        if (nameText != null)
        {
            nameText.color = meetsRequirements
                ? ItemRarityColors.GetColor(
                    entry.data.item.rarity)
                : new Color(1f, 0.7f, 0.7f, 1f);
        }

        if (priceText != null)
        {
            priceText.color = meetsRequirements
                ? Color.white
                : new Color(1f, 0.7f, 0.7f, 1f);
        }

        bool limited =
            entry.data.stockType == VendorStockType.Limited;

        bool soldOut =
            limited && entry.currentStock <= 0;

        if (stockText != null)
        {
            stockText.gameObject.SetActive(
                limited && !soldOut);

            stockText.text =
                entry.currentStock.ToString();
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(
                soldOut && entry.restockTimer > 0f);

            int seconds =
                Mathf.CeilToInt(entry.restockTimer);

            timerText.text =
                $"{seconds / 60:00}:{seconds % 60:00}";
        }

        if (buttonImage != null)
        {
            buttonImage.color = soldOut
                ? new Color(0.6f, 0.6f, 0.6f, 0.6f)
                : originalButtonColor;
        }
    }

    public void OnClick()
    {
        if (isBuybackSlot)
        {
            if (buybackVendor != null &&
                buybackVendor.TryBuyback(buybackEntry))
            {
                VendorUI.Instance?.RefreshBuybackUI();
            }
        }
        else
        {
            vendorUI?.BuyItem(entry);
            RefreshOffer();
        }
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (eventData.button ==
            PointerEventData.InputButton.Right)
        {
            OnClick();
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (ItemTooltip.Instance == null ||
            icon == null)
        {
            return;
        }

        ItemData item;
        VendorItem offer = null;
        int price;

        if (isBuybackSlot)
        {
            if (buybackEntry?.item == null)
                return;

            item = buybackEntry.item;
            price = buybackEntry.pricePaid;
        }
        else
        {
            Vendor vendor = vendorUI != null
                ? vendorUI.ActiveVendor
                : null;

            if (vendor == null ||
                entry?.data?.item == null)
            {
                return;
            }

            offer = entry.data;
            item = offer.item;
            price = vendor.GetModifiedPrice(offer);
        }

        Sprite currencySprite =
            GetCurrencyIcon(isBuybackSlot);

        ItemTooltip.Instance.Show(
            new VendorTooltipProvider(
                item,
                offer,
                price,
                currencySprite,
                currencySprite != null),
            icon.rectTransform,
            PlayerReference.Player);
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();
    }
}