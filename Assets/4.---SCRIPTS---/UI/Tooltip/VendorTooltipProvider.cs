public class VendorTooltipProvider : ITooltipProvider
{
    private readonly ItemData item;
    private readonly int price;
    private readonly string currencyName;

    public VendorTooltipProvider(
        ItemData item,
        VendorItem vendorItem,
        int price,
        string currencyName = "coins")
    {
        this.item = item;
        this.price = price;
        this.currencyName = currencyName;
    }

    public TooltipData GetTooltipData(
        CharacterStats caster)
    {
        TooltipData data =
            item.GetTooltipData(caster);

        data.footer =
            $"Price: {price} {currencyName}";

        data.showFooter = true;

        return data;
    }
}