using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;
using LostIsland.Logic.Wave;
using LostIsland.Logic.Tower;

namespace LostIsland.Logic.Skill
{
    /// <summary>
    /// 技能卡管理器
    /// 管理技能卡的装配、冷却、释放
    /// </summary>
    public class SkillCardManager : MonoSingleton<SkillCardManager>
    {
        #region 配置

        /// <summary>主动技能槽位数</summary>
        public const int ACTIVE_SLOT_COUNT = 4;

        /// <summary>被动技能槽位数</summary>
        public const int PASSIVE_SLOT_COUNT = 3;

        #endregion

        #region 槽位

        /// <summary>主动技能槽位</summary>
        private SkillCardSlot[] _activeSlots;

        /// <summary>被动技能槽位</summary>
        private SkillCardSlot[] _passiveSlots;

        public IReadOnlyList<SkillCardSlot> ActiveSlots => _activeSlots;
        public IReadOnlyList<SkillCardSlot> PassiveSlots => _passiveSlots;

        #endregion

        #region 施放者引用

        /// <summary>玩家实体引用</summary>
        private EntityBase _playerEntity;

        #endregion

        #region 事件

        /// <summary>技能释放事件 (slotIndex, cardData, level)</summary>
        public event Action<int, SkillCardData, int> OnSkillCast;

        /// <summary>技能冷却变化事件</summary>
        public event Action<int, float> OnCDChanged; // slotIndex, progress

        /// <summary>技能装配变化事件</summary>
        public event Action OnEquipChanged;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            InitializeSlots();

            Debug.Log("[SkillCardManager] 技能卡管理器初始化完成");
        }

        /// <summary>
        /// 初始化槽位
        /// </summary>
        private void InitializeSlots()
        {
            _activeSlots = new SkillCardSlot[ACTIVE_SLOT_COUNT];
            for (int i = 0; i < ACTIVE_SLOT_COUNT; i++)
            {
                _activeSlots[i] = new SkillCardSlot(i);
            }

            _passiveSlots = new SkillCardSlot[PASSIVE_SLOT_COUNT];
            for (int i = 0; i < PASSIVE_SLOT_COUNT; i++)
            {
                _passiveSlots[i] = new SkillCardSlot(i);
            }
        }

        /// <summary>
        /// 设置玩家实体
        /// </summary>
        public void SetPlayerEntity(EntityBase player)
        {
            _playerEntity = player;
        }

        #endregion

        #region 装配管理

        /// <summary>
        /// 装配主动技能
        /// </summary>
        public bool EquipActiveSkill(int slotIndex, SkillCardData card, int level = 1)
        {
            if (slotIndex < 0 || slotIndex >= ACTIVE_SLOT_COUNT) return false;
            if (card == null) return false;
            if (card.IsPassive) return false;

            _activeSlots[slotIndex].Equip(card, level);

            OnEquipChanged?.Invoke();

            Debug.Log($"[SkillCardManager] 装配主动技能 [{slotIndex}]: {card.CardName} Lv.{level}");
            return true;
        }

        /// <summary>
        /// 装配被动技能
        /// </summary>
        public bool EquipPassiveSkill(int slotIndex, SkillCardData card, int level = 1)
        {
            if (slotIndex < 0 || slotIndex >= PASSIVE_SLOT_COUNT) return false;
            if (card == null) return false;
            if (!card.IsPassive) return false;

            // 先移除旧效果
            var oldCard = _passiveSlots[slotIndex].CardData;
            if (oldCard != null)
            {
                RemovePassiveBonuses(oldCard, _passiveSlots[slotIndex].Level);
            }

            _passiveSlots[slotIndex].Equip(card, level);

            // 应用新效果
            ApplyPassiveBonuses(card, level);

            OnEquipChanged?.Invoke();

            Debug.Log($"[SkillCardManager] 装配被动技能 [{slotIndex}]: {card.CardName} Lv.{level}");
            return true;
        }

