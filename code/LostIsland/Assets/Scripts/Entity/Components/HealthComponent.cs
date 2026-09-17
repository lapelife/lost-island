using System;
using UnityEngine;

namespace LostIsland.Entity
{
    /// <summary>
    /// 伤害结果结构体：包含伤害计算的详细信息
    /// </summary>
    public struct DamageResult
    {
        /// <summary>
        /// 最终伤害值
        /// </summary>
        public float FinalDamage;

        /// <summary>
        /// 原始伤害（未减免前）
        /// </summary>
        public float RawDamage;

        /// <summary>
        /// 是否暴击
        /// </summary>
        public bool IsCrit;

        /// <summary>
        /// 伤害减免比例
        /// </summary>
        public float DamageReduction;

        /// <summary>
        /// 反伤值
        /// </summary>
        public float ReflectDamage;

        /// <summary>
        /// 吸血回复量
        /// </summary>
        public float LifestealHeal;

        /// <summary>
        /// 防御减伤比例
        /// </summary>
        public float DefReduction;

        /// <summary>
        /// 伤害减免比例（伤害减免属性）
        /// </summary>
        public float DmgReductionPct;

        /// <summary>
        /// 是否是真实伤害
        /// </summary>
        public bool IsTrueDamage;

        /// <summary>
        /// 伤害来源
        /// </summary>
        public EntityBase Source;

        /// <summary>
        /// 创建一个标准伤害结果
        /// </summary>
        public static DamageResult Create(float damage, bool isCrit = false)
        {
            return new DamageResult
            {
                FinalDamage = damage,
                RawDamage = damage,
                IsCrit = isCrit,
                DefReduction = 0f,
                DmgReductionPct = 0f,
                LifestealHeal = 0f,
                ReflectDamage = 0f,
                IsTrueDamage = false,
                DamageReduction = 0f
            };
        }
    }

    /// <summary>
    /// 生命组件：管理生命值、受伤、治疗、死亡
    /// 与 AttributeComponent 联动，MaxHP 从属性系统获取
    /// </summary>
    public class HealthComponent
    {
        #region 数据

        private AttributeComponent _attribute;
        private float _currentHP;
        private bool _isDead = false;

        // 无敌帧
        private float _invincibleTimer = 0f;
        private bool _isInvincible = false;

        // 上次HP百分比（用于检测变化）
        private float _lastHPPct = 1f;

        #endregion

        #region 属性

        /// <summary>
        /// 当前生命值
        /// </summary>
        public float CurrentHP
        {
            get => _currentHP;
            private set
            {
                float oldPct = HPPct;
                _currentHP = Mathf.Clamp(value, 0f, MaxHP);
                float newPct = HPPct;

                if (Mathf.Abs(oldPct - newPct) > 0.001f)
                {
                    OnHPPctChanged?.Invoke(oldPct, newPct);
                    _lastHPPct = newPct;
                }
            }
        }

        /// <summary>
        /// 最大生命值（从属性系统获取）
        /// </summary>
        public float MaxHP => _attribute != null ? _attribute.FinalMaxHP : 0f;

        /// <summary>
        /// 生命值百分比
        /// </summary>
        public float HPPct => MaxHP > 0f ? _currentHP / MaxHP : 0f;

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool IsDead => _isDead;

        /// <summary>
        /// 是否无敌
        /// </summary>
        public bool IsInvincible => _isInvincible || _invincibleTimer > 0f;

        #endregion

        #region 事件

        /// <summary>
        /// 受伤事件：伤害结果
        /// </summary>
        public event Action<DamageResult> OnTakeDamage;

        /// <summary>
        /// 治疗事件：治疗量
        /// </summary>
        public event Action<float> OnHeal;

        /// <summary>
        /// 死亡事件
        /// </summary>
        public event Action OnDeath;

        /// <summary>
        /// 复活事件
        /// </summary>
        public event Action OnRevive;

        /// <summary>
        /// HP百分比变化事件：旧百分比，新百分比
        /// </summary>
        public event Action<float, float> OnHPPctChanged;

        #endregion

        #region 初始化

        public HealthComponent(AttributeComponent attribute)
        {
            _attribute = attribute;
            _currentHP = MaxHP;
            _isDead = false;

            // 监听属性变化，最大HP变化时同步调整当前HP
            if (_attribute != null)
            {
                _attribute.OnAttrChanged += OnAttributeChanged;
            }
        }

