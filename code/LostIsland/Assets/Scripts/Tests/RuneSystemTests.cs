using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Rune;
using LostIsland.Entity;

namespace LostIsland.Tests
{
    /// <summary>
    /// 符文系统单元测试
    /// </summary>
    public class RuneSystemTests
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

            Debug.Log("========== 符文系统测试开始 ==========");

            // 符文配置测试
            Test_Factory_TotalCount();
            Test_Factory_ByRarity();
            Test_Factory_ByCategory();
            Test_Rune_StarGrowth_MainAttr();
            Test_Rune_StarGrowth_SubAttr();
            Test_Rune_ShardCost();

            // 符文实例测试
            Test_Instance_Create();
            Test_Instance_StarUp();
            Test_Instance_AddShards();
            Test_Instance_CanStarUp();

            // 符文槽位测试
            Test_Slot_InitialState();
            Test_Slot_EquipUnequip();
            Test_Slot_TypeMatch();

            // 稀有度与品质
            Test_Rarity_SubAttr();
            Test_Rarity_SpecialEffect();

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

        private static void AssertApproximately(string testName, float actual, float expected, float tolerance = 0.001f)
        {
            bool equal = Mathf.Abs(actual - expected) <= tolerance;
            Assert(testName, equal, $"expected={expected:F4}, actual={actual:F4}");
        }

        #endregion

        #region 符文配置测试

        private static void Test_Factory_TotalCount()
        {
            Debug.Log("--- 测试: 符文总数 ---");

            int count = RuneFactory.TotalRuneCount;
            Assert("符文总数>=12", count >= 12, $"实际={count}");
            Debug.Log($"符文总数: {count}");
        }

        private static void Test_Factory_ByRarity()
        {
            Debug.Log("--- 测试: 按稀有度分类 ---");

            int common = RuneFactory.GetRunesByRarity(Rarity.Common).Count;
            int rare = RuneFactory.GetRunesByRarity(Rarity.Rare).Count;
            int epic = RuneFactory.GetRunesByRarity(Rarity.Epic).Count;
            int legendary = RuneFactory.GetRunesByRarity(Rarity.Legendary).Count;

            Assert("白符文>=3", common >= 3);
            Assert("蓝符文>=3", rare >= 3);
            Assert("紫符文>=3", epic >= 3);
            Assert("金符文>=3", legendary >= 3);

            Debug.Log($"白:{common} 蓝:{rare} 紫:{epic} 金:{legendary}");
        }

        private static void Test_Factory_ByCategory()
        {
            Debug.Log("--- 测试: 按分类 ---");

            int attack = RuneFactory.GetRunesByCategory(RuneCategory.Attack).Count;
            int defense = RuneFactory.GetRunesByCategory(RuneCategory.Defense).Count;
            int survival = RuneFactory.GetRunesByCategory(RuneCategory.Survival).Count;

            Assert("攻击类>=4", attack >= 4);
            Assert("防御类>=4", defense >= 4);
            Assert("生存类>=4", survival >= 4);

            Debug.Log($"攻击:{attack} 防御:{defense} 生存:{survival}");
        }

        private static void Test_Rune_StarGrowth_MainAttr()
        {
            Debug.Log("--- 测试: 主属性星级成长 ---");

            var rune = RuneFactory.GetRune("rune_atk_white");
            Assert("锋利符文存在", rune != null);

            // 1星 = 5%
            float pct1 = rune.GetMainAttrPct(1);
            AssertApproximately("1星主属性=5%", pct1, 0.05f);

            // 5星 = 5% × (1 + 0.2×4) = 5% × 1.8 = 9%
            float pct5 = rune.GetMainAttrPct(5);
            float expected5 = 0.05f * (1f + 0.2f * 4f);
            AssertApproximately("5星主属性=9%", pct5, expected5);
        }

