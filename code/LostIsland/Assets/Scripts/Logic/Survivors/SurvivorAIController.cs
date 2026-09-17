using System;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;
using LostIsland.Logic.Wave;

namespace LostIsland.Logic.Survivors
{
    /// <summary>
    /// 幸存者AI状态
    /// </summary>
    public enum SurvivorAIState
    {
        Idle = 0,         // 待机
        Chase = 1,        // 追击敌人
        Attack = 2,       // 攻击
        Flee = 3,         // 逃跑（低血量）
        Assist = 4,       // 辅助（治疗等）
        Casting = 5,      // 释放技能
    }

    /// <summary>
    /// 幸存者战斗AI控制器
    /// 控制幸存者在战斗中的行为
    /// </summary>
    public class SurvivorAIController
    {
        #region 配置

        /// <summary>AI数据引用</summary>
        public SurvivorInstance Survivor { get; private set; }

        /// <summary>当前状态</summary>
        public SurvivorAIState CurrentState { get; private set; }

        /// <summary>目标丧尸</summary>
        public ZombieController Target { get; private set; }

        #endregion

        #region 战斗参数

        private float _attackTimer = 0f;
        private float _skillTimer = 0f;
        private float _stateTimer = 0f;

        /// <summary>攻击间隔</summary>
        private float AttackInterval => 1.0f; // 默认1秒一次

        /// <summary>攻击范围</summary>
        private float AttackRange
        {
            get
            {
                if (Survivor == null || Survivor.Data == null) return 2f;
                switch (Survivor.Data.Class)
                {
                    case SurvivorClass.Archer: return 7f;
                    case SurvivorClass.Medic: return 5f;
                    case SurvivorClass.Scout: return 3f;
                    case SurvivorClass.Engineer: return 6f;
                    default: return 2f; // 战士/守卫近战
                }
            }
        }

        /// <summary>移动速度</summary>
        private float MoveSpeed => Survivor != null ? Survivor.Data.BaseSpeed : 2f;

        /// <summary>低血量逃跑阈值</summary>
        private const float LOW_HP_FLEE_THRESHOLD = 0.15f;

        #endregion

        #region 位置

        /// <summary>当前位置</summary>
        public Vector3 Position { get; set; }

        /// <summary>灯塔位置（防守目标）</summary>
        public Vector3 TowerPosition { get; set; }

        /// <summary>最大追击距离</summary>
        private const float MAX_CHASE_DISTANCE = 15f;

        #endregion

        #region 构造函数

        public SurvivorAIController(SurvivorInstance survivor, Vector3 position)
        {
            Survivor = survivor;
            Position = position;
            CurrentState = SurvivorAIState.Idle;
            _attackTimer = 0f;
            _skillTimer = 0f;
        }

        #endregion

        #region 主更新

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update(float dt)
        {
            if (Survivor == null || Survivor.Status == SurvivorStatus.Injured) return;

            _attackTimer += dt;
            _skillTimer += dt;
            _stateTimer += dt;

            // 先检查是否需要逃跑（低血量）
            if (CheckFlee())
            {
                UpdateFlee(dt);
                return;
            }

            // 职业特殊行为
            if (Survivor.Data.Class == SurvivorClass.Medic)
            {
                // 医生优先检查是否需要治疗
                if (CheckAssist())
                {
                    UpdateAssist(dt);
                    return;
                }
            }

            // 查找目标
            FindTarget();

            // 根据目标决定状态
            if (Target == null || Target.IsDead)
            {
                ChangeState(SurvivorAIState.Idle);
                UpdateIdle(dt);
            }
            else
            {
                float distToTarget = Vector3.Distance(Position, Target.MoveComp.Position);

                if (distToTarget <= AttackRange)
                {
                    // 在攻击范围内
                    ChangeState(SurvivorAIState.Attack);
                    UpdateAttack(dt);
                }
                else if (distToTarget <= MAX_CHASE_DISTANCE)
                {
                    // 在追击范围内
                    ChangeState(SurvivorAIState.Chase);
                    UpdateChase(dt);
                }
                else
                {
                    // 太远，回到防御位置
                    ChangeState(SurvivorAIState.Idle);
                    UpdateIdle(dt);
                }
            }
        }

        #endregion

        #region 状态更新

        private void UpdateIdle(float dt)
        {
            // 待机时慢慢回到塔附近
            if (Vector3.Distance(Position, TowerPosition) > 3f)
            {
                Vector3 dir = (TowerPosition - Position).normalized;
                Position += dir * MoveSpeed * 0.5f * dt;
            }
        }

        private void UpdateChase(float dt)
        {
            if (Target == null) return;

            Vector3 dir = (Target.MoveComp.Position - Position).normalized;
            Position += dir * MoveSpeed * dt;
        }

        private void UpdateAttack(float dt)
        {
            if (Target == null || Target.IsDead) return;

            // 面向目标
            Vector3 dir = (Target.MoveComp.Position - Position).normalized;

            // 攻击冷却
            if (_attackTimer >= AttackInterval)
            {
                _attackTimer = 0f;
                PerformAttack();
            }

            // 技能冷却
            if (Survivor.Data.Skills.Count > 0 && _skillTimer >= Survivor.Data.Skills[0].Cooldown)
            {
                _skillTimer = 0f;
                PerformSkill();
            }
        }

