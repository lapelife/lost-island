using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Skill
{
    /// <summary>
    /// 技能卡配置工厂
    /// 提供所有技能卡的默认配置
    /// </summary>
    public static class SkillCardFactory
    {
        private static Dictionary<string, SkillCardData> _cardCache;
        private static Dictionary<Rarity, List<SkillCardData>> _cardsByRarity;

        static SkillCardFactory()
        {
            _cardCache = new Dictionary<string, SkillCardData>();
            _cardsByRarity = new Dictionary<Rarity, List<SkillCardData>>();

            foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
            {
                _cardsByRarity[r] = new List<SkillCardData>();
            }

            InitializeAllCards();
        }

        /// <summary>
        /// 获取指定ID的技能卡
        /// </summary>
        public static SkillCardData GetCard(string cardId)
        {
            if (_cardCache.TryGetValue(cardId, out var card))
                return card;
            return null;
        }

        /// <summary>
        /// 获取所有技能卡
        /// </summary>
        public static List<SkillCardData> GetAllCards()
        {
            return new List<SkillCardData>(_cardCache.Values);
        }

        /// <summary>
        /// 获取指定稀有度的技能卡
        /// </summary>
        public static List<SkillCardData> GetCardsByRarity(Rarity rarity)
        {
            return new List<SkillCardData>(_cardsByRarity[rarity]);
        }

        /// <summary>
        /// 获取技能卡总数
        /// </summary>
        public static int TotalCardCount => _cardCache.Count;

        /// <summary>
        /// 注册技能卡
        /// </summary>
        private static void RegisterCard(SkillCardData card)
        {
            _cardCache[card.CardId] = card;
            _cardsByRarity[card.Rarity].Add(card);
        }

        #region 初始化所有卡片

        private static void InitializeAllCards()
        {
            // ===== 主动技能：白色（3张）=====
            InitCard_FlashStrike();
            InitCard_Shockwave();
            InitCard_HealingPotion();

            // ===== 主动技能：蓝色（4张）=====
            InitCard_Fireball();
            InitCard_IceSpear();
            InitCard_Whirlwind();
            InitCard_Barrier();

            // ===== 主动技能：紫色（4张）=====
            InitCard_Meteor();
            InitCard_ThunderStrike();
            InitCard_PoisonMist();
            InitCard_BattleCry();

            // ===== 主动技能：金色（3张）=====
            InitCard_Nova();
            InitCard_TimeSlow();
            InitCard_BerserkMode();

            // ===== 被动技能：白色（2张）=====
            InitCard_Passive_Strength();
            InitCard_Passive_Vitality();

            // ===== 被动技能：蓝色（2张）=====
            InitCard_Passive_Fury();
            InitCard_Passive_IronSkin();

            // ===== 被动技能：紫色（2张）=====
            InitCard_Passive_Lifesteal();
            InitCard_Passive_Berserker();

            // ===== 被动技能：金色（2张）=====
            InitCard_Passive_Legendary();
            InitCard_Passive_Immortal();

            // 共 24 张卡
        }

        #endregion

        #region 白色主动技能

        private static void InitCard_FlashStrike()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_flashstrike";
            card.CardName = "闪击";
            card.Description = "瞬间冲向敌人，造成中等伤害。";
            card.Rarity = Rarity.Common;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.SingleEnemy;

            card.DamageMultiplier = 1.5f;
            card.Cooldown = 6f;
            card.CastRange = 5f;
            card.EffectRadius = 0f;

            card.Effects.Add(EffectType.Damage);
            card.Effects.Add(EffectType.Knockback);
            card.EffectValue = 1f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_Shockwave()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_shockwave";
            card.CardName = "震荡波";
            card.Description = "以自身为中心释放震荡波，对周围敌人造成伤害。";
            card.Rarity = Rarity.Common;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.AOE;

            card.DamageMultiplier = 1.0f;
            card.Cooldown = 8f;
            card.CastRange = 0f;
            card.EffectRadius = 3f;

            card.Effects.Add(EffectType.AoeDamage);

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_HealingPotion()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_healpotion";
            card.CardName = "治疗药剂";
            card.Description = "饮下药剂，恢复一定生命值。";
            card.Rarity = Rarity.Common;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.Self;

            card.DamageMultiplier = 2.0f; // 治疗量 = ATK × 倍率
            card.Cooldown = 15f;
            card.CastRange = 0f;

            card.Effects.Add(EffectType.Heal);

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        #endregion

        #region 蓝色主动技能

        private static void InitCard_Fireball()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_fireball";
            card.CardName = "火球术";
            card.Description = "投掷火球，造成范围伤害并点燃敌人。";
            card.Rarity = Rarity.Rare;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.AOE;

            card.DamageMultiplier = 2.0f;
            card.Cooldown = 10f;
            card.CastRange = 6f;
            card.EffectRadius = 2.5f;

            card.Effects.Add(EffectType.AoeDamage);
            card.Effects.Add(EffectType.Burn);
            card.EffectValue = 8f; // 燃烧伤害/秒
            card.EffectDuration = 3f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_IceSpear()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_icespear";
            card.CardName = "冰矛";
            card.Description = "发射冰矛，对单体造成伤害并冰冻减速。";
            card.Rarity = Rarity.Rare;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.SingleEnemy;

            card.DamageMultiplier = 2.5f;
            card.Cooldown = 8f;
            card.CastRange = 7f;

            card.Effects.Add(EffectType.Damage);
            card.Effects.Add(EffectType.Freeze);
            card.EffectValue = 0.5f; // 减速50%
            card.EffectDuration = 3f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_Whirlwind()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_whirlwind";
            card.CardName = "旋风斩";
            card.Description = "快速旋转武器，对周围敌人造成伤害并击退。";
            card.Rarity = Rarity.Rare;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.AOE;

            card.DamageMultiplier = 1.8f;
            card.Cooldown = 12f;
            card.CastRange = 0f;
            card.EffectRadius = 3.5f;

            card.Effects.Add(EffectType.AoeDamage);
            card.Effects.Add(EffectType.Knockback);
            card.EffectValue = 2f; // 击退2米

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_Barrier()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_barrier";
            card.CardName = "能量护盾";
            card.Description = "生成能量护盾，吸收一定伤害。";
            card.Rarity = Rarity.Rare;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.Self;

            card.DamageMultiplier = 3.0f; // 护盾量 = ATK × 倍率
            card.Cooldown = 20f;
            card.CastRange = 0f;
            card.Duration = 5f;

            card.Effects.Add(EffectType.Shield);
            card.EffectDuration = 5f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        #endregion

        #region 紫色主动技能

        private static void InitCard_Meteor()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_meteor";
            card.CardName = "陨石坠落";
            card.Description = "召唤陨石砸向指定区域，造成大量范围伤害。";
            card.Rarity = Rarity.Epic;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.AOE;

            card.DamageMultiplier = 4.0f;
            card.Cooldown = 20f;
            card.CastRange = 8f;
            card.EffectRadius = 3.5f;

            card.Effects.Add(EffectType.AoeDamage);
            card.Effects.Add(EffectType.Burn);
            card.EffectValue = 15f;
            card.EffectDuration = 4f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_ThunderStrike()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_thunder";
            card.CardName = "雷霆一击";
            card.Description = "召唤雷电打击敌人，造成伤害并眩晕。";
            card.Rarity = Rarity.Epic;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.SingleEnemy;

            card.DamageMultiplier = 3.5f;
            card.Cooldown = 15f;
            card.CastRange = 6f;

            card.Effects.Add(EffectType.Damage);
            card.Effects.Add(EffectType.Stun);
            card.EffectDuration = 1.5f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_PoisonMist()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_poisonmist";
            card.CardName = "毒雾";
            card.Description = "释放毒雾，持续对范围内敌人造成中毒伤害。";
            card.Rarity = Rarity.Epic;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.AOE;

            card.DamageMultiplier = 0.5f;
            card.Cooldown = 18f;
            card.CastRange = 5f;
            card.EffectRadius = 3f;

            card.Effects.Add(EffectType.AoeDamage);
            card.Effects.Add(EffectType.Burn); // 复用燃烧逻辑表示中毒
            card.EffectValue = 12f;
            card.EffectDuration = 5f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.15f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_BattleCry()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_battlecry";
            card.CardName = "战吼";
            card.Description = "发出战吼，短暂提升攻击力和攻速。";
            card.Rarity = Rarity.Epic;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.Self;

            card.DamageMultiplier = 0f;
            card.Cooldown = 25f;
            card.CastRange = 0f;
            card.Duration = 8f;
            card.EffectDuration = 8f;

            // 战吼作为增益效果，这里简化处理
            card.Effects.Add(EffectType.Shield); // 占位
            card.EffectValue = 1.5f; // 攻击力+50%

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.10f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        #endregion

        #region 金色主动技能

        private static void InitCard_Nova()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_nova";
            card.CardName = "星爆";
            card.Description = "释放毁灭性能量，对全屏敌人造成巨额伤害。";
            card.Rarity = Rarity.Legendary;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.AllEnemies;

            card.DamageMultiplier = 5.0f;
            card.Cooldown = 30f;
            card.CastRange = 20f;
            card.EffectRadius = 20f;

            card.Effects.Add(EffectType.AoeDamage);
            card.Effects.Add(EffectType.Burn);
            card.EffectValue = 20f;
            card.EffectDuration = 5f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.20f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_TimeSlow()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_timeslow";
            card.CardName = "时间减速";
            card.Description = "减缓周围敌人的时间，大幅降低移动和攻击速度。";
            card.Rarity = Rarity.Legendary;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.AOE;

            card.DamageMultiplier = 0f;
            card.Cooldown = 25f;
            card.CastRange = 0f;
            card.EffectRadius = 8f;

            card.Effects.Add(EffectType.Slow);
            card.EffectValue = 0.8f; // 减速80%
            card.EffectDuration = 5f;

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.05f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        private static void InitCard_BerserkMode()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "active_berserk";
            card.CardName = "狂暴模式";
            card.Description = "进入狂暴状态，大幅提升攻击和移动速度，但受到更多伤害。";
            card.Rarity = Rarity.Legendary;
            card.SkillType = SkillType.Active;
            card.TargetType = SkillTargetType.Self;

            card.DamageMultiplier = 0f;
            card.Cooldown = 40f;
            card.CastRange = 0f;
            card.Duration = 10f;
            card.EffectDuration = 10f;

            // 简化：用护盾表示狂暴状态
            card.Effects.Add(EffectType.Shield);
            card.EffectValue = 2.0f; // 攻速+100%

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0.10f;
            card.CdReducePerLevel = 0.05f;

            RegisterCard(card);
        }

        #endregion

        #region 白色被动技能

        private static void InitCard_Passive_Strength()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_strength";
            card.CardName = "力量强化";
            card.Description = "永久提升攻击力。";
            card.Rarity = Rarity.Common;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.ATK,
                IsFlat = false,
                ValuePerLevel = 0.08f, // 每级+8%
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        private static void InitCard_Passive_Vitality()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_vitality";
            card.CardName = "生命强化";
            card.Description = "永久提升生命上限。";
            card.Rarity = Rarity.Common;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.MaxHP,
                IsFlat = false,
                ValuePerLevel = 0.10f, // 每级+10%
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        #endregion

        #region 蓝色被动技能

        private static void InitCard_Passive_Fury()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_fury";
            card.CardName = "狂暴之怒";
            card.Description = "提升暴击率和暴击伤害。";
            card.Rarity = Rarity.Rare;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.CritRate,
                IsFlat = false,
                ValuePerLevel = 0.05f, // 每级+5%暴击率
                Layer = AttrLayer.Pct_SkillCard
            });

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.CritDamage,
                IsFlat = false,
                ValuePerLevel = 0.10f, // 每级+10%暴击伤害
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        private static void InitCard_Passive_IronSkin()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_ironskin";
            card.CardName = "铁甲";
            card.Description = "提升防御力，减少受到的伤害。";
            card.Rarity = Rarity.Rare;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.DEF,
                IsFlat = false,
                ValuePerLevel = 0.12f, // 每级+12%防御
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        #endregion

        #region 紫色被动技能

        private static void InitCard_Passive_Lifesteal()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_lifesteal";
            card.CardName = "吸血";
            card.Description = "攻击时恢复造成伤害一定比例的生命。";
            card.Rarity = Rarity.Epic;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.Lifesteal,
                IsFlat = false,
                ValuePerLevel = 0.06f, // 每级+6%吸血
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        private static void InitCard_Passive_Berserker()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_berserker";
            card.CardName = "狂战之魂";
            card.Description = "生命值越低，攻击力越高。";
            card.Rarity = Rarity.Epic;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.ATK,
                IsFlat = false,
                ValuePerLevel = 0.10f, // 每级+10%基础攻击
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        #endregion

        #region 金色被动技能

        private static void InitCard_Passive_Legendary()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_legendary";
            card.CardName = "传奇之力";
            card.Description = "全属性大幅提升。";
            card.Rarity = Rarity.Legendary;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.ATK,
                IsFlat = false,
                ValuePerLevel = 0.15f, // 每级+15%攻击
                Layer = AttrLayer.Pct_SkillCard
            });

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.MaxHP,
                IsFlat = false,
                ValuePerLevel = 0.15f, // 每级+15%生命
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        private static void InitCard_Passive_Immortal()
        {
            var card = ScriptableObject.CreateInstance<SkillCardData>();
            card.CardId = "passive_immortal";
            card.CardName = "不死意志";
            card.Description = "受到致命伤害时，有概率保留1点生命并短暂无敌。";
            card.Rarity = Rarity.Legendary;
            card.SkillType = SkillType.Passive;

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.MaxHP,
                IsFlat = false,
                ValuePerLevel = 0.20f, // 每级+20%生命
                Layer = AttrLayer.Pct_SkillCard
            });

            card.PassiveBonuses.Add(new SkillBonusEntry
            {
                AttrType = AttrType.DEF,
                IsFlat = false,
                ValuePerLevel = 0.10f, // 每级+10%防御
                Layer = AttrLayer.Pct_SkillCard
            });

            card.MaxLevel = 5;
            card.DmgGrowthPerLevel = 0f;
            card.CdReducePerLevel = 0f;

            RegisterCard(card);
        }

        #endregion
    }
}
