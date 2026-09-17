// ============================================================
// MonoSingleton.cs
// 单例基类 - 所有Manager的基类
// 提供全局访问点、生命周期管理、DontDestroyOnLoad
// ============================================================

using UnityEngine;

namespace LostIsland.Core
{
    /// <summary>
    /// 泛型单例基类，继承MonoBehaviour
    /// 使用方法：public class MyManager : MonoSingleton<MyManager>
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isApplicationQuitting = false;

        /// <summary>
        /// 全局访问点
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_isApplicationQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] Instance of {typeof(T)} already destroyed on application quit.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();

                        if (FindObjectsOfType<T>().Length > 1)
                        {
                            Debug.LogError($"[MonoSingleton] Multiple instances of {typeof(T)} found!");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"[Singleton] {typeof(T).Name}";
                            DontDestroyOnLoad(singletonObject);
                            _instance.OnInit();
                        }
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// 是否已创建实例
        /// </summary>
        public static bool IsCreated => _instance != null;

        /// <summary>
        /// 应用是否正在退出
        /// </summary>
        protected static bool IsApplicationQuitting => _isApplicationQuitting;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
                OnInit();
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] Duplicate instance of {typeof(T).Name} detected. Destroying.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化回调 - 在单例创建时调用一次
        /// </summary>
        protected virtual void OnInit()
        {
            // 子类重写
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                OnDispose();
                _instance = null;
            }
        }

        /// <summary>
        /// 销毁回调 - 在单例销毁时调用一次
        /// </summary>
        protected virtual void OnDispose()
        {
            // 子类重写
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        /// <summary>
        /// 手动销毁单例
        /// </summary>
        public static void DestroyInstance()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        /// <summary>
        /// 重置退出标志（Editor下用）
        /// </summary>
        public static void ResetApplicationQuitFlag()
        {
            _isApplicationQuitting = false;
        }
    }
}
