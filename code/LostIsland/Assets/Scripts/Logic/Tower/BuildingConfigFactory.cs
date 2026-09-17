using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 建筑配置工厂
    /// 提供所有建筑的默认配置（无需ScriptableObject即可运行）
    /// </summary>
    public static class BuildingConfigFactory
    {
        private static Dictionary<BuildingType, BuildingConfigSO> _configCache;

        static BuildingConfigFactory()
        {
            _configCache = new Dictionary<BuildingType, BuildingConfigSO>();
            InitializeAllConfigs();
        }

        /// <summary>
        /// 获取指定类型的建筑配置
        /// </summary>
        public static BuildingConfigSO GetConfig(BuildingType type)
        {
            if (_configCache.TryGetValue(type, out var config))
                return config;

            Debug.LogWarning($"[BuildingConfigFactory] 未找到建筑配置: {type}");
            return null;
        }

        /// <summary>
        /// 获取所有建筑配置
        /// </summary>
        public static List<BuildingConfigSO> GetAllConfigs()
        {
            return new List<BuildingConfigSO>(_configCache.Values);
        }

        /// <summary>
        /// 获取指定分类的建筑配置
        /// </summary>
        public static List<BuildingConfigSO> GetConfigsByCategory(BuildingCategory category)
        {
            List<BuildingConfigSO> result = new List<BuildingConfigSO>();
            foreach (var kvp in _configCache)
            {
                if (kvp.Value.Category == category)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// 初始化所有建筑配置
        /// </summary>
        private static void InitializeAllConfigs()
        {
            // ===== 防御类（4种）=====
            InitMachineGun();
            InitCannon();
            InitArrowTower();
            InitTeslaTower();

            // ===== 资源类（3种）=====
            InitScrapRecycler();
            InitFoodStorage();
            InitCrystalRefinery();

            // ===== 功能类（6种）=====
            InitGenerator();
            InitMedBay();
            InitWorkshop();
            InitResearchLab();
            InitSurveillanceRoom();
            InitSearchlight();
        }

        #region 防御类建筑

        private static void InitMachineGun()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "defense_machinegun";
            config.BuildingName = "机枪塔";
            config.Description = "快速射击的防御塔，对单体目标持续输出。攻速快，伤害中等。";
            config.BuildingType = BuildingType.MachineGun;
            config.Category = BuildingCategory.Defense;
            config.UnlockFloor = 1;  // 1F
            config.UnlockDay = 1;
            config.MaxLevel = 5;

            config.BuildCostScrap = 80;
            config.BuildCostGrowth = 0.25f;
            config.BuildPowerCost = 3;
            config.PowerCostPerLevel = 1;

            config.BaseDamage = 8f;
            config.DamagePerLevel = 4f;
            config.BaseAttackSpeed = 2.5f;    // 每秒2.5发
            config.AttackSpeedPerLevel = 0.3f;
            config.BaseRange = 6f;
            config.RangePerLevel = 0.5f;
            config.Targeting = TargetingStrategy.Nearest;
            config.AttackType = AttackType.SingleShot;

            _configCache[BuildingType.MachineGun] = config;
        }

        private static void InitCannon()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "defense_cannon";
            config.BuildingName = "炮塔";
            config.Description = "发射炮弹造成范围爆炸伤害。攻速慢，伤害高，适合清群。";
            config.BuildingType = BuildingType.Cannon;
            config.Category = BuildingCategory.Defense;
            config.UnlockFloor = 1;  // 1F
            config.UnlockDay = 2;
            config.MaxLevel = 5;

            config.BuildCostScrap = 150;
            config.BuildCostGrowth = 0.3f;
            config.BuildPowerCost = 5;
            config.PowerCostPerLevel = 1;

            config.BaseDamage = 30f;
            config.DamagePerLevel = 12f;
            config.BaseAttackSpeed = 0.8f;
            config.AttackSpeedPerLevel = 0.1f;
            config.BaseRange = 7f;
            config.RangePerLevel = 0.5f;
            config.Targeting = TargetingStrategy.FirstInPath;
            config.AttackType = AttackType.AOE;
            config.AoeRadius = 2.5f;

            _configCache[BuildingType.Cannon] = config;
        }

        private static void InitArrowTower()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "defense_arrow";
            config.BuildingName = "箭塔";
            config.Description = "发射穿透箭矢，可同时伤害一条直线上的多个敌人。";
            config.BuildingType = BuildingType.ArrowTower;
            config.Category = BuildingCategory.Defense;
            config.UnlockFloor = 3;  // 3F
            config.UnlockDay = 4;
            config.MaxLevel = 5;

            config.BuildCostScrap = 120;
            config.BuildCostGrowth = 0.25f;
            config.BuildPowerCost = 4;
            config.PowerCostPerLevel = 1;

            config.BaseDamage = 15f;
            config.DamagePerLevel = 7f;
            config.BaseAttackSpeed = 1.2f;
            config.AttackSpeedPerLevel = 0.15f;
            config.BaseRange = 8f;
            config.RangePerLevel = 0.6f;
            config.Targeting = TargetingStrategy.Farthest;
            config.AttackType = AttackType.Piercing;
            config.PierceCount = 3;

            _configCache[BuildingType.ArrowTower] = config;
        }

        private static void InitTeslaTower()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "defense_tesla";
            config.BuildingName = "电击塔";
            config.Description = "释放链式闪电，在多个敌人之间跳跃传导。";
            config.BuildingType = BuildingType.TeslaTower;
            config.Category = BuildingCategory.Defense;
            config.UnlockFloor = 5;  // 5F
            config.UnlockDay = 8;
            config.MaxLevel = 5;

            config.BuildCostScrap = 200;
            config.BuildCostGrowth = 0.3f;
            config.BuildPowerCost = 7;
            config.PowerCostPerLevel = 2;

            config.BaseDamage = 12f;
            config.DamagePerLevel = 6f;
            config.BaseAttackSpeed = 1.0f;
            config.AttackSpeedPerLevel = 0.1f;
            config.BaseRange = 5f;
            config.RangePerLevel = 0.4f;
            config.Targeting = TargetingStrategy.Nearest;
            config.AttackType = AttackType.Chain;
            config.ChainCount = 3;
            config.ChainRange = 3f;

            _configCache[BuildingType.TeslaTower] = config;
        }

        #endregion

        #region 资源类建筑

        private static void InitScrapRecycler()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "resource_scrap";
            config.BuildingName = "废料回收站";
            config.Description = "增加丧尸掉落的腐肉数量。每级+10%腐肉掉落。";
            config.BuildingType = BuildingType.ScrapRecycler;
            config.Category = BuildingCategory.Resource;
            config.UnlockFloor = 0;  // B1F
            config.UnlockDay = 1;
            config.MaxLevel = 5;

            config.BuildCostScrap = 60;
            config.BuildCostGrowth = 0.2f;
            config.BuildPowerCost = 2;
            config.PowerCostPerLevel = 0;

            // 腐肉掉落加成（百分比）
            config.Bonuses.Add(new BuildingBonusEntry
            {
                AttrType = AttrType.Unknown, // TODO: 添加FleshDrop属性
                IsFlat = false,
                ValuePerLevel = 0.10f, // 每级+10%
                Layer = AttrLayer.Pct_Tower
            });

            config.BaseProduction = 0.5f;  // 每秒少量产出
            config.ProductionPerLevel = 0.2f;

            _configCache[BuildingType.ScrapRecycler] = config;
        }

        private static void InitFoodStorage()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "resource_food";
            config.BuildingName = "食物储藏室";
            config.Description = "增加食物储存上限，延长生存时间。";
            config.BuildingType = BuildingType.FoodStorage;
            config.Category = BuildingCategory.Resource;
            config.UnlockFloor = 2;  // 2F
            config.UnlockDay = 2;
            config.MaxLevel = 5;

            config.BuildCostScrap = 40;
            config.BuildCostGrowth = 0.15f;
            config.BuildPowerCost = 1;
            config.PowerCostPerLevel = 0;

            config.BaseProduction = 0.2f;
            config.ProductionPerLevel = 0.1f;

            _configCache[BuildingType.FoodStorage] = config;
        }

        private static void InitCrystalRefinery()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "resource_crystal";
            config.BuildingName = "晶核提炼器";
            config.Description = "增加丧尸掉落晶核的概率。稀有资源建筑。";
            config.BuildingType = BuildingType.CrystalRefinery;
            config.Category = BuildingCategory.Resource;
            config.UnlockFloor = 4;  // 4F
            config.UnlockDay = 6;
            config.MaxLevel = 5;

            config.BuildCostScrap = 200;
            config.BuildCostGrowth = 0.3f;
            config.BuildPowerCost = 5;
            config.PowerCostPerLevel = 1;

            _configCache[BuildingType.CrystalRefinery] = config;
        }

        #endregion

        #region 功能类建筑

        private static void InitGenerator()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "utility_generator";
            config.BuildingName = "发电机";
            config.Description = "为灯塔提供电力。每级+5电力上限。是建造策略的核心。";
            config.BuildingType = BuildingType.Generator;
            config.Category = BuildingCategory.Utility;
            config.UnlockFloor = 0;  // B1F
            config.UnlockDay = 1;
            config.MaxLevel = 5;

            config.BuildCostScrap = 100;
            config.BuildCostGrowth = 0.25f;
            config.BuildPowerCost = 0;    // 发电机不耗电
            config.PowerCostPerLevel = 0;

            config.BasePowerOutput = 10;
            config.PowerOutputPerLevel = 5;

            _configCache[BuildingType.Generator] = config;
        }

        private static void InitMedBay()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "utility_medbay";
            config.BuildingName = "医疗室";
            config.Description = "增加生命上限并提供回血能力。每级+15%生命上限。";
            config.BuildingType = BuildingType.MedBay;
            config.Category = BuildingCategory.Utility;
            config.UnlockFloor = 2;  // 2F
            config.UnlockDay = 2;
            config.MaxLevel = 5;

            config.BuildCostScrap = 80;
            config.BuildCostGrowth = 0.2f;
            config.BuildPowerCost = 3;
            config.PowerCostPerLevel = 1;

            config.Bonuses.Add(new BuildingBonusEntry
            {
                AttrType = AttrType.MaxHP,
                IsFlat = false,
                ValuePerLevel = 0.15f,
                Layer = AttrLayer.Pct_Tower
            });

            _configCache[BuildingType.MedBay] = config;
        }

        private static void InitWorkshop()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "utility_workshop";
            config.BuildingName = "工坊";
            config.Description = "改良武器装备，增加攻击力。每级+10%攻击力。";
            config.BuildingType = BuildingType.Workshop;
            config.Category = BuildingCategory.Utility;
            config.UnlockFloor = 3;  // 3F
            config.UnlockDay = 4;
            config.MaxLevel = 5;

            config.BuildCostScrap = 100;
            config.BuildCostGrowth = 0.25f;
            config.BuildPowerCost = 3;
            config.PowerCostPerLevel = 1;

            config.Bonuses.Add(new BuildingBonusEntry
            {
                AttrType = AttrType.ATK,
                IsFlat = false,
                ValuePerLevel = 0.10f,
                Layer = AttrLayer.Pct_Tower
            });

            _configCache[BuildingType.Workshop] = config;
        }

        private static void InitResearchLab()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "utility_research";
            config.BuildingName = "研究室";
            config.Description = "进行科学研究，解锁新科技和建筑。";
            config.BuildingType = BuildingType.ResearchLab;
            config.Category = BuildingCategory.Utility;
            config.UnlockFloor = 3;  // 3F
            config.UnlockDay = 4;
            config.MaxLevel = 5;

            config.BuildCostScrap = 150;
            config.BuildCostGrowth = 0.25f;
            config.BuildPowerCost = 4;
            config.PowerCostPerLevel = 1;

            _configCache[BuildingType.ResearchLab] = config;
        }

        private static void InitSurveillanceRoom()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "utility_surveillance";
            config.BuildingName = "监控室";
            config.Description = "扩大视野范围，增加防御塔索敌范围。每级+8%射程。";
            config.BuildingType = BuildingType.SurveillanceRoom;
            config.Category = BuildingCategory.Utility;
            config.UnlockFloor = 5;  // 5F
            config.UnlockDay = 8;
            config.MaxLevel = 5;

            config.BuildCostScrap = 120;
            config.BuildCostGrowth = 0.2f;
            config.BuildPowerCost = 3;
            config.PowerCostPerLevel = 1;

            _configCache[BuildingType.SurveillanceRoom] = config;
        }

        private static void InitSearchlight()
        {
            var config = ScriptableObject.CreateInstance<BuildingConfigSO>();
            config.BuildingId = "utility_searchlight";
            config.BuildingName = "探照灯";
            config.Description = "塔顶固定建筑，照亮周围区域。无法建造或拆除。";
            config.BuildingType = BuildingType.Searchlight;
            config.Category = BuildingCategory.Utility;
            config.UnlockFloor = 6;  // 塔顶
            config.UnlockDay = 1;
            config.MaxLevel = 3;

            config.BuildCostScrap = 0;
            config.BuildCostGrowth = 0f;
            config.BuildPowerCost = 3;
            config.PowerCostPerLevel = 1;

            _configCache[BuildingType.Searchlight] = config;
        }

        #endregion
    }
}
