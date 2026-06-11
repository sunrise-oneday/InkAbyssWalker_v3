namespace StoreAndInventory
{
    public enum BuyResult
    {
        Success,
        NoCurrency,
        NoSpace,
        OutOfStock,
        InvalidItem,
        ShopClosed
    }

    public enum SellResult
    {
        Success,
        NotSellable,
        InvalidSlot,
        InvalidCount,
        InvalidItem
    }
}