        /// <summary>
        /// 卸下主动技能
        /// </summary>
        public void UnequipActiveSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= ACTIVE_SLOT_COUNT) return;

            var card = _activeSlots[slotIndex].CardData;
            _activeSlots[slotIndex].Unequip();

            OnEquipChanged?.Invoke();

            if (card != null)
                Debug.Log($"[SkillCardManager] 卸下主动技能 [{slotIndex}]: {card.CardName}");
        }

        /// <summary>
        /// 卸下被动技能
        /// </summary>
        public void UnequipPassiveSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PASSIVE_SLOT_COUNT) return;

            var card = _passiveSlots[slotIndex].CardData;
            int level = _passiveSlots[slotIndex].Level;

            if (card != null)
            {
                RemovePassiveBonuses(card, level);
            }

            _passiveSlots[slotIndex].Unequip();

            OnEquipChanged?.Invoke();

            if (card != null)
                Debug.Log($"[SkillCardManager] 卸下被动技能 [{slotIndex}]: {card.CardName}");
        }

        /// <summary>
        /// 检查是否已装备某张卡
        /// </summary>
        public bool IsEquipped(string cardId)
        {
            foreach (var slot in _activeSlots)
            {
                if (slot.CardData != null && slot.CardData.CardId == cardId)
                    return true;
            }
            foreach (var slot in _passiveSlots)
            {
                if (slot.CardData != null && slot.CardData.CardId == cardId)
                    return true;
            }
            return false;
        }

        #endregion

        #region 被动加成

        /// <summary>
        /// 应用被动技能加成
        /// </summary>
        private void ApplyPassiveBonuses(SkillCardData card, int level)
        {
            if (card == null || card.PassiveBonuses == null) return;

            foreach (var bonus in card.PassiveBonuses)
            {
                float totalValue = bonus.GetTotalValue(level);
                EventBus.Trigger(new BuildingBonusAppliedEvent
                {
                    BuildingType = BuildingType.None,
                    AttrType = bonus.AttrType,
                    IsFlat = bonus.IsFlat,
                    Value = totalValue,
                    Layer = bonus.Layer,
                    IsAdding = true
                });
            }
        }

        /// <summary>
        /// 移除被动技能加成
        /// </summary>
        private void RemovePassiveBonuses(SkillCardData card, int level)
        {
            if (card == null || card.PassiveBonuses == null) return;

            foreach (var bonus in card.PassiveBonuses)
            {
                float totalValue = bonus.GetTotalValue(level);
                EventBus.Trigger(new BuildingBonusAppliedEvent
                {
                    BuildingType = BuildingType.None,
                    AttrType = bonus.AttrType,
                    IsFlat = bonus.IsFlat,
                    Value = totalValue,
                    Layer = bonus.Layer,
                    IsAdding = false
                });
            }
        }

        #endregion

        #region 技能释放

        /// <summary>
        /// 尝试释放主动技能
        /// </summary>
        public bool CastSkill(int slotIndex, Vector3 targetPosition)
        {
            if (slotIndex < 0 || slotIndex >= ACTIVE_SLOT_COUNT) return false;

            var slot = _activeSlots[slotIndex];
            if (slot.IsEmpty || !slot.IsReady) return false;

            var card = slot.CardData;
            int level = slot.Level;

            // 查找目标
            ZombieController target = FindTarget(targetPosition, card);

            // 消耗冷却
            if (!slot.TryCast()) return false;

            // 应用效果
            SkillEffectFactory.ApplyAllEffects(_playerEntity, target, targetPosition, card, level);

            // 触发事件
            OnSkillCast?.Invoke(slotIndex, card, level);
            OnCDChanged?.Invoke(slotIndex, slot.CDProgress);

            EventBus.Trigger(new SkillCastEvent
            {
                SlotIndex = slotIndex,
                CardId = card.CardId,
                CardName = card.CardName,
                Level = level,
                TargetPosition = targetPosition
            });

            Debug.Log($"[SkillCardManager] 释放技能 [{slotIndex}]: {card.CardName} Lv.{level}");
            return true;
        }

        /// <summary>
        /// 查找技能目标
        /// </summary>
        private ZombieController FindTarget(Vector3 targetPosition, SkillCardData card)
        {
            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return null;

            ZombieController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var zombie in spawner.ActiveZombies)
            {
                if (zombie == null || zombie.IsDead) continue;

                float dist = Vector3.Distance(targetPosition, zombie.MoveComp.Position);
                if (dist < nearestDist && dist <= card.CastRange)
                {
                    nearestDist = dist;
                    nearest = zombie;
                }
            }

            return nearest;
        }

        #endregion

        #region 更新

        private void Update()
        {
            float dt = Time.deltaTime;

            // 更新主动技能冷却
            for (int i = 0; i < _activeSlots.Length; i++)
            {
                var slot = _activeSlots[i];
                if (slot.IsEmpty) continue;

                float oldProgress = slot.CDProgress;
                slot.Tick(dt);
                float newProgress = slot.CDProgress;

                if (Math.Abs(oldProgress - newProgress) > 0.001f)
                {
                    OnCDChanged?.Invoke(i, newProgress);
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
            // 重置所有冷却
            foreach (var slot in _activeSlots)
            {
                slot.ResetCD();
            }

            Debug.Log("[SkillCardManager] 战斗开始，技能冷却已重置");
        }

        /// <summary>
        /// 战斗结束
        /// </summary>
        public void OnBattleEnd(bool victory)
        {
            // 可以在这里处理胜利奖励等
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取主动技能槽位
        /// </summary>
        public SkillCardSlot GetActiveSlot(int index)
        {
            if (index < 0 || index >= ACTIVE_SLOT_COUNT) return null;
            return _activeSlots[index];
        }

        /// <summary>
        /// 获取被动技能槽位
        /// </summary>
        public SkillCardSlot GetPassiveSlot(int index)
        {
            if (index < 0 || index >= PASSIVE_SLOT_COUNT) return null;
            return _passiveSlots[index];
        }

        /// <summary>
        /// 已装配的主动技能数
        /// </summary>
        public int ActiveSkillCount
        {
            get
            {
                int count = 0;
                foreach (var slot in _activeSlots)
                {
                    if (!slot.IsEmpty) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 已装配的被动技能数
        /// </summary>
        public int PassiveSkillCount
        {
            get
            {
                int count = 0;
                foreach (var slot in _passiveSlots)
                {
                    if (!slot.IsEmpty) count++;
                }
                return count;
            }
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            OnSkillCast = null;
            OnCDChanged = null;
            OnEquipChanged = null;
            _playerEntity = null;
            base.OnDispose();
        }

        #endregion
    }
}
