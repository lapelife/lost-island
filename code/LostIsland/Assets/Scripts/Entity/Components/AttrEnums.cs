namespace LostIsland.Entity
{
    /// <summary>
    /// 属性类型枚举
    /// </summary>
    public enum AttrType
    {
        MaxHP = 0,              // 最大生命值
        ATK = 1,                // 攻击力
        DEF = 2,                // 防御力
        AttackSpeed = 3,        // 攻击速度（次/秒）
        CritRate = 4,           // 暴击率
        CritDamage = 5,         // 暴击伤害倍率
        MoveSpeed = 6,          // 移动速度
        Lifesteal = 7,          // 吸血百分比
        DamageReduction = 8,    // 伤害减免
        Reflect = 9,            // 反伤百分比
        // 可扩展更多属性...
        Max
    }

    /// <summary>
    /// 加成层级枚举
    /// 层数顺序 = 计算顺序，后面的乘在更外面
    /// 越靠近战斗的加成层级越高（战斗内成长爽感最明显）
    /// 越永久的加成层级越低（避免永久加成过于膨胀）
    /// </summary>
    public enum AttrLayer
    {
        Base = 0,               // L0: 基础值（等级系数计算后）
        FlatAdd = 1,            // L1: 固定值加（装备等）
        Pct_SkillCard = 2,      // L2: 技能卡百分比
        Pct_Rune = 3,           // L3: 符文百分比
        Pct_Awaken = 4,         // L4: 觉醒百分比
        Pct_Persevere = 5,      // L5: 毅力加成（0氪专属）
        Pct_Skin = 6,           // L6: 皮肤百分比
        Pct_Survivor = 7,       // L7: 幸存者百分比
        Pct_Tower = 8,          // L8: 塔设施百分比
        Pct_Legacy = 9,         // L9: 赛季传承
        Pct_Permanent = 10,     // L10: 永久微加成
        Pct_Activity = 11,      // L11: 累计活跃加成
        Pct_Flesh = 12,         // L12: 腐肉升级（战斗内临时）
        Pct_Buff = 13,          // L13: 战斗buff/debuff
        Pct_SpeedUp = 14,       // L14: 成长加速（卡点触发）
        // 注意：层级数量固定为15层
        Max
    }
}
