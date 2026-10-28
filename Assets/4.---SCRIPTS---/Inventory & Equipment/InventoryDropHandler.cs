using UnityEngine;

public class InventoryDropHandler : MonoBehaviour
{
    public static InventoryDropHandler Instance;

    private int pendingDropIndex = -1;

    private void Awake()
    {
        Instance = this;
    }

    public void AskDrop(int slotIndex)
    {
        pendingDropIndex = slotIndex;

        // TODO: koppla till din UI-popup
        Debug.Log($"Drop confirmation for slot {slotIndex}");
    }

    public void ConfirmDrop()
    {
        if (pendingDropIndex >= 0)
        {
            Inventory.Instance.RemoveItemAt(pendingDropIndex);
            pendingDropIndex = -1;
        }
    }

    public void CancelDrop()
    {
        pendingDropIndex = -1;
    }
}

