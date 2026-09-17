// ============================================================
// PoolManager.cs
// 对象池管理器 - 管理所有对象池
// 按类型分类，统一管理所有需要池化的对象
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Infrastructure
{
    /// <summary>
    /// 对象池管理器
    /// 管理所有类型的对象池，统一调度
    /// </summary>
    public class PoolManager : MonoSingleton<PoolManager>
    {
        private Dictionary<string, object> _pools = new Dictionary<string, object>();

        protected override void OnInit()
        {
            base.OnInit();
            Debug.Log("[PoolManager] Initialized.");
        }

        /// <summary>
        /// 创建一个GameObject类型的对象池
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父节点</param>
        /// <param name="initialSize">初始大小</param>
        /// <param name="expandStep">扩容步长</param>
        /// <param name="maxSize">最大容量</param>
        public GameObjectPool CreateGameObjectPool(string poolName, GameObject prefab,
            Transform parent = null, int initialSize = 10, int expandStep = 5, int maxSize = 50)
        {
            if (_pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[PoolManager] Pool '{poolName}' already exists.");
                return _pools[poolName] as GameObjectPool;
            }

            GameObjectPool pool = new GameObjectPool(prefab, parent, initialSize, expandStep, maxSize);
            _pools.Add(poolName, pool);
            return pool;
        }

        /// <summary>
        /// 获取对象池
        /// </summary>
        public GameObjectPool GetPool(string poolName)
        {
            if (_pools.TryGetValue(poolName, out object pool))
            {
                return pool as GameObjectPool;
            }
            Debug.LogError($"[PoolManager] Pool '{poolName}' not found!");
            return null;
        }

        /// <summary>
        /// 从指定池获取对象
        /// </summary>
        public GameObject GetFromPool(string poolName)
        {
            GameObjectPool pool = GetPool(poolName);
            return pool?.Get();
        }

        /// <summary>
        /// 回收对象到指定池
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            GameObjectPool pool = GetPool(poolName);
            pool?.Return(obj);
        }

        /// <summary>
        /// 销毁所有池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in _pools)
            {
                if (kvp.Value is GameObjectPool goPool)
                {
                    goPool.Clear();
                }
            }
            _pools.Clear();
        }

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        public string GetPoolStats()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Pool Stats ===");
            foreach (var kvp in _pools)
            {
                if (kvp.Value is GameObjectPool goPool)
                {
                    sb.AppendLine($"  {kvp.Key}: pool={goPool.PoolSize}, active={goPool.ActiveCount}, total={goPool.TotalCount}");
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// GameObject专用对象池
    /// </summary>
    public class GameObjectPool
    {
        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private readonly HashSet<GameObject> _activeSet = new HashSet<GameObject>();
        private readonly GameObject _prefab;
        private readonly Transform _parent;

        private int _initialSize;
        private int _expandStep;
        private int _maxSize;

        public int PoolSize => _pool.Count;
        public int ActiveCount => _activeSet.Count;
        public int TotalCount => _pool.Count + _activeSet.Count;

        public GameObjectPool(GameObject prefab, Transform parent, int initialSize, int expandStep, int maxSize)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            _expandStep = expandStep;
            _maxSize = maxSize;

            PreWarm(initialSize);
        }

        public void PreWarm(int count)
        {
            int toCreate = Mathf.Min(count, _maxSize - _pool.Count);
            for (int i = 0; i < toCreate; i++)
            {
                GameObject obj = CreateNew();
                obj.SetActive(false);
                _pool.Push(obj);
            }
        }

        public GameObject Get()
        {
            GameObject obj = null;

            while (_pool.Count > 0)
            {
                obj = _pool.Pop();
                if (obj != null) break;
            }

            if (obj == null)
            {
                if (TotalCount >= _maxSize)
                {
                    // 达到上限，强制复用一个活跃对象
                    Debug.LogWarning($"[GameObjectPool] Max size {_maxSize} reached for {_prefab.name}.");
                    return null;
                }

                PreWarm(_expandStep);
                obj = _pool.Pop();
            }

            _activeSet.Add(obj);
            obj.SetActive(true);

            // 通知IPoolable组件
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var p in poolables)
            {
                p.IsInPool = false;
                p.OnGetFromPool();
            }

            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null || !_activeSet.Contains(obj)) return;

            _activeSet.Remove(obj);

            if (_pool.Count >= _maxSize)
            {
                Object.Destroy(obj);
                return;
            }

            // 通知IPoolable组件
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var p in poolables)
            {
                p.OnReturnToPool();
                p.IsInPool = true;
            }

            obj.SetActive(false);
            if (_parent != null)
            {
                obj.transform.SetParent(_parent);
            }
            _pool.Push(obj);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                GameObject obj = _pool.Pop();
                if (obj != null) Object.Destroy(obj);
            }
            _pool.Clear();

            foreach (var obj in _activeSet)
            {
                if (obj != null) Object.Destroy(obj);
            }
            _activeSet.Clear();
        }

        private GameObject CreateNew()
        {
            GameObject obj = Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name}_{TotalCount}";
            return obj;
        }
    }
}
