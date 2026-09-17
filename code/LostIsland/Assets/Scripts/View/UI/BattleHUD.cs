using UnityEngine;
using UnityEngine.UI;
using LostIsland.Core;
using LostIsland.Logic.Battle;

namespace LostIsland.View.UI
{
    /// <summary>
    /// 战斗HUD控制器：管理战斗场景的所有UI元素
    /// 包括：玩家血条、狂暴条、灯塔血条、波次信息、腐肉数量、技能按钮等
    /// 
    /// 注：这是UI逻辑层，实际UI元素在Unity中绑定
    /// 这里提供完整的UI更新逻辑和事件响应
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        #region UI元素（Inspector中绑定）

        [Header("玩家血条")]
        [SerializeField] private Image _playerHpBar;
        [SerializeField] private Text _playerHpText;

        [Header("狂暴条")]
        [SerializeField] private Image _rageBar;
        [SerializeField] private Text _rageText;
        [SerializeField] private Button _rageButton;
        [SerializeField] private GameObject _rageReadyEffect;

        [Header("灯塔血条")]
        [SerializeField] private Image _towerHpBar;
        [SerializeField] private Text _towerHpText;

        [Header("波次信息")]
        [SerializeField] private Text _waveInfoText;      // "第 15 / 300 波"
        [SerializeField] private Text _waveCountdownText; // "下一波: 25s"

        [Header("腐肉数量")]
        [SerializeField] private Text _fleshText;
        [SerializeField] private Button _upgradeButton;

        [Header("技能槽")]
        [SerializeField] private SkillSlot[] _skillSlots; // 4个技能槽

