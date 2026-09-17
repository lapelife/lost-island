using UnityEngine;
using LostIsland.Data.Config;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// 丧尸配置 ScriptableObject
    /// 每种丧尸一个配置文件，定义基础属性和行为
    /// </summary>
    public class ZombieConfigSO : BaseConfigSO
    {
        [Header("类型")]
        [Tooltip("丧尸类型")]
        public ZombieType ZombieType;

        [Tooltip("稀有度")]
        public ZombieRarity Rarity;

        [Header("基础属性（第1天第1波）")]
        [Tooltip("基础HP")]
        public float BaseHP = 100f;

        [Tooltip("基础ATK")]
        public float BaseATK = 10f;

        [Tooltip("基础DEF")]
        public float BaseDEF = 0f;

        [Tooltip("基础移动速度")]
        public float BaseMoveSpeed = 2f;

        [Tooltip("基础攻击速度（次/秒）")]
        public float BaseAttackSpeed = 1f;

        [Tooltip("攻击范围")]
        public float AttackRange = 1.5f;

        [Tooltip("攻击前摇时间（秒）")]
        public float AttackWindup = 0.3f;

        [Tooltip("攻击后摇时间（秒）")]
        public float AttackCooldown = 0.2f;

        [Header("掉落")]
        [Tooltip("基础腐肉掉落")]
        public int FleshDrop = 1;

        [Tooltip("晶核掉落概率")]
        [Range(0f, 1f)]
        public float CrystalDropChance = 0.01f;

        [Tooltip("晶核掉落数量")]
        public int CrystalDropAmount = 1;

        [Header("特殊属性")]
        [Tooltip("HP倍率（相对于普通丧尸）")]
        public float HPMultiplier = 1f;

        [Tooltip("ATK倍率")]
        public float ATKMultiplier = 1f;

        [Tooltip("移动速度倍率")]
        public float SpeedMultiplier = 1f;

        [Tooltip("体型缩放")]
        public float ScaleMultiplier = 1f;

        [Header("特殊能力")]
        [Tooltip("是否自爆丧尸")]
        public bool IsExploder = false;

        [Tooltip("自爆范围")]
        public float ExplosionRange = 2f;

        [Tooltip("自爆伤害倍率")]
        public float ExplosionDamageMultiplier = 2f;

        [Tooltip("是否有护盾")]
        public bool HasShield = false;

        [Tooltip("护盾HP")]
        public float ShieldHP = 100f;

        [Tooltip("是否会召唤")]
        public bool CanSummon = false;

        [Tooltip("召唤间隔（秒）")]
        public float SummonInterval = 10f;

        [Tooltip("召唤数量")]
        public int SummonCount = 2;

        [Tooltip("召唤的丧尸类型")]
        public ZombieType SummonType = ZombieType.Normal;

        [Tooltip("是否会治疗")]
        public bool CanHeal = false;

        [Tooltip("治疗范围")]
        public float HealRange = 3f;

        [Tooltip("治疗量（最大HP百分比）")]
        [Range(0f, 1f)]
        public float HealPct = 0.05f;

        [Tooltip("治疗间隔")]
        public float HealInterval = 3f;

        [Tooltip("是否可隐形")]
        public bool CanInvisible = false;

        [Tooltip("显形距离")]
        public float VisibleDistance = 3f;

        [Tooltip("是否会冲锋")]
        public bool CanCharge = false;

        [Tooltip("冲锋距离")]
        public float ChargeDistance = 5f;

        [Tooltip("冲锋速度倍率")]
        public float ChargeSpeedMultiplier = 3f;

        [Tooltip("冲锋冷却")]
        public float ChargeCooldown = 8f;

        [Tooltip("是否有毒")]
        public bool IsPoisonous = false;

        [Tooltip("中毒持续时间")]
        public float PoisonDuration = 3f;

        [Tooltip("中毒每秒伤害（最大HP%）")]
        [Range(0f, 1f)]
        public float PoisonDpsPct = 0.02f;

        [Tooltip("中毒减速比例")]
        [Range(0f, 1f)]
        public float PoisonSlowPct = 0.3f;

        [Header("AI行为")]
        [Tooltip("追击目标优先级：0=灯塔，1=玩家")]
        [Range(0, 1)]
        public int TargetPriority = 0;

        [Tooltip("攻击灯塔优先还是玩家优先")]
        public bool PreferTower = true;

        #region 工具方法

        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(ConfigName) ? ZombieType.ToString() : ConfigName;
        }

        /// <summary>
        /// 计算指定波次的HP
        /// </summary>
        public float GetHP(int wave, int day, float hpGrowthMult)
        {
            return BaseHP * HPMultiplier * hpGrowthMult;
        }

        /// <summary>
        /// 计算指定波次的ATK
        /// </summary>
        public float GetATK(int wave, int day, float atkGrowthMult)
        {
            return BaseATK * ATKMultiplier * atkGrowthMult;
        }

        /// <summary>
        /// 计算指定波次的DEF
        /// </summary>
        public float GetDEF(int wave, int day, float defGrowthMult)
        {
            return BaseDEF * defGrowthMult;
        }

        /// <summary>
        /// 获取实际移动速度
        /// </summary>
        public float GetMoveSpeed()
        {
            return BaseMoveSpeed * SpeedMultiplier;
        }

        #endregion
    }
}
