using System;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Logic.Awakening
{
    /// <summary>
    /// DDDR (Dynamic Difficulty Dynamic Regulation) 动态难度调节系统
    /// 在战斗中实时检测玩家状态，渐进式调整难度
    /// </summary>
    public class DDDRSystem : MonoSingleton<DDDRSystem>
    {
        #region 配置

        /// <summary>检测间隔（秒）</summary>
        public const float CHECK_INTERVAL = 0.5f;

        /// <summary>平滑过渡速度</summary>
        public const float SMOOTH_SPEED = 2f;

        /// <summary>最大调节幅度</summary>
        public const float MAX_ADJUSTMENT = 0.50f;  // 最多±50%

        /// <summary>最小调节幅度</summary>
        public const float MIN_ADJUSTMENT = -0.40f; // 最多-40%

        // 血量阈值
        public const float LOW_HP_THRESHOLD = 0.30f;    // 低血量 30%
        public const float CRITICAL_HP_THRESHOLD = 0.15f; // 危险血量 15%

        // 调整步长
        public const float STEP_HP_CRITICAL = 0.05f;    // 危险血量每次调整
        public const float STEP_HP_LOW = 0.02f;         // 低血量每次调整
        public const float STEP_KILL_FAST = -0.02f;     // 击杀过快每次调整
        public const float STEP_ZOMBIE_MANY = 0.03f;    // 丧尸过多每次调整

        #endregion

        #region 状态

        /// <summary>检测计时器</summary>
        private float _checkTimer = 0f;

        /// <summary>是否运行中</summary>
        private bool _isRunning = false;

        /// <summary>当前波次</summary>
        private int _currentWave = 0;

        // 目标调节值
        private float _targetSpawnRateMult = 1f;
        private float _targetZombieAtkMult = 1f;
        private float _targetZombieSpeedMult = 1f;

        // 当前实际值（平滑过渡）
        private float _currentSpawnRateMult = 1f;
        private float _currentZombieAtkMult = 1f;
        private float _currentZombieSpeedMult = 1f;

        // 统计数据
        private int _killsInLastWindow = 0;
        private float _windowTimer = 0f;
        private const float KILL_WINDOW = 5f; // 5秒窗口

        #endregion

        #region 公共属性

        /// <summary>当前生成速率倍率</summary>
        public float SpawnRateMultiplier => _currentSpawnRateMult;

        /// <summary>当前丧尸攻击倍率</summary>
        public float ZombieAtkMultiplier => _currentZombieAtkMult;

        /// <summary>当前丧尸速度倍率</summary>
        public float ZombieSpeedMultiplier => _currentZombieSpeedMult;

        /// <summary>是否运行中</summary>
        public bool IsRunning => _isRunning;

        #endregion

        #region 事件

        /// <summary>难度变化事件</summary>
        public event Action<float, float, float> OnDifficultyChanged; // spawnMult, atkMult, speedMult

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            ResetDifficulty();

            Debug.Log("[DDDRSystem] DDDR动态难度系统初始化完成");
        }

        /// <summary>
        /// 重置难度
        /// </summary>
        public void ResetDifficulty()
        {
            _targetSpawnRateMult = 1f;
            _targetZombieAtkMult = 1f;
            _targetZombieSpeedMult = 1f;

            _currentSpawnRateMult = 1f;
            _currentZombieAtkMult = 1f;
            _currentZombieSpeedMult = 1f;

            _killsInLastWindow = 0;
            _windowTimer = 0f;
            _checkTimer = 0f;
        }

        #endregion

        #region 战斗控制

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle(int wave)
        {
            _isRunning = true;
            _currentWave = wave;
            ResetDifficulty();

            Debug.Log($"[DDDRSystem] 战斗开始，波次={wave}，DDDR启动");
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndBattle(bool victory)
        {
            _isRunning = false;

            Debug.Log($"[DDDRSystem] 战斗结束（{(victory ? "胜利" : "失败")}），最终难度: 生成{SpawnRateMultiplier:F2}x 攻击{ZombieAtkMultiplier:F2}x 速度{ZombieSpeedMultiplier:F2}x");

            EventBus.Trigger(new DDDRBattleEndedEvent
            {
                Victory = victory,
                FinalSpawnMult = _currentSpawnRateMult,
                FinalAtkMult = _currentZombieAtkMult,
                FinalSpeedMult = _currentZombieSpeedMult
            });
        }

        /// <summary>
        /// 报告击杀
        /// </summary>
        public void ReportKill()
        {
            _killsInLastWindow++;
        }

        #endregion

        #region 更新

        private void Update()
        {
            if (!_isRunning) return;

            float dt = Time.deltaTime;

            // 平滑过渡到目标值
            _currentSpawnRateMult = Mathf.Lerp(_currentSpawnRateMult, _targetSpawnRateMult, SMOOTH_SPEED * dt);
            _currentZombieAtkMult = Mathf.Lerp(_currentZombieAtkMult, _targetZombieAtkMult, SMOOTH_SPEED * dt);
            _currentZombieSpeedMult = Mathf.Lerp(_currentZombieSpeedMult, _targetZombieSpeedMult, SMOOTH_SPEED * dt);

            // 击杀窗口计时
            _windowTimer += dt;
            if (_windowTimer >= KILL_WINDOW)
            {
                _windowTimer = 0f;
                // 每5秒重置击杀计数（已用于检测，重置）
                _killsInLastWindow = 0;
            }

            // 定期检测
            _checkTimer += dt;
            if (_checkTimer >= CHECK_INTERVAL)
            {
                _checkTimer = 0f;
                RunDifficultyCheck();
            }
        }

        /// <summary>
        /// 执行难度检测
        /// </summary>
        private void RunDifficultyCheck()
        {
            bool changed = false;

            // 1. 玩家血量检测（降低难度）
            float hpRatio = GetPlayerHpRatio();

            if (hpRatio <= CRITICAL_HP_THRESHOLD)
            {
                // 危险血量：降低丧尸攻击和速度
                AdjustTarget(ref _targetZombieAtkMult, -STEP_HP_CRITICAL);
                AdjustTarget(ref _targetZombieSpeedMult, -STEP_HP_CRITICAL * 0.5f);
                changed = true;
            }
            else if (hpRatio <= LOW_HP_THRESHOLD)
            {
                // 低血量：小幅降低
                AdjustTarget(ref _targetZombieAtkMult, -STEP_HP_LOW);
                changed = true;
            }

            // 2. 击杀速度检测（提高难度）
            if (_killsInLastWindow >= 15) // 5秒内杀15只以上算很快
            {
                AdjustTarget(ref _targetSpawnRateMult, -STEP_KILL_FAST); // 注意：击杀快=降低生成间隔=提高难度
                AdjustTarget(ref _targetZombieAtkMult, -STEP_KILL_FAST * 0.5f);
                changed = true;
            }

            // 3. 场上丧尸数量检测
            int zombieCount = GetActiveZombieCount();
            if (zombieCount >= 30)
            {
                // 丧尸太多：降低生成速率
                AdjustTarget(ref _targetSpawnRateMult, STEP_ZOMBIE_MANY);
                changed = true;
            }
            else if (zombieCount <= 5 && _killsInLastWindow < 3)
            {
                // 丧尸太少：提高生成速率
                AdjustTarget(ref _targetSpawnRateMult, -STEP_ZOMBIE_MANY * 0.5f);
                changed = true;
            }

            // 钳制目标值
            _targetSpawnRateMult = Mathf.Clamp(_targetSpawnRateMult, 1f + MIN_ADJUSTMENT, 1f + MAX_ADJUSTMENT);
            _targetZombieAtkMult = Mathf.Clamp(_targetZombieAtkMult, 1f + MIN_ADJUSTMENT, 1f + MAX_ADJUSTMENT);
            _targetZombieSpeedMult = Mathf.Clamp(_targetZombieSpeedMult, 1f + MIN_ADJUSTMENT * 0.5f, 1f + MAX_ADJUSTMENT * 0.5f);

            if (changed)
            {
                OnDifficultyChanged?.Invoke(_currentSpawnRateMult, _currentZombieAtkMult, _currentZombieSpeedMult);

                EventBus.Trigger(new DDDRDifficultyChangedEvent
                {
                    SpawnRateMult = _targetSpawnRateMult,
                    ZombieAtkMult = _targetZombieAtkMult,
                    ZombieSpeedMult = _targetZombieSpeedMult
                });
            }
        }

        /// <summary>
        /// 调整目标值
        /// </summary>
        private void AdjustTarget(ref float target, float delta)
        {
            target += delta;
        }

        #endregion

        #region 辅助方法（从其他系统获取数据）

        /// <summary>
        /// 获取玩家血量比例
        /// </summary>
        private float GetPlayerHpRatio()
        {
            // 实际项目中通过 PlayerManager 获取
            // 这里返回默认值，战斗时会被实际数据覆盖
            return 0.5f;
        }

        /// <summary>
        /// 获取场上活跃丧尸数
        /// </summary>
        private int GetActiveZombieCount()
        {
            // 实际项目中通过 ZombieSpawner 获取
            var spawner = Wave.ZombieSpawner.Instance;
            if (spawner != null)
                return spawner.ActiveZombieCount;
            return 0;
        }

        #endregion

        #region 手动调节（供外部调用）

        /// <summary>
        /// 手动设置难度倍率
        /// </summary>
        public void SetDifficulty(float spawnMult, float atkMult, float speedMult)
        {
            _targetSpawnRateMult = spawnMult;
            _targetZombieAtkMult = atkMult;
            _targetZombieSpeedMult = speedMult;
        }

        /// <summary>
        /// 偏移难度（增加/减少）
        /// </summary>
        public void OffsetDifficulty(float spawnOffset, float atkOffset, float speedOffset)
        {
            _targetSpawnRateMult += spawnOffset;
            _targetZombieAtkMult += atkOffset;
            _targetZombieSpeedMult += speedOffset;

            // 钳制
            _targetSpawnRateMult = Mathf.Clamp(_targetSpawnRateMult, 1f + MIN_ADJUSTMENT, 1f + MAX_ADJUSTMENT);
            _targetZombieAtkMult = Mathf.Clamp(_targetZombieAtkMult, 1f + MIN_ADJUSTMENT, 1f + MAX_ADJUSTMENT);
            _targetZombieSpeedMult = Mathf.Clamp(_targetZombieSpeedMult, 1f + MIN_ADJUSTMENT * 0.5f, 1f + MAX_ADJUSTMENT * 0.5f);
        }

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            OnDifficultyChanged = null;
            _isRunning = false;
            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// DDDR难度变化事件
    /// </summary>
    public struct DDDRDifficultyChangedEvent : IEvent
    {
        public float SpawnRateMult;
        public float ZombieAtkMult;
        public float ZombieSpeedMult;
    }

    /// <summary>
    /// DDDR战斗结束事件
    /// </summary>
    public struct DDDRBattleEndedEvent : IEvent
    {
        public bool Victory;
        public float FinalSpawnMult;
        public float FinalAtkMult;
        public float FinalSpeedMult;
    }
}
