using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Logic.Battle;
using LostIsland.Core;

namespace LostIsland.Tests
{
    /// <summary>
    /// 战斗核心集成测试
    /// 模拟完整战斗流程：玩家攻击、丧尸攻击、狂暴模式、腐肉升级、胜负判定
    /// 
    /// 测试场景：
    /// 1. 基础战斗流程：玩家 vs 丧尸
    /// 2. 狂暴模式测试
    /// 3. 腐肉升级测试
    /// 4. 胜负判定测试
    /// 5. 多波次战斗模拟
    /// </summary>
    public class BattleIntegrationTests
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

            Debug.Log("========== 战斗集成测试开始 ==========");

            Test_PlayerVsZombie_Basic();
            Test_RageMode_Damage();
            Test_FleshUpgrade_Effect();
            Test_TowerDestruction_Defeat();
            Test_MultipleZombies();
            Test_PlayerDeath_And_Revive();

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
                Debug.Log("🎉 所有集成测试通过！");
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
        /// 创建测试玩家
        /// </summary>
        private static EntityBase CreateTestPlayer(float atk, float hp, float def = 0f)
        {
            EntityBase player = new EntityBase();
            player.Initialize(1, EntityType.Player, "TestPlayer");
            player.SetLevel(10);
            player.Attribute.SetBaseValue(AttrType.ATK, atk);
            player.Attribute.SetBaseValue(AttrType.MaxHP, hp);
            player.Attribute.SetBaseValue(AttrType.DEF, def);
            player.Attribute.SetBaseValue(AttrType.AttackSpeed, 2f); // 2次/秒
            player.Attribute.SetBaseValue(AttrType.CritRate, 0f);
            player.Attribute.SetBaseValue(AttrType.CritDamage, 2f);
            player.Attribute.SetBaseValue(AttrType.MoveSpeed, 5f);
            player.Attribute.SetBaseValue(AttrType.Lifesteal, 0f);
            player.AddComponent<MoveComponent>();
            player.AddComponent<AttackComponent>();
            return player;
        }

        /// <summary>
        /// 创建测试丧尸
        /// </summary>
        private static EntityBase CreateTestZombie(float atk, float hp, float def = 0f)
        {
            EntityBase zombie = new EntityBase();
            zombie.Initialize(100, EntityType.Zombie, "TestZombie");
            zombie.SetLevel(10);
            zombie.Attribute.SetBaseValue(AttrType.ATK, atk);
            zombie.Attribute.SetBaseValue(AttrType.MaxHP, hp);
            zombie.Attribute.SetBaseValue(AttrType.DEF, def);
            zombie.Attribute.SetBaseValue(AttrType.AttackSpeed, 1f);
            zombie.Attribute.SetBaseValue(AttrType.MoveSpeed, 2f);
            zombie.AddComponent<MoveComponent>();
            zombie.AddComponent<AttackComponent>();
            return zombie;
        }

        #endregion

        #region 基础战斗测试

        private static void Test_PlayerVsZombie_Basic()
        {
            Debug.Log("--- 集成测试: 玩家vs丧尸 基础战斗 ---");

            // 创建玩家：攻击100，生命1000
            var player = CreateTestPlayer(100f, 1000f);
            var playerCtrl = new PlayerController(player);

            // 创建丧尸：攻击50，生命300，防御0
            var zombie = CreateTestZombie(50f, 300f, 0f);

            // 玩家攻击丧尸
            var attackComp = player.GetComponent<AttackComponent>();
            attackComp.SetTarget(zombie);
            attackComp.SetAttackRange(10f); // 确保在范围内

            // 第一次攻击
            bool attacked = attackComp.TryAttack();
            Assert("玩家攻击成功", attacked);
            Assert("丧尸HP减少", zombie.Health.CurrentHP < 300f);

            // 预期伤害：100攻击，0防御，无减免 = 100伤害
            AssertApproximately("第一次攻击伤害100", zombie.Health.CurrentHP, 200f);

            // 第二次攻击
            attackComp.Update(0.6f); // 攻速2次/秒，冷却0.5秒，等0.6秒可以再攻击
            attacked = attackComp.TryAttack();
            Assert("冷却后可以再次攻击", attacked);
            AssertApproximately("第二次攻击后HP=100", zombie.Health.CurrentHP, 100f);

            // 第三次攻击击杀
            attackComp.Update(0.6f);
            attackComp.TryAttack();
            Assert("第三次攻击击杀丧尸", zombie.Health.IsDead);
        }

