using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 建筑加成管理器
    /// 汇总所有建筑的属性加成，统一应用到玩家属性系统
    /// 采用事件驱动：建筑建造/升级/拆除时触发加成更新
    /// </summary>
    public class BuildingBonusManager : MonoSingleton<BuildingBonusManager>
    {
        #region 加成数据

        /// <summary>
        /// 所有建筑加成的汇总（按属性类型+层分类）
        /// </summary>
        private Dictionary<AttrType, Dictionary<AttrLayer, float>> _bonusMap
            = new Dictionary<AttrType, Dictionary<AttrLayer, float>>();

        /// <summary>
        /// 建筑加成来源追踪（用于调试和显示）
        /// </summary>
        private Dictionary<BuildingType, List<BuildingBonusEntry>> _buildingBonuses
            = new Dictionary<BuildingType, List<BuildingBonusEntry>>();

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            // 监听建筑事件
            EventBus.Subscribe<BuildingBuiltEvent>(OnBuildingBuilt);
            EventBus.Subscribe<BuildingUpgradedEvent>(OnBuildingUpgraded);
            EventBus.Subscribe<BuildingBonusAppliedEvent>(OnBuildingBonusApplied);

            Debug.Log("[BuildingBonusManager] 建筑加成管理器初始化完成");
        }

        #endregion

        #region 事件处理

        private void OnBuildingBuilt(BuildingBuiltEvent e)
        {
            // 建筑建造时的加成已通过 BuildingBonusAppliedEvent 处理
        }

        private void OnBuildingUpgraded(BuildingUpgradedEvent e)
        {
            // 升级时的加成已通过 BuildingBonusAppliedEvent 处理
        }

        private void OnBuildingBonusApplied(BuildingBonusAppliedEvent e)
        {
            if (e.IsAdding)
            {
                AddBonus(e.AttrType, e.Layer, e.Value);
            }
            else
            {
                RemoveBonus(e.AttrType, e.Layer, e.Value);
            }
        }

        #endregion

        #region 加成操作

        /// <summary>
        /// 添加加成
        /// </summary>
        public void AddBonus(AttrType attrType, AttrLayer layer, float value)
        {
            if (!_bonusMap.ContainsKey(attrType))
            {
                _bonusMap[attrType] = new Dictionary<AttrLayer, float>();
            }

            if (!_bonusMap[attrType].ContainsKey(layer))
            {
                _bonusMap[attrType][layer] = 0f;
            }

            _bonusMap[attrType][layer] += value;

            // 通知属性系统更新
            NotifyAttributeSystem(attrType);
        }

        /// <summary>
        /// 移除加成
        /// </summary>
        public void RemoveBonus(AttrType attrType, AttrLayer layer, float value)
        {
            if (!_bonusMap.ContainsKey(attrType)) return;
            if (!_bonusMap[attrType].ContainsKey(layer)) return;

            _bonusMap[attrType][layer] -= value;

            // 通知属性系统更新
            NotifyAttributeSystem(attrType);
        }

        /// <summary>
        /// 获取指定属性和层的总加成
        /// </summary>
        public float GetTotalBonus(AttrType attrType, AttrLayer layer)
        {
            if (_bonusMap.TryGetValue(attrType, out var layerMap))
            {
                if (layerMap.TryGetValue(layer, out float value))
                {
                    return value;
                }
            }
            return 0f;
        }

        /// <summary>
        /// 获取指定属性的百分比加成（Tower层）
        /// </summary>
        public float GetPctTowerBonus(AttrType attrType)
        {
            return GetTotalBonus(attrType, AttrLayer.Pct_Tower);
        }

        /// <summary>
        /// 获取指定属性的固定值加成（Tower层）
        /// </summary>
        public float GetFlatTowerBonus(AttrType attrType)
        {
            return GetTotalBonus(attrType, AttrLayer.Flat_Tower);
        }

        #endregion

        #region 属性系统集成

        /// <summary>
        /// 通知属性系统更新
        /// </summary>
        private void NotifyAttributeSystem(AttrType attrType)
        {
            // 这里可以调用 AttributeSystem 来更新玩家属性
            // 由于属性系统在 P2 已经实现，我们通过事件通知
            EventBus.Trigger(new TowerBonusChangedEvent
            {
                AttrType = attrType,
                PctBonus = GetTotalBonus(attrType, AttrLayer.Pct_Tower),
                FlatBonus = GetTotalBonus(attrType, AttrLayer.Flat_Tower)
            });
        }

        /// <summary>
        /// 重新计算所有建筑加成（全量刷新）
        /// </summary>
        public void RecalculateAllBonuses()
        {
            // 清空
            _bonusMap.Clear();
            _buildingBonuses.Clear();

            // 遍历所有建筑重新累加
            var towerMgr = TowerManager.Instance;
            if (towerMgr == null) return;

            foreach (var floor in towerMgr.Floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    if (slot.BuildingInstance == null) continue;
                    if (!slot.BuildingInstance.IsActive) continue;

                    var config = slot.BuildingInstance.Config;
                    if (config == null || config.Bonuses == null) continue;

                    foreach (var bonus in config.Bonuses)
                    {
                        float totalValue = bonus.GetTotalValue(slot.BuildingLevel);
                        AddBonus(bonus.AttrType, bonus.Layer, totalValue);
                    }
                }
            }

            Debug.Log($"[BuildingBonusManager] 全量刷新加成完成");
        }

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取加成统计信息（用于UI显示）
        /// </summary>
        public Dictionary<AttrType, float> GetAllPctTowerBonuses()
        {
            Dictionary<AttrType, float> result = new Dictionary<AttrType, float>();

            foreach (var kvp in _bonusMap)
            {
                if (kvp.Value.TryGetValue(AttrLayer.Pct_Tower, out float value))
                {
                    result[kvp.Key] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// 打印当前所有加成（调试用）
        /// </summary>
        public void PrintAllBonuses()
        {
            Debug.Log("===== 建筑加成汇总 =====");
            foreach (var attrKvp in _bonusMap)
            {
                foreach (var layerKvp in attrKvp.Value)
                {
                    if (layerKvp.Value != 0f)
                    {
                        Debug.Log($"{attrKvp.Key} [{layerKvp.Key}]: {layerKvp.Value:F2}");
                    }
                }
            }
            Debug.Log("========================");
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            EventBus.Unsubscribe<BuildingBuiltEvent>(OnBuildingBuilt);
            EventBus.Unsubscribe<BuildingUpgradedEvent>(OnBuildingUpgraded);
            EventBus.Unsubscribe<BuildingBonusAppliedEvent>(OnBuildingBonusApplied);

            _bonusMap.Clear();
            _buildingBonuses.Clear();

            base.OnDispose();
        }

        #endregion
    }
}
