using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Data.Config;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// 丧尸配置工厂
    /// 提供默认丧尸配置，也可以从配置数据库加载
    /// </summary>
    public static class ZombieConfigFactory
    {
        // 缓存的配置字典
        private static Dictionary<ZombieType, ZombieConfigSO> _configCache = new Dictionary<ZombieType, ZombieConfigSO>();
        private static bool _initialized = false;

        /// <summary>
        /// 获取丧尸配置
        /// 优先从 ConfigDatabase 加载，没有则使用默认配置
        /// </summary>
        public static ZombieConfigSO GetConfig(ZombieType type)
        {
            if (!_initialized)
            {
                InitDefaultConfigs();
            }

            // 先尝试从配置数据库获取
            var dbConfig = ConfigDatabase.GetConfig<ZombieConfigSO>(type.ToString());
            if (dbConfig != null)
            {
                return dbConfig;
            }

            // 返回默认配置
            if (_configCache.TryGetValue(type, out var config))
            {
                return config;
            }

            // 找不到返回普通丧尸配置
            Debug.LogWarning($"[ZombieConfigFactory] 未找到配置: {type}，使用默认普通丧尸配置");
            return _configCache[ZombieType.Normal];
        }

        /// <summary>
        /// 初始化默认配置
        /// </summary>
        private static void InitDefaultConfigs()
        {
            _configCache.Clear();

            // 普通丧尸
            _configCache[ZombieType.Normal] = CreateNormalConfig();
            // 快速丧尸
            _configCache[ZombieType.Fast] = CreateFastConfig();
            // 重甲丧尸
            _configCache[ZombieType.Heavy] = CreateHeavyConfig();
            // 自爆丧尸
            _configCache[ZombieType.Exploder] = CreateExploderConfig();

            // 精英丧尸
            _configCache[ZombieType.Poison] = CreatePoisonConfig();
            _configCache[ZombieType.Shield] = CreateShieldConfig();
            _configCache[ZombieType.Summoner] = CreateSummonerConfig();
            _configCache[ZombieType.Charger] = CreateChargerConfig();
            _configCache[ZombieType.Healer] = CreateHealerConfig();
            _configCache[ZombieType.Invisible] = CreateInvisibleConfig();

            // BOSS
            _configCache[ZombieType.Boss_RottenFist] = CreateBossRottenFistConfig();
            _configCache[ZombieType.Boss_PoisonWitch] = CreateBossPoisonWitchConfig();
            _configCache[ZombieType.Boss_IronButcher] = CreateBossIronButcherConfig();
            _configCache[ZombieType.Boss_FleshGolem] = CreateBossFleshGolemConfig();
            _configCache[ZombieType.Boss_TeslaTitan] = CreateBossTeslaTitanConfig();
            _configCache[ZombieType.Boss_IslandLord] = CreateBossIslandLordConfig();

            _initialized = true;
        }

        #region 普通丧尸配置

        private static ZombieConfigSO CreateNormalConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Normal";
            config.ZombieType = ZombieType.Normal;
            config.Rarity = ZombieRarity.Normal;
            config.BaseHP = 100f;
            config.BaseATK = 10f;
            config.BaseDEF = 0f;
            config.BaseMoveSpeed = 2f;
            config.BaseAttackSpeed = 1f;
            config.AttackRange = 1.5f;
            config.FleshDrop = 1;
            config.HPMultiplier = 1f;
            config.ATKMultiplier = 1f;
            config.SpeedMultiplier = 1f;
            config.ScaleMultiplier = 1f;
            return config;
        }

        private static ZombieConfigSO CreateFastConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Fast";
            config.ZombieType = ZombieType.Fast;
            config.Rarity = ZombieRarity.Normal;
            config.BaseHP = 60f;
            config.BaseATK = 8f;
            config.BaseDEF = 0f;
            config.BaseMoveSpeed = 3.6f; // 1.8x
            config.BaseAttackSpeed = 1.5f;
            config.AttackRange = 1.2f;
            config.FleshDrop = 1;
            config.HPMultiplier = 0.6f;
            config.ATKMultiplier = 0.8f;
            config.SpeedMultiplier = 1.8f;
            config.ScaleMultiplier = 0.9f;
            return config;
        }

        private static ZombieConfigSO CreateHeavyConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Heavy";
            config.ZombieType = ZombieType.Heavy;
            config.Rarity = ZombieRarity.Normal;
            config.BaseHP = 250f;
            config.BaseATK = 15f;
            config.BaseDEF = 50f;
            config.BaseMoveSpeed = 1.4f; // 0.7x
            config.BaseAttackSpeed = 0.7f;
            config.AttackRange = 1.8f;
            config.FleshDrop = 3;
            config.HPMultiplier = 2.5f;
            config.ATKMultiplier = 1.5f;
            config.SpeedMultiplier = 0.7f;
            config.ScaleMultiplier = 1.3f;
            return config;
        }

        private static ZombieConfigSO CreateExploderConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Exploder";
            config.ZombieType = ZombieType.Exploder;
            config.Rarity = ZombieRarity.Normal;
            config.BaseHP = 80f;
            config.BaseATK = 20f;
            config.BaseDEF = 0f;
            config.BaseMoveSpeed = 2.5f;
            config.BaseAttackSpeed = 0.5f;
            config.AttackRange = 2f;
            config.FleshDrop = 2;
            config.IsExploder = true;
            config.ExplosionRange = 2.5f;
            config.ExplosionDamageMultiplier = 3f;
            config.HPMultiplier = 0.8f;
            config.ATKMultiplier = 2f;
            config.SpeedMultiplier = 1.25f;
            config.ScaleMultiplier = 1.1f;
            return config;
        }

        #endregion

        #region 精英丧尸配置

        private static ZombieConfigSO CreatePoisonConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Poison";
            config.ZombieType = ZombieType.Poison;
            config.Rarity = ZombieRarity.Elite;
            config.BaseHP = 180f;
            config.BaseATK = 12f;
            config.BaseDEF = 10f;
            config.BaseMoveSpeed = 1.8f;
            config.BaseAttackSpeed = 0.8f;
            config.AttackRange = 2f;
            config.FleshDrop = 5;
            config.IsPoisonous = true;
            config.PoisonDuration = 4f;
            config.PoisonDpsPct = 0.03f;
            config.PoisonSlowPct = 0.25f;
            config.HPMultiplier = 1.8f;
            config.ATKMultiplier = 1.2f;
            config.ScaleMultiplier = 1.15f;
            return config;
        }

        private static ZombieConfigSO CreateShieldConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Shield";
            config.ZombieType = ZombieType.Shield;
            config.Rarity = ZombieRarity.Elite;
            config.BaseHP = 200f;
            config.BaseATK = 10f;
            config.BaseDEF = 30f;
            config.BaseMoveSpeed = 1.6f;
            config.BaseAttackSpeed = 0.8f;
            config.AttackRange = 1.5f;
            config.FleshDrop = 5;
            config.HasShield = true;
            config.ShieldHP = 150f;
            config.HPMultiplier = 2f;
            config.ATKMultiplier = 1f;
            config.SpeedMultiplier = 0.8f;
            config.ScaleMultiplier = 1.2f;
            return config;
        }

        private static ZombieConfigSO CreateSummonerConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Summoner";
            config.ZombieType = ZombieType.Summoner;
            config.Rarity = ZombieRarity.Elite;
            config.BaseHP = 150f;
            config.BaseATK = 8f;
            config.BaseDEF = 10f;
            config.BaseMoveSpeed = 1.5f;
            config.BaseAttackSpeed = 0.6f;
            config.AttackRange = 3f;
            config.FleshDrop = 6;
            config.CanSummon = true;
            config.SummonInterval = 12f;
            config.SummonCount = 2;
            config.SummonType = ZombieType.Normal;
            config.HPMultiplier = 1.5f;
            config.ATKMultiplier = 0.8f;
            config.ScaleMultiplier = 1.2f;
            return config;
        }

        private static ZombieConfigSO CreateChargerConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Charger";
            config.ZombieType = ZombieType.Charger;
            config.Rarity = ZombieRarity.Elite;
            config.BaseHP = 220f;
            config.BaseATK = 18f;
            config.BaseDEF = 20f;
            config.BaseMoveSpeed = 2f;
            config.BaseAttackSpeed = 0.8f;
            config.AttackRange = 2f;
            config.FleshDrop = 5;
            config.CanCharge = true;
            config.ChargeDistance = 6f;
            config.ChargeSpeedMultiplier = 3f;
            config.ChargeCooldown = 8f;
            config.HPMultiplier = 2.2f;
            config.ATKMultiplier = 1.8f;
            config.ScaleMultiplier = 1.25f;
            return config;
        }

        private static ZombieConfigSO CreateHealerConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Healer";
            config.ZombieType = ZombieType.Healer;
            config.Rarity = ZombieRarity.Elite;
            config.BaseHP = 160f;
            config.BaseATK = 6f;
            config.BaseDEF = 15f;
            config.BaseMoveSpeed = 1.8f;
            config.BaseAttackSpeed = 0.5f;
            config.AttackRange = 3f;
            config.FleshDrop = 6;
            config.CanHeal = true;
            config.HealRange = 4f;
            config.HealPct = 0.06f;
            config.HealInterval = 2.5f;
            config.HPMultiplier = 1.6f;
            config.ATKMultiplier = 0.6f;
            config.ScaleMultiplier = 1.15f;
            return config;
        }

        private static ZombieConfigSO CreateInvisibleConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Zombie_Invisible";
            config.ZombieType = ZombieType.Invisible;
            config.Rarity = ZombieRarity.Elite;
            config.BaseHP = 120f;
            config.BaseATK = 15f;
            config.BaseDEF = 5f;
            config.BaseMoveSpeed = 2.2f;
            config.BaseAttackSpeed = 1.2f;
            config.AttackRange = 1.5f;
            config.FleshDrop = 5;
            config.CanInvisible = true;
            config.VisibleDistance = 2.5f;
            config.HPMultiplier = 1.2f;
            config.ATKMultiplier = 1.5f;
            config.SpeedMultiplier = 1.1f;
            config.ScaleMultiplier = 1f;
            return config;
        }

        #endregion

        #region BOSS配置

        private static ZombieConfigSO CreateBossRottenFistConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Boss_RottenFist";
            config.ZombieType = ZombieType.Boss_RottenFist;
            config.Rarity = ZombieRarity.Boss;
            config.BaseHP = 2000f;
            config.BaseATK = 30f;
            config.BaseDEF = 30f;
            config.BaseMoveSpeed = 1.5f;
            config.BaseAttackSpeed = 0.6f;
            config.AttackRange = 2.5f;
            config.FleshDrop = 50;
            config.CrystalDropChance = 0.5f;
            config.CrystalDropAmount = 2;
            config.HPMultiplier = 20f;
            config.ATKMultiplier = 3f;
            config.ScaleMultiplier = 2.5f;
            return config;
        }

        private static ZombieConfigSO CreateBossPoisonWitchConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Boss_PoisonWitch";
            config.ZombieType = ZombieType.Boss_PoisonWitch;
            config.Rarity = ZombieRarity.Boss;
            config.BaseHP = 1500f;
            config.BaseATK = 25f;
            config.BaseDEF = 20f;
            config.BaseMoveSpeed = 1.2f;
            config.BaseAttackSpeed = 0.8f;
            config.AttackRange = 4f;
            config.FleshDrop = 45;
            config.CrystalDropChance = 0.5f;
            config.CrystalDropAmount = 2;
            config.IsPoisonous = true;
            config.PoisonDuration = 5f;
            config.PoisonDpsPct = 0.04f;
            config.PoisonSlowPct = 0.35f;
            config.CanSummon = true;
            config.SummonInterval = 15f;
            config.SummonCount = 3;
            config.SummonType = ZombieType.Poison;
            config.HPMultiplier = 15f;
            config.ATKMultiplier = 2.5f;
            config.ScaleMultiplier = 2.2f;
            return config;
        }

        private static ZombieConfigSO CreateBossIronButcherConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Boss_IronButcher";
            config.ZombieType = ZombieType.Boss_IronButcher;
            config.Rarity = ZombieRarity.Boss;
            config.BaseHP = 3000f;
            config.BaseATK = 40f;
            config.BaseDEF = 100f;
            config.BaseMoveSpeed = 1f;
            config.BaseAttackSpeed = 0.5f;
            config.AttackRange = 3f;
            config.FleshDrop = 60;
            config.CrystalDropChance = 0.5f;
            config.CrystalDropAmount = 3;
            config.HasShield = true;
            config.ShieldHP = 1000f;
            config.CanCharge = true;
            config.ChargeDistance = 8f;
            config.ChargeSpeedMultiplier = 2.5f;
            config.ChargeCooldown = 10f;
            config.HPMultiplier = 30f;
            config.ATKMultiplier = 4f;
            config.ScaleMultiplier = 2.8f;
            return config;
        }

        private static ZombieConfigSO CreateBossFleshGolemConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Boss_FleshGolem";
            config.ZombieType = ZombieType.Boss_FleshGolem;
            config.Rarity = ZombieRarity.Boss;
            config.BaseHP = 8000f;
            config.BaseATK = 50f;
            config.BaseDEF = 50f;
            config.BaseMoveSpeed = 1.3f;
            config.BaseAttackSpeed = 0.7f;
            config.AttackRange = 3f;
            config.FleshDrop = 200;
            config.CrystalDropChance = 1f;
            config.CrystalDropAmount = 10;
            config.CanSummon = true;
            config.SummonInterval = 20f;
            config.SummonCount = 4;
            config.SummonType = ZombieType.Heavy;
            config.HPMultiplier = 80f;
            config.ATKMultiplier = 5f;
            config.ScaleMultiplier = 3.5f;
            return config;
        }

        private static ZombieConfigSO CreateBossTeslaTitanConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Boss_TeslaTitan";
            config.ZombieType = ZombieType.Boss_TeslaTitan;
            config.Rarity = ZombieRarity.Boss;
            config.BaseHP = 12000f;
            config.BaseATK = 60f;
            config.BaseDEF = 80f;
            config.BaseMoveSpeed = 1f;
            config.BaseAttackSpeed = 0.6f;
            config.AttackRange = 5f;
            config.FleshDrop = 300;
            config.CrystalDropChance = 1f;
            config.CrystalDropAmount = 20;
            config.HasShield = true;
            config.ShieldHP = 3000f;
            config.HPMultiplier = 120f;
            config.ATKMultiplier = 6f;
            config.ScaleMultiplier = 4f;
            return config;
        }

        private static ZombieConfigSO CreateBossIslandLordConfig()
        {
            var config = ScriptableObject.CreateInstance<ZombieConfigSO>();
            config.name = "Boss_IslandLord";
            config.ZombieType = ZombieType.Boss_IslandLord;
            config.Rarity = ZombieRarity.Boss;
            config.BaseHP = 20000f;
            config.BaseATK = 80f;
            config.BaseDEF = 100f;
            config.BaseMoveSpeed = 1.2f;
            config.BaseAttackSpeed = 0.8f;
            config.AttackRange = 5f;
            config.FleshDrop = 500;
            config.CrystalDropChance = 1f;
            config.CrystalDropAmount = 50;
            config.HasShield = true;
            config.ShieldHP = 5000f;
            config.CanSummon = true;
            config.SummonInterval = 15f;
            config.SummonCount = 5;
            config.SummonType = ZombieType.Heavy;
            config.CanCharge = true;
            config.ChargeDistance = 10f;
            config.ChargeSpeedMultiplier = 2f;
            config.ChargeCooldown = 12f;
            config.HPMultiplier = 200f;
            config.ATKMultiplier = 8f;
            config.ScaleMultiplier = 5f;
            return config;
        }

        #endregion
    }
}
