using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Logic.Wave;
using LostIsland.Logic.Battle;
using LostIsland.Entity;

namespace LostIsland.Tests
{
    /// <summary>
    /// 波次与丧尸系统单元测试
    /// 验证波次管理器、丧尸配置、属性成长、BOSS机制、对象池等
    /// </summary>
    public class WaveSystemTests
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

            Debug.Log("========== 波次与丧尸系统测试开始 ==========");

            // 波次间隔测试
            Test_WaveInterval_Linear();
            Test_WaveInterval_Curved();
            Test_WaveInterval_EdgeCases();

            // 属性成长测试
            Test_HPGrowth_PerWave();
            Test_ATKGrowth_PerWave();
            Test_HPGrowth_PerDay();
            Test_CombinedGrowth();

            // 丧尸数量测试
            Test_ZombieCount_Growth();

            // BOSS波判定
            Test_BossWave_Every20();
            Test_BossWave_Milestone();
            Test_BossWave_NonBoss();

            // 丧尸配置测试
            Test_ZombieConfig_Normal();
            Test_ZombieConfig_Fast();
            Test_ZombieConfig_Heavy();
            Test_ZombieConfig_Boss();

            // 丧尸控制器测试
            Test_ZombieController_Init();
            Test_ZombieStateMachine_SpawnToChase();

            // BOSS阶段测试
            Test_Boss_Phases();
            Test_Boss_Shield();

            // 对象池测试
            Test_ZombiePool_GetReturn();

            // 300波模拟（抽样验证）
            Test_300Waves_Sampling();

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

        private static void AssertApproximately(string testName, float actual, float expected, float tolerance = 0.01f)
        {
            bool equal = Mathf.Abs(actual - expected) <= tolerance;
            Assert(testName, equal, $"expected={expected:F4}, actual={actual:F4}, diff={Mathf.Abs(actual - expected):F6}");
        }

        /// <summary>
        /// 创建测试用波次配置
        /// </summary>
        private static WaveConfigSO CreateTestConfig()
        {
            var config = ScriptableObject.CreateInstance<WaveConfigSO>();
            config.TotalWaves = 300;
            config.MaxWaveInterval = 30f;
            config.MinWaveInterval = 15f;
            config.IntervalCurvePower = 0.7f;
            config.HpGrowthPerWave = 1.02f;
            config.AtkGrowthPerWave = 1.015f;
            config.DefGrowthPerWave = 1.01f;
            config.HpGrowthPerDay = 1.10f;
            config.AtkGrowthPerDay = 1.08f;
            config.DefGrowthPerDay = 1.05f;
            config.BaseZombieCount = 5;
            config.CountGrowthPerWave = 0.5f;
            config.MaxZombiesPerWave = 50;
            return config;
        }

        #endregion

        #region 波次间隔测试

        private static void Test_WaveInterval_Linear()
        {
            Debug.Log("--- 测试: 波次间隔 ---");

            var config = CreateTestConfig();

            // 第1波 = 30秒
            float interval1 = config.GetWaveInterval(1);
            AssertApproximately("第1波间隔30秒", interval1, 30f);

            // 第300波 = 15秒
            float interval300 = config.GetWaveInterval(300);
            AssertApproximately("第300波间隔15秒", interval300, 15f);

            // 中间波在15~30之间
            float interval150 = config.GetWaveInterval(150);
            Assert("第150波在15~30之间", interval150 > 15f && interval150 < 30f);
        }

        private static void Test_WaveInterval_Curved()
        {
            Debug.Log("--- 测试: 波次间隔曲线（上凸） ---");

            var config = CreateTestConfig();

            // 上凸曲线：前段变化慢，后段变化快
            // 第50波应该比线性值更小（间隔更短）
            float curved50 = config.GetWaveInterval(50);

            // 线性值：30 - (30-15) * 49/299 ≈ 27.54
            float linear50 = Mathf.Lerp(30f, 15f, 49f / 299f);

            Assert("上凸曲线：第50波间隔 < 线性值", curved50 < linear50);

            // 第250波变化更快
            float curved250 = config.GetWaveInterval(250);
            float linear250 = Mathf.Lerp(30f, 15f, 249f / 299f);
            Assert("上凸曲线：第250波间隔 < 线性值", curved250 < linear250);
        }

