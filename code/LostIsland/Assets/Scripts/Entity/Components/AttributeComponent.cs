using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Entity
{
    /// <summary>
    /// 属性组件：核心属性计算系统
    /// 支持15层加成叠加、脏标记懒计算、属性上限裁剪、属性变化事件
    /// 
    /// 计算规则：
    /// 最终值 = (基础值 + 固定加成) × Π(1 + 各层百分比加成)
    /// 同一层内多个加成源互相独立（各自乘）
    /// </summary>
    public class AttributeComponent
    {
        #region 数据结构

        // 基础属性值（L0层）
        private float[] _baseValues = new float[(int)AttrType.Max];

        // 固定值加成（L1层） - 按来源存储
        private Dictionary<string, float[]> _flatAdders = new Dictionary<string, float[]>();

        // 各层级百分比加成 - 按来源存储，每个来源对应一个层级数组
        // key: 来源ID, value: 各属性的百分比加成数组
        private Dictionary<string, float[]> _pctAdders = new Dictionary<string, float[]>();

        // 记录每个加成来源所属的层级
        private Dictionary<string, AttrLayer> _pctSourceLayers = new Dictionary<string, AttrLayer>();

        // 缓存的最终属性值（脏标记模式）
        private float[] _finalValues = new float[(int)AttrType.Max];

        // 脏标记数组
        private bool[] _isDirty = new bool[(int)AttrType.Max];

        #endregion

        #region 属性上限常量

        /// <summary>
        /// 暴击率上限
        /// </summary>
        public const float CRIT_RATE_CAP = 0.75f;

        /// <summary>
        /// 攻击速度上限（次/秒）
        /// </summary>
        public const float ATTACK_SPEED_CAP = 3.0f;

        /// <summary>
        /// 伤害减免上限
        /// </summary>
        public const float DAMAGE_REDUCTION_CAP = 0.9f;

        #endregion

        #region 对外快捷属性

        public float FinalMaxHP => GetValue(AttrType.MaxHP);
        public float FinalATK => GetValue(AttrType.ATK);
        public float FinalDEF => GetValue(AttrType.DEF);
        public float FinalAttackSpeed => GetValue(AttrType.AttackSpeed);
        public float FinalCritRate => GetValue(AttrType.CritRate);
        public float FinalCritDamage => GetValue(AttrType.CritDamage);
        public float FinalMoveSpeed => GetValue(AttrType.MoveSpeed);
        public float FinalLifesteal => GetValue(AttrType.Lifesteal);
        public float FinalDamageReduction => GetValue(AttrType.DamageReduction);
        public float FinalReflect => GetValue(AttrType.Reflect);

        #endregion

        #region 事件

        /// <summary>
        /// 属性变化事件：属性类型，旧值，新值
        /// </summary>
        public event Action<AttrType, float, float> OnAttrChanged;

        #endregion

        #region 构造与初始化

        public AttributeComponent()
        {
            // 初始全部标记为脏
            for (int i = 0; i < (int)AttrType.Max; i++)
            {
                _isDirty[i] = true;
            }
        }

        /// <summary>
        /// 设置基础属性值（L0层）
        /// </summary>
        public void SetBaseValue(AttrType type, float value)
        {
            int idx = (int)type;
            float oldValue = GetValue(type);

            _baseValues[idx] = value;
            MarkDirty(type);

            float newValue = GetValue(type);
            if (Mathf.Abs(oldValue - newValue) > 0.001f)
            {
                OnAttrChanged?.Invoke(type, oldValue, newValue);
            }
        }

        /// <summary>
        /// 获取基础属性值
        /// </summary>
        public float GetBaseValue(AttrType type)
        {
            return _baseValues[(int)type];
        }

        #endregion

        #region 固定值加成（L1层）

        /// <summary>
        /// 添加固定值加成
        /// </summary>
        /// <param name="sourceId">来源唯一ID</param>
        /// <param name="type">属性类型</param>
        /// <param name="value">固定值</param>
        public void AddFlat(string sourceId, AttrType type, float value)
        {
            if (!_flatAdders.ContainsKey(sourceId))
            {
                _flatAdders[sourceId] = new float[(int)AttrType.Max];
            }

            float oldValue = GetValue(type);

            _flatAdders[sourceId][(int)type] = value;
            MarkDirty(type);

            float newValue = GetValue(type);
            if (Mathf.Abs(oldValue - newValue) > 0.001f)
            {
                OnAttrChanged?.Invoke(type, oldValue, newValue);
            }
        }

        /// <summary>
        /// 移除固定值加成
        /// </summary>
        public void RemoveFlat(string sourceId, AttrType type)
        {
            if (_flatAdders.TryGetValue(sourceId, out var values))
            {
                float oldValue = GetValue(type);

                values[(int)type] = 0f;
                MarkDirty(type);

                float newValue = GetValue(type);
                if (Mathf.Abs(oldValue - newValue) > 0.001f)
                {
                    OnAttrChanged?.Invoke(type, oldValue, newValue);
                }
            }
        }

        /// <summary>
        /// 移除某来源的所有固定值加成
        /// </summary>
        public void RemoveFlatAll(string sourceId)
        {
            if (_flatAdders.ContainsKey(sourceId))
            {
                // 先记录所有变化的属性
                List<(AttrType type, float oldVal)> changedAttrs = new List<(AttrType, float)>();
                for (int i = 0; i < (int)AttrType.Max; i++)
                {
                    if (_flatAdders[sourceId][i] != 0f)
                    {
                        changedAttrs.Add(((AttrType)i, GetValue((AttrType)i)));
                        MarkDirty((AttrType)i);
                    }
                }

                _flatAdders.Remove(sourceId);

                // 触发变化事件
                foreach (var (type, oldVal) in changedAttrs)
                {
                    float newVal = GetValue(type);
                    if (Mathf.Abs(oldVal - newVal) > 0.001f)
                    {
                        OnAttrChanged?.Invoke(type, oldVal, newVal);
                    }
                }
            }
        }

        #endregion

        #region 百分比加成（L2-L14层）

        /// <summary>
        /// 添加百分比加成
        /// </summary>
        /// <param name="sourceId">来源唯一ID</param>
        /// <param name="type">属性类型</param>
        /// <param name="pct">百分比值（如0.1表示10%）</param>
        /// <param name="layer">加成层级</param>
        public void AddPct(string sourceId, AttrType type, float pct, AttrLayer layer)
        {
            // 层级必须在百分比层范围内（L2及以上）
            if (layer <= AttrLayer.FlatAdd)
            {
                Debug.LogError($"[AttributeComponent] 百分比加成层级错误: {layer}");
                return;
            }

            if (!_pctAdders.ContainsKey(sourceId))
            {
                _pctAdders[sourceId] = new float[(int)AttrType.Max];
                _pctSourceLayers[sourceId] = layer;
            }

            float oldValue = GetValue(type);

            _pctAdders[sourceId][(int)type] = pct;
            MarkDirty(type);

            float newValue = GetValue(type);
            if (Mathf.Abs(oldValue - newValue) > 0.001f)
            {
                OnAttrChanged?.Invoke(type, oldValue, newValue);
            }
        }

        /// <summary>
        /// 移除百分比加成
        /// </summary>
        public void RemovePct(string sourceId, AttrType type)
        {
            if (_pctAdders.TryGetValue(sourceId, out var values))
            {
                float oldValue = GetValue(type);

                values[(int)type] = 0f;
                MarkDirty(type);

                float newValue = GetValue(type);
                if (Mathf.Abs(oldValue - newValue) > 0.001f)
                {
                    OnAttrChanged?.Invoke(type, oldValue, newValue);
                }
            }
        }

        /// <summary>
        /// 移除某来源的所有百分比加成
        /// </summary>
        public void RemovePctAll(string sourceId)
        {
            if (_pctAdders.ContainsKey(sourceId))
            {
                // 先记录所有变化的属性
                List<(AttrType type, float oldVal)> changedAttrs = new List<(AttrType, float)>();
                for (int i = 0; i < (int)AttrType.Max; i++)
                {
                    if (_pctAdders[sourceId][i] != 0f)
                    {
                        changedAttrs.Add(((AttrType)i, GetValue((AttrType)i)));
                        MarkDirty((AttrType)i);
                    }
                }

                _pctAdders.Remove(sourceId);
                _pctSourceLayers.Remove(sourceId);

                // 触发变化事件
                foreach (var (type, oldVal) in changedAttrs)
                {
                    float newVal = GetValue(type);
                    if (Mathf.Abs(oldVal - newVal) > 0.001f)
                    {
                        OnAttrChanged?.Invoke(type, oldVal, newVal);
                    }
                }
            }
        }

        /// <summary>
        /// 检查某来源是否已添加百分比加成
        /// </summary>
        public bool HasPctSource(string sourceId)
        {
            return _pctAdders.ContainsKey(sourceId);
        }

        #endregion

        #region 核心计算逻辑

        /// <summary>
        /// 获取属性最终值
        /// 脏标记模式：只有标记为脏时才重新计算
        /// </summary>
        public float GetValue(AttrType type)
        {
            int idx = (int)type;
            if (!_isDirty[idx])
            {
                return _finalValues[idx];
            }

            // Step1: 基础值 + 固定加成
            float value = _baseValues[idx];

            // 加上所有固定值加成
            foreach (var kvp in _flatAdders)
            {
                value += kvp.Value[idx];
            }

            // Step2: 按层级顺序乘上各层百分比加成
            // 注意：同一层级内的多个加成源互相独立（各自乘）
            // 这里需要按层级顺序遍历，所以需要按层级分组
            // 为了性能，我们按层级遍历所有来源，只处理对应层级的加成
            for (int layer = (int)AttrLayer.Pct_SkillCard; layer < (int)AttrLayer.Max; layer++)
            {
                float layerMultiplier = 1f;

                foreach (var kvp in _pctAdders)
                {
                    if ((int)_pctSourceLayers[kvp.Key] == layer)
                    {
                        float pct = kvp.Value[idx];
                        if (pct != 0f)
                        {
                            layerMultiplier *= (1f + pct);
                        }
                    }
                }

                if (layerMultiplier != 1f)
                {
                    value *= layerMultiplier;
                }
            }

            // Step3: 特殊属性做上限裁剪
            value = ApplyCap(type, value);

            _finalValues[idx] = value;
            _isDirty[idx] = false;
            return value;
        }

        /// <summary>
        /// 应用属性上限
        /// </summary>
        private float ApplyCap(AttrType type, float value)
        {
            switch (type)
            {
                case AttrType.CritRate:
                    return Mathf.Clamp(value, 0f, CRIT_RATE_CAP);

                case AttrType.AttackSpeed:
                    return Mathf.Min(value, ATTACK_SPEED_CAP);

                case AttrType.DamageReduction:
                    return Mathf.Clamp(value, 0f, DAMAGE_REDUCTION_CAP);

                case AttrType.Lifesteal:
                    return Mathf.Max(0f, value);

                case AttrType.Reflect:
                    return Mathf.Max(0f, value);

                default:
                    // 大部分属性只有下限0
                    return Mathf.Max(0f, value);
            }
        }

        /// <summary>
        /// 标记属性为脏
        /// </summary>
        private void MarkDirty(AttrType type)
        {
            _isDirty[(int)type] = true;
        }

        /// <summary>
        /// 强制全部重新计算
        /// </summary>
        public void MarkAllDirty()
        {
            for (int i = 0; i < (int)AttrType.Max; i++)
            {
                _isDirty[i] = true;
            }
        }

        /// <summary>
        /// 检查属性是否为脏
        /// </summary>
        public bool IsDirty(AttrType type)
        {
            return _isDirty[(int)type];
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 重置所有属性
        /// </summary>
        public void ResetAll()
        {
            // 记录变化的属性
            List<(AttrType type, float oldVal)> changedAttrs = new List<(AttrType, float)>();
            for (int i = 0; i < (int)AttrType.Max; i++)
            {
                float oldVal = GetValue((AttrType)i);
                if (oldVal != _baseValues[i])
                {
                    changedAttrs.Add(((AttrType)i, oldVal));
                }
            }

            _flatAdders.Clear();
            _pctAdders.Clear();
            _pctSourceLayers.Clear();

            MarkAllDirty();

            // 触发变化事件
            foreach (var (type, oldVal) in changedAttrs)
            {
                float newVal = GetValue(type);
                if (Mathf.Abs(oldVal - newVal) > 0.001f)
                {
                    OnAttrChanged?.Invoke(type, oldVal, newVal);
                }
            }
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        public void DebugDump()
        {
            Debug.Log("=== AttributeComponent Dump ===");
            for (int i = 0; i < (int)AttrType.Max; i++)
            {
                AttrType type = (AttrType)i;
                Debug.Log($"{type}: base={_baseValues[i]:F2}, final={GetValue(type):F2}, dirty={_isDirty[i]}");
            }
            Debug.Log($"Flat adders: {_flatAdders.Count}, Pct adders: {_pctAdders.Count}");
        }

        #endregion
    }
}
