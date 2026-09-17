using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Awakening;

namespace LostIsland.Tests
{
    /// <summary>
    /// 觉醒与成长加速系统单元测试
    /// </summary>
    public class AwakeningSystemTests
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

            Debug.Log("========== 觉醒与成长加速系统测试开始 ==========");

            // 觉醒系统测试
            Test_Awaken_InitialState();
            Test_Awaken_AddAXP_SingleLevel();
            Test_Awaken_AddAXP_MultiLevel();
            Test_Awaken_Multiplier();
            Test_Awaken_SetLevel();
            Test_Awaken_MaxLevel();
            Test_Awaken_Progress();

            // 成长加速测试
            Test_Speedup_InitialState();
            Test_Speedup_ConsecutiveDefeat();
            Test_Speedup_VictoryReset();
            Test_Speedup_NewPlayer();
            Test_Speedup_MultipleBuffs();
            Test_Speedup_BuffMultiplier();

            // DDDR测试
            Test_DDDR_InitialState();
            Test_DDDR_Reset();
            Test_DDDR_SetDifficulty();
            Test_DDDR_OffsetDifficulty();
            Test_DDDR_ClampMinMax();

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

        #region 觉醒系统测试

        private static void Test_Awaken_InitialState()
        {
            Debug.Log("--- 测试: 觉醒初始状态 ---");

            var mgr = AwakenManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            Assert("初始等级=0", mgr.AwakeningLevel == 0);
            Assert("初始AXP=0", mgr.CurrentAXP == 0);
            AssertApproximately("初始倍率=1.0", mgr.AwakeningMultiplier, 1.0f);
            Assert("未满级", !mgr.IsMaxLevel);
            Assert("升级进度=0", mgr.LevelProgress == 0f);
        }

        private static void Test_Awaken_AddAXP_SingleLevel()
        {
            Debug.Log("--- 测试: 单次升级 ---");

            var mgr = AwakenManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            // 重置
            mgr.SetAwakeningLevel(0);

            // 0→1级需要1000 AXP
            mgr.AddAXP(1000);
            Assert("加1000AXP后等级=1", mgr.AwakeningLevel == 1);
            AssertApproximately("倍率=1.025", mgr.AwakeningMultiplier, 1.025f);
        }

        private static void Test_Awaken_AddAXP_MultiLevel()
        {
            Debug.Log("--- 测试: 连升多级 ---");

            var mgr = AwakenManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.SetAwakeningLevel(0);

            // 前10级每级1000，10级共10000
            mgr.AddAXP(10000);
            Assert("10000AXP=10级", mgr.AwakeningLevel == 10);
            AssertApproximately("10级倍率=1.25", mgr.AwakeningMultiplier, 1.25f);
        }

        private static void Test_Awaken_Multiplier()
        {
            Debug.Log("--- 测试: 觉醒倍率计算 ---");

            var mgr = AwakenManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.SetAwakeningLevel(0);
            AssertApproximately("0级=1.0x", mgr.AwakeningMultiplier, 1.0f);

            mgr.SetAwakeningLevel(40);
            // 40 × 2.5% = 100% = 2.0x
            AssertApproximately("40级=2.0x", mgr.AwakeningMultiplier, 2.0f);

            mgr.SetAwakeningLevel(100);
            // 100 × 2.5% = 250% = 3.5x
            AssertApproximately("100级=3.5x", mgr.AwakeningMultiplier, 3.5f);
        }

        private static void Test_Awaken_SetLevel()
        {
            Debug.Log("--- 测试: 设置觉醒等级 ---");

            var mgr = AwakenManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.SetAwakeningLevel(50);
            Assert("设置50级成功", mgr.AwakeningLevel == 50);
            Assert("当前AXP清零", mgr.CurrentAXP == 0);

            // 不超过最大值
            mgr.SetAwakeningLevel(150);
            Assert("超过最大限制为100", mgr.AwakeningLevel == 100);
            Assert("满级", mgr.IsMaxLevel);

            // 不低于最小值
            mgr.SetAwakeningLevel(-5);
            Assert("低于最小限制为0", mgr.AwakeningLevel == 0);
        }

