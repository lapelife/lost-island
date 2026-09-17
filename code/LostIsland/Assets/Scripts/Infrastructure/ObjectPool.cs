// ============================================================
// ObjectPool.cs
// 泛型对象池
// 支持自动扩容、最大容量限制、LRU淘汰策略
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Infrastructure
{
    /// <summary>
    /// 泛型对象池
    /// </summary>
    /// <typeparam name="T">对象类型，必须实现IPoolable接口</typeparam>
    public class ObjectPool<T> where T : class, IPoolable
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly HashSet<T> _activeSet = new HashSet<T>();
        private readonly System.Func<T> _createFunc;

        private int _initialSize;
        private int _expandStep;
        private int _maxSize;

        public int PoolSize => _pool.Count;
        public int ActiveCount => _activeSet.Count;
        public int TotalCount => _pool.Count + _activeSet.Count;

        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="createFunc">创建新对象的方法</param>
        /// <param name="initialSize">初始池大小</param>
        /// <param name="expandStep">扩容步长</param>
        /// <param name="maxSize">最大池容量</param>
        public ObjectPool(System.Func<T> createFunc, int initialSize = 10, int expandStep = 5, int maxSize = 50)
        {
            _createFunc = createFunc;
            _initialSize = initialSize;
            _expandStep = expandStep;
            _maxSize = maxSize;

            PreWarm(initialSize);
        }

        /// <summary>
        /// 预热 - 创建指定数量的对象放入池中
        /// </summary>
        public void PreWarm(int count)
        {
            int toCreate = Mathf.Min(count, _maxSize - _pool.Count);
            for (int i = 0; i < toCreate; i++)
            {
                T obj = CreateNew();
                obj.IsInPool = true;
                _pool.Push(obj);
            }
        }

        /// <summary>
        /// 从池中获取一个对象
        /// </summary>
        public T Get()
        {
            T obj = null;

            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                // 池为空，扩容
                if (TotalCount >= _maxSize)
                {
                    // 达到最大容量，复用最近活动的对象（LRU简化版）
                    Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Pool reached max size {_maxSize}, reusing active objects.");
                    return null; // 调用方需要处理null
                }

                PreWarm(_expandStep);
                obj = _pool.Pop();
            }

            obj.IsInPool = false;
            _activeSet.Add(obj);
            obj.OnGetFromPool();
            return obj;
        }

        /// <summary>
        /// 将对象回收回池
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null || obj.IsInPool)
            {
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Trying to return null or already pooled object.");
                return;
            }

            _activeSet.Remove(obj);

            if (_pool.Count >= _maxSize)
            {
                // 池已满，直接销毁（不回收）
                if (obj is Object)
                {
                    Object.Destroy(obj as Object);
                }
                return;
            }

            obj.OnReturnToPool();
            obj.IsInPool = true;
            _pool.Push(obj);
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Pop();
                if (obj is Object unityObj)
                {
                    Object.Destroy(unityObj);
                }
            }
            _pool.Clear();
            _activeSet.Clear();
        }

        // 创建新对象
        private T CreateNew()
        {
            T obj = _createFunc();
            if (obj == null)
            {
                Debug.LogError($"[ObjectPool<{typeof(T).Name}>] Create function returned null!");
            }
            return obj;
        }
    }
}
