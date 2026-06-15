// Assets/_Project/Scripts/Core/IServices.cs

using System.Collections.Generic;
using UnityEngine;

namespace OilGame
{
    public interface IPlayerDataService
    {
        long Money { get; }
        long OilHeld { get; }
        int PlayerZoneID { get; }
        PlayerData CurrentData { get; }
        void AddMoney(long amount, MoneyChangeReason reason);
        bool SubtractMoney(long amount, MoneyChangeReason reason);
        void AddOil(long amount, OilChangeReason reason);
        bool SubtractOil(long amount, OilChangeReason reason);
        bool IsPlotUnlocked(int zoneID, int plotID);
        void UnlockPlot(int zoneID, int plotID);
        List<BuildingRuntimeData> GetAllBuildings();
        void AddBuilding(BuildingRuntimeData building);
        void RemoveBuilding(int uniqueID);
        void UpdateBucketOil(int uniqueID, long newAmount);
        Dictionary<int, int> GetInventory();
        int GetInventoryCount(int buildingID);
        void SetInventoryItem(int buildingID, int count);
        int GetNextBuildingID();
        SaveData CreateSaveData();
    }

    public interface ILandService
    {
        bool IsPlayerZone(int zoneID);
        bool IsPlotUnlocked(int zoneID, int plotID);
        float GetPlotMultiplier(int zoneID, int plotID);
        double GetPlotUnlockCost(int zoneID, int plotID);
        bool UnlockPlot(int zoneID, int plotID);
        ZoneOwner GetZoneOwner(int zoneID);
        ZoneData GetZoneData(int zoneID);
        int TotalZones { get; }
        void SetZoneOwner(int zoneID, ZoneOwner owner);
        List<int> GetEmptyZoneIDs();
        List<int> GetAllPlotIDs(int zoneID);
        PlotInfo GetPlotInfo(int zoneID, int plotID);
    }

    public interface IGridService
    {
        bool CanPlace(int zoneID, int plotID, int gridX, int gridZ);
        void OccupyCell(int zoneID, int plotID, int gridX, int gridZ, int buildingID, BuildingType type);
        void FreeCell(int zoneID, int plotID, int gridX, int gridZ);
        Vector3 GridToWorld(int zoneID, int plotID, int gridX, int gridZ);
        bool WorldToGrid(Vector3 worldPos, out int zoneID, out int plotID, out int gridX, out int gridZ);
    }

    public interface IBuildingService
    {
        void EnterPlacementMode(BuildingData data);
        void CancelPlacement();
        bool IsInPlacementMode { get; }
        BuildingData CurrentPlacementData { get; }
        List<Building> GetAllPlayerBuildings();
        List<Building> GetBuildingsOfType(BuildingType type);
        Building GetBuildingByID(int uniqueID);
        void RemoveBuilding(int uniqueID);
        void RestoreBuildingsFromSave(List<PlacedBuildingSaveData> saveDataList);
        void TryPlaceFromUI();
    }

    public interface IInventoryService
    {
        int GetCount(int buildingID);
        void AddItem(int buildingID, int count);
        bool RemoveItem(int buildingID, int count);
        Dictionary<int, int> GetAllItems();
    }

    public interface IProductionService
    {
        bool IsProductionPaused { get; }
        void ResumeProduction();
        void PauseProduction();
        long TotalProductionRate { get; }
    }

    public interface IBucketService
    {
        void FillOil(long amount);
        long CollectOil(int bucketUniqueID);
        BucketState GetBucketState(int bucketUniqueID);
        long GetBucketCurrentOil(int bucketUniqueID);
    }

    public interface IMarketService
    {
        long CurrentOilPrice { get; }
        long SellOil(long amount);
        float GetTimeUntilNextPriceUpdate();
    }

    public interface IShopService
    {
        List<BuildingData> GetAvailableItems(BuildingType? filterType = null);
        PurchaseResult Purchase(int buildingID);
    }
}