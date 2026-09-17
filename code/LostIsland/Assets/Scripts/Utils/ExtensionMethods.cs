// ============================================================
// ExtensionMethods.cs
// 扩展方法集合
// Transform/List/String/GameObject 等常用扩展
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LostIsland.Utils
{
    /// <summary>
    /// 扩展方法集合
    /// </summary>
    public static class ExtensionMethods
    {
        #region Transform 扩展

        /// <summary>
        /// 重置Transform的位置、旋转、缩放
        /// </summary>
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 设置X坐标
        /// </summary>
        public static void SetPosX(this Transform transform, float x)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        /// <summary>
        /// 设置Y坐标
        /// </summary>
        public static void SetPosY(this Transform transform, float y)
        {
            Vector3 pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        /// <summary>
        /// 设置Z坐标
        /// </summary>
        public static void SetPosZ(this Transform transform, float z)
        {
            Vector3 pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }

        /// <summary>
        /// 销毁所有子物体
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 立即销毁所有子物体（Editor中使用）
        /// </summary>
        public static void DestroyAllChildrenImmediate(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 查找子物体（递归）
        /// </summary>
        public static Transform FindRecursive(this Transform parent, string name)
        {
            Transform result = parent.Find(name);
            if (result != null) return result;

            for (int i = 0; i < parent.childCount; i++)
            {
                result = FindRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        #endregion

        #region GameObject 扩展

        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// 设置Layer（包括所有子物体）
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                SetLayerRecursively(gameObject.transform.GetChild(i).gameObject, layer);
            }
        }

        #endregion

        #region List 扩展

        /// <summary>
        /// 随机打乱列表（Fisher-Yates洗牌算法）
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// 随机选择一个元素
        /// </summary>
        public static T RandomChoice<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 移除最后一个元素并返回
        /// </summary>
        public static T Pop<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            T last = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return last;
        }

        /// <summary>
        /// 判断列表是否为空
        /// </summary>
        public static bool IsEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }

        #endregion

        #region String 扩展

        /// <summary>
        /// 判断字符串是否为空或空白
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// 格式化字符串（简化写法）
        /// </summary>
        public static string FormatWith(this string format, params object[] args)
        {
            return string.Format(format, args);
        }

        #endregion

        #region Image 扩展

        /// <summary>
        /// 设置Image的透明度
        /// </summary>
        public static void SetAlpha(this Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        #endregion

        #region float 扩展

        /// <summary>
        /// 判断是否接近零
        /// </summary>
        public static bool IsApproximatelyZero(this float value, float epsilon = 0.0001f)
        {
            return Mathf.Abs(value) < epsilon;
        }

        /// <summary>
        /// 限制在0-1
        /// </summary>
        public static float Clamp01(this float value)
        {
            return Mathf.Clamp01(value);
        }

        #endregion
    }
}