        /// <summary>
        /// 属性变化时的处理
        /// </summary>
        private void OnAttributeChanged(AttrType type, float oldValue, float newValue)
        {
            if (type == AttrType.MaxHP)
            {
                if (!_isDead)
                {
                    // 最大HP变化时，按比例调整当前HP
                    float ratio = oldValue > 0f ? _currentHP / oldValue : 1f;
                    _currentHP = newValue * ratio;
                }
            }
        }

        #endregion

        #region 受伤逻辑

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害结果</param>
        /// <returns>实际造成的伤害</returns>
        public float TakeDamage(DamageResult damage)
        {
            if (_isDead) return 0f;
            if (IsInvincible) return 0f;
            if (damage.FinalDamage <= 0f) return 0f;

            float actualDamage = Mathf.Min(damage.FinalDamage, _currentHP);
            CurrentHP -= actualDamage;

            // 触发受伤事件
            OnTakeDamage?.Invoke(damage);

            // 检查死亡
            if (_currentHP <= 0f && !_isDead)
            {
                Die();
            }

            return actualDamage;
        }

        /// <summary>
        /// 直接造成伤害（简化版，内部创建DamageResult）
        /// </summary>
        public float TakeDamage(float damage, bool isCrit = false)
        {
            return TakeDamage(DamageResult.Create(damage, isCrit));
        }

        #endregion

        #region 治疗逻辑

        /// <summary>
        /// 治疗
        /// </summary>
        /// <param name="amount">治疗量</param>
        /// <returns>实际治疗量</returns>
        public float Heal(float amount)
        {
            if (_isDead) return 0f;
            if (amount <= 0f) return 0f;

            float oldHP = _currentHP;
            CurrentHP += amount;
            float actualHeal = _currentHP - oldHP;

            if (actualHeal > 0f)
            {
                OnHeal?.Invoke(actualHeal);
            }

            return actualHeal;
        }

        /// <summary>
        /// 按百分比治疗
        /// </summary>
        /// <param name="pct">最大HP的百分比（0-1）</param>
        /// <returns>实际治疗量</returns>
        public float HealPct(float pct)
        {
            return Heal(MaxHP * Mathf.Clamp01(pct));
        }

        /// <summary>
        /// 回满生命值
        /// </summary>
        public float HealFull()
        {
            return Heal(MaxHP - _currentHP);
        }

        #endregion

        #region 死亡与复活

        /// <summary>
        /// 死亡
        /// </summary>
        private void Die()
        {
            if (_isDead) return;

            _isDead = true;
            _currentHP = 0f;

            Debug.Log($"[HealthComponent] 实体死亡");
            OnDeath?.Invoke();
        }

        /// <summary>
        /// 复活
        /// </summary>
        /// <param name="hpPct">复活时的HP百分比（默认100%）</param>
        public void Revive(float hpPct = 1f)
        {
            if (!_isDead) return;

            _isDead = false;
            _currentHP = MaxHP * Mathf.Clamp01(hpPct);
            _invincibleTimer = 0f;
            _isInvincible = false;

            Debug.Log($"[HealthComponent] 实体复活，HP={_currentHP:F0}");
            OnRevive?.Invoke();
        }

        #endregion

        #region 无敌机制

        /// <summary>
        /// 设置无敌状态
        /// </summary>
        public void SetInvincible(bool invincible)
        {
            _isInvincible = invincible;
        }

        /// <summary>
        /// 添加无敌帧时间
        /// </summary>
        /// <param name="duration">无敌持续时间（秒）</param>
        public void AddInvincibleTime(float duration)
        {
            if (duration > 0f)
            {
                _invincibleTimer = Mathf.Max(_invincibleTimer, duration);
            }
        }

        /// <summary>
        /// 更新无敌帧计时器
        /// </summary>
        public void UpdateInvincibleTimer(float deltaTime)
        {
            if (_invincibleTimer > 0f)
            {
                _invincibleTimer -= deltaTime;
                if (_invincibleTimer <= 0f)
                {
                    _invincibleTimer = 0f;
                }
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 重置生命组件
        /// </summary>
        public void Reset()
        {
            _isDead = false;
            _currentHP = MaxHP;
            _invincibleTimer = 0f;
            _isInvincible = false;
            _lastHPPct = 1f;
        }

        /// <summary>
        /// 设置当前HP（用于初始化或特殊情况）
        /// </summary>
        public void SetCurrentHP(float hp)
        {
            if (_isDead) return;
            CurrentHP = hp;
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        public void DebugDump()
        {
            Debug.Log($"[HealthComponent] HP: {_currentHP:F0}/{MaxHP:F0} ({HPPct:P0}), Dead: {_isDead}, Invincible: {IsInvincible}");
        }

        #endregion
    }
}
