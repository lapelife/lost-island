// ============================================================
// TimerManager.cs
// 计时器管理器
// 支持延迟执行、循环执行、倒计时、帧定时
// 统一管理所有计时器，避免散落的协程
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Utils
{
    /// <summary>
    /// 计时器管理器
    /// 所有游戏中的计时器都应通过此管理器创建
    /// </summary>
    public class TimerManager : MonoSingleton<TimerManager>
    {
        private class TimerData
        {
            public int id;
            public float duration;
            public float elapsed;
            public bool isLooping;
            public bool useUnscaledTime;
            public Action onComplete;
            public Action<float> onTick; // 每帧回调，参数为剩余时间百分比
            public bool isRunning;
            public bool autoRemove;
        }

        private Dictionary<int, TimerData> _timers = new Dictionary<int, TimerData>();
        private List<int> _timersToRemove = new List<int>();
        private int _nextTimerId = 1;

        protected override void OnInit()
        {
            base.OnInit();
            Debug.Log("[TimerManager] Initialized.");
        }

        private void Update()
        {
            if (_timers.Count == 0) return;

            _timersToRemove.Clear();

            foreach (var kvp in _timers)
            {
                TimerData timer = kvp.Value;
                if (!timer.isRunning) continue;

                float dt = timer.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                timer.elapsed += dt;

                // 每帧回调
                timer.onTick?.Invoke(1f - timer.elapsed / timer.duration);

                if (timer.elapsed >= timer.duration)
                {
                    // 计时完成
                    timer.onComplete?.Invoke();

                    if (timer.isLooping)
                    {
                        timer.elapsed = 0f;
                    }
                    else if (timer.autoRemove)
                    {
                        _timersToRemove.Add(kvp.Key);
                    }
                    else
                    {
                        timer.isRunning = false;
                    }
                }
            }

            // 移除已完成的计时器
            for (int i = 0; i < _timersToRemove.Count; i++)
            {
                _timers.Remove(_timersToRemove[i]);
            }
        }

        /// <summary>
        /// 延迟执行一次
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">回调</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>计时器ID，用于手动取消</returns>
        public int Delay(float delay, Action callback, bool useUnscaledTime = false)
        {
            TimerData timer = new TimerData
            {
                id = _nextTimerId++,
                duration = delay,
                elapsed = 0f,
                isLooping = false,
                useUnscaledTime = useUnscaledTime,
                onComplete = callback,
                isRunning = true,
                autoRemove = true
            };

            _timers.Add(timer.id, timer);
            return timer.id;
        }

        /// <summary>
        /// 循环执行
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="callback">回调</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>计时器ID</returns>
        public int Loop(float interval, Action callback, bool useUnscaledTime = false)
        {
            TimerData timer = new TimerData
            {
                id = _nextTimerId++,
                duration = interval,
                elapsed = 0f,
                isLooping = true,
                useUnscaledTime = useUnscaledTime,
                onComplete = callback,
                isRunning = true,
                autoRemove = false
            };

            _timers.Add(timer.id, timer);
            return timer.id;
        }

        /// <summary>
        /// 倒计时（带每帧回调）
        /// </summary>
        /// <param name="duration">总时长</param>
        /// <param name="onTick">每帧回调（参数：剩余百分比0-1）</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="useUnscaledTime">是否使用不受时间缩放影响的时间</param>
        /// <returns>计时器ID</returns>
        public int Countdown(float duration, Action<float> onTick, Action onComplete = null, bool useUnscaledTime = false)
        {
            TimerData timer = new TimerData
            {
                id = _nextTimerId++,
                duration = duration,
                elapsed = 0f,
                isLooping = false,
                useUnscaledTime = useUnscaledTime,
                onTick = onTick,
                onComplete = onComplete,
                isRunning = true,
                autoRemove = true
            };

            _timers.Add(timer.id, timer);
            return timer.id;
        }

        /// <summary>
        /// 延迟指定帧数执行
        /// </summary>
        /// <param name="frameCount">帧数</param>
        /// <param name="callback">回调</param>
        /// <returns>计时器ID</returns>
        public int DelayFrames(int frameCount, Action callback)
        {
            // 用一个极短间隔的循环计数器近似
            int framesLeft = frameCount;
            int timerId = 0;
            timerId = Loop(0.001f, () =>
            {
                framesLeft--;
                if (framesLeft <= 0)
                {
                    callback?.Invoke();
                    CancelTimer(timerId);
                }
            });
            return timerId;
        }

        /// <summary>
        /// 取消计时器
        /// </summary>
        public void CancelTimer(int timerId)
        {
            if (_timers.ContainsKey(timerId))
            {
                _timers[timerId].isRunning = false;
                _timers.Remove(timerId);
            }
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        public void PauseTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerData timer))
            {
                timer.isRunning = false;
            }
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        public void ResumeTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerData timer))
            {
                timer.isRunning = true;
            }
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        public void ResetTimer(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerData timer))
            {
                timer.elapsed = 0f;
                timer.isRunning = true;
            }
        }

        /// <summary>
        /// 清除所有计时器
        /// </summary>
        public void ClearAll()
        {
            _timers.Clear();
            _timersToRemove.Clear();
        }

        /// <summary>
        /// 获取计时器剩余时间（秒）
        /// </summary>
        public float GetRemainingTime(int timerId)
        {
            if (_timers.TryGetValue(timerId, out TimerData timer))
            {
                return Mathf.Max(0f, timer.duration - timer.elapsed);
            }
            return 0f;
        }

        /// <summary>
        /// 当前活跃计时器数量
        /// </summary>
        public int ActiveTimerCount => _timers.Count;
    }
}
