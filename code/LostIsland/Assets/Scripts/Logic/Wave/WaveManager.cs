using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;
using LostIsland.Logic.Battle;

// 使用别名解决 BossTier 二义性
using BossTier = LostIsland.Logic.Battle.BossTier;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// 波次管理器：波次生成与调度的核心管理类
    /// 负责波次间隔计算、属性成长、出怪表、BOSS判定
    /// </summary>
    public class WaveManager : MonoSingleton<WaveManager>
    {
        #region 配置

        [Header("配置")]
        [SerializeField] private WaveConfigSO _waveConfig;

        [Header("当前进度")]
        [SerializeField] private int _currentDay = 1;
        [SerializeField] private int _currentWave = 0;

        #endregion

        #region 属性

        /// <summary>
        /// 波次配置
        /// </summary>
        public WaveConfigSO Config => _waveConfig;

        /// <summary>
        /// 当前天数
        /// </summary>
        public int CurrentDay => _currentDay;

        /// <summary>
        /// 当前波数
        /// </summary>
        public int CurrentWave => _currentWave;

        /// <summary>
        /// 总波数
        /// </summary>
        public int TotalWaves => _waveConfig != null ? _waveConfig.TotalWaves : 300;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            if (_waveConfig == null)
            {
                Debug.LogWarning("[WaveManager] 波次配置未设置，使用默认值");
            }
        }

        /// <summary>
        /// 设置波次配置
        /// </summary>
        public void SetConfig(WaveConfigSO config)
        {
            _waveConfig = config;
        }

        /// <summary>
        /// 开始新的一天
        /// </summary>
        public void StartDay(int day)
        {
            _currentDay = day;
            _currentWave = 0;
            Debug.Log($"[WaveManager] 第{day}天开始");
        }

        /// <summary>
        /// 开始新的一波
        /// </summary>
        public void StartWave(int wave)
        {
            _currentWave = wave;
            Debug.Log($"[WaveManager] 第{wave}波开始");
        }

        #endregion

        #region 波次间隔

        /// <summary>
        /// 获取指定波的间隔时间（秒）
        /// 上凸曲线：前期慢后期快
        /// </summary>
        public float GetWaveInterval(int wave)
        {
            if (_waveConfig == null) return 20f;
            return _waveConfig.GetWaveInterval(wave);
        }

        /// <summary>
        /// 获取下一波的间隔
        /// </summary>
        public float GetNextWaveInterval()
        {
            return GetWaveInterval(_currentWave + 1);
        }

        #endregion

        #region 属性成长

        /// <summary>
        /// 获取当前波次的HP成长倍率
        /// </summary>
        public float GetHpMultiplier(int wave = -1, int day = -1)
        {
            if (wave < 0) wave = _currentWave;
            if (day < 0) day = _currentDay;
            if (_waveConfig == null) return 1f;
            return _waveConfig.GetHpMultiplier(wave, day);
        }

        /// <summary>
        /// 获取当前波次的ATK成长倍率
        /// </summary>
        public float GetAtkMultiplier(int wave = -1, int day = -1)
        {
            if (wave < 0) wave = _currentWave;
            if (day < 0) day = _currentDay;
            if (_waveConfig == null) return 1f;
            return _waveConfig.GetAtkMultiplier(wave, day);
        }

        /// <summary>
        /// 获取当前波次的DEF成长倍率
        /// </summary>
        public float GetDefMultiplier(int wave = -1, int day = -1)
        {
            if (wave < 0) wave = _currentWave;
            if (day < 0) day = _currentDay;
            if (_waveConfig == null) return 1f;
            return _waveConfig.GetDefMultiplier(wave, day);
        }

        #endregion

        #region 丧尸数量与类型

        /// <summary>
        /// 获取指定波的丧尸总数
        /// </summary>
        public int GetZombieCount(int wave = -1)
        {
            if (wave < 0) wave = _currentWave;
            if (_waveConfig == null) return 5;
            return _waveConfig.GetZombieCount(wave);
        }

        /// <summary>
        /// 生成指定波的丧尸列表（类型+数量）
        /// </summary>
        /// <param name="wave">波数</param>
        /// <returns>丧尸类型列表</returns>
        public List<ZombieType> GenerateWaveZombies(int wave)
        {
            List<ZombieType> result = new List<ZombieType>();

            if (_waveConfig == null || _waveConfig.SpawnTable == null || _waveConfig.SpawnTable.Length == 0)
            {
                // 没有配置表，全是普通丧尸
                int count = GetZombieCount(wave);
                for (int i = 0; i < count; i++)
                {
                    result.Add(ZombieType.Normal);
                }
                return result;
            }

            int totalCount = GetZombieCount(wave);

            // 收集当前波可用的丧尸类型及权重
            List<SpawnEntry> available = new List<SpawnEntry>();
            float totalWeight = 0f;

            foreach (var entry in _waveConfig.SpawnTable)
            {
                if (wave >= entry.StartWave && entry.SpawnWeight > 0f)
                {
                    available.Add(entry);
                    totalWeight += entry.SpawnWeight;
                }
            }

            if (available.Count == 0 || totalWeight <= 0f)
            {
                // 没有可用类型，用普通丧尸
                for (int i = 0; i < totalCount; i++)
                {
                    result.Add(ZombieType.Normal);
                }
                return result;
            }

            // 按权重随机分配
            for (int i = 0; i < totalCount; i++)
            {
                float rand = UnityEngine.Random.value * totalWeight;
                float acc = 0f;

                foreach (var entry in available)
                {
                    acc += entry.SpawnWeight;
                    if (rand <= acc)
                    {
                        result.Add(entry.ZombieType);
                        break;
                    }
                }
            }

            return result;
        }

        #endregion

        #region BOSS判定

        /// <summary>
        /// 检查指定波是否是BOSS波
        /// </summary>
        public bool IsBossWave(int wave, out BossTier tier)
        {
            if (_waveConfig == null)
            {
                tier = BossTier.Normal;
                return wave % 20 == 0;
            }
            return _waveConfig.IsBossWave(wave, out tier);
        }

        /// <summary>
        /// 获取当前波是否是BOSS波
        /// </summary>
        public bool IsCurrentBossWave(out BossTier tier)
        {
            return IsBossWave(_currentWave, out tier);
        }

        /// <summary>
        /// 获取对应波次的BOSS类型
        /// </summary>
        public ZombieType GetBossType(int wave)
        {
            // 按波次循环选择普通BOSS
            int bossIndex = (wave / 20) % 3;

            if (wave == 100) return ZombieType.Boss_FleshGolem;
            if (wave == 200) return ZombieType.Boss_TeslaTitan;
            if (wave == 300) return ZombieType.Boss_IslandLord;

            switch (bossIndex)
            {
                case 0: return ZombieType.Boss_RottenFist;
                case 1: return ZombieType.Boss_PoisonWitch;
                case 2: return ZombieType.Boss_IronButcher;
                default: return ZombieType.Boss_RottenFist;
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 输出波次信息（调试用）
        /// </summary>
        public void DebugWaveInfo(int wave)
        {
            float interval = GetWaveInterval(wave);
            float hpMult = GetHpMultiplier(wave, _currentDay);
            float atkMult = GetAtkMultiplier(wave, _currentDay);
            float defMult = GetDefMultiplier(wave, _currentDay);
            int count = GetZombieCount(wave);
            bool isBoss = IsBossWave(wave, out BossTier tier);

            Debug.Log($"=== 第{wave}波信息 ===");
            Debug.Log($"间隔: {interval:F1}秒");
            Debug.Log($"数量: {count}");
            Debug.Log($"HP倍率: {hpMult:F2}x");
            Debug.Log($"ATK倍率: {atkMult:F2}x");
            Debug.Log($"DEF倍率: {defMult:F2}x");
            Debug.Log($"BOSS波: {isBoss} ({tier})");
        }

        #endregion
    }
}
