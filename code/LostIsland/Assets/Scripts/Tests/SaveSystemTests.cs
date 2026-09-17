using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LostIsland.Data.Save;

namespace LostIsland.Tests
{
    /// <summary>
    /// 存档系统单元测试
    /// </summary>
    public class SaveSystemTests
    {
        private static int _totalTests = 0;
        private static int _passedTests = 0;
        private static int _failedTests = 0;
        private static List<string> _failedMessages = new List<string>();

        private static string _testSaveDir;

        public static void RunAllTests()
        {
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;
            _failedMessages.Clear();

            Debug.Log("========== 存档系统测试开始 ==========");

            // 创建临时测试目录
            _testSaveDir = Path.Combine(Application.temporaryCachePath, "TestSaves");
            if (Directory.Exists(_testSaveDir))
                Directory.Delete(_testSaveDir, true);
            Directory.CreateDirectory(_testSaveDir);

            // 加密测试
            Test_Encryption_Roundtrip();
            Test_Encryption_EmptyString();
            Test_Encryption_Checksum();

            // 存档数据测试
            Test_SaveData_CreateNew();
            Test_SaveData_Version();
            Test_SaveData_InitialResources();
            Test_SaveData_Floors();

            // 版本迁移测试
            Test_Migration_CurrentVersion();
            Test_Migration_CreateNewSave();
            Test_Migration_ValidateValid();
            Test_Migration_ValidateNull();

            // JSON序列化测试
            Test_Json_Serialization();
            Test_Json_Deserialization();

            // 加密+序列化联合测试
            Test_EncryptAndDecrypt_SaveData();

            // 存档槽测试
            Test_SlotInfo_Struct();

            // 清理
            if (Directory.Exists(_testSaveDir))
                Directory.Delete(_testSaveDir, true);

            // 汇总
            Debug.Log($"========== 测试结束 ==========");
            Debug.Log($"总计: {_totalTests}, 通过: {_passedTests}, 失败: {_failedTests}");

            if (_failedTests > 0)
            {
                Debug.LogError("失败的测试:");
                foreach (var msg in _failedMessages)
                {
                    Debug.LogError($"  - {msg}");
                }
            }
            else
            {
                Debug.Log("🎉 所有测试通过！");
            }
        }

        #region 测试辅助

        private static void Assert(string testName, bool condition, string message = "")
        {
            _totalTests++;
            if (condition)
            {
                _passedTests++;
            }
            else
            {
                _failedTests++;
                string failMsg = string.IsNullOrEmpty(message) ? testName : $"{testName}: {message}";
                _failedMessages.Add(failMsg);
                Debug.LogError($"❌ {testName}: {message}");
            }
        }

        private static void AssertApproximately(string testName, float actual, float expected, float tolerance = 0.001f)
        {
            bool equal = Mathf.Abs(actual - expected) <= tolerance;
            Assert(testName, equal, $"expected={expected:F4}, actual={actual:F4}");
        }

        #endregion

        #region 加密测试

        private static void Test_Encryption_Roundtrip()
        {
            Debug.Log("--- 测试: 加解密往返 ---");

            string original = "Hello, LostIsland! 你好，迷失孤岛！12345";
            string encrypted = SaveEncryption.Encrypt(original);
            string decrypted = SaveEncryption.Decrypt(encrypted);

            Assert("加密后不等于原文", encrypted != original);
            Assert("解密后等于原文", decrypted == original);
        }

        private static void Test_Encryption_EmptyString()
        {
            Debug.Log("--- 测试: 空字符串加密 ---");

            string empty = "";
            string encrypted = SaveEncryption.Encrypt(empty);
            string decrypted = SaveEncryption.Decrypt(encrypted);

            Assert("空字符串加密不变", decrypted == empty);

            string nullStr = null;
            string encryptedNull = SaveEncryption.Encrypt(nullStr);
            Assert("null加密返回null", encryptedNull == null);
        }

        private static void Test_Encryption_Checksum()
        {
            Debug.Log("--- 测试: 校验和 ---");

            string data = "test data for checksum";
            string checksum1 = SaveEncryption.GenerateChecksum(data);
            string checksum2 = SaveEncryption.GenerateChecksum(data);

            Assert("相同数据校验和相同", checksum1 == checksum2);
            Assert("校验和验证通过", SaveEncryption.VerifyChecksum(data, checksum1));

            string differentData = "different data";
            Assert("不同数据校验和不同", !SaveEncryption.VerifyChecksum(differentData, checksum1));
        }

        #endregion

        #region 存档数据测试

        private static void Test_SaveData_CreateNew()
        {
            Debug.Log("--- 测试: 创建新存档 ---");

            var save = SaveMigration.CreateNewSave();
            Assert("新存档不为空", save != null);
            Assert("版本号正确", save.version == SaveMigration.CurrentVersion);
            Assert("时间戳>0", save.saveTimestamp > 0);
        }

        private static void Test_SaveData_Version()
        {
            Debug.Log("--- 测试: 版本号 ---");

            Assert("当前版本>=1", SaveMigration.CurrentVersion >= 1);

            var save = SaveMigration.CreateNewSave();
            Assert("新存档版本=当前版本", save.version == SaveMigration.CurrentVersion);
        }

