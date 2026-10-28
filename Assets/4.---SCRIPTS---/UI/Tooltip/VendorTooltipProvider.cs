using UnityEngine;

public class VendorTooltipProvider : ITooltipProvider
{
    private ItemData item;
    private VendorItem vendorItem;
    private int price;

    public VendorTooltipProvider(ItemData item, VendorItem vendorItem, int price)
    {
        this.item = item;
        this.vendorItem = vendorItem;
        this.price = price;
    }

    public TooltipData GetTooltipData(CharacterStats caster)
    {
        TooltipData data = item.GetTooltipData(caster);

        // Override price
        data.footer = $"Price: {price}";
        data.showFooter = true;

        return data;
    }
}
