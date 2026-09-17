using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Data.Config
{
    /// <summary>
    /// 配置数据库：统一管理所有配置表，支持按ID查找
    /// 游戏启动时由 ConfigLoader 填充数据
    /// </summary>
    public static class ConfigDatabase
    {
        // 按类型存储的配置表字典
        private static Dictionary<Type, Dictionary<string, BaseConfigSO>> _configTables
            = new Dictionary<Type, Dictionary<string, BaseConfigSO>>();

        // 是否已初始化
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化配置数据库
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            _configTables.Clear();
            _isInitialized = true;

            Debug.Log("[ConfigDatabase] 配置数据库初始化完成");
        }

        /// <summary>
        /// 注册一张配置表
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="config">配置实例</param>
        public static void RegisterConfig<T>(T config) where T : BaseConfigSO
        {
            if (config == null)
            {
                Debug.LogError("[ConfigDatabase] 注册的配置为空！");
                return;
            }

            Type type = typeof(T);
            if (!_configTables.ContainsKey(type))
            {
                _configTables[type] = new Dictionary<string, BaseConfigSO>();
            }

            if (_configTables[type].ContainsKey(config.Id))
            {
                Debug.LogWarning($"[ConfigDatabase] 重复注册配置: {type.Name} - {config.Id}");
                return;
            }

            _configTables[type][config.Id] = config;
        }

        /// <summary>
        /// 批量注册配置表
        /// </summary>
        public static void RegisterConfigs<T>(IEnumerable<T> configs) where T : BaseConfigSO
        {
            foreach (var config in configs)
            {
                RegisterConfig(config);
            }
        }

        /// <summary>
        /// 根据ID获取配置
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="id">配置ID</param>
        /// <returns>配置实例，找不到返回null</returns>
        public static T GetConfig<T>(string id) where T : BaseConfigSO
        {
            Type type = typeof(T);
            if (_configTables.TryGetValue(type, out var table))
            {
                if (table.TryGetValue(id, out var config))
                {
                    return config as T;
                }
            }

            Debug.LogWarning($"[ConfigDatabase] 未找到配置: {type.Name} - {id}");
            return null;
        }

        /// <summary>
        /// 尝试获取配置
        /// </summary>
        public static bool TryGetConfig<T>(string id, out T config) where T : BaseConfigSO
        {
            config = GetConfig<T>(id);
            return config != null;
        }

        /// <summary>
        /// 获取某类型的所有配置
        /// </summary>
        public static List<T> GetAllConfigs<T>() where T : BaseConfigSO
        {
            Type type = typeof(T);
            List<T> result = new List<T>();

            if (_configTables.TryGetValue(type, out var table))
            {
                foreach (var kvp in table)
                {
                    result.Add(kvp.Value as T);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取某类型配置的数量
        /// </summary>
        public static int GetConfigCount<T>() where T : BaseConfigSO
        {
            Type type = typeof(T);
            if (_configTables.TryGetValue(type, out var table))
            {
                return table.Count;
            }
            return 0;
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        public static bool HasConfig<T>(string id) where T : BaseConfigSO
        {
            Type type = typeof(T);
            if (_configTables.TryGetValue(type, out var table))
            {
                return table.ContainsKey(id);
            }
            return false;
        }

        /// <summary>
        /// 清空所有配置（用于重新加载）
        /// </summary>
        public static void Clear()
        {
            _configTables.Clear();
            _isInitialized = false;
            Debug.Log("[ConfigDatabase] 配置数据库已清空");
        }

        /// <summary>
        /// 输出所有已加载的配置统计
        /// </summary>
        public static void LogStats()
        {
            Debug.Log("[ConfigDatabase] 已加载配置统计:");
            foreach (var kvp in _configTables)
            {
                Debug.Log($"  {kvp.Key.Name}: {kvp.Value.Count} 条");
            }
        }
    }
}
