// Assets/_Project/Scripts/Market/MarketManager.cs

using UnityEngine;
using System.Collections;

namespace OilGame
{
    /// <summary>
    /// MarketManager - Service quản lý chợ dầu.
    /// Implement IMarketService để UI và BotSimulationManager truy xuất qua ServiceLocator.
    /// 
    /// Trách nhiệm:
    /// - Random giá dầu mỗi khoảng thời gian (từ GameConfig).
    /// - Cung cấp giá dầu hiện tại cho người chơi và bot.
    /// - Xử lý bán dầu: kiểm tra số dầu, tính tiền, cập nhật PlayerData.
    /// - Phát sự kiện OnOilPriceChanged khi giá thay đổi.
    /// - Phát sự kiện OnOilSold khi bán dầu thành công.
    /// 
    /// Luồng bán dầu:
    /// 1. Người chơi mở MarketUI, thấy giá hiện tại.
    /// 2. Người chơi nhập số lượng hoặc nhấn "Bán tất cả".
    /// 3. MarketUI gọi MarketManager.SellOil(amount).
    /// 4. MarketManager kiểm tra dầu, tính tiền, cập nhật PlayerData.
    /// 5. Trả về số tiền nhận được.
    /// </summary>
    public class MarketManager : MonoBehaviour, IMarketService
    {
        [Header("Tham chiếu")]
        [Tooltip("GameConfig để lấy tham số giá dầu và thời gian cập nhật.")]
        [SerializeField] private GameConfig gameConfig;

        // === Dữ liệu runtime ===

        /// <summary>Giá dầu hiện tại.</summary>
        private long currentOilPrice;

        /// <summary>Thời gian còn lại đến lần cập nhật giá tiếp theo (giây).</summary>
        private float timeUntilNextUpdate;

        /// <summary>Coroutine đang chạy update giá.</summary>
        private IEnumerator priceUpdateCoroutine;

        // Tham chiếu đến PlayerDataService
        private IPlayerDataService playerDataService;

        #region IMarketService Properties

        /// <summary>Giá dầu hiện tại.</summary>
        public long CurrentOilPrice => currentOilPrice;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Đăng ký service
            ServiceLocator.Register<IMarketService>(this);
        }

        private void Start()
        {
            // Lấy PlayerDataService
            playerDataService = ServiceLocator.Get<IPlayerDataService>();
            if (playerDataService == null)
            {
                Debug.LogError("[MarketManager] IPlayerDataService chưa được đăng ký!");
            }

            // Khởi tạo giá mặc định
            if (gameConfig != null)
            {
                currentOilPrice = (long)Random.Range(gameConfig.minOilPrice, gameConfig.maxOilPrice + 1);
            }
            else
            {
                currentOilPrice = 10;
                Debug.LogWarning("[MarketManager] GameConfig chưa được gán, dùng giá mặc định $10.");
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IMarketService>();
            StopPriceUpdate();
        }

        #endregion

        #region Cập nhật giá dầu

        /// <summary>
        /// Bắt đầu Coroutine cập nhật giá dầu định kỳ.
        /// Gọi bởi GameManager khi game bắt đầu.
        /// </summary>
        public void StartPriceUpdate()
        {
            if (gameConfig == null)
            {
                Debug.LogError("[MarketManager] GameConfig chưa được gán!");
                return;
            }

            StopPriceUpdate();

            priceUpdateCoroutine = PriceUpdateRoutine();
            CoroutineRunner.Instance.Run(priceUpdateCoroutine);

        }

        /// <summary>
        /// Dừng Coroutine cập nhật giá.
        /// </summary>
        public void StopPriceUpdate()
        {
            if (priceUpdateCoroutine != null)
            {
                CoroutineRunner.Instance.Stop(priceUpdateCoroutine);
                priceUpdateCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine cập nhật giá dầu mỗi khoảng thời gian.
        /// </summary>
        private IEnumerator PriceUpdateRoutine()
        {
            while (true)
            {
                // Đợi đến lần cập nhật tiếp theo
                timeUntilNextUpdate = gameConfig.priceUpdateInterval;

                while (timeUntilNextUpdate > 0f)
                {
                    timeUntilNextUpdate -= Time.deltaTime;
                    yield return null;
                }

                // Random giá mới
                float oldPrice = currentOilPrice;
                currentOilPrice = (long)Random.Range(gameConfig.minOilPrice, gameConfig.maxOilPrice + 1);

                Debug.Log($"[MarketManager] Giá dầu thay đổi: ${oldPrice} -> ${currentOilPrice}");

                // Phát sự kiện
                EventBus.Publish(new OnOilPriceChanged(oldPrice, currentOilPrice));
            }
        }

        /// <summary>
        /// Lấy thời gian còn lại đến lần cập nhật giá tiếp theo.
        /// Dùng để UI hiển thị countdown.
        /// </summary>
        public float GetTimeUntilNextPriceUpdate()
        {
            return timeUntilNextUpdate;
        }

        #endregion

        #region Bán dầu

        /// <summary>
        /// Bán một lượng dầu với giá hiện tại.
        /// </summary>
        /// <param name="amount">Lượng dầu muốn bán.</param>
        /// <returns>Số tiền nhận được (0 nếu bán thất bại).</returns>
        public long SellOil(long amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning("[MarketManager] Lượng dầu bán phải > 0!");
                return 0;
            }

            if (playerDataService == null)
            {
                Debug.LogError("[MarketManager] PlayerDataService chưa sẵn sàng!");
                return 0;
            }

            // Kiểm tra người chơi có đủ dầu không
            if (playerDataService.OilHeld < amount)
            {
                Debug.LogWarning($"[MarketManager] Không đủ dầu! Cần {amount}, hiện có {playerDataService.OilHeld}.");
                return 0;
            }

            // Tính tiền
            float moneyEarned = amount * currentOilPrice;

            // Trừ dầu
            playerDataService.SubtractOil(amount, OilChangeReason.Sell);

            // Thêm tiền
            playerDataService.AddMoney((long)moneyEarned, MoneyChangeReason.SellOil);

            // Phát sự kiện bán dầu
            EventBus.Publish(new OnOilSold(amount, currentOilPrice, moneyEarned));

            Debug.Log($"[MarketManager] Bán {amount} Oil x ${currentOilPrice} = ${moneyEarned}.");

            return (long)moneyEarned;
        }

        /// <summary>
        /// Bán toàn bộ dầu hiện có.
        /// </summary>
        /// <returns>Số tiền nhận được.</returns>
        public float SellAllOil()
        {
            if (playerDataService == null) return 0f;
            return SellOil((long)playerDataService.OilHeld);
        }

        #endregion

        #region Save / Load

        /// <summary>
        /// Lấy giá dầu hiện tại để lưu.
        /// </summary>
        public float GetPriceForSave()
        {
            return currentOilPrice;
        }

        /// <summary>
        /// Khôi phục giá dầu từ SaveData.
        /// </summary>
        /// <param name="price">Giá dầu đã lưu.</param>
        public void SetPriceFromSave(float price)
        {
            float oldPrice = currentOilPrice;
            currentOilPrice = (long)price;


            // Phát sự kiện để UI cập nhật
            EventBus.Publish(new OnOilPriceChanged(oldPrice, currentOilPrice));
        }

        #endregion
    }
}