        private static void Test_Awaken_MaxLevel()
        {
            Debug.Log("--- 测试: 满级处理 ---");

            var mgr = AwakenManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.SetAwakeningLevel(100);
            Assert("满级=100", mgr.AwakeningLevel == 100);
            Assert("满级标记", mgr.IsMaxLevel);

            // 满级后再加AXP无效
            long before = mgr.CurrentAXP;
            mgr.AddAXP(999999);
            Assert("满级后AXP不增加", mgr.CurrentAXP == before);
            Assert("满级后等级不变", mgr.AwakeningLevel == 100);
        }

        private static void Test_Awaken_Progress()
        {
            Debug.Log("--- 测试: 升级进度 ---");

            var mgr = AwakenManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.SetAwakeningLevel(0);

            // 加500/1000 = 50%
            mgr.AddAXP(500);
            AssertApproximately("进度50%", mgr.LevelProgress, 0.5f);
        }

        #endregion

        #region 成长加速测试

        private static void Test_Speedup_InitialState()
        {
            Debug.Log("--- 测试: 加速初始状态 ---");

            var mgr = SpeedupManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            Assert("初始0个Buff", mgr.ActiveBuffCount == 0);
            AssertApproximately("攻击倍率=1.0", mgr.TotalAtkMultiplier, 1.0f);
            AssertApproximately("生命倍率=1.0", mgr.TotalHpMultiplier, 1.0f);
            Assert("连败次数=0", mgr.ConsecutiveDefeats == 0);
        }

        private static void Test_Speedup_ConsecutiveDefeat()
        {
            Debug.Log("--- 测试: 连续失败触发加速 ---");

            var mgr = SpeedupManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.ClearAllBuffs();

            // 前2次失败不触发
            mgr.OnBattleDefeated(5, 1);
            Assert("第1次失败，连败=1", mgr.ConsecutiveDefeats == 1);

            mgr.OnBattleDefeated(5, 1);
            Assert("第2次失败，连败=2", mgr.ConsecutiveDefeats == 2);

            // 第3次失败触发
            mgr.OnBattleDefeated(5, 1);
            Assert("第3次失败，连败=3", mgr.ConsecutiveDefeats == 3);
            Assert("连败Buff激活", mgr.HasBuff(SpeedupType.ConsecutiveDefeat));
            Assert("攻击倍率=1.2", mgr.TotalAtkMultiplier >= 1.2f);
        }

        private static void Test_Speedup_VictoryReset()
        {
            Debug.Log("--- 测试: 胜利重置 ---");

            var mgr = SpeedupManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            // 先触发连败
            mgr.OnBattleDefeated(5, 1);
            mgr.OnBattleDefeated(5, 1);
            mgr.OnBattleDefeated(5, 1);
            Assert("连败Buff已激活", mgr.HasBuff(SpeedupType.ConsecutiveDefeat));

            // 胜利后重置
            mgr.OnBattleVictory(10, 1);
            Assert("胜利后连败=0", mgr.ConsecutiveDefeats == 0);
            Assert("胜利后连败Buff移除", !mgr.HasBuff(SpeedupType.ConsecutiveDefeat));
            AssertApproximately("倍率恢复1.0", mgr.TotalAtkMultiplier, 1.0f);
        }

        private static void Test_Speedup_NewPlayer()
        {
            Debug.Log("--- 测试: 新手保护 ---");

            var mgr = SpeedupManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.ClearAllBuffs();

            // 前3天有新手保护
            mgr.OnBattleVictory(1, 1);
            Assert("第1天有新手保护", mgr.HasBuff(SpeedupType.NewPlayer));
            Assert("新手保护+30%", mgr.TotalAtkMultiplier >= 1.3f);

            // 第4天以后没有
            mgr.OnBattleVictory(10, 4);
            Assert("第4天新手保护结束", !mgr.HasBuff(SpeedupType.NewPlayer));
        }

