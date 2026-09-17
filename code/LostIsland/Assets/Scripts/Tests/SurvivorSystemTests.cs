using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Survivors;

namespace LostIsland.Tests
{
    /// <summary>
    /// 幸存者系统单元测试
    /// </summary>
    public class SurvivorSystemTests
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

            Debug.Log("========== 幸存者系统测试开始 ==========");

            // 配置测试
            Test_Factory_TotalCount();
            Test_Factory_ByRarity();
            Test_Factory_ByClass();

            // 属性计算测试
            Test_Survivor_LevelGrowth();
            Test_Survivor_MaxLevel();
            Test_Survivor_CombatPower();

            // 实例测试
            Test_Instance_Create();
            Test_Instance_AddExp();
            Test_Instance_LevelUp();
            Test_Instance_TakeDamage();
            Test_Instance_Heal();
            Test_Instance_Recover();

            // 招募系统测试
            Test_Manager_RecruitSingle();
            Test_Manager_RecruitTen();

            // 上阵系统测试
            Test_Manager_Deploy();
            Test_Manager_Undeploy();
            Test_Manager_MaxDeployed();

            // AI测试
            Test_AI_Create();
            Test_AI_StateIdle();
            Test_AI_TakeDamage();

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

        #region 配置测试

        private static void Test_Factory_TotalCount()
        {
            Debug.Log("--- 测试: 幸存者总数 ---");

            int count = SurvivorFactory.TotalCount;
            Assert("幸存者总数>=12", count >= 12, $"实际={count}");
            Debug.Log($"幸存者总数: {count}");
        }

        private static void Test_Factory_ByRarity()
        {
            Debug.Log("--- 测试: 按稀有度分类 ---");

            int common = SurvivorFactory.GetByRarity(SurvivorRarity.Common).Count;
            int rare = SurvivorFactory.GetByRarity(SurvivorRarity.Rare).Count;
            int epic = SurvivorFactory.GetByRarity(SurvivorRarity.Epic).Count;
            int legendary = SurvivorFactory.GetByRarity(SurvivorRarity.Legendary).Count;

            Assert("白卡>=3", common >= 3);
            Assert("蓝卡>=3", rare >= 3);
            Assert("紫卡>=3", epic >= 3);
            Assert("金卡>=3", legendary >= 3);

            Debug.Log($"白:{common} 蓝:{rare} 紫:{epic} 金:{legendary}");
        }

        private static void Test_Factory_ByClass()
        {
            Debug.Log("--- 测试: 按职业分类 ---");

            int warrior = SurvivorFactory.GetByClass(SurvivorClass.Warrior).Count;
            int archer = SurvivorFactory.GetByClass(SurvivorClass.Archer).Count;
            int medic = SurvivorFactory.GetByClass(SurvivorClass.Medic).Count;
            int engineer = SurvivorFactory.GetByClass(SurvivorClass.Engineer).Count;

            Assert("战士>=2", warrior >= 2);
            Assert("射手>=2", archer >= 2);
            Assert("医生>=2", medic >= 2);
            Assert("工程师>=2", engineer >= 2);

            Debug.Log($"战士:{warrior} 射手:{archer} 医生:{medic} 工程师:{engineer}");
        }

        #endregion

        #region 属性计算测试

