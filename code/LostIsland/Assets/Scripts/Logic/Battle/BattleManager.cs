using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;
using LostIsland.Entity;

namespace LostIsland.Logic.Battle
{
    /// <summary>
    /// 战斗状态枚举
    /// </summary>
    public enum BattleState
    {
        None = 0,       // 未开始
        Prepare = 1,    // 准备阶段（白天）
        PreNight = 2,   // 入夜倒计时
        WaveStart = 3,  // 波次开始
        WaveActive = 4, // 波次进行中
        Victory = 5,    // 胜利
        Defeat = 6,     // 失败
    }

    /// <summary>
    /// BOSS 难度等级
    /// </summary>
    public enum BossTier
    {
        Normal = 0,     // 普通BOSS
        Elite = 1,      // 精英BOSS
        Lord = 2,       // 领主BOSS
    }

    /// <summary>
    /// 战斗管理器：战斗场景的核心管理器
    /// 维护战斗状态机、波次计时、胜负判定、腐肉资源
    /// </summary>
    public class BattleManager : MonoSingleton<BattleManager>
    {
        #region 战斗配置

        [Header("波次配置")]
        [Tooltip("总波数")]
        [SerializeField] private int _totalWaves = 300;

        [Tooltip("初始波次间隔（秒）")]
        [SerializeField] private float _waveIntervalStart = 30f;

        [Tooltip("最终波次间隔（秒）")]
        [SerializeField] private float _waveIntervalEnd = 15f;

        [Tooltip("入夜倒计时（秒）")]
        [SerializeField] private float _preNightDuration = 3f;

        [Header("天数配置")]
        [Tooltip("当前天数")]
        [SerializeField] private int _currentDay = 1;

        #endregion

        #region 战斗状态

        /// <summary>
        /// 当前战斗状态
        /// </summary>
        public BattleState CurrentState { get; private set; } = BattleState.None;

        /// <summary>
        /// 当前波数（从1开始）
        /// </summary>
        public int CurrentWave { get; private set; } = 0;

        /// <summary>
        /// 波次计时器
        /// </summary>
        public float WaveTimer { get; private set; } = 0f;

        /// <summary>
        /// 下一波倒计时
        /// </summary>
        public float NextWaveCountdown { get; private set; } = 0f;

        /// <summary>
        /// 战斗是否暂停
        /// </summary>
        public bool IsPaused { get; private set; } = false;

        /// <summary>
        /// 战斗总时长（秒）
        /// </summary>
        public float BattleDuration { get; private set; } = 0f;

        #endregion

        #region 腐肉资源

        /// <summary>
        /// 当前腐肉数量
        /// </summary>
        public int CurrentFlesh { get; private set; } = 0;

        #endregion

        #region 实体引用

        /// <summary>
        /// 玩家实体
        /// </summary>
        public EntityBase Player { get; private set; }

        /// <summary>
        /// 灯塔实体
        /// </summary>
        public EntityBase Tower { get; private set; }

        /// <summary>
        /// 当前存活的丧尸列表
        /// </summary>
        private List<EntityBase> _aliveZombies = new List<EntityBase>();

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();
            CurrentState = BattleState.None;
        }

        /// <summary>
        /// 初始化战斗
        /// </summary>
        /// <param name="day">天数</param>
        /// <param name="player">玩家实体</param>
        /// <param name="tower">灯塔实体</param>
        public void InitBattle(int day, EntityBase player, EntityBase tower)
        {
            _currentDay = day;
            Player = player;
            Tower = tower;
            CurrentFlesh = 0;
            CurrentWave = 0;
            BattleDuration = 0f;
            _aliveZombies.Clear();

            // 监听玩家和灯塔死亡
            if (player?.Health != null)
            {
                player.Health.OnDeath += OnPlayerDeath;
            }
            if (tower?.Health != null)
            {
                tower.Health.OnDeath += OnTowerDeath;
            }

            // 进入准备状态
            ChangeState(BattleState.Prepare);

            Debug.Log($"[BattleManager] 战斗初始化：第{day}天");
        }

        #endregion

        #region 状态机

        /// <summary>
        /// 切换战斗状态
        /// </summary>
        public void ChangeState(BattleState newState)
        {
            if (newState == CurrentState) return;

            BattleState oldState = CurrentState;

            // 退出旧状态
            OnStateExit(oldState);

            CurrentState = newState;
            Debug.Log($"[BattleManager] 状态变更: {oldState} -> {newState}");

            // 触发事件
            EventBus.Trigger(new BattleStateChangedEvent
            {
                PreviousState = oldState,
                NewState = newState
            });

            // 进入新状态
            OnStateEnter(newState, oldState);
        }

