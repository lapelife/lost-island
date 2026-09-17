using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;
using LostIsland.Logic.Tower;

namespace LostIsland.Logic.Rune
{
    /// <summary>
    /// 符文槽位
    /// </summary>
    [Serializable]
    public class RuneSlot
    {
        /// <summary>槽位索引</summary>
        public int SlotIndex;

        /// <summary>槽位类型</summary>
        public RuneSlotType SlotType;

        /// <summary>是否已解锁</summary>
        public bool IsUnlocked;

        /// <summary>装备的符文实例</summary>
        public RuneInstance EquippedRune;

        public RuneSlot(int index, RuneSlotType type, bool unlocked = true)
        {
            SlotIndex = index;
            SlotType = type;
            IsUnlocked = unlocked;
            EquippedRune = null;
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => EquippedRune == null;
    }

    /// <summary>
    /// 符文管理器
    /// 管理符文的获取、装备、升星、效果计算
    /// </summary>
    public class RuneManager : MonoSingleton<RuneManager>
    {
        #region 槽位配置

        /// <summary>玩家符文槽位数</summary>
        public const int PLAYER_SLOT_COUNT = 6;

        /// <summary>玩家符文槽位</summary>
        private RuneSlot[] _playerSlots;
        public IReadOnlyList<RuneSlot> PlayerSlots => _playerSlots;

        #endregion

        #region 符文收藏

        /// <summary>玩家拥有的所有符文实例</summary>
        private List<RuneInstance> _ownedRunes = new List<RuneInstance>();

        /// <summary>碎片背包（按符文ID存储碎片数）</summary>
        private Dictionary<string, int> _shardInventory = new Dictionary<string, int>();

        /// <summary>实例ID计数器</summary>
        private int _instanceIdCounter = 10000;

        #endregion

        #region 抽卡保底

        /// <summary>距离上次出蓝的次数</summary>
        private int _pityRareCounter = 0;

        /// <summary>距离上次出紫的次数</summary>
        private int _pityEpicCounter = 0;

        /// <summary>距离上次出金的次数</summary>
        private int _pityLegendaryCounter = 0;

        /// <summary>连续抽到同类型同品质的次数</summary>
        private int _duplicateCounter = 0;
        private Rarity _lastRarity = Rarity.Common;
        private string _lastRuneId = "";

        #endregion

        #region 事件

        /// <summary>符文装备变化事件</summary>
        public event Action OnEquipChanged;

        /// <summary>获得新符文事件</summary>
        public event Action<RuneInstance> OnRuneGained;

        /// <summary>符文升星事件</summary>
        public event Action<RuneInstance, int> OnRuneStarUp; // instance, newStar

        /// <summary>抽卡结果事件</summary>
        public event Action<List<RuneInstance>> OnGachaResult;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            InitializeSlots();

            Debug.Log("[RuneManager] 符文管理器初始化完成");
        }

        /// <summary>
        /// 初始化玩家符文槽位（6个：2攻击+2防御+1生存+1特殊）
        /// </summary>
        private void InitializeSlots()
        {
            _playerSlots = new RuneSlot[PLAYER_SLOT_COUNT];

            // 2个攻击槽
            _playerSlots[0] = new RuneSlot(0, RuneSlotType.Attack, true);
            _playerSlots[1] = new RuneSlot(1, RuneSlotType.Attack, true);

            // 2个防御槽
            _playerSlots[2] = new RuneSlot(2, RuneSlotType.Defense, true);
            _playerSlots[3] = new RuneSlot(3, RuneSlotType.Defense, true);

            // 1个生存槽
            _playerSlots[4] = new RuneSlot(4, RuneSlotType.Survival, true);

            // 1个特殊槽
            _playerSlots[5] = new RuneSlot(5, RuneSlotType.Special, true);
        }

        #endregion

        #region 符文装备

