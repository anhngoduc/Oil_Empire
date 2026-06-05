// Assets/_Project/Scripts/Data/Enums.cs

namespace OilGame
{
    public enum BuildingType
    {
        Drill,
        Bucket
    }

    public enum CellState
    {
        Empty,
        Occupied,
        Locked
    }

    public enum ZoneOwner
    {
        Player,
        Bot,
        Empty
    }

    public enum BucketState
    {
        Empty,
        Partial,
        Full
    }

    public enum PurchaseResult
    {
        Success,
        NotEnoughMoney,
        InventoryFull,
        UnknownError
    }

    public enum OilChangeReason
    {
        Production,
        Collect,
        Sell,
        Debug
    }

    public enum MoneyChangeReason
    {
        SellOil,
        Purchase,
        UnlockLand,
        Debug
    }
}