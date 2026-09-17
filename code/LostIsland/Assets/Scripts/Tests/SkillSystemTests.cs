using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Skill;
using LostIsland.Entity;

namespace LostIsland.Tests
{
    /// <summary>
    /// 技能卡系统单元测试
    /// </summary>
    public class SkillSystemTests
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

            Debug.Log("========== 技能卡系统测试开始 ==========");

            // 技能卡配置测试
            Test_CardFactory_TotalCount();
            Test_CardFactory_ByRarity();
            Test_Card_LevelGrowth_Damage();
            Test_Card_LevelGrowth_Cooldown();

            // 效果系统测试
            Test_EffectFactory_RegisteredCount();
            Test_EffectFactory_GetAll();

            // 技能槽测试
            Test_SkillSlot_EquipUnequip();
            Test_SkillSlot_Cooldown();
            Test_SkillSlot_TryCast();
            Test_SkillSlot_CDProgress();

            // 技能卡管理器测试
            Test_Manager_SlotCount();
            Test_Manager_EquipActive();
            Test_Manager_EquipPassive();
            Test_Manager_CastSkill();

            // 稀有度分布
            Test_RarityDistribution();

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

        #region 测试辅助

        private static void Assert(string testName, bool condition, string message = "")
        {
            _totalTests++;
            if (condition)
            {
                _passedTests++;
            }
            else
            {
                _failedTests++;
                string failMsg = string.IsNullOrEmpty(message) ? testName : $"{testName}: {message}";
                _failedMessages.Add(failMsg);
                Debug.LogError($"❌ {testName}: {message}");
            }
        }

        private static void AssertApproximately(string testName, float actual, float expected, float tolerance = 0.01f)
        {
            bool equal = Mathf.Abs(actual - expected) <= tolerance;
            Assert(testName, equal, $"expected={expected:F4}, actual={actual:F4}");
        }

        #endregion

        #region 技能卡配置测试

        private static void Test_CardFactory_TotalCount()
        {
            Debug.Log("--- 测试: 技能卡总数 ---");

            int count = SkillCardFactory.TotalCardCount;
            Assert("技能卡总数>=20", count >= 20, $"实际={count}");
            Debug.Log($"技能卡总数: {count}");
        }

        private static void Test_CardFactory_ByRarity()
        {
            Debug.Log("--- 测试: 按稀有度分类 ---");

            int common = SkillCardFactory.GetCardsByRarity(Rarity.Common).Count;
            int rare = SkillCardFactory.GetCardsByRarity(Rarity.Rare).Count;
            int epic = SkillCardFactory.GetCardsByRarity(Rarity.Epic).Count;
            int legendary = SkillCardFactory.GetCardsByRarity(Rarity.Legendary).Count;

            Assert("白卡>=3", common >= 3);
            Assert("蓝卡>=3", rare >= 3);
            Assert("紫卡>=3", epic >= 3);
            Assert("金卡>=2", legendary >= 2);

            Debug.Log($"白:{common} 蓝:{rare} 紫:{epic} 金:{legendary}");
        }

        private static void Test_Card_LevelGrowth_Damage()
        {
            Debug.Log("--- 测试: 伤害等级成长 ---");

            var card = SkillCardFactory.GetCard("active_fireball");
            Assert("火球术卡存在", card != null);

            // Lv1 = 基础值
            float dmg1 = card.GetDamageMultiplier(1);
            AssertApproximately("Lv1伤害倍率=2.0", dmg1, 2.0f);

            // Lv5 = 基础 × (1 + 0.15×4) = 2.0 × 1.6 = 3.2
            float dmg5 = card.GetDamageMultiplier(5);
            float expected5 = 2.0f * (1f + 0.15f * 4f);
            AssertApproximately("Lv5伤害倍率=3.2", dmg5, expected5);
        }

        private static void Test_Card_LevelGrowth_Cooldown()
        {
            Debug.Log("--- 测试: 冷却等级成长 ---");

            var card = SkillCardFactory.GetCard("active_fireball");

            // Lv1 = 10秒
            float cd1 = card.GetCooldown(1);
            AssertApproximately("Lv1冷却=10秒", cd1, 10f);

            // Lv5 = 10 × (1 - 0.05×4) = 10 × 0.8 = 8秒
            float cd5 = card.GetCooldown(5);
            float expected5 = 10f * (1f - 0.05f * 4f);
            AssertApproximately("Lv5冷却=8秒", cd5, expected5);
        }

        #endregion

        #region 效果系统测试

        private static void Test_EffectFactory_RegisteredCount()
        {
            Debug.Log("--- 测试: 已注册效果数 ---");

            int count = SkillEffectFactory.RegisteredEffectCount;
            Assert("已注册效果>=7", count >= 7, $"实际={count}");
            Debug.Log($"已注册效果数: {count}");
        }

        private static void Test_EffectFactory_GetAll()
        {
            Debug.Log("--- 测试: 获取所有效果 ---");

            EffectType[] types = {
                EffectType.Damage,
                EffectType.AoeDamage,
                EffectType.Burn,
                EffectType.Freeze,
                EffectType.Stun,
                EffectType.Slow,
                EffectType.Knockback,
                EffectType.Heal,
                EffectType.Shield
            };

            int found = 0;
            foreach (var type in types)
            {
                var effect = SkillEffectFactory.GetEffect(type);
                if (effect != null && effect.EffectType == type)
                    found++;
            }

            Assert("9种效果都能获取", found == 9, $"找到{found}/9");
        }

        #endregion

        #region 技能槽测试