        /// <summary>
        /// 状态进入
        /// </summary>
        private void OnStateEnter(BattleState state, BattleState prevState)
        {
            switch (state)
            {
                case BattleState.Prepare:
                    // 准备阶段，等待玩家点击入夜
                    break;

                case BattleState.PreNight:
                    // 入夜倒计时
                    NextWaveCountdown = _preNightDuration;
                    break;

                case BattleState.WaveStart:
                    // 波次开始：生成丧尸（由WaveManager处理）
                    CurrentWave++;
                    WaveTimer = 0f;

                    EventBus.Trigger(new WaveStartEvent
                    {
                        WaveNumber = CurrentWave,
                        TotalWaves = _totalWaves
                    });

                    // 立即进入活跃状态
                    ChangeState(BattleState.WaveActive);
                    break;

                case BattleState.WaveActive:
                    // 波次进行中
                    break;

                case BattleState.Victory:
                    // 胜利
                    EventBus.Trigger(new BattleVictoryEvent
                    {
                        TotalWavesCleared = CurrentWave,
                        DayNumber = _currentDay
                    });
                    Debug.Log($"[BattleManager] 胜利！共通过{CurrentWave}波");
                    break;

                case BattleState.Defeat:
                    // 失败
                    EventBus.Trigger(new BattleDefeatEvent
                    {
                        WaveReached = CurrentWave,
                        DayNumber = _currentDay
                    });
                    Debug.Log($"[BattleManager] 失败！坚持到第{CurrentWave}波");
                    break;
            }
        }

