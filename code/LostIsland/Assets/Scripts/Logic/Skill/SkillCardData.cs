using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Skill
{
    /// <summary>
    /// 技能卡配置 ScriptableObject
    /// 所有技能卡数值配置
    /// </summary>
    [CreateAssetMenu(fileName = "SkillCard", menuName = "LostIsland/Skill/技能卡配置")]
    public class SkillCardData : ScriptableObject
    {
        [Header("基础信息")]
        public string CardId;
        public string CardName;
        public string Description;
        public Rarity Rarity;
        public SkillType SkillType;
        public SkillTargetType TargetType;

        [Header("数值")]
        [Tooltip("伤害倍率（相对于ATK）")]
        public float DamageMultiplier = 1f;

        [Tooltip("冷却时间（秒）")]
        public float Cooldown = 10f;

        [Tooltip("施放范围")]
        public float CastRange = 5f;

        [Tooltip("效果半径（AOE用）")]
        public float EffectRadius = 2f;

        [Tooltip("持续时间（0=瞬时）")]
        public float Duration = 0f;

        [Header("属性加成（被动卡）")]
        public List<SkillBonusEntry> PassiveBonuses = new List<SkillBonusEntry>();

        [Header("效果列表")]
        public List<EffectType> Effects = new List<EffectType>();

        [Tooltip("效果数值（如燃烧伤害/减速百分比等）")]
        public float EffectValue = 0f;

        [Tooltip("效果持续时间（秒）")]
        public float EffectDuration = 3f;

        [Header("升级成长")]
        public int MaxLevel = 5;

        [Tooltip("每级伤害增加比例")]
        public float DmgGrowthPerLevel = 0.15f;

        [Tooltip("每级冷却减免比例")]
        public float CdReducePerLevel = 0.05f;

        [Header("外观")]
        public Sprite Icon;
        public Color CardColor = Color.white;

        #region 计算方法

        /// <summary>
        /// 获取指定等级的伤害倍率
        /// </summary>
        public float GetDamageMultiplier(int level)
        {
            if (level <= 1) return DamageMultiplier;
            return DamageMultiplier * (1f + DmgGrowthPerLevel * (level - 1));
        }

        /// <summary>
        /// 获取指定等级的实际冷却时间
        /// </summary>
        public float GetCooldown(int level)
        {
            if (level <= 1) return Cooldown;
            float reduceAmount = Mathf.Clamp01(CdReducePerLevel * (level - 1));
            return Cooldown * (1f - reduceAmount);
        }

        /// <summary>
        /// 获取指定等级的效果值
        /// </summary>
        public float GetEffectValue(int level)
        {
            if (level <= 1) return EffectValue;
            return EffectValue * (1f + DmgGrowthPerLevel * (level - 1));
        }

        /// <summary>
        /// 获取指定等级的效果持续时间
        /// </summary>
        public float GetEffectDuration(int level)
        {
            if (level <= 1) return EffectDuration;
            return EffectDuration * (1f + 0.1f * (level - 1)); // 每级+10%持续时间
        }

        /// <summary>
        /// 检查是否是被动技能
        /// </summary>
        public bool IsPassive => SkillType == SkillType.Passive;

        /// <summary>
        /// 检查是否是主动技能
        /// </summary>
        public bool IsActive => SkillType == SkillType.Active || SkillType == SkillType.Summon || SkillType == SkillType.Trap;

        #endregion
    }

    /// <summary>
    /// 技能加成条目
    /// </summary>
    [Serializable]
    public struct SkillBonusEntry
    {
        public AttrType AttrType;
        public bool IsFlat;
        public float ValuePerLevel;
        public AttrLayer Layer;

        public float GetTotalValue(int level)
        {
            return ValuePerLevel * level;
        }
    }
}
