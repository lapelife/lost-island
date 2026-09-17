using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Logic.Battle;

namespace LostIsland.Tests
{
    /// <summary>
    /// 战斗系统单元测试
    /// 验证 DamageCalculator、BattleManager 等战斗核心功能
    /// </summary>
    public class BattleSystemTests
    {
        private static int _totalTests = 0;
        private static int _passedTests = 0;
        private static int _failedTests = 0;
        private static List<string> _failedMessages = new List<string>();

        public static void RunAllTests()
        {
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;
            _failedMessages.Clear();

            Debug.Log("========== 战斗系统单元测试开始 ==========");

            // 伤害计算测试
            Test_BaseDamage();
            Test_DefenseReduction();
            Test_DefenseReduction_ByLevel();
            Test_CriticalHit();
            Test_DamageReduction();
            Test_TrueDamage();
            Test_MinimumDamage();
            Test_Lifesteal();
            Test_FullPipeline();
            Test_DealDamage_Integration();

            // 汇总
            Debug.Log($"========== 测试结束 ==========");
            Debug.Log($"总计: {_totalTests}, 通过: {_passedTests}, 失败: {_failedTests}");

            if (_failedTests > 0)
            {
                Debug.LogError("失败的测试:");
                foreach (var msg in _failedMessages)
                {
                    Debug.LogError($"  - {msg}");
                }
            }
            else
            {
                Debug.Log("🎉 所有测试通过！");
            }
        }

        #region 测试辅助方法

        private static void Assert(string testName, bool condition, string message = "")
        {
            _totalTests++;
            if (condition)
            {
                _passedTests++;
                Debug.Log($"✅ {testName}");
            }
            else
            {
                _failedTests++;
                string failMsg = string.IsNullOrEmpty(message) ? testName : $"{testName}: {message}";
                _failedMessages.Add(failMsg);
                Debug.LogError($"❌ {testName}: {message}");
            }
        }

        private static void AssertApproximately(string testName, float actual, float expected, float tolerance = 0.001f)
        {
            bool equal = Mathf.Abs(actual - expected) <= tolerance;
            Assert(testName, equal, $"expected={expected:F4}, actual={actual:F4}, diff={Mathf.Abs(actual - expected):F6}");
        }

        #endregion

        #region 基础伤害测试

        private static void Test_BaseDamage()
        {
            Debug.Log("--- 测试: 基础伤害计算 ---");

            float atk = 100f;
            float damage = DamageCalculator.CalcBaseDamage(atk);
            AssertApproximately("100ATK × 1.0倍率 = 100", damage, 100f);

            damage = DamageCalculator.CalcBaseDamage(atk, 2.0f);
            AssertApproximately("100ATK × 2.0倍率 = 200", damage, 200f);

            damage = DamageCalculator.CalcBaseDamage(atk, 0.5f);
            AssertApproximately("100ATK × 0.5倍率 = 50", damage, 50f);
        }

        #endregion

        #region 防御减伤测试

        private static void Test_DefenseReduction()
        {
            Debug.Log("--- 测试: 防御减伤 ---");

            // DEF=0, 减伤0%
            float reduction = DamageCalculator.CalculateDefReduction(0f, 1);
            AssertApproximately("DEF=0 减伤0%", reduction, 0f);

            // DEF=200, 等级1: 200/(200+200+10) = 200/410 ≈ 48.78%
            reduction = DamageCalculator.CalculateDefReduction(200f, 1);
            float expected = 200f / (200f + 200f + 10f);
            AssertApproximately("DEF=200 Lv=1 减伤约48.78%", reduction, expected);

            // DEF=500, 等级1: 500/(500+200+10) = 500/710 ≈ 70.42%
            reduction = DamageCalculator.CalculateDefReduction(500f, 1);
            expected = 500f / (500f + 200f + 10f);
            AssertApproximately("DEF=500 Lv=1 减伤约70.42%", reduction, expected);

            // DEF=1000, 等级1: 1000/(1000+200+10) = 1000/1210 ≈ 82.64%
            reduction = DamageCalculator.CalculateDefReduction(1000f, 1);
            expected = 1000f / (1000f + 200f + 10f);
            AssertApproximately("DEF=1000 Lv=1 减伤约82.64%", reduction, expected);

            // 减伤比例在0~1之间
            Assert("减伤比例上限不超过1", reduction <= 1f);
            Assert("减伤比例下限不低于0", reduction >= 0f);
        }