        /// <summary>
        /// 状态退出
        /// </summary>
        private void OnStateExit(BattleState state)
        {
            switch (state)
            {
                case BattleState.Prepare:
                    break;
                case BattleState.PreNight:
                    break;
                case BattleState.WaveActive:
                    break;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始夜间战斗（玩家点击入夜按钮）
        /// </summary>
        public void StartNight()
        {
            if (CurrentState == BattleState.Prepare)
            {
                ChangeState(BattleState.PreNight);
            }
        }

        /// <summary>
        /// 暂停/继续战斗
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        /// <summary>
        /// 设置暂停状态
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;

            IsPaused = paused;
            EventBus.Trigger(new GamePausedEvent { IsPaused = paused });
            Debug.Log($"[BattleManager] 战斗{(paused ? "暂停" : "继续")}");
        }

        /// <summary>
        /// 添加腐肉
        /// </summary>
        public void AddFlesh(int amount)
        {
            if (amount <= 0) return;

            int oldValue = CurrentFlesh;
            CurrentFlesh += amount;

            EventBus.Trigger(new FleshChangedEvent
            {
                CurrentFlesh = CurrentFlesh,
                Delta = amount
            });
        }

        /// <summary>
        /// 消耗腐肉
        /// </summary>
        /// <returns>是否足够</returns>
        public bool SpendFlesh(int amount)
        {
            if (amount <= 0) return true;
            if (CurrentFlesh < amount) return false;

            int oldValue = CurrentFlesh;
            CurrentFlesh -= amount;

            EventBus.Trigger(new FleshChangedEvent
            {
                CurrentFlesh = CurrentFlesh,
                Delta = -amount
            });

            return true;
        }

        /// <summary>
        /// 注册丧尸
        /// </summary>
        public void RegisterZombie(EntityBase zombie)
        {
            if (zombie == null) return;
            if (!_aliveZombies.Contains(zombie))
            {
                _aliveZombies.Add(zombie);
                zombie.Health.OnDeath += () => OnZombieDeath(zombie);
            }
        }

        /// <summary>
        /// 获取当前存活丧尸数量
        /// </summary>
        public int GetAliveZombieCount()
        {
            return _aliveZombies.Count;
        }

        /// <summary>
        /// 获取波次间隔（根据波数递减）
        /// </summary>
        public float GetWaveInterval(int wave)
        {
            // 线性递减：从 _waveIntervalStart 到 _waveIntervalEnd
            float t = Mathf.Clamp01((float)(wave - 1) / (_totalWaves - 1));
            return Mathf.Lerp(_waveIntervalStart, _waveIntervalEnd, t);
        }

        /// <summary>
        /// 检查是否是BOSS波
        /// </summary>
        public bool IsBossWave(int wave, out BossTier tier)
        {
            tier = BossTier.Normal;

            // 每20波一个BOSS
            if (wave % 20 != 0) return false;

            int bossIndex = wave / 20;
            if (bossIndex % 5 == 0)
            {
                tier = BossTier.Lord;  // 第100、200、300波：领主
            }
            else if (bossIndex % 2 == 0)
            {
                tier = BossTier.Elite; // 第40、60、80...波：精英
            }
            else
            {
                tier = BossTier.Normal; // 第20波：普通
            }

            return true;
        }

        #endregion

        #region 胜负判定

        /// <summary>
        /// 检查胜利条件
        /// </summary>
        private bool CheckVictory()
        {
            // 所有波次完成且没有存活丧尸
            if (CurrentWave >= _totalWaves && _aliveZombies.Count == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查失败条件
        /// </summary>
        private bool CheckDefeat()
        {
            // 灯塔HP归零
            if (Tower != null && Tower.Health != null && Tower.Health.IsDead)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 玩家死亡
        /// </summary>
        private void OnPlayerDeath()
        {
            Debug.Log("[BattleManager] 玩家死亡");
            // 玩家死亡不直接失败，可以复活
        }

        /// <summary>
        /// 灯塔死亡 = 失败
        /// </summary>
        private void OnTowerDeath()
        {
            Debug.Log("[BattleManager] 灯塔被摧毁");
            if (CurrentState == BattleState.WaveActive || CurrentState == BattleState.WaveStart)
            {
                ChangeState(BattleState.Defeat);
            }
        }

        /// <summary>
        /// 丧尸死亡
        /// </summary>
        private void OnZombieDeath(EntityBase zombie)
        {
            if (_aliveZombies.Contains(zombie))
            {
                _aliveZombies.Remove(zombie);

                // 检查是否波次清空
                if (CurrentState == BattleState.WaveActive && _aliveZombies.Count == 0)
                {
                    OnWaveCleared();
                }
            }
        }

        /// <summary>
        /// 波次清空
        /// </summary>
        private void OnWaveCleared()
        {
            EventBus.Trigger(new WaveClearedEvent
            {
                WaveNumber = CurrentWave
            });

            // 检查胜利
            if (CheckVictory())
            {
                ChangeState(BattleState.Victory);
                return;
            }

            // 还有下一波，进入下一波倒计时
            if (CurrentWave < _totalWaves)
            {
                NextWaveCountdown = GetWaveInterval(CurrentWave + 1);
            }
        }

        #endregion

        #region 更新循环

        private void Update()
        {
            if (IsPaused) return;
            if (CurrentState == BattleState.None) return;

            float dt = Time.deltaTime;

            switch (CurrentState)
            {
                case BattleState.PreNight:
                    UpdatePreNight(dt);
                    break;

                case BattleState.WaveActive:
                    UpdateWaveActive(dt);
                    break;
            }

            // 累计战斗时长
            if (CurrentState >= BattleState.PreNight && CurrentState <= BattleState.WaveActive)
            {
                BattleDuration += dt;
            }
        }

        /// <summary>
        /// 入夜倒计时更新
        /// </summary>
        private void UpdatePreNight(float dt)
        {
            NextWaveCountdown -= dt;
            if (NextWaveCountdown <= 0f)
            {
                // 进入第一波
                ChangeState(BattleState.WaveStart);
            }
        }

        /// <summary>
        /// 波次进行中更新
        /// </summary>
        private void UpdateWaveActive(float dt)
        {
            WaveTimer += dt;

            // 如果当前波次的丧尸已全部清空，且还有下一波
            if (_aliveZombies.Count == 0 && NextWaveCountdown > 0f)
            {
                NextWaveCountdown -= dt;
                if (NextWaveCountdown <= 0f && CurrentWave < _totalWaves)
                {
                    // 下一波开始
                    ChangeState(BattleState.WaveStart);
                }
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理战斗
        /// </summary>
        public void CleanupBattle()
        {
            if (Player?.Health != null)
            {
                Player.Health.OnDeath -= OnPlayerDeath;
            }
            if (Tower?.Health != null)
            {
                Tower.Health.OnDeath -= OnTowerDeath;
            }

            _aliveZombies.Clear();
            Player = null;
            Tower = null;

            ChangeState(BattleState.None);
        }

        protected override void OnDispose()
        {
            CleanupBattle();
            base.OnDispose();
        }

        #endregion
    }
}
