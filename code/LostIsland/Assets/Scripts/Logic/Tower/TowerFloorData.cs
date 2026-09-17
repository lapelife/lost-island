using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 楼层数据
    /// </summary>
    [Serializable]
    public class TowerFloorData
    {
        /// <summary>楼层索引（0=B1F地下室，1=1F，...，6=塔顶）</summary>
        public int FloorIndex;

        /// <summary>楼层名称</summary>
        public string FloorName;

        /// <summary>是否已解锁</summary>
        public bool IsUnlocked;

        /// <summary>解锁所需天数</summary>
        public int UnlockDay;

        /// <summary>该层的房间槽位</summary>
        public List<RoomSlot> RoomSlots;

        public TowerFloorData(int index, string name, int unlockDay, int slotCount)
        {
            FloorIndex = index;
            FloorName = name;
            UnlockDay = unlockDay;
            IsUnlocked = false;
            RoomSlots = new List<RoomSlot>();

            for (int i = 0; i < slotCount; i++)
            {
                int slotId = index * 10 + i;
                RoomSlots.Add(new RoomSlot(slotId, i));
            }
        }
    }

    /// <summary>
    /// 房间槽位
    /// </summary>
    [Serializable]
    public class RoomSlot
    {
        /// <summary>槽位唯一ID</summary>
        public int SlotId;

        /// <summary>槽位在楼层中的索引</summary>
        public int SlotIndex;

        /// <summary>是否已解锁</summary>
        public bool IsUnlocked;

        /// <summary>解锁消耗废料</summary>
        public int UnlockCost;

        /// <summary>已建造的建筑类型（None=空）</summary>
        public BuildingType BuiltType;

        /// <summary>建筑等级（0=未建造）</summary>
        public int BuildingLevel;

        /// <summary>建筑实例引用</summary>
        [NonSerialized]
        public BuildingBase BuildingInstance;

        public RoomSlot(int slotId, int slotIndex)
        {
            SlotId = slotId;
            SlotIndex = slotIndex;
            IsUnlocked = false;
            UnlockCost = GetUnlockCost(slotIndex);
            BuiltType = BuildingType.None;
            BuildingLevel = 0;
            BuildingInstance = null;
        }

        /// <summary>
        /// 获取解锁消耗（每个槽位递增）
        /// </summary>
        private int GetUnlockCost(int slotIndex)
        {
            // 第一个槽位免费，后续递增
            return slotIndex == 0 ? 0 : 50 + slotIndex * 30;
        }

        /// <summary>
        /// 是否为空（未建造）
        /// </summary>
        public bool IsEmpty => BuiltType == BuildingType.None;

        /// <summary>
        /// 是否有建筑
        /// </summary>
        public bool HasBuilding => BuiltType != BuildingType.None;
    }
}