        /// <summary>
        /// 装备符文到指定槽位
        /// </summary>
        public bool EquipRune(int slotIndex, RuneInstance rune)
        {
            if (slotIndex < 0 || slotIndex >= PLAYER_SLOT_COUNT) return false;
            if (rune == null || rune.Data == null) return false;

            var slot = _playerSlots[slotIndex];
            if (!slot.IsUnlocked) return false;

            // 检查槽位类型匹配
            if (!IsCategoryMatch(slot.SlotType, rune.Data.Category))
            {
                Debug.LogWarning($"[RuneManager] 槽位类型不匹配: {slot.SlotType} vs {rune.Data.Category}");
                return false;
            }

            // 如果槽位已有符文，先卸下
            if (slot.EquippedRune != null)
            {
                UnequipRune(slotIndex);
            }

            // 如果符文已在其他槽位装备，先卸下
            if (rune.IsEquipped && rune.EquippedSlotIndex >= 0)
            {
                UnequipRune(rune.EquippedSlotIndex);
            }

            // 装备
            slot.EquippedRune = rune;
            rune.IsEquipped = true;
            rune.EquippedSlotIndex = slotIndex;

            // 刷新属性
            RefreshAttributes();

            OnEquipChanged?.Invoke();

            Debug.Log($"[RuneManager] 装备符文 [{slotIndex}]: {rune.Data.RuneName} ★{rune.Star}");
            return true;
        }

        /// <summary>
        /// 卸下指定槽位的符文
        /// </summary>
        public bool UnequipRune(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PLAYER_SLOT_COUNT) return false;

            var slot = _playerSlots[slotIndex];
            if (slot.EquippedRune == null) return false;

            var rune = slot.EquippedRune;
            rune.IsEquipped = false;
            rune.EquippedSlotIndex = -1;
            slot.EquippedRune = null;

            // 刷新属性
            RefreshAttributes();

            OnEquipChanged?.Invoke();

            Debug.Log($"[RuneManager] 卸下符文 [{slotIndex}]: {rune.Data.RuneName}");
            return true;
        }

        /// <summary>
        /// 检查分类是否匹配槽位类型
        /// </summary>
        private bool IsCategoryMatch(RuneSlotType slotType, RuneCategory category)
        {
            // 特殊槽可以装任何类型
            if (slotType == RuneSlotType.Special) return true;

            switch (slotType)
            {
                case RuneSlotType.Attack:
                    return category == RuneCategory.Attack;
                case RuneSlotType.Defense:
                    return category == RuneCategory.Defense;
                case RuneSlotType.Survival:
                    return category == RuneCategory.Survival;
                default:
                    return false;
            }
        }

        #endregion

        #region 属性应用

        /// <summary>
        /// 刷新所有符文的属性加成
        /// </summary>
        public void RefreshAttributes()
        {
            // 清除所有符文层加成
            EventBus.Trigger(new RuneAttributesRefreshedEvent
            {
                IsRefresh = true
            });

            // 重新应用所有已装备符文
            foreach (var slot in _playerSlots)
            {
                if (slot.EquippedRune == null) continue;

                ApplyRuneBonuses(slot.EquippedRune);
            }
        }

        /// <summary>
        /// 应用单个符文的加成
        /// </summary>
        private void ApplyRuneBonuses(RuneInstance rune)
        {
            if (rune == null || rune.Data == null) return;

            // 主属性
            if (rune.Data.MainAttrType != AttrType.Unknown)
            {
                EventBus.Trigger(new BuildingBonusAppliedEvent
                {
                    BuildingType = BuildingType.None,
                    AttrType = rune.Data.MainAttrType,
                    IsFlat = false,
                    Value = rune.MainAttrPct,
                    Layer = AttrLayer.Pct_Rune,
                    IsAdding = true
                });
            }

            // 副属性（紫色及以上）
            if (rune.Data.HasSubAttr)
            {
                EventBus.Trigger(new BuildingBonusAppliedEvent
                {
                    BuildingType = BuildingType.None,
                    AttrType = rune.Data.SubAttrType,
                    IsFlat = false,
                    Value = rune.SubAttrPct,
                    Layer = AttrLayer.Pct_Rune,
                    IsAdding = true
                });
            }
        }

        #endregion

        #region 符文获取与抽卡

        /// <summary>
        /// 单抽
        /// </summary>
        public RuneInstance GachaSingle()
        {
            var results = Gacha(1);
            return results.Count > 0 ? results[0] : null;
        }