        private static void Test_DefenseReduction_ByLevel()
        {
            Debug.Log("--- 测试: 等级对防御减伤的影响 ---");

            float def = 200f;

            // 等级1: 200/(200+200+10) = 200/410
            float red1 = DamageCalculator.CalculateDefReduction(def, 1);
            // 等级10: 200/(200+200+100) = 200/500 = 40%
            float red10 = DamageCalculator.CalculateDefReduction(def, 10);
            // 等级50: 200/(200+200+500) = 200/900 ≈ 22.2%
            float red50 = DamageCalculator.CalculateDefReduction(def, 50);

            // 等级越高，同样防御的减伤效果越低
            Assert("等级越高减伤越低", red1 > red10 && red10 > red50);
            AssertApproximately("Lv10 DEF200 减伤40%", red10, 0.4f);
        }

        #endregion

        #region 暴击测试

        private static void Test_CriticalHit()
        {
            Debug.Log("--- 测试: 暴击判定 ---");

            float damage = 100f;
            bool isCrit;

            // 0%暴击率：必不暴击
            float result = DamageCalculator.ApplyCrit(damage, 0f, 2f, out isCrit);
            Assert("0%暴击率不暴击", !isCrit);
            AssertApproximately("不暴击伤害不变", result, 100f);

            // 100%暴击率：必暴击
            result = DamageCalculator.ApplyCrit(damage, 1f, 2f, out isCrit);
            Assert("100%暴击率必暴击", isCrit);
            AssertApproximately("2倍暴击伤害", result, 200f);

            // 暴击伤害1.5倍
            result = DamageCalculator.ApplyCrit(damage, 1f, 1.5f, out isCrit);
            AssertApproximately("1.5倍暴击伤害", result, 150f);
        }

        #endregion

        #region 伤害减免测试

        private static void Test_DamageReduction()
        {
            Debug.Log("--- 测试: 伤害减免 ---");

            float damage = 100f;

            // 0%减免
            float result = DamageCalculator.ApplyDamageReduction(damage, 0f);
            AssertApproximately("0%减免伤害不变", result, 100f);

            // 30%减免
            result = DamageCalculator.ApplyDamageReduction(damage, 0.3f);
            AssertApproximately("30%减免 = 70伤害", result, 70f);

            // 50%减免
            result = DamageCalculator.ApplyDamageReduction(damage, 0.5f);
            AssertApproximately("50%减免 = 50伤害", result, 50f);

            // 超过100%会被裁剪到0
            result = DamageCalculator.ApplyDamageReduction(damage, 1.5f);
            AssertApproximately("100%减免 = 0伤害", result, 0f);
        }

        #endregion

        #region 真实伤害测试

        private static void Test_TrueDamage()
        {
            Debug.Log("--- 测试: 真实伤害 ---");

            var attacker = new AttributeComponent();
            attacker.SetBaseValue(AttrType.ATK, 100f);
            attacker.SetBaseValue(AttrType.CritRate, 0f);

            var defender = new AttributeComponent();
            defender.SetBaseValue(AttrType.DEF, 500f); // 高防御
            defender.SetBaseValue(AttrType.DamageReduction, 0.3f); // 30%伤害减免

            // 普通伤害：经过防御减伤和伤害减免
            var normalResult = DamageCalculator.CalculateDamage(attacker, defender, 1f, false, 1);
            // 真实伤害：跳过防御减伤，但仍受伤害减免影响
            var trueResult = DamageCalculator.CalculateDamage(attacker, defender, 1f, true, 1);

            Assert("真实伤害高于普通伤害", trueResult.FinalDamage > normalResult.FinalDamage);
            Assert("真实伤害标记正确", trueResult.IsTrueDamage);
            Assert("普通伤害标记正确", !normalResult.IsTrueDamage);

            // 真实伤害 = 100 × (1-0.3) = 70
            AssertApproximately("真实伤害跳过防御", trueResult.FinalDamage, 70f);
        }

        #endregion

        #region 最小伤害测试

        private static void Test_MinimumDamage()
        {
            Debug.Log("--- 测试: 最小伤害保证 ---");

            var attacker = new AttributeComponent();
            attacker.SetBaseValue(AttrType.ATK, 1f); // 极低攻击
            attacker.SetBaseValue(AttrType.CritRate, 0f);

            var defender = new AttributeComponent();
            defender.SetBaseValue(AttrType.DEF, 9999f); // 极高防御
            defender.SetBaseValue(AttrType.DamageReduction, 0.99f); // 99%减免

            var result = DamageCalculator.CalculateDamage(attacker, defender, 1f, false, 99);

            // 即使防御很高、减免很高，最少也有1点伤害
            Assert("最小伤害为1", result.FinalDamage >= DamageCalculator.MIN_DAMAGE);
            AssertApproximately("最终伤害 = 最小伤害", result.FinalDamage, DamageCalculator.MIN_DAMAGE);
        }

        #endregion

        #region 吸血测试

