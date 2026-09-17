using System;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;

namespace LostIsland.Logic.Awakening
{
    /// <summary>
    /// 觉醒管理器
    /// 管理觉醒等级、AXP经验、全局属性加成
    /// </summary>
    public class AwakenManager : MonoSingleton<AwakenManager>
    {
        #region 配置

        /// <summary>最高觉醒等级</summary>
        public const int MAX_AWAKENING_LEVEL = 100;

        /// <summary>每级觉醒增加的属性倍率</summary>
        public const float MULTIPLIER_PER_LEVEL = 0.025f; // 每级+2.5%

        #endregion

        #region 状态

        /// <summary>当前觉醒等级</summary>
        public int AwakeningLevel { get; private set; }

        /// <summary>当前AXP</summary>
        public long CurrentAXP { get; private set; }

        /// <summary>升级到下一级所需AXP</summary>
        public long AXPToNextLevel { get; private set; }

        /// <summary>觉醒属性倍率</summary>
        public float AwakeningMultiplier => 1f + AwakeningLevel * MULTIPLIER_PER_LEVEL;

        #endregion

        #region 升级表

        /// <summary>每级所需AXP查表</summary>
        private long[] _axpPerLevelTable;

        #endregion

        #region 事件

        /// <summary>觉醒升级事件 (newLevel)</summary>
        public event Action<int> OnAwakenLevelUp;

        /// <summary>AXP变化事件 (currentAXP, axpToNext)</summary>
        public event Action<long, long> OnAXPChanged;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            GenerateAXPTable();

            AwakeningLevel = 0;
            CurrentAXP = 0;
            AXPToNextLevel = _axpPerLevelTable[0];

            Debug.Log("[AwakenManager] 觉醒管理器初始化完成");
        }

        /// <summary>
        /// 生成AXP升级表
        /// </summary>
        private void GenerateAXPTable()
        {
            _axpPerLevelTable = new long[MAX_AWAKENING_LEVEL];

            for (int i = 0; i < MAX_AWAKENING_LEVEL; i++)
            {
                if (i < 10)
                    _axpPerLevelTable[i] = 1000;        // 1-10级：1000 AXP
                else if (i < 25)
                    _axpPerLevelTable[i] = 3000;        // 11-25级：3000 AXP
                else if (i < 50)
                    _axpPerLevelTable[i] = 6000;        // 26-50级：6000 AXP
                else if (i < 75)
                    _axpPerLevelTable[i] = 12000;       // 51-75级：12000 AXP
                else
                    _axpPerLevelTable[i] = 20000;       // 76-100级：20000 AXP
            }
        }

        #endregion

        #region AXP 操作

        /// <summary>
        /// 添加AXP
        /// </summary>
        public void AddAXP(long amount)
        {
            if (amount <= 0) return;
            if (AwakeningLevel >= MAX_AWAKENING_LEVEL) return;

            CurrentAXP += amount;

            // 检查升级
            while (CurrentAXP >= AXPToNextLevel && AwakeningLevel < MAX_AWAKENING_LEVEL)
            {
                CurrentAXP -= AXPToNextLevel;
                AwakeningLevel++;

                if (AwakeningLevel < MAX_AWAKENING_LEVEL)
                {
                    AXPToNextLevel = _axpPerLevelTable[AwakeningLevel];
                }
                else
                {
                    AXPToNextLevel = 0;
                    CurrentAXP = 0;
                }

                OnLevelUp();
            }

            OnAXPChanged?.Invoke(CurrentAXP, AXPToNextLevel);

            // 触发全局加成更新
            RefreshGlobalBonus();
        }

        /// <summary>
        /// 直接设置觉醒等级（用于调试/加载存档）
        /// </summary>
        public void SetAwakeningLevel(int level)
        {
            level = Mathf.Clamp(level, 0, MAX_AWAKENING_LEVEL);

            AwakeningLevel = level;
            CurrentAXP = 0;

            if (level < MAX_AWAKENING_LEVEL)
                AXPToNextLevel = _axpPerLevelTable[level];
            else
                AXPToNextLevel = 0;

            RefreshGlobalBonus();
        }

        /// <summary>
        /// 升级回调
        /// </summary>
        private void OnLevelUp()
        {
            Debug.Log($"[AwakenManager] 觉醒升级！Lv.{AwakeningLevel}, 倍率: {AwakeningMultiplier:F2}x");

            OnAwakenLevelUp?.Invoke(AwakeningLevel);

            EventBus.Trigger(new AwakeningLevelUpEvent
            {
                NewLevel = AwakeningLevel,
                Multiplier = AwakeningMultiplier
            });
        }

        #endregion

        #region 全局加成

        /// <summary>
        /// 刷新全局觉醒加成
        /// </summary>
        public void RefreshGlobalBonus()
        {
            float bonusPct = AwakeningMultiplier - 1f; // 额外百分比

            // 通过事件总线通知属性系统更新全局加成
            EventBus.Trigger(new AwakeningBonusRefreshedEvent
            {
                Level = AwakeningLevel,
                Multiplier = AwakeningMultiplier,
                BonusPct = bonusPct
            });
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取升级进度（0~1）
        /// </summary>
        public float LevelProgress
        {
            get
            {
                if (AwakeningLevel >= MAX_AWAKENING_LEVEL) return 1f;
                if (AXPToNextLevel <= 0) return 1f;
                return (float)CurrentAXP / AXPToNextLevel;
            }
        }

        /// <summary>
        /// 是否满级
        /// </summary>
        public bool IsMaxLevel => AwakeningLevel >= MAX_AWAKENING_LEVEL;

        /// <summary>
        /// 获取指定等级的AXP需求
        /// </summary>
        public long GetAXPRequired(int level)
        {
            if (level < 0 || level >= MAX_AWAKENING_LEVEL) return 0;
            return _axpPerLevelTable[level];
        }

        /// <summary>
        /// 计算总AXP
        /// </summary>
        public long GetTotalAXP()
        {
            long total = CurrentAXP;
            for (int i = 0; i < AwakeningLevel && i < MAX_AWAKENING_LEVEL; i++)
            {
                total += _axpPerLevelTable[i];
            }
            return total;
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            OnAwakenLevelUp = null;
            OnAXPChanged = null;
            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// 觉醒升级事件
    /// </summary>
    public struct AwakeningLevelUpEvent : IEvent
    {
        public int NewLevel;
        public float Multiplier;
    }

    /// <summary>
    /// 觉醒加成刷新事件
    /// </summary>
    public struct AwakeningBonusRefreshedEvent : IEvent
    {
        public int Level;
        public float Multiplier;
        public float BonusPct;
    }
}
