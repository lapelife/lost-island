using UnityEngine;
using LostIsland.Data.Config;
using LostIsland.Logic.Battle;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// 丧尸类型枚举
    /// </summary>
    public enum ZombieType
    {
        // 普通丧尸
        Normal = 0,         // 普通丧尸
        Fast = 1,           // 快速丧尸
        Heavy = 2,          // 重甲丧尸
        Exploder = 3,       // 自爆丧尸

        // 精英丧尸
        Poison = 10,        // 毒液丧尸
        Shield = 11,        // 护盾丧尸
        Summoner = 12,      // 召唤丧尸
        Charger = 13,       // 冲锋丧尸
        Healer = 14,        // 治疗丧尸
        Invisible = 15,     // 隐形丧尸

        // BOSS
        Boss_RottenFist = 20,   // 腐臭巨拳（近战BOSS）
        Boss_PoisonWitch = 21,  // 毒雾女巫（召唤+毒BOSS）
        Boss_IronButcher = 22,  // 铁甲屠夫（高防+冲锋BOSS）
        Boss_FleshGolem = 23,   // 血肉聚合体（W100里程碑）
        Boss_TeslaTitan = 24,   // 电磁泰坦（W200里程碑）
        Boss_IslandLord = 25,   // 孤岛主宰（W300最终BOSS）
    }

    /// <summary>
    /// 丧尸稀有度
    /// </summary>
    public enum ZombieRarity
    {
        Normal = 0,     // 普通
        Elite = 1,      // 精英
        Boss = 2,       // BOSS
    }

    /// <summary>
    /// 出生点配置
    /// </summary>
    [System.Serializable]
    public class SpawnPoint
    {
        public string Name;
        public Vector3 Position;
        public float Radius = 1f;
        public bool IsActive = true;
    }

    /// <summary>
    /// 出怪表条目
    /// </summary>
    [System.Serializable]
    public struct SpawnEntry
    {
        public ZombieType ZombieType;
        public int StartWave;         // 从第几波开始出现
        [Range(0f, 1f)]
        public float SpawnWeight;     // 出现权重
        public int MinCount;          // 最少数量
        public int MaxCount;          // 最多数量
    }

    /// <summary>
    /// 波次配置 ScriptableObject
    /// </summary>
    public class WaveConfigSO : BaseConfigSO
    {
        [Header("基础设置")]
        [Tooltip("总波数")]
        public int TotalWaves = 300;

        [Tooltip("第1波间隔（秒）")]
        public float MaxWaveInterval = 30f;

        [Tooltip("第300波间隔（秒）")]
        public float MinWaveInterval = 15f;

        [Tooltip("间隔曲线指数（上凸曲线：前段慢后段快）")]
        public float IntervalCurvePower = 0.7f;

        [Header("波次成长系数")]
        [Tooltip("HP波次成长: 1.02^(W-1)")]
        public float HpGrowthPerWave = 1.02f;

        [Tooltip("ATK波次成长: 1.015^(W-1)")]
        public float AtkGrowthPerWave = 1.015f;

        [Tooltip("DEF波次成长: 1.01^(W-1)")]
        public float DefGrowthPerWave = 1.01f;

        [Header("天数成长系数")]
        [Tooltip("HP天数成长: 1.10^(D-1)")]
        public float HpGrowthPerDay = 1.10f;

        [Tooltip("ATK天数成长: 1.08^(D-1)")]
        public float AtkGrowthPerDay = 1.08f;

        [Tooltip("DEF天数成长: 1.05^(D-1)")]
        public float DefGrowthPerDay = 1.05f;

        [Header("基础数量")]
        [Tooltip("第1波基础丧尸数量")]
        public int BaseZombieCount = 5;

        [Tooltip("每波数量增长")]
        public float CountGrowthPerWave = 0.5f;

        [Tooltip("每波数量上限")]
        public int MaxZombiesPerWave = 50;

        [Header("出怪表")]
        public SpawnEntry[] SpawnTable;

        #region 计算方法

        /// <summary>
        /// 获取波次间隔（上凸曲线）
        /// </summary>
        public float GetWaveInterval(int wave)
        {
            if (wave <= 1) return MaxWaveInterval;
            if (wave >= TotalWaves) return MinWaveInterval;

            float t = (float)(wave - 1) / (TotalWaves - 1);
            t = Mathf.Pow(t, IntervalCurvePower); // 上凸曲线
            return Mathf.Lerp(MaxWaveInterval, MinWaveInterval, t);
        }

        /// <summary>
        /// 获取波次HP乘数
        /// </summary>
        public float GetHpMultiplier(int wave, int day)
        {
            float waveMult = Mathf.Pow(HpGrowthPerWave, Mathf.Max(0, wave - 1));
            float dayMult = Mathf.Pow(HpGrowthPerDay, Mathf.Max(0, day - 1));
            return waveMult * dayMult;
        }

        /// <summary>
        /// 获取波次ATK乘数
        /// </summary>
        public float GetAtkMultiplier(int wave, int day)
        {
            float waveMult = Mathf.Pow(AtkGrowthPerWave, Mathf.Max(0, wave - 1));
            float dayMult = Mathf.Pow(AtkGrowthPerDay, Mathf.Max(0, day - 1));
            return waveMult * dayMult;
        }

        /// <summary>
        /// 获取波次DEF乘数
        /// </summary>
        public float GetDefMultiplier(int wave, int day)
        {
            float waveMult = Mathf.Pow(DefGrowthPerWave, Mathf.Max(0, wave - 1));
            float dayMult = Mathf.Pow(DefGrowthPerDay, Mathf.Max(0, day - 1));
            return waveMult * dayMult;
        }

        /// <summary>
        /// 获取某波的丧尸数量
        /// </summary>
        public int GetZombieCount(int wave)
        {
            float count = BaseZombieCount + (wave - 1) * CountGrowthPerWave;
            return Mathf.Min(Mathf.FloorToInt(count), MaxZombiesPerWave);
        }

        /// <summary>
        /// 检查是否是BOSS波
        /// </summary>
        public bool IsBossWave(int wave, out BossTier tier)
        {
            tier = BossTier.Normal;

            if (wave % 20 != 0) return false;

            // 里程碑BOSS: 第100/200/300波
            if (wave % 100 == 0)
            {
                tier = BossTier.Lord; // 领主级
            }
            else if (wave % 60 == 0)
            {
                tier = BossTier.Elite; // 精英级
            }
            else
            {
                tier = BossTier.Normal; // 普通级
            }

            return true;
        }

        #endregion
    }
}
