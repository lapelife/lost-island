using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Survivors
{
    /// <summary>
    /// 幸存者配置工厂
    /// 提供所有幸存者的默认配置
    /// </summary>
    public static class SurvivorFactory
    {
        private static Dictionary<string, SurvivorData> _survivorCache;
        private static Dictionary<SurvivorRarity, List<SurvivorData>> _byRarity;
        private static Dictionary<SurvivorClass, List<SurvivorData>> _byClass;

        static SurvivorFactory()
        {
            _survivorCache = new Dictionary<string, SurvivorData>();
            _byRarity = new Dictionary<SurvivorRarity, List<SurvivorData>>();
            _byClass = new Dictionary<SurvivorClass, List<SurvivorData>>();

            foreach (SurvivorRarity r in System.Enum.GetValues(typeof(SurvivorRarity)))
                _byRarity[r] = new List<SurvivorData>();

            foreach (SurvivorClass c in System.Enum.GetValues(typeof(SurvivorClass)))
                _byClass[c] = new List<SurvivorData>();

            InitializeAllSurvivors();
        }

        /// <summary>
        /// 获取指定ID的幸存者
        /// </summary>
        public static SurvivorData GetSurvivor(string id)
        {
            _survivorCache.TryGetValue(id, out var data);
            return data;
        }

        /// <summary>
        /// 获取所有幸存者
        /// </summary>
        public static List<SurvivorData> GetAllSurvivors()
        {
            return new List<SurvivorData>(_survivorCache.Values);
        }

        /// <summary>
        /// 按稀有度获取
        /// </summary>
        public static List<SurvivorData> GetByRarity(SurvivorRarity rarity)
        {
            return new List<SurvivorData>(_byRarity[rarity]);
        }

        /// <summary>
        /// 按职业获取
        /// </summary>
        public static List<SurvivorData> GetByClass(SurvivorClass cls)
        {
            return new List<SurvivorData>(_byClass[cls]);
        }

        /// <summary>
        /// 幸存者总数
        /// </summary>
        public static int TotalCount => _survivorCache.Count;

        private static void Register(SurvivorData data)
        {
            _survivorCache[data.SurvivorId] = data;
            _byRarity[data.Rarity].Add(data);
            _byClass[data.Class].Add(data);
        }

        #region 初始化所有幸存者

        private static void InitializeAllSurvivors()
        {
            // ===== 白色（普通）3个 =====
            InitSurvivor_Warrior_White();
            InitSurvivor_Archer_White();
            InitSurvivor_Scout_White();

            // ===== 蓝色（精英）3个 =====
            InitSurvivor_Warrior_Blue();
            InitSurvivor_Medic_Blue();
            InitSurvivor_Guard_Blue();

            // ===== 紫色（稀有）3个 =====
            InitSurvivor_Archer_Purple();
            InitSurvivor_Engineer_Purple();
            InitSurvivor_Guard_Purple();

            // ===== 金色（传说）3个 =====
            InitSurvivor_Warrior_Gold();
            InitSurvivor_Medic_Gold();
            InitSurvivor_Engineer_Gold();
        }

        #endregion

        #region 白色幸存者

        private static void InitSurvivor_Warrior_White()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_warrior_white";
            s.SurvivorName = "新兵";
            s.Description = "刚加入的新兵，基础战斗力。";
            s.Class = SurvivorClass.Warrior;
            s.Rarity = SurvivorRarity.Common;

            s.BaseHP = 200f;
            s.BaseATK = 20f;
            s.BaseDEF = 8f;
            s.BaseSpeed = 2.0f;
            s.BaseCritRate = 0.05f;
            s.BaseCritDamage = 1.5f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.08f;
            s.ATKGrowth = 0.06f;
            s.DEFGrowth = 0.05f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "挥砍",
                SkillType = SurvivorSkillType.Active,
                Description = "对敌人造成150%伤害",
                UnlockLevel = 1,
                DamageMultiplier = 1.5f,
                Cooldown = 5f,
                Range = 2f
            });

            Register(s);
        }

        private static void InitSurvivor_Archer_White()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_archer_white";
            s.SurvivorName = "猎人";
            s.Description = "擅长远程射击的猎人。";
            s.Class = SurvivorClass.Archer;
            s.Rarity = SurvivorRarity.Common;

            s.BaseHP = 150f;
            s.BaseATK = 25f;
            s.BaseDEF = 5f;
            s.BaseSpeed = 2.5f;
            s.BaseCritRate = 0.10f;
            s.BaseCritDamage = 1.5f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.07f;
            s.ATKGrowth = 0.07f;
            s.DEFGrowth = 0.04f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "精准射击",
                SkillType = SurvivorSkillType.Active,
                Description = "远程射击造成200%伤害",
                UnlockLevel = 1,
                DamageMultiplier = 2.0f,
                Cooldown = 6f,
                Range = 8f
            });

            Register(s);
        }

        private static void InitSurvivor_Scout_White()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_scout_white";
            s.SurvivorName = "侦察兵";
            s.Description = "移动迅速的侦察兵。";
            s.Class = SurvivorClass.Scout;
            s.Rarity = SurvivorRarity.Common;

            s.BaseHP = 120f;
            s.BaseATK = 18f;
            s.BaseDEF = 4f;
            s.BaseSpeed = 3.5f;
            s.BaseCritRate = 0.08f;
            s.BaseCritDamage = 1.6f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.06f;
            s.ATKGrowth = 0.06f;
            s.DEFGrowth = 0.04f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "疾跑",
                SkillType = SurvivorSkillType.Passive,
                Description = "移动速度提升",
                UnlockLevel = 1,
                AttrType = AttrType.MoveSpeed,
                ValuePerLevel = 0.15f,
                IsPercentage = true
            });

            Register(s);
        }

        #endregion

        #region 蓝色幸存者

        private static void InitSurvivor_Warrior_Blue()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_warrior_blue";
            s.SurvivorName = "老兵";
            s.Description = "身经百战的老兵，战斗经验丰富。";
            s.Class = SurvivorClass.Warrior;
            s.Rarity = SurvivorRarity.Rare;

            s.BaseHP = 350f;
            s.BaseATK = 35f;
            s.BaseDEF = 15f;
            s.BaseSpeed = 2.2f;
            s.BaseCritRate = 0.08f;
            s.BaseCritDamage = 1.6f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.09f;
            s.ATKGrowth = 0.07f;
            s.DEFGrowth = 0.06f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "重击",
                SkillType = SurvivorSkillType.Active,
                Description = "造成180%伤害并击退敌人",
                UnlockLevel = 1,
                DamageMultiplier = 1.8f,
                Cooldown = 5f,
                Range = 2.5f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "战斗经验",
                SkillType = SurvivorSkillType.Passive,
                Description = "攻击力提升",
                UnlockLevel = 10,
                AttrType = AttrType.ATK,
                ValuePerLevel = 0.10f,
                IsPercentage = true
            });

            Register(s);
        }

        private static void InitSurvivor_Medic_Blue()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_medic_blue";
            s.SurvivorName = "护士";
            s.Description = "能够治疗队友的医护人员。";
            s.Class = SurvivorClass.Medic;
            s.Rarity = SurvivorRarity.Rare;

            s.BaseHP = 200f;
            s.BaseATK = 15f;
            s.BaseDEF = 8f;
            s.BaseSpeed = 2.3f;
            s.BaseCritRate = 0.05f;
            s.BaseCritDamage = 1.5f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.07f;
            s.ATKGrowth = 0.05f;
            s.DEFGrowth = 0.05f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "治疗术",
                SkillType = SurvivorSkillType.Active,
                Description = "治疗附近队友",
                UnlockLevel = 1,
                DamageMultiplier = 0f,
                EffectValue = 0.2f, // 治疗20%最大HP
                Cooldown = 8f,
                Range = 4f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "医疗知识",
                SkillType = SurvivorSkillType.Passive,
                Description = "全队最大HP提升",
                UnlockLevel = 10,
                AttrType = AttrType.MaxHP,
                ValuePerLevel = 0.08f,
                IsPercentage = true
            });

            Register(s);
        }

        private static void InitSurvivor_Guard_Blue()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_guard_blue";
            s.SurvivorName = "保安";
            s.Description = "前保安，擅长防御。";
            s.Class = SurvivorClass.Guard;
            s.Rarity = SurvivorRarity.Rare;

            s.BaseHP = 400f;
            s.BaseATK = 20f;
            s.BaseDEF = 25f;
            s.BaseSpeed = 1.8f;
            s.BaseCritRate = 0.03f;
            s.BaseCritDamage = 1.4f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.10f;
            s.ATKGrowth = 0.05f;
            s.DEFGrowth = 0.08f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "嘲讽",
                SkillType = SurvivorSkillType.Active,
                Description = "吸引敌人攻击自己",
                UnlockLevel = 1,
                DamageMultiplier = 0.5f,
                Cooldown = 10f,
                Range = 3f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "铁壁",
                SkillType = SurvivorSkillType.Passive,
                Description = "防御力提升",
                UnlockLevel = 10,
                AttrType = AttrType.DEF,
                ValuePerLevel = 0.15f,
                IsPercentage = true
            });

            Register(s);
        }

        #endregion

        #region 紫色幸存者

        private static void InitSurvivor_Archer_Purple()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_archer_purple";
            s.SurvivorName = "神射手";
            s.Description = "百步穿杨的神射手。";
            s.Class = SurvivorClass.Archer;
            s.Rarity = SurvivorRarity.Epic;

            s.BaseHP = 300f;
            s.BaseATK = 55f;
            s.BaseDEF = 12f;
            s.BaseSpeed = 2.8f;
            s.BaseCritRate = 0.20f;
            s.BaseCritDamage = 1.8f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.08f;
            s.ATKGrowth = 0.08f;
            s.DEFGrowth = 0.05f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "穿透箭",
                SkillType = SurvivorSkillType.Active,
                Description = "穿透多个敌人造成220%伤害",
                UnlockLevel = 1,
                DamageMultiplier = 2.2f,
                Cooldown = 5f,
                Range = 10f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "致命一击",
                SkillType = SurvivorSkillType.Passive,
                Description = "暴击伤害提升",
                UnlockLevel = 10,
                AttrType = AttrType.CritDamage,
                ValuePerLevel = 0.12f,
                IsPercentage = true
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_3",
                SkillName = "鹰眼",
                SkillType = SurvivorSkillType.Passive,
                Description = "暴击率提升",
                UnlockLevel = 20,
                AttrType = AttrType.CritRate,
                ValuePerLevel = 0.05f,
                IsPercentage = true
            });

            Register(s);
        }

        private static void InitSurvivor_Engineer_Purple()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_engineer_purple";
            s.SurvivorName = "机械师";
            s.Description = "擅长修理和建造的机械师。";
            s.Class = SurvivorClass.Engineer;
            s.Rarity = SurvivorRarity.Epic;

            s.BaseHP = 250f;
            s.BaseATK = 30f;
            s.BaseDEF = 15f;
            s.BaseSpeed = 2.0f;
            s.BaseCritRate = 0.05f;
            s.BaseCritDamage = 1.5f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.07f;
            s.ATKGrowth = 0.06f;
            s.DEFGrowth = 0.06f;

            s.BuildingSpeedBonus = 0.25f;
            s.ProductionBonus = 0.20f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "炮台",
                SkillType = SurvivorSkillType.Active,
                Description = "部署临时炮台攻击敌人",
                UnlockLevel = 1,
                DamageMultiplier = 1.5f,
                Cooldown = 15f,
                Range = 6f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "工程学",
                SkillType = SurvivorSkillType.Work,
                Description = "建造速度大幅提升",
                UnlockLevel = 10,
                ValuePerLevel = 0.15f,
                IsPercentage = true
            });

            Register(s);
        }

        private static void InitSurvivor_Guard_Purple()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_guard_purple";
            s.SurvivorName = "盾卫";
            s.Description = "手持巨盾的精英守卫。";
            s.Class = SurvivorClass.Guard;
            s.Rarity = SurvivorRarity.Epic;

            s.BaseHP = 600f;
            s.BaseATK = 30f;
            s.BaseDEF = 40f;
            s.BaseSpeed = 1.6f;
            s.BaseCritRate = 0.05f;
            s.BaseCritDamage = 1.4f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.12f;
            s.ATKGrowth = 0.05f;
            s.DEFGrowth = 0.10f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "盾击",
                SkillType = SurvivorSkillType.Active,
                Description = "用盾牌猛击造成伤害并眩晕",
                UnlockLevel = 1,
                DamageMultiplier = 1.2f,
                EffectValue = 1f, // 眩晕1秒
                Cooldown = 8f,
                Range = 2f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "坚守",
                SkillType = SurvivorSkillType.Passive,
                Description = "最大HP提升",
                UnlockLevel = 10,
                AttrType = AttrType.MaxHP,
                ValuePerLevel = 0.12f,
                IsPercentage = true
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_3",
                SkillName = "反伤",
                SkillType = SurvivorSkillType.Passive,
                Description = "反弹部分伤害",
                UnlockLevel = 20,
                AttrType = AttrType.DamageReduction,
                ValuePerLevel = 0.05f,
                IsPercentage = true
            });

            Register(s);
        }

        #endregion

        #region 金色幸存者

        private static void InitSurvivor_Warrior_Gold()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_warrior_gold";
            s.SurvivorName = "战神";
            s.Description = "传说中的战神，无人能挡。";
            s.Class = SurvivorClass.Warrior;
            s.Rarity = SurvivorRarity.Legendary;

            s.BaseHP = 800f;
            s.BaseATK = 80f;
            s.BaseDEF = 30f;
            s.BaseSpeed = 2.5f;
            s.BaseCritRate = 0.15f;
            s.BaseCritDamage = 2.0f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.12f;
            s.ATKGrowth = 0.10f;
            s.DEFGrowth = 0.08f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "旋风斩",
                SkillType = SurvivorSkillType.Active,
                Description = "对周围敌人造成250%范围伤害",
                UnlockLevel = 1,
                DamageMultiplier = 2.5f,
                Cooldown = 6f,
                Range = 3.5f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "战神之力",
                SkillType = SurvivorSkillType.Passive,
                Description = "攻击力大幅提升",
                UnlockLevel = 10,
                AttrType = AttrType.ATK,
                ValuePerLevel = 0.15f,
                IsPercentage = true
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_3",
                SkillName = "不屈意志",
                SkillType = SurvivorSkillType.Passive,
                Description = "生命越低攻击越高",
                UnlockLevel = 25,
                AttrType = AttrType.CritRate,
                ValuePerLevel = 0.08f,
                IsPercentage = true
            });

            Register(s);
        }

        private static void InitSurvivor_Medic_Gold()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_medic_gold";
            s.SurvivorName = "圣女";
            s.Description = "拥有神圣力量的治疗者。";
            s.Class = SurvivorClass.Medic;
            s.Rarity = SurvivorRarity.Legendary;

            s.BaseHP = 500f;
            s.BaseATK = 35f;
            s.BaseDEF = 20f;
            s.BaseSpeed = 2.5f;
            s.BaseCritRate = 0.08f;
            s.BaseCritDamage = 1.5f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.10f;
            s.ATKGrowth = 0.07f;
            s.DEFGrowth = 0.07f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "神圣治愈",
                SkillType = SurvivorSkillType.Active,
                Description = "治愈全队30%最大HP",
                UnlockLevel = 1,
                DamageMultiplier = 0f,
                EffectValue = 0.30f,
                Cooldown = 10f,
                Range = 6f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "圣光庇护",
                SkillType = SurvivorSkillType.Passive,
                Description = "全队防御提升",
                UnlockLevel = 10,
                AttrType = AttrType.DEF,
                ValuePerLevel = 0.12f,
                IsPercentage = true
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_3",
                SkillName = "神圣恩典",
                SkillType = SurvivorSkillType.Passive,
                Description = "全队生命上限提升",
                UnlockLevel = 25,
                AttrType = AttrType.MaxHP,
                ValuePerLevel = 0.15f,
                IsPercentage = true
            });

            Register(s);
        }

        private static void InitSurvivor_Engineer_Gold()
        {
            var s = ScriptableObject.CreateInstance<SurvivorData>();
            s.SurvivorId = "survivor_engineer_gold";
            s.SurvivorName = "科学狂人";
            s.Description = "天才科学家，发明各种强力装置。";
            s.Class = SurvivorClass.Engineer;
            s.Rarity = SurvivorRarity.Legendary;

            s.BaseHP = 450f;
            s.BaseATK = 60f;
            s.BaseDEF = 25f;
            s.BaseSpeed = 2.2f;
            s.BaseCritRate = 0.10f;
            s.BaseCritDamage = 1.7f;

            s.MaxLevel = 50;
            s.HPGrowth = 0.09f;
            s.ATKGrowth = 0.09f;
            s.DEFGrowth = 0.07f;

            s.BuildingSpeedBonus = 0.50f;
            s.ProductionBonus = 0.40f;
            s.ResearchBonus = 0.30f;

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_1",
                SkillName = "激光炮",
                SkillType = SurvivorSkillType.Active,
                Description = "发射激光造成300%穿透伤害",
                UnlockLevel = 1,
                DamageMultiplier = 3.0f,
                Cooldown = 8f,
                Range = 12f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_2",
                SkillName = "力场护盾",
                SkillType = SurvivorSkillType.Active,
                Description = "为全队提供护盾",
                UnlockLevel = 10,
                DamageMultiplier = 0f,
                EffectValue = 0.25f, // 25%最大HP的护盾
                Cooldown = 15f,
                Range = 5f
            });

            s.Skills.Add(new SurvivorSkillEntry
            {
                SkillId = "skill_3",
                SkillName = "科技加持",
                SkillType = SurvivorSkillType.Work,
                Description = "全局产出大幅提升",
                UnlockLevel = 20,
                ValuePerLevel = 0.20f,
                IsPercentage = true
            });

            Register(s);
        }

        #endregion
    }
}