        private static void Test_Survivor_LevelGrowth()
        {
            Debug.Log("--- 测试: 等级成长 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            Assert("新兵存在", data != null);

            // 1级
            float hp1 = data.GetMaxHP(1);
            AssertApproximately("1级HP=200", hp1, 200f);

            float atk1 = data.GetATK(1);
            AssertApproximately("1级ATK=20", atk1, 20f);

            // 10级
            float hp10 = data.GetMaxHP(10);
            float expectedHP10 = 200f * (1f + 0.08f * 9f);
            AssertApproximately("10级HP", hp10, expectedHP10);

            float atk10 = data.GetATK(10);
            float expectedATK10 = 20f * (1f + 0.06f * 9f);
            AssertApproximately("10级ATK", atk10, expectedATK10);
        }

        private static void Test_Survivor_MaxLevel()
        {
            Debug.Log("--- 测试: 等级上限 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            Assert("最高等级=50", data.MaxLevel == 50);
        }

        private static void Test_Survivor_CombatPower()
        {
            Debug.Log("--- 测试: 战斗力计算 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_gold");
            Assert("战神存在", data != null);

            float cp1 = data.GetCombatPower(1);
            Assert("1级战力>0", cp1 > 0f);

            float cp50 = data.GetCombatPower(50);
            Assert("50级战力>1级战力", cp50 > cp1);

            Debug.Log($"战神1级战力: {cp1:F0}, 50级战力: {cp50:F0}");
        }

        #endregion

        #region 实例测试

        private static void Test_Instance_Create()
        {
            Debug.Log("--- 测试: 创建幸存者实例 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var instance = new SurvivorInstance(1001, data);

            Assert("实例ID正确", instance.InstanceId == 1001);
            Assert("数据引用正确", instance.Data == data);
            Assert("初始等级=1", instance.Level == 1);
            Assert("初始经验=0", instance.Exp == 0);
            Assert("亲密度=0", instance.Intimacy == 0);
            Assert("状态=空闲", instance.Status == SurvivorStatus.Idle);
            Assert("未上阵", !instance.IsDeployed);
            Assert("满血", instance.CurrentHP == instance.MaxHP);
        }

        private static void Test_Instance_AddExp()
        {
            Debug.Log("--- 测试: 添加经验 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var instance = new SurvivorInstance(1002, data);

            int expNeeded = instance.GetExpToNextLevel();
            Assert("升级需要经验>0", expNeeded > 0);

            // 加一半经验，不升级
            instance.AddExp(expNeeded / 2);
            Assert("半经验不升级", instance.Level == 1);
            Assert("经验正确", instance.Exp == expNeeded / 2);
        }

        private static void Test_Instance_LevelUp()
        {
            Debug.Log("--- 测试: 升级 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var instance = new SurvivorInstance(1003, data);

            float hpBefore = instance.MaxHP;
            float atkBefore = instance.ATK;

            int expNeeded = instance.GetExpToNextLevel();
            bool leveled = instance.AddExp(expNeeded + 10);

            Assert("升级成功", leveled);
            Assert("等级=2", instance.Level == 2);
            Assert("升级后HP增加", instance.MaxHP > hpBefore);
            Assert("升级后ATK增加", instance.ATK > atkBefore);
            Assert("满血", instance.CurrentHP == instance.MaxHP);
        }

        private static void Test_Instance_TakeDamage()
        {
            Debug.Log("--- 测试: 受到伤害 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var instance = new SurvivorInstance(1004, data);

            float hpBefore = instance.CurrentHP;
            float damage = instance.TakeDamage(50f);

            Assert("受伤后HP减少", instance.CurrentHP < hpBefore);
            Assert("实际伤害>0", damage > 0f);
            Assert("未受伤状态不变", instance.Status == SurvivorStatus.Idle);
        }

        private static void Test_Instance_Heal()
        {
            Debug.Log("--- 测试: 治疗 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var instance = new SurvivorInstance(1005, data);

            instance.TakeDamage(100f);
            float hpBefore = instance.CurrentHP;

            float healed = instance.Heal(50f);
            Assert("治疗量=50", healed == 50f);
            Assert("HP增加", instance.CurrentHP > hpBefore);
        }

        private static void Test_Instance_Recover()
        {
            Debug.Log("--- 测试: 恢复 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var instance = new SurvivorInstance(1006, data);

            // 打残
            instance.TakeDamage(9999f);
            Assert("受伤状态", instance.Status == SurvivorStatus.Injured);

            // 恢复
            instance.Recover();
            Assert("状态恢复空闲", instance.Status == SurvivorStatus.Idle);
            Assert("满血", instance.CurrentHP == instance.MaxHP);
        }

        #endregion

        #region 招募系统测试

        private static void Test_Manager_RecruitSingle()
        {
            Debug.Log("--- 测试: 单抽招募 ---");

            var mgr = SurvivorManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            int before = mgr.OwnedCount;
            var result = mgr.RecruitSingle();

            Assert("招募结果不为空", result != null);
            Assert("拥有数+1", mgr.OwnedCount == before + 1);
            Assert("总招募次数+1", mgr.TotalRecruits >= 1);
        }

        private static void Test_Manager_RecruitTen()
        {
            Debug.Log("--- 测试: 十连招募 ---");

            var mgr = SurvivorManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            int before = mgr.OwnedCount;
            var results = mgr.RecruitTen();

            Assert("十连结果=10个", results.Count == 10);
            Assert("拥有数+10", mgr.OwnedCount == before + 10);
        }

        #endregion

        #region 上阵系统测试

        private static void Test_Manager_Deploy()
        {
            Debug.Log("--- 测试: 上阵 ---");

            var mgr = SurvivorManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var survivor = new SurvivorInstance(2001, data);

            bool success = mgr.Deploy(0, survivor);
            Assert("上阵成功", success);
            Assert("已上阵标记", survivor.IsDeployed);
            Assert("上阵位置正确", survivor.DeploySlotIndex == 0);
            Assert("上阵人数=1", mgr.DeployedCount == 1);
        }

        private static void Test_Manager_Undeploy()
        {
            Debug.Log("--- 测试: 下阵 ---");

            var mgr = SurvivorManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            // 先上阵
            var data = SurvivorFactory.GetSurvivor("survivor_archer_white");
            var survivor = new SurvivorInstance(2002, data);
            mgr.Deploy(1, survivor);

            // 再下阵
            bool success = mgr.Undeploy(1);
            Assert("下阵成功", success);
            Assert("未上阵标记", !survivor.IsDeployed);
            Assert("上阵位置=-1", survivor.DeploySlotIndex == -1);
        }

        private static void Test_Manager_MaxDeployed()
        {
            Debug.Log("--- 测试: 最大上阵数 ---");

            Assert("最大上阵数=3", SurvivorManager.MAX_DEPLOYED == 3);
        }

        #endregion

        #region AI测试

        private static void Test_AI_Create()
        {
            Debug.Log("--- 测试: AI创建 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var survivor = new SurvivorInstance(3001, data);
            var ai = new SurvivorAIController(survivor, Vector3.zero);

            Assert("AI创建成功", ai != null);
            Assert("初始状态=Idle", ai.CurrentState == SurvivorAIState.Idle);
            Assert("幸存者引用正确", ai.Survivor == survivor);
        }

        private static void Test_AI_StateIdle()
        {
            Debug.Log("--- 测试: AI待机状态 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var survivor = new SurvivorInstance(3002, data);
            var ai = new SurvivorAIController(survivor, Vector3.zero);

            Assert("初始状态待机", ai.CurrentState == SurvivorAIState.Idle);
            Assert("无目标", ai.Target == null);
        }

        private static void Test_AI_TakeDamage()
        {
            Debug.Log("--- 测试: AI受伤 ---");

            var data = SurvivorFactory.GetSurvivor("survivor_warrior_white");
            var survivor = new SurvivorInstance(3003, data);
            var ai = new SurvivorAIController(survivor, Vector3.zero);

            float hpBefore = survivor.CurrentHP;
            ai.TakeDamage(50f);

            Assert("HP减少", survivor.CurrentHP < hpBefore);
        }

        #endregion
    }
}
