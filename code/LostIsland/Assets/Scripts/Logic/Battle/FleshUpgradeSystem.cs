using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;

namespace LostIsland.Logic.Battle
{
    /// <summary>
    /// 腐肉升级类型
    /// </summary>
    public enum FleshUpgradeType
    {
        ATK = 0,        // 攻击力升级
        MaxHP = 1,      // 生命值升级
        AttackSpeed = 2,// 攻击速度升级
        DEF = 3,        // 防御升级
        CritRate = 4,   // 暴击率升级
        MoveSpeed = 5,  // 移动速度升级
    }

    /// <summary>
    /// 腐肉升级系统：战斗内临时成长系统
    /// 击杀丧尸获得腐肉，消耗腐肉升级属性
    /// 升级效果作用于 Pct_Flesh 层（战斗内临时加成）
    /// </summary>
    public class FleshUpgradeSystem
    {
        #region 配置常量

        /// <summary>
        /// 基础升级消耗
        /// </summary>
        public const int BASE_COST = 10;

        /// <summary>
        /// 每级消耗递增比例
        /// </summary>
        public const float COST_GROWTH_RATE = 1.2f;

        /// <summary>
        /// 每级属性提升百分比
        /// </summary>
        public const float UPGRADE_PCT_PER_LEVEL = 0.1f; // 10%

        /// <summary>
        /// 最大升级等级
        /// </summary>
        public const int MAX_LEVEL = 50;

        #endregion

        #region 数据

        private AttributeComponent _playerAttribute;

        // 各类型的当前等级
        private Dictionary<FleshUpgradeType, int> _upgradeLevels = new Dictionary<FleshUpgradeType, int>();

        // 各类型的来源ID前缀（用于属性系统）
        private const string SOURCE_PREFIX = "flesh_upgrade_";

        #endregion

        #region 属性

        /// <summary>
        /// 获取指定升级类型的当前等级
        /// </summary>
        public int GetLevel(FleshUpgradeType type)
        {
            if (_upgradeLevels.TryGetValue(type, out int level))
                return level;
            return 0;
        }

        /// <summary>
        /// 获取升级消耗
        /// </summary>
        public int GetUpgradeCost(FleshUpgradeType type)
        {
            int currentLevel = GetLevel(type);
            return CalculateCost(currentLevel);
        }

        /// <summary>
        /// 是否可以升级
        /// </summary>
        public bool CanUpgrade(FleshUpgradeType type, int currentFlesh)
        {
            int level = GetLevel(type);
            if (level >= MAX_LEVEL) return false;

            int cost = GetUpgradeCost(type);
            return currentFlesh >= cost;
        }

        /// <summary>
        /// 获取当前等级对应的加成百分比
        /// </summary>
        public float GetUpgradePct(FleshUpgradeType type)
        {
            return GetLevel(type) * UPGRADE_PCT_PER_LEVEL;
        }

        #endregion

        #region 初始化

        public FleshUpgradeSystem(AttributeComponent playerAttribute)
        {
            _playerAttribute = playerAttribute;

            // 初始化所有升级类型为0级
            foreach (FleshUpgradeType type in Enum.GetValues(typeof(FleshUpgradeType)))
            {
                _upgradeLevels[type] = 0;
            }
        }

        /// <summary>
        /// 计算升级消耗
        /// </summary>
        /// <param name="currentLevel">当前等级</param>
        /// <returns>消耗腐肉数量</returns>
        public static int CalculateCost(int currentLevel)
        {
            if (currentLevel >= MAX_LEVEL) return int.MaxValue;

            float cost = BASE_COST * Mathf.Pow(COST_GROWTH_RATE, currentLevel);
            return Mathf.CeilToInt(cost);
        }

        /// <summary>
        /// 升级类型对应的属性类型
        /// </summary>
        public static AttrType GetAttrType(FleshUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case FleshUpgradeType.ATK: return AttrType.ATK;
                case FleshUpgradeType.MaxHP: return AttrType.MaxHP;
                case FleshUpgradeType.AttackSpeed: return AttrType.AttackSpeed;
                case FleshUpgradeType.DEF: return AttrType.DEF;
                case FleshUpgradeType.CritRate: return AttrType.CritRate;
                case FleshUpgradeType.MoveSpeed: return AttrType.MoveSpeed;
                default: return AttrType.Max;
            }
        }

        #endregion

        #region 升级操作

        /// <summary>
        /// 尝试升级
        /// </summary>
        /// <param name="type">升级类型</param>
        /// <param name="currentFlesh">当前腐肉数量（引用，会扣除消耗）</param>
        /// <returns>是否升级成功</returns>
        public bool TryUpgrade(FleshUpgradeType type, ref int currentFlesh)
        {
            int level = GetLevel(type);
            if (level >= MAX_LEVEL) return false;

            int cost = GetUpgradeCost(type);
            if (currentFlesh < cost) return false;

            // 扣除腐肉
            currentFlesh -= cost;

            // 提升等级
            _upgradeLevels[type] = level + 1;

            // 应用到属性系统
            ApplyUpgradeToAttribute(type);

            Debug.Log($"[FleshUpgradeSystem] 升级 {type}: Lv.{level} -> Lv.{level + 1}, 消耗 {cost} 腐肉");

            return true;
        }

        /// <summary>
        /// 将升级效果应用到属性系统
        /// </summary>
        private void ApplyUpgradeToAttribute(FleshUpgradeType type)
        {
            AttrType attrType = GetAttrType(type);
            if (attrType == AttrType.Max) return;

            int level = GetLevel(type);
            float pct = level * UPGRADE_PCT_PER_LEVEL;
            string sourceId = SOURCE_PREFIX + type.ToString();

            _playerAttribute.AddPct(sourceId, attrType, pct, AttrLayer.Pct_Flesh);
        }

        #endregion

        #region 重置

        /// <summary>
        /// 重置所有升级（战斗结束时调用）
        /// </summary>
        public void ResetAll()
        {
            foreach (FleshUpgradeType type in Enum.GetValues(typeof(FleshUpgradeType)))
            {
                string sourceId = SOURCE_PREFIX + type.ToString();
                _playerAttribute.RemovePctAll(sourceId);
                _upgradeLevels[type] = 0;
            }

            Debug.Log("[FleshUpgradeSystem] 所有腐肉升级已重置");
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取升级类型的显示名称
        /// </summary>
        public static string GetUpgradeName(FleshUpgradeType type)
        {
            switch (type)
            {
                case FleshUpgradeType.ATK: return "攻击强化";
                case FleshUpgradeType.MaxHP: return "生命强化";
                case FleshUpgradeType.AttackSpeed: return "攻速强化";
                case FleshUpgradeType.DEF: return "防御强化";
                case FleshUpgradeType.CritRate: return "暴击强化";
                case FleshUpgradeType.MoveSpeed: return "移速强化";
                default: return type.ToString();
            }
        }

        /// <summary>
        /// 输出当前升级状态
        /// </summary>
        public void DebugDump()
        {
            Debug.Log("=== 腐肉升级状态 ===");
            foreach (FleshUpgradeType type in Enum.GetValues(typeof(FleshUpgradeType)))
            {
                int level = GetLevel(type);
                int cost = GetUpgradeCost(type);
                float pct = GetUpgradePct(type);
                Debug.Log($"{GetUpgradeName(type)}: Lv.{level}, +{pct:P0}, 下级消耗: {cost}");
            }
        }

        #endregion
    }
}