        private static void Test_Lifesteal()
        {
            Debug.Log("--- 测试: 吸血计算 ---");

            float finalDamage = 100f;

            // 0%吸血
            float lifesteal = DamageCalculator.CalculateLifesteal(finalDamage, 0f);
            AssertApproximately("0%吸血 = 0", lifesteal, 0f);

            // 10%吸血
            lifesteal = DamageCalculator.CalculateLifesteal(finalDamage, 0.1f);
            AssertApproximately("10%吸血 = 10", lifesteal, 10f);

            // 20%吸血
            lifesteal = DamageCalculator.CalculateLifesteal(finalDamage, 0.2f);
            AssertApproximately("20%吸血 = 20", lifesteal, 20f);
        }

        #endregion

        #region 完整流水线测试

        private static void Test_FullPipeline()
        {
            Debug.Log("--- 测试: 完整9步伤害流水线 ---");

            // 攻击者
            var attacker = new AttributeComponent();
            attacker.SetBaseValue(AttrType.ATK, 200f);
            attacker.SetBaseValue(AttrType.CritRate, 0f); // 0暴击，确定结果
            attacker.SetBaseValue(AttrType.CritDamage, 2f);
            attacker.SetBaseValue(AttrType.Lifesteal, 0.1f);

            // 防御者
            var defender = new AttributeComponent();
            defender.SetBaseValue(AttrType.DEF, 300f);
            defender.SetBaseValue(AttrType.DamageReduction, 0.2f);

            // 计算伤害
            var result = DamageCalculator.CalculateDamage(attacker, defender, 1f, false, 10);

            // 验证各步骤
            // Step1: 基础伤害 = 200 × 1.0 = 200
            AssertApproximately("基础伤害 = 200", result.RawDamage, 200f);

            // Step2: 防御减伤 = 300/(300+200+10×10) = 300/600 = 50%
            // 减伤后 = 200 × 0.5 = 100
            AssertApproximately("防御减伤 = 50%", result.DefReduction, 0.5f);

            // Step3: 0%暴击率，不暴击
            Assert("不暴击", !result.IsCrit);

            // Step4: 伤害减免20%，100 × 0.8 = 80
            AssertApproximately("伤害减免 = 20%", result.DmgReductionPct, 0.2f);

            // Step5: 最终伤害 = 80
            AssertApproximately("最终伤害 = 80", result.FinalDamage, 80f);

            // Step8: 吸血 = 80 × 10% = 8
            AssertApproximately("吸血量 = 8", result.LifestealHeal, 8f);
        }

        #endregion

        #region 集成测试

        private static void Test_DealDamage_Integration()
        {
            Debug.Log("--- 测试: DealDamage 集成测试 ---");

            // 创建攻击者
            EntityBase attacker = new EntityBase();
            attacker.Initialize(1, EntityType.Player, "Attacker");
            attacker.SetLevel(10);
            attacker.Attribute.SetBaseValue(AttrType.ATK, 200f);
            attacker.Attribute.SetBaseValue(AttrType.MaxHP, 1000f);
            attacker.Attribute.SetBaseValue(AttrType.CritRate, 0f); // 0暴击方便测试
            attacker.Attribute.SetBaseValue(AttrType.Lifesteal, 0.2f); // 20%吸血
            attacker.Health.SetCurrentHP(500f);

            // 创建防御者
            EntityBase defender = new EntityBase();
            defender.Initialize(2, EntityType.Zombie, "Defender");
            defender.SetLevel(10);
            defender.Attribute.SetBaseValue(AttrType.DEF, 300f); // 减伤50%
            defender.Attribute.SetBaseValue(AttrType.MaxHP, 500f);
            defender.Attribute.SetBaseValue(AttrType.DamageReduction, 0f);
            defender.Attribute.SetBaseValue(AttrType.Reflect, 0f);

            // 初始HP
            float attackerHPBefore = attacker.Health.CurrentHP;
            float defenderHPBefore = defender.Health.CurrentHP;

            // 造成伤害
            float damage = DamageCalculator.DealDamage(attacker, defender, 1f);

            // 防御者受到伤害：200 × (1-0.5) = 100
            float expectedDamage = 100f;
            AssertApproximately("实际伤害正确", damage, expectedDamage);
            AssertApproximately("防御者HP减少", defender.Health.CurrentHP, defenderHPBefore - expectedDamage);

            // 攻击者吸血：100 × 20% = 20
            float expectedHeal = expectedDamage * 0.2f;
            AssertApproximately("攻击者吸血回复", attacker.Health.CurrentHP, attackerHPBefore + expectedHeal);

            // 来源正确
            Assert("伤害非0", damage > 0f);
        }

        #endregion
    }
}
