using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Core;

namespace LostIsland.Logic.Rune
{
    /// <summary>
    /// 符文配置工厂
    /// 提供所有符文的默认配置
    /// </summary>
    public static class RuneFactory
    {
        private static Dictionary<string, RuneData> _runeCache;
        private static Dictionary<Rarity, List<RuneData>> _runesByRarity;
        private static Dictionary<RuneCategory, List<RuneData>> _runesByCategory;

        static RuneFactory()
        {
            _runeCache = new Dictionary<string, RuneData>();
            _runesByRarity = new Dictionary<Rarity, List<RuneData>>();
            _runesByCategory = new Dictionary<RuneCategory, List<RuneData>>();

            foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
                _runesByRarity[r] = new List<RuneData>();

            foreach (RuneCategory c in System.Enum.GetValues(typeof(RuneCategory)))
                _runesByCategory[c] = new List<RuneData>();

            InitializeAllRunes();
        }

        /// <summary>
        /// 获取指定ID的符文
        /// </summary>
        public static RuneData GetRune(string runeId)
        {
            _runeCache.TryGetValue(runeId, out var rune);
            return rune;
        }

        /// <summary>
        /// 获取所有符文
        /// </summary>
        public static List<RuneData> GetAllRunes()
        {
            return new List<RuneData>(_runeCache.Values);
        }

        /// <summary>
        /// 获取指定稀有度的符文
        /// </summary>
        public static List<RuneData> GetRunesByRarity(Rarity rarity)
        {
            return new List<RuneData>(_runesByRarity[rarity]);
        }

        /// <summary>
        /// 获取指定分类的符文
        /// </summary>
        public static List<RuneData> GetRunesByCategory(RuneCategory category)
        {
            return new List<RuneData>(_runesByCategory[category]);
        }

        /// <summary>
        /// 符文总数
        /// </summary>
        public static int TotalRuneCount => _runeCache.Count;

        /// <summary>
        /// 注册符文
        /// </summary>
        private static void RegisterRune(RuneData rune)
        {
            _runeCache[rune.RuneId] = rune;
            _runesByRarity[rune.Rarity].Add(rune);
            _runesByCategory[rune.Category].Add(rune);
        }

        #region 初始化所有符文

        private static void InitializeAllRunes()
        {
            // ===== 攻击类 =====
            InitRune_Atk_White();
            InitRune_Atk_Blue();
            InitRune_Atk_Purple();
            InitRune_Atk_Gold();

            // ===== 防御类 =====
            InitRune_Def_White();
            InitRune_Def_Blue();
            InitRune_Def_Purple();
            InitRune_Def_Gold();

            // ===== 生存类 =====
            InitRune_Hp_White();
            InitRune_Hp_Blue();
            InitRune_Hp_Purple();
            InitRune_Hp_Gold();
        }

        #endregion

        #region 攻击类符文

        private static void InitRune_Atk_White()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_atk_white";
            rune.RuneName = "锋利符文";
            rune.Description = "基础攻击符文，提升攻击力。";
            rune.Rarity = Rarity.Common;
            rune.Category = RuneCategory.Attack;

            rune.MainAttrType = AttrType.ATK;
            rune.MainAttrBasePct = 0.05f; // 1星+5%攻击
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 10, 20, 40, 80 };