        private static void Test_WaveInterval_EdgeCases()
        {
            Debug.Log("--- 测试: 波次间隔边界 ---");

            var config = CreateTestConfig();

            // 第0波（无效输入）
            float interval0 = config.GetWaveInterval(0);
            Assert("第0波使用第1波间隔", interval0 == 30f);

            // 第301波（超过总波数）
            float interval301 = config.GetWaveInterval(301);
            AssertApproximately("超过总波数使用最小间隔", interval301, 15f);
        }

        #endregion

        #region 属性成长测试

        private static void Test_HPGrowth_PerWave()
        {
            Debug.Log("--- 测试: HP波次成长 ---");

            var config = CreateTestConfig();

            // 第1波 = 1.0x
            float mult1 = config.GetHpMultiplier(1, 1);
            AssertApproximately("第1波HP倍率=1.0", mult1, 1f);

            // 第2波 = 1.02^1 = 1.02
            float mult2 = config.GetHpMultiplier(2, 1);
            AssertApproximately("第2波HP倍率=1.02", mult2, 1.02f);

            // 第10波 = 1.02^9 ≈ 1.195
            float mult10 = config.GetHpMultiplier(10, 1);
            float expected10 = Mathf.Pow(1.02f, 9f);
            AssertApproximately("第10波HP倍率≈1.195", mult10, expected10);

            // 第100波 = 1.02^99 ≈ 7.24
            float mult100 = config.GetHpMultiplier(100, 1);
            float expected100 = Mathf.Pow(1.02f, 99f);
            AssertApproximately("第100波HP倍率≈7.24", mult100, expected100);
        }

        private static void Test_ATKGrowth_PerWave()
        {
            Debug.Log("--- 测试: ATK波次成长 ---");

            var config = CreateTestConfig();

            // 第1波 = 1.0x
            float mult1 = config.GetAtkMultiplier(1, 1);
            AssertApproximately("第1波ATK倍率=1.0", mult1, 1f);

            // 第10波 = 1.015^9
            float mult10 = config.GetAtkMultiplier(10, 1);
            float expected10 = Mathf.Pow(1.015f, 9f);
            AssertApproximately("第10波ATK倍率", mult10, expected10);
        }

        private static void Test_HPGrowth_PerDay()
        {
            Debug.Log("--- 测试: HP天数成长 ---");

            var config = CreateTestConfig();

            // 第1天 = 1.0x
            float mult1 = config.GetHpMultiplier(1, 1);
            AssertApproximately("第1天HP倍率=1.0", mult1, 1f);

            // 第2天 = 1.10^1 = 1.10
            float mult2 = config.GetHpMultiplier(1, 2);
            AssertApproximately("第2天HP倍率=1.10", mult2, 1.10f);

            // 第10天 = 1.10^9 ≈ 2.358
            float mult10 = config.GetHpMultiplier(1, 10);
            float expected10 = Mathf.Pow(1.10f, 9f);
            AssertApproximately("第10天HP倍率≈2.36", mult10, expected10);
        }

        private static void Test_CombinedGrowth()
        {
            Debug.Log("--- 测试: 波次+天数 联合成长 ---");

            var config = CreateTestConfig();

            // 第5天第50波
            float mult = config.GetHpMultiplier(50, 5);
            float waveMult = Mathf.Pow(1.02f, 49f);
            float dayMult = Mathf.Pow(1.10f, 4f);
            float expected = waveMult * dayMult;
            AssertApproximately("联合成长 = 波次×天数", mult, expected);

            // 联合成长远大于单一成长
            Assert("联合成长 > 仅波次成长", mult > waveMult);
            Assert("联合成长 > 仅天数成长", mult > dayMult);
        }

        #endregion

        #region 丧尸数量测试