        private void UpdateFlee(float dt)
        {
            // 朝灯塔方向逃跑
            Vector3 dir = (TowerPosition - Position).normalized;
            Position += dir * MoveSpeed * 1.2f * dt; // 逃跑加速

            // 如果回到塔附近且血量回升，退出逃跑
            float distToTower = Vector3.Distance(Position, TowerPosition);
            if (distToTower < 2f && Survivor.HPPct > LOW_HP_FLEE_THRESHOLD + 0.1f)
            {
                ChangeState(SurvivorAIState.Idle);
            }
        }

        private void UpdateAssist(float dt)
        {
            // 医生辅助逻辑：向队友移动并治疗
            // 简化：回到塔附近
            Vector3 dir = (TowerPosition - Position).normalized;
            Position += dir * MoveSpeed * 0.7f * dt;
        }

        #endregion

        #region 行为逻辑

        /// <summary>
        /// 查找目标
        /// </summary>
        private void FindTarget()
        {
            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return;

            // 如果当前目标有效，保持
            if (Target != null && !Target.IsDead)
            {
                float dist = Vector3.Distance(Position, Target.MoveComp.Position);
                if (dist <= MAX_CHASE_DISTANCE)
                    return;
            }

            // 找最近的敌人
            ZombieController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var zombie in spawner.ActiveZombies)
            {
                if (zombie == null || zombie.IsDead) continue;

                float dist = Vector3.Distance(Position, zombie.MoveComp.Position);
                if (dist < nearestDist && dist <= MAX_CHASE_DISTANCE)
                {
                    nearestDist = dist;
                    nearest = zombie;
                }
            }

            Target = nearest;
        }

        /// <summary>
        /// 检查是否需要逃跑
        /// </summary>
        private bool CheckFlee()
        {
            if (Survivor.HPPct <= LOW_HP_FLEE_THRESHOLD)
            {
                if (CurrentState != SurvivorAIState.Flee)
                {
                    ChangeState(SurvivorAIState.Flee);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否需要辅助（医生）
        /// </summary>
        private bool CheckAssist()
        {
            // 简化：如果没有敌人在附近，进入辅助状态
            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return false;

            int nearbyEnemies = 0;
            foreach (var zombie in spawner.ActiveZombies)
            {
                if (zombie == null || zombie.IsDead) continue;
                float dist = Vector3.Distance(Position, zombie.MoveComp.Position);
                if (dist < AttackRange * 1.5f)
                    nearbyEnemies++;
            }

            return nearbyEnemies == 0;
        }

        /// <summary>
        /// 执行普通攻击
        /// </summary>
        private void PerformAttack()
        {
            if (Target == null || Target.IsDead) return;

            float damage = Survivor.ATK;

            // 暴击判定
            float critRate = Survivor.Data.BaseCritRate;
            bool isCrit = UnityEngine.Random.value < critRate;
            if (isCrit)
            {
                damage *= Survivor.Data.BaseCritDamage;
            }

            var result = DamageResult.Create(damage);
            result.SourceType = DamageSourceType.Skill;
            result.SourceName = Survivor.Data.SurvivorName;
            result.IsCrit = isCrit;

            Target.TakeDamage(result);

            // Debug.Log($"[SurvivorAI] {Survivor.Data.SurvivorName} 攻击 {damage:F0} 伤害");
        }

        /// <summary>
        /// 释放技能
        /// </summary>
        private void PerformSkill()
        {
            if (Survivor.Data.Skills.Count == 0) return;

            var skill = Survivor.Data.Skills[0];
            if (skill.SkillType != SurvivorSkillType.Active) return;

            // 简单技能实现
            if (skill.DamageMultiplier > 0 && Target != null && !Target.IsDead)
            {
                float damage = Survivor.ATK * skill.DamageMultiplier;

                var result = DamageResult.Create(damage);
                result.SourceType = DamageSourceType.Skill;
                result.SourceName = skill.SkillName;
                result.IsSkillDamage = true;

                Target.TakeDamage(result);

                Debug.Log($"[SurvivorAI] {Survivor.Data.SurvivorName} 释放 {skill.SkillName}, 伤害 {damage:F0}");
            }
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public float TakeDamage(float damage)
        {
            float actual = Survivor.TakeDamage(damage);

            if (Survivor.Status == SurvivorStatus.Injured)
            {
                ChangeState(SurvivorAIState.Idle);
                Debug.Log($"[SurvivorAI] {Survivor.Data.SurvivorName} 倒下了！");
            }

            return actual;
        }

        #endregion

        #region 状态切换

        private void ChangeState(SurvivorAIState newState)
        {
            if (newState == CurrentState) return;

            // 退出旧状态
            ExitState(CurrentState);

            CurrentState = newState;
            _stateTimer = 0f;

            // 进入新状态
            EnterState(newState);
        }

        private void EnterState(SurvivorAIState state)
        {
            switch (state)
            {
                case SurvivorAIState.Attack:
                    _attackTimer = AttackInterval * 0.5f; // 进入攻击状态时快速攻击一次
                    break;
            }
        }

        private void ExitState(SurvivorAIState state)
        {
        }

        #endregion
    }
}
