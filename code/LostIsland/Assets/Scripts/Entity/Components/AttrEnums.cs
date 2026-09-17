namespace LostIsland.Entity
{
    /// <summary>
    /// 伤害来源类型
    /// </summary>
    public enum DamageSourceType
    {
        Unknown = 0,    // 未知来源
        Normal = 1,     // 普通攻击
        Skill = 2,      // 技能伤害
        Tower = 3,      // 防御塔伤害
        Survivor = 4,   // 幸存者伤害
        Burn = 5,       // 燃烧伤害
        Explosion = 6,  // 爆炸伤害
        Reflect = 7,    // 反伤
    }

    /// <summary>
    /// 状态效果类型
    /// </summary>
    public enum StatusType
    {
        None = 0,       // 无状态
        Burn = 1,       // 燃烧（持续伤害）
        Slow = 2,       // 减速
        Stun = 3,       // 眩晕
        Poison = 4,     // 中毒
        Freeze = 5,     // 冰冻
        Shield = 6,     // 护盾
        Haste = 7,      // 加速
        Invincible = 8, // 无敌
    }

    /// <summary>
    /// 属性类型枚举
    /// </summary>
    public enum AttrType
    {
        Unknown = 0,            // 未知/无效
        MaxHP = 1,              // 最大生命值
        ATK = 2,                // 攻击力
        DEF = 3,                // 防御力
        AttackSpeed = 4,        // 攻击速度（次/秒）
        CritRate = 5,           // 暴击率
        CritDamage = 6,         // 暴击伤害倍率
        MoveSpeed = 7,          // 移动速度
        Lifesteal = 8,          // 吸血百分比
        DamageReduction = 9,    // 伤害减免
        Reflect = 10,           // 反伤百分比
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
        Flat_Tower = 15,        // L15: 塔设施固定值加成
        // 注意：层级数量固定为16层
        Max
    }
}