        private static void Test_ZombieCount_Growth()
        {
            Debug.Log("--- 测试: 丧尸数量成长 ---");

            var config = CreateTestConfig();

            // 第1波 = 5只
            int count1 = config.GetZombieCount(1);
            Assert("第1波=5只", count1 == 5);

            // 第11波 = 5 + 10×0.5 = 10只
            int count11 = config.GetZombieCount(11);
            Assert("第11波=10只", count11 == 10);

            // 第100波 = 5 + 99×0.5 = 54.5 → 50（上限）
            int count100 = config.GetZombieCount(100);
            Assert("第100波达到上限50", count100 == 50);

            // 第300波 = 50（上限）
            int count300 = config.GetZombieCount(300);
            Assert("第300波仍为上限50", count300 == 50);
        }

        #endregion

        #region BOSS波判定测试

        private static void Test_BossWave_Every20()
        {
            Debug.Log("--- 测试: BOSS波每20波 ---");

            var config = CreateTestConfig();

            // 第20波是BOSS波
            bool isBoss20 = config.IsBossWave(20, out BossTier tier20);
            Assert("第20波是BOSS波", isBoss20);

            // 第40波是BOSS波
            bool isBoss40 = config.IsBossWave(40, out _);
            Assert("第40波是BOSS波", isBoss40);

            // 第280波是BOSS波
            bool isBoss280 = config.IsBossWave(280, out _);
            Assert("第280波是BOSS波", isBoss280);

            // 第300波是BOSS波
            bool isBoss300 = config.IsBossWave(300, out _);
            Assert("第300波是BOSS波", isBoss300);
        }

        private static void Test_BossWave_Milestone()
        {
            Debug.Log("--- 测试: 里程碑BOSS ---");

            var config = CreateTestConfig();

            // 第100波：领主级
            bool isBoss100 = config.IsBossWave(100, out BossTier tier100);
            Assert("第100波是BOSS波", isBoss100);
            Assert("第100波是领主级", tier100 == BossTier.Lord);

            // 第200波：领主级
            bool isBoss200 = config.IsBossWave(200, out BossTier tier200);
            Assert("第200波是BOSS波", isBoss200);
            Assert("第200波是领主级", tier200 == BossTier.Lord);

            // 第300波：领主级
            bool isBoss300 = config.IsBossWave(300, out BossTier tier300);
            Assert("第300波是BOSS波", isBoss300);
            Assert("第300波是领主级", tier300 == BossTier.Lord);
        }

        private static void Test_BossWave_NonBoss()
        {
            Debug.Log("--- 测试: 非BOSS波 ---");

            var config = CreateTestConfig();

            // 普通波次不是BOSS波
            for (int i = 1; i <= 300; i++)
            {
                if (i % 20 != 0)
                {
                    bool isBoss = config.IsBossWave(i, out _);
                    if (isBoss)
                    {
                        Assert($"第{i}波不应该是BOSS波", false);
                        return;
                    }
                }
            }
            Assert("所有非20倍数波都不是BOSS波", true);
        }

        #endregion

        #region 丧尸配置测试

        private static void Test_ZombieConfig_Normal()
        {
            Debug.Log("--- 测试: 普通丧尸配置 ---");

            var config = ZombieConfigFactory.GetConfig(ZombieType.Normal);
            Assert("配置不为空", config != null);
            Assert("类型正确", config.ZombieType == ZombieType.Normal);
            Assert("稀有度普通", config.Rarity == ZombieRarity.Normal);
            Assert("HP=100", config.BaseHP == 100f);
            Assert("ATK=10", config.BaseATK == 10f);
        }

        private static void Test_ZombieConfig_Fast()
        {
            Debug.Log("--- 测试: 快速丧尸配置 ---");

            var config = ZombieConfigFactory.GetConfig(ZombieType.Fast);
            Assert("类型正确", config.ZombieType == ZombieType.Fast);
            Assert("速度倍率1.8", config.SpeedMultiplier == 1.8f);
            Assert("HP倍率0.6", config.HPMultiplier == 0.6f);
            Assert("速度 > 普通丧尸", config.GetMoveSpeed() > ZombieConfigFactory.GetConfig(ZombieType.Normal).GetMoveSpeed());
        }

        private static void Test_ZombieConfig_Heavy()
        {
            Debug.Log("--- 测试: 重甲丧尸配置 ---");

            var config = ZombieConfigFactory.GetConfig(ZombieType.Heavy);
            Assert("类型正确", config.ZombieType == ZombieType.Heavy);
            Assert("HP倍率2.5", config.HPMultiplier == 2.5f);
            Assert("速度倍率0.7", config.SpeedMultiplier == 0.7f);
            Assert("有防御", config.BaseDEF > 0);
        }

