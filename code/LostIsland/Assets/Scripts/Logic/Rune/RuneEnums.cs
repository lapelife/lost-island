using System;
using UnityEngine;

namespace LostIsland.Logic.Rune
{
    /// <summary>
    /// 符文分类
    /// </summary>
    public enum RuneCategory
    {
        Attack = 0,     // 攻击类
        Defense = 1,    // 防御类
        Survival = 2,   // 生存类
        Special = 3,    // 特殊类
    }

    /// <summary>
    /// 符文特殊效果类型（金色专属）
    /// </summary>
    public enum RuneSpecialEffect
    {
        None = 0,
        Lifesteal = 1,          // 吸血
        DamageReduction = 2,    // 伤害减免
        AtkFromHp = 3,          // 生命转攻击
        HpFromAtk = 4,          // 攻击转生命
        BerserkOnLowHp = 5,     // 濒死狂暴
        ReflectDamage = 6,      // 反伤护盾
        HealOnKill = 7,         // 击杀回血
        AttackSpeedBoost = 8,   // 攻速加成
        CritChanceBoost = 9,    // 暴击率加成
        MoveSpeedBoost = 10,    // 移速加成
    }

    /// <summary>
    /// 符文槽位类型
    /// </summary>
    public enum RuneSlotType
    {
        Attack = 0,     // 攻击槽
        Defense = 1,    // 防御槽
        Survival = 2,   // 生存槽
        Special = 3,    // 特殊槽
    }
}
