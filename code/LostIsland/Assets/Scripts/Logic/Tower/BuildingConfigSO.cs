using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 建筑配置 ScriptableObject
    /// 所有建筑数值都在这里配置
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "LostIsland/Building/建筑配置")]
    public class BuildingConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        public string BuildingId;
        public string BuildingName;
        public string Description;
        public BuildingType BuildingType;
        public BuildingCategory Category;

        [Header("解锁条件")]
        [Tooltip("解锁楼层（0=B1F）")]
        public int UnlockFloor = 0;
        [Tooltip("解锁天数")]
        public int UnlockDay = 1;

        [Header("等级")]
        public int MaxLevel = 5;

        [Header("建造消耗")]
        [Tooltip("基础建造消耗（废料）")]
        public int BuildCostScrap = 50;
        [Tooltip("建造消耗增长系数（每级+20%）")]
        public float BuildCostGrowth = 0.2f;
        [Tooltip("电力消耗")]
        public int BuildPowerCost = 2;
        [Tooltip("电力消耗每级增长")]
        public int PowerCostPerLevel = 0;

        [Header("电力产出（仅发电机）")]
        public int BasePowerOutput = 0;
        public int PowerOutputPerLevel = 5;

        [Header("属性加成")]
        public List<BuildingBonusEntry> Bonuses = new List<BuildingBonusEntry>();

        [Header("防御塔专属（防御类）")]
        public float BaseDamage = 10f;
        public float DamagePerLevel = 5f;
        public float BaseAttackSpeed = 1f;       // 每秒攻击次数
        public float AttackSpeedPerLevel = 0.1f;
        public float BaseRange = 5f;
        public float RangePerLevel = 0.5f;
        public TargetingStrategy Targeting = TargetingStrategy.Nearest;
        public AttackType AttackType = AttackType.SingleShot;

        [Header("AOE/穿透/链式参数")]
        public float AoeRadius = 2f;
        public int PierceCount = 3;
        public int ChainCount = 3;
        public float ChainRange = 3f;

        [Header("资源类专属")]
        [Tooltip("每秒产出（废料/食物等）")]
        public float BaseProduction = 0f;
        public float ProductionPerLevel = 0f;

        [Header("外观")]
        public Sprite Icon;
        public Color BuildingColor = Color.white;

        #region 计算方法

        /// <summary>
        /// 获取指定等级的升级消耗
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            if (currentLevel >= MaxLevel) return 0;
            float growth = Mathf.Pow(1f + BuildCostGrowth, currentLevel);
            return Mathf.RoundToInt(BuildCostScrap * growth);
        }

        /// <summary>
        /// 获取指定等级的电力消耗
        /// </summary>
        public int GetPowerCost(int level)
        {
            if (level <= 0) return 0;
            return BuildPowerCost + PowerCostPerLevel * (level - 1);
        }

        /// <summary>
        /// 获取指定等级的电力产出（仅发电机）
        /// </summary>
        public int GetPowerOutput(int level)
        {
            if (level <= 0) return 0;
            return BasePowerOutput + PowerOutputPerLevel * (level - 1);
        }

        /// <summary>
        /// 获取指定等级的伤害
        /// </summary>
        public float GetDamage(int level)
        {
            if (level <= 0) return 0f;
            return BaseDamage + DamagePerLevel * (level - 1);
        }

        /// <summary>
        /// 获取指定等级的攻速
        /// </summary>
        public float GetAttackSpeed(int level)
        {
            if (level <= 0) return 0f;
            return BaseAttackSpeed + AttackSpeedPerLevel * (level - 1);
        }

        /// <summary>
        /// 获取指定等级的射程
        /// </summary>
        public float GetRange(int level)
        {
            if (level <= 0) return 0f;
            return BaseRange + RangePerLevel * (level - 1);
        }

        /// <summary>
        /// 获取指定等级的产出
        /// </summary>
        public float GetProduction(int level)
        {
            if (level <= 0) return 0f;
            return BaseProduction + ProductionPerLevel * (level - 1);
        }

        /// <summary>
        /// 获取指定等级的总建造消耗（从1级升到level级的总消耗）
        /// </summary>
        public int GetTotalCost(int level)
        {
            int total = 0;
            for (int i = 0; i < level; i++)
            {
                total += GetUpgradeCost(i);
            }
            return total;
        }

        #endregion
    }

    /// <summary>
    /// 建筑加成条目
    /// </summary>
    [Serializable]
    public struct BuildingBonusEntry
    {
        public AttrType AttrType;
        public bool IsFlat;           // true=固定值, false=百分比
        public float ValuePerLevel;   // 每级加成值
        public AttrLayer Layer;       // 加成层

        /// <summary>
        /// 获取指定等级的总加成
        /// </summary>
        public float GetTotalValue(int level)
        {
            return ValuePerLevel * level;
        }
    }
}