        private static void Test_ZombieConfig_Boss()
        {
            Debug.Log("--- 测试: BOSS配置 ---");

            var bossConfig = ZombieConfigFactory.GetConfig(ZombieType.Boss_RottenFist);
            Assert("BOSS稀有度正确", bossConfig.Rarity == ZombieRarity.Boss);
            Assert("BOSS HP倍率高", bossConfig.HPMultiplier > 10f);
            Assert("BOSS体型大", bossConfig.ScaleMultiplier > 2f);
        }

        #endregion

        #region 丧尸控制器测试

        private static void Test_ZombieController_Init()
        {
            Debug.Log("--- 测试: 丧尸控制器初始化 ---");

            var config = ZombieConfigFactory.GetConfig(ZombieType.Normal);
            EntityBase entity = new EntityBase();
            entity.Initialize(100, EntityType.Zombie, "TestZombie");

            ZombieController zombie = new ZombieController(entity, config);
            zombie.InitAttributes(1, 1, 1f, 1f, 1f);

            Assert("HP正确", entity.Health.MaxHP == config.BaseHP);
            Assert("ATK正确", entity.Attribute.FinalATK == config.BaseATK);
            Assert("初始状态None", zombie.CurrentState == ZombieState.None);
            Assert("未死亡", !zombie.IsDead);
        }

        private static void Test_ZombieStateMachine_SpawnToChase()
        {
            Debug.Log("--- 测试: 丧尸状态机 Spawn→Chase ---");

            var config = ZombieConfigFactory.GetConfig(ZombieType.Normal);
            EntityBase entity = new EntityBase();
            entity.Initialize(100, EntityType.Zombie, "TestZombie");

            ZombieController zombie = new ZombieController(entity, config);
            zombie.InitAttributes(1, 1, 1f, 1f, 1f);
            zombie.TowerPosition = Vector3.zero;

            // 出生
            zombie.Spawn(Vector3.right * 10f);
            Assert("出生状态", zombie.CurrentState == ZombieState.Spawn);

            // 模拟时间流逝（出生动画0.5秒）
            zombie.Update(0.6f);
            Assert("出生后进入追击", zombie.CurrentState == ZombieState.Chase);
        }

        #endregion

        #region BOSS阶段测试

        private static void Test_Boss_Phases()
        {
            Debug.Log("--- 测试: BOSS阶段转换 ---");

            var config = ZombieConfigFactory.GetConfig(ZombieType.Boss_RottenFist);
            EntityBase entity = new EntityBase();
            entity.Initialize(200, EntityType.Boss, "TestBoss");

            BossController boss = new BossController(entity, config, "TestBoss", BossTier.Normal);
            boss.InitAttributes(20, 1, 1f, 1f, 1f);

            // 设置阶段配置
            var phases = new List<BossPhase>
            {
                new BossPhase { PhaseName = "P2", HpThreshold = 0.7f, AtkMultiplier = 1.2f },
                new BossPhase { PhaseName = "P3", HpThreshold = 0.4f, AtkMultiplier = 1.5f },
                new BossPhase { PhaseName = "P4", HpThreshold = 0.2f, AtkMultiplier = 2.0f },
            };
            boss.InitPhases(phases);

            // 初始阶段0
            Assert("初始阶段0", boss.CurrentPhase == 0);

            // 受到伤害到60%HP → 进入阶段1
            float damage = entity.Health.MaxHP * 0.4f; // 掉40%，剩60%
            entity.Health.TakeDamage(damage);
            Assert("60%HP进入阶段1", boss.CurrentPhase == 1);

            // 继续受伤到30%HP → 进入阶段2
            damage = entity.Health.MaxHP * 0.3f;
            entity.Health.TakeDamage(damage);
            Assert("30%HP进入阶段2", boss.CurrentPhase == 2);

            // 继续受伤到10%HP → 进入阶段3（狂暴）
            damage = entity.Health.MaxHP * 0.2f;
            entity.Health.TakeDamage(damage);
            Assert("10%HP进入阶段3（狂暴）", boss.CurrentPhase == 3);
            Assert("狂暴状态激活", boss.IsEnraged);
        }

