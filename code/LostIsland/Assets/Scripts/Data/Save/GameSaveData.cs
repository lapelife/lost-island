using System;
using System.Collections.Generic;

namespace LostIsland.Data.Save
{
    /// <summary>
    /// 游戏存档数据
    /// 版本号控制数据结构变更
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public int version = 1;
        public long saveTimestamp;

        // ===== 玩家基础 =====
        public int playerLevel;
        public long playerExp;
        public int currentDay;
        public int maxDayReached;
        public int currentWave;
        public int highestWaveReached;

        // ===== 资源 =====
        public int scrap;
        public int food;
        public int diamond;
        public int coupon;       // 点券
        public int crystal;      // 晶核
        public int rotMeat;      // 腐肉

        // ===== 觉醒系统 =====
        public int awakenLevel;
        public long awakenExp;
        public int totalLoginDays;
        public int totalPlaySeconds;
        public bool isF2P;
        public int totalRechargeAmount; // 累充金额(分)

        // ===== 塔内建造 =====
        public FloorSaveData[] floors;

        // ===== 技能卡 =====
        public string[] equippedActiveCardIds;
        public int[] equippedActiveCardLevels;
        public string[] equippedPassiveCardIds;
        public int[] equippedPassiveCardLevels;
        public List<CardSaveData> ownedCards;

        // ===== 符文 =====
        public string[] equippedRuneIds;
        public int[] equippedRuneStars;
        public List<RuneSaveData> ownedRunes;
        public int[] runeFragments;  // 各品质符文碎片 [白,蓝,紫,金]

        // ===== 幸存者 =====
        public List<SurvivorSaveData> survivors;
        public int[] deployedSurvivorInstanceIds;

        // ===== 统计数据 =====
        public long totalZombiesKilled;
        public long totalBossesKilled;
        public long totalWavesCleared;
        public long totalDamageDealt;
        public float playTimeSeconds;

        // ===== 成长加速 =====
        public int consecutiveDefeats;
        public int lastClearedWave;
        public int lastClearedDay;

        // ===== 成就 =====
        public List<string> unlockedAchievements;

        // ===== 设置 =====
        public float bgmVolume;
        public float sfxVolume;
        public int qualityLevel; // 画质等级
    }

    #region 辅助结构体

    /// <summary>
    /// 楼层存档数据
    /// </summary>
    [Serializable]
    public struct FloorSaveData
    {
        public int floorIndex;
        public bool unlocked;
        public RoomSaveData[] rooms;
    }

    /// <summary>
    /// 房间存档数据
    /// </summary>
    [Serializable]
    public struct RoomSaveData
    {
        public int roomIndex;
        public bool unlocked;
        public string buildingId;
        public int buildingLevel;
    }

    /// <summary>
    /// 技能卡存档数据
    /// </summary>
    [Serializable]
    public struct CardSaveData
    {
        public string cardId;
        public int level;
        public int count; // 拥有数量（用于升星）
    }

    /// <summary>
    /// 符文存档数据
    /// </summary>
    [Serializable]
    public struct RuneSaveData
    {
        public string runeId;
        public int star;
        public int shards;
        public int instanceId;
        public bool isEquipped;
        public int slotIndex;
    }

    /// <summary>
    /// 幸存者存档数据
    /// </summary>
    [Serializable]
    public struct SurvivorSaveData
    {
        public string survivorId;
        public int instanceId;
        public int level;
        public int exp;
        public int intimacy;
        public bool isDeployed;
        public int deploySlotIndex;
        public int[] skillLevels;
    }

    /// <summary>
    /// 存档槽信息
    /// </summary>
    [Serializable]
    public struct SaveSlotInfo
    {
        public int slotIndex;
        public string slotName;
        public long saveTimestamp;
        public int day;
        public int wave;
        public bool isAutoSave;
        public bool exists;
    }

    #endregion
}
