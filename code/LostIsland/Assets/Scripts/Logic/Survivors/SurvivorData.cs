using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Survivors
{
    /// <summary>
    /// 幸存者配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "SurvivorData", menuName = "LostIsland/Survivor/幸存者配置")]
    public class SurvivorData : ScriptableObject
    {
        [Header("基础信息")]
        public string SurvivorId;
        public string SurvivorName;
        public string Description;
        public SurvivorClass Class;
        public SurvivorRarity Rarity;

        [Header("基础属性（1级）")]
        public float BaseHP;
        public float BaseATK;
        public float BaseDEF;
        public float BaseSpeed;
        public float BaseCritRate;
        public float BaseCritDamage;

        [Header("属性成长（每级）")]
        public float HPGrowth = 0.08f;    // 每级+8%
        public float ATKGrowth = 0.06f;   // 每级+6%
        public float DEFGrowth = 0.05f;   // 每级+5%

        [Header("等级上限")]
        public int MaxLevel = 50;

        [Header("技能")]
        public List<SurvivorSkillEntry> Skills = new List<SurvivorSkillEntry>();

        [Header("工作加成")]
        public float BuildingSpeedBonus = 0f;   // 建造速度加成
        public float ProductionBonus = 0f;      // 产出加成
        public float ResearchBonus = 0f;        // 研究加成

        [Header("外观")]
        public Sprite Avatar;
        public Color ThemeColor = Color.white;

        #region 计算方法

        /// <summary>
        /// 获取指定等级的最大HP
        /// </summary>
        public float GetMaxHP(int level)
        {
            return BaseHP * (1f + HPGrowth * (level - 1));
        }

        /// <summary>
        /// 获取指定等级的ATK
        /// </summary>
        public float GetATK(int level)
        {
            return BaseATK * (1f + ATKGrowth * (level - 1));
        }

        /// <summary>
        /// 获取指定等级的DEF
        /// </summary>
        public float GetDEF(int level)
        {
            return BaseDEF * (1f + DEFGrowth * (level - 1));
        }

        /// <summary>
        /// 获取战斗力（粗略计算）
        /// </summary>
        public float GetCombatPower(int level)
        {
            float hp = GetMaxHP(level);
            float atk = GetATK(level);
            float def = GetDEF(level);
            return hp * 0.2f + atk * 3f + def * 2f + BaseSpeed * 5f;
        }

        #endregion
    }

    /// <summary>
    /// 幸存者技能条目
    /// </summary>
    [Serializable]
    public struct SurvivorSkillEntry
    {
        public string SkillId;
        public string SkillName;
        public SurvivorSkillType SkillType;
        public string Description;

        [Header("解锁等级")]
        public int UnlockLevel;

        [Header("数值")]
        public AttrType AttrType;     // 被动技能：加成属性类型
        public float ValuePerLevel;   // 每级数值
        public bool IsPercentage;     // 是否百分比

        [Header("主动技能")]
        public float Cooldown;        // 冷却时间
        public float DamageMultiplier; // 伤害倍率
        public float EffectValue;     // 效果值
        public float Range;           // 作用范围
    }

    /// <summary>
    /// 幸存者实例
    /// </summary>
    [Serializable]
    public class SurvivorInstance
    {
        /// <summary>实例唯一ID</summary>
        public int InstanceId;

        /// <summary>幸存者数据引用</summary>
        public SurvivorData Data;

        /// <summary>当前等级</summary>
        public int Level;

        /// <summary>当前经验</summary>
        public int Exp;

        /// <summary>亲密度（0~100）</summary>
        public int Intimacy;

        /// <summary>当前状态</summary>
        public SurvivorStatus Status;

        /// <summary>当前HP</summary>
        public float CurrentHP;

        /// <summary>是否已上阵</summary>
        public bool IsDeployed;

        /// <summary>上阵位置</summary>
        public int DeploySlotIndex = -1;

        /// <summary>技能等级列表（索引对应Data.Skills索引）</summary>
        public int[] SkillLevels;

        public SurvivorInstance(int instanceId, SurvivorData data)
        {
            InstanceId = instanceId;
            Data = data;
            Level = 1;
            Exp = 0;
            Intimacy = 0;
            Status = SurvivorStatus.Idle;
            IsDeployed = false;
            DeploySlotIndex = -1;

            // 初始化技能等级
            SkillLevels = new int[data.Skills.Count];
            for (int i = 0; i < SkillLevels.Length; i++)
            {
                SkillLevels[i] = 1;
            }

            // 初始满血
            CurrentHP = data.GetMaxHP(1);
        }

        #region 属性计算

        /// <summary>最大HP</summary>
        public float MaxHP => Data != null ? Data.GetMaxHP(Level) : 0f;

        /// <summary>攻击力</summary>
        public float ATK => Data != null ? Data.GetATK(Level) : 0f;

        /// <summary>防御力</summary>
        public float DEF => Data != null ? Data.GetDEF(Level) : 0f;

        /// <summary>HP百分比</summary>
        public float HPPct => MaxHP > 0f ? CurrentHP / MaxHP : 0f;

        /// <summary>战斗力</summary>
        public float CombatPower => Data != null ? Data.GetCombatPower(Level) : 0f;

        #endregion

        #region 经验与升级

        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        public int GetExpToNextLevel()
        {
            if (Data == null || Level >= Data.MaxLevel) return 0;
            return 50 + Level * 30; // 简单公式
        }

        /// <summary>
        /// 添加经验
        /// </summary>
        public bool AddExp(int amount)
        {
            if (Data == null || Level >= Data.MaxLevel) return false;

            Exp += amount;
            bool leveledUp = false;

            while (Exp >= GetExpToNextLevel() && Level < Data.MaxLevel)
            {
                Exp -= GetExpToNextLevel();
                Level++;
                leveledUp = true;

                // 升级回满血
                CurrentHP = MaxHP;
            }

            return leveledUp;
        }

        #endregion

        #region 生命操作

        /// <summary>
        /// 受伤
        /// </summary>
        public float TakeDamage(float damage)
        {
            float actualDamage = Mathf.Max(1f, damage - DEF * 0.5f);
            CurrentHP = Mathf.Max(0f, CurrentHP - actualDamage);

            if (CurrentHP <= 0f)
            {
                Status = SurvivorStatus.Injured;
            }

            return actualDamage;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public float Heal(float amount)
        {
            if (Status == SurvivorStatus.Injured) return 0f;

            float oldHP = CurrentHP;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            return CurrentHP - oldHP;
        }

        /// <summary>
        /// 恢复（休息后）
        /// </summary>
        public void Recover()
        {
            CurrentHP = MaxHP;
            if (Status == SurvivorStatus.Injured)
                Status = SurvivorStatus.Idle;
        }

        #endregion
    }
}
