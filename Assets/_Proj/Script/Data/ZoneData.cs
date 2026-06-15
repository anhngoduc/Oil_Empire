// Assets/_Project/Scripts/Data/ZoneData.cs

using UnityEngine;
using System.Collections.Generic;

namespace OilGame
{
    /// <summary>
    /// ScriptableObject định nghĩa một khu đất.
    /// Mỗi Zone chứa nhiều mảnh xếp theo lưới đều.
    /// </summary>
    [CreateAssetMenu(fileName = "ZoneData", menuName = "OilGame/ZoneData")]
    public class ZoneData : ScriptableObject
    {
        [Header("=== Định danh ===")]
        public int zoneID;
        public string zoneName;

        [Header("=== Kích thước lưới ===")]
        [Tooltip("Số ô ngang mỗi mảnh.")]
        [Min(1)] public int cellsPerPlotX = 3;

        [Tooltip("Số ô dọc mỗi mảnh.")]
        [Min(1)] public int cellsPerPlotZ = 3;

        [Tooltip("Số cột mảnh (mảnh trên 1 hàng).")]
        [Min(1)] public int columns = 3;

        [Tooltip("Số hàng mảnh.")]
        [Min(1)] public int rows = 2;

        [Header("=== Danh sách mảnh ===")]
        [Tooltip("Danh sách thông tin từng mảnh. Số lượng = columns x rows.")]
        public List<PlotInfo> plots;

        /// <summary>Tổng số mảnh.</summary>
        public int TotalPlots => columns * rows;

        /// <summary>Tổng số ô ngang toàn Zone.</summary>
        public int TotalCellsX => columns * cellsPerPlotX;

        /// <summary>Tổng số ô dọc toàn Zone.</summary>
        public int TotalCellsZ => rows * cellsPerPlotZ;

        /// <summary>
        /// Lấy PlotInfo theo plotID.
        /// </summary>
        public PlotInfo GetPlot(int plotID)
        {
            return plots.Find(p => p.plotID == plotID);
        }

        /// <summary>
        /// Lấy PlotInfo chứa ô (globalX, globalZ).
        /// </summary>
        public PlotInfo GetPlotAtCell(int globalX, int globalZ)
        {
            int col = globalX / cellsPerPlotX;
            int row = globalZ / cellsPerPlotZ;
            int index = row * columns + col;
            if (index >= 0 && index < plots.Count)
                return plots[index];
            return null;
        }

        /// <summary>
        /// Kiểm tra plotID có tồn tại không.
        /// </summary>
        public bool HasPlot(int plotID)
        {
            return plots.Exists(p => p.plotID == plotID);
        }

        private void OnValidate()
        {
            // Tự động tạo danh sách PlotInfo nếu chưa đủ
            int expectedCount = columns * rows;
            while (plots.Count < expectedCount)
            {
                plots.Add(new PlotInfo
                {
                    plotID = plots.Count + 1,
                    oilMultiplier = 1,
                    unlockCost = 1000
                });
            }
            // Cập nhật plotID
            for (int i = 0; i < plots.Count; i++)
            {
                plots[i].plotID = i + 1;
            }
        }
        public void Initialize()
        {
            // Không cần làm gì vì không dùng dictionary nữa
        }
    }

    /// <summary>
    /// Thông tin một mảnh đất (thay thế PlotInfo ScriptableObject).
    /// </summary>
    [System.Serializable]
    public class PlotInfo
    {
        [Tooltip("ID mảnh (tự động gán).")]
        public int plotID;

        [Tooltip("Tên mảnh.")]
        public string plotName;

        [Tooltip("Hệ số nhân dầu.")]
        [Min(1)] public float oilMultiplier = 1;

        [Tooltip("Giá mở khóa.")]
        public long unlockCost = 1000;

        [Tooltip("màu mảnh đát.")]
        public Texture2D plotTexture;
    }
}