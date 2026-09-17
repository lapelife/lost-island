// ============================================================
// CoroutineHelper.cs
// 协程辅助工具
// 常用的协程等待模式，避免散落的IEnumerator
// ============================================================

using System;
using System.Collections;
using UnityEngine;

namespace LostIsland.Utils
{
    /// <summary>
    /// 协程辅助工具
    /// </summary>
    public static class CoroutineHelper
    {
        /// <summary>
        /// 等待指定秒数
        /// </summary>
        public static IEnumerator WaitForSeconds(float seconds, Action callback = null)
        {
            yield return new WaitForSeconds(seconds);
            callback?.Invoke();
        }

        /// <summary>
        /// 等待指定秒数（不受时间缩放影响）
        /// </summary>
        public static IEnumerator WaitForSecondsRealtime(float seconds, Action callback = null)
        {
            yield return new WaitForSecondsRealtime(seconds);
            callback?.Invoke();
        }

        /// <summary>
        /// 等待指定帧数
        /// </summary>
        public static IEnumerator WaitForFrames(int frameCount, Action callback = null)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
            callback?.Invoke();
        }

        /// <summary>
        /// 等待到下一帧末尾
        /// </summary>
        public static IEnumerator WaitForEndOfFrame(Action callback = null)
        {
            yield return new WaitForEndOfFrame();
            callback?.Invoke();
        }

        /// <summary>
        /// 等待直到条件满足
        /// </summary>
        /// <param name="condition">条件判断函数</param>
        /// <param name="callback">条件满足后的回调</param>
        /// <param name="timeout">超时时间（秒），0表示不超时</param>
        public static IEnumerator WaitUntil(Func<bool> condition, Action callback = null, float timeout = 0f)
        {
            float elapsed = 0f;
            while (!condition.Invoke())
            {
                if (timeout > 0f)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeout) yield break;
                }
                yield return null;
            }
            callback?.Invoke();
        }

        /// <summary>
        /// 等待while条件为真时持续执行
        /// </summary>
        /// <param name="condition">保持等待的条件</param>
        /// <param name="callback">每帧回调</param>
        /// <param name="onComplete">条件结束后的回调</param>
        public static IEnumerator WaitWhile(Func<bool> condition, Action<float> callback = null, Action onComplete = null)
        {
            while (condition.Invoke())
            {
                callback?.Invoke(Time.deltaTime);
                yield return null;
            }
            onComplete?.Invoke();
        }

        /// <summary>
        /// 执行一个持续指定时间的动作（带插值）
        /// </summary>
        /// <param name="duration">持续时间</param>
        /// <param name="onUpdate">每帧回调，参数为进度0-1</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="useUnscaledTime">是否使用真实时间</param>
        public static IEnumerator Tween(float duration, Action<float> onUpdate, Action onComplete = null, bool useUnscaledTime = false)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate?.Invoke(t);
                yield return null;
            }
            onUpdate?.Invoke(1f);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 延迟到下一帧执行（等同于yield return null）
        /// </summary>
        public static IEnumerator NextFrame(Action callback)
        {
            yield return null;
            callback?.Invoke();
        }

        /// <summary>
        /// 延迟到FixedUpdate执行
        /// </summary>
        public static IEnumerator WaitForFixedUpdate(Action callback = null)
        {
            yield return new WaitForFixedUpdate();
            callback?.Invoke();
        }

        /// <summary>
        /// 按指定间隔循环执行，直到返回false
        /// </summary>
        /// <param name="interval">间隔时间</param>
        /// <param name="action">执行的动作，返回false停止</param>
        public static IEnumerator Repeat(float interval, Func<bool> action)
        {
            WaitForSeconds wait = new WaitForSeconds(interval);
            while (action.Invoke())
            {
                yield return wait;
            }
        }

        /// <summary>
        /// 顺序执行多个协程
        /// </summary>
        public static IEnumerator Sequence(params IEnumerator[] coroutines)
        {
            foreach (var coroutine in coroutines)
            {
                yield return coroutine;
            }
        }

        /// <summary>
        /// 并行等待多个协程全部完成
        /// </summary>
        public static IEnumerator Parallel(MonoBehaviour runner, params IEnumerator[] coroutines)
        {
            int completed = 0;
            int total = coroutines.Length;

            foreach (var coroutine in coroutines)
            {
                runner.StartCoroutine(RunAndCount(coroutine, () => completed++));
            }

            while (completed < total)
            {
                yield return null;
            }
        }

        private static IEnumerator RunAndCount(IEnumerator coroutine, Action onComplete)
        {
            yield return coroutine;
            onComplete?.Invoke();
        }
    }
}
