using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Data.Save
{
    /// <summary>
    /// 存档管理器
    /// 负责存档的保存、读取、删除、自动存档
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        #region 配置

        /// <summary>手动存档槽数量</summary>
        public const int MANUAL_SLOT_COUNT = 3;

        /// <summary>自动存档槽索引</summary>
        public const int AUTO_SAVE_SLOT = 3;

        /// <summary>存档文件前缀</summary>
        private const string SAVE_FILE_PREFIX = "save_slot_";

        /// <summary>存档文件后缀</summary>
        private const string SAVE_FILE_EXT = ".dat";

        /// <summary>存档信息文件后缀</summary>
        private const string INFO_FILE_EXT = ".info";

        /// <summary>是否启用加密</summary>
        public bool EnableEncryption = true;

        #endregion

        #region 当前状态

        /// <summary>当前使用的存档槽</summary>
        public int CurrentSlotIndex { get; private set; } = -1;

        /// <summary>当前存档数据</summary>
        public GameSaveData CurrentSave { get; private set; }

        /// <summary>是否有未保存的更改</summary>
        public bool HasUnsavedChanges { get; private set; }

        #endregion

        #region 自动存档

        /// <summary>自动存档间隔（秒）</summary>
        public float AutoSaveInterval = 120f; // 2分钟

        private float _autoSaveTimer = 0f;
        private bool _autoSaveEnabled = true;

        #endregion

        #region 事件

        /// <summary>存档保存前事件</summary>
        public event Action<int> OnBeforeSave;

        /// <summary>存档保存后事件</summary>
        public event Action<int, bool> OnAfterSave; // slotIndex, success

        /// <summary>存档读取前事件</summary>
        public event Action<int> OnBeforeLoad;

        /// <summary>存档读取后事件</summary>
        public event Action<int, bool> OnAfterLoad; // slotIndex, success

        /// <summary>新游戏开始事件</summary>
        public event Action OnNewGame;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            Debug.Log("[SaveManager] 存档管理器初始化完成");
            Debug.Log($"[SaveManager] 存档路径: {GetSaveDirectory()}");
        }

        #endregion

        #region 存档目录

        /// <summary>
        /// 获取存档目录路径
        /// </summary>
        public string GetSaveDirectory()
        {
            string dir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        /// <summary>
        /// 获取存档文件路径
        /// </summary>
        public string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(GetSaveDirectory(), $"{SAVE_FILE_PREFIX}{slotIndex}{SAVE_FILE_EXT}");
        }

        /// <summary>
        /// 获取存档信息文件路径
        /// </summary>
        public string GetSaveInfoPath(int slotIndex)
        {
            return Path.Combine(GetSaveDirectory(), $"{SAVE_FILE_PREFIX}{slotIndex}{INFO_FILE_EXT}");
        }

        #endregion

        #region 新游戏

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public GameSaveData NewGame(int slotIndex = 0)
        {
            Debug.Log($"[SaveManager] 新游戏，槽位: {slotIndex}");

            CurrentSave = SaveMigration.CreateNewSave();
            CurrentSlotIndex = slotIndex;
            HasUnsavedChanges = true;

            OnNewGame?.Invoke();

            // 立即保存一次
            Save(slotIndex);

            return CurrentSave;
        }

        #endregion

        #region 保存

        /// <summary>
        /// 保存到当前槽位
        /// </summary>
        public bool Save()
        {
            if (CurrentSlotIndex < 0)
            {
                Debug.LogWarning("[SaveManager] 没有当前存档槽位");
                return false;
            }
            return Save(CurrentSlotIndex);
        }

        /// <summary>
        /// 保存到指定槽位
        /// </summary>
        public bool Save(int slotIndex)
        {
            if (CurrentSave == null)
            {
                Debug.LogWarning("[SaveManager] 当前存档数据为空");
                return false;
            }

            OnBeforeSave?.Invoke(slotIndex);

            try
            {
                // 更新时间戳
                CurrentSave.saveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 序列化
                string json = JsonUtility.ToJson(CurrentSave, true);

                // 加密
                if (EnableEncryption)
                {
                    json = SaveEncryption.Encrypt(json);
                }

                // 写入文件
                string filePath = GetSaveFilePath(slotIndex);
                File.WriteAllText(filePath, json);

                // 写入存档信息
                SaveSlotInfo info = new SaveSlotInfo
                {
                    slotIndex = slotIndex,
                    saveTimestamp = CurrentSave.saveTimestamp,
                    day = CurrentSave.currentDay,
                    wave = CurrentSave.highestWaveReached,
                    isAutoSave = slotIndex == AUTO_SAVE_SLOT,
                    exists = true
                };
                string infoJson = JsonUtility.ToJson(info);
                File.WriteAllText(GetSaveInfoPath(slotIndex), infoJson);

                HasUnsavedChanges = false;

                OnAfterSave?.Invoke(slotIndex, true);
                Debug.Log($"[SaveManager] 保存成功: 槽位{slotIndex}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 保存失败: {e.Message}");
                OnAfterSave?.Invoke(slotIndex, false);
                return false;
            }
        }

        /// <summary>
        /// 自动存档
        /// </summary>
        public bool AutoSave()
        {
            if (CurrentSave == null) return false;

            Debug.Log("[SaveManager] 执行自动存档");
            return Save(AUTO_SAVE_SLOT);
        }

        /// <summary>
        /// 标记有未保存更改
        /// </summary>
        public void MarkDirty()
        {
            HasUnsavedChanges = true;
        }

        #endregion

        #region 读取

        /// <summary>
        /// 读取指定槽位存档
        /// </summary>
        public GameSaveData Load(int slotIndex)
        {
            OnBeforeLoad?.Invoke(slotIndex);

            try
            {
                string filePath = GetSaveFilePath(slotIndex);
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SaveManager] 存档不存在: 槽位{slotIndex}");
                    OnAfterLoad?.Invoke(slotIndex, false);
                    return null;
                }

                // 读取文件
                string json = File.ReadAllText(filePath);

                // 解密
                if (EnableEncryption)
                {
                    json = SaveEncryption.Decrypt(json);
                }

                // 反序列化
                GameSaveData save = JsonUtility.FromJson<GameSaveData>(json);

                if (save == null)
                {
                    Debug.LogError($"[SaveManager] 存档解析失败: 槽位{slotIndex}");
                    OnAfterLoad?.Invoke(slotIndex, false);
                    return null;
                }

                // 版本迁移
                save = SaveMigration.Migrate(save);

                // 验证
                if (!SaveMigration.ValidateSave(save))
                {
                    Debug.LogError($"[SaveManager] 存档验证失败: 槽位{slotIndex}");
                    OnAfterLoad?.Invoke(slotIndex, false);
                    return null;
                }

                CurrentSave = save;
                CurrentSlotIndex = slotIndex;
                HasUnsavedChanges = false;

                OnAfterLoad?.Invoke(slotIndex, true);
                Debug.Log($"[SaveManager] 读取成功: 槽位{slotIndex}, 版本v{save.version}");
                return save;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 读取失败: {e.Message}");
                OnAfterLoad?.Invoke(slotIndex, false);
                return null;
            }
        }

        #endregion

        #region 存档槽管理

        /// <summary>
        /// 获取所有存档槽信息
        /// </summary>
        public SaveSlotInfo[] GetAllSlotInfos()
        {
            SaveSlotInfo[] infos = new SaveSlotInfo[MANUAL_SLOT_COUNT + 1]; // 手动+自动

            for (int i = 0; i <= MANUAL_SLOT_COUNT; i++)
            {
                infos[i] = GetSlotInfo(i);
            }

            return infos;
        }

        /// <summary>
        /// 获取指定槽位信息
        /// </summary>
        public SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            SaveSlotInfo info = new SaveSlotInfo
            {
                slotIndex = slotIndex,
                exists = false,
                isAutoSave = slotIndex == AUTO_SAVE_SLOT
            };

            try
            {
                string infoPath = GetSaveInfoPath(slotIndex);
                if (File.Exists(infoPath))
                {
                    string json = File.ReadAllText(infoPath);
                    info = JsonUtility.FromJson<SaveSlotInfo>(json);
                    info.slotIndex = slotIndex;
                    info.exists = true;
                }
            }
            catch
            {
                info.exists = false;
            }

            return info;
        }

        /// <summary>
        /// 检查槽位是否有存档
        /// </summary>
        public bool HasSave(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除指定槽位存档
        /// </summary>
        public bool DeleteSave(int slotIndex)
        {
            try
            {
                string savePath = GetSaveFilePath(slotIndex);
                string infoPath = GetSaveInfoPath(slotIndex);

                if (File.Exists(savePath))
                    File.Delete(savePath);

                if (File.Exists(infoPath))
                    File.Delete(infoPath);

                // 如果删除的是当前槽位
                if (slotIndex == CurrentSlotIndex)
                {
                    CurrentSlotIndex = -1;
                    CurrentSave = null;
                    HasUnsavedChanges = false;
                }

                Debug.Log($"[SaveManager] 删除存档: 槽位{slotIndex}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 删除存档失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 复制存档
        /// </summary>
        public bool CopySave(int fromSlot, int toSlot)
        {
            try
            {
                string fromSavePath = GetSaveFilePath(fromSlot);
                string fromInfoPath = GetSaveInfoPath(fromSlot);
                string toSavePath = GetSaveFilePath(toSlot);
                string toInfoPath = GetSaveInfoPath(toSlot);

                if (!File.Exists(fromSavePath)) return false;

                File.Copy(fromSavePath, toSavePath, true);

                if (File.Exists(fromInfoPath))
                {
                    File.Copy(fromInfoPath, toInfoPath, true);
                }

                Debug.Log($"[SaveManager] 复制存档: {fromSlot} → {toSlot}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 复制存档失败: {e.Message}");
                return false;
            }
        }

        #endregion

        #region 自动存档更新

        private void Update()
        {
            if (!_autoSaveEnabled || CurrentSave == null) return;

            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= AutoSaveInterval)
            {
                _autoSaveTimer = 0f;
                AutoSave();
            }
        }

        /// <summary>
        /// 设置自动存档开关
        /// </summary>
        public void SetAutoSaveEnabled(bool enabled)
        {
            _autoSaveEnabled = enabled;
        }

        /// <summary>
        /// 重置自动存档计时器
        /// </summary>
        public void ResetAutoSaveTimer()
        {
            _autoSaveTimer = 0f;
        }

        #endregion

        #region 战斗前后存档

        /// <summary>
        /// 战斗前存档（备份）
        /// </summary>
        public void SaveBeforeBattle()
        {
            // 保存到自动存档槽作为战斗前备份
            AutoSave();
            Debug.Log("[SaveManager] 战斗前备份存档");
        }

        /// <summary>
        /// 战斗胜利后存档
        /// </summary>
        public void SaveAfterVictory()
        {
            Save();
            Debug.Log("[SaveManager] 战斗胜利后存档");
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            OnBeforeSave = null;
            OnAfterSave = null;
            OnBeforeLoad = null;
            OnAfterLoad = null;
            OnNewGame = null;

            // 退出前保存
            if (CurrentSave != null && HasUnsavedChanges)
            {
                Save();
            }

            base.OnDispose();
        }

        #endregion
    }
}
