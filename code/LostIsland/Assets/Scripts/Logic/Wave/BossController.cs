using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Core;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// BOSS控制器：继承丧尸控制器，增加阶段机制和技能系统
    /// 
    /// 阶段机制：
    /// - 阶段1（100%~70% HP）：基础攻击 + 1个技能
    /// - 阶段2（70%~40% HP）：攻速提升 + 解锁第2个技能
    /// - 阶段3（40%~20% HP）：攻速再提升 + 解锁第3个技能 + 召唤小怪
    /// - 阶段4（20%~0% HP）：狂暴模式 + 全技能可用
    /// </summary>
    public class BossController : ZombieController
    {
        #region 阶段系统

        /// <summary>
        /// 当前阶段（从0开始）
        /// </summary>
        public int CurrentPhase { get; private set; } = 0;

        /// <summary>
        /// 阶段配置列表
        /// </summary>
        private List<BossPhase> _phases = new List<BossPhase>();

        /// <summary>
        /// 阶段数量
        /// </summary>
        public int PhaseCount => _phases.Count;

        #endregion

        #region 技能系统

        /// <summary>
        /// 可用技能列表
        /// </summary>
        private List<BossSkillBase> _skills = new List<BossSkillBase>();

        /// <summary>
        /// 当前正在施放的技能
        /// </summary>
        public BossSkillBase CurrentCastingSkill { get; private set; }

        #endregion

        #region BOSS属性

        /// <summary>
        /// BOSS名称
        /// </summary>
        public string BossName { get; private set; }

        /// <summary>
        /// BOSS等级
        /// </summary>
        public BossTier BossTier { get; private set; }

        /// <summary>
        /// 是否狂暴（最终阶段）
        /// </summary>
        public bool IsEnraged { get; private set; }

        #endregion

        #region 事件

        /// <summary>
        /// 阶段转换事件：旧阶段，新阶段
        /// </summary>
        public event Action<int, int> OnPhaseChanged;

        /// <summary>
        /// 技能开始施放事件
        /// </summary>
        public event Action<BossSkillBase> OnSkillCastStart;

        /// <summary>
        /// 技能施放完成事件
        /// </summary>
        public event Action<BossSkillBase> OnSkillCastComplete;

        #endregion

        #region 构造与初始化

        public BossController(EntityBase entity, ZombieConfigSO config, string bossName, BossTier tier)
            : base(entity, config)
        {
            BossName = bossName;
            BossTier = tier;
            CurrentPhase = 0;
            IsEnraged = false;

            // 监听HP变化以检测阶段转换
            entity.Health.OnHPPctChanged += OnHPPctChanged;
        }

        /// <summary>
        /// 初始化阶段配置
        /// </summary>
        public void InitPhases(List<BossPhase> phases)
        {
            _phases = phases ?? new List<BossPhase>();
            CurrentPhase = 0;
        }

        /// <summary>
        /// 添加技能
        /// </summary>
        public void AddSkill(BossSkillBase skill)
        {
            if (skill != null && !_skills.Contains(skill))
            {
                _skills.Add(skill);
            }
        }

        #endregion

        #region 阶段转换

        /// <summary>
        /// HP百分比变化时检测阶段转换
        /// </summary>
        private void OnHPPctChanged(float oldPct, float newPct)
        {
            CheckPhaseTransition(newPct);
        }

        /// <summary>
        /// 检查是否需要转换阶段
        /// </summary>
        private void CheckPhaseTransition(float hpPct)
        {
            if (_phases == null || _phases.Count == 0) return;

            // 找到当前应该处于的阶段
            int targetPhase = 0;
            for (int i = _phases.Count - 1; i >= 0; i--)
            {
                if (hpPct <= _phases[i].HpThreshold)
                {
                    targetPhase = i + 1;
                    break;
                }
            }

            // 确保阶段不超过最大
            targetPhase = Mathf.Clamp(targetPhase, 0, _phases.Count);

            if (targetPhase != CurrentPhase && targetPhase > CurrentPhase)
            {
                EnterPhase(targetPhase);
            }
        }

        /// <summary>
        /// 进入新阶段
        /// </summary>
        private void EnterPhase(int phaseIndex)
        {
            int oldPhase = CurrentPhase;
            CurrentPhase = phaseIndex;

            Debug.Log($"[BossController] {BossName} 进入阶段 {phaseIndex + 1}");

            // 应用阶段加成
            if (phaseIndex > 0 && phaseIndex <= _phases.Count)
            {
                var phase = _phases[phaseIndex - 1];

                // 攻击倍率（通过临时加成实现，简化版）
                if (phase.AtkMultiplier != 1f)
                {
                    // 这里可以通过属性系统的加成来实现
                    Debug.Log($"  攻击倍率: {phase.AtkMultiplier}x");
                }

                // 阶段技能
                if (phase.PhaseSkills != null)
                {
                    Debug.Log($"  解锁技能: {phase.PhaseSkills.Count}个");
                }
            }

            // 最终阶段狂暴
            if (phaseIndex >= _phases.Count)
            {
                IsEnraged = true;
                Debug.Log("  BOSS狂暴！");
            }

            // 触发事件
            OnPhaseChanged?.Invoke(oldPhase, CurrentPhase);

            // 触发事件总线事件
            EventBus.Trigger(new BossPhaseChangedEvent
            {
                BossName = BossName,
                OldPhase = oldPhase,
                NewPhase = CurrentPhase,
                TotalPhases = _phases.Count + 1
            });
        }

        #endregion

        #region 技能管理

        /// <summary>
        /// 尝试自动施放技能
        /// </summary>
        public void TryAutoCastSkills()
        {
            if (IsDead) return;
            if (CurrentCastingSkill != null) return; // 正在施放中

            // 找一个可用的技能
            foreach (var skill in _skills)
            {
                if (skill.IsReady)
                {
                    if (skill.TryCast())
                    {
                        CurrentCastingSkill = skill;
                        OnSkillCastStart?.Invoke(skill);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 打断当前施法
        /// </summary>
        public void InterruptCurrentSkill()
        {
            if (CurrentCastingSkill != null)
            {
                CurrentCastingSkill.Interrupt();
                CurrentCastingSkill = null;
            }
        }

        #endregion

        #region 护盾系统

        /// <summary>
        /// 添加护盾
        /// </summary>
        public void AddShield(float amount)
        {
            ShieldHP += amount;
            MaxShieldHP = Mathf.Max(MaxShieldHP, ShieldHP);
            Debug.Log($"[BossController] 获得护盾: {amount:F0}, 当前: {ShieldHP:F0}");
        }

        #endregion

        #region 更新

        /// <summary>
        /// 每帧更新
        /// </summary>
        public new void Update(float dt)
        {
            base.Update(dt);

            if (IsDead) return;

            // 更新所有技能
            foreach (var skill in _skills)
            {
                skill.Update(dt);
            }

            // 检查当前技能是否施放完成
            if (CurrentCastingSkill != null && !CurrentCastingSkill.IsCasting)
            {
                OnSkillCastComplete?.Invoke(CurrentCastingSkill);
                CurrentCastingSkill = null;
            }

            // 自动施放技能（AI决策）
            if (CurrentCastingSkill == null)
            {
                TryAutoCastSkills();
            }
        }

        #endregion

        #region 重置

        /// <summary>
        /// 重置BOSS
        /// </summary>
        public new void Reset()
        {
            base.Reset();
            CurrentPhase = 0;
            IsEnraged = false;
            CurrentCastingSkill = null;
            _skills.Clear();
            _phases.Clear();
        }

        #endregion
    }

    /// <summary>
    /// BOSS阶段变化事件
    /// </summary>
    public struct BossPhaseChangedEvent : IEvent
    {
        public string BossName;
        public int OldPhase;
        public int NewPhase;
        public int TotalPhases;
    }
}
