using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Tower;
using LostIsland.Entity;

namespace LostIsland.Tests
{
    /// <summary>
    /// 塔内建造与经营系统单元测试
    /// 验证楼层管理、电力系统、建筑配置、防御塔战斗、加成系统等
    /// </summary>
    public class TowerSystemTests
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

            Debug.Log("========== 塔内建造与经营系统测试开始 ==========");

            // 楼层系统测试
            Test_FloorStructure();
            Test_FloorUnlockByDay();
            Test_SlotUnlock();

            // 建筑配置测试
            Test_BuildingConfig_AllTypes();
            Test_BuildingConfig_DefenseStats();
            Test_UpgradeCostGrowth();

            // 电力系统测试
            Test_PowerSystem_BasePower();
            Test_PowerSystem_Generator();
            Test_PowerSystem_CanBuild();

            // 建筑工厂测试
            Test_BuildingFactory_CreateAll();
            Test_BuildingFactory_Categories();

            // 防御塔测试
            Test_DefenseTower_MachineGun_Stats();
            Test_DefenseTower_Cannon_Stats();
            Test_DefenseTower_Tesla_Chain();

            // 加成系统测试
            Test_BuildingBonus_Layers();
            Test_BuildingUpgrade_BonusIncrease();

            // 综合测试
            Test_FullBuildCycle();

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
                // Debug.Log($"✅ {testName}");
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
            Assert(testName, equal, $"expected={expected:F4}, actual={actual:F4}, diff={Mathf.Abs(actual - expected):F6}");
        }

        #endregion

        #region 楼层系统测试

        private static void Test_FloorStructure()
        {
            Debug.Log("--- 测试: 楼层结构 ---");

            // 手动创建楼层数据
            var floors = CreateTestFloors();

            Assert("共7层楼", floors.Count == 7);
            Assert("B1F=3房间", floors[0].RoomSlots.Count == 3);
            Assert("1F=4房间", floors[1].RoomSlots.Count == 4);
            Assert("2F=3房间", floors[2].RoomSlots.Count == 3);
            Assert("3F=3房间", floors[3].RoomSlots.Count == 3);
            Assert("4F=3房间", floors[4].RoomSlots.Count == 3);
            Assert("5F=2房间", floors[5].RoomSlots.Count == 2);
            Assert("塔顶=1房间", floors[6].RoomSlots.Count == 1);

            // 总房间数 = 3+4+3+3+3+2+1 = 19
            int totalSlots = 0;
            foreach (var f in floors) totalSlots += f.RoomSlots.Count;
            Assert("总房间数=19", totalSlots == 19);
        }

        private static void Test_FloorUnlockByDay()
        {
            Debug.Log("--- 测试: 楼层按天数解锁 ---");

            var floors = CreateTestFloors();

            // 第1天：B1F、1F、塔顶解锁
            Assert("第1天: B1F解锁", floors[0].UnlockDay == 1);
            Assert("第1天: 1F解锁", floors[1].UnlockDay == 1);
            Assert("第1天: 塔顶解锁", floors[6].UnlockDay == 1);

            // 第2天：2F解锁
            Assert("第2天: 2F解锁", floors[2].UnlockDay == 2);

            // 第4天：3F解锁
            Assert("第4天: 3F解锁", floors[3].UnlockDay == 4);

            // 第6天：4F解锁
            Assert("第6天: 4F解锁", floors[4].UnlockDay == 6);

            // 第8天：5F解锁
            Assert("第8天: 5F解锁", floors[5].UnlockDay == 8);
        }

        private static void Test_SlotUnlock()
        {
            Debug.Log("--- 测试: 房间槽位解锁 ---");

            var floor = new TowerFloorData(0, "B1F", 1, 3);

            // 第一个槽位免费
            Assert("第1个槽位免费", floor.RoomSlots[0].UnlockCost == 0);

            // 后续槽位递增
            Assert("第2个槽位>0", floor.RoomSlots[1].UnlockCost > 0);
            Assert("第3个槽位>第2个", floor.RoomSlots[2].UnlockCost > floor.RoomSlots[1].UnlockCost);
        }

        private static List<TowerFloorData> CreateTestFloors()
        {
            return new List<TowerFloorData>
            {
                new TowerFloorData(0, "B1F 地下室", 1, 3),
                new TowerFloorData(1, "1F 大厅", 1, 4),
                new TowerFloorData(2, "2F 生活区", 2, 3),
                new TowerFloorData(3, "3F 工坊层", 4, 3),
                new TowerFloorData(4, "4F 种植层", 6, 3),
                new TowerFloorData(5, "5F 监控层", 8, 2),
                new TowerFloorData(6, "塔顶", 1, 1),
            };
        }

        #endregion

        #region 建筑配置测试

        private static void Test_BuildingConfig_AllTypes()
        {
            Debug.Log("--- 测试: 所有建筑配置 ---");

            int defenseCount = 0;
            int resourceCount = 0;
            int utilityCount = 0;

            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                if (type == BuildingType.None) continue;

                var config = BuildingConfigFactory.GetConfig(type);
                Assert($"{type} 配置存在", config != null);

                if (config == null) continue;

                Assert($"{type} 名称非空", !string.IsNullOrEmpty(config.BuildingName));
                Assert($"{type} 最大等级>=1", config.MaxLevel >= 1);

                switch (config.Category)
                {
                    case BuildingCategory.Defense: defenseCount++; break;
                    case BuildingCategory.Resource: resourceCount++; break;
                    case BuildingCategory.Utility: utilityCount++; break;
                }
            }

            Assert("防御类=4种", defenseCount == 4);
            Assert("资源类=3种", resourceCount == 3);
            Assert("功能类=6种", utilityCount == 6);
            Assert("总共13种建筑", defenseCount + resourceCount + utilityCount == 13);
        }

        private static void Test_BuildingConfig_DefenseStats()
        {
            Debug.Log("--- 测试: 防御塔属性成长 ---");

            // 机枪塔
            var mg = BuildingConfigFactory.GetConfig(BuildingType.MachineGun);
            Assert("机枪塔 Lv1伤害=8", mg.GetDamage(1) == 8f);
            Assert("机枪塔 Lv5伤害=24", mg.GetDamage(5) == 24f); // 8 + 4*4
            Assert("机枪塔 Lv1攻速=2.5", mg.GetAttackSpeed(1) == 2.5f);
            Assert("机枪塔 Lv5攻速=3.7", mg.GetAttackSpeed(5) == 3.7f); // 2.5 + 0.3*4

            // 炮塔
            var cannon = BuildingConfigFactory.GetConfig(BuildingType.Cannon);
            Assert("炮塔 Lv1伤害=30", cannon.GetDamage(1) == 30f);
            Assert("炮塔 AOE半径=2.5", cannon.AoeRadius == 2.5f);
            Assert("炮塔攻速 < 机枪塔攻速", cannon.BaseAttackSpeed < mg.BaseAttackSpeed);

            // 电击塔
            var tesla = BuildingConfigFactory.GetConfig(BuildingType.TeslaTower);
            Assert("电击塔 链式=3", tesla.ChainCount == 3);
            Assert("电击塔 电力消耗高", tesla.BuildPowerCost >= 7);
        }

        private static void Test_UpgradeCostGrowth()
        {
            Debug.Log("--- 测试: 升级消耗增长 ---");

            var config = BuildingConfigFactory.GetConfig(BuildingType.MachineGun);

            // Lv1→Lv2 消耗
            int cost1 = config.GetUpgradeCost(1);
            // Lv2→Lv3 消耗
            int cost2 = config.GetUpgradeCost(2);
            // Lv3→Lv4 消耗
            int cost3 = config.GetUpgradeCost(3);

            // 消耗递增
            Assert("升级消耗递增", cost2 > cost1 && cost3 > cost2);

            // 增长率约 25%
            float growth = (float)cost2 / cost1;
            Assert("增长率≈1.25", Mathf.Abs(growth - 1.25f) < 0.01f, $"实际={growth:F3}");

            // Lv5 不能再升级
            int cost5 = config.GetUpgradeCost(5);
            Assert("Lv5升级消耗=0（已满级）", cost5 == 0);
        }

        #endregion

        #region 电力系统测试

        private static void Test_PowerSystem_BasePower()
        {
            Debug.Log("--- 测试: 基础电力 ---");

            Assert("基础电力=20", PowerSystem.BASE_POWER == 20f);
        }

        private static void Test_PowerSystem_Generator()
        {
            Debug.Log("--- 测试: 发电机电力产出 ---");

            var gen = BuildingConfigFactory.GetConfig(BuildingType.Generator);

            Assert("发电机 Lv1产出=10", gen.GetPowerOutput(1) == 10);
            Assert("发电机 Lv5产出=30", gen.GetPowerOutput(5) == 30); // 10 + 5*4
            Assert("发电机不耗电", gen.BuildPowerCost == 0);
        }

        private static void Test_PowerSystem_CanBuild()
        {
            Debug.Log("--- 测试: 电力建造检查 ---");

            var gen = BuildingConfigFactory.GetConfig(BuildingType.Generator);
            var mg = BuildingConfigFactory.GetConfig(BuildingType.MachineGun);

            // 发电机总是可以建
            Assert("发电机总是可建", CheckCanBuild(gen, 20f, 15f));

            // 电力足够时可以建
            Assert("电力足够时可建机枪塔", CheckCanBuild(mg, 20f, 15f));

            // 电力不足时不能建
            Assert("电力不足时不可建", !CheckCanBuild(mg, 5f, 3f));
        }

        private static bool CheckCanBuild(BuildingConfigSO config, float maxPower, float usedPower)
        {
            float remaining = maxPower - usedPower;
            if (config.BuildingType == BuildingType.Generator) return true;
            return remaining >= config.BuildPowerCost;
        }

        #endregion

        #region 建筑工厂测试

        private static void Test_BuildingFactory_CreateAll()
        {
            Debug.Log("--- 测试: 建筑工厂创建所有类型 ---");

            int successCount = 0;
            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                if (type == BuildingType.None) continue;

                var building = BuildingFactory.CreateBuilding(type);
                if (building != null)
                {
                    successCount++;
                    Assert($"{type} 创建成功", building.BuildingType == type);
                }
            }

            Assert("所有13种建筑都能创建", successCount == 13);
        }

        private static void Test_BuildingFactory_Categories()
        {
            Debug.Log("--- 测试: 建筑分类 ---");

            Assert("机枪塔是防御类", BuildingFactory.IsDefense(BuildingType.MachineGun));
            Assert("炮塔是防御类", BuildingFactory.IsDefense(BuildingType.Cannon));
            Assert("废料回收站是资源类", BuildingFactory.IsResource(BuildingType.ScrapRecycler));
            Assert("发电机是功能类", BuildingFactory.IsUtility(BuildingType.Generator));
            Assert("医疗室是功能类", BuildingFactory.IsUtility(BuildingType.MedBay));
            Assert("防御塔不是资源类", !BuildingFactory.IsResource(BuildingType.MachineGun));
        }

        #endregion

        #region 防御塔测试

        private static void Test_DefenseTower_MachineGun_Stats()
        {
            Debug.Log("--- 测试: 机枪塔属性 ---");

            var config = BuildingConfigFactory.GetConfig(BuildingType.MachineGun);
            var tower = new DefenseTower_MachineGun();
            tower.Initialize(config, 1, 1, 0);
            tower.TowerPosition = Vector3.zero;

            Assert("机枪塔 Lv1伤害=8", tower.Damage == 8f);
            Assert("机枪塔 Lv1攻速=2.5", tower.AttackSpeed == 2.5f);
            Assert("机枪塔 攻击间隔=0.4秒", Mathf.Abs(tower.AttackInterval - 0.4f) < 0.01f);
            Assert("机枪塔 射程=6", tower.Range == 6f);
            Assert("机枪塔 单体攻击", tower.AttackType == AttackType.SingleShot);
        }

        private static void Test_DefenseTower_Cannon_Stats()
        {
            Debug.Log("--- 测试: 炮塔属性 ---");

            var config = BuildingConfigFactory.GetConfig(BuildingType.Cannon);
            var tower = new DefenseTower_Cannon();
            tower.Initialize(config, 1, 1, 1);
            tower.TowerPosition = Vector3.zero;

            Assert("炮塔 Lv1伤害=30", tower.Damage == 30f);
            Assert("炮塔 AOE攻击", tower.AttackType == AttackType.AOE);
            Assert("炮塔 射程=7", tower.Range == 7f);
            Assert("炮塔 攻速 < 机枪塔", tower.AttackSpeed < BuildingConfigFactory.GetConfig(BuildingType.MachineGun).BaseAttackSpeed);
        }

        private static void Test_DefenseTower_Tesla_Chain()
        {
            Debug.Log("--- 测试: 电击塔链式 ---");

            var config = BuildingConfigFactory.GetConfig(BuildingType.TeslaTower);
            var tower = new DefenseTower_Tesla();
            tower.Initialize(config, 3, 5, 0);
            tower.TowerPosition = Vector3.zero;

            Assert("电击塔 链式攻击", tower.AttackType == AttackType.Chain);
            Assert("电击塔 链数=3", config.ChainCount == 3);
            Assert("电击塔 Lv3伤害=24", tower.Damage == 24f); // 12 + 6*2
        }

        #endregion

        #region 加成系统测试

        private static void Test_BuildingBonus_Layers()
        {
            Debug.Log("--- 测试: 建筑加成层级 ---");

            var medbay = BuildingConfigFactory.GetConfig(BuildingType.MedBay);
            var workshop = BuildingConfigFactory.GetConfig(BuildingType.Workshop);

            // 医疗室加生命
            bool hasHPBonus = false;
            foreach (var bonus in medbay.Bonuses)
            {
                if (bonus.AttrType == AttrType.MaxHP && bonus.Layer == AttrLayer.Pct_Tower)
                {
                    hasHPBonus = true;
                    Assert("医疗室每级+15%HP", bonus.ValuePerLevel == 0.15f);
                }
            }
            Assert("医疗室有HP加成", hasHPBonus);

            // 工坊加攻击
            bool hasATKBonus = false;
            foreach (var bonus in workshop.Bonuses)
            {
                if (bonus.AttrType == AttrType.ATK && bonus.Layer == AttrLayer.Pct_Tower)
                {
                    hasATKBonus = true;
                    Assert("工坊每级+10%ATK", bonus.ValuePerLevel == 0.10f);
                }
            }
            Assert("工坊有ATK加成", hasATKBonus);
        }

        private static void Test_BuildingUpgrade_BonusIncrease()
        {
            Debug.Log("--- 测试: 升级后加成增加 ---");

            var config = BuildingConfigFactory.GetConfig(BuildingType.MedBay);

            // Lv1 总加成
            float bonus1 = 0f;
            foreach (var b in config.Bonuses)
            {
                if (b.AttrType == AttrType.MaxHP) bonus1 = b.GetTotalValue(1);
            }

            // Lv5 总加成
            float bonus5 = 0f;
            foreach (var b in config.Bonuses)
            {
                if (b.AttrType == AttrType.MaxHP) bonus5 = b.GetTotalValue(5);
            }

            Assert("Lv5加成是Lv1的5倍", Mathf.Abs(bonus5 - bonus1 * 5f) < 0.001f);
            Assert("Lv5=+75%HP", bonus5 == 0.75f); // 0.15 * 5
        }

        #endregion

        #region 综合测试

        private static void Test_FullBuildCycle()
        {
            Debug.Log("--- 测试: 完整建造升级流程 ---");

            // 模拟一个完整的建造-升级流程
            var config = BuildingConfigFactory.GetConfig(BuildingType.MachineGun);
            var building = BuildingFactory.CreateAndInitialize(BuildingType.MachineGun, 1, 1, 0);

            Assert("初始等级=1", building.Level == 1);
            Assert("初始伤害=8", (building as DefenseTowerBase)?.Damage == 8f);

            // 模拟升级到3级
            building.Level = 3;
            building.OnPreUpgrade();
            building.Level = 3;
            building.OnPostUpgrade();

            var tower = building as DefenseTowerBase;
            Assert("Lv3伤害=16", tower?.Damage == 16f); // 8 + 4*2

            // 升级到5级（满级）
            building.Level = 5;
            building.OnPreUpgrade();
            building.Level = 5;
            building.OnPostUpgrade();

            tower = building as DefenseTowerBase;
            Assert("Lv5伤害=24", tower?.Damage == 24f); // 8 + 4*4
            Assert("Lv5攻速=3.7", tower?.AttackSpeed == 3.7f);
            Assert("Lv5射程=8", tower?.Range == 8f);

            // 满级不能再升
            Assert("满级CanUpgrade=false", !building.CanUpgrade());

            // 拆除
            building.OnRemoved();
            Assert("拆除后无异常", true);
        }

        #endregion
    }
}
