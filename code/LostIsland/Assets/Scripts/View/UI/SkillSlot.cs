using UnityEngine;
using UnityEngine.UI;
using LostIsland.Core;

namespace LostIsland.View.UI
{
    /// <summary>
    /// 技能槽位：单个技能按钮的UI逻辑
    /// 管理技能图标、冷却显示、可用状态等
    /// </summary>
    public class SkillSlot : MonoBehaviour
    {
        [Header("UI元素")]
        [SerializeField] private Image _skillIcon;
        [SerializeField] private Image _cooldownMask;  // 冷却遮罩（fill amount）
        [SerializeField] private Text _cooldownText;    // 冷却时间文字
        [SerializeField] private Button _skillButton;
        [SerializeField] private GameObject _lockedMask; // 锁定遮罩
        [SerializeField] private Text _levelText;       // 技能等级

        [Header("技能配置")]
        [SerializeField] private int _skillId;
        [SerializeField] private bool _isUnlocked = true;
        [SerializeField] private int _level = 1;

        private float _currentCooldown = 0f;
        private float _maxCooldown = 10f;
        private bool _isCooling = false;

        /// <summary>
        /// 技能ID
        /// </summary>
        public int SkillId => _skillId;

        /// <summary>
        /// 是否冷却中
        /// </summary>
        public bool IsCooling => _isCooling;

        /// <summary>
        /// 是否解锁
        /// </summary>
        public bool IsUnlocked => _isUnlocked;

        /// <summary>
        /// 技能等级
        /// </summary>
        public int Level => _level;

        #region 初始化

        private void Awake()
        {
            if (_skillButton != null)
                _skillButton.onClick.AddListener(OnSkillButtonClicked);
        }

        private void Start()
        {
            UpdateUI();
        }

        /// <summary>
        /// 设置技能
        /// </summary>
        public void SetSkill(int skillId, float cooldown, Sprite icon = null)
        {
            _skillId = skillId;
            _maxCooldown = cooldown;

            if (_skillIcon != null && icon != null)
                _skillIcon.sprite = icon;

            _isCooling = false;
            _currentCooldown = 0f;
            UpdateUI();
        }

        /// <summary>
        /// 设置解锁状态
        /// </summary>
        public void SetUnlocked(bool unlocked)
        {
            _isUnlocked = unlocked;
            UpdateUI();
        }

        /// <summary>
        /// 设置等级
        /// </summary>
        public void SetLevel(int level)
        {
            _level = Mathf.Max(1, level);
            UpdateUI();
        }

        #endregion

        #region 冷却逻辑

        /// <summary>
        /// 开始冷却
        /// </summary>
        public void StartCooldown()
        {
            if (!_isUnlocked) return;

            _isCooling = true;
            _currentCooldown = _maxCooldown;
            UpdateUI();
        }

        /// <summary>
        /// 立即结束冷却
        /// </summary>
        public void ResetCooldown()
        {
            _isCooling = false;
            _currentCooldown = 0f;
            UpdateUI();
        }

        /// <summary>
        /// 设置冷却时间
        /// </summary>
        public void SetCooldown(float cooldown)
        {
            _maxCooldown = Mathf.Max(0.1f, cooldown);
        }

        #endregion

        #region UI更新

        private void UpdateUI()
        {
            // 按钮交互状态
            if (_skillButton != null)
                _skillButton.interactable = _isUnlocked && !_isCooling;

            // 冷却遮罩
            if (_cooldownMask != null)
            {
                _cooldownMask.fillAmount = _isCooling ? (_currentCooldown / _maxCooldown) : 0f;
            }

            // 冷却文字
            if (_cooldownText != null)
            {
                _cooldownText.text = _isCooling && _currentCooldown > 0.1f ? $"{_currentCooldown:F1}" : "";
            }

            // 锁定遮罩
            if (_lockedMask != null)
                _lockedMask.SetActive(!_isUnlocked);

            // 等级文字
            if (_levelText != null)
                _levelText.text = _isUnlocked ? $"Lv.{_level}" : "";
        }

        #endregion

        #region 按钮回调

        private void OnSkillButtonClicked()
        {
            if (!_isUnlocked || _isCooling) return;

            // 触发技能释放事件
            EventBus.Trigger(new SkillUsedEvent
            {
                SkillId = _skillId,
                Level = _level
            });

            // 开始冷却
            StartCooldown();

            Debug.Log($"[SkillSlot] 释放技能 {_skillId}, Lv.{_level}");
        }

        #endregion

        #region 每帧更新

        private void Update()
        {
            if (!_isCooling) return;

            _currentCooldown -= Time.deltaTime;
            if (_currentCooldown <= 0f)
            {
                _currentCooldown = 0f;
                _isCooling = false;
            }

            UpdateUI();
        }

        #endregion
    }

    /// <summary>
    /// 技能使用事件
    /// </summary>
    public struct SkillUsedEvent : IEvent
    {
        public int SkillId;
        public int Level;
    }
}
