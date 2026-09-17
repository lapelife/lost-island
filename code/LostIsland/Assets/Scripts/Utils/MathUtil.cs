// ============================================================
// MathUtil.cs
// 数学工具类
// 概率判断、随机范围、缓动函数、插值等常用数学工具
// ============================================================

using UnityEngine;

namespace LostIsland.Utils
{
    /// <summary>
    /// 数学工具类
    /// </summary>
    public static class MathUtil
    {
        #region 概率与随机

        /// <summary>
        /// 概率判断
        /// </summary>
        /// <param name="probability">概率（0-1）</param>
        /// <returns>是否命中</returns>
        public static bool Probability(float probability)
        {
            return Random.value < probability;
        }

        /// <summary>
        /// 百分比概率判断
        /// </summary>
        /// <param name="percent">百分比（0-100）</param>
        public static bool PercentChance(float percent)
        {
            return Random.value * 100f < percent;
        }

        /// <summary>
        /// 从权重数组中随机选择一个索引（加权随机）
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <returns>选中的索引，-1表示所有权重为0</returns>
        public static int WeightedRandom(float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                total += Mathf.Max(0f, weights[i]);
            }

            if (total <= 0f) return -1;

            float value = Random.value * total;
            float cumulative = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += Mathf.Max(0f, weights[i]);
                if (value <= cumulative) return i;
            }

            return weights.Length - 1;
        }

        /// <summary>
        /// 随机返回一个范围内的整数（含两端）
        /// </summary>
        public static int RandomInt(int min, int max)
        {
            return Random.Range(min, max + 1);
        }

        /// <summary>
        /// 随机返回一个范围内的浮点数
        /// </summary>
        public static float RandomFloat(float min, float max)
        {
            return Random.Range(min, max);
        }

        /// <summary>
        /// 在一个数组中随机选择一个元素
        /// </summary>
        public static T RandomChoice<T>(T[] array)
        {
            if (array == null || array.Length == 0) return default(T);
            return array[Random.Range(0, array.Length)];
        }

        #endregion

        #region 缓动函数

        /// <summary>
        /// 线性插值
        /// </summary>
        public static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * Mathf.Clamp01(t);
        }

        /// <summary>
        /// 缓入 - 开始慢，后面快
        /// </summary>
        public static float EaseIn(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        /// <summary>
        /// 缓出 - 开始快，后面慢
        /// </summary>
        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>
        /// 缓入缓出 - 两头慢，中间快
        /// </summary>
        public static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        /// <summary>
        /// 弹性缓出 - 超出后回弹
        /// </summary>
        public static float EaseOutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            float c4 = (2f * Mathf.PI) / 3f;
            return t == 0f ? 0f : t == 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>
        /// 弹跳缓出 - 类似球落地弹跳
        /// </summary>
        public static float EaseOutBounce(float t)
        {
            t = Mathf.Clamp01(t);
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
                return n1 * t * t;
            if (t < 2f / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        #endregion

        #region 数值处理

        /// <summary>
        /// 将值限制在0-1之间
        /// </summary>
        public static float Clamp01(float value)
        {
            return Mathf.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// 将值限制在最小-最大之间
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// 取绝对值
        /// </summary>
        public static float Abs(float value)
        {
            return Mathf.Abs(value);
        }

        /// <summary>
        /// 平滑阻尼 - 类似SmoothDamp
        /// </summary>
        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float deltaTime)
        {
            return Mathf.SmoothDamp(current, target, ref velocity, smoothTime, Mathf.Infinity, deltaTime);
        }

        /// <summary>
        /// 取两个值中较大的
        /// </summary>
        public static float Max(float a, float b)
        {
            return Mathf.Max(a, b);
        }

        /// <summary>
        /// 取两个值中较小的
        /// </summary>
        public static float Min(float a, float b)
        {
            return Mathf.Min(a, b);
        }

        /// <summary>
        /// 四舍五入到指定小数位
        /// </summary>
        public static float Round(float value, int decimals = 0)
        {
            return (float)System.Math.Round(value, decimals);
        }

        /// <summary>
        /// 向0取整
        /// </summary>
        public static int FloorToInt(float value)
        {
            return Mathf.FloorToInt(value);
        }

        /// <summary>
        /// 符号函数 - 正数返回1，负数返回-1，0返回0
        /// </summary>
        public static float Sign(float value)
        {
            return Mathf.Sign(value);
        }

        #endregion

        #region 距离计算

        /// <summary>
        /// 两点之间的距离（无GC，用平方距离判断时用SqrDistance）
        /// </summary>
        public static float Distance(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        public static float Distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// 距离的平方（用于比较，避免开方运算）
        /// </summary>
        public static float SqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// 判断两点是否在指定距离内（用平方比较，性能更好）
        /// </summary>
        public static bool IsWithinDistance(Vector3 a, Vector3 b, float distance)
        {
            return SqrDistance(a, b) <= distance * distance;
        }

        #endregion

        #region 角度计算

        /// <summary>
        /// 角度转弧度
        /// </summary>
        public static float Deg2Rad(float deg)
        {
            return deg * Mathf.Deg2Rad;
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        public static float Rad2Deg(float rad)
        {
            return rad * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 获取两个方向之间的角度
        /// </summary>
        public static float AngleBetween(Vector3 from, Vector3 to)
        {
            return Vector3.Angle(from, to);
        }

        #endregion
    }
}