        #endregion

        #region 狂暴模式测试

        private static void Test_RageMode_Damage()
        {
            Debug.Log("--- 集成测试: 狂暴模式伤害翻倍 ---");

            var player = CreateTestPlayer(100f, 1000f);
            var playerCtrl = new PlayerController(player);

            var zombie = CreateTestZombie(0f, 500f, 0f); // 0攻击，只测试伤害

            var attackComp = player.GetComponent<AttackComponent>();
            attackComp.SetTarget(zombie);
            attackComp.SetAttackRange(10f);

            // 普通攻击伤害
            attackComp.TryAttack();
            float normalDamage = 500f - zombie.Health.CurrentHP;
            AssertApproximately("普通攻击伤害100", normalDamage, 100f);

            // 激活狂暴
            playerCtrl.CurrentRage = 100f; // 手动加满
            bool activated = playerCtrl.ActivateRage();
            Assert("狂暴激活成功", activated);
            Assert("狂暴状态正确", playerCtrl.IsRaging);

            // 狂暴攻击：伤害翻倍
            attackComp.Update(1f); // 等冷却
            attackComp.TryAttack();
            float hpAfterRage = zombie.Health.CurrentHP;
            // 狂暴前400HP，狂暴攻击200伤害，应该是200HP
            AssertApproximately("狂暴攻击伤害200(双倍)", 400f - hpAfterRage, 200f, 1f);
        }

        #endregion

        #region 腐肉升级测试

        private static void Test_FleshUpgrade_Effect()
        {
            Debug.Log("--- 集成测试: 腐肉升级效果 ---");

            var player = CreateTestPlayer(100f, 1000f);
            var zombie = CreateTestZombie(0f, 1000f, 0f);

            // 初始攻击
            var attackComp = player.GetComponent<AttackComponent>();
            attackComp.SetTarget(zombie);
            attackComp.SetAttackRange(10f);
            attackComp.TryAttack();
            float damage1 = 1000f - zombie.Health.CurrentHP;
            AssertApproximately("升级前伤害100", damage1, 100f);

            // 创建腐肉升级系统
            var fleshSystem = new FleshUpgradeSystem(player.Attribute);
            int flesh = 100;

            // 升级攻击
            bool upgraded = fleshSystem.TryUpgrade(FleshUpgradeType.ATK, ref flesh);
            Assert("升级成功", upgraded);
            Assert("腐肉扣除正确", flesh == 100 - FleshUpgradeSystem.BASE_COST);
            Assert("等级变为1", fleshSystem.GetLevel(FleshUpgradeType.ATK) == 1);

            // 重置丧尸HP
            zombie.Health.HealFull();
            attackComp.ResetCooldown();

            // 升级后攻击：+10%攻击
            attackComp.TryAttack();
            float damage2 = 1000f - zombie.Health.CurrentHP;
            AssertApproximately("升级后伤害110(+10%)", damage2, 110f);

            // 再升级2级
            flesh = 500;
            fleshSystem.TryUpgrade(FleshUpgradeType.ATK, ref flesh); // Lv2
            fleshSystem.TryUpgrade(FleshUpgradeType.ATK, ref flesh); // Lv3

            Assert("3级攻击升级", fleshSystem.GetLevel(FleshUpgradeType.ATK) == 3);

            // 重置并攻击
            zombie.Health.HealFull();
            attackComp.ResetCooldown();
            attackComp.TryAttack();
            float damage3 = 1000f - zombie.Health.CurrentHP;
            // 3级：+30% = 130伤害
            AssertApproximately("3级升级伤害130(+30%)", damage3, 130f);
        }

        #endregion

        #region 灯塔与胜负测试

