using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerClickHandler

{
    [Header("DEBUG")]
    [SerializeField] private int slotIndex = -1;

    [Header("UI")]
    [SerializeField] private Image border;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amountText;
    public static InventorySlotUI DraggedSlot;

    private PlayerConsumableController consumableController;
    private PlayerItemUseController itemUseController;

    private ItemData currentItem;
    private ItemTooltip tooltip;

    private Canvas rootCanvas;
    private GameObject dragGhost;

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public ItemData GetItem()
    {
        return currentItem;
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    private void Awake()
    {
        tooltip =
            FindFirstObjectByType<
                ItemTooltip>();

        rootCanvas =
            GetComponentInParent<
                Canvas>();

        PlayerStats player =
    PlayerReference.Player;

        if (player != null)
        {
            consumableController =
                player.GetComponent<
                    PlayerConsumableController>();

            itemUseController =
                player.GetComponent<
                    PlayerItemUseController>();
        }
    }

    // ANROPAS FRÅN InventoryUI
    public void Init(int index)
    {
        slotIndex = index;    }

    // =========================
    // UI UPDATE
    // =========================

    public void SetSlot(
    ItemData item,
    int amount)
    {
        currentItem =
            item;

        if (item == null)
        {
            ClearSlot();
            return;
        }

        if (border != null)
        {
            border.color =
                ItemRarityColors.GetColor(
                    item.rarity
                );

            border.enabled =
                true;
        }

        if (icon != null)
        {
            icon.enabled =
                true;

            icon.sprite =
                item.icon;
        }

        if (amountText != null)
        {
            bool showAmount =
                item.stackable &&
                amount > 1;

            amountText.gameObject.SetActive(
                showAmount
            );

            amountText.text =
                showAmount
                    ? amount.ToString()
                    : string.Empty;
        }
    }

    public void ClearSlot()
    {
        currentItem = null;
        icon.enabled = false;
        border.enabled= false;
        icon.sprite = null;
        amountText.gameObject.SetActive(false);
    }

    // =========================
    // POINTER
    // =========================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null) return;

        var player = PlayerReference.Player;

        // Previous call (kept as reference):
        // ItemTooltip.Instance.Show(currentItem, icon.rectTransform, player);

        // New call: include explicit anchor mode so inventory items use TopRight anchoring
        ItemTooltip.Instance.Show(
            (ITooltipProvider)currentItem,
            icon.rectTransform,
            player,
            ItemTooltip.TooltipAnchorMode.TopRight
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }

    // =========================
    // DRAG
    // =========================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        DraggedSlot = this;

        // Dölj original
        icon.enabled = false;
        border.enabled = false;
        amountText.gameObject.SetActive(false);

        // Skapa ghost
        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));

        dragGhost.transform.SetParent(rootCanvas.transform, false);
        dragGhost.transform.SetAsLastSibling();

        Image ghostImage = dragGhost.GetComponent<Image>();
        ghostImage.sprite = currentItem.icon;
        ghostImage.raycastTarget = false;
        ghostImage.preserveAspect = true;

        CanvasGroup cg = dragGhost.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        RectTransform rt = dragGhost.GetComponent<RectTransform>();

        rt.sizeDelta = icon.rectTransform.rect.size;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            dragGhost.GetComponent<RectTransform>().position = eventData.position;
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            Destroy(dragGhost);

        bool droppedOnSlot = false;

        if (eventData.pointerEnter != null)
        {
            InventorySlotUI inventorySlot =
                eventData.pointerEnter.GetComponentInParent<InventorySlotUI>();

            EquipmentSlotUI equipmentSlot =
                eventData.pointerEnter.GetComponentInParent<EquipmentSlotUI>();

            if (inventorySlot != null || equipmentSlot != null)
            {
                droppedOnSlot = true;
            }
        }


        // ❗ Om vi INTE släppte på en slot → fråga om drop
        if (!droppedOnSlot)
        {
            ShowDropConfirmation();
        }
        else
        {
            RefreshSlot();
        }

        DraggedSlot = null;
    }

    private void RefreshSlot()
    {
        InventorySlot slotData = Inventory.Instance.slots[slotIndex];
        SetSlot(slotData.item, slotData.amount);
    }

    private void ShowDropConfirmation()
    {
        InventorySlot slotData = Inventory.Instance.slots[slotIndex];

        if (slotData.IsEmpty())
        {
            RefreshSlot();
            return;
        }

        ConfirmPopup.Instance.Show(
            $"Drop {slotData.item.itemName}?",
            () =>
            {
                Inventory.Instance.RemoveItemAt(slotIndex, slotData.amount);
            }
        );

        StartCoroutine(WaitForPopupClose());
    }
    public void OnDrop(PointerEventData eventData)
    {
        // =========================
        // INVENTORY → INVENTORY
        // =========================
        if (DraggedSlot != null)
        {
            Inventory.Instance.SwapSlots(
                DraggedSlot.GetSlotIndex(),
                slotIndex
            );

            DraggedSlot = null;
            return;
        }

        // =========================
        // EQUIPMENT → INVENTORY
        // =========================
        if (EquipmentSlotUI.DraggedEquipmentSlot != null)
        {
            EquipmentSlotUI equipSlot =
                EquipmentSlotUI.DraggedEquipmentSlot;

            ItemData item =
                equipSlot.GetEquippedItem();

            if (item == null)
                return;

            InventorySlot targetSlot =
                Inventory.Instance.slots[slotIndex];

            if (!targetSlot.IsEmpty())
            {
                EquipmentSlotUI.DraggedEquipmentSlot =
                    null;

                RefreshSlot();
                return;
            }

            EquipmentManager manager =
                EquipmentManager.Instance;

            if (manager == null)
                return;

            targetSlot.item = item;
            targetSlot.amount = 1;

            manager.RemoveEquippedItemFromSlot(
                equipSlot
            );

            Inventory.Instance.NotifyChanged();

            EquipmentSlotUI.DraggedEquipmentSlot =
                null;
        }

    }


    private System.Collections.IEnumerator WaitForPopupClose()
    {
        // Vänta tills popup stängs
        while (ConfirmPopup.Instance.gameObject.activeSelf)
            yield return null;

        // Om item fortfarande finns kvar (No klickades)
        InventorySlot slotData = Inventory.Instance.slots[slotIndex];

        if (!slotData.IsEmpty())
        {
            RefreshSlot();
        }
    }

    public void OnPointerClick(
    PointerEventData eventData)
    {
        if (currentItem == null)
            return;

        // =====================================================
        // VENDOR OPEN -> SELL
        // =====================================================

        if (VendorUI.Instance != null &&
            VendorUI.Instance.IsOpen)
        {
            if (eventData.button ==
                    PointerEventData
                        .InputButton.Right &&
                Input.GetKey(
                    KeyCode.LeftShift))
            {
                InventorySlot slot =
                    Inventory.Instance
                        .slots[
                            slotIndex
                        ];

                int quantity =
                    slot.amount;

                VendorUI.Instance.SellItem(
                    currentItem,
                    slotIndex,
                    quantity
                );

                return;
            }

            if (eventData.button ==
                    PointerEventData
                        .InputButton.Right)
            {
                VendorUI.Instance.SellItem(
                    currentItem,
                    slotIndex,
                    1
                );

                return;
            }
        }

        // =====================================================
        // ITEM USE -> FAVOUR / FUTURE CAPABILITIES
        // =====================================================

        if (eventData.button ==
                PointerEventData
                    .InputButton.Right)
        {
            if (itemUseController == null)
            {
                PlayerStats player =
                    PlayerReference.Player;

                if (player != null)
                {
                    itemUseController =
                        player.GetComponent<
                            PlayerItemUseController>();
                }
            }

            if (itemUseController != null &&
                itemUseController.TryUseItem(
                    currentItem))
            {
                return;
            }
        }

        // =====================================================
        // FOOD -> CONSUME
        // =====================================================

        if (eventData.button ==
                PointerEventData
                    .InputButton.Right &&
            currentItem.IsFood)
        {
            if (consumableController == null)
            {
                PlayerStats player =
                    PlayerReference.Player;

                if (player != null)
                {
                    consumableController =
                        player.GetComponent<
                            PlayerConsumableController>();
                }
            }

            consumableController
                ?.TryConsumeFoodFromSlot(
                    slotIndex
                );

            return;
        }

        // =====================================================
        // NORMAL -> EQUIP
        // =====================================================

        if (eventData.button !=
                PointerEventData
                    .InputButton.Right ||
            !currentItem.equippable)
        {
            return;
        }

        EquipmentManager manager =
            EquipmentManager.Instance;

        if (manager == null)
            return;

        manager.TryEquipItem(
            currentItem,
            slotIndex
        );
    }

}
