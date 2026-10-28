using TMPro;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private InventorySlotUI[] slotUIs;

    private Inventory inventory;
    private bool isOpen = false;
    public Vendor ActiveVendor { get; private set; }

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;
    [SerializeField] private TextMeshProUGUI goldText;

    // =========================
    // LIFECYCLE
    // =========================

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        inventory = Inventory.Instance;

        if (inventory == null)
        {
            Debug.LogError("Inventory.Instance is NULL in InventoryUI");
            return;
        }

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null)
            {
                Debug.LogError($"slotUIs[{i}] is NULL in Start()");
                continue;
            }

            slotUIs[i].Init(i);
        }

        if (PlayerCurrency.Instance != null)
        {
            PlayerCurrency.Instance.OnCoinsChanged += UpdateGoldUI;
            UpdateGoldUI();
        }
        else
        {
            Debug.LogWarning("PlayerCurrency.Instance is NULL in InventoryUI.Start()");
        }

        inventory.OnInventoryChanged += Refresh;

        Close();
        Refresh();
    }

    private void UpdateGoldUI()
    {
        if (PlayerCurrency.Instance == null || goldText == null)
            return;

        int coins = PlayerCurrency.Instance.GetCoins();
        goldText.text = coins.ToString("N0");
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;

        if (PlayerCurrency.Instance != null)
            PlayerCurrency.Instance.OnCoinsChanged -= UpdateGoldUI;
    }

    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        isOpen = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        Refresh();
    }

    public void Close()
    {
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    // =========================
    // REFRESH
    // =========================

    public void Refresh()
    {
        if (inventory == null)
            return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null)
                continue;

            if (i >= inventory.slots.Count)
            {
                slotUIs[i].ClearSlot();
                continue;
            }

            InventorySlot slot = inventory.slots[i];

            if (slot.IsEmpty())
                slotUIs[i].ClearSlot();
            else
                slotUIs[i].SetSlot(slot.item, slot.amount);
        }
    }
}