        private static void Test_TowerDestruction_Defeat()
        {
            Debug.Log("--- 集成测试: 灯塔被摧毁触发失败 ---");

            // 创建玩家
            var player = CreateTestPlayer(100f, 1000f);

            // 创建灯塔（用EntityBase模拟）
            var tower = new EntityBase();
            tower.Initialize(2, EntityType.Tower, "TestTower");
            tower.Attribute.SetBaseValue(AttrType.MaxHP, 5000f);
            tower.Attribute.SetBaseValue(AttrType.DEF, 0f);
            tower.Health.HealFull();

            var towerCtrl = new TowerController(tower);
            towerCtrl.InitByDay(1);

            Assert("第1天灯塔HP=5000", towerCtrl.MaxHP == 5000f);

            // 创建攻击灯塔的丧尸
            var zombie = CreateTestZombie(500f, 1000f, 0f);

            // 丧尸攻击灯塔10次（500×10=5000伤害）
            var zombieAttack = zombie.GetComponent<AttackComponent>();
            zombieAttack.SetTarget(tower);
            zombieAttack.SetAttackRange(10f);

            for (int i = 0; i < 9; i++)
            {
                zombieAttack.ResetCooldown();
                zombieAttack.TryAttack();
            }

            // 9次攻击后还没死
            Assert("9次攻击后灯塔还在", !towerCtrl.IsDestroyed);

            // 第10次攻击摧毁灯塔
            zombieAttack.ResetCooldown();
            zombieAttack.TryAttack();

            Assert("第10次攻击摧毁灯塔", towerCtrl.IsDestroyed);
        }

        #endregion

        #region 多丧尸测试

        private static void Test_MultipleZombies()
        {
            Debug.Log("--- 集成测试: 多丧尸战斗 ---");

            var player = CreateTestPlayer(200f, 2000f);
            var playerCtrl = new PlayerController(player);

            // 创建3个丧尸
            var zombie1 = CreateTestZombie(30f, 500f);
            var zombie2 = CreateTestZombie(30f, 500f);
            var zombie3 = CreateTestZombie(30f, 500f);

            var attackComp = player.GetComponent<AttackComponent>();
            attackComp.SetAttackRange(10f);

            // 设置位置（都在范围内）
            var playerMove = player.GetComponent<MoveComponent>();
            playerMove.SetPosition(Vector3.zero);

            var z1Move = zombie1.GetComponent<MoveComponent>();
            z1Move.SetPosition(new Vector3(2f, 0f, 0f));
            var z2Move = zombie2.GetComponent<MoveComponent>();
            z2Move.SetPosition(new Vector3(3f, 0f, 0f));
            var z3Move = zombie3.GetComponent<MoveComponent>();
            z3Move.SetPosition(new Vector3(4f, 0f, 0f));

            // 玩家自动攻击
            EntityBase[] enemies = { zombie1, zombie2, zombie3 };

            int kills = 0;
            for (int i = 0; i < 20; i++)
            {
                playerCtrl.AutoAttack(enemies);
                player.Update(0.6f); // 模拟时间流逝

                if (!zombie1.IsAlive && !zombie2.IsAlive && !zombie3.IsAlive)
                {
                    kills = 3;
                    break;
                }
            }

            Assert("3个丧尸都被击杀", kills == 3);
        }

        #endregion

        #region 死亡与复活测试

        private static void Test_PlayerDeath_And_Revive()
        {
            Debug.Log("--- 集成测试: 玩家死亡与复活 ---");

            var player = CreateTestPlayer(100f, 500f);
            var playerCtrl = new PlayerController(player);

            // 受到致命伤害
            player.Health.TakeDamage(600f);

            Assert("玩家死亡", player.Health.IsDead);
            Assert("PlayerController检测到死亡", playerCtrl.IsDead);

            // 复活
            playerCtrl.Revive();

            Assert("复活后存活", !player.Health.IsDead);
            Assert("复活后满血", player.Health.CurrentHP == player.Health.MaxHP);
            Assert("PlayerController检测到存活", !playerCtrl.IsDead);
        }

        #endregion
    }
}
