using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Tests
{
    /// <summary>
    /// 属性系统单元测试
    /// 验证 AttributeComponent、HealthComponent、EntityBase 的正确性
    /// 
    /// 测试用例：
    /// 1. 基础属性设置与读取
    /// 2. 固定值加成
    /// 3. 单层百分比加成
    /// 4. 多层百分比叠加（乘法关系）
    /// 5. 固定值与百分比混合加成的先后顺序
    /// 6. 属性上限裁剪（暴击率、攻速、伤害减免）
    /// 7. 脏标记缓存机制
    /// 8. 加成移除后属性正确回退
    /// 9. 属性变化事件触发
    /// 10. HealthComponent 受伤/治疗/死亡
    /// 11. Entity 组件系统
    /// </summary>
    public class AttributeSystemTests
    {
        private static int _totalTests = 0;
        private static int _passedTests = 0;
        private static int _failedTests = 0;
        private static List<string> _failedMessages = new List<string>();

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;
            _failedMessages.Clear();

            Debug.Log("========== 属性系统单元测试开始 ==========");

            // 属性系统测试
            Test_BaseValue();
            Test_FlatAdd();
            Test_SinglePctLayer();
            Test_MultiPctLayers();
            Test_FlatAndPctOrder();
            Test_AttributeCaps();
            Test_DirtyFlagCaching();
            Test_RemoveBonus();
            Test_AttributeChangedEvent();
            Test_MultipleSourcesSameLayer();

            // HealthComponent 测试
            Test_Health_TakeDamage();
            Test_Health_Heal();
            Test_Health_Death();
            Test_Health_Invincible();
            Test_Health_HPPctEvent();
            Test_Health_MaxHPChange();

            // Entity 测试
            Test_Entity_Basic();
            Test_Entity_AddGetComponent();
            Test_Entity_RemoveComponent();
            Test_Entity_MoveComponent();
            Test_Entity_AttackComponent();

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

        #region 属性系统测试

        private static void Test_BaseValue()
        {
            Debug.Log("--- 测试: 基础属性 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.MaxHP, 1000f);
            attr.SetBaseValue(AttrType.ATK, 100f);
            attr.SetBaseValue(AttrType.DEF, 50f);

            AssertApproximately("基础HP读取", attr.GetValue(AttrType.MaxHP), 1000f);
            AssertApproximately("基础ATK读取", attr.GetValue(AttrType.ATK), 100f);
            AssertApproximately("基础DEF读取", attr.GetValue(AttrType.DEF), 50f);
            AssertApproximately("未设置属性为0", attr.GetValue(AttrType.MoveSpeed), 0f);
        }

        private static void Test_FlatAdd()
        {
            Debug.Log("--- 测试: 固定值加成 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            // 添加固定值加成
            attr.AddFlat("weapon", AttrType.ATK, 50f);
            AssertApproximately("固定值加成后ATK", attr.GetValue(AttrType.ATK), 150f);

            // 再添加一个
            attr.AddFlat("ring", AttrType.ATK, 30f);
            AssertApproximately("两个固定值加成", attr.GetValue(AttrType.ATK), 180f);

            // 移除一个
            attr.RemoveFlat("weapon", AttrType.ATK);
            AssertApproximately("移除一个固定值", attr.GetValue(AttrType.ATK), 130f);
        }

        private static void Test_SinglePctLayer()
        {
            Debug.Log("--- 测试: 单层百分比加成 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            // 添加10%攻击加成
            attr.AddPct("skill_1", AttrType.ATK, 0.1f, AttrLayer.Pct_SkillCard);
            AssertApproximately("单层10%加成", attr.GetValue(AttrType.ATK), 110f);

            // 改为20%
            attr.AddPct("skill_1", AttrType.ATK, 0.2f, AttrLayer.Pct_SkillCard);
            AssertApproximately("修改为20%加成", attr.GetValue(AttrType.ATK), 120f);

            // 移除
            attr.RemovePct("skill_1", AttrType.ATK);
            AssertApproximately("移除百分比加成", attr.GetValue(AttrType.ATK), 100f);
        }

        private static void Test_MultiPctLayers()
        {
            Debug.Log("--- 测试: 多层百分比叠加（乘法关系） ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            // L2: 技能卡 +20%
            attr.AddPct("skill_1", AttrType.ATK, 0.2f, AttrLayer.Pct_SkillCard);
            // L3: 符文 +30%
            attr.AddPct("rune_1", AttrType.ATK, 0.3f, AttrLayer.Pct_Rune);
            // L4: 觉醒 +10%
            attr.AddPct("awaken_1", AttrType.ATK, 0.1f, AttrLayer.Pct_Awaken);

            // 乘法关系：100 * 1.2 * 1.3 * 1.1 = 171.6
            float expected = 100f * 1.2f * 1.3f * 1.1f;
            AssertApproximately("三层乘法叠加", attr.GetValue(AttrType.ATK), expected);
        }

        private static void Test_FlatAndPctOrder()
        {
            Debug.Log("--- 测试: 固定值与百分比的先后顺序 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            // 固定值 +50
            attr.AddFlat("weapon", AttrType.ATK, 50f);
            // 百分比 +50%
            attr.AddPct("skill_1", AttrType.ATK, 0.5f, AttrLayer.Pct_SkillCard);

            // 正确顺序：(基础 + 固定) × 百分比 = (100 + 50) × 1.5 = 225
            float expected = (100f + 50f) * 1.5f;
            AssertApproximately("固定值在百分比之前", attr.GetValue(AttrType.ATK), expected);

            // 验证不是 100 × 1.5 + 50 = 200
            float wrongOrder = 100f * 1.5f + 50f;
            Assert("顺序验证：不是错误的200", Mathf.Abs(attr.GetValue(AttrType.ATK) - wrongOrder) > 1f);
        }

        private static void Test_AttributeCaps()
        {
            Debug.Log("--- 测试: 属性上限裁剪 ---");

            AttributeComponent attr = new AttributeComponent();

            // 暴击率上限 75%
            attr.SetBaseValue(AttrType.CritRate, 0.5f);
            attr.AddPct("test_crit", AttrType.CritRate, 1.0f, AttrLayer.Pct_Buff); // 100%加成 → 100%
            AssertApproximately("暴击率上限75%", attr.GetValue(AttrType.CritRate), AttributeComponent.CRIT_RATE_CAP);

            // 攻速上限 3.0
            attr.SetBaseValue(AttrType.AttackSpeed, 2f);
            attr.AddPct("test_as", AttrType.AttackSpeed, 1.0f, AttrLayer.Pct_Buff); // 100% → 4.0 → 裁剪到3.0
            AssertApproximately("攻速上限3.0", attr.GetValue(AttrType.AttackSpeed), AttributeComponent.ATTACK_SPEED_CAP);

            // 伤害减免上限
            attr.SetBaseValue(AttrType.DamageReduction, 0.5f);
            attr.AddPct("test_dr", AttrType.DamageReduction, 1.0f, AttrLayer.Pct_Buff); // 100% → 100% → 裁剪
            AssertApproximately("伤害减免上限", attr.GetValue(AttrType.DamageReduction), AttributeComponent.DAMAGE_REDUCTION_CAP);
        }

        private static void Test_DirtyFlagCaching()
        {
            Debug.Log("--- 测试: 脏标记缓存机制 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            // 初始状态是脏的
            Assert("初始状态是脏的", attr.IsDirty(AttrType.ATK));

            // 读取一次后变干净
            float val = attr.GetValue(AttrType.ATK);
            Assert("读取后变干净", !attr.IsDirty(AttrType.ATK));

            // 再次读取不重新计算
            float val2 = attr.GetValue(AttrType.ATK);
            Assert("缓存值一致", Mathf.Abs(val - val2) < 0.001f);
            Assert("仍然干净", !attr.IsDirty(AttrType.ATK));

            // 修改后变脏
            attr.AddPct("test", AttrType.ATK, 0.1f, AttrLayer.Pct_SkillCard);
            Assert("修改后变脏", attr.IsDirty(AttrType.ATK));

            // 其他属性不受影响
            Assert("其他属性不脏", !attr.IsDirty(AttrType.DEF));
        }

        private static void Test_RemoveBonus()
        {
            Debug.Log("--- 测试: 加成移除后正确回退 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            // 添加多个加成
            attr.AddPct("skill_1", AttrType.ATK, 0.2f, AttrLayer.Pct_SkillCard);
            attr.AddPct("rune_1", AttrType.ATK, 0.3f, AttrLayer.Pct_Rune);
            attr.AddFlat("weapon", AttrType.ATK, 50f);

            float valueBefore = attr.GetValue(AttrType.ATK);

            // 移除符文加成
            attr.RemovePctAll("rune_1");
            float valueAfterRemoveRune = attr.GetValue(AttrType.ATK);

            // 验证值变小了
            Assert("移除符文后值变小", valueAfterRemoveRune < valueBefore);

            // 移除所有
            attr.ResetAll();
            AssertApproximately("重置后回到基础值", attr.GetValue(AttrType.ATK), 100f);
        }

        private static void Test_AttributeChangedEvent()
        {
            Debug.Log("--- 测试: 属性变化事件 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            int eventCount = 0;
            AttrType lastType = AttrType.Max;
            float lastOld = 0f;
            float lastNew = 0f;

            attr.OnAttrChanged += (type, oldVal, newVal) =>
            {
                eventCount++;
                lastType = type;
                lastOld = oldVal;
                lastNew = newVal;
            };

            // 添加加成触发事件
            attr.AddPct("test", AttrType.ATK, 0.1f, AttrLayer.Pct_SkillCard);
            Assert("事件触发次数", eventCount == 1);
            Assert("事件类型正确", lastType == AttrType.ATK);
            AssertApproximately("事件旧值正确", lastOld, 100f);
            AssertApproximately("事件新值正确", lastNew, 110f);

            // 移除加成也触发事件
            attr.RemovePct("test", AttrType.ATK);
            Assert("移除也触发事件", eventCount == 2);
            AssertApproximately("移除后新值回到基础", lastNew, 100f);
        }

        private static void Test_MultipleSourcesSameLayer()
        {
            Debug.Log("--- 测试: 同一层多个加成源（互相独立乘法） ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.ATK, 100f);

            // 同一层（符文层）两个加成源
            attr.AddPct("rune_red", AttrType.ATK, 0.2f, AttrLayer.Pct_Rune);   // +20%
            attr.AddPct("rune_blue", AttrType.ATK, 0.3f, AttrLayer.Pct_Rune);  // +30%

            // 同层独立乘法：100 × 1.2 × 1.3 = 156
            float expected = 100f * 1.2f * 1.3f;
            AssertApproximately("同层多源乘法", attr.GetValue(AttrType.ATK), expected);

            // 验证不是加法：100 × (1 + 0.2 + 0.3) = 150
            float additiveWrong = 100f * (1f + 0.2f + 0.3f);
            Assert("同层不是加法关系", Mathf.Abs(attr.GetValue(AttrType.ATK) - additiveWrong) > 1f);
        }

        #endregion

        #region HealthComponent 测试

        private static void Test_Health_TakeDamage()
        {
            Debug.Log("--- 测试: HealthComponent 受伤 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.MaxHP, 1000f);
            attr.SetBaseValue(AttrType.DEF, 50f);

            HealthComponent health = new HealthComponent(attr);

            AssertApproximately("初始满血", health.CurrentHP, 1000f);
            Assert("初始存活", !health.IsDead);

            // 受到100点伤害
            float actual = health.TakeDamage(100f);
            AssertApproximately("受伤100后HP", health.CurrentHP, 900f);
            AssertApproximately("实际伤害正确", actual, 100f);
        }

        private static void Test_Health_Heal()
        {
            Debug.Log("--- 测试: HealthComponent 治疗 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.MaxHP, 1000f);
            HealthComponent health = new HealthComponent(attr);

            health.TakeDamage(500f);
            AssertApproximately("受伤后500HP", health.CurrentHP, 500f);

            // 治疗200
            float healed = health.Heal(200f);
            AssertApproximately("治疗后700HP", health.CurrentHP, 700f);
            AssertApproximately("实际治疗量", healed, 200f);

            // 过量治疗（上限）
            healed = health.Heal(500f);
            AssertApproximately("过量治疗后满血", health.CurrentHP, 1000f);
            AssertApproximately("只治疗了300", healed, 300f);
        }

        private static void Test_Health_Death()
        {
            Debug.Log("--- 测试: HealthComponent 死亡 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.MaxHP, 500f);
            HealthComponent health = new HealthComponent(attr);

            bool died = false;
            health.OnDeath += () => { died = true; };

            // 致命伤害
            health.TakeDamage(600f);
            Assert("死亡标记正确", health.IsDead);
            Assert("死亡事件触发", died);
            AssertApproximately("死亡后HP为0", health.CurrentHP, 0f);

            // 死亡后不能再受伤
            float actual = health.TakeDamage(100f);
            Assert("死亡后不受伤害", actual == 0f);

            // 复活
            health.Revive();
            Assert("复活后存活", !health.IsDead);
            AssertApproximately("复活后满血", health.CurrentHP, 500f);
        }

        private static void Test_Health_Invincible()
        {
            Debug.Log("--- 测试: HealthComponent 无敌帧 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.MaxHP, 1000f);
            HealthComponent health = new HealthComponent(attr);

            // 设置无敌
            health.SetInvincible(true);
            Assert("无敌状态", health.IsInvincible);

            // 无敌时不受伤害
            float actual = health.TakeDamage(100f);
            Assert("无敌时不受伤害", actual == 0f);
            AssertApproximately("HP保持不变", health.CurrentHP, 1000f);

            // 取消无敌
            health.SetInvincible(false);
            Assert("取消无敌", !health.IsInvincible);

            // 无敌帧时间
            health.AddInvincibleTime(2f);
            Assert("无敌帧生效", health.IsInvincible);

            // 更新1.5秒
            health.UpdateInvincibleTimer(1.5f);
            Assert("1.5秒后仍无敌", health.IsInvincible);

            // 再更新0.6秒
            health.UpdateInvincibleTimer(0.6f);
            Assert("2.1秒后无敌结束", !health.IsInvincible);
        }

        private static void Test_Health_HPPctEvent()
        {
            Debug.Log("--- 测试: HealthComponent HP百分比变化事件 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.MaxHP, 1000f);
            HealthComponent health = new HealthComponent(attr);

            float lastOldPct = 1f;
            float lastNewPct = 1f;
            int eventCount = 0;

            health.OnHPPctChanged += (oldPct, newPct) =>
            {
                eventCount++;
                lastOldPct = oldPct;
                lastNewPct = newPct;
            };

            // 受伤到50%
            health.TakeDamage(500f);
            Assert("HP变化事件触发", eventCount == 1);
            AssertApproximately("旧百分比", lastOldPct, 1f);
            AssertApproximately("新百分比", lastNewPct, 0.5f);

            // 治疗回满
            health.HealFull();
            Assert("治疗也触发事件", eventCount == 2);
            AssertApproximately("回到100%", lastNewPct, 1f);
        }

        private static void Test_Health_MaxHPChange()
        {
            Debug.Log("--- 测试: MaxHP变化时当前HP按比例调整 ---");

            AttributeComponent attr = new AttributeComponent();
            attr.SetBaseValue(AttrType.MaxHP, 1000f);
            HealthComponent health = new HealthComponent(attr);

            // 受伤到500HP（50%）
            health.TakeDamage(500f);
            AssertApproximately("初始50%HP", health.HPPct, 0.5f);

            // MaxHP翻倍
            attr.SetBaseValue(AttrType.MaxHP, 2000f);
            AssertApproximately("MaxHP翻倍后HP比例不变", health.HPPct, 0.5f);
            AssertApproximately("当前HP变为1000", health.CurrentHP, 1000f);
        }

        #endregion

        #region Entity 测试

        private static void Test_Entity_Basic()
        {
            Debug.Log("--- 测试: Entity 基础功能 ---");

            EntityBase entity = new EntityBase();
            entity.Initialize(1, EntityType.Player, "TestPlayer");

            Assert("ID正确", entity.EntityId == 1);
            Assert("类型正确", entity.EntityType == EntityType.Player);
            Assert("名称正确", entity.EntityName == "TestPlayer");
            Assert("已初始化", entity.IsInitialized);
            Assert("初始存活", entity.IsAlive);
            Assert("属性组件存在", entity.Attribute != null);
            Assert("生命组件存在", entity.Health != null);
        }

        private static void Test_Entity_AddGetComponent()
        {
            Debug.Log("--- 测试: Entity 添加/获取组件 ---");

            EntityBase entity = new EntityBase();
            entity.Initialize(1, EntityType.Player, "TestPlayer");

            // 添加移动组件
            var moveComp = entity.AddComponent<MoveComponent>();
            Assert("添加移动组件", moveComp != null);
            Assert("组件已初始化", moveComp.IsInitialized);

            // 获取组件
            var moveComp2 = entity.GetComponent<MoveComponent>();
            Assert("获取同一组件", moveComp == moveComp2);

            // HasComponent
            Assert("HasComponent正确", entity.HasComponent<MoveComponent>());
            Assert("没有攻击组件", !entity.HasComponent<AttackComponent>());

            // TryGetComponent
            bool hasMove = entity.TryGetComponent(out MoveComponent mc);
            Assert("TryGetComponent成功", hasMove && mc != null);

            bool hasAttack = entity.TryGetComponent(out AttackComponent ac);
            Assert("TryGetComponent失败返回false", !hasAttack && ac == null);
        }

        private static void Test_Entity_RemoveComponent()
        {
            Debug.Log("--- 测试: Entity 移除组件 ---");

            EntityBase entity = new EntityBase();
            entity.Initialize(1, EntityType.Player, "TestPlayer");

            entity.AddComponent<MoveComponent>();
            Assert("添加成功", entity.HasComponent<MoveComponent>());

            bool removed = entity.RemoveComponent<MoveComponent>();
            Assert("移除成功", removed);
            Assert("移除后不存在", !entity.HasComponent<MoveComponent>());

            // 再次移除返回false
            removed = entity.RemoveComponent<MoveComponent>();
            Assert("重复移除返回false", !removed);
        }

        private static void Test_Entity_MoveComponent()
        {
            Debug.Log("--- 测试: MoveComponent ---");

            EntityBase entity = new EntityBase();
            entity.Initialize(1, EntityType.Zombie, "TestZombie");

            var moveComp = entity.AddComponent<MoveComponent>();
            moveComp.SetBaseSpeed(5f);

            // 初始位置
            Assert("初始位置为0", moveComp.Position == Vector3.zero);
            Assert("初始不移动", !moveComp.IsMoving);

            // 设置移动方向
            moveComp.SetMoveDirection(Vector3.right);
            Assert("开始移动", moveComp.IsMoving);
            Assert("方向正确", moveComp.MoveDirection == Vector3.right);

            // 模拟更新1秒
            moveComp.Update(1f);
            AssertApproximately("移动1秒后X位置", moveComp.Position.x, 5f);

            // 停止移动
            moveComp.StopMove();
            Assert("停止移动", !moveComp.IsMoving);
        }

        private static void Test_Entity_AttackComponent()
        {
            Debug.Log("--- 测试: AttackComponent ---");

            // 创建攻击者
            EntityBase attacker = new EntityBase();
            attacker.Initialize(1, EntityType.Player, "Attacker");
            attacker.Attribute.SetBaseValue(AttrType.ATK, 100f);
            attacker.Attribute.SetBaseValue(AttrType.AttackSpeed, 1f);
            attacker.Attribute.SetBaseValue(AttrType.CritRate, 0f); // 0暴击，方便测试
            var attackComp = attacker.AddComponent<AttackComponent>();
            attackComp.SetAttackRange(5f);

            // 创建目标
            EntityBase target = new EntityBase();
            target.Initialize(2, EntityType.Zombie, "Target");
            target.Attribute.SetBaseValue(AttrType.MaxHP, 500f);
            var targetMove = target.AddComponent<MoveComponent>();

            // 目标站在攻击范围内
            var attackerMove = attacker.AddComponent<MoveComponent>();
            attackerMove.SetPosition(Vector3.zero);
            targetMove.SetPosition(new Vector3(2f, 0f, 0f)); // 距离2，在范围内

            // 设置目标
            attackComp.SetTarget(target);

            // 攻击
            bool attacked = attackComp.TryAttack();
            Assert("攻击成功", attacked);
            AssertApproximately("目标受到伤害", target.Health.CurrentHP, 400f);

            // 攻击后进入冷却
            Assert("攻击后不能立即攻击", !attackComp.CanAttack);

            // 更新冷却
            attackComp.Update(1.1f); // 1秒冷却 + 0.1秒缓冲
            Assert("冷却后可再次攻击", attackComp.CanAttack);
        }

        #endregion
    }
}
