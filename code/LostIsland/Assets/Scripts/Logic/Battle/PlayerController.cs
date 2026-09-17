using UnityEngine;
using LostIsland.Entity;
using LostIsland.Core;
using LostIsland.Logic.Battle;

namespace LostIsland.Logic.Battle
{
    /// <summary>
    /// 玩家控制器：处理玩家的移动、攻击、狂暴模式等
    /// 基于 EntityBase 的组件化架构，作为玩家实体的大脑
    /// </summary>
    public class PlayerController
    {
        #region 配置

        [Header("移动配置")]
        [Tooltip("移动速度基础值")]
        public float BaseMoveSpeed = 5f;

        [Header("攻击配置")]
        [Tooltip("自动攻击范围检测半径")]
        public float AutoAttackRange = 3f;

        [Tooltip("攻击范围")]
        public float AttackRange = 2f;

        [Header("狂暴配置")]
        [Tooltip("狂暴值上限")]
        public float MaxRage = 100f;

        [Tooltip("每次攻击积累狂暴值")]
        public float RagePerAttack = 5f;

        [Tooltip("击杀积累狂暴值")]
        public float RagePerKill = 15f;

        [Tooltip("狂暴持续时间（秒）")]
        public float RageDuration = 15f;

        [Tooltip("狂暴伤害倍率")]
        public float RageDamageMultiplier = 2f;

        [Header("受击反馈")]
        [Tooltip("受击无敌帧（秒）")]
        public float HitInvincibleTime = 0.5f;

        [Tooltip("击退距离")]
        public float KnockbackDistance = 0.5f;

        #endregion

        #region 引用

        /// <summary>
        /// 玩家实体
        /// </summary>
        public EntityBase Entity { get; private set; }

        /// <summary>
        /// 移动组件
        /// </summary>
        public MoveComponent MoveComp { get; private set; }

        /// <summary>
        /// 攻击组件
        /// </summary>
        public AttackComponent AttackComp { get; private set; }

        #endregion

        #region 移动控制

        /// <summary>
        /// 移动输入方向（-1~1）
        /// </summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>
        /// 是否有移动输入
        /// </summary>
        public bool HasMoveInput => MoveInput.sqrMagnitude > 0.01f;

        #endregion

        #region 狂暴模式

        /// <summary>
        /// 当前狂暴值
        /// </summary>
        public float CurrentRage { get; private set; }

        /// <summary>
        /// 狂暴值百分比
        /// </summary>
        public float RagePercent => Mathf.Clamp01(CurrentRage / MaxRage);

        /// <summary>
        /// 是否狂暴中
        /// </summary>
        public bool IsRaging { get; private set; }

        /// <summary>
        /// 狂暴剩余时间
        /// </summary>
        public float RageRemainingTime { get; private set; }

        /// <summary>
        /// 狂暴是否可用（满值且不在狂暴中）
        /// </summary>
        public bool CanActivateRage => CurrentRage >= MaxRage && !IsRaging;

        #endregion

        #region 状态

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead => Entity != null && Entity.Health != null && Entity.Health.IsDead;

        /// <summary>
        /// 死亡后复活时间
        /// </summary>
        public float ReviveTime = 3f;

        private float _deathTimer = 0f;

        #endregion

        #region 初始化

        public PlayerController(EntityBase entity)
        {
            Entity = entity;

            // 获取或添加组件
            MoveComp = entity.GetComponent<MoveComponent>();
            if (MoveComp == null)
            {
                MoveComp = entity.AddComponent<MoveComponent>();
            }

            AttackComp = entity.GetComponent<AttackComponent>();
            if (AttackComp == null)
            {
                AttackComp = entity.AddComponent<AttackComponent>();
            }

            // 配置基础值
            MoveComp.SetBaseSpeed(BaseMoveSpeed);
            AttackComp.SetAttackRange(AttackRange);

            // 监听事件
            entity.Health.OnDeath += OnDeath;
            AttackComp.OnAttackHit += OnAttackHit;

            CurrentRage = 0f;
            IsRaging = false;
        }

        #endregion

        #region 移动控制

        /// <summary>
        /// 设置移动输入
        /// </summary>
        public void SetMoveInput(Vector2 direction)
        {
            MoveInput = direction;

            if (IsDead) return;

            if (HasMoveInput)
            {
                Vector3 dir3D = new Vector3(direction.x, 0f, direction.y).normalized;
                MoveComp.SetMoveDirection(dir3D);
            }
            else
            {
                MoveComp.StopMove();
            }
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMove()
        {
            MoveInput = Vector2.zero;
            MoveComp.StopMove();
        }

        #endregion

        #region 攻击逻辑

        /// <summary>
        /// 自动攻击：寻找最近的敌人并攻击
        /// </summary>
        public void AutoAttack(EntityBase[] enemies)
        {
            if (IsDead) return;
            if (enemies == null || enemies.Length == 0) return;

            // 找最近的敌人
            EntityBase nearestEnemy = null;
            float nearestDist = float.MaxValue;

            Vector3 playerPos = MoveComp.Position;

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;

                var enemyMove = enemy.GetComponent<MoveComponent>();
                if (enemyMove == null) continue;

                float dist = Vector3.Distance(playerPos, enemyMove.Position);
                if (dist < nearestDist && dist <= AutoAttackRange)
                {
                    nearestDist = dist;
                    nearestEnemy = enemy;
                }
            }

            if (nearestEnemy != null)
            {
                // 设置目标并尝试攻击
                AttackComp.SetTarget(nearestEnemy);
                AttackComp.TryAttack();
            }
            else
            {
                AttackComp.ClearTarget();
            }
        }

