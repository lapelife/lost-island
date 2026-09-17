using UnityEngine;

namespace LostIsland.Entity
{
    /// <summary>
    /// 移动组件：处理实体的移动逻辑
    /// 支持设置移动速度、目标点、移动方向等
    /// </summary>
    public class MoveComponent : EntityComponent
    {
        #region 移动状态

        /// <summary>
        /// 当前位置
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        /// 移动方向（归一化）
        /// </summary>
        public Vector3 MoveDirection { get; private set; }

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving { get; private set; }

        /// <summary>
        /// 目标点（移动到目标点模式）
        /// </summary>
        private Vector3? _targetPosition;

        /// <summary>
        /// 到达目标点的停止距离
        /// </summary>
        private float _stopDistance = 0.1f;

        #endregion

        #region 速度相关

        /// <summary>
        /// 实际移动速度（从属性系统获取）
        /// </summary>
        public float MoveSpeed
        {
            get
            {
                if (_entity != null && _entity.Attribute != null)
                {
                    return _entity.Attribute.FinalMoveSpeed;
                }
                return _baseSpeed;
            }
        }

        /// <summary>
        /// 基础移动速度
        /// </summary>
        private float _baseSpeed = 3f;

        /// <summary>
        /// 速度倍率（用于减速/加速效果）
        /// </summary>
        private float _speedMultiplier = 1f;

        /// <summary>
        /// 是否被定身
        /// </summary>
        private bool _isRooted = false;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            Position = Vector3.zero;
            MoveDirection = Vector3.zero;
            IsMoving = false;
        }

        #endregion

        #region 移动控制

        /// <summary>
        /// 设置基础移动速度
        /// </summary>
        public void SetBaseSpeed(float speed)
        {
            _baseSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// 直接设置位置
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        /// <summary>
        /// 设置移动方向（持续移动模式）
        /// </summary>
        /// <param name="direction">移动方向（会自动归一化）</param>
        public void SetMoveDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                MoveDirection = direction;
                IsMoving = !_isRooted;
                _targetPosition = null;
            }
            else
            {
                StopMove();
            }
        }

        /// <summary>
        /// 移动到目标点
        /// </summary>
        /// <param name="target">目标位置</param>
        /// <param name="stopDistance">停止距离</param>
        public void MoveTo(Vector3 target, float stopDistance = 0.1f)
        {
            _targetPosition = target;
            _stopDistance = Mathf.Max(0f, stopDistance);
            IsMoving = !_isRooted;
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMove()
        {
            MoveDirection = Vector3.zero;
            IsMoving = false;
            _targetPosition = null;
        }

        #endregion

        #region 速度效果

        /// <summary>
        /// 设置速度倍率
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// 设置定身状态
        /// </summary>
        public void SetRooted(bool rooted)
        {
            _isRooted = rooted;
            if (rooted)
            {
                IsMoving = false;
            }
        }

        #endregion

        #region 更新逻辑

        protected override void OnUpdate(float deltaTime)
        {
            if (!IsMoving || _isRooted) return;

            float actualSpeed = MoveSpeed * _speedMultiplier;
            if (actualSpeed <= 0f) return;

            if (_targetPosition.HasValue)
            {
                // 移动到目标点模式
                Vector3 target = _targetPosition.Value;
                Vector3 toTarget = target - Position;
                float distance = toTarget.magnitude;

                if (distance <= _stopDistance)
                {
                    // 到达目标
                    Position = target;
                    StopMove();
                    return;
                }

                MoveDirection = toTarget.normalized;
            }

            // 应用移动
            Position += MoveDirection * actualSpeed * deltaTime;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 计算到目标点的距离
        /// </summary>
        public float DistanceTo(Vector3 target)
        {
            return Vector3.Distance(Position, target);
        }

        /// <summary>
        /// 重置移动组件
        /// </summary>
        public void Reset()
        {
            StopMove();
            _speedMultiplier = 1f;
            _isRooted = false;
        }

        #endregion
    }
}
