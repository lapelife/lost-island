using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Data.Config
{
    /// <summary>
    /// 配置加载器：游戏启动时从 Resources 加载所有配置到 ConfigDatabase
    /// 后续可扩展为 Addressables 异步加载
    /// </summary>
    public class ConfigLoader : MonoSingleton<ConfigLoader>
    {
        [Header("加载设置")]
        [Tooltip("是否在启动时自动加载")]
        [SerializeField] private bool _loadOnStart = true;

        [Tooltip("Resources 下的配置根目录")]
        [SerializeField] private string _configRootPath = "Configs/";

        // 加载进度
        private float _loadProgress = 0f;
        private bool _isLoading = false;
        private bool _isLoaded = false;

        public float LoadProgress => _loadProgress;
        public bool IsLoading => _isLoading;
        public bool IsLoaded => _isLoaded;

        protected override void OnInit()
        {
            base.OnInit();

            if (_loadOnStart)
            {
                LoadAllConfigs();
            }
        }

        /// <summary>
        /// 加载所有配置
        /// </summary>
        public void LoadAllConfigs()
        {
            if (_isLoading)
            {
                Debug.LogWarning("[ConfigLoader] 配置正在加载中，请稍候...");
                return;
            }

            _isLoading = true;
            _loadProgress = 0f;

            // 初始化配置数据库
            ConfigDatabase.Initialize();

            // 同步加载所有配置（后续可改为异步）
            try
            {
                LoadConfigs<ZombieConfigSO>("Zombies");
                _loadProgress = 0.2f;

                LoadConfigs<SkillCardConfigSO>("SkillCards");
                _loadProgress = 0.3f;

                LoadConfigs<RuneConfigSO>("Runes");
                _loadProgress = 0.4f;

                LoadConfigs<BuildingConfigSO>("Buildings");
                _loadProgress = 0.5f;

                LoadConfigs<SkinConfigSO>("Skins");
                _loadProgress = 0.6f;

                LoadConfigs<ZombieConfigSO>("Zombies");
                _loadProgress = 0.8f;

                LoadConfigs<WaveConfigSO>("Waves");
                _loadProgress = 1.0f;

                _isLoaded = true;
                Debug.Log("[ConfigLoader] 所有配置加载完成");

                // 输出统计
                ConfigDatabase.LogStats();

                // 触发加载完成事件
                EventBus.Trigger(new ConfigLoadedEvent());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ConfigLoader] 配置加载失败: {e.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// 加载指定类型的配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="folderName">Resources 下的子文件夹名</param>
        private void LoadConfigs<T>(string folderName) where T : BaseConfigSO
        {
            string fullPath = _configRootPath + folderName;
            T[] configs = Resources.LoadAll<T>(fullPath);

            if (configs != null && configs.Length > 0)
            {
                ConfigDatabase.RegisterConfigs(configs);
                Debug.Log($"[ConfigLoader] 加载 {folderName}: {configs.Length} 条");
            }
            else
            {
                Debug.LogWarning($"[ConfigLoader] 未找到配置: {fullPath}");
            }
        }

        /// <summary>
        /// 重新加载所有配置
        /// </summary>
        public void ReloadAllConfigs()
        {
            ConfigDatabase.Clear();
            _isLoaded = false;
            LoadAllConfigs();
        }
    }

    #region 事件定义

    /// <summary>
    /// 配置加载完成事件
    /// </summary>
    public struct ConfigLoadedEvent : IEvent
    {
    }

    #endregion

    #region 配置类型占位类（后续在对应模块中实现具体配置）

    // 以下为各系统配置类型的占位声明，实际实现在各自模块中
    // 这里仅用于 ConfigLoader 能正确加载，后续会被具体实现替换

    /// <summary>
    /// 丧尸配置（已在 Wave 模块实现）
    /// </summary>
    public class ZombieConfigSO : BaseConfigSO
    {
    }

    /// <summary>
    /// 技能卡配置（占位，后续在 Skill 模块实现）
    /// </summary>
    public class SkillCardConfigSO : BaseConfigSO
    {
    }

    /// <summary>
    /// 符文配置（占位，后续在 Rune 模块实现）
    /// </summary>
    public class RuneConfigSO : BaseConfigSO
    {
    }

    /// <summary>
    /// 建筑配置（占位，后续在 Tower 模块实现）
    /// </summary>
    public class BuildingConfigSO : BaseConfigSO
    {
    }

    /// <summary>
    /// 皮肤配置（占位，后续在 Skin 模块实现）
    /// </summary>
    public class SkinConfigSO : BaseConfigSO
    {
    }

    /// <summary>
    /// 波次配置（占位，后续在 Wave 模块实现）
    /// </summary>
    public class WaveConfigSO : BaseConfigSO
    {
    }

    #endregion
}
