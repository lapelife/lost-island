using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Wave;
using LostIsland.Entity;
using LostIsland.Core;

namespace LostIsland.Logic.Skill
{
    /// <summary>
    /// 技能效果基类
    /// 效果组件模式：每个技能可以挂载多个效果组件
    /// </summary>
    public abstract class SkillEffectBase
    {
        /// <summary>效果类型</summary>
        public abstract EffectType EffectType { get; }

        /// <summary>
        /// 应用效果
        /// </summary>
        /// <param name="caster">施放者</param>
        /// <param name="target">目标（可为null，AOE类自行查找）</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="cardData">技能卡数据</param>
        /// <param name="level">技能等级</param>
        public abstract void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level);

        /// <summary>
        /// 获取等级倍率
        /// </summary>
        protected float GetLevelMultiplier(SkillCardData data, int level)
        {
            if (level <= 1) return 1f;
            return 1f + data.DmgGrowthPerLevel * (level - 1);
        }
    }

    #region 单体伤害效果

    /// <summary>
    /// 单体伤害效果
    /// </summary>
    public class DamageEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Damage;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (target == null || target.IsDead) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float damage = caster.Attribute.FinalATK * cardData.DamageMultiplier * levelMult;

            var result = DamageResult.Create(damage);
            result.SourceType = DamageSourceType.Skill;
            result.SourceName = cardData.CardName;
            result.IsSkillDamage = true;

            target.TakeDamage(result);
        }
    }

    #endregion

    #region 范围伤害效果

    /// <summary>
    /// 范围伤害效果
    /// </summary>
    public class AoeDamageEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.AoeDamage;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            var spawner = ZombieSpawner.Instance;
            if (spawner == null) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float baseDamage = caster.Attribute.FinalATK * cardData.DamageMultiplier * levelMult;
            float radius = cardData.EffectRadius;

            int hitCount = 0;
            foreach (var zombie in spawner.ActiveZombies)
            {
                if (zombie == null || zombie.IsDead) continue;

                float dist = Vector3.Distance(targetPosition, zombie.MoveComp.Position);
                if (dist <= radius)
                {
                    // 距离衰减
                    float falloff = 1f - (dist / radius) * 0.3f; // 边缘70%伤害
                    float dmg = baseDamage * falloff;

                    var result = DamageResult.Create(dmg);
                    result.SourceType = DamageSourceType.Skill;
                    result.SourceName = cardData.CardName;
                    result.IsSkillDamage = true;

                    zombie.TakeDamage(result);
                    hitCount++;
                }
            }

            // Debug.Log($"[AoeDamageEffect] {cardData.CardName} 命中 {hitCount} 个目标");
        }
    }

    #endregion

    #region 燃烧效果

    /// <summary>
    /// 燃烧效果（持续伤害）
    /// </summary>
    public class BurnEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Burn;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (target == null || target.IsDead) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float damagePerSecond = cardData.EffectValue * levelMult;
            float duration = cardData.GetEffectDuration(level);

            // 应用燃烧状态
            target.ApplyStatus(StatusType.Burn, damagePerSecond, duration);

            Debug.Log($"[BurnEffect] {cardData.CardName} 施加燃烧: {damagePerSecond:F0}/秒, 持续{duration:F1}秒");
        }
    }

    #endregion

    #region 冰冻效果

    /// <summary>
    /// 冰冻效果（减速 + 短暂定身）
    /// </summary>
    public class FreezeEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Freeze;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (target == null || target.IsDead) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float slowAmount = cardData.EffectValue; // 减速百分比
            float duration = cardData.GetEffectDuration(level);

            // 应用减速
            target.ApplyStatus(StatusType.Slow, slowAmount * levelMult, duration);

            // 短暂定身（前0.5秒）
            if (duration > 0.5f)
            {
                target.ApplyStatus(StatusType.Stun, 0f, 0.5f);
            }

            Debug.Log($"[FreezeEffect] {cardData.CardName} 冰冻: 减速{slowAmount*100:F0}%, 持续{duration:F1}秒");
        }
    }

    #endregion

    #region 眩晕效果

    /// <summary>
    /// 眩晕效果（无法行动）
    /// </summary>
    public class StunEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Stun;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (target == null || target.IsDead) return;

            float duration = cardData.GetEffectDuration(level);
            target.ApplyStatus(StatusType.Stun, 0f, duration);

            Debug.Log($"[StunEffect] {cardData.CardName} 眩晕: 持续{duration:F1}秒");
        }
    }

    #endregion

    #region 减速效果

    /// <summary>
    /// 减速效果
    /// </summary>
    public class SlowEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Slow;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (target == null || target.IsDead) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float slowAmount = cardData.EffectValue * levelMult;
            float duration = cardData.GetEffectDuration(level);

            target.ApplyStatus(StatusType.Slow, slowAmount, duration);

            Debug.Log($"[SlowEffect] {cardData.CardName} 减速: {slowAmount*100:F0}%, 持续{duration:F1}秒");
        }
    }

    #endregion

    #region 击退效果

    /// <summary>
    /// 击退效果
    /// </summary>
    public class KnockbackEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Knockback;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (target == null || target.IsDead) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float knockbackDistance = cardData.EffectValue * levelMult;

            // 计算击退方向（从施放者指向目标的反方向）
            Vector3 direction = (target.MoveComp.Position - targetPosition).normalized;
            if (direction == Vector3.zero) direction = Vector3.forward;

            Vector3 newPos = target.MoveComp.Position + direction * knockbackDistance;
            target.MoveComp.SetPosition(newPos);

            // 击退时短暂硬直
            target.ApplyStatus(StatusType.Stun, 0f, 0.3f);

            Debug.Log($"[KnockbackEffect] {cardData.CardName} 击退: {knockbackDistance:F1}米");
        }
    }

    #endregion

    #region 治疗效果

    /// <summary>
    /// 治疗效果
    /// </summary>
    public class HealEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Heal;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (caster == null) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float healAmount = caster.Attribute.FinalATK * cardData.DamageMultiplier * levelMult;

            caster.Health.Heal(healAmount);

            Debug.Log($"[HealEffect] {cardData.CardName} 治疗: {healAmount:F0}");
        }
    }

    #endregion

    #region 护盾效果

    /// <summary>
    /// 护盾效果
    /// </summary>
    public class ShieldEffect : SkillEffectBase
    {
        public override EffectType EffectType => EffectType.Shield;

        public override void Apply(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (caster == null) return;

            float levelMult = GetLevelMultiplier(cardData, level);
            float shieldAmount = caster.Attribute.FinalATK * cardData.DamageMultiplier * levelMult;
            float duration = cardData.GetEffectDuration(level);

            // 应用护盾状态
            caster.Health.ApplyShield(shieldAmount, duration);

            Debug.Log($"[ShieldEffect] {cardData.CardName} 护盾: {shieldAmount:F0}, 持续{duration:F1}秒");
        }
    }

    #endregion

    #region 效果工厂

    /// <summary>
    /// 技能效果工厂
    /// </summary>
    public static class SkillEffectFactory
    {
        private static Dictionary<EffectType, SkillEffectBase> _effectCache = new Dictionary<EffectType, SkillEffectBase>();

        static SkillEffectFactory()
        {
            RegisterEffect(new DamageEffect());
            RegisterEffect(new AoeDamageEffect());
            RegisterEffect(new BurnEffect());
            RegisterEffect(new FreezeEffect());
            RegisterEffect(new StunEffect());
            RegisterEffect(new SlowEffect());
            RegisterEffect(new KnockbackEffect());
            RegisterEffect(new HealEffect());
            RegisterEffect(new ShieldEffect());
        }

        private static void RegisterEffect(SkillEffectBase effect)
        {
            _effectCache[effect.EffectType] = effect;
        }

        /// <summary>
        /// 获取效果实例
        /// </summary>
        public static SkillEffectBase GetEffect(EffectType type)
        {
            if (_effectCache.TryGetValue(type, out var effect))
                return effect;

            Debug.LogWarning($"[SkillEffectFactory] 未找到效果: {type}");
            return null;
        }

        /// <summary>
        /// 应用技能的所有效果
        /// </summary>
        public static void ApplyAllEffects(EntityBase caster, ZombieController target,
            Vector3 targetPosition, SkillCardData cardData, int level)
        {
            if (cardData == null || cardData.Effects == null) return;

            foreach (var effectType in cardData.Effects)
            {
                var effect = GetEffect(effectType);
                effect?.Apply(caster, target, targetPosition, cardData, level);
            }
        }

        /// <summary>
        /// 获取已注册的效果数量
        /// </summary>
        public static int RegisteredEffectCount => _effectCache.Count;
    }

    #endregion
}
