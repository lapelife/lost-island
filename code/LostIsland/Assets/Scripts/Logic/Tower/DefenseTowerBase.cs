using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Logic.Wave;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 防御塔基类
    /// 负责自动索敌和攻击逻辑
    /// </summary>
    public abstract class DefenseTowerBase : BuildingBase
    {
        #region 战斗属性

        /// <summary>当前伤害</summary>
        public float Damage => Config != null ? Config.GetDamage(Level) : 0f;

        /// <summary>当前攻速（每秒攻击次数）</summary>
        public float AttackSpeed => Config != null ? Config.GetAttackSpeed(Level) : 0f;

        /// <summary>当前射程</summary>
        public float Range => Config != null ? Config.GetRange(Level) : 0f;

        /// <summary>攻击间隔（秒）</summary>
        public float AttackInterval => AttackSpeed > 0 ? 1f / AttackSpeed : 999f;

        /// <summary>索敌策略</summary>
        public TargetingStrategy Targeting => Config != null ? Config.Targeting : TargetingStrategy.Nearest;

        /// <summary>攻击方式</summary>
        public AttackType AttackType => Config != null ? Config.AttackType : AttackType.SingleShot;

        #endregion

        #region 战斗状态

        /// <summary>攻击计时器</summary>
        protected float _attackTimer;

        /// <summary>当前目标</summary>
        protected ZombieController _currentTarget;

        /// <summary>是否正在攻击</summary>
        protected bool _isAttacking;

        /// <summary>塔的世界位置</summary>
        public Vector3 TowerPosition { get; set; }

        #endregion

        #region 初始化

        public override void Initialize(BuildingConfigSO config, int level, int floorIndex, int slotIndex)
        {
            base.Initialize(config, level, floorIndex, slotIndex);
            _attackTimer = 0f;
            _currentTarget = null;
            _isAttacking = false;
        }

        #endregion

        #region 生命周期

        public override void OnNightStart(int day, int wave)
        {
            base.OnNightStart(day, wave);
            _attackTimer = 0f;
            _currentTarget = null;
            _isAttacking = false;
        }

        public override void OnNightEnd(bool victory)
        {
            base.OnNightEnd(victory);
            _currentTarget = null;
            _isAttacking = false;
        }

        public override void Update(float dt)
        {
            base.Update(dt);

            if (!IsActive) return;

            // 索敌
            UpdateTarget();

            // 攻击
            UpdateAttack(dt);
        }

        #endregion

        #region 索敌系统

        /// <summary>
        /// 更新目标
        /// </summary>
        protected virtual void UpdateTarget()
        {
            // 检查当前目标是否有效
            if (_currentTarget != null)
            {
                if (_currentTarget.IsDead || !IsTargetInRange(_currentTarget))
                {
                    _currentTarget = null;
                }
            }

            // 寻找新目标
            if (_currentTarget == null)
            {
                _currentTarget = FindTarget();
            }
        }

        /// <summary>
        /// 检查目标是否在射程内
        /// </summary>
        protected bool IsTargetInRange(ZombieController target)
        {
            if (target == null) return false;
            float dist = Vector3.Distance(TowerPosition, target.MoveComp.Position);
            return dist <= Range;
        }

        /// <summary>
        /// 根据索敌策略寻找目标
        /// </summary>
        protected virtual ZombieController FindTarget()
        {
            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return null;

            var zombies = spawner.ActiveZombies;
            if (zombies == null || zombies.Count == 0) return null;

            ZombieController bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var zombie in zombies)
            {
                if (zombie == null || zombie.IsDead) continue;
                if (!IsTargetInRange(zombie)) continue;

                float score = CalculateTargetScore(zombie);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = zombie;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// 计算目标评分（根据索敌策略）
        /// </summary>
        protected virtual float CalculateTargetScore(ZombieController zombie)
        {
            float dist = Vector3.Distance(TowerPosition, zombie.MoveComp.Position);

            switch (Targeting)
            {
                case TargetingStrategy.Nearest:
                    return -dist;  // 距离越近，分数越高

                case TargetingStrategy.Farthest:
                    return dist;   // 距离越远，分数越高

                case TargetingStrategy.LowestHP:
                    return -zombie.Entity.Health.CurrentHP;

                case TargetingStrategy.HighestHP:
                    return zombie.Entity.Health.CurrentHP;

                case TargetingStrategy.FirstInPath:
                    // 简化：距离灯塔中心最近的（即最前面的）
                    float distToCenter = zombie.MoveComp.Position.magnitude;
                    return -distToCenter;

                default:
                    return -dist;
            }
        }

        #endregion

        #region 攻击系统

        /// <summary>
        /// 更新攻击逻辑
        /// </summary>
        protected virtual void UpdateAttack(float dt)
        {
            if (_currentTarget == null)
            {
                _attackTimer = 0f;
                return;
            }

            _attackTimer += dt;

            if (_attackTimer >= AttackInterval)
            {
                _attackTimer = 0f;
                FireAttack(_currentTarget);
            }
        }

        /// <summary>
        /// 执行攻击（子类实现具体攻击方式）
        /// </summary>
        protected abstract void FireAttack(ZombieController target);

        /// <summary>
        /// 对目标造成伤害
        /// </summary>
        protected virtual void DealDamage(ZombieController target, float damage)
        {
            if (target == null || target.IsDead) return;

            var result = DamageResult.Create(damage);
            result.SourceType = DamageSourceType.Tower;
            result.SourceName = Config.BuildingName;

            target.TakeDamage(result);
        }

        #endregion

        #region 升级效果

        public override void OnPostUpgrade()
        {
            base.OnPostUpgrade();
            // 升级后重新计算攻速等
        }

        #endregion
    }
}
