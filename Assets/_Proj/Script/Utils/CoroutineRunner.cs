// Assets/_Project/Scripts/Utils/CoroutineRunner.cs

using UnityEngine;
using System.Collections;

namespace OilGame
{
    /// <summary>
    /// Singleton MonoBehaviour tồn tại xuyên suốt game.
    /// Cho phép các class không phải MonoBehaviour chạy Coroutine.
    /// Được tạo tự động bởi GameManager khi game khởi động.
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        // Singleton instance - truy cập tĩnh từ bất kỳ đâu
        private static CoroutineRunner _instance;

        /// <summary>
        /// Instance duy nhất của CoroutineRunner.
        /// Nếu chưa tồn tại, tự động tạo một GameObject mới.
        /// </summary>
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Tự động tạo nếu chưa có (fallback an toàn)
                    GameObject go = new GameObject("[CoroutineRunner]");
                    _instance = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Khởi tạo: đảm bảo chỉ có một instance và không bị hủy khi load scene.
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Chạy một Coroutine từ bất kỳ class nào.
        /// Cách dùng: CoroutineRunner.Instance.StartCoroutine(MyCoroutine());
        /// </summary>
        /// <param name="routine">IEnumerator cần chạy.</param>
        public void Run(IEnumerator routine)
        {
            StartCoroutine(routine);
        }

        /// <summary>
        /// Dừng một Coroutine đang chạy.
        /// </summary>
        /// <param name="routine">Coroutine cần dừng.</param>
        public void Stop(IEnumerator routine)
        {
            StopCoroutine(routine);
        }

        /// <summary>
        /// Dừng tất cả Coroutine đang chạy trên runner này.
        /// </summary>
        public void StopAll()
        {
            StopAllCoroutines();
        }
    }
}