using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LostIsland.Core;
using LostIsland.Infrastructure;

namespace LostIsland.View.UI
{
    /// <summary>
    /// 伤害飘字类型
    /// </summary>
    public enum DamagePopType
    {
        Normal = 0,     // 普通伤害（白色）
        Critical = 1,   // 暴击伤害（红色/放大）
        Heal = 2,       // 治疗（绿色）
        TrueDamage = 3, // 真实伤害（紫色）
        Miss = 4,       // 闪避/未命中（灰色）
    }

    /// <summary>
    /// 伤害飘字管理器：管理战斗中的伤害数字飘字效果
    /// 使用对象池管理飘字对象，战斗中零GC
    /// </summary>
    public class DamagePopUpManager : MonoSingleton<DamagePopUpManager>
    {
        [Header("飘字配置")]
        [SerializeField] private GameObject _popUpPrefab;
        [SerializeField] private Transform _parentTransform;
        [SerializeField] private int _initialPoolSize = 30;
        [SerializeField] private int _maxPoolSize = 100;

        [Header("飘字动画")]
        [SerializeField] private float _popUpDuration = 1f;
        [SerializeField] private float _popUpHeight = 50f;
        [SerializeField] private float _randomOffset = 20f;

        [Header("颜色配置")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _criticalColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _healColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color _trueDamageColor = new Color(0.8f, 0.3f, 1f);
        [SerializeField] private Color _missColor = new Color(0.5f, 0.5f, 0.5f);

        // 对象池
        private Queue<DamagePopUpItem> _pool = new Queue<DamagePopUpItem>();

        // 当前活跃的飘字
        private List<DamagePopUpItem> _activeItems = new List<DamagePopUpItem>();

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            // 预创建对象池
            PreWarmPool();
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        private void PreWarmPool()
        {
            if (_popUpPrefab == null)
            {
                Debug.LogWarning("[DamagePopUpManager] 飘字预制体未设置！");
                return;
            }

            for (int i = 0; i < _initialPoolSize; i++)
            {
                DamagePopUpItem item = CreateItem();
                if (item != null)
                {
                    item.gameObject.SetActive(false);
                    _pool.Enqueue(item);
                }
            }

            Debug.Log($"[DamagePopUpManager] 对象池预热完成: {_initialPoolSize}个");
        }

        /// <summary>
        /// 创建一个飘字对象
        /// </summary>
        private DamagePopUpItem CreateItem()
        {
            if (_popUpPrefab == null) return null;

            GameObject go = Instantiate(_popUpPrefab, _parentTransform != null ? _parentTransform : transform);
            DamagePopUpItem item = go.GetComponent<DamagePopUpItem>();

            if (item == null)
            {
                item = go.AddComponent<DamagePopUpItem>();
                item.InitDefault();
            }

            return item;
        }

        #endregion

        #region 对象池操作

        /// <summary>
        /// 从池中获取一个飘字
        /// </summary>
        private DamagePopUpItem GetItem()
        {
            if (_pool.Count > 0)
            {
                DamagePopUpItem item = _pool.Dequeue();
                item.gameObject.SetActive(true);
                _activeItems.Add(item);
                return item;
            }

            // 池为空，创建新的
            if (_activeItems.Count >= _maxPoolSize)
            {
                // 达到上限，复用最老的
                DamagePopUpItem oldest = _activeItems[0];
                _activeItems.RemoveAt(0);
                oldest.StopAnimation();
                _activeItems.Add(oldest);
                return oldest;
            }

            DamagePopUpItem newItem = CreateItem();
            if (newItem != null)
            {
                _activeItems.Add(newItem);
            }
            return newItem;
        }

        /// <summary>
        /// 归还飘字到池中
        /// </summary>
        private void ReturnItem(DamagePopUpItem item)
        {
            if (item == null) return;

            _activeItems.Remove(item);
            item.gameObject.SetActive(false);
            _pool.Enqueue(item);
        }

        #endregion

        #region 显示飘字

        /// <summary>
        /// 显示伤害飘字
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="damage">伤害值</param>
        /// <param name="popType">飘字类型</param>
        public void ShowDamage(Vector3 worldPos, float damage, DamagePopType popType)
        {
            DamagePopUpItem item = GetItem();
            if (item == null) return;

            // 设置内容
            string text = Mathf.FloorToInt(damage).ToString();
            Color color = GetColorByType(popType);
            float scale = popType == DamagePopType.Critical ? 1.5f : 1f;

            // 随机偏移
            Vector3 randomOffset = new Vector3(
                Random.Range(-_randomOffset, _randomOffset),
                Random.Range(0f, _randomOffset * 0.5f),
                0f
            );

            // 播放动画
            item.Play(text, worldPos + randomOffset, color, scale, _popUpDuration, _popUpHeight, () =>
            {
                ReturnItem(item);
            });
        }

        /// <summary>
        /// 显示普通伤害
        /// </summary>
        public void ShowNormalDamage(Vector3 worldPos, float damage)
        {
            ShowDamage(worldPos, damage, DamagePopType.Normal);
        }

        /// <summary>
        /// 显示暴击伤害
        /// </summary>
        public void ShowCriticalDamage(Vector3 worldPos, float damage)
        {
            ShowDamage(worldPos, damage, DamagePopType.Critical);
        }

        /// <summary>
        /// 显示治疗
        /// </summary>
        public void ShowHeal(Vector3 worldPos, float healAmount)
        {
            ShowDamage(worldPos, healAmount, DamagePopType.Heal);
        }

        /// <summary>
        /// 显示Miss
        /// </summary>
        public void ShowMiss(Vector3 worldPos)
        {
            DamagePopUpItem item = GetItem();
            if (item == null) return;

            item.Play("Miss", worldPos, _missColor, 0.8f, _popUpDuration * 0.7f, _popUpHeight * 0.5f, () =>
            {
                ReturnItem(item);
            });
        }

        /// <summary>
        /// 根据类型获取颜色
        /// </summary>
        private Color GetColorByType(DamagePopType type)
        {
            switch (type)
            {
                case DamagePopType.Critical: return _criticalColor;
                case DamagePopType.Heal: return _healColor;
                case DamagePopType.TrueDamage: return _trueDamageColor;
                case DamagePopType.Miss: return _missColor;
                default: return _normalColor;
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清除所有活跃飘字
        /// </summary>
        public void ClearAll()
        {
            for (int i = _activeItems.Count - 1; i >= 0; i--)
            {
                DamagePopUpItem item = _activeItems[i];
                item.StopAnimation();
                ReturnItem(item);
            }
        }

        protected override void OnDispose()
        {
            ClearAll();
            _pool.Clear();
            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// 单个伤害飘字项
    /// </summary>
    public class DamagePopUpItem : MonoBehaviour
    {
        private Text _text;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        private float _duration;
        private float _height;
        private float _timer;
        private Vector3 _startPos;
        private System.Action _onComplete;
        private bool _isPlaying = false;

        /// <summary>
        /// 默认初始化（代码创建时用）
        /// </summary>
        public void InitDefault()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            _text = GetComponent<Text>();
            if (_text == null)
            {
                _text = gameObject.AddComponent<Text>();
                _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _text.alignment = TextAnchor.MiddleCenter;
                _text.fontSize = 24;
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// 播放飘字动画
        /// </summary>
        public void Play(string text, Vector3 worldPos, Color color, float scale, float duration, float height, System.Action onComplete)
        {
            if (_text != null)
            {
                _text.text = text;
                _text.color = color;
            }

            if (_rectTransform != null)
            {
                _rectTransform.position = worldPos;
                _rectTransform.localScale = Vector3.one * scale;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            _startPos = worldPos;
            _duration = duration;
            _height = height;
            _timer = 0f;
            _onComplete = onComplete;
            _isPlaying = true;
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        public void StopAnimation()
        {
            _isPlaying = false;
            _timer = 0f;
            _onComplete = null;
        }

        private void Update()
        {
            if (!_isPlaying) return;

            _timer += Time.deltaTime;
            float t = _timer / _duration;

            if (t >= 1f)
            {
                _isPlaying = false;
                _onComplete?.Invoke();
                return;
            }

            // 向上移动
            if (_rectTransform != null)
            {
                Vector3 pos = _startPos + Vector3.up * (_height * t);
                _rectTransform.position = pos;
            }

            // 渐隐
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f - t;
            }
        }
    }
}