        /// <summary>
        /// 十连抽
        /// </summary>
        public List<RuneInstance> GachaTen()
        {
            return Gacha(10);
        }

        /// <summary>
        /// 抽卡
        /// </summary>
        public List<RuneInstance> Gacha(int count)
        {
            List<RuneInstance> results = new List<RuneInstance>();
            bool guaranteedRare = false;
            bool guaranteedEpic = false;
            bool guaranteedLegendary = false;

            for (int i = 0; i < count; i++)
            {
                // 检查保底
                if (i == count - 1 && _pityRareCounter + count >= 10 && !HasRareOrAbove(results))
                {
                    guaranteedRare = true;
                }
                if (i == count - 1 && _pityEpicCounter + count >= 50 && !HasEpicOrAbove(results))
                {
                    guaranteedEpic = true;
                }
                if (i == count - 1 && _pityLegendaryCounter + count >= 100 && !HasLegendary(results))
                {
                    guaranteedLegendary = true;
                }

                // 抽取稀有度
                Rarity rarity = RollRarity(guaranteedRare, guaranteedEpic, guaranteedLegendary);

                // 抽取符文
                var rune = RollRune(rarity);
                if (rune != null)
                {
                    results.Add(rune);
                    _ownedRunes.Add(rune);

                    // 更新保底计数
                    UpdatePityCounters(rarity);

                    // 软保底：防重复
                    UpdateDuplicateCheck(rune);
                }
            }

            OnGachaResult?.Invoke(results);

            Debug.Log($"[RuneManager] 抽卡 {count} 次，获得 {results.Count} 个符文");
            return results;
        }

        /// <summary>
        /// 抽取稀有度
        /// </summary>
        private Rarity RollRarity(bool forceRare, bool forceEpic, bool forceLegendary)
        {
            // 强保底线
            if (forceLegendary) return Rarity.Legendary;
            if (forceEpic) return Rarity.Epic;
            if (forceRare) return Rarity.Rare;

            float roll = UnityEngine.Random.value;

            // 普通概率：白60% / 蓝30% / 紫8% / 金2%
            if (roll < 0.60f) return Rarity.Common;
            if (roll < 0.90f) return Rarity.Rare;
            if (roll < 0.98f) return Rarity.Epic;
            return Rarity.Legendary;
        }

        /// <summary>
        /// 抽取指定稀有度的符文
        /// </summary>
        private RuneInstance RollRune(Rarity rarity)
        {
            var runesOfRarity = RuneFactory.GetRunesByRarity(rarity);
            if (runesOfRarity.Count == 0) return null;

            // 软保底：如果连续3次同类型同品质，换一个
            RuneData selected = null;
            if (_duplicateCounter >= 3)
            {
                // 随机选一个不同的
                List<RuneData> candidates = new List<RuneData>();
                foreach (var r in runesOfRarity)
                {
                    if (r.RuneId != _lastRuneId)
                        candidates.Add(r);
                }

                if (candidates.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, candidates.Count);
                    selected = candidates[idx];
                }
                _duplicateCounter = 0;
            }

            if (selected == null)
            {
                int idx = UnityEngine.Random.Range(0, runesOfRarity.Count);
                selected = runesOfRarity[idx];
            }

            // 创建实例
            _instanceIdCounter++;
            var instance = new RuneInstance(_instanceIdCounter, selected, 1);

