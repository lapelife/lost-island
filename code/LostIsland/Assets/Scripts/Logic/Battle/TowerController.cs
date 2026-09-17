using UnityEngine;
using LostIsland.Entity;
using LostIsland.Core;

namespace LostIsland.Logic.Battle
{
    /// <summary>
    /// 灯塔控制器：管理灯塔的生命值、位置、被攻击等
    /// 灯塔是玩家需要保护的目标，也是丧尸的攻击对象
    /// 灯塔HP归零 = 战斗失败
    /// </summary>
    public class TowerController
    {
        #region 配置常量

        /// <summary>
        /// 灯塔基础HP（第1天）
        /// </summary>
        public const float BASE_MAX_HP = 5000f;

        /// <summary>
        /// 每天HP增量
        /// </summary>
        public const float HP_INCREMENT_PER_DAY = 2000f;

        /// <summary>
        /// 灯塔防御值（基础）
        /// </summary>
        public const float BASE_DEF = 100f;

        /// <summary>
        /// 每天防御增量
        /// </summary>
        public const float DEF_INCREMENT_PER_DAY = 20f;

        #endregion

        #region 引用

        /// <summary>
        /// 灯塔实体
        /// </summary>
        public EntityBase Entity { get; private set; }

        /// <summary>
        /// 移动组件（用于位置）
        /// </summary>
        public MoveComponent MoveComp { get; private set; }

        #endregion

        #region 属性

        /// <summary>
        /// 当前天数
        /// </summary>
        public int DayNumber { get; private set; }

        /// <summary>
        /// 最大HP
        /// </summary>
        public float MaxHP => Entity != null && Entity.Health != null ? Entity.Health.MaxHP : 0f;

        /// <summary>
        /// 当前HP
        /// </summary>
        public float CurrentHP => Entity != null && Entity.Health != null ? Entity.Health.CurrentHP : 0f;

        /// <summary>
        /// HP百分比
        /// </summary>
        public float HPPercent => Entity != null && Entity.Health != null ? Entity.Health.HPPct : 0f;

        /// <summary>
        /// 是否被摧毁
        /// </summary>
        public bool IsDestroyed => Entity != null && Entity.Health != null && Entity.Health.IsDead;

        /// <summary>
        /// 灯塔位置
        /// </summary>
        public Vector3 Position
        {
            get => MoveComp != null ? MoveComp.Position : Vector3.zero;
            set
            {
                if (MoveComp != null)
                    MoveComp.SetPosition(value);
            }
        }

        #endregion

        #region 破损状态

        /// <summary>
        /// 破损等级（0-3，用于外观表现）
        /// 0: 完好
        /// 1: 轻微破损 (HP < 75%)
        /// 2: 中度破损 (HP < 50%)
        /// 3: 严重破损 (HP < 25%)
        /// </summary>
        public int DamageLevel
        {
            get
            {
                float pct = HPPercent;
                if (pct >= 0.75f) return 0;
                if (pct >= 0.5f) return 1;
                if (pct >= 0.25f) return 2;
                return 3;
            }
        }

        #endregion

        #region 初始化

        public TowerController(EntityBase entity)
        {
            Entity = entity;

            // 获取或添加移动组件（用于位置管理）
            MoveComp = entity.GetComponent<MoveComponent>();
            if (MoveComp == null)
            {
                MoveComp = entity.AddComponent<MoveComponent>();
            }

            // 监听HP变化
            entity.Health.OnHPPctChanged += OnHPPctChanged;
            entity.Health.OnDeath += OnTowerDestroyed;
        }

        /// <summary>
        /// 根据天数初始化灯塔属性
        /// </summary>
        /// <param name="day">当前天数</param>
        public void InitByDay(int day)
        {
            DayNumber = day;

            // 计算最大HP: 5000 + 2000 × (day-1)
            float maxHP = CalculateMaxHP(day);
            Entity.Attribute.SetBaseValue(AttrType.MaxHP, maxHP);

            // 计算防御: 100 + 20 × (day-1)
            float def = CalculateDef(day);
            Entity.Attribute.SetBaseValue(AttrType.DEF, def);

            // 回满HP
            Entity.Health.HealFull();

            Debug.Log($"[TowerController] 灯塔初始化：第{day}天，HP={maxHP:F0}，DEF={def:F0}");
        }

