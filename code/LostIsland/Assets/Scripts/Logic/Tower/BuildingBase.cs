using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Core;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 建筑基类
    /// 所有建筑（防御类、资源类、功能类）都继承自此类
    /// </summary>
    public abstract class BuildingBase
    {
        #region 基础属性

        /// <summary>建筑配置</summary>
        public BuildingConfigSO Config { get; protected set; }

        /// <summary>建筑等级</summary>
        public int Level { get; set; }

        /// <summary>所在楼层</summary>
        public int FloorIndex { get; protected set; }

        /// <summary>所在槽位</summary>
        public int SlotIndex { get; protected set; }

        /// <summary>建筑类型</summary>
        public BuildingType BuildingType => Config != null ? Config.BuildingType : BuildingType.None;

        /// <summary>建筑分类</summary>
        public BuildingCategory Category => Config != null ? Config.Category : BuildingCategory.Utility;

        /// <summary>是否激活（供电正常）</summary>
        public bool IsActive { get; protected set; }

        #endregion

        #region 事件

        /// <summary>建筑激活状态变化</summary>
        public event Action<bool> OnActiveChanged;

        #endregion

        #region 初始化

        public BuildingBase()
        {
            IsActive = true;
        }

        /// <summary>
        /// 初始化建筑
        /// </summary>
        public virtual void Initialize(BuildingConfigSO config, int level, int floorIndex, int slotIndex)
        {
            Config = config;
            Level = level;
            FloorIndex = floorIndex;
            SlotIndex = slotIndex;
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 建造完成时调用
        /// </summary>
        public virtual void OnBuilt()
        {
            ApplyBonuses();
            Debug.Log($"[BuildingBase] {Config.BuildingName} 建造完成 (Lv.{Level})");
        }

        /// <summary>
        /// 拆除时调用
        /// </summary>
        public virtual void OnRemoved()
        {
            RemoveBonuses();
            Debug.Log($"[BuildingBase] {Config.BuildingName} 已拆除");
        }

        /// <summary>
        /// 升级前调用（移除旧等级加成）
        /// </summary>
        public virtual void OnPreUpgrade()
        {
            RemoveBonuses();
        }

        /// <summary>
        /// 升级后调用（应用新等级加成）
        /// </summary>
        public virtual void OnPostUpgrade()
        {
            ApplyBonuses();
            Debug.Log($"[BuildingBase] {Config.BuildingName} 升级到 Lv.{Level}");
        }

        /// <summary>
        /// 白天开始时调用
        /// </summary>
        public virtual void OnDayStart(int day)
        {
        }

        /// <summary>
        /// 夜晚开始时调用
        /// </summary>
        public virtual void OnNightStart(int day, int wave)
        {
        }

        /// <summary>
        /// 夜晚结束时调用
        /// </summary>
        public virtual void OnNightEnd(bool victory)
        {
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public virtual void Update(float dt)
        {
        }

        #endregion

        #region 加成管理

        /// <summary>
        /// 应用建筑加成
        /// </summary>
        protected virtual void ApplyBonuses()
        {
            if (Config == null || Config.Bonuses == null || Config.Bonuses.Count == 0)
                return;

            // 加成应用到玩家属性系统
            // 注意：这里需要属性系统支持添加/移除来源加成
            // 简化实现：通过事件总线通知
            foreach (var bonus in Config.Bonuses)
            {
                float totalValue = bonus.GetTotalValue(Level);
                EventBus.Trigger(new BuildingBonusAppliedEvent
                {
                    BuildingType = BuildingType,
                    AttrType = bonus.AttrType,
                    IsFlat = bonus.IsFlat,
                    Value = totalValue,
                    Layer = bonus.Layer,
                    IsAdding = true
                });
            }
        }

        /// <summary>
        /// 移除建筑加成
        /// </summary>
        protected virtual void RemoveBonuses()
        {
            if (Config == null || Config.Bonuses == null || Config.Bonuses.Count == 0)
                return;

            foreach (var bonus in Config.Bonuses)
            {
                float totalValue = bonus.GetTotalValue(Level);
                EventBus.Trigger(new BuildingBonusAppliedEvent
                {
                    BuildingType = BuildingType,
                    AttrType = bonus.AttrType,
                    IsFlat = bonus.IsFlat,
                    Value = totalValue,
                    Layer = bonus.Layer,
                    IsAdding = false
                });
            }
        }

        #endregion

        #region 激活状态

        /// <summary>
        /// 设置激活状态
        /// </summary>
        public virtual void SetActive(bool active)
        {
            if (IsActive == active) return;

            IsActive = active;
            OnActiveChanged?.Invoke(active);

            if (active)
            {
                ApplyBonuses();
            }
            else
            {
                RemoveBonuses();
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取当前等级的电力消耗
        /// </summary>
        public int GetPowerCost()
        {
            return Config != null ? Config.GetPowerCost(Level) : 0;
        }

        /// <summary>
        /// 获取当前等级的电力产出
        /// </summary>
        public int GetPowerOutput()
        {
            return Config != null ? Config.GetPowerOutput(Level) : 0;
        }

        /// <summary>
        /// 获取升级消耗
        /// </summary>
        public int GetUpgradeCost()
        {
            return Config != null ? Config.GetUpgradeCost(Level) : 0;
        }

        /// <summary>
        /// 是否可以升级
        /// </summary>
        public bool CanUpgrade()
        {
            return Config != null && Level < Config.MaxLevel;
        }

        #endregion
    }
}