            return instance;
        }

        /// <summary>
        /// 更新保底计数器
        /// </summary>
        private void UpdatePityCounters(Rarity rarity)
        {
            _pityRareCounter++;
            _pityEpicCounter++;
            _pityLegendaryCounter++;

            if (rarity >= Rarity.Rare) _pityRareCounter = 0;
            if (rarity >= Rarity.Epic) _pityEpicCounter = 0;
            if (rarity >= Rarity.Legendary) _pityLegendaryCounter = 0;
        }

        /// <summary>
        /// 更新重复检查
        /// </summary>
        private void UpdateDuplicateCheck(RuneInstance rune)
        {
            if (rune.Data.Rarity == _lastRarity && rune.Data.RuneId == _lastRuneId)
            {
                _duplicateCounter++;
            }
            else
            {
                _duplicateCounter = 1;
                _lastRarity = rune.Data.Rarity;
                _lastRuneId = rune.Data.RuneId;
            }
        }

        private bool HasRareOrAbove(List<RuneInstance> results)
        {
            foreach (var r in results)
                if (r.Data.Rarity >= Rarity.Rare) return true;
            return false;
        }

        private bool HasEpicOrAbove(List<RuneInstance> results)
        {
            foreach (var r in results)
                if (r.Data.Rarity >= Rarity.Epic) return true;
            return false;
        }

        private bool HasLegendary(List<RuneInstance> results)
        {
            foreach (var r in results)
                if (r.Data.Rarity >= Rarity.Legendary) return true;
            return false;
        }

        #endregion

        #region 碎片与升星

        /// <summary>
        /// 获取指定符文的碎片数
        /// </summary>
        public int GetShards(string runeId)
        {
            if (_shardInventory.TryGetValue(runeId, out int count))
                return count;
            return 0;
        }

        /// <summary>
        /// 添加碎片
        /// </summary>
        public void AddShards(string runeId, int amount)
        {
            if (!_shardInventory.ContainsKey(runeId))
                _shardInventory[runeId] = 0;

            _shardInventory[runeId] += amount;
        }

        /// <summary>
        /// 消耗碎片合成符文
        /// </summary>
        public RuneInstance CraftRune(string runeId)
        {
            var data = RuneFactory.GetRune(runeId);
            if (data == null) return null;

            int cost = GetCraftCost(data.Rarity);
            if (GetShards(runeId) < cost) return null;

            _shardInventory[runeId] -= cost;

            _instanceIdCounter++;
            var rune = new RuneInstance(_instanceIdCounter, data, 1);
            _ownedRunes.Add(rune);

            OnRuneGained?.Invoke(rune);
            return rune;
        }

        /// <summary>
        /// 获取合成消耗
        /// </summary>
        public int GetCraftCost(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return 100;
                case Rarity.Rare: return 300;
                case Rarity.Epic: return 1000;
                case Rarity.Legendary: return 5000;
                default: return 9999;
            }
        }

        /// <summary>
        /// 符文升星
        /// </summary>
        public bool StarUpRune(RuneInstance rune)
        {
            if (rune == null || !rune.CanStarUp()) return false;

            int oldStar = rune.Star;
            bool success = rune.StarUp();

            if (success)
            {
                // 如果已装备，刷新属性
                if (rune.IsEquipped)
                {
                    RefreshAttributes();
                }

                OnRuneStarUp?.Invoke(rune, rune.Star);
                Debug.Log($"[RuneManager] {rune.Data.RuneName} 升星: ★{oldStar} → ★{rune.Star}");
            }

            return success;
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取已装备的符文数量
        /// </summary>
        public int EquippedCount
        {
            get
            {
                int count = 0;
                foreach (var slot in _playerSlots)
                {
                    if (slot.EquippedRune != null) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取拥有的符文总数
        /// </summary>
        public int OwnedRuneCount => _ownedRunes.Count;

        /// <summary>
        /// 获取指定槽位
        /// </summary>
        public RuneSlot GetSlot(int index)
        {
            if (index < 0 || index >= PLAYER_SLOT_COUNT) return null;
            return _playerSlots[index];
        }

        /// <summary>
        /// 获取指定类型的符文列表
        /// </summary>
        public List<RuneInstance> GetOwnedRunesByCategory(RuneCategory category)
        {
            List<RuneInstance> result = new List<RuneInstance>();
            foreach (var rune in _ownedRunes)
            {
                if (rune.Data != null && rune.Data.Category == category)
                    result.Add(rune);
            }
            return result;
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            OnEquipChanged = null;
            OnRuneGained = null;
            OnRuneStarUp = null;
            OnGachaResult = null;

            _ownedRunes.Clear();
            _shardInventory.Clear();

            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// 符文属性刷新事件
    /// </summary>
    public struct RuneAttributesRefreshedEvent : IEvent
    {
        public bool IsRefresh;
    }
}
