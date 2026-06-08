// Assets/_Project/Scripts/Core/GameEvents.cs

using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Sự kiện khi tiền của người chơi thay đổi.
    /// </summary>
    public struct OnMoneyChanged
    {
        public double oldAmount;            // Số tiền trước khi thay đổi
        public double newAmount;            // Số tiền sau khi thay đổi
        public double changeAmount;         // Số tiền thay đổi (dương = tăng, âm = giảm)
        public MoneyChangeReason reason;    // Lý do thay đổi

        public OnMoneyChanged(double oldAmt, double newAmt, MoneyChangeReason reason)
        {
            this.oldAmount = oldAmt;
            this.newAmount = newAmt;
            this.changeAmount = newAmt - oldAmt;
            this.reason = reason;
        }
    }

    /// <summary>
    /// Sự kiện khi dầu của người chơi thay đổi (dầu trong kho, không phải trong bucket).
    /// </summary>
    public struct OnOilChanged
    {
        public double oldAmount;
        public double newAmount;
        public double changeAmount;
        public OilChangeReason reason;

        public OnOilChanged(double oldAmt, double newAmt, OilChangeReason reason)
        {
            this.oldAmount = oldAmt;
            this.newAmount = newAmt;
            this.changeAmount = newAmt - oldAmt;
            this.reason = reason;
        }
    }

    /// <summary>
    /// Sự kiện khi inventory thay đổi (thêm/xóa item).
    /// </summary>
    public struct OnInventoryChanged
    {
        public int buildingID;      // ID của BuildingData bị thay đổi
        public int newCount;        // Số lượng mới
        public int changeAmount;    // Số lượng thay đổi (dương = thêm, âm = bớt)

        public OnInventoryChanged(int buildingID, int newCount, int changeAmount)
        {
            this.buildingID = buildingID;
            this.newCount = newCount;
            this.changeAmount = changeAmount;
        }
    }

    /// <summary>
    /// Sự kiện khi một công trình được đặt thành công.
    /// </summary>
    public struct OnBuildingPlaced
    {
        public int uniqueID;
        public int buildingDataID;
        public BuildingType buildingType;
        public int zoneID;
        public int plotID;
        public int gridX;
        public int gridZ;

        public OnBuildingPlaced(int uniqueID, int buildingDataID, BuildingType buildingType,
            int zoneID, int plotID, int gridX, int gridZ)
        {
            this.uniqueID = uniqueID;
            this.buildingDataID = buildingDataID;
            this.buildingType = buildingType;
            this.zoneID = zoneID;
            this.plotID = plotID;
            this.gridX = gridX;
            this.gridZ = gridZ;
        }
    }

    /// <summary>
    /// Sự kiện khi một công trình bị xóa.
    /// </summary>
    public struct OnBuildingRemoved
    {
        public int uniqueID;
        public int zoneID;
        public int plotID;
        public int gridX;
        public int gridZ;

        public OnBuildingRemoved(int uniqueID, int zoneID, int plotID, int gridX, int gridZ)
        {
            this.uniqueID = uniqueID;
            this.zoneID = zoneID;
            this.plotID = plotID;
            this.gridX = gridX;
            this.gridZ = gridZ;
        }
    }

    /// <summary>
    /// Sự kiện khi sản lượng dầu được cập nhật mỗi giây.
    /// </summary>
    public struct OnOilProductionUpdated
    {
        public float totalProductionThisTick;   // Tổng dầu sản xuất trong tick này
        public float currentTotalProductionRate; // Tổng tốc độ sản xuất hiện tại (sau khi áp hệ số)
        public int totalDrills;                  // Tổng số Drill đang hoạt động
        public int totalBuckets;                 // Tổng số Bucket
        public int filledBuckets;                // Số Bucket đang đầy

        public OnOilProductionUpdated(float totalThisTick, float totalRate,
            int drills, int buckets, int filled)
        {
            this.totalProductionThisTick = totalThisTick;
            this.currentTotalProductionRate = totalRate;
            this.totalDrills = drills;
            this.totalBuckets = buckets;
            this.filledBuckets = filled;
        }
    }

    /// <summary>
    /// Sự kiện khi một bucket đầy.
    /// </summary>
    public struct OnBucketFilled
    {
        public int bucketUniqueID;
        public float capacity;

        public OnBucketFilled(int uniqueID, float capacity)
        {
            this.bucketUniqueID = uniqueID;
            this.capacity = capacity;
        }
    }

    /// <summary>
    /// Sự kiện khi một bucket được thu dầu (trở về rỗng).
    /// </summary>
    public struct OnBucketEmptied
    {
        public int bucketUniqueID;
        public float collectedAmount;

        public OnBucketEmptied(int uniqueID, float collected)
        {
            this.bucketUniqueID = uniqueID;
            this.collectedAmount = collected;
        }
    }

    /// <summary>
    /// Sự kiện khi bucket có sự thay đổi về lượng dầu bên trong.
    /// </summary>
    public struct OnBucketUpdated
    {
        public int bucketUniqueID;
        public float currentOil;
        public float capacity;
        public BucketState state;

        public OnBucketUpdated(int uniqueID, float current, float capacity, BucketState state)
        {
            this.bucketUniqueID = uniqueID;
            this.currentOil = current;
            this.capacity = capacity;
            this.state = state;
        }
    }

    /// <summary>
    /// Sự kiện khi người chơi thu dầu từ bucket.
    /// </summary>
    public struct OnOilCollected
    {
        public float collectedAmount;
        public float totalOilAfterCollect;

        public OnOilCollected(float collected, float totalOil)
        {
            this.collectedAmount = collected;
            this.totalOilAfterCollect = totalOil;
        }
    }

    /// <summary>
    /// Sự kiện khi giá dầu thay đổi.
    /// </summary>
    public struct OnOilPriceChanged
    {
        public float oldPrice;
        public float newPrice;

        public OnOilPriceChanged(float oldPrice, float newPrice)
        {
            this.oldPrice = oldPrice;
            this.newPrice = newPrice;
        }
    }

    /// <summary>
    /// Sự kiện khi người chơi bán dầu.
    /// </summary>
    public struct OnOilSold
    {
        public float amountSold;
        public float pricePerUnit;
        public double moneyEarned;

        public OnOilSold(float amount, float price, double earned)
        {
            this.amountSold = amount;
            this.pricePerUnit = price;
            this.moneyEarned = earned;
        }
    }

    /// <summary>
    /// Sự kiện khi một mảnh đất được mở khóa.
    /// </summary>
    public struct OnLandUnlocked
    {
        public int zoneID;
        public int plotID;
        public double unlockCost;

        public OnLandUnlocked(int zoneID, int plotID, double cost)
        {
            this.zoneID = zoneID;
            this.plotID = plotID;
            this.unlockCost = cost;
        }
    }

    /// <summary>
    /// Sự kiện khi game được lưu.
    /// </summary>
    public struct OnGameSaved
    {
        public string filePath;

        public OnGameSaved(string path)
        {
            this.filePath = path;
        }
    }

    /// <summary>
    /// Sự kiện khi game được load.
    /// </summary>
    public struct OnGameLoaded
    {
        public SaveData saveData;

        public OnGameLoaded(SaveData data)
        {
            this.saveData = data;
        }
    }

    /// <summary>
    /// Sự kiện khi một công trình được chọn trong Inventory để đặt.
    /// </summary>
    public struct OnPlacementStarted
    {
        public BuildingData buildingData;

        public OnPlacementStarted(BuildingData data)
        {
            this.buildingData = data;
        }
    }

    /// <summary>
    /// Sự kiện khi thoát khỏi chế độ đặt công trình.
    /// </summary>
    public struct OnPlacementEnded
    {
        // Có thể mở rộng thêm thông tin nếu cần
    }

    public struct OnActiveBucketChanged
    {
        public int? activeBucketID;
        public int zoneID; // Zone của Bucket đang được bơm
    }
}