using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Core;
using LostIsland.Logic.Battle;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// 丧尸控制器：管理丧尸的AI、属性、行为
    /// 基于 EntityBase 的组件化架构，持有AI状态机
    /// 
    /// 状态机：Spawn → Chase → Attack → Chase → ... → Dead
    /// </summary>
    public class ZombieController
    {
        #region 配置与引用

        /// <summary>
        /// 丧尸配置
        /// </summary>
        public ZombieConfigSO Config { get; private set; }

        /// <summary>
        /// 丧尸实体
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

        #region AI状态机

        private Dictionary<ZombieState, ZombieStateBase> _states = new Dictionary<ZombieState, ZombieStateBase>();
        private ZombieStateBase _currentState;

        /// <summary>
        /// 当前状态
        /// </summary>
        public ZombieState CurrentState { get; private set; } = ZombieState.None;

        #endregion

        #region 目标

        /// <summary>
        /// 攻击目标
        /// </summary>
        public EntityBase AttackTarget { get; private set; }

        /// <summary>
        /// 目标位置（灯塔位置）
        /// </summary>
        public Vector3 TowerPosition { get; set; }

        /// <summary>
        /// 玩家位置引用
        /// </summary>
        public Func<Vector3> GetPlayerPosition { get; set; }

        #endregion

        #region 特殊属性

        /// <summary>
        /// 护盾HP
        /// </summary>
        public float ShieldHP { get; private set; }

        /// <summary>
        /// 最大护盾HP
        /// </summary>
        public float MaxShieldHP { get; private set; }

        /// <summary>
        /// 添加护盾
        /// </summary>
        /// <param name="amount">护盾值</param>
        public void AddShield(float amount)
        {
            if (amount <= 0f) return;
            ShieldHP += amount;
            MaxShieldHP = Mathf.Max(MaxShieldHP, ShieldHP);
        }

        /// <summary>
        /// 是否有护盾
        /// </summary>
        public bool HasShield => Config != null && Config.HasShield && ShieldHP > 0f;

        /// <summary>
        /// 丧尸类型
        /// </summary>
        public ZombieType ZombieType => Config != null ? Config.ZombieType : ZombieType.Normal;

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead => Entity != null && Entity.Health != null && Entity.Health.IsDead;

        #endregion

        #region 状态效果系统

        private Dictionary<StatusType, StatusEffectData> _activeStatuses = new Dictionary<StatusType, StatusEffectData>();

        /// <summary>
        /// 状态效果数据
        /// </summary>
        private class StatusEffectData
        {
            public StatusType Type;
            public float Value;           // 效果数值（如伤害/减速比例）
            public float Duration;        // 总持续时间
            public float RemainingTime;   // 剩余时间
            public float TickTimer;       // 跳变计时器（用于持续伤害）
            public const float TickInterval = 0.5f; // 每0.5秒跳一次
        }

        /// <summary>
        /// 基础移动速度（用于减速效果恢复）
        /// </summary>
        private float _baseMoveSpeed;

        /// <summary>
        /// 当前减速比例
        /// </summary>
        private float _currentSlowAmount = 0f;

        #endregion

        #region 事件

        /// <summary>
        /// 死亡完成事件（可以回收了）
        /// </summary>
        public event Action<ZombieController> OnDeathCompleted;

        #endregion

        #region 初始化

        public ZombieController(EntityBase entity, ZombieConfigSO config)
        {
            Entity = entity;
            Config = config;

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

            // 初始化AI状态
            InitStateMachine();

            // 监听死亡
            entity.Health.OnDeath += OnZombieDeath;
        }

        /// <summary>
        /// 初始化状态机
        /// </summary>
        private void InitStateMachine()
        {
            _states[ZombieState.Spawn] = new ZombieState_Spawn(this);
            _states[ZombieState.Chase] = new ZombieState_Chase(this);
            _states[ZombieState.Attack] = new ZombieState_Attack(this);
            _states[ZombieState.Stun] = new ZombieState_Stun(this);
            _states[ZombieState.Dead] = new ZombieState_Dead(this);
        }

        /// <summary>
        /// 初始化属性（根据波次和天数）
        /// </summary>
        public void InitAttributes(int wave, int day, float hpMult, float atkMult, float defMult)
        {
            float hp = Config.GetHP(wave, day, hpMult);
            float atk = Config.GetATK(wave, day, atkMult);
            float def = Config.GetDEF(wave, day, defMult);
            float moveSpeed = Config.GetMoveSpeed();

            Entity.Attribute.SetBaseValue(AttrType.MaxHP, hp);
            Entity.Attribute.SetBaseValue(AttrType.ATK, atk);
            Entity.Attribute.SetBaseValue(AttrType.DEF, def);
            Entity.Attribute.SetBaseValue(AttrType.MoveSpeed, moveSpeed);
            Entity.Attribute.SetBaseValue(AttrType.AttackSpeed, Config.BaseAttackSpeed);

            Entity.Health.HealFull();

            // 保存基础移速（用于减速效果计算）
            _baseMoveSpeed = moveSpeed;
            _currentSlowAmount = 0f;

            // 护盾
            if (Config.HasShield)
            {
                MaxShieldHP = Config.ShieldHP;
                ShieldHP = Config.ShieldHP;
            }

            // 配置攻击组件
            AttackComp.SetAttackRange(Config.AttackRange);

            // 配置移动速度
            MoveComp.SetBaseSpeed(moveSpeed);
        }

        /// <summary>
        /// 出生（进入出生状态）
        /// </summary>
        /// <param name="position">出生位置</param>
        public void Spawn(Vector3 position)
        {
            MoveComp.SetPosition(position);
            ChangeState(ZombieState.Spawn);
        }

        #endregion

        #region 状态机操作

        /// <summary>
        /// 切换状态
        /// </summary>
        public void ChangeState(ZombieState newState)
        {
            if (newState == CurrentState) return;

            // 退出旧状态
            _currentState?.Exit();

            // 切换到新状态
            CurrentState = newState;
            if (_states.TryGetValue(newState, out var state))
            {
                _currentState = state;
                _currentState.Enter();
            }
            else
            {
                _currentState = null;
                Debug.LogWarning($"[ZombieController] 未找到状态: {newState}");
            }
        }

        /// <summary>
        /// 眩晕
        /// </summary>
        public void Stun(float duration)
        {
            if (CurrentState == ZombieState.Dead) return;
            if (_states.TryGetValue(ZombieState.Stun, out var stunState))
            {
                (stunState as ZombieState_Stun)?.SetDuration(duration);
                ChangeState(ZombieState.Stun);
            }
        }

        #endregion

        #region 状态效果系统

        /// <summary>
        /// 应用状态效果
        /// </summary>
        /// <param name="statusType">状态类型</param>
        /// <param name="value">效果数值（伤害/减速比例等）</param>
        /// <param name="duration">持续时间（秒）</param>
        public void ApplyStatus(StatusType statusType, float value, float duration)
        {
            if (IsDead) return;
            if (statusType == StatusType.None) return;

            // 如果已有相同状态，刷新持续时间并取最高效果值
            if (_activeStatuses.TryGetValue(statusType, out var existing))
            {
                existing.RemainingTime = Mathf.Max(existing.RemainingTime, duration);
                existing.Value = Mathf.Max(existing.Value, value);
                return;
            }

            // 创建新状态效果
            var statusData = new StatusEffectData
            {
                Type = statusType,
                Value = value,
                Duration = duration,
                RemainingTime = duration,
                TickTimer = 0f
            };

            _activeStatuses[statusType] = statusData;

            // 立即应用状态效果
            OnStatusApplied(statusType, value);
        }

        /// <summary>
        /// 状态效果应用时的即时处理
        /// </summary>
        private void OnStatusApplied(StatusType statusType, float value)
        {
            switch (statusType)
            {
                case StatusType.Stun:
                    // 立即进入眩晕状态
                    Stun(value > 0f ? value : 0.5f);
                    break;

                case StatusType.Slow:
                    // 应用减速
                    ApplySlowEffect(value);
                    break;

                case StatusType.Burn:
                    // 燃烧效果在tick中处理，无需即时处理
                    break;

                case StatusType.Freeze:
                    // 冰冻 = 减速 + 短暂眩晕
                    ApplySlowEffect(value);
                    break;
            }
        }

        /// <summary>
        /// 应用减速效果
        /// </summary>
        private void ApplySlowEffect(float slowAmount)
        {
            if (slowAmount <= 0f) return;

            _currentSlowAmount = Mathf.Max(_currentSlowAmount, slowAmount);
            float newSpeed = _baseMoveSpeed * (1f - Mathf.Clamp01(_currentSlowAmount));
            MoveComp?.SetBaseSpeed(Mathf.Max(0.1f, newSpeed));
        }

        /// <summary>
        /// 移除状态效果
        /// </summary>
        private void RemoveStatus(StatusType statusType)
        {
            if (!_activeStatuses.ContainsKey(statusType)) return;

            _activeStatuses.Remove(statusType);

            // 处理状态移除后的恢复
            switch (statusType)
            {
                case StatusType.Slow:
                case StatusType.Freeze:
                    // 重新计算当前减速（可能有多个减速效果叠加）
                    RecalculateSlowAmount();
                    break;
            }
        }

        /// <summary>
        /// 重新计算当前减速总量
        /// </summary>
        private void RecalculateSlowAmount()
        {
            float maxSlow = 0f;
            foreach (var kvp in _activeStatuses)
            {
                if (kvp.Key == StatusType.Slow || kvp.Key == StatusType.Freeze)
                {
                    maxSlow = Mathf.Max(maxSlow, kvp.Value.Value);
                }
            }

            _currentSlowAmount = maxSlow;
            float newSpeed = _baseMoveSpeed * (1f - Mathf.Clamp01(_currentSlowAmount));
            MoveComp?.SetBaseSpeed(Mathf.Max(0.1f, newSpeed));
        }

        /// <summary>
        /// 更新状态效果
        /// </summary>
        private void UpdateStatusEffects(float dt)
        {
            if (_activeStatuses.Count == 0) return;

            // 收集需要移除的状态
            List<StatusType> toRemove = null;

            foreach (var kvp in _activeStatuses)
            {
                var status = kvp.Value;
                status.RemainingTime -= dt;

                // 处理持续伤害类型的状态（燃烧、中毒等）
                if (kvp.Key == StatusType.Burn || kvp.Key == StatusType.Poison)
                {
                    status.TickTimer += dt;
                    if (status.TickTimer >= StatusEffectData.TickInterval)
                    {
                        status.TickTimer -= StatusEffectData.TickInterval;
                        // 造成持续伤害
                        float tickDamage = status.Value * StatusEffectData.TickInterval;
                        var dmgResult = DamageResult.Create(tickDamage);
                        dmgResult.SourceType = DamageSourceType.Burn;
                        dmgResult.SourceName = kvp.Key.ToString();
                        TakeDamage(dmgResult);
                    }
                }

                // 检查状态是否结束
                if (status.RemainingTime <= 0f)
                {
                    if (toRemove == null) toRemove = new List<StatusType>();
                    toRemove.Add(kvp.Key);
                }
            }

            // 移除过期状态
            if (toRemove != null)
            {
                foreach (var statusType in toRemove)
                {
                    RemoveStatus(statusType);
                }
            }
        }

        /// <summary>
        /// 检查是否有指定状态
        /// </summary>
        public bool HasStatus(StatusType statusType)
        {
            return _activeStatuses.ContainsKey(statusType);
        }

        /// <summary>
        /// 获取状态效果剩余时间
        /// </summary>
        public float GetStatusRemainingTime(StatusType statusType)
        {
            if (_activeStatuses.TryGetValue(statusType, out var status))
                return status.RemainingTime;
            return 0f;
        }

        #endregion

        #region 目标管理

        /// <summary>
        /// 设置攻击目标
        /// </summary>
        public void SetAttackTarget(EntityBase target)
        {
            AttackTarget = target;
            AttackComp.SetTarget(target);
        }

        /// <summary>
        /// 获取目标位置
        /// 优先攻击玩家（如果配置为玩家优先或玩家很近），否则攻击灯塔
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            if (AttackTarget != null && AttackTarget.IsAlive)
            {
                var targetMove = AttackTarget.GetComponent<MoveComponent>();
                if (targetMove != null)
                    return targetMove.Position;
            }

            return TowerPosition;
        }

        /// <summary>
        /// 检查目标是否在攻击范围内
        /// </summary>
        public bool IsTargetInAttackRange()
        {
            Vector3 targetPos = GetTargetPosition();
            float dist = Vector3.Distance(MoveComp.Position, targetPos);
            return dist <= Config.AttackRange;
        }

        #endregion

        #region 攻击逻辑

        /// <summary>
        /// 对目标造成伤害
        /// </summary>
        public void DealDamageToTarget()
        {
            if (AttackTarget != null && AttackTarget.IsAlive)
            {
                DamageCalculator.DealDamage(Entity, AttackTarget);
            }
        }

        /// <summary>
        /// 受到伤害（考虑护盾）
        /// </summary>
        public float TakeDamage(DamageResult damage)
        {
            if (IsDead) return 0f;

            float actualDamage = damage.FinalDamage;

            // 护盾吸收伤害
            if (HasShield && actualDamage > 0f)
            {
                if (ShieldHP >= actualDamage)
                {
                    ShieldHP -= actualDamage;
                    actualDamage = 0f;
                    return 0f; // 全部被护盾吸收
                }
                else
                {
                    actualDamage -= ShieldHP;
                    ShieldHP = 0f;
                }
            }

            // 剩余伤害作用于HP
            var result = new DamageResult
            {
                FinalDamage = actualDamage,
                RawDamage = damage.RawDamage,
                IsCrit = damage.IsCrit,
                Source = damage.Source,
                SourceType = damage.SourceType,
                SourceName = damage.SourceName,
                IsSkillDamage = damage.IsSkillDamage,
                DefReduction = damage.DefReduction,
                DmgReductionPct = damage.DmgReductionPct,
                DamageReduction = damage.DamageReduction,
                LifestealHeal = damage.LifestealHeal,
                ReflectDamage = damage.ReflectDamage,
                IsTrueDamage = damage.IsTrueDamage
            };

            return Entity.Health.TakeDamage(result);
        }

        #endregion

        #region 死亡逻辑

        /// <summary>
        /// 丧尸死亡回调
        /// </summary>
        private void OnZombieDeath()
        {
            ChangeState(ZombieState.Dead);

            // 自爆丧尸死亡时爆炸
            if (Config.IsExploder)
            {
                TriggerExplosion();
            }
        }

        /// <summary>
        /// 自爆
        /// </summary>
        private void TriggerExplosion()
        {
            float explosionDamage = Entity.Attribute.FinalATK * Config.ExplosionDamageMultiplier;
            Debug.Log($"[ZombieController] 自爆丧尸爆炸！伤害: {explosionDamage:F0}, 范围: {Config.ExplosionRange}");
            // 范围伤害逻辑由调用方处理
        }

        /// <summary>
        /// 死亡动画完成（可以回收了）
        /// </summary>
        public void OnDeathComplete()
        {
            OnDeathCompleted?.Invoke(this);
        }

        #endregion

        #region 更新

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update(float dt)
        {
            if (IsDead && CurrentState != ZombieState.Dead)
            {
                ChangeState(ZombieState.Dead);
                return;
            }

            // 更新状态效果
            UpdateStatusEffects(dt);

            // 更新状态
            _currentState?.Update(dt);

            // 更新实体
            Entity.Update(dt);
        }

        #endregion

        #region 重置（对象池复用）

        /// <summary>
        /// 重置丧尸（对象池复用时调用）
        /// </summary>
        public void Reset()
        {
            _currentState = null;
            CurrentState = ZombieState.None;
            AttackTarget = null;
            ShieldHP = 0f;
            MaxShieldHP = 0f;

            // 清空状态效果
            _activeStatuses.Clear();
            _currentSlowAmount = 0f;

            MoveComp?.Reset();
            AttackComp?.Reset();
            AttackComp?.SetAttackDisabled(false);
            Entity.Health?.Reset();
        }

        #endregion
    }
}
