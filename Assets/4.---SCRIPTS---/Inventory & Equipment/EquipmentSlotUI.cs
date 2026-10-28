using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class EquipmentSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerExitHandler,
    IPointerEnterHandler,
    IDropHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Slot Info")]
    public ItemType slotType;

    [Header("UI")]
    [SerializeField] private Image border;
    [SerializeField] private Image icon;
    private Canvas rootCanvas;
    private GameObject dragGhost;
    public static EquipmentSlotUI DraggedEquipmentSlot;

    private ItemData equippedItem;
    private PlayerItemUseController itemUseController;


    private void Awake()
    {
        rootCanvas =
            GetComponentInParent<Canvas>();

        if (border != null)
        {
            border.enabled =
                false;
        }

        if (icon != null)
        {
            icon.enabled =
                false;
        }

        PlayerStats player =
            PlayerReference.Player;

        if (player != null)
        {
            itemUseController =
                player.GetComponent<
                    PlayerItemUseController>();
        }
    }

    public bool IsEmpty()
    {
        return equippedItem == null;
    }

    public void SetItem(ItemData item)
    {
        equippedItem = item;

        if (item == null)
        {
            ClearSlot();
            return;
        }

        icon.enabled = true;
        icon.sprite = item.icon;

        border.enabled = true;
        border.color = ItemRarityColors.GetColor(item.rarity);
    }

    public void ClearSlot()
    {
        equippedItem = null;

        icon.sprite = null;
        icon.enabled = false;

        border.enabled = false;
    }

    public ItemData GetEquippedItem()
    {
        return equippedItem;
    }

    public void OnPointerClick(
    PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData.InputButton.Right)
        {
            return;
        }

        if (equippedItem == null)
            return;

        // =====================================================
        // ITEM USE -> FAVOUR / FUTURE CAPABILITIES
        // =====================================================

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
                equippedItem))
        {
            /*
             * Itemets primary use tog hand om högerklicket.
             *
             * För ett Favour-item betyder det att FavourWindow
             * öppnades. Itemet ska därför INTE unequippas.
             */
            return;
        }

        // =====================================================
        // NORMAL -> UNEQUIP
        // =====================================================

        EquipmentManager manager =
            EquipmentManager.Instance;

        if (manager == null)
            return;

        manager.Unequip(
            this
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (equippedItem == null) return;

        var player = PlayerReference.Player;

        // Previous call (kept for reference):
        // ItemTooltip.Instance.Show(equippedItem, icon.rectTransform, player);

        // New call: anchor equipment tooltips TopRight (tooltip's bottom-left to icon's top-right)
        ItemTooltip.Instance.Show(
            (ITooltipProvider)equippedItem,
            icon.rectTransform,
            player,
            ItemTooltip.TooltipAnchorMode.TopRight
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            dragGhost.transform.position = eventData.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (equippedItem == null)
            return;

        DraggedEquipmentSlot = this;

        // Dölj original
        icon.enabled = false;
        border.enabled = false;

        // Skapa ghost
        dragGhost = new GameObject("EquipDragGhost",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));

        dragGhost.transform.SetParent(rootCanvas.transform, false);
        dragGhost.transform.SetAsLastSibling();

        Image ghostImage = dragGhost.GetComponent<Image>();
        ghostImage.sprite = equippedItem.icon;
        ghostImage.raycastTarget = false;
        ghostImage.preserveAspect = true;

        CanvasGroup cg = dragGhost.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        RectTransform rt = dragGhost.GetComponent<RectTransform>();
        rt.sizeDelta = icon.rectTransform.sizeDelta;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
            Destroy(dragGhost);

        bool droppedOnInventory = false;

        if (eventData.pointerEnter != null)
        {
            InventorySlotUI inventorySlot =
                eventData.pointerEnter.GetComponentInParent<InventorySlotUI>();

            if (inventorySlot != null)
            {
                droppedOnInventory = true;
            }
        }

        if (!droppedOnInventory)
        {
            // Släppt utanför → destroy confirm
            ConfirmPopup.Instance.Show(
                $"Destroy {equippedItem.itemName}?",
                () =>
                {
                    EquipmentManager.Instance.RemoveEquippedItemFromSlot(this);
                }
            );

            // Återställ ikon tills popup avgör
            icon.enabled = true;

            if (equippedItem != null)
            {
                border.enabled = true;
                border.color = ItemRarityColors.GetColor(equippedItem.rarity);
            }

        }
        else
        {
            // Återställ ikon (InventorySlotUI sköter själva flytten)
            icon.enabled = true;

            if (equippedItem != null)
            {
                border.enabled = true;
                border.color = ItemRarityColors.GetColor(equippedItem.rarity);
            }
        }

        DraggedEquipmentSlot = null;
    }



    public void OnDrop(PointerEventData eventData)
    {
        if (InventorySlotUI.DraggedSlot == null)
            return;

        InventorySlotUI draggedSlot = InventorySlotUI.DraggedSlot;

        if (draggedSlot.IsEmpty())
            return;

        ItemData item = draggedSlot.GetItem();

        // Fel typ?
        if (item.itemType != slotType)
            return;

        EquipmentManager.Instance.TryEquipItem(item, draggedSlot.GetSlotIndex());
    }

}




