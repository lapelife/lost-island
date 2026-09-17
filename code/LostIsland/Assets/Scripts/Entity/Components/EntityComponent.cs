using UnityEngine;

namespace LostIsland.Entity
{
    /// <summary>
    /// 实体组件基类
    /// 所有可附加到 EntityBase 上的组件都继承此类
    /// </summary>
    public abstract class EntityComponent
    {
        /// <summary>
        /// 所属实体
        /// </summary>
        protected EntityBase _entity;

        /// <summary>
        /// 组件是否已初始化
        /// </summary>
        protected bool _isInitialized = false;

        /// <summary>
        /// 组件是否已启用
        /// </summary>
        protected bool _isEnabled = true;

        /// <summary>
        /// 组件名称
        /// </summary>
        public virtual string ComponentName => GetType().Name;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 组件是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_isEnabled)
                        OnEnabled();
                    else
                        OnDisabled();
                }
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        public void Initialize(EntityBase entity)
        {
            if (_isInitialized) return;

            _entity = entity;
            OnInit();
            _isInitialized = true;
        }

        /// <summary>
        /// 初始化回调（子类重写）
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isInitialized || !_isEnabled) return;
            OnUpdate(deltaTime);
        }

        /// <summary>
        /// 固定帧率更新（物理相关）
        /// </summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (!_isInitialized || !_isEnabled) return;
            OnFixedUpdate(fixedDeltaTime);
        }

        /// <summary>
        /// 更新回调（子类重写）
        /// </summary>
        protected virtual void OnUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// 固定更新回调（子类重写）
        /// </summary>
        protected virtual void OnFixedUpdate(float fixedDeltaTime)
        {
        }

        /// <summary>
        /// 组件启用时回调
        /// </summary>
        protected virtual void OnEnabled()
        {
        }

        /// <summary>
        /// 组件禁用时回调
        /// </summary>
        protected virtual void OnDisabled()
        {
        }

        /// <summary>
        /// 销毁组件
        /// </summary>
        public void Dispose()
        {
            if (!_isInitialized) return;

            OnDispose();
            _isInitialized = false;
            _entity = null;
        }

        /// <summary>
        /// 销毁回调（子类重写）
        /// </summary>
        protected virtual void OnDispose()
        {
        }
    }
}
