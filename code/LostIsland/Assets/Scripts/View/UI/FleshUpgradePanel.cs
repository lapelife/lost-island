using UnityEngine;
using UnityEngine.UI;
using LostIsland.Core;
using LostIsland.Logic.Battle;

namespace LostIsland.View.UI
{
    /// <summary>
    /// 腐肉升级面板UI
    /// 显示各升级类型的等级、消耗、效果，提供升级按钮
    /// </summary>
    public class FleshUpgradePanel : MonoBehaviour
    {
        [Header("腐肉显示")]
        [SerializeField] private Text _fleshText;

        [Header("升级项")]
        [SerializeField] private FleshUpgradeItem[] _upgradeItems;

        [Header("关闭按钮")]
        [SerializeField] private Button _closeButton;

        /// <summary>
        /// 面板是否打开
        /// </summary>
        public bool IsOpen => gameObject.activeSelf;

        #region 初始化

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            // 订阅腐肉变化事件
            EventBus.Subscribe<FleshChangedEvent>(OnFleshChanged);
            UpdateFleshDisplay();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FleshChangedEvent>(OnFleshChanged);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 打开面板
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            UpdateAllItems();
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 切换面板
        /// </summary>
        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        /// 更新所有升级项显示
        /// </summary>
        public void UpdateAllItems()
        {
            if (_upgradeItems == null) return;

            foreach (var item in _upgradeItems)
            {
                item?.UpdateDisplay();
            }
        }

        #endregion

        #region 事件回调

        private void OnFleshChanged(FleshChangedEvent e)
        {
            UpdateFleshDisplay();
            UpdateAllItems();
        }

        private void UpdateFleshDisplay()
        {
            if (_fleshText != null && BattleManager.Instance != null)
            {
                _fleshText.text = $"腐肉: {BattleManager.Instance.CurrentFlesh}";
            }
        }

        #endregion
    }

    /// <summary>
    /// 单个腐肉升级项UI
    /// </summary>
    public class FleshUpgradeItem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private FleshUpgradeType _upgradeType;

        [Header("UI元素")]
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _effectText;
        [SerializeField] private Text _costText;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Image _icon;

        /// <summary>
        /// 升级类型
        /// </summary>
        public FleshUpgradeType UpgradeType => _upgradeType;

        private void Awake()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void Start()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// 更新显示
        /// </summary>
        public void UpdateDisplay()
        {
            var system = GetUpgradeSystem();
            if (system == null) return;

            int level = system.GetLevel(_upgradeType);
            float pct = system.GetUpgradePct(_upgradeType);
            int cost = system.GetUpgradeCost(_upgradeType);
            int currentFlesh = BattleManager.Instance != null ? BattleManager.Instance.CurrentFlesh : 0;
            bool canUpgrade = system.CanUpgrade(_upgradeType, currentFlesh);

            if (_nameText != null)
                _nameText.text = FleshUpgradeSystem.GetUpgradeName(_upgradeType);

            if (_levelText != null)
                _levelText.text = level >= FleshUpgradeSystem.MAX_LEVEL ? "MAX" : $"Lv.{level}";

            if (_effectText != null)
                _effectText.text = $"+{pct:P0}";

            if (_costText != null)
            {
                _costText.text = level >= FleshUpgradeSystem.MAX_LEVEL ? "满级" : cost.ToString();
            }

            if (_upgradeButton != null)
                _upgradeButton.interactable = canUpgrade;
        }

        /// <summary>
        /// 获取升级系统引用
        /// </summary>
        private FleshUpgradeSystem GetUpgradeSystem()
        {
            // 实际项目中从BattleManager或其他地方获取
            return null;
        }

        /// <summary>
        /// 升级按钮点击
        /// </summary>
        private void OnUpgradeClicked()
        {
            // 实际升级逻辑由控制器处理
            // 这里发送事件
            EventBus.Trigger(new FleshUpgradeRequestEvent
            {
                UpgradeType = _upgradeType
            });

            UpdateDisplay();
        }
    }

    /// <summary>
    /// 腐肉升级请求事件
    /// </summary>
    public struct FleshUpgradeRequestEvent : IEvent
    {
        public FleshUpgradeType UpgradeType;
    }
}
