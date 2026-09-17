using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;
using LostIsland.Logic.Wave;

namespace LostIsland.Logic.Skill
{
    /// <summary>
    /// 技能卡槽
    /// 持有技能卡数据 + 等级 + 冷却管理
    /// </summary>
    [Serializable]
    public class SkillCardSlot
    {
        /// <summary>槽位索引</summary>
        public int SlotIndex;

        /// <summary>技能卡数据</summary>
        public SkillCardData CardData;

        /// <summary>技能等级</summary>
        public int Level;

        /// <summary>当前冷却时间</summary>
        public float CurrentCD { get; private set; }

        /// <summary>是否就绪</summary>
        public bool IsReady => CurrentCD <= 0f;

        /// <summary>是否空槽</summary>
        public bool IsEmpty => CardData == null;

        /// <summary>实际冷却时间（考虑等级冷却减免）</summary>
        public float ActualCD
        {
            get
            {
                if (CardData == null) return 0f;
                return CardData.GetCooldown(Level);
            }
        }

        /// <summary>冷却进度（0~1，1=就绪）</summary>
        public float CDProgress
        {
            get
            {
                if (CardData == null || ActualCD <= 0f) return 1f;
                return 1f - (CurrentCD / ActualCD);
            }
        }

        public SkillCardSlot(int slotIndex)
        {
            SlotIndex = slotIndex;
            Level = 0;
            CurrentCD = 0f;
        }

        /// <summary>
        /// 装配技能卡
        /// </summary>
        public void Equip(SkillCardData data, int level = 1)
        {
            CardData = data;
            Level = level;
            CurrentCD = 0f;
        }

        /// <summary>
        /// 卸下技能卡
        /// </summary>
        public void Unequip()
        {
            CardData = null;
            Level = 0;
            CurrentCD = 0f;
        }

        /// <summary>
        /// 更新冷却
        /// </summary>
        public void Tick(float dt)
        {
            if (CurrentCD > 0f)
            {
                CurrentCD = Mathf.Max(0f, CurrentCD - dt);
            }
        }

        /// <summary>
        /// 尝试释放技能
        /// </summary>
        public bool TryCast()
        {
            if (IsEmpty || !IsReady) return false;
            if (CardData.IsPassive) return false;

            CurrentCD = ActualCD;
            return true;
        }

        /// <summary>
        /// 重置冷却
        /// </summary>
        public void ResetCD()
        {
            CurrentCD = 0f;
        }

        /// <summary>
        /// 增加冷却时间（如被沉默）
        /// </summary>
        public void AddCD(float seconds)
        {
            CurrentCD += seconds;
        }

        /// <summary>
        /// 减少冷却时间
        /// </summary>
        public void ReduceCD(float seconds)
        {
            CurrentCD = Mathf.Max(0f, CurrentCD - seconds);
        }
    }
}
