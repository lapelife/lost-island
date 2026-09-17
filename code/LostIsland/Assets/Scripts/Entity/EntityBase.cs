using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Entity
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public enum EntityType
    {
        None = 0,
        Player = 1,         // 玩家
        Zombie = 2,         // 丧尸
        Tower = 3,          // 防御塔
        Survivor = 4,       // 幸存者
        Boss = 5,           // BOSS
    }

    /// <summary>
    /// 实体基类：Entity-Component 模式
    /// 玩家、丧尸、防御塔等所有游戏实体都继承此类
    /// 持有 AttributeComponent 和 HealthComponent 作为基础组件
    /// 支持动态添加/移除其他组件
    /// </summary>
    public class EntityBase
    {
        #region 基础属性

        /// <summary>
        /// 实体唯一ID
        /// </summary>
        public int EntityId { get; protected set; }

        /// <summary>
        /// 实体类型
        /// </summary>
        public EntityType EntityType { get; protected set; }

        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; protected set; }

        /// <summary>
        /// 等级
        /// </summary>
        public int Level { get; protected set; }

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => Health != null && !Health.IsDead;

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        public bool IsInitialized { get; protected set; }

        #endregion

        #region 核心组件

        /// <summary>
        /// 属性组件
        /// </summary>
        public AttributeComponent Attribute { get; protected set; }

        /// <summary>
        /// 生命组件
        /// </summary>
        public HealthComponent Health { get; protected set; }

        #endregion

        #region 组件系统

        // 组件字典：类型 -> 组件实例
        private Dictionary<Type, EntityComponent> _components = new Dictionary<Type, EntityComponent>();

        // 更新用的组件列表（避免遍历时修改字典）
        private List<EntityComponent> _updateList = new List<EntityComponent>();
        private bool _updateListDirty = false;

        #endregion

        #region 事件

        /// <summary>
        /// 实体初始化完成事件
        /// </summary>
        public event Action<EntityBase> OnEntityInitialized;

        /// <summary>
        /// 实体销毁事件
        /// </summary>
        public event Action<EntityBase> OnEntityDisposed;

        #endregion

        #region 构造与初始化

        public EntityBase()
        {
            // 创建核心组件
            Attribute = new AttributeComponent();
            Health = new HealthComponent(Attribute);
        }

        /// <summary>
        /// 初始化实体
        /// </summary>
        public virtual void Initialize(int entityId, EntityType type, string name)
        {
            if (IsInitialized) return;

            EntityId = entityId;
            EntityType = type;
            EntityName = name;
            Level = 1;

            // 初始化所有已添加的组件
            foreach (var kvp in _components)
            {
                kvp.Value.Initialize(this);
            }

            IsInitialized = true;
            OnEntityInitialized?.Invoke(this);

            Debug.Log($"[EntityBase] 实体初始化: {EntityName} (ID={EntityId}, Type={EntityType})");
        }

        #endregion

        #region 组件管理

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        public T AddComponent<T>() where T : EntityComponent, new()
        {
            Type type = typeof(T);

            if (_components.ContainsKey(type))
            {
                Debug.LogWarning($"[EntityBase] 组件已存在: {type.Name}");
                return _components[type] as T;
            }

            T component = new T();
            _components[type] = component;
            _updateListDirty = true;

            // 如果实体已初始化，则立即初始化组件
            if (IsInitialized)
            {
                component.Initialize(this);
            }

            return component;
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例，不存在返回null</returns>
        public T GetComponent<T>() where T : EntityComponent
        {
            Type type = typeof(T);
            if (_components.TryGetValue(type, out var component))
            {
                return component as T;
            }
            return null;
        }

        /// <summary>
        /// 尝试获取组件
        /// </summary>
        public bool TryGetComponent<T>(out T component) where T : EntityComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

        /// <summary>
        /// 检查是否有某组件
        /// </summary>
        public bool HasComponent<T>() where T : EntityComponent
        {
            return _components.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public bool RemoveComponent<T>() where T : EntityComponent
        {
            Type type = typeof(T);
            if (_components.TryGetValue(type, out var component))
            {
                component.Dispose();
                _components.Remove(type);
                _updateListDirty = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新组件更新列表
        /// </summary>
        private void RefreshUpdateList()
        {
            _updateList.Clear();
            foreach (var kvp in _components)
            {
                _updateList.Add(kvp.Value);
            }
            _updateListDirty = false;
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 每帧更新
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (!IsInitialized) return;
            if (!IsAlive) return;

            if (_updateListDirty)
                RefreshUpdateList();

            // 更新所有组件
            for (int i = 0; i < _updateList.Count; i++)
            {
                _updateList[i].Update(deltaTime);
            }

            // 更新生命组件的无敌帧
            Health?.UpdateInvincibleTimer(deltaTime);

            // 更新生命组件的护盾计时器
            Health?.UpdateShieldTimer(deltaTime);
        }

        /// <summary>
        /// 固定帧率更新
        /// </summary>
        public virtual void FixedUpdate(float fixedDeltaTime)
        {
            if (!IsInitialized) return;
            if (!IsAlive) return;

            if (_updateListDirty)
                RefreshUpdateList();

            for (int i = 0; i < _updateList.Count; i++)
            {
                _updateList[i].FixedUpdate(fixedDeltaTime);
            }
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public virtual void Dispose()
        {
            if (!IsInitialized) return;

            // 销毁所有组件
            foreach (var kvp in _components)
            {
                kvp.Value.Dispose();
            }
            _components.Clear();
            _updateList.Clear();

            IsInitialized = false;
            OnEntityDisposed?.Invoke(this);

            Debug.Log($"[EntityBase] 实体销毁: {EntityName} (ID={EntityId})");
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 设置等级
        /// </summary>
        public virtual void SetLevel(int level)
        {
            Level = Mathf.Max(1, level);
        }

        /// <summary>
        /// 输出实体信息
        /// </summary>
        public void DebugDump()
        {
            Debug.Log($"=== Entity: {EntityName} (ID={EntityId}) ===");
            Debug.Log($"Type: {EntityType}, Level: {Level}, Alive: {IsAlive}");
            Debug.Log($"Components: {_components.Count}");
            Attribute?.DebugDump();
            Health?.DebugDump();
        }

        #endregion
    }
}
