using UnityEngine;

public class VendorTooltipProvider : ITooltipProvider
{
    private readonly ItemData item;
    private readonly int price;
    private readonly Sprite currencyIcon;
    private readonly bool showCurrencyIcon;

    public VendorTooltipProvider(
        ItemData item,
        VendorItem vendorItem,
        int price,
        Sprite currencyIcon = null,
        bool showCurrencyIcon = true)
    {
        this.item = item;
        this.price = price;
        this.currencyIcon = currencyIcon;
        this.showCurrencyIcon = showCurrencyIcon;
    }

    public TooltipData GetTooltipData(
        CharacterStats caster)
    {
        TooltipData data =
            item.GetTooltipData(caster);

        data.footer = price >= 0
            ? price.ToString()
            : "Unavailable";

        data.footerIcon = currencyIcon;

        data.showFooterIcon =
            price >= 0 && showCurrencyIcon;

        data.showFooter = true;

        return data;
    }
}