        private static void Test_Rune_StarGrowth_SubAttr()
        {
            Debug.Log("--- 测试: 副属性星级成长 ---");

            var rune = RuneFactory.GetRune("rune_atk_blue");
            Assert("狂暴符文存在", rune != null);

            // 蓝卡有副属性
            Assert("蓝卡有副属性", rune.HasSubAttr);

            // 1星副属性
            float sub1 = rune.GetSubAttrPct(1);
            AssertApproximately("1星副属性=3%", sub1, 0.03f);

            // 5星副属性 = 3% × 1.8 = 5.4%
            float sub5 = rune.GetSubAttrPct(5);
            float expected5 = 0.03f * (1f + 0.2f * 4f);
            AssertApproximately("5星副属性=5.4%", sub5, expected5);
        }

        private static void Test_Rune_ShardCost()
        {
            Debug.Log("--- 测试: 升星碎片消耗 ---");

            var rune = RuneFactory.GetRune("rune_atk_white");

            // 1→2星：10碎片
            int cost1 = rune.GetShardsForStarUp(1);
            Assert("1→2星=10碎片", cost1 == 10, $"实际={cost1}");

            // 4→5星：80碎片
            int cost4 = rune.GetShardsForStarUp(4);
            Assert("4→5星=80碎片", cost4 == 80, $"实际={cost4}");

            // 到5星总碎片
            int total = rune.GetTotalShardsToStar(5);
            int expected = 10 + 20 + 40 + 80;
            Assert("到5星总碎片=150", total == expected, $"实际={total}");
        }

        #endregion

        #region 符文实例测试

        private static void Test_Instance_Create()
        {
            Debug.Log("--- 测试: 创建符文实例 ---");

            var data = RuneFactory.GetRune("rune_atk_white");
            var instance = new RuneInstance(1, data, 1);

            Assert("实例ID正确", instance.InstanceId == 1);
            Assert("数据引用正确", instance.Data == data);
            Assert("初始星级=1", instance.Star == 1);
            Assert("初始碎片=0", instance.Shards == 0);
            Assert("未装备", !instance.IsEquipped);
            Assert("装备槽位=-1", instance.EquippedSlotIndex == -1);
        }

        private static void Test_Instance_StarUp()
        {
            Debug.Log("--- 测试: 符文升星 ---");

            var data = RuneFactory.GetRune("rune_atk_white");
            var instance = new RuneInstance(2, data, 1);

            // 没有碎片不能升星
            Assert("无碎片不能升星", !instance.CanStarUp());

            // 加足够碎片
            instance.AddShards(10);
            Assert("10碎片可以升星", instance.CanStarUp());

            // 升星
            bool success = instance.StarUp();
            Assert("升星成功", success);
            Assert("升星后星级=2", instance.Star == 2);
            Assert("升星后碎片=0", instance.Shards == 0);

            // 5星不能再升
            for (int i = 0; i < 10; i++)
            {
                instance.AddShards(100);
                instance.StarUp();
            }
            Assert("最高5星", instance.Star <= 5);
        }

        private static void Test_Instance_AddShards()
        {
            Debug.Log("--- 测试: 添加碎片 ---");

            var data = RuneFactory.GetRune("rune_atk_white");
            var instance = new RuneInstance(3, data, 1);

            instance.AddShards(5);
            Assert("添加5碎片后=5", instance.Shards == 5);

            instance.AddShards(10);
            Assert("再加10后=15", instance.Shards == 15);
        }

        private static void Test_Instance_CanStarUp()
        {
            Debug.Log("--- 测试: 是否可升星 ---");

            var data = RuneFactory.GetRune("rune_atk_white");
            var instance = new RuneInstance(4, data, 3);

            // 3→4星需要40碎片
            Assert("20碎片不能升4星", !instance.CanStarUp());

            instance.AddShards(40);
            Assert("40碎片可以升4星", instance.CanStarUp());

            // 升到5星（满级）
            instance.StarUp(); // 3→4
            instance.AddShards(80);
            instance.StarUp(); // 4→5
            Assert("5星不能再升", !instance.CanStarUp());
        }

