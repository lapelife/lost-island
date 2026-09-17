using UnityEngine;

namespace LostIsland.Data.Config
{
    /// <summary>
    /// 所有配置数据的基类，提供统一的 id、名称、描述、图标字段
    /// 所有 ScriptableObject 配置表都应继承此类
    /// </summary>
    public abstract class BaseConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("配置唯一ID，用于查找")]
        [SerializeField] protected string _id;

        [Tooltip("配置名称（显示用）")]
        [SerializeField] protected string _configName;

        [Tooltip("配置描述")]
        [TextArea(2, 5)]
        [SerializeField] protected string _description;

        [Tooltip("配置图标")]
        [SerializeField] protected Sprite _icon;

        /// <summary>
        /// 配置唯一ID
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// 配置名称
        /// </summary>
        public string ConfigName => _configName;

        /// <summary>
        /// 配置描述
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// 配置图标
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// 配置校验（编辑器下可用）
        /// </summary>
        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                Debug.LogWarning($"[BaseConfigSO] {name} 的 ID 为空！");
            }
        }

        protected virtual void OnValidate()
        {
            Validate();
        }
    }
}
