// ============================================================
// EventBus.cs
// 事件总线 - 模块间通信的核心基础设施
// 使用泛型事件类型，支持订阅/取消订阅/触发
// 各模块通过事件解耦，不直接引用
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Core
{
    /// <summary>
    /// 事件总线 - 全局事件系统
    /// 用法：
    ///   订阅：EventBus.Subscribe&lt;MyEvent&gt;(OnMyEvent);
    ///   触发：EventBus.Trigger(new MyEvent { data = ... });
    ///   取消：EventBus.Unsubscribe&lt;MyEvent&gt;(OnMyEvent);
    /// </summary>
    public static class EventBus
    {
        private delegate void EventCallback(IEvent e);
        private static readonly Dictionary<Type, EventCallback> _eventCallbacks
            = new Dictionary<Type, EventCallback>();
        private static readonly Dictionary<Type, Delegate> _genericCallbacks
            = new Dictionary<Type, Delegate>();

        /// <summary>
        /// 订阅事件
        /// </summary>
        public static void Subscribe<T>(Action<T> callback) where T : IEvent
        {
            Type eventType = typeof(T);

            if (!_genericCallbacks.ContainsKey(eventType))
            {
                _genericCallbacks[eventType] = callback;
                _eventCallbacks[eventType] = (e) => callback((T)e);
            }
            else
            {
                _genericCallbacks[eventType] = Delegate.Combine(_genericCallbacks[eventType], callback);
                _eventCallbacks[eventType] = (EventCallback)Delegate.Combine(
                    _eventCallbacks[eventType],
                    new EventCallback(e => callback((T)e))
                );
            }
        }

        /// <summary>
        /// 一次性订阅 - 触发后自动取消订阅
        /// </summary>
        public static void SubscribeOnce<T>(Action<T> callback) where T : IEvent
        {
            Action<T> wrapper = null;
            wrapper = (e) =>
            {
                callback(e);
                Unsubscribe(wrapper);
            };
            Subscribe(wrapper);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : IEvent
        {
            Type eventType = typeof(T);

            if (_genericCallbacks.TryGetValue(eventType, out Delegate existing))
            {
                Delegate newDel = Delegate.Remove(existing, callback);
                if (newDel == null)
                {
                    _genericCallbacks.Remove(eventType);
                    _eventCallbacks.Remove(eventType);
                }
                else
                {
                    _genericCallbacks[eventType] = newDel;
                    // 注意：_eventCallbacks无法精确移除单个，整体重建
                    RebuildEventCallbacks(eventType);
                }
            }
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public static void Trigger<T>(T eventData) where T : IEvent
        {
            Type eventType = typeof(T);

            if (_eventCallbacks.TryGetValue(eventType, out EventCallback callback))
            {
                try
                {
                    callback?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Error triggering event {eventType.Name}: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        /// <summary>
        /// 检查是否有订阅者
        /// </summary>
        public static bool HasSubscribers<T>() where T : IEvent
        {
            return _eventCallbacks.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 清除某一事件的所有订阅
        /// </summary>
        public static void ClearEvent<T>() where T : IEvent
        {
            Type eventType = typeof(T);
            _eventCallbacks.Remove(eventType);
            _genericCallbacks.Remove(eventType);
        }

        /// <summary>
        /// 清除所有事件（慎用，一般只在重新加载时使用）
        /// </summary>
        public static void ClearAll()
        {
            _eventCallbacks.Clear();
            _genericCallbacks.Clear();
        }

        /// <summary>
        /// 获取订阅者数量
        /// </summary>
        public static int GetSubscriberCount<T>() where T : IEvent
        {
            Type eventType = typeof(T);
            if (_genericCallbacks.TryGetValue(eventType, out Delegate del))
            {
                return del.GetInvocationList().Length;
            }
            return 0;
        }

        // 重建_eventCallbacks（取消单个订阅时使用）
        private static void RebuildEventCallbacks(Type eventType)
        {
            if (_genericCallbacks.TryGetValue(eventType, out Delegate genDel))
            {
                EventCallback newCallback = null;
                foreach (var d in genDel.GetInvocationList())
                {
                    newCallback += (e) => d.DynamicInvoke(e);
                }
                _eventCallbacks[eventType] = newCallback;
            }
            else
            {
                _eventCallbacks.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 事件接口 - 所有事件必须实现此接口
    /// </summary>
    public interface IEvent
    {
    }
}
