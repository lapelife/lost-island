using System;
using UnityEngine;
using LostIsland.Entity;

namespace LostIsland.Logic.Rune
{
    /// <summary>
    /// 符文配置 ScriptableObject
    /// 符文的静态数据配置
    /// </summary>
    [CreateAssetMenu(fileName = "RuneData", menuName = "LostIsland/Rune/符文配置")]
    public class RuneData : ScriptableObject
    {
        [Header("基础信息")]
        public string RuneId;
        public string RuneName;
        public string Description;
        public Rarity Rarity;
        public RuneCategory Category;

        [Header("主属性加成")]
        public AttrType MainAttrType;
        public float MainAttrBasePct;      // 1星基础百分比
        public float MainAttrGrowthPerStar = 0.2f; // 每星+20%

        [Header("副属性加成（紫色及以上）")]
        public AttrType SubAttrType;
        public float SubAttrBasePct;

        [Header("特殊效果（金色专属）")]
        public RuneSpecialEffect SpecialEffect;
        public float SpecialEffectValue;

        [Header("升星")]
        public int MaxStar = 5;

        [Tooltip("每星需要的碎片数（索引0=1→2星需要的碎片）")]
        public int[] ShardsPerStar = new int[] { 10, 20, 40, 80 };

        [Header("外观")]
        public Sprite Icon;
        public Color RuneColor = Color.white;

        #region 计算方法

        /// <summary>
        /// 获取指定星级的主属性加成百分比
        /// </summary>
        public float GetMainAttrPct(int star)
        {
            if (star <= 1) return MainAttrBasePct;
            return MainAttrBasePct * (1f + MainAttrGrowthPerStar * (star - 1));
        }

        /// <summary>
        /// 获取指定星级的副属性加成百分比
        /// </summary>
        public float GetSubAttrPct(int star)
        {
            if (Rarity < Rarity.Epic) return 0f;
            if (star <= 1) return SubAttrBasePct;
            return SubAttrBasePct * (1f + MainAttrGrowthPerStar * (star - 1));
        }

        /// <summary>
        /// 是否有副属性
        /// </summary>
        public bool HasSubAttr => Rarity >= Rarity.Epic && SubAttrType != AttrType.Unknown;

        /// <summary>
        /// 是否有特殊效果
        /// </summary>
        public bool HasSpecialEffect => Rarity == Rarity.Legendary && SpecialEffect != RuneSpecialEffect.None;

        /// <summary>
        /// 获取升星所需碎片数
        /// </summary>
        public int GetShardsForStarUp(int currentStar)
        {
            if (currentStar >= MaxStar) return 0;
            if (currentStar < 1) currentStar = 1;
            int index = currentStar - 1;
            if (index < 0 || index >= ShardsPerStar.Length) return 999;
            return ShardsPerStar[index];
        }

        /// <summary>
        /// 获取升星到目标星级的总碎片
        /// </summary>
        public int GetTotalShardsToStar(int targetStar)
        {
            if (targetStar <= 1) return 0;
            int total = 0;
            for (int i = 1; i < targetStar && i <= MaxStar; i++)
            {
                total += GetShardsForStarUp(i);
            }
            return total;
        }

        #endregion
    }

    /// <summary>
    /// 符文实例（玩家拥有的符文）
    /// </summary>
    [Serializable]
    public class RuneInstance
    {
        /// <summary>实例唯一ID</summary>
        public int InstanceId;

        /// <summary>符文数据引用</summary>
        public RuneData Data;

        /// <summary>当前星级</summary>
        public int Star;

        /// <summary>当前拥有的碎片数</summary>
        public int Shards;

        /// <summary>是否已装备</summary>
        public bool IsEquipped;

        /// <summary>装备到的槽位索引</summary>
        public int EquippedSlotIndex = -1;

        public RuneInstance(int instanceId, RuneData data, int star = 1)
        {
            InstanceId = instanceId;
            Data = data;
            Star = star;
            Shards = 0;
            IsEquipped = false;
            EquippedSlotIndex = -1;
        }

        /// <summary>
        /// 获取当前主属性加成
        /// </summary>
        public float MainAttrPct => Data != null ? Data.GetMainAttrPct(Star) : 0f;

        /// <summary>
        /// 获取当前副属性加成
        /// </summary>
        public float SubAttrPct => Data != null ? Data.GetSubAttrPct(Star) : 0f;

        /// <summary>
        /// 是否可以升星
        /// </summary>
        public bool CanStarUp()
        {
            if (Data == null) return false;
            if (Star >= Data.MaxStar) return false;
            return Shards >= Data.GetShardsForStarUp(Star);
        }

        /// <summary>
        /// 升星
        /// </summary>
        public bool StarUp()
        {
            if (!CanStarUp()) return false;

            int cost = Data.GetShardsForStarUp(Star);
            Shards -= cost;
            Star++;
            return true;
        }

        /// <summary>
        /// 添加碎片
        /// </summary>
        public void AddShards(int amount)
        {
            Shards += amount;
        }
    }
}
