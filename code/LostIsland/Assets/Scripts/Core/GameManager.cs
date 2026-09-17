// ============================================================
// GameManager.cs
// 游戏全局管理器
// 负责游戏状态切换、场景加载、游戏生命周期管理
// ============================================================

using System.Collections;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Core
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Boot = 0,       // 启动初始化
        MainMenu = 1,   // 主菜单
        Tower = 2,      // 塔内（白天）
        Battle = 3,     // 战斗（夜间）
        Loading = 4,    // 场景切换中
        Paused = 5      // 暂停
    }

    /// <summary>
    /// BOSS等级
    /// </summary>
    public enum BossTier
    {
        None = 0,
        Normal = 1,     // 普通BOSS（每20波）
        Milestone = 2   // 里程碑BOSS（第100/200/300波）
    }

    /// <summary>
    /// 丧尸类型
    /// </summary>
    public enum ZombieType
    {
        Normal = 0,     // 普通丧尸
        Fast = 1,       // 快速丧尸
        Heavy = 2,      // 重甲丧尸
        Exploder = 3,   // 自爆丧尸
        Poison = 4,     // 毒液丧尸（精英）
        Shield = 5,     // 护盾丧尸（精英）
        Summoner = 6,   // 召唤丧尸（精英）
        Charger = 7,    // 冲锋丧尸（精英）
        Healer = 8,     // 治疗丧尸（精英）
        Stealth = 9,    // 隐形丧尸（精英）
        Boss = 10       // BOSS
    }

    /// <summary>
    /// 资源类型
    /// </summary>
    public enum ResourceType
    {
        Scrap = 0,      // 废料
        Food = 1,       // 食物
        Diamond = 2,    // 钻石
        Coupon = 3,     // 点券
        Crystal = 4,    // 晶核
        Flesh = 5       // 腐肉（战斗内临时）
    }

    /// <summary>
    /// 品质/稀有度
    /// </summary>
    public enum Rarity
    {
        Common = 0,     // 白色 - 普通
        Rare = 1,       // 蓝色 - 稀有
        Epic = 2,       // 紫色 - 史诗
        Legendary = 3   // 金色 - 传说
    }

    /// <summary>
    /// 游戏全局管理器
    /// </summary>
    public class GameManager : MonoSingleton<GameManager>
    {
        [Header("游戏设置")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _runInBackground = false;

        public GameState CurrentState { get; private set; } = GameState.Boot;
        private GameState _previousState = GameState.Boot;
        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        protected override void OnInit()
        {
            base.OnInit();

            // 应用设置
            Application.targetFrameRate = _targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.runInBackground = _runInBackground;

            // 质量设置（可根据设备动态调整）
            QualitySettings.vSyncCount = 0;

            Debug.Log("[GameManager] Initialized.");
        }

        private void Start()
        {
            // 启动后进入主菜单
            ChangeState(GameState.MainMenu);
        }

        /// <summary>
        /// 切换游戏状态
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (newState == CurrentState) return;

            _previousState = CurrentState;
            GameState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameManager] State changed: {oldState} -> {newState}");

            // 触发事件
            EventBus.Trigger(new GameStateChangedEvent
            {
                PreviousState = oldState,
                NewState = newState
            });

            // 状态进入逻辑
            OnStateEnter(newState, oldState);
        }

        /// <summary>
        /// 状态进入处理
        /// </summary>
        private void OnStateEnter(GameState newState, GameState oldState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Tower:
                    Time.timeScale = 1f;
                    break;
                case GameState.Battle:
                    Time.timeScale = 1f;
                    break;
                case GameState.Loading:
                    // 加载场景时不暂停（保持加载动画）
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (_isPaused) return;

            _isPaused = true;
            Time.timeScale = 0f;

            EventBus.Trigger(new GamePausedEvent { IsPaused = true });
            Debug.Log("[GameManager] Game paused.");
        }

        /// <summary>
        /// 继续游戏
        /// </summary>
        public void ResumeGame()
        {
            if (!_isPaused) return;

            _isPaused = false;
            Time.timeScale = 1f;

            EventBus.Trigger(new GamePausedEvent { IsPaused = false });
            Debug.Log("[GameManager] Game resumed.");
        }

        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game...");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>
        /// 设置画质等级
        /// </summary>
        public void SetQualityLevel(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            Debug.Log($"[GameManager] Quality level set to {QualitySettings.names[level]}");
        }

        /// <summary>
        /// 设置目标帧率
        /// </summary>
        public void SetTargetFrameRate(int fps)
        {
            Application.targetFrameRate = fps;
            _targetFrameRate = fps;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // 应用进入后台/前台时的处理
            Debug.Log($"[GameManager] Application pause: {pauseStatus}");
        }

        private void OnApplicationFocus(bool focusStatus)
        {
            // 应用获得/失去焦点时的处理
        }
    }
}
