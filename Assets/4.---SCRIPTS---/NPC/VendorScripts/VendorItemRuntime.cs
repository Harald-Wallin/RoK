using UnityEngine;

[System.Serializable]
public class VendorItemRuntime
{
    public VendorItem data;

    public int currentStock;

    public float restockTimer;

    public VendorItemRuntime(VendorItem item)
    {
        data = item;

        if (item.stockType == VendorStockType.Infinite)
            currentStock = -1;
        else
            currentStock = item.maxStock;

        restockTimer = 0f;
    }
}