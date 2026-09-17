using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Logic.Survivors
{
    /// <summary>
    /// 幸存者管理器
    /// 管理幸存者的招募、上阵、升级、战斗
    /// </summary>
    public class SurvivorManager : MonoSingleton<SurvivorManager>
    {
        #region 配置

        /// <summary>最大上阵人数</summary>
        public const int MAX_DEPLOYED = 3;

        /// <summary>招募一次消耗的食物</summary>
        public const int RECRUIT_COST_FOOD = 50;

        /// <summary>十连招募消耗</summary>
        public const int RECRUIT_TEN_COST_FOOD = 450;

        #endregion

        #region 状态

        /// <summary>拥有的幸存者列表</summary>
        private List<SurvivorInstance> _ownedSurvivors = new List<SurvivorInstance>();

        /// <summary>上阵的幸存者槽位</summary>
        private SurvivorInstance[] _deployedSlots;

        /// <summary>实例ID计数器</summary>
        private int _instanceIdCounter = 20000;

        /// <summary>累计招募次数</summary>
        private int _totalRecruits = 0;

        #endregion

        #region 招募保底

        /// <summary>距离上次出蓝的次数</summary>
        private int _pityRareCounter = 0;

        /// <summary>距离上次出紫的次数</summary>
        private int _pityEpicCounter = 0;

        /// <summary>距离上次出金的次数</summary>
        private int _pityLegendaryCounter = 0;

        #endregion

        #region 事件

        /// <summary>幸存者获得事件</summary>
        public event Action<SurvivorInstance> OnSurvivorGained;

        /// <summary>上阵变化事件</summary>
        public event Action OnDeployChanged;

        /// <summary>幸存者升级事件</summary>
        public event Action<SurvivorInstance, int> OnSurvivorLevelUp; // instance, newLevel

        /// <summary>招募结果事件</summary>
        public event Action<List<SurvivorInstance>> OnRecruitResult;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            _deployedSlots = new SurvivorInstance[MAX_DEPLOYED];

            Debug.Log("[SurvivorManager] 幸存者管理器初始化完成");
        }

        #endregion

        #region 招募系统

        /// <summary>
        /// 单抽招募
        /// </summary>
        public SurvivorInstance RecruitSingle()
        {
            var results = Recruit(1);
            return results.Count > 0 ? results[0] : null;
        }

        /// <summary>
        /// 十连招募
        /// </summary>
        public List<SurvivorInstance> RecruitTen()
        {
            return Recruit(10);
        }

        /// <summary>
        /// 招募
        /// </summary>
        public List<SurvivorInstance> Recruit(int count)
        {
            List<SurvivorInstance> results = new List<SurvivorInstance>();

            for (int i = 0; i < count; i++)
            {
                // 保底检测
                bool forceRare = (i == count - 1 && _pityRareCounter + count >= 10 && !HasRareOrAbove(results));
                bool forceEpic = (i == count - 1 && _pityEpicCounter + count >= 30 && !HasEpicOrAbove(results));
                bool forceLegendary = (i == count - 1 && _pityLegendaryCounter + count >= 100 && !HasLegendary(results));

                var rarity = RollRarity(forceRare, forceEpic, forceLegendary);
                var survivor = RollSurvivor(rarity);

                if (survivor != null)
                {
                    results.Add(survivor);
                    _ownedSurvivors.Add(survivor);

                    UpdatePityCounters(rarity);
                    _totalRecruits++;

                    OnSurvivorGained?.Invoke(survivor);
                }
            }

            OnRecruitResult?.Invoke(results);

            Debug.Log($"[SurvivorManager] 招募 {count} 次，获得 {results.Count} 个幸存者");
            return results;
        }

        /// <summary>
        /// 抽取稀有度
        /// </summary>
        private SurvivorRarity RollRarity(bool forceRare, bool forceEpic, bool forceLegendary)
        {
            if (forceLegendary) return SurvivorRarity.Legendary;
            if (forceEpic) return SurvivorRarity.Epic;
            if (forceRare) return SurvivorRarity.Rare;

            float roll = UnityEngine.Random.value;

            // 白60% / 蓝25% / 紫12% / 金3%
            if (roll < 0.60f) return SurvivorRarity.Common;
            if (roll < 0.85f) return SurvivorRarity.Rare;
            if (roll < 0.97f) return SurvivorRarity.Epic;
            return SurvivorRarity.Legendary;
        }

        /// <summary>
        /// 抽取指定稀有度的幸存者
        /// </summary>
        private SurvivorInstance RollSurvivor(SurvivorRarity rarity)
        {
            var list = SurvivorFactory.GetByRarity(rarity);
            if (list.Count == 0) return null;

            int idx = UnityEngine.Random.Range(0, list.Count);
            var data = list[idx];

            _instanceIdCounter++;
            var instance = new SurvivorInstance(_instanceIdCounter, data);

            return instance;
        }

        /// <summary>
        /// 更新保底计数
        /// </summary>
        private void UpdatePityCounters(SurvivorRarity rarity)
        {
            _pityRareCounter++;
            _pityEpicCounter++;
            _pityLegendaryCounter++;

            if (rarity >= SurvivorRarity.Rare) _pityRareCounter = 0;
            if (rarity >= SurvivorRarity.Epic) _pityEpicCounter = 0;
            if (rarity >= SurvivorRarity.Legendary) _pityLegendaryCounter = 0;
        }

        private bool HasRareOrAbove(List<SurvivorInstance> results)
        {
            foreach (var s in results)
                if (s.Data.Rarity >= SurvivorRarity.Rare) return true;
            return false;
        }

        private bool HasEpicOrAbove(List<SurvivorInstance> results)
        {
            foreach (var s in results)
                if (s.Data.Rarity >= SurvivorRarity.Epic) return true;
            return false;
        }

        private bool HasLegendary(List<SurvivorInstance> results)
        {
            foreach (var s in results)
                if (s.Data.Rarity >= SurvivorRarity.Legendary) return true;
            return false;
        }

        #endregion

        #region 上阵系统

        /// <summary>
        /// 上阵
        /// </summary>
        public bool Deploy(int slotIndex, SurvivorInstance survivor)
        {
            if (slotIndex < 0 || slotIndex >= MAX_DEPLOYED) return false;
            if (survivor == null || survivor.Data == null) return false;
            if (survivor.Status == SurvivorStatus.Injured) return false;

            // 如果槽位已有，先卸下
            if (_deployedSlots[slotIndex] != null)
            {
                Undeploy(slotIndex);
            }

            // 如果幸存者已在其他槽位，先卸下
            if (survivor.IsDeployed && survivor.DeploySlotIndex >= 0)
            {
                Undeploy(survivor.DeploySlotIndex);
            }

            _deployedSlots[slotIndex] = survivor;
            survivor.IsDeployed = true;
            survivor.DeploySlotIndex = slotIndex;
            survivor.Status = SurvivorStatus.Fighting;

            OnDeployChanged?.Invoke();

            Debug.Log($"[SurvivorManager] 上阵 [{slotIndex}]: {survivor.Data.SurvivorName}");
            return true;
        }

        /// <summary>
        /// 下阵
        /// </summary>
        public bool Undeploy(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_DEPLOYED) return false;

            var survivor = _deployedSlots[slotIndex];
            if (survivor == null) return false;

            survivor.IsDeployed = false;
            survivor.DeploySlotIndex = -1;
            if (survivor.Status == SurvivorStatus.Fighting)
                survivor.Status = SurvivorStatus.Idle;

            _deployedSlots[slotIndex] = null;

            OnDeployChanged?.Invoke();

            Debug.Log($"[SurvivorManager] 下阵 [{slotIndex}]: {survivor.Data.SurvivorName}");
            return true;
        }

        /// <summary>
        /// 获取上阵的幸存者
        /// </summary>
        public SurvivorInstance GetDeployed(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_DEPLOYED) return null;
            return _deployedSlots[slotIndex];
        }

        /// <summary>
        /// 上阵人数
        /// </summary>
        public int DeployedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MAX_DEPLOYED; i++)
                {
                    if (_deployedSlots[i] != null) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取所有上阵幸存者
        /// </summary>
        public List<SurvivorInstance> GetAllDeployed()
        {
            List<SurvivorInstance> result = new List<SurvivorInstance>();
            for (int i = 0; i < MAX_DEPLOYED; i++)
            {
                if (_deployedSlots[i] != null)
                    result.Add(_deployedSlots[i]);
            }
            return result;
        }

        #endregion

        #region 升级与经验

        /// <summary>
        /// 给指定幸存者添加经验
        /// </summary>
        public bool AddExp(SurvivorInstance survivor, int exp)
        {
            if (survivor == null) return false;

            int oldLevel = survivor.Level;
            bool leveledUp = survivor.AddExp(exp);

            if (leveledUp)
            {
                OnSurvivorLevelUp?.Invoke(survivor, survivor.Level);
                Debug.Log($"[SurvivorManager] {survivor.Data.SurvivorName} 升级: Lv.{oldLevel} → Lv.{survivor.Level}");
            }

            return leveledUp;
        }

        /// <summary>
        /// 给所有上阵幸存者添加经验
        /// </summary>
        public void AddExpToAllDeployed(int exp)
        {
            for (int i = 0; i < MAX_DEPLOYED; i++)
            {
                var s = _deployedSlots[i];
                if (s != null && s.Status != SurvivorStatus.Injured)
                {
                    AddExp(s, exp);
                }
            }
        }

        #endregion

        #region 战斗生命周期

        /// <summary>
        /// 战斗开始
        /// </summary>
        public void OnBattleStart()
        {
            for (int i = 0; i < MAX_DEPLOYED; i++)
            {
                var s = _deployedSlots[i];
                if (s != null)
                {
                    s.CurrentHP = s.MaxHP;
                    s.Status = SurvivorStatus.Fighting;
                }
            }

            Debug.Log("[SurvivorManager] 战斗开始，幸存者已就绪");
        }

        /// <summary>
        /// 战斗结束
        /// </summary>
        public void OnBattleEnd(bool victory)
        {
            for (int i = 0; i < MAX_DEPLOYED; i++)
            {
                var s = _deployedSlots[i];
                if (s != null)
                {
                    if (victory && s.Status != SurvivorStatus.Injured)
                    {
                        s.Status = SurvivorStatus.Idle;
                    }
                    // 受伤的保持受伤状态
                }
            }
        }

        /// <summary>
        /// 休息恢复（白天结束时）
        /// </summary>
        public void RecoverAll()
        {
            foreach (var s in _ownedSurvivors)
            {
                s.Recover();
            }

            Debug.Log("[SurvivorManager] 所有幸存者已恢复");
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 拥有的幸存者总数
        /// </summary>
        public int OwnedCount => _ownedSurvivors.Count;

        /// <summary>
        /// 总招募次数
        /// </summary>
        public int TotalRecruits => _totalRecruits;

        /// <summary>
        /// 获取指定职业的幸存者
        /// </summary>
        public List<SurvivorInstance> GetByClass(SurvivorClass cls)
        {
            List<SurvivorInstance> result = new List<SurvivorInstance>();
            foreach (var s in _ownedSurvivors)
            {
                if (s.Data.Class == cls) result.Add(s);
            }
            return result;
        }

        /// <summary>
        /// 获取总战力（所有上阵幸存者）
        /// </summary>
        public float TotalCombatPower
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < MAX_DEPLOYED; i++)
                {
                    if (_deployedSlots[i] != null)
                        total += _deployedSlots[i].CombatPower;
                }
                return total;
            }
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            OnSurvivorGained = null;
            OnDeployChanged = null;
            OnSurvivorLevelUp = null;
            OnRecruitResult = null;

            _ownedSurvivors.Clear();
            _deployedSlots = null;

            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// 幸存者招募事件
    /// </summary>
    public struct SurvivorRecruitedEvent : IEvent
    {
        public string SurvivorId;
        public string SurvivorName;
        public SurvivorRarity Rarity;
        public SurvivorClass Class;
    }

    /// <summary>
    /// 幸存者上阵变化事件
    /// </summary>
    public struct SurvivorDeployChangedEvent : IEvent
    {
        public int DeployedCount;
        public float TotalCombatPower;
    }
}