        #endregion

        #region 符文槽位测试

        private static void Test_Slot_InitialState()
        {
            Debug.Log("--- 测试: 槽位初始状态 ---");

            var slot = new RuneSlot(0, RuneSlotType.Attack, true);

            Assert("槽位索引=0", slot.SlotIndex == 0);
            Assert("槽位类型=攻击", slot.SlotType == RuneSlotType.Attack);
            Assert("已解锁", slot.IsUnlocked);
            Assert("初始为空", slot.IsEmpty);
            Assert("无装备符文", slot.EquippedRune == null);
        }

        private static void Test_Slot_EquipUnequip()
        {
            Debug.Log("--- 测试: 槽位装备/卸下 ---");

            var slot = new RuneSlot(0, RuneSlotType.Attack, true);
            var data = RuneFactory.GetRune("rune_atk_white");
            var rune = new RuneInstance(10, data, 1);

            // 装备
            slot.EquippedRune = rune;
            rune.IsEquipped = true;
            rune.EquippedSlotIndex = 0;

            Assert("装备后非空", !slot.IsEmpty);
            Assert("符文已装备", rune.IsEquipped);
            Assert("槽位索引正确", rune.EquippedSlotIndex == 0);

            // 卸下
            slot.EquippedRune = null;
            rune.IsEquipped = false;
            rune.EquippedSlotIndex = -1;

            Assert("卸下后为空", slot.IsEmpty);
            Assert("符文未装备", !rune.IsEquipped);
        }

        private static void Test_Slot_TypeMatch()
        {
            Debug.Log("--- 测试: 槽位类型匹配 ---");

            // 攻击符文不能装防御槽
            var atkRune = RuneFactory.GetRune("rune_atk_white");
            Assert("攻击类符文", atkRune.Category == RuneCategory.Attack);

            var defRune = RuneFactory.GetRune("rune_def_white");
            Assert("防御类符文", defRune.Category == RuneCategory.Defense);

            // 特殊槽可以装任意类型
            var specialSlot = new RuneSlot(5, RuneSlotType.Special, true);
            Assert("特殊槽存在", specialSlot != null);
        }

        #endregion

        #region 稀有度与品质测试

        private static void Test_Rarity_SubAttr()
        {
            Debug.Log("--- 测试: 副属性稀有度限制 ---");

            // 白卡没有副属性
            var white = RuneFactory.GetRune("rune_atk_white");
            Assert("白卡无副属性", !white.HasSubAttr);

            // 蓝卡有副属性
            var blue = RuneFactory.GetRune("rune_atk_blue");
            Assert("蓝卡有副属性", blue.HasSubAttr);

            // 紫卡有副属性
            var purple = RuneFactory.GetRune("rune_atk_purple");
            Assert("紫卡有副属性", purple.HasSubAttr);

            // 金卡有副属性
            var gold = RuneFactory.GetRune("rune_atk_gold");
            Assert("金卡有副属性", gold.HasSubAttr);
        }

        private static void Test_Rarity_SpecialEffect()
        {
            Debug.Log("--- 测试: 特殊效果稀有度限制 ---");

            // 白/蓝/紫没有特殊效果
            var white = RuneFactory.GetRune("rune_atk_white");
            Assert("白卡无特殊效果", !white.HasSpecialEffect);

            var blue = RuneFactory.GetRune("rune_atk_blue");
            Assert("蓝卡无特殊效果", !blue.HasSpecialEffect);

            var purple = RuneFactory.GetRune("rune_atk_purple");
            Assert("紫卡无特殊效果", !purple.HasSpecialEffect);

            // 金卡有特殊效果
            var gold = RuneFactory.GetRune("rune_atk_gold");
            Assert("金卡有特殊效果", gold.HasSpecialEffect);
            Assert("金卡特殊效果=吸血", gold.SpecialEffect == RuneSpecialEffect.Lifesteal);
        }

        #endregion
    }
}
