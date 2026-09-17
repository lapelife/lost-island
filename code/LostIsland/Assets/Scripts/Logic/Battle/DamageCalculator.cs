using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Battle
{
    /// <summary>
    /// 伤害计算器：9步伤害计算流水线
    /// 纯函数实现，便于单元测试
    /// 
    /// 9步流水线：
    /// Step1: 基础伤害 = ATK × 技能倍率
    /// Step2: 防御减伤 = DEF / (DEF + 200 + attackerLv × 10)
    /// Step3: 暴击判定 = 随机值 vs 暴击率，暴击伤害倍率
    /// Step4: 伤害减免 = defender.DamageReduction
    /// Step5: 最终伤害 = Mathf.Max(1, damage)
    /// Step6: 应用伤害到defender（由调用方执行）
    /// Step7: 受击反馈（由View层监听事件）
    /// Step8: 吸血回复 = 最终伤害 × 吸血率
    /// Step9: 击杀奖励（由调用方在HP归零时触发）
    /// </summary>
    public static class DamageCalculator
    {
        #region 常量

        /// <summary>
        /// 防御减伤公式基础值
        /// </summary>
        public const float DEF_BASE = 200f;

        /// <summary>
        /// 防御减伤等级系数
        /// </summary>
        public const float DEF_LEVEL_FACTOR = 10f;

        /// <summary>
        /// 最小伤害值
        /// </summary>
        public const float MIN_DAMAGE = 1f;

        #endregion

        #region 核心计算方法

        /// <summary>
        /// 计算伤害（完整9步流水线，Step1-Step5 + Step8）
        /// Step6/7/9 由调用方执行
        /// </summary>
        /// <param name="attacker">攻击者属性组件</param>
        /// <param name="defender">防御者属性组件</param>
        /// <param name="skillMultiplier">技能倍率（默认1.0）</param>
        /// <param name="isTrueDamage">是否真实伤害（跳过防御减伤）</param>
        /// <param name="attackerLevel">攻击者等级（影响防御减伤计算）</param>
        /// <returns>伤害结果</returns>
        public static DamageResult CalculateDamage(
            AttributeComponent attacker,
            AttributeComponent defender,
            float skillMultiplier = 1f,
            bool isTrueDamage = false,
            int attackerLevel = 1)
        {
            DamageResult result = new DamageResult();
            result.IsTrueDamage = isTrueDamage;

            // Step1: 基础伤害 = 最终ATK × 技能倍率
            float damage = attacker.FinalATK * skillMultiplier;
            result.RawDamage = damage;

            // Step2: 防御减伤（真实伤害跳过）
            if (!isTrueDamage)
            {
                float def = defender.FinalDEF;
                float defReduction = CalculateDefReduction(def, attackerLevel);
                damage *= (1f - defReduction);
                result.DefReduction = defReduction;
            }

            // Step3: 暴击判定
            float critRate = attacker.FinalCritRate;
            if (Random.value < critRate)
            {
                damage *= attacker.FinalCritDamage;
                result.IsCrit = true;
            }

            // Step4: 伤害减免（defender的DamageReduction属性）
            float dmgReductionPct = defender.FinalDamageReduction;
            damage *= (1f - dmgReductionPct);
            result.DmgReductionPct = dmgReductionPct;
            result.DamageReduction = dmgReductionPct; // 兼容旧字段

            // Step5: 最终伤害（确保最小1点）
            result.FinalDamage = Mathf.Max(MIN_DAMAGE, damage);

            // Step8: 吸血回复
            result.LifestealHeal = result.FinalDamage * attacker.FinalLifesteal;

            // 反伤（Step9的一部分，由反伤系统处理）
            result.ReflectDamage = result.FinalDamage * defender.FinalReflect;

            return result;
        }

        #endregion

        #region 分步计算（便于调试和测试）

        /// <summary>
        /// Step1: 计算基础伤害
        /// </summary>
        public static float CalcBaseDamage(float atk, float skillMultiplier = 1f)
        {
            return atk * skillMultiplier;
        }

        /// <summary>
        /// Step2: 计算防御减伤比例
        /// 公式：DEF / (DEF + 200 + attackerLv × 10)
        /// </summary>
        /// <param name="def">防御值</param>
        /// <param name="attackerLevel">攻击者等级</param>
        /// <returns>减伤比例 0~1</returns>
        public static float CalculateDefReduction(float def, int attackerLevel = 1)
        {
            if (def <= 0f) return 0f;

            float denominator = def + DEF_BASE + attackerLevel * DEF_LEVEL_FACTOR;
            if (denominator <= 0f) return 0f;

            float reduction = def / denominator;
            return Mathf.Clamp01(reduction);
        }

        /// <summary>
        /// Step3: 应用暴击
        /// </summary>
        /// <param name="damage">当前伤害</param>
        /// <param name="critRate">暴击率</param>
        /// <param name="critDamage">暴击伤害倍率</param>
        /// <param name="isCrit">输出：是否暴击</param>
        /// <returns>暴击后的伤害</returns>
        public static float ApplyCrit(float damage, float critRate, float critDamage, out bool isCrit)
        {
            isCrit = false;
            if (critRate <= 0f) return damage;

            if (Random.value < critRate)
            {
                damage *= critDamage;
                isCrit = true;
            }

            return damage;
        }

        /// <summary>
        /// Step4: 应用伤害减免
        /// </summary>
        public static float ApplyDamageReduction(float damage, float reductionPct)
        {
            return damage * (1f - Mathf.Clamp01(reductionPct));
        }

        /// <summary>
        /// Step8: 计算吸血量
        /// </summary>
        public static float CalculateLifesteal(float finalDamage, float lifestealPct)
        {
            if (lifestealPct <= 0f) return 0f;
            return finalDamage * lifestealPct;
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 计算伤害后直接应用到目标
        /// 封装了伤害计算 + 扣血 + 吸血 + 反伤
        /// </summary>
        /// <param name="attacker">攻击者实体</param>
        /// <param name="defender">防御者实体</param>
        /// <param name="skillMultiplier">技能倍率</param>
        /// <param name="isTrueDamage">是否真实伤害</param>
        /// <returns>实际造成的伤害</returns>
        public static float DealDamage(
            EntityBase attacker,
            EntityBase defender,
            float skillMultiplier = 1f,
            bool isTrueDamage = false)
        {
            if (attacker == null || defender == null) return 0f;
            if (!defender.IsAlive) return 0f;

            int attackerLevel = attacker != null ? attacker.Level : 1;

            // 计算伤害
            DamageResult result = CalculateDamage(
                attacker.Attribute,
                defender.Attribute,
                skillMultiplier,
                isTrueDamage,
                attackerLevel);

            result.Source = attacker;

            // Step6: 应用伤害到防御者
            float actualDamage = defender.Health.TakeDamage(result);

            // Step8: 吸血（攻击者回血）
            if (result.LifestealHeal > 0f && attacker.Health != null && attacker.IsAlive)
            {
                attacker.Health.Heal(result.LifestealHeal);
            }

            // 反伤（攻击者受到反伤）
            if (result.ReflectDamage > 0f && attacker.Health != null && attacker.IsAlive)
            {
                DamageResult reflectResult = new DamageResult
                {
                    FinalDamage = result.ReflectDamage,
                    RawDamage = result.ReflectDamage,
                    IsTrueDamage = true,
                    Source = defender
                };
                attacker.Health.TakeDamage(reflectResult);
            }

            return actualDamage;
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 输出伤害计算详情（调试用）
        /// </summary>
        public static string DebugDamageInfo(
            AttributeComponent attacker,
            AttributeComponent defender,
            float skillMultiplier = 1f,
            bool isTrueDamage = false,
            int attackerLevel = 1)
        {
            float baseDamage = attacker.FinalATK * skillMultiplier;
            float def = defender.FinalDEF;
            float defReduction = isTrueDamage ? 0f : CalculateDefReduction(def, attackerLevel);
            float afterDef = baseDamage * (1f - defReduction);
            float critRate = attacker.FinalCritRate;
            float critDamage = attacker.FinalCritDamage;
            float dmgReduction = defender.FinalDamageReduction;
            float afterReduction = afterDef * (1f - dmgReduction);
            float finalDmg = Mathf.Max(MIN_DAMAGE, afterReduction);
            float lifesteal = finalDmg * attacker.FinalLifesteal;

            return $"=== 伤害计算详情 ===\n" +
                   $"基础ATK: {attacker.FinalATK:F1} × 技能倍率 {skillMultiplier:F2} = {baseDamage:F1}\n" +
                   $"防御: {def:F1}, 减伤比例: {defReduction:P2}, 减伤后: {afterDef:F1}\n" +
                   $"暴击率: {critRate:P2}, 爆伤倍率: {critDamage:F2}x\n" +
                   $"伤害减免: {dmgReduction:P2}, 减免后: {afterReduction:F1}\n" +
                   $"最终伤害: {finalDmg:F1} (最小{MIN_DAMAGE})\n" +
                   $"吸血: {lifesteal:F1} (吸血率 {attacker.FinalLifesteal:P2})\n" +
                   $"真实伤害: {isTrueDamage}";
        }

        #endregion
    }
}
