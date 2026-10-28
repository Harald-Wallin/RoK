public readonly struct FavourItemCost
{
    public ItemData Item
    {
        get;
    }

    public int Amount
    {
        get;
    }

    public FavourItemCost(
        ItemData item,
        int amount)
    {
        Item = item;
        Amount = amount;
    }
}