        private static void Test_Boss_Shield()
        {
            Debug.Log("--- 测试: BOSS护盾 ---");

            var config = ZombieConfigFactory.GetConfig(ZombieType.Boss_IronButcher);
            EntityBase entity = new EntityBase();
            entity.Initialize(200, EntityType.Boss, "TestBoss");

            BossController boss = new BossController(entity, config, "TestBoss", BossTier.Normal);
            boss.InitAttributes(20, 1, 1f, 1f, 1f);

            // 检查初始护盾
            Assert("有护盾", boss.HasShield);
            Assert("护盾HP>0", boss.ShieldHP > 0f);

            // 护盾吸收伤害
            float shieldBefore = boss.ShieldHP;
            float hpBefore = entity.Health.CurrentHP;

            float damage = 100f;
            boss.TakeDamage(DamageResult.Create(damage));

            // HP不变，护盾减少
            Assert("HP不变（护盾吸收）", entity.Health.CurrentHP == hpBefore);
            Assert("护盾减少", boss.ShieldHP < shieldBefore);
        }

        #endregion

        #region 对象池测试

        private static void Test_ZombiePool_GetReturn()
        {
            Debug.Log("--- 测试: 丧尸对象池 ---");

            // 注意：完整的对象池测试需要MonoSingleton环境
            // 这里只测试工厂和配置
            var config1 = ZombieConfigFactory.GetConfig(ZombieType.Normal);
            var config2 = ZombieConfigFactory.GetConfig(ZombieType.Fast);
            var config3 = ZombieConfigFactory.GetConfig(ZombieType.Heavy);

            Assert("普通丧尸配置正确", config1 != null && config1.ZombieType == ZombieType.Normal);
            Assert("快速丧尸配置正确", config2 != null && config2.ZombieType == ZombieType.Fast);
            Assert("重甲丧尸配置正确", config3 != null && config3.ZombieType == ZombieType.Heavy);

            // 测试所有类型都有配置
            foreach (ZombieType type in Enum.GetValues(typeof(ZombieType)))
            {
                var cfg = ZombieConfigFactory.GetConfig(type);
                Assert($"{type} 配置存在", cfg != null);
            }
        }

        #endregion

        #region 300波模拟测试

        private static void Test_300Waves_Sampling()
        {
            Debug.Log("--- 测试: 300波抽样验证 ---");

            var config = CreateTestConfig();

            // 关键波次抽样
            int[] sampleWaves = { 1, 10, 20, 50, 100, 150, 200, 250, 300 };

            Debug.Log("波次 | 间隔(s) | HP倍率 | ATK倍率 | 数量 | BOSS?");
            Debug.Log("-----|---------|--------|---------|------|------");

            foreach (int wave in sampleWaves)
            {
                float interval = config.GetWaveInterval(wave);
                float hpMult = config.GetHpMultiplier(wave, 1);
                float atkMult = config.GetAtkMultiplier(wave, 1);
                int count = config.GetZombieCount(wave);
                bool isBoss = config.IsBossWave(wave, out _);

                Debug.Log($"{wave,4} | {interval,7:F1} | {hpMult,6:F2}x | {atkMult,7:F2}x | {count,4} | {(isBoss ? "BOSS" : "")}");

                // 基本验证
                Assert($"第{wave}波间隔在范围内", interval >= 15f && interval <= 30f);
                Assert($"第{wave}波HP倍率>=1", hpMult >= 1f);
                Assert($"第{wave}波数量>=5", count >= 5);
            }

            // 第300波验证
            float hp300 = config.GetHpMultiplier(300, 1);
            Assert("第300波HP倍率很大", hp300 > 10f); // 1.02^299 ≈ 380
            Assert("第300波是BOSS波", config.IsBossWave(300, out _));

            Debug.Log($"第300波HP倍率: {hp300:F2}x (1.02^299)");
            Debug.Log($"第300波间隔: {config.GetWaveInterval(300):F1}秒");
        }

        #endregion
    }
}
