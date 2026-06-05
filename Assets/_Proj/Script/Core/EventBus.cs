// Assets/_Project/Scripts/Core/EventBus.cs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// EventBus - Trung tâm phát/nhận sự kiện toàn game.
    /// Sử dụng generic để đảm bảo type-safe.
    /// 
    /// Cách dùng:
    ///   Đăng ký: EventBus.Subscribe<OnMoneyChanged>(HandleMoneyChanged);
    ///   Hủy đăng ký: EventBus.Unsubscribe<OnMoneyChanged>(HandleMoneyChanged);
    ///   Phát sự kiện: EventBus.Publish(new OnMoneyChanged { newAmount = 100 });
    /// </summary>
    public static class EventBus
    {
        // Dictionary lưu danh sách listener cho mỗi loại sự kiện
        // Key = Type của sự kiện, Value = danh sách các Action đăng ký
        private static Dictionary<Type, List<Delegate>> listeners = new Dictionary<Type, List<Delegate>>();

        /// <summary>
        /// Đăng ký lắng nghe một loại sự kiện.
        /// </summary>
        /// <typeparam name="T">Loại sự kiện (class hoặc struct).</typeparam>
        /// <param name="callback">Hàm callback được gọi khi sự kiện phát.</param>
        public static void Subscribe<T>(Action<T> callback) where T : struct
        {
            Type eventType = typeof(T);
            if (!listeners.ContainsKey(eventType))
            {
                listeners[eventType] = new List<Delegate>();
            }
            // Tránh đăng ký trùng lặp
            if (!listeners[eventType].Contains(callback))
            {
                listeners[eventType].Add(callback);
            }
        }

        /// <summary>
        /// Hủy đăng ký lắng nghe một loại sự kiện.
        /// </summary>
        /// <typeparam name="T">Loại sự kiện.</typeparam>
        /// <param name="callback">Hàm callback đã đăng ký trước đó.</param>
        public static void Unsubscribe<T>(Action<T> callback) where T : struct
        {
            Type eventType = typeof(T);
            if (listeners.TryGetValue(eventType, out List<Delegate> delegateList))
            {
                delegateList.Remove(callback);
                // Nếu không còn ai lắng nghe, xóa key để tiết kiệm bộ nhớ
                if (delegateList.Count == 0)
                {
                    listeners.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Phát một sự kiện đến tất cả listener đã đăng ký.
        /// </summary>
        /// <typeparam name="T">Loại sự kiện.</typeparam>
        /// <param name="eventData">Dữ liệu sự kiện.</param>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);
            if (listeners.TryGetValue(eventType, out List<Delegate> delegateList))
            {
                // Duyệt ngược để an toàn nếu listener tự hủy đăng ký trong callback
                for (int i = delegateList.Count - 1; i >= 0; i--)
                {
                    if (delegateList[i] is Action<T> action)
                    {
                        try
                        {
                            action.Invoke(eventData);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[EventBus] Lỗi khi phát sự kiện {eventType.Name}: {e.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Xóa tất cả listener. Dùng khi load scene mới hoặc thoát game.
        /// </summary>
        public static void ClearAll()
        {
            listeners.Clear();
            Debug.Log("[EventBus] Đã xóa tất cả listener.");
        }

        /// <summary>
        /// Debug: In ra tất cả sự kiện đang có listener.
        /// </summary>
        public static void DebugPrint()
        {
            Debug.Log($"[EventBus] Tổng số loại sự kiện đang lắng nghe: {listeners.Count}");
            foreach (var kvp in listeners)
            {
                Debug.Log($"  - {kvp.Key.Name}: {kvp.Value.Count} listener(s)");
            }
        }
    }
}