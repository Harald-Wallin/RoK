using UnityEngine;
using TMPro;

public class VendorUI : MonoBehaviour, IUIWindow
{
    public static VendorUI Instance;

    public GameObject panel;
    public Transform itemContainer;
    public VendorItemSlot slotPrefab;
    [Header("References")]
    [SerializeField] private TMP_Text titleText;

    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Transform buybackContainer;
    [SerializeField] private GameObject buybackSlotPrefab;

    public Vendor ActiveVendor { get; private set; }


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        Close();
    }

    public bool IsOpen =>
    canvasGroup != null &&
    canvasGroup.alpha > 0f;

    public void Open(Vendor vendor)
    {
        ActiveVendor = vendor;

        bool titleTextAssigned = false;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Update title text with vendor name if available
        if (titleText != null && ActiveVendor != null)
        {
            // Try to get a display name from CharacterStats. Use children/parent fallback.
            CharacterStats cs = ActiveVendor.GetComponent<CharacterStats>();
            if (cs == null)
                cs = ActiveVendor.GetComponentInChildren<CharacterStats>(true);
            if (cs == null)
                cs = ActiveVendor.GetComponentInParent<CharacterStats>(true);

            if (cs != null)
            {
                titleText.text = cs.displayName;
            }
            else
            {
                // No CharacterStats found; fall back to GameObject name and log for debugging
                titleText.text = ActiveVendor.name;
                Debug.LogWarning($"VendorUI.Open: CharacterStats not found on vendor '{ActiveVendor.name}'. Using GameObject.name as title.");
            }
        }
        else if (ActiveVendor != null && titleText == null)
        {
            Debug.LogWarning("VendorUI.Open: titleText reference is null. Assign TitleText in the VendorUI inspector.");
        }

        Debug.Log($"VendorUI.Open: ActiveVendor='{ActiveVendor?.name}', titleText='{titleText?.text}'");

        Populate();
        RefreshBuybackUI();
    }

    public void Close()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Clear active vendor when closing
        ActiveVendor = null;

        GlobalUIManager.Instance?.ClearInteractionWindow();
    }

    void Populate()
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);

        foreach (var item in ActiveVendor.GetItems())
        {
            var slot = Instantiate(slotPrefab, itemContainer);
            slot.Initialize(item, this);
        }
    }



    public void BuyItem(VendorItemRuntime entry)
    {
        if (ActiveVendor == null)
            return;

        if (!ActiveVendor.GetItems().Contains(entry))
            return;

        ActiveVendor.BuyItem(entry);
    }


    public void SellItem(
    ItemData item,
    int inventorySlot,
    int quantity = 1)
    {
        if (item == null ||
            ActiveVendor == null ||
            quantity <= 0)
        {
            return;
        }

        bool sold =
            ActiveVendor.TryBuyFromPlayer(
                item,
                inventorySlot,
                quantity
            );

        if (!sold)
        {
            Debug.LogWarning(
                $"Kunde inte sälja '{item.DisplayName}' " +
                $"från inventory-slot {inventorySlot}.",
                this
            );

            return;
        }

        RefreshBuybackUI();
    }

    public void SellItemStack(
    ItemData item,
    int inventorySlot,
    int quantity)
    {
        if (item == null ||
            ActiveVendor == null ||
            quantity <= 0)
        {
            return;
        }

        bool sold =
            ActiveVendor.TryBuyFromPlayer(
                item,
                inventorySlot,
                quantity
            );

        if (!sold)
        {
            Debug.LogWarning(
                $"Kunde inte sälja stacken '{item.DisplayName}' " +
                $"från inventory-slot {inventorySlot}.",
                this
            );

            return;
        }

        RefreshBuybackUI();
    }


    public int GetCurrentVendorPrice(VendorItem entry)
    {
        if (ActiveVendor == null)
            return 0;

        return ActiveVendor.GetModifiedPrice(entry);
    }

    public void RefreshBuybackUI()
    {
        foreach (Transform child in buybackContainer)
        {
            Destroy(child.gameObject);
        }

        if (ActiveVendor == null)
            return;

        var buybackItems = ActiveVendor.GetBuybackItems();

        foreach (var entry in buybackItems)
        {
            GameObject slot = Instantiate(buybackSlotPrefab, buybackContainer);

            VendorItemSlot slotUI = slot.GetComponent<VendorItemSlot>();

            slotUI.SetupBuyback(entry, ActiveVendor);
        }
    }
    public void RefreshVendorUI()
    {
        if (ActiveVendor == null)
            return;

        Populate();
    }
}