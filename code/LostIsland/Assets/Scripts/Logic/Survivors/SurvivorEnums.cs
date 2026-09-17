using System;
using UnityEngine;

namespace LostIsland.Logic.Survivors
{
    /// <summary>
    /// 幸存者职业
    /// </summary>
    public enum SurvivorClass
    {
        Warrior = 0,      // 战士 - 近战高伤
        Archer = 1,       // 射手 - 远程输出
        Medic = 2,        // 医生 - 治疗辅助
        Engineer = 3,     // 工程师 - 建造加成
        Scout = 4,        // 侦察兵 - 高移速
        Guard = 5,        // 守卫 - 高防御
    }

    /// <summary>
    /// 幸存者稀有度
    /// </summary>
    public enum SurvivorRarity
    {
        Common = 0,       // 普通 - 白
        Rare = 1,         // 精英 - 蓝
        Epic = 2,         // 稀有 - 紫
        Legendary = 3,    // 传说 - 金
    }

    /// <summary>
    /// 幸存者状态
    /// </summary>
    public enum SurvivorStatus
    {
        Idle = 0,         // 空闲
        Working = 1,      // 工作中（建筑内）
        Fighting = 2,     // 战斗中
        Injured = 3,      // 受伤（不能出战）
        Resting = 4,      // 休息中
    }

    /// <summary>
    /// 幸存者技能类型
    /// </summary>
    public enum SurvivorSkillType
    {
        Passive = 0,      // 被动技能（属性加成）
        Active = 1,       // 主动技能（战斗中释放）
        Work = 2,         // 工作技能（建造/生产加成）
    }
}
