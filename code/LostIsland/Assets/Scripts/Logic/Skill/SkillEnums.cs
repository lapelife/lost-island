using System;
using UnityEngine;

namespace LostIsland.Logic.Skill
{
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        Active = 0,     // 主动技能
        Passive = 1,    // 被动技能
        Summon = 2,     // 召唤技能
        Trap = 3,       // 陷阱技能
    }

    /// <summary>
    /// 稀有度
    /// </summary>
    public enum Rarity
    {
        Common = 0,     // 白
        Rare = 1,       // 蓝
        Epic = 2,       // 紫
        Legendary = 3,  // 金
    }

    /// <summary>
    /// 效果类型
    /// </summary>
    public enum EffectType
    {
        None = 0,
        Damage = 1,         // 单体伤害
        AoeDamage = 2,      // 范围伤害
        Burn = 3,           // 燃烧（持续伤害）
        Freeze = 4,         // 冰冻（减速+定身）
        Stun = 5,           // 眩晕
        Slow = 6,           // 减速
        Knockback = 7,      // 击退
        Heal = 8,           // 治疗
        Summon = 9,         // 召唤
        Shield = 10,        // 护盾
        Electric = 11,      // 电击
        Poison = 12,        // 中毒
    }

    /// <summary>
    /// 技能目标类型
    /// </summary>
    public enum SkillTargetType
    {
        Self = 0,           // 自身
        SingleEnemy = 1,    // 单个敌人
        AOE = 2,            // 范围敌人
        AllEnemies = 3,     // 所有敌人
        Ally = 4,           // 友方
        Position = 5,       // 指定位置
    }
}