        [Header("暂停")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _pausePanel;

        [Header("战斗结果")]
        [SerializeField] private GameObject _victoryPanel;
        [SerializeField] private GameObject _defeatPanel;
        [SerializeField] private Text _resultWaveText;

        #endregion

        #region 引用

        private BattleManager _battleMgr;
        private PlayerController _playerCtrl;
        private TowerController _towerCtrl;

        #endregion

        #region 初始化

        private void Awake()
        {
            // 按钮事件
            if (_rageButton != null)
                _rageButton.onClick.AddListener(OnRageButtonClicked);

            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);

            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        private void Start()
        {
            // 获取管理器引用
            _battleMgr = BattleManager.Instance;

            // 订阅事件
            SubscribeEvents();

            // 初始化UI
            UpdateAllUI();
        }

        /// <summary>
        /// 设置引用
        /// </summary>
        public void SetReferences(PlayerController player, TowerController tower)
        {
            _playerCtrl = player;
            _towerCtrl = tower;
            UpdateAllUI();
        }

        #endregion

        #region 事件订阅

        private void SubscribeEvents()
        {
            // 玩家HP变化
            EventBus.Subscribe<PlayerHpChangedEvent>(OnPlayerHpChanged);

            // 灯塔HP变化
            EventBus.Subscribe<TowerHpChangedEvent>(OnTowerHpChanged);

            // 狂暴值变化
            EventBus.Subscribe<RageValueChangedEvent>(OnRageValueChanged);
            EventBus.Subscribe<RageModeActivatedEvent>(OnRageModeActivated);
            EventBus.Subscribe<RageModeEndedEvent>(OnRageModeEnded);

            // 波次事件
            EventBus.Subscribe<WaveStartEvent>(OnWaveStart);
            EventBus.Subscribe<WaveClearedEvent>(OnWaveCleared);

            // 腐肉变化
            EventBus.Subscribe<FleshChangedEvent>(OnFleshChanged);

            // 战斗结果
            EventBus.Subscribe<BattleVictoryEvent>(OnBattleVictory);
            EventBus.Subscribe<BattleDefeatEvent>(OnBattleDefeat);
        }

        private void OnDestroy()
        {
            // 取消订阅
            EventBus.Unsubscribe<PlayerHpChangedEvent>(OnPlayerHpChanged);
            EventBus.Unsubscribe<TowerHpChangedEvent>(OnTowerHpChanged);
            EventBus.Unsubscribe<RageValueChangedEvent>(OnRageValueChanged);
            EventBus.Unsubscribe<RageModeActivatedEvent>(OnRageModeActivated);
            EventBus.Unsubscribe<RageModeEndedEvent>(OnRageModeEnded);
            EventBus.Unsubscribe<WaveStartEvent>(OnWaveStart);
            EventBus.Unsubscribe<WaveClearedEvent>(OnWaveCleared);
            EventBus.Unsubscribe<FleshChangedEvent>(OnFleshChanged);
            EventBus.Unsubscribe<BattleVictoryEvent>(OnBattleVictory);
            EventBus.Unsubscribe<BattleDefeatEvent>(OnBattleDefeat);
        }

        #endregion

        #region UI更新

        /// <summary>
        /// 更新所有UI
        /// </summary>
        private void UpdateAllUI()
        {
            UpdatePlayerHP();
            UpdateRage();
            UpdateTowerHP();
            UpdateWaveInfo();
            UpdateFlesh();
        }

        /// <summary>
        /// 更新玩家血条
        /// </summary>
        private void UpdatePlayerHP()
        {
            if (_playerCtrl == null) return;

            float current = _playerCtrl.Entity.Health.CurrentHP;
            float max = _playerCtrl.Entity.Health.MaxHP;
            float pct = _playerCtrl.Entity.Health.HPPct;

            if (_playerHpBar != null)
                _playerHpBar.fillAmount = pct;

            if (_playerHpText != null)
                _playerHpText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }

        /// <summary>
        /// 更新狂暴条
        /// </summary>
        private void UpdateRage()
        {
            if (_playerCtrl == null) return;

            float pct = _playerCtrl.RagePercent;
            bool canActivate = _playerCtrl.CanActivateRage;
            bool isRaging = _playerCtrl.IsRaging;

            if (_rageBar != null)
                _rageBar.fillAmount = pct;

            if (_rageText != null)
            {
                if (isRaging)
                    _rageText.text = $"狂暴中 {_playerCtrl.RageRemainingTime:F1}s";
                else
                    _rageText.text = $"{Mathf.FloorToInt(_playerCtrl.CurrentRage)} / {Mathf.FloorToInt(_playerCtrl.MaxRage)}";
            }

            if (_rageButton != null)
                _rageButton.interactable = canActivate;

            if (_rageReadyEffect != null)
                _rageReadyEffect.SetActive(canActivate);
        }

        /// <summary>
        /// 更新灯塔血条
        /// </summary>
        private void UpdateTowerHP()
        {
            if (_towerCtrl == null) return;

            float current = _towerCtrl.CurrentHP;
            float max = _towerCtrl.MaxHP;
            float pct = _towerCtrl.HPPercent;

            if (_towerHpBar != null)
                _towerHpBar.fillAmount = pct;

            if (_towerHpText != null)
                _towerHpText.text = $"灯塔 {Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
        }

        /// <summary>
        /// 更新波次信息
        /// </summary>
        private void UpdateWaveInfo()
        {
            if (_battleMgr == null) return;

            int current = _battleMgr.CurrentWave;
            int total = 300; // _battleMgr.TotalWaves

            if (_waveInfoText != null)
                _waveInfoText.text = $"第 {current} / {total} 波";

            if (_waveCountdownText != null)
            {
                if (_battleMgr.NextWaveCountdown > 0f && _battleMgr.GetAliveZombieCount() == 0)
                {
                    _waveCountdownText.text = $"下一波: {_battleMgr.NextWaveCountdown:F0}s";
                }
                else
                {
                    _waveCountdownText.text = "";
                }
            }
        }

        /// <summary>
        /// 更新腐肉数量
        /// </summary>
        private void UpdateFlesh()
        {
            if (_battleMgr == null) return;
            if (_fleshText != null)
                _fleshText.text = $"腐肉: {_battleMgr.CurrentFlesh}";
        }

        #endregion

        #region 事件回调

        private void OnPlayerHpChanged(PlayerHpChangedEvent e)
        {
            if (_playerHpBar != null)
                _playerHpBar.fillAmount = e.HpPercent;

            if (_playerHpText != null)
                _playerHpText.text = $"{Mathf.FloorToInt(e.CurrentHp)} / {Mathf.FloorToInt(e.MaxHp)}";
        }

        private void OnTowerHpChanged(TowerHpChangedEvent e)
        {
            if (_towerHpBar != null)
                _towerHpBar.fillAmount = e.HpPercent;

            if (_towerHpText != null)
                _towerHpText.text = $"灯塔 {Mathf.FloorToInt(e.CurrentHp)} / {Mathf.FloorToInt(e.MaxHp)}";
        }

        private void OnRageValueChanged(RageValueChangedEvent e)
        {
            if (_rageBar != null)
                _rageBar.fillAmount = e.RagePercent;

            if (_rageText != null)
                _rageText.text = $"{Mathf.FloorToInt(e.CurrentRage)} / {Mathf.FloorToInt(e.MaxRage)}";

            bool canActivate = e.CurrentRage >= e.MaxRage;
            if (_rageButton != null)
                _rageButton.interactable = canActivate;

            if (_rageReadyEffect != null)
                _rageReadyEffect.SetActive(canActivate);
        }

        private void OnRageModeActivated(RageModeActivatedEvent e)
        {
            if (_rageText != null)
                _rageText.text = $"狂暴中 {e.Duration:F1}s";

            if (_rageReadyEffect != null)
                _rageReadyEffect.SetActive(false);
        }

        private void OnRageModeEnded(RageModeEndedEvent e)
        {
            UpdateRage();
        }

        private void OnWaveStart(WaveStartEvent e)
        {
            if (_waveInfoText != null)
                _waveInfoText.text = $"第 {e.WaveNumber} / {e.TotalWaves} 波";

            if (_waveCountdownText != null)
                _waveCountdownText.text = "";
        }

        private void OnWaveCleared(WaveClearedEvent e)
        {
            // 波次清空时的处理
        }

        private void OnFleshChanged(FleshChangedEvent e)
        {
            if (_fleshText != null)
                _fleshText.text = $"腐肉: {e.CurrentFlesh}";
        }

        private void OnBattleVictory(BattleVictoryEvent e)
        {
            if (_victoryPanel != null)
            {
                _victoryPanel.SetActive(true);
                if (_resultWaveText != null)
                    _resultWaveText.text = $"通关 {e.TotalWavesCleared} 波";
            }
        }

        private void OnBattleDefeat(BattleDefeatEvent e)
        {
            if (_defeatPanel != null)
            {
                _defeatPanel.SetActive(true);
                if (_resultWaveText != null)
                    _resultWaveText.text = $"坚持到第 {e.WaveReached} 波";
            }
        }

        #endregion

        #region 按钮回调

        /// <summary>
        /// 狂暴按钮点击
        /// </summary>
        private void OnRageButtonClicked()
        {
            if (_playerCtrl != null)
            {
                _playerCtrl.ActivateRage();
            }
        }

        /// <summary>
        /// 升级按钮点击
        /// </summary>
        private void OnUpgradeButtonClicked()
        {
            // 打开腐肉升级面板
            // 由UIManager处理
        }

        /// <summary>
        /// 暂停按钮点击
        /// </summary>
        private void OnPauseButtonClicked()
        {
            if (_battleMgr != null)
            {
                _battleMgr.TogglePause();

                if (_pausePanel != null)
                    _pausePanel.SetActive(_battleMgr.IsPaused);
            }
        }

        #endregion

        #region 每帧更新

        private void Update()
        {
            // 更新倒计时显示
            UpdateWaveInfo();

            // 狂暴中更新时间
            if (_playerCtrl != null && _playerCtrl.IsRaging)
            {
                if (_rageText != null)
                    _rageText.text = $"狂暴中 {_playerCtrl.RageRemainingTime:F1}s";
            }
        }

        #endregion
    }
}
