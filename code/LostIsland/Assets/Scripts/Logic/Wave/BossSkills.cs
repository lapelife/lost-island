using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// BOSS阶段定义
    /// </summary>
    [Serializable]
    public class BossPhase
    {
        public string PhaseName;
        [Range(0f, 1f)]
        public float HpThreshold;    // 触发阶段的HP百分比
        public float AtkMultiplier = 1f;   // 攻击倍率变化
        public float SpeedMultiplier = 1f; // 速度倍率变化
        public List<BossSkillConfig> PhaseSkills; // 本阶段解锁的技能
    }

    /// <summary>
    /// BOSS技能配置
    /// </summary>
    [Serializable]
    public class BossSkillConfig
    {
        public string SkillName;
        public float Cooldown = 10f;
        public float DamageMultiplier = 2f;
        public float CastTime = 1f;
        public float Range = 5f;
    }

    /// <summary>
    /// BOSS技能基类
    /// </summary>
    public abstract class BossSkillBase
    {
        protected BossController _boss;
        protected BossSkillConfig _config;

        protected float _cooldownTimer = 0f;
        protected bool _isCasting = false;
        protected float _castTimer = 0f;

        public string SkillName => _config.SkillName;
        public float Cooldown => _config.Cooldown;
        public bool IsReady => _cooldownTimer <= 0f && !_isCasting;
        public bool IsCasting => _isCasting;
        public float CooldownProgress => Mathf.Clamp01(1f - _cooldownTimer / _config.Cooldown);

        public BossSkillBase(BossController boss, BossSkillConfig config)
        {
            _boss = boss;
            _config = config;
            _cooldownTimer = 0f;
        }

        /// <summary>
        /// 尝试释放技能
        /// </summary>
        public virtual bool TryCast()
        {
            if (!IsReady) return false;

            _isCasting = true;
            _castTimer = 0f;
            OnCastStart();
            return true;
        }

        /// <summary>
        /// 更新技能
        /// </summary>
        public virtual void Update(float dt)
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= dt;
                if (_cooldownTimer < 0f) _cooldownTimer = 0f;
            }

            if (_isCasting)
            {
                _castTimer += dt;
                OnCastUpdate(dt);

                if (_castTimer >= _config.CastTime)
                {
                    CompleteCast();
                }
            }
        }

        /// <summary>
        /// 施法开始
        /// </summary>
        protected virtual void OnCastStart() { }

        /// <summary>
        /// 施法中更新
        /// </summary>
        protected virtual void OnCastUpdate(float dt) { }

        /// <summary>
        /// 施法完成，产生效果
        /// </summary>
        protected virtual void OnCastComplete() { }

        /// <summary>
        /// 完成施法
        /// </summary>
        private void CompleteCast()
        {
            OnCastComplete();
            _isCasting = false;
            _castTimer = 0f;
            _cooldownTimer = _config.Cooldown;
        }

        /// <summary>
        /// 打断施法
        /// </summary>
        public virtual void Interrupt()
        {
            if (_isCasting)
            {
                _isCasting = false;
                _castTimer = 0f;
                _cooldownTimer = _config.Cooldown * 0.5f; // 打断后半冷却
            }
        }

        /// <summary>
        /// 阶段进入时调用
        /// </summary>
        public virtual void OnPhaseEnter(int phaseIndex) { }
    }

    /// <summary>
    /// 冲锋技能
    /// </summary>
    public class BossSkill_Charge : BossSkillBase
    {
        private Vector3 _chargeTarget;
        private Vector3 _chargeStart;
        private float _chargeProgress = 0f;

        public BossSkill_Charge(BossController boss, BossSkillConfig config) : base(boss, config) { }

        protected override void OnCastStart()
        {
            _chargeTarget = _boss.GetTargetPosition();
            _chargeStart = _boss.MoveComp.Position;
            _chargeProgress = 0f;
        }

        protected override void OnCastUpdate(float dt)
        {
            // 冲锋移动
            _chargeProgress = _castTimer / _config.CastTime;
            Vector3 pos = Vector3.Lerp(_chargeStart, _chargeTarget, _chargeProgress);
            _boss.MoveComp.SetPosition(pos);
        }

        protected override void OnCastComplete()
        {
            // 冲锋到达造成伤害
            Debug.Log($"[BossSkill_Charge] 冲锋命中！伤害倍率: {_config.DamageMultiplier}");
            // 实际伤害由调用方处理
        }
    }

    /// <summary>
    /// 召唤技能
    /// </summary>
    public class BossSkill_Summon : BossSkillBase
    {
        public ZombieType SummonType = ZombieType.Normal;
        public int SummonCount = 3;

        public BossSkill_Summon(BossController boss, BossSkillConfig config) : base(boss, config) { }

        protected override void OnCastComplete()
        {
            Debug.Log($"[BossSkill_Summon] 召唤 {SummonCount} 只 {SummonType}");
            // 实际召唤由 ZombieSpawner 处理
        }
    }

    /// <summary>
    /// 范围毒雾技能
    /// </summary>
    public class BossSkill_PoisonCloud : BossSkillBase
    {
        public float PoisonDuration = 5f;
        public float PoisonDpsPct = 0.05f;

        public BossSkill_PoisonCloud(BossController boss, BossSkillConfig config) : base(boss, config) { }

        protected override void OnCastComplete()
        {
            Debug.Log($"[BossSkill_PoisonCloud] 释放毒雾，范围 {_config.Range}");
        }
    }

    /// <summary>
    /// 护盾技能
    /// </summary>
    public class BossSkill_Shield : BossSkillBase
    {
        public float ShieldAmount = 1000f;

        public BossSkill_Shield(BossController boss, BossSkillConfig config) : base(boss, config) { }

        protected override void OnCastComplete()
        {
            _boss.AddShield(ShieldAmount);
            Debug.Log($"[BossSkill_Shield] 获得护盾: {ShieldAmount}");
        }
    }
}
