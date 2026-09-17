using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 塔内管理器
    /// 管理灯塔7层楼、房间槽位、建筑建造与升级
    /// </summary>
    public class TowerManager : MonoSingleton<TowerManager>
    {
        #region 楼层配置

        /// <summary>
        /// 楼层总数（B1F + 1F~5F + 塔顶 = 7层）
        /// </summary>
        public const int TOTAL_FLOORS = 7;

        /// <summary>
        /// 楼层数据列表（索引0=B1F，6=塔顶）
        /// </summary>
        private List<TowerFloorData> _floors;
        public IReadOnlyList<TowerFloorData> Floors => _floors;

        #endregion

        #region 电力系统

        private PowerSystem _powerSystem;
        public PowerSystem Power => _powerSystem;

        #endregion

        #region 事件

        /// <summary>楼层解锁事件</summary>
        public event Action<int> OnFloorUnlocked;

        /// <summary>房间解锁事件</summary>
        public event Action<int, int> OnSlotUnlocked; // floorIndex, slotIndex

        /// <summary>建筑建造事件</summary>
        public event Action<int, int, BuildingType> OnBuildingBuilt; // floorIndex, slotIndex, type

        /// <summary>建筑升级事件</summary>
        public event Action<int, int, int> OnBuildingUpgraded; // floorIndex, slotIndex, newLevel

        /// <summary>建筑拆除事件</summary>
        public event Action<int, int, BuildingType> OnBuildingRemoved; // floorIndex, slotIndex, type

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            InitializeFloors();
            InitializePowerSystem();

            Debug.Log("[TowerManager] 塔内管理器初始化完成");
        }

        /// <summary>
        /// 初始化7层楼
        /// </summary>
        private void InitializeFloors()
        {
            _floors = new List<TowerFloorData>
            {
                // B1F 地下室 - 第1天解锁 - 3个房间
                new TowerFloorData(0, "B1F 地下室", 1, 3),
                // 1F 第一层 - 第1天解锁 - 4个房间
                new TowerFloorData(1, "1F 大厅", 1, 4),
                // 2F 第二层 - 第2天解锁 - 3个房间
                new TowerFloorData(2, "2F 生活区", 2, 3),
                // 3F 第三层 - 第4天解锁 - 3个房间
                new TowerFloorData(3, "3F 工坊层", 4, 3),
                // 4F 第四层 - 第6天解锁 - 3个房间
                new TowerFloorData(4, "4F 种植层", 6, 3),
                // 5F 第五层 - 第8天解锁 - 2个房间
                new TowerFloorData(5, "5F 监控层", 8, 2),
                // 塔顶 - 第1天解锁 - 1个房间（探照灯）
                new TowerFloorData(6, "塔顶", 1, 1),
            };

            Debug.Log($"[TowerManager] 初始化 {_floors.Count} 层楼");
        }

        /// <summary>
        /// 初始化电力系统
        /// </summary>
        private void InitializePowerSystem()
        {
            _powerSystem = new PowerSystem();
            _powerSystem.Initialize(this);
        }

        #endregion

        #region 楼层管理

        /// <summary>
        /// 根据天数更新楼层解锁状态
        /// </summary>
        public void UpdateFloorUnlocks(int currentDay)
        {
            for (int i = 0; i < _floors.Count; i++)
            {
                var floor = _floors[i];
                if (!floor.IsUnlocked && currentDay >= floor.UnlockDay)
                {
                    UnlockFloor(i);
                }
            }
        }

        /// <summary>
        /// 解锁楼层
        /// </summary>
        public void UnlockFloor(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= _floors.Count) return;

            var floor = _floors[floorIndex];
            if (floor.IsUnlocked) return;

            floor.IsUnlocked = true;
            Debug.Log($"[TowerManager] 楼层解锁: {floor.FloorName}");

            // 第一个房间槽位默认解锁
            if (floor.RoomSlots.Count > 0)
            {
                floor.RoomSlots[0].IsUnlocked = true;
            }

            OnFloorUnlocked?.Invoke(floorIndex);

            EventBus.Trigger(new FloorUnlockedEvent
            {
                FloorIndex = floorIndex,
                FloorName = floor.FloorName
            });
        }

        /// <summary>
        /// 获取楼层数据
        /// </summary>
        public TowerFloorData GetFloor(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= _floors.Count) return null;
            return _floors[floorIndex];
        }

        /// <summary>
        /// 获取已解锁楼层数量
        /// </summary>
        public int UnlockedFloorCount
        {
            get
            {
                int count = 0;
                foreach (var floor in _floors)
                {
                    if (floor.IsUnlocked) count++;
                }
                return count;
            }
        }

        #endregion

        #region 房间槽位管理

        /// <summary>
        /// 获取房间槽位
        /// </summary>
        public RoomSlot GetSlot(int floorIndex, int slotIndex)
        {
            var floor = GetFloor(floorIndex);
            if (floor == null || slotIndex < 0 || slotIndex >= floor.RoomSlots.Count)
                return null;
            return floor.RoomSlots[slotIndex];
        }

        /// <summary>
        /// 解锁房间槽位（消耗废料）
        /// </summary>
        public bool UnlockSlot(int floorIndex, int slotIndex)
        {
            var slot = GetSlot(floorIndex, slotIndex);
            if (slot == null || slot.IsUnlocked) return false;

            var floor = GetFloor(floorIndex);
            if (!floor.IsUnlocked)
            {
                Debug.LogWarning($"[TowerManager] 楼层未解锁，无法解锁槽位");
                return false;
            }

            // 检查资源（这里先直接解锁，资源检查在UI层做）
            slot.IsUnlocked = true;
            Debug.Log($"[TowerManager] 房间解锁: {floor.FloorName} - 槽位{slotIndex}");

            OnSlotUnlocked?.Invoke(floorIndex, slotIndex);
            return true;
        }

        /// <summary>
        /// 获取指定楼层已解锁的槽位数
        /// </summary>
        public int GetUnlockedSlotCount(int floorIndex)
        {
            var floor = GetFloor(floorIndex);
            if (floor == null) return 0;

            int count = 0;
            foreach (var slot in floor.RoomSlots)
            {
                if (slot.IsUnlocked) count++;
            }
            return count;
        }

        /// <summary>
        /// 获取所有已解锁的槽位数量
        /// </summary>
        public int TotalUnlockedSlots
        {
            get
            {
                int count = 0;
                foreach (var floor in _floors)
                {
                    foreach (var slot in floor.RoomSlots)
                    {
                        if (slot.IsUnlocked) count++;
                    }
                }
                return count;
            }
        }

        #endregion

        #region 建筑建造

        /// <summary>
        /// 检查是否可以在指定槽位建造建筑
        /// </summary>
        public bool CanBuild(int floorIndex, int slotIndex, BuildingType type)
        {
            var slot = GetSlot(floorIndex, slotIndex);
            if (slot == null || !slot.IsUnlocked) return false;
            if (slot.HasBuilding) return false;

            // 获取建筑配置
            var config = BuildingConfigFactory.GetConfig(type);
            if (config == null) return false;

            // 检查楼层解锁要求
            if (floorIndex < config.UnlockFloor) return false;

            // 检查电力
            if (!_powerSystem.CanBuild(config)) return false;

            return true;
        }

        /// <summary>
        /// 建造建筑
        /// </summary>
        public bool Build(int floorIndex, int slotIndex, BuildingType type)
        {
            if (!CanBuild(floorIndex, slotIndex, type))
            {
                Debug.LogWarning($"[TowerManager] 无法建造: {type} at {floorIndex}F-{slotIndex}");
                return false;
            }

            var slot = GetSlot(floorIndex, slotIndex);
            var config = BuildingConfigFactory.GetConfig(type);

            // 创建建筑实例
            BuildingBase building = BuildingFactory.CreateBuilding(type);
            if (building == null)
            {
                Debug.LogError($"[TowerManager] 建筑创建失败: {type}");
                return false;
            }

            building.Initialize(config, 1, floorIndex, slotIndex);

            slot.BuiltType = type;
            slot.BuildingLevel = 1;
            slot.BuildingInstance = building;

            // 更新电力
            _powerSystem.Recalculate();

            // 应用建筑效果
            building.OnBuilt();

            Debug.Log($"[TowerManager] 建造完成: {config.BuildingName} at {floorIndex}F-{slotIndex}");

            OnBuildingBuilt?.Invoke(floorIndex, slotIndex, type);

            EventBus.Trigger(new BuildingBuiltEvent
            {
                FloorIndex = floorIndex,
                SlotIndex = slotIndex,
                BuildingType = type,
                Level = 1
            });

            return true;
        }

        /// <summary>
        /// 拆除建筑
        /// </summary>
        public bool RemoveBuilding(int floorIndex, int slotIndex)
        {
            var slot = GetSlot(floorIndex, slotIndex);
            if (slot == null || !slot.HasBuilding) return false;

            BuildingType type = slot.BuiltType;

            // 触发拆除效果
            if (slot.BuildingInstance != null)
            {
                slot.BuildingInstance.OnRemoved();
            }

            slot.BuiltType = BuildingType.None;
            slot.BuildingLevel = 0;
            slot.BuildingInstance = null;

            // 更新电力
            _powerSystem.Recalculate();

            Debug.Log($"[TowerManager] 拆除建筑: {type} at {floorIndex}F-{slotIndex}");

            OnBuildingRemoved?.Invoke(floorIndex, slotIndex, type);
            return true;
        }

        #endregion

        #region 建筑升级

        /// <summary>
        /// 检查是否可以升级
        /// </summary>
        public bool CanUpgrade(int floorIndex, int slotIndex)
        {
            var slot = GetSlot(floorIndex, slotIndex);
            if (slot == null || !slot.HasBuilding) return false;

            var config = BuildingConfigFactory.GetConfig(slot.BuiltType);
            if (config == null) return false;

            // 检查是否达到最大等级
            if (slot.BuildingLevel >= config.MaxLevel) return false;

            // 检查电力（升级可能增加电力消耗）
            int nextLevel = slot.BuildingLevel + 1;
            int powerCost = config.GetPowerCost(nextLevel);
            int currentPowerCost = config.GetPowerCost(slot.BuildingLevel);
            int extraPower = powerCost - currentPowerCost;

            if (extraPower > 0 && _powerSystem.RemainingPower < extraPower)
                return false;

            return true;
        }

        /// <summary>
        /// 获取升级消耗
        /// </summary>
        public int GetUpgradeCost(int floorIndex, int slotIndex)
        {
            var slot = GetSlot(floorIndex, slotIndex);
            if (slot == null || !slot.HasBuilding) return 0;

            var config = BuildingConfigFactory.GetConfig(slot.BuiltType);
            if (config == null) return 0;

            return config.GetUpgradeCost(slot.BuildingLevel);
        }

        /// <summary>
        /// 升级建筑
        /// </summary>
        public bool UpgradeBuilding(int floorIndex, int slotIndex)
        {
            if (!CanUpgrade(floorIndex, slotIndex))
            {
                Debug.LogWarning($"[TowerManager] 无法升级: {floorIndex}F-{slotIndex}");
                return false;
            }

            var slot = GetSlot(floorIndex, slotIndex);
            var config = BuildingConfigFactory.GetConfig(slot.BuiltType);

            // 扣除旧效果
            if (slot.BuildingInstance != null)
            {
                slot.BuildingInstance.OnPreUpgrade();
            }

            slot.BuildingLevel++;

            // 应用新效果
            if (slot.BuildingInstance != null)
            {
                slot.BuildingInstance.Level = slot.BuildingLevel;
                slot.BuildingInstance.OnPostUpgrade();
            }

            // 更新电力
            _powerSystem.Recalculate();

            Debug.Log($"[TowerManager] 升级完成: {config.BuildingName} Lv.{slot.BuildingLevel}");

            OnBuildingUpgraded?.Invoke(floorIndex, slotIndex, slot.BuildingLevel);

            EventBus.Trigger(new BuildingUpgradedEvent
            {
                FloorIndex = floorIndex,
                SlotIndex = slotIndex,
                BuildingType = slot.BuiltType,
                NewLevel = slot.BuildingLevel
            });

            return true;
        }

        #endregion

        #region 建筑查询

        /// <summary>
        /// 获取所有防御塔（用于战斗）
        /// </summary>
        public List<DefenseTowerBase> GetAllDefenseTowers()
        {
            List<DefenseTowerBase> result = new List<DefenseTowerBase>();

            foreach (var floor in _floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    if (slot.BuildingInstance is DefenseTowerBase tower)
                    {
                        result.Add(tower);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取指定类型的建筑数量
        /// </summary>
        public int GetBuildingCount(BuildingType type)
        {
            int count = 0;
            foreach (var floor in _floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    if (slot.BuiltType == type) count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取总建筑数量
        /// </summary>
        public int TotalBuildingCount
        {
            get
            {
                int count = 0;
                foreach (var floor in _floors)
                {
                    foreach (var slot in floor.RoomSlots)
                    {
                        if (slot.HasBuilding) count++;
                    }
                }
                return count;
            }
        }

        #endregion

        #region 战斗接口

        /// <summary>
        /// 白天开始时调用
        /// </summary>
        public void OnDayStart(int day)
        {
            UpdateFloorUnlocks(day);

            // 通知所有建筑白天开始
            foreach (var floor in _floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    slot.BuildingInstance?.OnDayStart(day);
                }
            }
        }

        /// <summary>
        /// 夜晚开始时调用
        /// </summary>
        public void OnNightStart(int day, int wave)
        {
            // 通知所有建筑夜晚开始
            foreach (var floor in _floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    slot.BuildingInstance?.OnNightStart(day, wave);
                }
            }
        }

        /// <summary>
        /// 夜晚结束时调用
        /// </summary>
        public void OnNightEnd(bool victory)
        {
            // 通知所有建筑夜晚结束
            foreach (var floor in _floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    slot.BuildingInstance?.OnNightEnd(victory);
                }
            }
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            // 清理所有建筑
            foreach (var floor in _floors)
            {
                foreach (var slot in floor.RoomSlots)
                {
                    slot.BuildingInstance?.OnRemoved();
                    slot.BuildingInstance = null;
                }
            }

            _powerSystem?.Dispose();
            base.OnDispose();
        }

        #endregion
    }
}
