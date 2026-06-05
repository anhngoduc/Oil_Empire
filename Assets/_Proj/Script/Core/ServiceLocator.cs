// Assets/_Project/Scripts/Core/ServiceLocator.cs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OilGame
{
    /// <summary>
    /// Service Locator - Định vị service toàn cục.
    /// Các Manager đăng ký service của mình thông qua Interface.
    /// Các Manager khác truy xuất service qua Get<T>().
    /// 
    /// Cách dùng:
    ///   Đăng ký: ServiceLocator.Register<IPlayerDataService>(playerDataManager);
    ///   Truy xuất: IPlayerDataService service = ServiceLocator.Get<IPlayerDataService>();
    /// </summary>
    public static class ServiceLocator
    {
        // Dictionary lưu trữ tất cả service đã đăng ký
        // Key = Type của Interface, Value = instance của service
        private static Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// Đăng ký một service. Chỉ đăng ký được một instance cho mỗi Interface.
        /// Nếu đã tồn tại, ghi đè và log cảnh báo.
        /// </summary>
        /// <typeparam name="T">Interface của service.</typeparam>
        /// <param name="service">Instance implement interface đó.</param>
        public static void Register<T>(T service)
        {
            Type type = typeof(T);
            if (services.ContainsKey(type))
            {
                services[type] = service;
            }
            else
            {
                services.Add(type, service);
            }
        }

        /// <summary>
        /// Hủy đăng ký một service.
        /// </summary>
        /// <typeparam name="T">Interface của service cần hủy.</typeparam>
        public static void Unregister<T>()
        {
            Type type = typeof(T);
            if (services.ContainsKey(type))
            {
                services.Remove(type);
                Debug.Log($"[ServiceLocator] Đã hủy đăng ký service: {type.Name}");
            }
        }

        /// <summary>
        /// Lấy service đã đăng ký.
        /// Trả về null nếu chưa được đăng ký.
        /// </summary>
        /// <typeparam name="T">Interface cần lấy.</typeparam>
        /// <returns>Instance của service hoặc null.</returns>
        public static T Get<T>()
        {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object service))
            {
                return (T)service;
            }
            Debug.LogError($"[ServiceLocator] Service {type.Name} chưa được đăng ký!");
            return default(T);
        }

        /// <summary>
        /// Kiểm tra xem một service đã được đăng ký chưa.
        /// </summary>
        /// <typeparam name="T">Interface cần kiểm tra.</typeparam>
        /// <returns>True nếu đã đăng ký.</returns>
        public static bool IsRegistered<T>()
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Xóa tất cả service đã đăng ký. Dùng khi thoát game hoặc reset.
        /// </summary>
        public static void ClearAll()
        {
            services.Clear();
            Debug.Log("[ServiceLocator] Đã xóa tất cả service.");
        }
    }
}