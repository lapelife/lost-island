using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Logic.Awakening
{
    /// <summary>
    /// 加速类型
    /// </summary>
    public enum SpeedupType
    {
        None = 0,
        ConsecutiveDefeat = 1,   // 连续失败
        WaveStuck = 2,           // 卡关
        DayStuck = 3,            // 卡天数
        NewPlayer = 4,           // 新手保护
        WeekendBoost = 5,        // 周末加成
    }

    /// <summary>
    /// 加速Buff
    /// </summary>
    [Serializable]
    public class SpeedupBuff
    {
        public SpeedupType Type;
        public float AtkMultiplier;      // 攻击倍率（1.2=+20%）
        public float HpMultiplier;       // 生命倍率
        public float Duration;           // 持续时间（秒），-1=永久
        public float RemainingTime;      // 剩余时间

        public SpeedupBuff(SpeedupType type, float atkMult, float hpMult, float duration = -1f)
        {
            Type = type;
            AtkMultiplier = atkMult;
            HpMultiplier = hpMult;
            Duration = duration;
            RemainingTime = duration;
        }

        /// <summary>
        /// 是否永久
        /// </summary>
        public bool IsPermanent => Duration < 0;

        /// <summary>
        /// 是否过期
        /// </summary>
        public bool IsExpired => !IsPermanent && RemainingTime <= 0f;
    }

    /// <summary>
    /// 成长加速管理器
    /// 检测玩家困难并提供适度的属性加成
    /// </summary>
    public class SpeedupManager : MonoSingleton<SpeedupManager>
    {
        #region 配置

        /// <summary>连续失败触发阈值</summary>
        public const int CONSECUTIVE_DEFEAT_THRESHOLD = 3;

        /// <summary>同一波次卡壳阈值</summary>
        public const int SAME_WAVE_STUCK_THRESHOLD = 5;

        /// <summary>同一天数卡壳阈值</summary>
        public const int SAME_DAY_STUCK_THRESHOLD = 3;

        /// <summary>连续失败加成</summary>
        public const float CONSECUTIVE_DEFEAT_BONUS = 0.20f; // +20%全属性

        /// <summary>卡关加成</summary>
        public const float WAVE_STUCK_BONUS = 0.25f; // +25%全属性

        /// <summary>卡天数加成</summary>
        public const float DAY_STUCK_BONUS = 0.15f; // +15%全属性

        /// <summary>新手保护等级</summary>
        public const int NEW_PLAYER_MAX_DAY = 3;

        /// <summary>新手保护加成</summary>
        public const float NEW_PLAYER_BONUS = 0.30f; // +30%全属性

        #endregion

        #region 状态

        /// <summary>连续失败计数</summary>
        private int _consecutiveDefeats = 0;

        /// <summary>上一次通过的波次</summary>
        private int _lastClearedWave = 0;

        /// <summary>同一波次卡壳计数</summary>
        private int _sameWaveStuckCount = 0;

        /// <summary>上一次通过的天数</summary>
        private int _lastClearedDay = 0;

        /// <summary>同一天数卡壳计数</summary>
        private int _sameDayStuckCount = 0;

        /// <summary>当前天数</summary>
        private int _currentDay = 1;

        /// <summary>激活的加速Buff列表</summary>
        private List<SpeedupBuff> _activeBuffs = new List<SpeedupBuff>();

        #endregion

        #region 计算属性

        /// <summary>总攻击加成倍率</summary>
        public float TotalAtkMultiplier
        {
            get
            {
                float mult = 1f;
                foreach (var buff in _activeBuffs)
                {
                    if (!buff.IsExpired)
                        mult *= buff.AtkMultiplier;
                }
                return mult;
            }
        }

        /// <summary>总生命加成倍率</summary>
        public float TotalHpMultiplier
        {
            get
            {
                float mult = 1f;
                foreach (var buff in _activeBuffs)
                {
                    if (!buff.IsExpired)
                        mult *= buff.HpMultiplier;
                }
                return mult;
            }
        }

        /// <summary>激活的Buff数量</summary>
        public int ActiveBuffCount
        {
            get
            {
                int count = 0;
                foreach (var buff in _activeBuffs)
                    if (!buff.IsExpired) count++;
                return count;
            }
        }

        #endregion

        #region 事件

        /// <summary>加速Buff变化事件</summary>
        public event Action OnSpeedupChanged;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            _consecutiveDefeats = 0;
            _lastClearedWave = 0;
            _sameWaveStuckCount = 0;
            _activeBuffs.Clear();

            Debug.Log("[SpeedupManager] 成长加速管理器初始化完成");
        }

        #endregion

        #region 战斗回调

        /// <summary>
        /// 战斗胜利
        /// </summary>
        public void OnBattleVictory(int waveCleared, int day)
        {
            _consecutiveDefeats = 0;

            // 重置波次卡壳计数
            if (waveCleared > _lastClearedWave)
            {
                _sameWaveStuckCount = 0;
                _lastClearedWave = waveCleared;
            }

            // 重置天数卡壳计数
            if (day > _lastClearedDay)
            {
                _sameDayStuckCount = 0;
                _lastClearedDay = day;
            }

            _currentDay = day;

            // 清除一次性加速buff（连败/卡关类）
            RemoveBuff(SpeedupType.ConsecutiveDefeat);
            RemoveBuff(SpeedupType.WaveStuck);
            RemoveBuff(SpeedupType.DayStuck);

            // 检查新手保护
            CheckNewPlayerProtection(day);

            Debug.Log($"[SpeedupManager] 战斗胜利！波次={waveCleared}, 天={day}, Buff数={ActiveBuffCount}");
        }

        /// <summary>
        /// 战斗失败
        /// </summary>
        public void OnBattleDefeated(int waveReached, int day)
        {
            _consecutiveDefeats++;
            _currentDay = day;

            // 波次卡壳检测
            if (waveReached <= _lastClearedWave)
            {
                _sameWaveStuckCount++;
                if (_sameWaveStuckCount >= SAME_WAVE_STUCK_THRESHOLD)
                {
                    ActivateBuff(SpeedupType.WaveStuck, WAVE_STUCK_BONUS);
                }
            }
            else
            {
                _sameWaveStuckCount = 0;
                _lastClearedWave = waveReached;
            }

            // 天数卡壳检测
            if (day <= _lastClearedDay)
            {
                _sameDayStuckCount++;
                if (_sameDayStuckCount >= SAME_DAY_STUCK_THRESHOLD)
                {
                    ActivateBuff(SpeedupType.DayStuck, DAY_STUCK_BONUS);
                }
            }
            else
            {
                _sameDayStuckCount = 0;
                _lastClearedDay = day;
            }

            // 连续失败检测
            if (_consecutiveDefeats >= CONSECUTIVE_DEFEAT_THRESHOLD)
            {
                ActivateBuff(SpeedupType.ConsecutiveDefeat, CONSECUTIVE_DEFEAT_BONUS);
            }

            // 检查新手保护
            CheckNewPlayerProtection(day);

            Debug.Log($"[SpeedupManager] 战斗失败！波次={waveReached}, 连败={_consecutiveDefeats}, Buff数={ActiveBuffCount}");

            EventBus.Trigger(new SpeedupStateChangedEvent
            {
                ActiveBuffCount = ActiveBuffCount,
                TotalAtkMult = TotalAtkMultiplier,
                TotalHpMult = TotalHpMultiplier,
                ConsecutiveDefeats = _consecutiveDefeats
            });
        }

        #endregion

        #region Buff 管理

        /// <summary>
        /// 激活加速Buff
        /// </summary>
        public void ActivateBuff(SpeedupType type, float bonusPct)
        {
            // 检查是否已存在
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == type && !buff.IsExpired)
                {
                    // 已存在，不重复添加
                    return;
                }
            }

            var newBuff = new SpeedupBuff(type, 1f + bonusPct, 1f + bonusPct, -1f); // 永久持续
            _activeBuffs.Add(newBuff);

            OnSpeedupChanged?.Invoke();

            // 触发事件
            EventBus.Trigger(new SpeedupBuffActivatedEvent
            {
                BuffType = type,
                AtkMultiplier = newBuff.AtkMultiplier,
                HpMultiplier = newBuff.HpMultiplier
            });

            Debug.Log($"[SpeedupManager] 激活加速Buff: {type}, +{bonusPct * 100:F0}%");
        }

        /// <summary>
        /// 移除指定类型Buff
        /// </summary>
        public bool RemoveBuff(SpeedupType type)
        {
            int removed = _activeBuffs.RemoveAll(b => b.Type == type);

            if (removed > 0)
            {
                OnSpeedupChanged?.Invoke();
                Debug.Log($"[SpeedupManager] 移除加速Buff: {type}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 清除所有Buff
        /// </summary>
        public void ClearAllBuffs()
        {
            if (_activeBuffs.Count > 0)
            {
                _activeBuffs.Clear();
                OnSpeedupChanged?.Invoke();
            }
        }

        /// <summary>
        /// 检查是否有指定类型的Buff
        /// </summary>
        public bool HasBuff(SpeedupType type)
        {
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == type && !buff.IsExpired)
                    return true;
            }
            return false;
        }

        #endregion

        #region 新手保护

        /// <summary>
        /// 检查新手保护
        /// </summary>
        private void CheckNewPlayerProtection(int day)
        {
            if (day <= NEW_PLAYER_MAX_DAY)
            {
                if (!HasBuff(SpeedupType.NewPlayer))
                {
                    ActivateBuff(SpeedupType.NewPlayer, NEW_PLAYER_BONUS);
                    Debug.Log("[SpeedupManager] 新手保护激活");
                }
            }
            else
            {
                if (HasBuff(SpeedupType.NewPlayer))
                {
                    RemoveBuff(SpeedupType.NewPlayer);
                    Debug.Log("[SpeedupManager] 新手保护结束");
                }
            }
        }

        #endregion

        #region 更新

        private void Update()
        {
            float dt = Time.deltaTime;
            bool changed = false;

            // 更新限时Buff
            for (int i = _activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = _activeBuffs[i];
                if (!buff.IsPermanent && !buff.IsExpired)
                {
                    buff.RemainingTime -= dt;
                    if (buff.IsExpired)
                    {
                        _activeBuffs.RemoveAt(i);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                OnSpeedupChanged?.Invoke();
            }
        }

        #endregion

        #region 查询

        /// <summary>
        /// 获取指定类型的Buff
        /// </summary>
        public SpeedupBuff GetBuff(SpeedupType type)
        {
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == type && !buff.IsExpired)
                    return buff;
            }
            return null;
        }

        /// <summary>
        /// 连续失败次数
        /// </summary>
        public int ConsecutiveDefeats => _consecutiveDefeats;

        /// <summary>
        /// 同一波次卡壳次数
        /// </summary>
        public int SameWaveStuckCount => _sameWaveStuckCount;

        #endregion

        #region 清理

        protected override void OnDispose()
        {
            OnSpeedupChanged = null;
            _activeBuffs.Clear();
            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// 加速Buff激活事件
    /// </summary>
    public struct SpeedupBuffActivatedEvent : IEvent
    {
        public SpeedupType BuffType;
        public float AtkMultiplier;
        public float HpMultiplier;
    }

    /// <summary>
    /// 加速状态变化事件
    /// </summary>
    public struct SpeedupStateChangedEvent : IEvent
    {
        public int ActiveBuffCount;
        public float TotalAtkMult;
        public float TotalHpMult;
        public int ConsecutiveDefeats;
    }
}