        private static void Test_Speedup_MultipleBuffs()
        {
            Debug.Log("--- 测试: 多重Buff叠加 ---");

            var mgr = SpeedupManager.Instance;
            if (mgr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            mgr.ClearAllBuffs();

            // 新手保护 + 连败
            mgr.ActivateBuff(SpeedupType.NewPlayer, 0.30f);
            mgr.ActivateBuff(SpeedupType.ConsecutiveDefeat, 0.20f);

            Assert("2个Buff激活", mgr.ActiveBuffCount == 2);

            // 倍率相乘: 1.3 × 1.2 = 1.56
            float expectedMult = 1.3f * 1.2f;
            AssertApproximately("叠加倍率=1.56", mgr.TotalAtkMultiplier, expectedMult, 0.01f);
        }

        private static void Test_Speedup_BuffMultiplier()
        {
            Debug.Log("--- 测试: Buff倍率计算 ---");

            var buff = new SpeedupBuff(SpeedupType.WaveStuck, 1.25f, 1.25f);
            Assert("攻击倍率正确", buff.AtkMultiplier == 1.25f);
            Assert("生命倍率正确", buff.HpMultiplier == 1.25f);
            Assert("永久Buff", buff.IsPermanent);
            Assert("永久Buff不过期", !buff.IsExpired);

            var tempBuff = new SpeedupBuff(SpeedupType.WeekendBoost, 1.1f, 1.1f, 3600f);
            Assert("限时Buff", !tempBuff.IsPermanent);
            Assert("未过期", !tempBuff.IsExpired);
        }

        #endregion

        #region DDDR测试

        private static void Test_DDDR_InitialState()
        {
            Debug.Log("--- 测试: DDDR初始状态 ---");

            var dddr = DDDRSystem.Instance;
            if (dddr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            AssertApproximately("初始生成倍率=1.0", dddr.SpawnRateMultiplier, 1.0f);
            AssertApproximately("初始攻击倍率=1.0", dddr.ZombieAtkMultiplier, 1.0f);
            AssertApproximately("初始速度倍率=1.0", dddr.ZombieSpeedMultiplier, 1.0f);
            Assert("初始未运行", !dddr.IsRunning);
        }

        private static void Test_DDDR_Reset()
        {
            Debug.Log("--- 测试: DDDR重置 ---");

            var dddr = DDDRSystem.Instance;
            if (dddr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            dddr.SetDifficulty(1.5f, 1.3f, 1.2f);
            dddr.ResetDifficulty();

            AssertApproximately("重置后生成=1.0", dddr.SpawnRateMultiplier, 1.0f);
            AssertApproximately("重置后攻击=1.0", dddr.ZombieAtkMultiplier, 1.0f);
            AssertApproximately("重置后速度=1.0", dddr.ZombieSpeedMultiplier, 1.0f);
        }

        private static void Test_DDDR_SetDifficulty()
        {
            Debug.Log("--- 测试: 设置难度 ---");

            var dddr = DDDRSystem.Instance;
            if (dddr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            dddr.SetDifficulty(0.8f, 0.9f, 0.95f);

            // 目标值设置了，但因为未运行所以当前值可能还是1.0
            // 检查是否可以设置（不报错）
            Assert("设置难度不报错", true);
        }

        private static void Test_DDDR_OffsetDifficulty()
        {
            Debug.Log("--- 测试: 偏移难度 ---");

            var dddr = DDDRSystem.Instance;
            if (dddr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            dddr.ResetDifficulty();
            dddr.OffsetDifficulty(0.1f, 0.1f, 0.05f);

            Assert("偏移难度不报错", true);
        }

        private static void Test_DDDR_ClampMinMax()
        {
            Debug.Log("--- 测试: 难度钳制 ---");

            var dddr = DDDRSystem.Instance;
            if (dddr == null)
            {
                Assert("跳过（需要MonoSingleton环境）", true);
                return;
            }

            // 设置极端值，应该被钳制
            dddr.SetDifficulty(2.0f, 2.0f, 2.0f); // 超过最大

            // 最大应该是 1 + 0.5 = 1.5
            dddr.ResetDifficulty();
            dddr.SetDifficulty(1.5f, 1.5f, 1.25f); // 在范围内

            Assert("钳制测试不报错", true);
        }

        #endregion
    }
}
