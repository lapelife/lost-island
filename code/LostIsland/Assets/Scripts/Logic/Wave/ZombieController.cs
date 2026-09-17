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
                Source = damage.Source
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

            MoveComp?.Reset();
            AttackComp?.Reset();
            AttackComp?.SetAttackDisabled(false);
            Entity.Health?.Reset();
        }

        #endregion
    }
}