        /// <summary>
        /// 攻击命中回调
        /// </summary>
        private void OnAttackHit(EntityBase target, float damage)
        {
            // 积累狂暴值
            if (!IsRaging)
            {
                AddRage(RagePerAttack);
            }

            // 狂暴模式下增加伤害（通过AttackComp的DamageMultiplier实现）
        }

        #endregion

        #region 狂暴模式

        /// <summary>
        /// 增加狂暴值
        /// </summary>
        public void AddRage(float amount)
        {
            if (IsRaging) return;

            float oldPct = RagePercent;
            CurrentRage = Mathf.Min(CurrentRage + amount, MaxRage);
            float newPct = RagePercent;

            if (Mathf.Abs(oldPct - newPct) > 0.001f)
            {
                EventBus.Trigger(new RageValueChangedEvent
                {
                    CurrentRage = CurrentRage,
                    MaxRage = MaxRage,
                    RagePercent = newPct
                });
            }
        }

        /// <summary>
        /// 激活狂暴模式
        /// </summary>
        public bool ActivateRage()
        {
            if (!CanActivateRage) return false;

            IsRaging = true;
            RageRemainingTime = RageDuration;
            CurrentRage = 0f;

            // 设置狂暴伤害倍率
            AttackComp.SetDamageMultiplier(RageDamageMultiplier);

            Debug.Log($"[PlayerController] 狂暴模式激活！持续{RageDuration}秒，伤害{RageDamageMultiplier}x");

            EventBus.Trigger(new RageModeActivatedEvent
            {
                Duration = RageDuration
            });

            return true;
        }

        /// <summary>
        /// 结束狂暴模式
        /// </summary>
        private void EndRage()
        {
            if (!IsRaging) return;

            IsRaging = false;
            RageRemainingTime = 0f;

            // 恢复伤害倍率
            AttackComp.SetDamageMultiplier(1f);

            Debug.Log("[PlayerController] 狂暴模式结束");

            EventBus.Trigger(new RageModeEndedEvent());
        }

        #endregion

        #region 死亡与重生

        /// <summary>
        /// 死亡回调
        /// </summary>
        private void OnDeath()
        {
            Debug.Log("[PlayerController] 玩家死亡");
            StopMove();
            _deathTimer = ReviveTime;

            // 结束狂暴
            if (IsRaging)
            {
                EndRage();
            }
        }

        /// <summary>
        /// 手动复活
        /// </summary>
        public void Revive()
        {
            if (!IsDead) return;

            Entity.Health.Revive();
            _deathTimer = 0f;

            // 给短暂无敌
            Entity.Health.AddInvincibleTime(2f);

            Debug.Log("[PlayerController] 玩家复活");
        }

        #endregion

        #region 受击反馈

        /// <summary>
        /// 受击处理
        /// </summary>
        public void OnTakeHit(Vector3 hitDirection)
        {
            if (IsDead) return;

            // 无敌帧
            Entity.Health.AddInvincibleTime(HitInvincibleTime);

            // 击退
            if (MoveComp != null && KnockbackDistance > 0f)
            {
                Vector3 knockback = hitDirection.normalized * KnockbackDistance;
                MoveComp.SetPosition(MoveComp.Position - knockback);
            }

            // 积累狂暴值（受击也积累）
            AddRage(2f);
        }

        #endregion

        #region 更新

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update(float deltaTime)
        {
            if (IsDead)
            {
                // 死亡倒计时
                _deathTimer -= deltaTime;
                if (_deathTimer <= 0f)
                {
                    // 自动复活（战斗中）
                    Revive();
                }
                return;
            }

            // 更新狂暴计时
            if (IsRaging)
            {
                RageRemainingTime -= deltaTime;
                if (RageRemainingTime <= 0f)
                {
                    EndRage();
                }
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 重置玩家状态
        /// </summary>
        public void Reset()
        {
            CurrentRage = 0f;
            IsRaging = false;
            RageRemainingTime = 0f;
            MoveInput = Vector2.zero;
            _deathTimer = 0f;

            MoveComp?.Reset();
            AttackComp?.Reset();
            AttackComp?.SetDamageMultiplier(1f);

            Entity?.Health?.Reset();
        }

        #endregion
    }
}