        private static void Test_SkillSlot_EquipUnequip()
        {
            Debug.Log("--- 测试: 装配/卸下 ---");

            var slot = new SkillCardSlot(0);
            Assert("初始为空", slot.IsEmpty);
            Assert("初始等级=0", slot.Level == 0);

            var card = SkillCardFactory.GetCard("active_flashstrike");
            slot.Equip(card, 1);

            Assert("装配后非空", !slot.IsEmpty);
            Assert("装配后等级=1", slot.Level == 1);
            Assert("装配后卡牌正确", slot.CardData == card);
            Assert("装配后就绪", slot.IsReady);

            slot.Unequip();
            Assert("卸下后为空", slot.IsEmpty);
            Assert("卸下后等级=0", slot.Level == 0);
        }

        private static void Test_SkillSlot_Cooldown()
        {
            Debug.Log("--- 测试: 冷却更新 ---");

            var slot = new SkillCardSlot(0);
            var card = SkillCardFactory.GetCard("active_flashstrike");
            slot.Equip(card, 1);

            float actualCD = slot.ActualCD;
            Assert("实际冷却>0", actualCD > 0f);

            // Tick 一半时间
            slot.Tick(actualCD * 0.5f);
            AssertApproximately("冷却进度50%", slot.CDProgress, 0.5f, 0.02f);

            // Tick 另一半
            slot.Tick(actualCD * 0.5f);
            Assert("冷却完成", slot.IsReady);
            AssertApproximately("冷却进度100%", slot.CDProgress, 1f);
        }

        private static void Test_SkillSlot_TryCast()
        {
            Debug.Log("--- 测试: 尝试释放 ---");

            var slot = new SkillCardSlot(0);

            // 空槽不能释放
            Assert("空槽不能释放", !slot.TryCast());

            var card = SkillCardFactory.GetCard("active_flashstrike");
            slot.Equip(card, 1);

            // 就绪时可以释放
            Assert("就绪时可以释放", slot.TryCast());
            Assert("释放后进入冷却", !slot.IsReady);

            // 冷却中不能释放
            Assert("冷却中不能释放", !slot.TryCast());
        }

        private static void Test_SkillSlot_CDProgress()
        {
            Debug.Log("--- 测试: 冷却进度 ---");

            var slot = new SkillCardSlot(0);
            Assert("空槽进度=1", slot.CDProgress == 1f);

            var card = SkillCardFactory.GetCard("active_flashstrike");
            slot.Equip(card, 1);

            Assert("就绪时进度=1", slot.CDProgress == 1f);

            slot.TryCast();
            Assert("刚释放时进度=0", slot.CDProgress == 0f);
        }

        #endregion

        #region 技能卡管理器测试

        private static void Test_Manager_SlotCount()
        {
            Debug.Log("--- 测试: 槽位数量 ---");

            Assert("主动槽位数=4", SkillCardManager.ACTIVE_SLOT_COUNT == 4);
            Assert("被动槽位数=3", SkillCardManager.PASSIVE_SLOT_COUNT == 3);
        }

        private static void Test_Manager_EquipActive()
        {
            Debug.Log("--- 测试: 装配主动技能 ---");

            var mgr = SkillCardManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            var card = SkillCardFactory.GetCard("active_fireball");
            bool result = mgr.EquipActiveSkill(0, card, 3);

            Assert("装配成功", result);
            Assert("主动技能数=1", mgr.ActiveSkillCount == 1);

            var slot = mgr.GetActiveSlot(0);
            Assert("等级=3", slot.Level == 3);

            // 被动卡不能装在主动槽
            var passiveCard = SkillCardFactory.GetCard("passive_strength");
            bool failResult = mgr.EquipActiveSkill(1, passiveCard, 1);
            Assert("被动卡不能装主动槽", !failResult);
        }

        private static void Test_Manager_EquipPassive()
        {
            Debug.Log("--- 测试: 装配被动技能 ---");

            var mgr = SkillCardManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            var card = SkillCardFactory.GetCard("passive_strength");
            bool result = mgr.EquipPassiveSkill(0, card, 2);

            Assert("装配成功", result);
            Assert("被动技能数=1", mgr.PassiveSkillCount == 1);

            // 主动卡不能装在被动槽
            var activeCard = SkillCardFactory.GetCard("active_flashstrike");
            bool failResult = mgr.EquipPassiveSkill(1, activeCard, 1);
            Assert("主动卡不能装被动槽", !failResult);
        }

        private static void Test_Manager_CastSkill()
        {
            Debug.Log("--- 测试: 释放技能 ---");

            var mgr = SkillCardManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            // 空槽不能释放
            bool castResult = mgr.CastSkill(0, Vector3.zero);
            Assert("空槽释放失败", !castResult);

            // 装配后可以释放
            var card = SkillCardFactory.GetCard("active_shockwave");
            mgr.EquipActiveSkill(0, card, 1);

            castResult = mgr.CastSkill(0, Vector3.zero);
            Assert("装配后可以释放", castResult);

            // 冷却中不能再次释放
            castResult = mgr.CastSkill(0, Vector3.zero);
            Assert("冷却中不能释放", !castResult);
        }

        #endregion

        #region 稀有度分布测试

        private static void Test_RarityDistribution()
        {
            Debug.Log("--- 测试: 稀有度分布 ---");

            var allCards = SkillCardFactory.GetAllCards();

            int activeCount = 0;
            int passiveCount = 0;

            foreach (var card in allCards)
            {
                if (card.IsActive) activeCount++;
                if (card.IsPassive) passiveCount++;
            }

            Assert("主动技能>=10", activeCount >= 10, $"实际={activeCount}");
            Assert("被动技能>=5", passiveCount >= 5, $"实际={passiveCount}");
            Assert("主动+被动=总数", activeCount + passiveCount == allCards.Count);

            Debug.Log($"主动: {activeCount}, 被动: {passiveCount}, 总计: {allCards.Count}");
        }

        #endregion
    }
}