        /// <summary>
        /// 计算第N天的灯塔最大HP
        /// </summary>
        public static float CalculateMaxHP(int day)
        {
            return BASE_MAX_HP + HP_INCREMENT_PER_DAY * Mathf.Max(0, day - 1);
        }

        /// <summary>
        /// 计算第N天的灯塔防御值
        /// </summary>
        public static float CalculateDef(int day)
        {
            return BASE_DEF + DEF_INCREMENT_PER_DAY * Mathf.Max(0, day - 1);
        }

        #endregion

        #region 受击逻辑

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <param name="source">伤害来源</param>
        /// <returns>实际造成的伤害</returns>
        public float TakeDamage(float damage, EntityBase source = null)
        {
            if (IsDestroyed) return 0f;

            DamageResult result = new DamageResult
            {
                FinalDamage = damage,
                RawDamage = damage,
                Source = source
            };

            float actualDamage = Entity.Health.TakeDamage(result);

            if (actualDamage > 0f)
            {
                // 触发灯塔HP变化事件
                EventBus.Trigger(new TowerHpChangedEvent
                {
                    CurrentHp = Entity.Health.CurrentHP,
                    MaxHp = Entity.Health.MaxHP,
                    HpPercent = Entity.Health.HPPct
                });
            }

            return actualDamage;
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// HP百分比变化
        /// </summary>
        private void OnHPPctChanged(float oldPct, float newPct)
        {
            // 检查破损等级变化
            int oldLevel = GetDamageLevelFromPct(oldPct);
            int newLevel = GetDamageLevelFromPct(newPct);

            if (oldLevel != newLevel)
            {
                Debug.Log($"[TowerController] 灯塔破损等级变化: {oldLevel} -> {newLevel}");
                // 可触发外观变化事件
            }
        }

        /// <summary>
        /// 根据HP百分比计算破损等级
        /// </summary>
        private int GetDamageLevelFromPct(float pct)
        {
            if (pct >= 0.75f) return 0;
            if (pct >= 0.5f) return 1;
            if (pct >= 0.25f) return 2;
            return 3;
        }

        /// <summary>
        /// 灯塔被摧毁
        /// </summary>
        private void OnTowerDestroyed()
        {
            Debug.Log($"[TowerController] 灯塔被摧毁！坚持到第{DayNumber}天");
        }

        #endregion

        #region 修复

        /// <summary>
        /// 修复灯塔
        /// </summary>
        /// <param name="amount">修复量</param>
        /// <returns>实际修复量</returns>
        public float Repair(float amount)
        {
            if (IsDestroyed) return 0f;

            float actual = Entity.Health.Heal(amount);

            if (actual > 0f)
            {
                EventBus.Trigger(new TowerHpChangedEvent
                {
                    CurrentHp = Entity.Health.CurrentHP,
                    MaxHp = Entity.Health.MaxHP,
                    HpPercent = Entity.Health.HPPct
                });
            }

            return actual;
        }

        /// <summary>
        /// 完全修复
        /// </summary>
        public float RepairFull()
        {
            return Repair(MaxHP);
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 重置灯塔（新的一天）
        /// </summary>
        public void ResetForNewDay(int day)
        {
            InitByDay(day);
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        public void DebugDump()
        {
            Debug.Log($"=== 灯塔状态 ===");
            Debug.Log($"天数: {DayNumber}");
            Debug.Log($"HP: {CurrentHP:F0} / {MaxHP:F0} ({HPPercent:P0})");
            Debug.Log($"破损等级: {DamageLevel}");
            Debug.Log($"位置: {Position}");
            Debug.Log($"是否摧毁: {IsDestroyed}");
        }

        #endregion
    }
}
