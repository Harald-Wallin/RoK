using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableAbility : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public AbilityData ability;

    private GameObject dragIcon;
    private RectTransform dragRect;
    private Canvas canvas;
    public ICombatSlot sourceSlot;

    public bool wasDroppedOnSlot = false;
    public static bool dragged = false;

    void Awake()
    {
        //Debug.Log("DraggableAbility attached to: " + gameObject.name);
        canvas = GetComponentInParent<Canvas>();
      
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("DRAG START from: " + gameObject.name);

        if (ability == null)
        {
            //Debug.Log("NO ABILITY ON THIS ICON");
            return;
        }

        PlayerStats player = PlayerReference.Player;

        if (player != null)
        {
            PlayerActionInput input =
                player.GetComponent<
                    PlayerActionInput>();

            input?.CancelHeldAbilityInput(
                ability
            );
        }

        wasDroppedOnSlot = false;
        sourceSlot = null;

        // mark global drag state and hide any visible tooltips while dragging
        dragged = true;
        if (ItemTooltip.Instance != null)
            ItemTooltip.Instance.Hide();

        // 🔥 hitta vilken slot vi kommer från
        sourceSlot = GetComponentInParent<ICombatSlot>();

        // skapa drag-ikon (din kod)
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);

        Image img = dragIcon.AddComponent<Image>();
        img.sprite = ability.icon;
        img.raycastTarget = false;

        dragRect = dragIcon.GetComponent<RectTransform>();
        dragRect.sizeDelta = new Vector2(50, 50);

        dragIcon.transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragRect != null)
        {
            dragRect.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }

        // clear global drag state
        dragged = false;

        // ❌ Droppades inte på slot → rensa
        if (!wasDroppedOnSlot && sourceSlot != null)
        {
            //Debug.Log("Dropped outside → clearing slot");
            sourceSlot.ClearSlot();
        }
    }
}