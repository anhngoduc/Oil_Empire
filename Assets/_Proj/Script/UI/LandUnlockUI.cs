// Assets/_Project/Scripts/UI/LandUnlockUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace OilGame
{
    public class LandUnlockUI : MonoBehaviour
    {
        [Header("=== Tham chiếu UI ===")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject plotItemPrefab;
        [SerializeField] private TextMeshProUGUI notificationText;

        [Header("=== Cấu hình ===")]
        [SerializeField] private float notificationDuration = 2f;

        private List<PlotItemSlot> activeSlots;
        private float notificationTimer;

        private ILandService landService;
        private IPlayerDataService playerDataService;

        private void Awake()
        {
            activeSlots = new List<PlotItemSlot>();
        }

        private void Start()
        {
            landService = ServiceLocator.Get<ILandService>();
            playerDataService = ServiceLocator.Get<IPlayerDataService>();

            EventBus.Subscribe<OnLandUnlocked>(OnLandUnlockedHandler);
            EventBus.Subscribe<OnMoneyChanged>(OnMoneyChangedHandler);

            if (notificationText != null)
                notificationText.gameObject.SetActive(false);

            RefreshLandList();
            Debug.Log("[LandUnlockUI] Đã khởi tạo.");
        }

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f && notificationText != null)
                    notificationText.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLandUnlocked>(OnLandUnlockedHandler);
            EventBus.Unsubscribe<OnMoneyChanged>(OnMoneyChangedHandler);
        }

        private void RefreshLandList()
        {
            foreach (var slot in activeSlots)
                if (slot != null) Destroy(slot.gameObject);
            activeSlots.Clear();

            if (landService == null || playerDataService == null) return;

            int playerZoneID = playerDataService.PlayerZoneID;
            if (playerZoneID < 0)
            {
                Debug.LogWarning("[LandUnlockUI] Player chưa có Zone!");
                return;
            }

            ZoneData zoneData = landService.GetZoneData(playerZoneID);
            if (zoneData == null) return;

            int cellsX = zoneData.cellsPerPlotX;
            int cellsZ = zoneData.cellsPerPlotZ;

            foreach (var plot in zoneData.plots)
            {
                GameObject slotGO = Instantiate(plotItemPrefab, contentParent);
                PlotItemSlot slot = slotGO.GetComponent<PlotItemSlot>();
                if (slot == null) { Destroy(slotGO); continue; }

                bool isUnlocked = landService.IsPlotUnlocked(playerZoneID, plot.plotID);
                bool canAfford = playerDataService.Money >= plot.unlockCost;

                slot.Setup(plot, isUnlocked, canAfford, cellsX, cellsZ,
                    () => OnUnlockClicked(playerZoneID, plot.plotID));

                activeSlots.Add(slot);
            }

            Debug.Log($"[LandUnlockUI] Hiển thị {activeSlots.Count} mảnh.");
        }

        private void UpdateAffordability()
        {
            if (playerDataService == null) return;
            double money = playerDataService.Money;
            foreach (var slot in activeSlots)
                if (slot != null) slot.UpdateAffordability(money);
        }

        private void OnUnlockClicked(int zoneID, int plotID)
        {
            if (landService == null) return;
            if (landService.IsPlotUnlocked(zoneID, plotID))
            {
                ShowNotification("Đã mở khóa rồi!");
                return;
            }
            double cost = landService.GetPlotUnlockCost(zoneID, plotID);
            if (playerDataService != null && playerDataService.Money < cost)
            {
                ShowNotification($"Không đủ tiền! Cần ${cost:N0}.");
                return;
            }
            bool ok = landService.UnlockPlot(zoneID, plotID);
            if (ok)
            {
                ShowNotification($"Đã mở khóa Mảnh {plotID}!");
                RefreshLandList();
            }
            else ShowNotification("Mở khóa thất bại!");
        }

        private void OnLandUnlockedHandler(OnLandUnlocked evt) => RefreshLandList();
        private void OnMoneyChangedHandler(OnMoneyChanged evt) => UpdateAffordability();

        private void ShowNotification(string msg)
        {
            if (notificationText != null)
            {
                notificationText.text = msg;
                notificationText.gameObject.SetActive(true);
                notificationTimer = notificationDuration;
            }
        }
    }
}