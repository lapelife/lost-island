using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 建筑类型枚举
    /// 分为三大类：防御类、资源类、功能类
    /// </summary>
    public enum BuildingType
    {
        None = 0,

        // ===== 防御类（4种）=====
        /// <summary>机枪塔：快速单体攻击</summary>
        MachineGun = 101,
        /// <summary>炮塔：慢速范围爆炸</summary>
        Cannon = 102,
        /// <summary>箭塔：穿透直线射击</summary>
        ArrowTower = 103,
        /// <summary>电击塔：链式闪电</summary>
        TeslaTower = 104,

        // ===== 资源类（3种）=====
        /// <summary>废料回收站：增加腐肉掉落</summary>
        ScrapRecycler = 201,
        /// <summary>食物储藏室：增加食物上限</summary>
        FoodStorage = 202,
        /// <summary>晶核提炼器：增加晶核掉落率</summary>
        CrystalRefinery = 203,

        // ===== 功能类（6种）=====
        /// <summary>发电机：增加电力上限</summary>
        Generator = 301,
        /// <summary>医疗室：增加生命上限/回血</summary>
        MedBay = 302,
        /// <summary>工坊：增加攻击力</summary>
        Workshop = 303,
        /// <summary>研究室：解锁科技/增加研究点</summary>
        ResearchLab = 304,
        /// <summary>监控室：增加视野/索敌范围</summary>
        SurveillanceRoom = 305,
        /// <summary>探照灯：塔顶固定建筑</summary>
        Searchlight = 306,
    }

    /// <summary>
    /// 建筑分类
    /// </summary>
    public enum BuildingCategory
    {
        Defense = 1,    // 防御类
        Resource = 2,   // 资源类
        Utility = 3,    // 功能类
    }

    /// <summary>
    /// 索敌策略
    /// </summary>
    public enum TargetingStrategy
    {
        Nearest = 0,        // 最近的
        Farthest = 1,       // 最远的
        LowestHP = 2,       // 血量最低
        HighestHP = 3,      // 血量最高
        FirstInPath = 4,    // 路径最前面的
    }

    /// <summary>
    /// 攻击方式
    /// </summary>
    public enum AttackType
    {
        SingleShot = 0,     // 单体射击
        AOE = 1,            // 范围爆炸
        Piercing = 2,       // 穿透
        Chain = 3,          // 链式
    }
}