        private static void Test_SaveData_InitialResources()
        {
            Debug.Log("--- 测试: 初始资源 ---");

            var save = SaveMigration.CreateNewSave();

            Assert("初始废料=100", save.scrap == 100);
            Assert("初始食物=50", save.food == 50);
            Assert("初始钻石=0", save.diamond == 0);
            Assert("初始点券=0", save.coupon == 0);
            Assert("初始晶核=0", save.crystal == 0);
            Assert("初始腐肉=0", save.rotMeat == 0);
        }

        private static void Test_SaveData_Floors()
        {
            Debug.Log("--- 测试: 楼层数据 ---");

            var save = SaveMigration.CreateNewSave();

            Assert("楼层数=7", save.floors != null && save.floors.Length == 7);
            Assert("B1F已解锁", save.floors[0].unlocked);
            Assert("1F已解锁", save.floors[1].unlocked);
            Assert("2F未解锁", !save.floors[2].unlocked);
        }

        #endregion

        #region 版本迁移测试

        private static void Test_Migration_CurrentVersion()
        {
            Debug.Log("--- 测试: 迁移当前版本 ---");

            var save = SaveMigration.CreateNewSave();
            var migrated = SaveMigration.Migrate(save);

            Assert("当前版本迁移不变", migrated.version == SaveMigration.CurrentVersion);
        }

        private static void Test_Migration_CreateNewSave()
        {
            Debug.Log("--- 测试: 创建新存档 ---");

            var save = SaveMigration.CreateNewSave();

            Assert("玩家等级=1", save.playerLevel == 1);
            Assert("当前天=1", save.currentDay == 1);
            Assert("最高天=1", save.maxDayReached == 1);
            Assert("觉醒等级=0", save.awakenLevel == 0);

            Assert("技能卡列表不为null", save.ownedCards != null);
            Assert("符文列表不为null", save.ownedRunes != null);
            Assert("幸存者列表不为null", save.survivors != null);
            Assert("成就列表不为null", save.unlockedAchievements != null);

            Assert("BGM音量=0.7", save.bgmVolume == 0.7f);
            Assert("SFX音量=0.8", save.sfxVolume == 0.8f);
            Assert("画质等级=2", save.qualityLevel == 2);
        }

        private static void Test_Migration_ValidateValid()
        {
            Debug.Log("--- 测试: 验证有效存档 ---");

            var save = SaveMigration.CreateNewSave();
            Assert("新存档验证通过", SaveMigration.ValidateSave(save));
        }

        private static void Test_Migration_ValidateNull()
        {
            Debug.Log("--- 测试: 验证null存档 ---");

            Assert("null存档验证失败", !SaveMigration.ValidateSave(null));
        }

        #endregion

        #region JSON序列化测试

        private static void Test_Json_Serialization()
        {
            Debug.Log("--- 测试: JSON序列化 ---");

            var save = SaveMigration.CreateNewSave();
            string json = JsonUtility.ToJson(save);

            Assert("序列化结果不为空", !string.IsNullOrEmpty(json));
            Assert("包含version字段", json.Contains("\"version\""));
            Assert("包含playerLevel字段", json.Contains("\"playerLevel\""));
            Assert("包含scrap字段", json.Contains("\"scrap\""));
        }

        private static void Test_Json_Deserialization()
        {
            Debug.Log("--- 测试: JSON反序列化 ---");

            var save = SaveMigration.CreateNewSave();
            save.scrap = 9999;
            save.food = 8888;
            save.playerLevel = 50;

            string json = JsonUtility.ToJson(save);
            var loaded = JsonUtility.FromJson<GameSaveData>(json);

            Assert("反序列化不为空", loaded != null);
            Assert("版本号一致", loaded.version == save.version);
            Assert("废料数量正确", loaded.scrap == 9999);
            Assert("食物数量正确", loaded.food == 8888);
            Assert("玩家等级正确", loaded.playerLevel == 50);
        }

        #endregion

        #region 加密+序列化联合测试

        private static void Test_EncryptAndDecrypt_SaveData()
        {
            Debug.Log("--- 测试: 加密+序列化往返 ---");

            var save = SaveMigration.CreateNewSave();
            save.scrap = 12345;
            save.playerLevel = 25;
            save.currentDay = 10;

            // 序列化 → 加密 → 解密 → 反序列化
            string json = JsonUtility.ToJson(save);
            string encrypted = SaveEncryption.Encrypt(json);
            string decrypted = SaveEncryption.Decrypt(encrypted);
            var loaded = JsonUtility.FromJson<GameSaveData>(decrypted);

            Assert("加密后不等于原JSON", encrypted != json);
            Assert("解密后JSON相同", decrypted == json);
            Assert("废料正确", loaded.scrap == 12345);
            Assert("等级正确", loaded.playerLevel == 25);
            Assert("天数正确", loaded.currentDay == 10);
        }

        #endregion

        #region 存档槽测试

        private static void Test_SlotInfo_Struct()
        {
            Debug.Log("--- 测试: 存档槽信息结构 ---");

            SaveSlotInfo info = new SaveSlotInfo
            {
                slotIndex = 0,
                slotName = "测试存档",
                saveTimestamp = 1234567890,
                day = 15,
                wave = 50,
                isAutoSave = false,
                exists = true
            };

            Assert("槽位索引=0", info.slotIndex == 0);
            Assert("天数=15", info.day == 15);
            Assert("波次=50", info.wave == 50);
            Assert("存在", info.exists);
            Assert("不是自动存档", !info.isAutoSave);
        }

        #endregion
    }
}