            RegisterRune(rune);
        }

        private static void InitRune_Atk_Blue()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_atk_blue";
            rune.RuneName = "狂暴符文";
            rune.Description = "提升攻击力和暴击率。";
            rune.Rarity = Rarity.Rare;
            rune.Category = RuneCategory.Attack;

            rune.MainAttrType = AttrType.ATK;
            rune.MainAttrBasePct = 0.08f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.SubAttrType = AttrType.CritRate;
            rune.SubAttrBasePct = 0.03f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 10, 20, 40, 80 };

            RegisterRune(rune);
        }

        private static void InitRune_Atk_Purple()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_atk_purple";
            rune.RuneName = "弑神符文";
            rune.Description = "大幅提升攻击和暴击伤害。";
            rune.Rarity = Rarity.Epic;
            rune.Category = RuneCategory.Attack;

            rune.MainAttrType = AttrType.ATK;
            rune.MainAttrBasePct = 0.12f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.SubAttrType = AttrType.CritDamage;
            rune.SubAttrBasePct = 0.10f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 15, 30, 60, 120 };

            RegisterRune(rune);
        }

        private static void InitRune_Atk_Gold()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_atk_gold";
            rune.RuneName = "毁灭符文";
            rune.Description = "传说中的毁灭之力，攻击时附带吸血效果。";
            rune.Rarity = Rarity.Legendary;
            rune.Category = RuneCategory.Attack;

            rune.MainAttrType = AttrType.ATK;
            rune.MainAttrBasePct = 0.18f;
            rune.MainAttrGrowthPerStar = 0.25f;

            rune.SubAttrType = AttrType.CritDamage;
            rune.SubAttrBasePct = 0.20f;

            rune.SpecialEffect = RuneSpecialEffect.Lifesteal;
            rune.SpecialEffectValue = 0.10f; // 10%吸血

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 20, 40, 80, 160 };

            RegisterRune(rune);
        }

        #endregion

        #region 防御类符文

        private static void InitRune_Def_White()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_def_white";
            rune.RuneName = "铁壁符文";
            rune.Description = "基础防御符文，提升防御力。";
            rune.Rarity = Rarity.Common;
            rune.Category = RuneCategory.Defense;

            rune.MainAttrType = AttrType.DEF;
            rune.MainAttrBasePct = 0.06f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 10, 20, 40, 80 };

            RegisterRune(rune);
        }

        private static void InitRune_Def_Blue()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_def_blue";
            rune.RuneName = "坚韧符文";
            rune.Description = "提升防御力和生命上限。";
            rune.Rarity = Rarity.Rare;
            rune.Category = RuneCategory.Defense;

            rune.MainAttrType = AttrType.DEF;
            rune.MainAttrBasePct = 0.08f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.SubAttrType = AttrType.MaxHP;
            rune.SubAttrBasePct = 0.05f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 10, 20, 40, 80 };

            RegisterRune(rune);
        }

        private static void InitRune_Def_Purple()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_def_purple";
            rune.RuneName = "磐石符文";
            rune.Description = "大幅提升防御和伤害减免。";
            rune.Rarity = Rarity.Epic;
            rune.Category = RuneCategory.Defense;

            rune.MainAttrType = AttrType.DEF;
            rune.MainAttrBasePct = 0.12f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.SubAttrType = AttrType.DamageReduction;
            rune.SubAttrBasePct = 0.05f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 15, 30, 60, 120 };

            RegisterRune(rune);
        }

        private static void InitRune_Def_Gold()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_def_gold";
            rune.RuneName = "不朽符文";
            rune.Description = "传说的守护之力，受到伤害时反弹一部分给敌人。";
            rune.Rarity = Rarity.Legendary;
            rune.Category = RuneCategory.Defense;

            rune.MainAttrType = AttrType.DEF;
            rune.MainAttrBasePct = 0.15f;
            rune.MainAttrGrowthPerStar = 0.25f;

            rune.SubAttrType = AttrType.MaxHP;
            rune.SubAttrBasePct = 0.10f;

            rune.SpecialEffect = RuneSpecialEffect.ReflectDamage;
            rune.SpecialEffectValue = 0.15f; // 反伤15%

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 20, 40, 80, 160 };

            RegisterRune(rune);
        }

        #endregion

        #region 生存类符文

        private static void InitRune_Hp_White()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_hp_white";
            rune.RuneName = "生命符文";
            rune.Description = "基础生命符文，提升生命上限。";
            rune.Rarity = Rarity.Common;
            rune.Category = RuneCategory.Survival;

            rune.MainAttrType = AttrType.MaxHP;
            rune.MainAttrBasePct = 0.08f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 10, 20, 40, 80 };

            RegisterRune(rune);
        }

        private static void InitRune_Hp_Blue()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_hp_blue";
            rune.RuneName = "活力符文";
            rune.Description = "提升生命上限和防御力。";
            rune.Rarity = Rarity.Rare;
            rune.Category = RuneCategory.Survival;

            rune.MainAttrType = AttrType.MaxHP;
            rune.MainAttrBasePct = 0.10f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.SubAttrType = AttrType.DEF;
            rune.SubAttrBasePct = 0.04f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 10, 20, 40, 80 };

            RegisterRune(rune);
        }

        private static void InitRune_Hp_Purple()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_hp_purple";
            rune.RuneName = "生命之泉符文";
            rune.Description = "大幅提升生命和击杀回血。";
            rune.Rarity = Rarity.Epic;
            rune.Category = RuneCategory.Survival;

            rune.MainAttrType = AttrType.MaxHP;
            rune.MainAttrBasePct = 0.15f;
            rune.MainAttrGrowthPerStar = 0.2f;

            rune.SubAttrType = AttrType.Lifesteal;
            rune.SubAttrBasePct = 0.03f;

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 15, 30, 60, 120 };

            RegisterRune(rune);
        }

        private static void InitRune_Hp_Gold()
        {
            var rune = ScriptableObject.CreateInstance<RuneData>();
            rune.RuneId = "rune_hp_gold";
            rune.RuneName = "不死鸟符文";
            rune.Description = "传说的不死之力，濒死时进入狂暴状态。";
            rune.Rarity = Rarity.Legendary;
            rune.Category = RuneCategory.Survival;

            rune.MainAttrType = AttrType.MaxHP;
            rune.MainAttrBasePct = 0.20f;
            rune.MainAttrGrowthPerStar = 0.25f;

            rune.SubAttrType = AttrType.DEF;
            rune.SubAttrBasePct = 0.08f;

            rune.SpecialEffect = RuneSpecialEffect.BerserkOnLowHp;
            rune.SpecialEffectValue = 0.30f; // 30%血量以下触发狂暴

            rune.MaxStar = 5;
            rune.ShardsPerStar = new int[] { 20, 40, 80, 160 };

            RegisterRune(rune);
        }

        #endregion
    }
}
