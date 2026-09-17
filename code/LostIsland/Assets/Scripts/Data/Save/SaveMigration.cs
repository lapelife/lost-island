using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Data.Save
{
    /// <summary>
    /// 存档数据版本迁移
    /// 处理旧版本存档升级到新版本
    /// </summary>
    public static class SaveMigration
    {
        /// <summary>当前存档版本号</summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// 迁移存档到最新版本
        /// </summary>
        public static GameSaveData Migrate(GameSaveData save)
        {
            if (save == null)
            {
                return CreateNewSave();
            }

            if (save.version == CurrentVersion)
            {
                return save;
            }

            Debug.Log($"[SaveMigration] 开始迁移存档: v{save.version} → v{CurrentVersion}");

            // 逐步迁移
            if (save.version == 0)
            {
                save = Migrate0To1(save);
                save.version = 1;
            }

            // if (save.version == 1) { save = Migrate1To2(save); save.version = 2; }
            // if (save.version == 2) { save = Migrate2To3(save); save.version = 3; }

            Debug.Log($"[SaveMigration] 迁移完成: v{save.version}");
            return save;
        }

        /// <summary>
        /// 创建新存档
        /// </summary>
        public static GameSaveData CreateNewSave()
        {
            var save = new GameSaveData
            {
                version = CurrentVersion,
                saveTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

                // 玩家
                playerLevel = 1,
                playerExp = 0,
                currentDay = 1,
                maxDayReached = 1,
                currentWave = 0,
                highestWaveReached = 0,

                // 资源（初始资源）
                scrap = 100,
                food = 50,
                diamond = 0,
                coupon = 0,
                crystal = 0,
                rotMeat = 0,

                // 觉醒
                awakenLevel = 0,
                awakenExp = 0,
                totalLoginDays = 1,
                totalPlaySeconds = 0,
                isF2P = true,
                totalRechargeAmount = 0,

                // 楼层（7层）
                floors = new FloorSaveData[7],

                // 技能卡
                equippedActiveCardIds = new string[4],
                equippedActiveCardLevels = new int[4],
                equippedPassiveCardIds = new string[3],
                equippedPassiveCardLevels = new int[3],
                ownedCards = new List<CardSaveData>(),

                // 符文
                equippedRuneIds = new string[6],
                equippedRuneStars = new int[6],
                ownedRunes = new List<RuneSaveData>(),
                runeFragments = new int[4], // 白/蓝/紫/金

                // 幸存者
                survivors = new List<SurvivorSaveData>(),
                deployedSurvivorInstanceIds = new int[3],

                // 统计
                totalZombiesKilled = 0,
                totalBossesKilled = 0,
                totalWavesCleared = 0,
                totalDamageDealt = 0,
                playTimeSeconds = 0f,

                // 成长加速
                consecutiveDefeats = 0,
                lastClearedWave = 0,
                lastClearedDay = 0,

                // 成就
                unlockedAchievements = new List<string>(),

                // 设置
                bgmVolume = 0.7f,
                sfxVolume = 0.8f,
                qualityLevel = 2, // 中等画质
            };

            // 初始化楼层数据
            for (int i = 0; i < 7; i++)
            {
                save.floors[i] = new FloorSaveData
                {
                    floorIndex = i,
                    unlocked = i <= 1, // 初始解锁B1F和1F
                    rooms = new RoomSaveData[0]
                };
            }

            return save;
        }

        #region 迁移方法

        /// <summary>
        /// v0 → v1 迁移
        /// </summary>
        private static GameSaveData Migrate0To1(GameSaveData old)
        {
            // 填充新版本新增字段的默认值
            if (old.runeFragments == null)
                old.runeFragments = new int[4];

            if (old.ownedCards == null)
                old.ownedCards = new List<CardSaveData>();

            if (old.ownedRunes == null)
                old.ownedRunes = new List<RuneSaveData>();

            if (old.survivors == null)
                old.survivors = new List<SurvivorSaveData>();

            if (old.deployedSurvivorInstanceIds == null)
                old.deployedSurvivorInstanceIds = new int[3];

            if (old.unlockedAchievements == null)
                old.unlockedAchievements = new List<string>();

            // 默认设置
            if (old.bgmVolume <= 0f)
                old.bgmVolume = 0.7f;
            if (old.sfxVolume <= 0f)
                old.sfxVolume = 0.8f;
            if (old.qualityLevel <= 0)
                old.qualityLevel = 2;

            Debug.Log("[SaveMigration] v0 → v1 迁移完成");
            return old;
        }

        // 示例：v1 → v2 迁移模板
        // private static GameSaveData Migrate1To2(GameSaveData old)
        // {
        //     // 添加v2新增字段
        //     // old.newField = defaultValue;
        //     Debug.Log("[SaveMigration] v1 → v2 迁移完成");
        //     return old;
        // }

        #endregion

        #region 校验

        /// <summary>
        /// 验证存档数据完整性
        /// </summary>
        public static bool ValidateSave(GameSaveData save)
        {
            if (save == null) return false;
            if (save.version <= 0) return false;
            if (save.version > CurrentVersion) return false; // 版本太新

            // 基本范围检查
            if (save.playerLevel < 1) return false;
            if (save.currentDay < 1) return false;

            return true;
        }

        #endregion
    }
}
