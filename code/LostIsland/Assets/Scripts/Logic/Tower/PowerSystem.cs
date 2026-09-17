using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 电力系统
    /// 电力是建造系统的核心策略维度：建筑数量受电力限制
    /// 玩家需要在"多造低级建筑"和"少造高级建筑"之间做选择
    /// </summary>
    public class PowerSystem : IDisposable
    {
        #region 基础电力配置

        /// <summary>灯塔基础电力</summary>
        public const float BASE_POWER = 20f;

        /// <summary>塔顶探照灯电力消耗（固定）</summary>
        public const float SEARCHLIGHT_POWER_COST = 3f;

        #endregion

        #region 属性

        /// <summary>最大电力</summary>
        public float MaxPower { get; private set; }

        /// <summary>已使用电力</summary>
        public float UsedPower { get; private set; }

        /// <summary>剩余电力</summary>
        public float RemainingPower => MaxPower - UsedPower;

        /// <summary>电力使用率（0~1）</summary>
        public float UsageRatio => MaxPower > 0 ? UsedPower / MaxPower : 0f;

        /// <summary>是否电力不足</summary>
        public bool IsPowerShortage => UsedPower > MaxPower;

        #endregion

        #region 引用

        private TowerManager _towerManager;

        #endregion

        #region 事件

        /// <summary>电力变化事件（maxPower, usedPower）</summary>
        public event Action<float, float> OnPowerChanged;

        #endregion

        #region 初始化

        public void Initialize(TowerManager towerManager)
        {
            _towerManager = towerManager;
            Recalculate();
        }

        #endregion

        #region 电力计算

        /// <summary>
        /// 重新计算电力
        /// </summary>
        public void Recalculate()
        {
            if (_towerManager == null) return;

            float oldMax = MaxPower;
            float oldUsed = UsedPower;

            // 计算最大电力
            MaxPower = CalculateMaxPower();

            // 计算已使用电力
            UsedPower = CalculateUsedPower();

            // 触发事件
            if (Math.Abs(MaxPower - oldMax) > 0.001f || Math.Abs(UsedPower - oldUsed) > 0.001f)
            {
                OnPowerChanged?.Invoke(MaxPower, UsedPower);
            }
        }

        /// <summary>
        /// 计算最大电力
        /// </summary>
        private float CalculateMaxPower()
        {
            float maxPower = BASE_POWER;

            foreach (var floor in _towerManager.Floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    if (!slot.HasBuilding || slot.BuildingInstance == null) continue;

                    var config = slot.BuildingInstance.Config;
                    if (config == null) continue;

                    // 发电机提供电力
                    if (config.Category == BuildingCategory.Utility &&
                        slot.BuiltType == BuildingType.Generator)
                    {
                        maxPower += config.GetPowerOutput(slot.BuildingLevel);
                    }
                }
            }

            return maxPower;
        }

        /// <summary>
        /// 计算已使用电力
        /// </summary>
        private float CalculateUsedPower()
        {
            float usedPower = 0f;

            foreach (var floor in _towerManager.Floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    if (!slot.HasBuilding || slot.BuildingInstance == null) continue;

                    var config = slot.BuildingInstance.Config;
                    if (config == null) continue;

                    // 发电机不消耗电力（它产出电力）
                    if (slot.BuiltType == BuildingType.Generator)
                        continue;

                    usedPower += config.GetPowerCost(slot.BuildingLevel);
                }
            }

            return usedPower;
        }

        #endregion

        #region 建造检查

        /// <summary>
        /// 检查是否可以建造（电力是否足够）
        /// </summary>
        public bool CanBuild(BuildingConfigSO config)
        {
            if (config == null) return false;

            // 发电机不消耗电力，反而增加
            if (config.BuildingType == BuildingType.Generator)
                return true;

            return RemainingPower >= config.BuildPowerCost;
        }

        /// <summary>
        /// 检查建造后是否电力不足（用于提示）
        /// </summary>
        public bool WouldBeOverloaded(BuildingConfigSO config)
        {
            if (config == null) return false;
            if (config.BuildingType == BuildingType.Generator)
                return false;

            return UsedPower + config.BuildPowerCost > MaxPower;
        }

        /// <summary>
        /// 获取还能建造几个指定建筑
        /// </summary>
        public int GetMaxBuildableCount(BuildingConfigSO config)
        {
            if (config == null || config.BuildPowerCost <= 0) return int.MaxValue;
            if (config.BuildingType == BuildingType.Generator) return int.MaxValue;

            return Mathf.FloorToInt(RemainingPower / config.BuildPowerCost);
        }

        #endregion

        #region 电力详情

        /// <summary>
        /// 获取电力来源明细
        /// </summary>
        public List<PowerSourceInfo> GetPowerSources()
        {
            List<PowerSourceInfo> sources = new List<PowerSourceInfo>
            {
                new PowerSourceInfo
                {
                    Name = "灯塔基础电力",
                    Type = BuildingType.None,
                    PowerOutput = BASE_POWER,
                    Count = 1
                }
            };

            if (_towerManager == null) return sources;

            // 统计发电机
            Dictionary<BuildingType, int> generatorCount = new Dictionary<BuildingType, int>();
            Dictionary<BuildingType, float> generatorOutput = new Dictionary<BuildingType, float>();

            foreach (var floor in _towerManager.Floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    if (!slot.HasBuilding || slot.BuiltType != BuildingType.Generator) continue;

                    var config = slot.BuildingInstance?.Config;
                    if (config == null) continue;

                    if (!generatorCount.ContainsKey(slot.BuiltType))
                    {
                        generatorCount[slot.BuiltType] = 0;
                        generatorOutput[slot.BuiltType] = 0f;
                    }

                    generatorCount[slot.BuiltType]++;
                    generatorOutput[slot.BuiltType] += config.GetPowerOutput(slot.BuildingLevel);
                }
            }

            foreach (var kvp in generatorCount)
            {
                var config = BuildingConfigFactory.GetConfig(kvp.Key);
                sources.Add(new PowerSourceInfo
                {
                    Name = config?.BuildingName ?? "发电机",
                    Type = kvp.Key,
                    PowerOutput = generatorOutput[kvp.Key],
                    Count = kvp.Value
                });
            }

            return sources;
        }

        /// <summary>
        /// 获取电力消耗明细
        /// </summary>
        public List<PowerConsumerInfo> GetPowerConsumers()
        {
            List<PowerConsumerInfo> consumers = new List<PowerConsumerInfo>();

            if (_towerManager == null) return consumers;

            // 按类型统计
            Dictionary<BuildingType, int> consumerCount = new Dictionary<BuildingType, int>();
            Dictionary<BuildingType, float> consumerPower = new Dictionary<BuildingType, float>();

            foreach (var floor in _towerManager.Floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    if (!slot.HasBuilding) continue;
                    if (slot.BuiltType == BuildingType.Generator) continue;

                    var config = slot.BuildingInstance?.Config;
                    if (config == null) continue;

                    if (!consumerCount.ContainsKey(slot.BuiltType))
                    {
                        consumerCount[slot.BuiltType] = 0;
                        consumerPower[slot.BuiltType] = 0f;
                    }

                    consumerCount[slot.BuiltType]++;
                    consumerPower[slot.BuiltType] += config.GetPowerCost(slot.BuildingLevel);
                }
            }

            foreach (var kvp in consumerCount)
            {
                var config = BuildingConfigFactory.GetConfig(kvp.Key);
                consumers.Add(new PowerConsumerInfo
                {
                    Name = config?.BuildingName ?? kvp.Key.ToString(),
                    Type = kvp.Key,
                    PowerCost = consumerPower[kvp.Key],
                    Count = kvp.Value
                });
            }

            // 按电力消耗排序
            consumers.Sort((a, b) => b.PowerCost.CompareTo(a.PowerCost));

            return consumers;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            OnPowerChanged = null;
            _towerManager = null;
        }

        #endregion
    }

    /// <summary>
    /// 电力来源信息
    /// </summary>
    public struct PowerSourceInfo
    {
        public string Name;
        public BuildingType Type;
        public float PowerOutput;
        public int Count;
    }

    /// <summary>
    /// 电力消耗信息
    /// </summary>
    public struct PowerConsumerInfo
    {
        public string Name;
        public BuildingType Type;
        public float PowerCost;
        public int Count;
    }
}
