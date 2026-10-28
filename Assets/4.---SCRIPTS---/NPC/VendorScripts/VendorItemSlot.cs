using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//using static UnityEditor.Progress;

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
    [SerializeField] TMP_Text stackText;

    //Restock
    [SerializeField] TMP_Text stockText;
    [SerializeField] TMP_Text timerText;
    [SerializeField] Image buttonImage;

    private ItemData buybackItem;
    private int buybackQuantity;
    private Vendor buybackVendor;
    private bool isBuybackSlot = false;


    private VendorItemRuntime entry;
    private VendorUI vendorUI;
    private Vendor.BuybackEntry buybackEntry;

    public void Initialize(VendorItemRuntime entry, VendorUI ui)
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);

        this.entry = entry;
        this.vendorUI = ui;
        UpdateStockUI();

        if (entry == null || entry.data == null || entry.data.item == null)
            return;

        icon.sprite = entry.data.item.icon;

        if (nameText != null)
            nameText.text = entry.data.item.itemName;
        nameText.color = ItemRarityColors.GetColor(entry.data.item.rarity);
        border.color = ItemRarityColors.GetColor(entry.data.item.rarity);

        int price = ui.GetCurrentVendorPrice(entry.data);
        if (priceText != null)
            priceText.text = price.ToString();

        ItemData item = entry.data.item;

        PlayerStats player =
            PlayerReference.Player;

        bool canBuy =
            item != null &&
            item.MeetsRequirements(player);

        UpdateAvailabilityUI(canBuy);
    }

    void Update()
    {
        if (entry == null)
            return;

        if (entry.data.stockType == VendorStockType.Limited)
        {
            UpdateStockUI();
        }
    }

    public void OnClick()
    {
        // BUYBACK först
        if (isBuybackSlot)
        {
            TryBuyback();
            return;
        }

        if (entry == null || vendorUI == null)
            return;

        if (entry.data.stockType == VendorStockType.Limited && entry.currentStock <= 0)
            return;

        vendorUI.BuyItem(entry);
        UpdateStockUI();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        // If this is a buyback slot, handle buyback first
        if (isBuybackSlot)
        {
            TryBuyback();
            return;
        }

        if (entry == null)
            return;

        if (entry.data.stockType == VendorStockType.Limited && entry.currentStock <= 0)
            return;

        if (vendorUI != null)
        {
            vendorUI.BuyItem(entry);
        }
    }

    void TryBuyback()
    {
        if (buybackVendor == null || buybackItem == null)
            return;

        PlayerItemOwnership ownership =
    PlayerFavourManager.Instance?
        .PlayerItems;

        if (ownership != null &&
            !ownership.CanReceive(
                buybackItem,
                buybackQuantity
            ))
        {
            Debug.Log(
                $"Cannot buy back '{buybackItem.DisplayName}': " +
                "ownership limit reached."
            );

            return;
        }

        int price = buybackEntry.pricePaid;

        if (!PlayerCurrency.Instance.TrySpendCoins(price))
        {
            Debug.Log("Not enough coins for buyback.");
            return;
        }

        bool added = Inventory.Instance.AddItem(buybackItem, buybackQuantity);

        if (!added)
        {
            Debug.Log("Inventory full.");
            PlayerCurrency.Instance.AddCoins(price);
            return;
        }

        // Ta bort exakt entry
        int index = buybackVendor.GetBuybackItems().IndexOf(buybackEntry);
        if (index >= 0)
            buybackVendor.GetBuybackItems().RemoveAt(index);

        // Refresh UI direkt
        VendorUI.Instance.RefreshBuybackUI();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isBuybackSlot && buybackItem != null)
        {
            var provider = new VendorTooltipProvider(
            buybackItem,
            null,
            buybackEntry.pricePaid
            );

            var player = PlayerReference.Player;

            ItemTooltip.Instance.Show(
                provider,
                icon.rectTransform,
                player
            );
        }
        else if (entry != null && entry.data.item != null)
        {
            var provider = new VendorTooltipProvider(
            entry.data.item,
            entry.data,
            vendorUI.GetCurrentVendorPrice(entry.data)
            );

            var player = PlayerReference.Player;

            ItemTooltip.Instance.Show(
                provider,
                icon.rectTransform,
                player
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }

    public void SetupBuyback(Vendor.BuybackEntry entry, Vendor vendor)
    {
        buybackEntry = entry;
        buybackItem = entry.item;
        buybackQuantity = entry.quantity;
        buybackVendor = vendor;
        isBuybackSlot = true;

        // Ensure button calls OnClick for buyback slots as well
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }

        if (entry == null || entry.item == null)
            return;

        icon.sprite = entry.item.icon;
        icon.gameObject.SetActive(true);

        border.color = ItemRarityColors.GetColor(entry.item.rarity);

        if (stackText != null)
        {
            if (buybackQuantity > 1)
            {
                stackText.text = buybackQuantity.ToString();
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }

        if (priceText != null)
        {
            priceText.text = entry.pricePaid.ToString();
        }
    }

    void UpdateStockUI()
    {
        if (entry == null)
            return;

        if (entry.data.stockType == VendorStockType.Infinite)
        {
            if (stockText != null)
                stockText.gameObject.SetActive(false);

            if (timerText != null)
                timerText.gameObject.SetActive(false);

            return;
        }

        if (entry.currentStock > 0)
        {
            if (stockText != null)
            {
                stockText.text = entry.currentStock.ToString();
                stockText.gameObject.SetActive(true);
            }

            if (timerText != null)
                timerText.gameObject.SetActive(false);

            SetNormalColor();
        }
        else
        {
            if (stockText != null)
                stockText.gameObject.SetActive(false);

            if (entry.restockTimer > 0 && timerText != null)
            {
                timerText.gameObject.SetActive(true);

                int seconds = Mathf.CeilToInt(entry.restockTimer);

                int minutes = seconds / 60;
                int remainingSeconds = seconds % 60;

                timerText.text = $"{minutes:00}:{remainingSeconds:00}";
            }

            SetGreyColor();
        }
    }
    void SetGreyColor()
    {
        if (buttonImage != null)
            buttonImage.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
    }

    void SetNormalColor()
    {
        if (buttonImage != null)
            buttonImage.color = new Color(0f, 0f, 0f);
    }

    void UpdateAvailabilityUI(bool canBuy)
    {
        if (!canBuy)
        {
            // Only tint the icon (red tint) and keep text tint as before
            if (icon != null)
                icon.color = new Color(1f, 0.4f, 0.4f, 1f);

            if (nameText != null)
                nameText.color = new Color(1f, 0.7f, 0.7f, 1f);

            if (priceText != null)
                priceText.color = new Color(1f, 0.7f, 0.7f, 1f);
        }
        else
        {
            // restore defaults
            if (icon != null)
                icon.color = Color.white;

            if (nameText != null && entry != null && entry.data != null && entry.data.item != null)
                nameText.color = ItemRarityColors.GetColor(entry.data.item.rarity);

            if (priceText != null)
                priceText.color = Color.white;
        }
    }
}
