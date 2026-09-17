using System;
using UnityEngine;

namespace LostIsland.Entity
{
    /// <summary>
    /// 攻击组件：处理实体的攻击逻辑
    /// 管理攻击冷却、攻击范围、目标检测等
    /// </summary>
    public class AttackComponent : EntityComponent
    {
        #region 攻击状态

        /// <summary>
        /// 是否正在攻击
        /// </summary>
        public bool IsAttacking { get; private set; }

        /// <summary>
        /// 攻击冷却计时器
        /// </summary>
        private float _attackCooldownTimer = 0f;

        /// <summary>
        /// 攻击冷却是否结束
        /// </summary>
        public bool CanAttack => _attackCooldownTimer <= 0f && !_isAttackDisabled;

        /// <summary>
        /// 当前攻击目标
        /// </summary>
        public EntityBase CurrentTarget { get; private set; }

        #endregion

        #region 攻击属性

        /// <summary>
        /// 实际攻击间隔（从属性系统获取攻速）
        /// </summary>
        public float AttackInterval
        {
            get
            {
                if (_entity != null && _entity.Attribute != null)
                {
                    float attackSpeed = _entity.Attribute.FinalAttackSpeed;
                    return attackSpeed > 0f ? 1f / attackSpeed : 1f;
                }
                return 1f;
            }
        }

        /// <summary>
        /// 攻击范围
        /// </summary>
        public float AttackRange { get; private set; } = 2f;

        /// <summary>
        /// 攻击伤害倍率（技能加成）
        /// </summary>
        public float DamageMultiplier { get; private set; } = 1f;

        /// <summary>
        /// 是否禁用攻击
        /// </summary>
        private bool _isAttackDisabled = false;

        #endregion

        #region 事件

        /// <summary>
        /// 攻击开始事件
        /// </summary>
        public event Action<EntityBase> OnAttackStart;

        /// <summary>
        /// 攻击命中事件：目标，伤害
        /// </summary>
        public event Action<EntityBase, float> OnAttackHit;

        /// <summary>
        /// 攻击结束事件
        /// </summary>
        public event Action OnAttackEnd;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            IsAttacking = false;
            _attackCooldownTimer = 0f;
        }

        #endregion

        #region 攻击控制

        /// <summary>
        /// 设置攻击范围
        /// </summary>
        public void SetAttackRange(float range)
        {
            AttackRange = Mathf.Max(0.1f, range);
        }

        /// <summary>
        /// 设置伤害倍率
        /// </summary>
        public void SetDamageMultiplier(float multiplier)
        {
            DamageMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// 设置攻击目标
        /// </summary>
        public void SetTarget(EntityBase target)
        {
            CurrentTarget = target;
        }

        /// <summary>
        /// 清除攻击目标
        /// </summary>
        public void ClearTarget()
        {
            CurrentTarget = null;
        }

        /// <summary>
        /// 设置攻击禁用状态
        /// </summary>
        public void SetAttackDisabled(bool disabled)
        {
            _isAttackDisabled = disabled;
            if (disabled)
            {
                IsAttacking = false;
            }
        }

        #endregion

        #region 攻击逻辑

        /// <summary>
        /// 尝试发动攻击
        /// </summary>
        /// <returns>是否成功发动攻击</returns>
        public bool TryAttack()
        {
            if (!CanAttack) return false;
            if (CurrentTarget == null || !CurrentTarget.IsAlive) return false;

            // 检查攻击范围
            float distance = 0f;
            var moveComp = _entity.GetComponent<MoveComponent>();
            var targetMoveComp = CurrentTarget.GetComponent<MoveComponent>();
            if (moveComp != null && targetMoveComp != null)
            {
                distance = moveComp.DistanceTo(targetMoveComp.Position);
            }

            if (distance > AttackRange)
            {
                return false;
            }

            // 开始攻击
            StartAttack();
            return true;
        }

        /// <summary>
        /// 开始攻击
        /// </summary>
        private void StartAttack()
        {
            IsAttacking = true;
            _attackCooldownTimer = AttackInterval;

            OnAttackStart?.Invoke(CurrentTarget);

            // 简化版：立即造成伤害（后续可加入攻击前摇/后摇动画）
            ApplyDamage();

            // 攻击结束
            EndAttack();
        }

        /// <summary>
        /// 应用伤害
        /// </summary>
        private void ApplyDamage()
        {
            if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
            if (_entity?.Attribute == null) return;

            // 计算伤害
            float baseDamage = _entity.Attribute.FinalATK * DamageMultiplier;
            bool isCrit = false;
            float finalDamage = baseDamage;

            // 暴击判定
            float critRate = _entity.Attribute.FinalCritRate;
            if (Random.value < critRate)
            {
                isCrit = true;
                finalDamage *= _entity.Attribute.FinalCritDamage;
            }

            // 构建伤害结果
            DamageResult damageResult = new DamageResult
            {
                RawDamage = baseDamage,
                FinalDamage = finalDamage,
                IsCrit = isCrit,
                Source = _entity
            };

            // 对目标造成伤害
            float actualDamage = CurrentTarget.Health.TakeDamage(damageResult);

            // 触发命中事件
            if (actualDamage > 0f)
            {
                OnAttackHit?.Invoke(CurrentTarget, actualDamage);

                // 吸血
                float lifesteal = _entity.Attribute.FinalLifesteal;
                if (lifesteal > 0f && _entity.Health != null)
                {
                    float healAmount = actualDamage * lifesteal;
                    _entity.Health.Heal(healAmount);
                }
            }
        }

        /// <summary>
        /// 结束攻击
        /// </summary>
        private void EndAttack()
        {
            IsAttacking = false;
            OnAttackEnd?.Invoke();
        }

        #endregion

        #region 更新逻辑

        protected override void OnUpdate(float deltaTime)
        {
            // 更新攻击冷却
            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= deltaTime;
                if (_attackCooldownTimer < 0f)
                {
                    _attackCooldownTimer = 0f;
                }
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 重置攻击组件
        /// </summary>
        public void Reset()
        {
            IsAttacking = false;
            _attackCooldownTimer = 0f;
            CurrentTarget = null;
            DamageMultiplier = 1f;
            _isAttackDisabled = false;
        }

        /// <summary>
        /// 立即重置冷却
        /// </summary>
        public void ResetCooldown()
        {
            _attackCooldownTimer = 0f;
        }

        /// <summary>
        /// 增加冷却时间（用于减速攻击）
        /// </summary>
        public void AddCooldown(float time)
        {
            _attackCooldownTimer += time;
        }

        #endregion
    }
}
