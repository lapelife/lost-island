// ============================================================
// GameBootstrap.cs
// 游戏启动引导（Bootstrap）
// 使用 RuntimeInitializeOnLoadMethod 自动执行，无需手动挂载场景对象。
// 职责：
//   1. 初始化所有核心管理器（自动创建单例）
//   2. 用代码创建可互动界面（主菜单 -> 战斗HUD）
//   3. 驱动完整战斗流程（开始游戏 -> 入夜 -> 波次进行）
// ============================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using LostIsland.Entity;
using LostIsland.Utils;
using LostIsland.Infrastructure;
using LostIsland.Data.Save;
using LostIsland.Data.Config;
using LostIsland.Logic.Battle;
using LostIsland.Logic.Wave;
using LostIsland.Logic.Rune;
using LostIsland.Logic.Skill;
using LostIsland.Logic.Survivors;
using LostIsland.Logic.Awakening;
using LostIsland.Logic.Tower;
using LostIsland.View.UI;

namespace LostIsland.Core
{
    /// <summary>
    /// 游戏启动引导
    /// </summary>
    public static class GameBootstrap
    {
        #region UI 引用

        private static Canvas _canvas;
        private static GameObject _mainMenuPanel;
        private static GameObject _battlePanel;
        private static Text _titleText;
        private static Text _statusText;
        private static Button _primaryButton;
        private static Text _primaryBtnText;
        private static Text _waveText;
        private static Text _fleshText;
        private static Text _zombieText;
        private static Text _stateText;

        #endregion

        #region 状态

        private static bool _initialized = false;
        private static bool _battleStarted = false;
        private static bool _nightStarted = false;

        private static EntityBase _playerEntity;
        private static EntityBase _towerEntity;
        private static PlayerController _playerCtrl;
        private static TowerController _towerCtrl;

        private static readonly string[] BattleStateNames =
        {
            "未开始", "准备阶段（白天）", "入夜倒计时", "波次开始", "波次进行中", "胜利", "失败"
        };

        #endregion

        #region 启动入口

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_initialized) return;
            _initialized = true;

            Debug.Log("========================================");
            Debug.Log("[Bootstrap] 迷失孤岛 启动引导开始");
            Debug.Log("========================================");

            // 1. 初始化所有核心系统（逐个容错）
            InitSystems();

            // 2. 创建可互动界面
            CreateUI();

            Debug.Log("[Bootstrap] 启动引导完成，进入主菜单");
        }

        #endregion

        #region 系统初始化

        private static void InitSystems()
        {
            InitSystem("GameManager", () => { var _ = GameManager.Instance; });
            InitSystem("TimerManager", () => { var _ = TimerManager.Instance; });
            InitSystem("PoolManager", () => { var _ = PoolManager.Instance; });
            InitSystem("SaveManager", () => { var _ = SaveManager.Instance; });
            InitSystem("ConfigLoader", () => { var _ = ConfigLoader.Instance; });
            InitSystem("RuneManager", () => { var _ = RuneManager.Instance; });
            InitSystem("SkillCardManager", () => { var _ = SkillCardManager.Instance; });
            InitSystem("SurvivorManager", () => { var _ = SurvivorManager.Instance; });
            InitSystem("AwakenManager", () => { var _ = AwakenManager.Instance; });
            InitSystem("TowerManager", () => { var _ = TowerManager.Instance; });
            InitSystem("BuildingBonusManager", () => { var _ = BuildingBonusManager.Instance; });
            InitSystem("WaveManager", () => { var _ = WaveManager.Instance; });
            InitSystem("ZombieSpawner", () => { var _ = ZombieSpawner.Instance; });
            InitSystem("BattleManager", () => { var _ = BattleManager.Instance; });
            InitSystem("DamagePopUpManager", () => { var _ = DamagePopUpManager.Instance; });
        }

        private static void InitSystem(string name, Action init)
        {
            try
            {
                init();
                Debug.Log($"[Bootstrap] {name} 初始化完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] {name} 初始化失败: {e.Message}\n{e.StackTrace}");
            }
        }

        #endregion

        #region UI 创建

        private static Font GetFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }

        private static void CreateUI()
        {
            // EventSystem
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                UnityEngine.Object.DontDestroyOnLoad(esGo);
            }

            // Canvas
            var canvasGo = new GameObject("BootstrapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(canvasGo);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // 背景
            CreatePanel(canvasGo.transform, "Background", new Color(0.06f, 0.07f, 0.09f, 1f));

            // 主菜单面板
            _mainMenuPanel = CreatePanel(canvasGo.transform, "MainMenuPanel", new Color(0f, 0f, 0f, 0f));
            RectTransform mmRt = _mainMenuPanel.GetComponent<RectTransform>();
            mmRt.anchorMin = Vector2.zero;
            mmRt.anchorMax = Vector2.one;
            mmRt.offsetMin = Vector2.zero;
            mmRt.offsetMax = Vector2.zero;

            // 标题
            _titleText = CreateText(_mainMenuPanel.transform, "TitleText", "迷失孤岛", 110, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f, 1f));
            SetAnchored(_titleText.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0f, 0f), new Vector2(1200f, 160f));

            Text subTitle = CreateText(_mainMenuPanel.transform, "SubTitleText", "僵尸塔防 · 幸存者", 40, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f, 1f));
            SetAnchored(subTitle.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0f, 0f), new Vector2(800f, 60f));

            // 状态文本
            _statusText = CreateText(_mainMenuPanel.transform, "StatusText", "正在初始化系统…", 26, TextAnchor.MiddleCenter, new Color(0.6f, 0.65f, 0.7f, 1f));
            SetAnchored(_statusText.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0f, 0f), new Vector2(1400f, 80f));

            // 开始按钮
            _primaryButton = CreateButton(_mainMenuPanel.transform, "StartButton", "开始游戏", 36);
            RectTransform btnRt = _primaryButton.GetComponent<RectTransform>();
            SetAnchored(btnRt, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(0f, 0f), new Vector2(420f, 100f));
            _primaryBtnText = btnRt.Find("Text").GetComponent<Text>();

            // 战斗面板
            _battlePanel = CreatePanel(canvasGo.transform, "BattlePanel", new Color(0f, 0f, 0f, 0.55f));
            _battlePanel.SetActive(false);
            RectTransform bpRt = _battlePanel.GetComponent<RectTransform>();
            bpRt.anchorMin = Vector2.zero;
            bpRt.anchorMax = Vector2.one;
            bpRt.offsetMin = Vector2.zero;
            bpRt.offsetMax = Vector2.zero;

            // 顶部信息条
            _waveText = CreateText(_battlePanel.transform, "WaveText", "第 0 / 300 波", 34, TextAnchor.MiddleLeft, Color.white);
            SetAnchored(_waveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -60f), new Vector2(420f, 60f));

            _fleshText = CreateText(_battlePanel.transform, "FleshText", "腐肉: 0", 34, TextAnchor.MiddleLeft, new Color(0.5f, 1f, 0.5f, 1f));
            SetAnchored(_fleshText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -140f), new Vector2(420f, 60f));

            _zombieText = CreateText(_battlePanel.transform, "ZombieText", "敌人: 0", 34, TextAnchor.MiddleLeft, new Color(1f, 0.6f, 0.6f, 1f));
            SetAnchored(_zombieText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -220f), new Vector2(420f, 60f));

            _stateText = CreateText(_battlePanel.transform, "StateText", "状态: 准备阶段（白天）", 34, TextAnchor.MiddleRight, new Color(0.8f, 0.8f, 1f, 1f));
            SetAnchored(_stateText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -60f), new Vector2(600f, 60f));

            // 中央提示（战斗说明）
            Text battleHint = CreateText(_battlePanel.transform, "BattleHint", "点击下方按钮进入夜晚\n丧尸将在夜晚来袭", 28, TextAnchor.MiddleCenter, new Color(0.75f, 0.75f, 0.75f, 1f));
            SetAnchored(battleHint.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0f, 0f), new Vector2(900f, 100f));

            // 底部主按钮（入夜/下一波）
            var battleBtn = CreateButton(_battlePanel.transform, "NightButton", "进入夜晚", 36);
            SetAnchored(battleBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(420f, 100f));

            // 按钮逻辑（主菜单 -> 开始游戏）
            _primaryButton.onClick.AddListener(OnStartGameClicked);

            // 每帧刷新驱动
            var driverGo = new GameObject("BootstrapDriver");
            driverGo.transform.SetParent(canvasGo.transform, false);
            driverGo.AddComponent<BootstrapDriver>();

            _statusText.text = "系统初始化完成，点击「开始游戏」进入战斗";
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.5f, 0.85f, 1f);
            image.sprite = null;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.95f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.7f, 1f);
            btn.colors = colors;

            var text = CreateText(go.transform, "Text", label, fontSize, TextAnchor.MiddleCenter, Color.white);
            RectTransform trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            return btn;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        #endregion

        #region 战斗流程

        private static void OnStartGameClicked()
        {
            try
            {
                // 1. 创建玩家实体
                _playerEntity = new EntityBase();
                _playerEntity.Initialize(1, EntityType.Player, "幸存者");
                _playerEntity.Attribute.SetBaseValue(AttrType.MaxHP, 1000f);
                _playerEntity.Attribute.SetBaseValue(AttrType.ATK, 100f);
                _playerEntity.Attribute.SetBaseValue(AttrType.MoveSpeed, 5f);
                _playerCtrl = new PlayerController(_playerEntity);

                // 2. 创建灯塔实体
                _towerEntity = new EntityBase();
                _towerEntity.Initialize(2, EntityType.Tower, "灯塔");
                _towerCtrl = new TowerController(_towerEntity);
                _towerCtrl.InitByDay(1);

                // 3. 初始化战斗
                BattleManager.Instance.InitBattle(1, _playerEntity, _towerEntity);
                WaveManager.Instance.StartDay(1);
                GameManager.Instance.ChangeState(GameState.Battle);

                // 4. 切换 UI
                _mainMenuPanel.SetActive(false);
                _battlePanel.SetActive(true);
                _battleStarted = true;

                // 5. 绑定入夜按钮
                Button nightBtn = _battlePanel.transform.Find("NightButton").GetComponent<Button>();
                nightBtn.onClick.AddListener(OnNightButtonClicked);

                Debug.Log("[Bootstrap] 战斗已初始化，等待入夜");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] 开始游戏失败: {e.Message}\n{e.StackTrace}");
                _statusText.text = $"开始游戏失败: {e.Message}";
            }
        }

        private static void OnNightButtonClicked()
        {
            if (_nightStarted) return;
            _nightStarted = true;

            BattleManager.Instance.StartNight();

            Button nightBtn = _battlePanel.transform.Find("NightButton").GetComponent<Button>();
            Text nightBtnText = nightBtn.transform.Find("Text").GetComponent<Text>();
            nightBtnText.text = "战斗进行中…";
            nightBtn.interactable = false;

            Debug.Log("[Bootstrap] 进入夜晚，战斗开始");
        }

        /// <summary>
        /// 每帧刷新战斗 HUD（由 BootstrapDriver 驱动）
        /// </summary>
        private static void RefreshHUD()
        {
            if (!_battleStarted || BattleManager.Instance == null) return;

            var battleMgr = BattleManager.Instance;

            if (_waveText != null)
                _waveText.text = $"第 {battleMgr.CurrentWave} / 300 波";

            if (_fleshText != null)
                _fleshText.text = $"腐肉: {battleMgr.CurrentFlesh}";

            if (_zombieText != null)
                _zombieText.text = $"敌人: {battleMgr.GetAliveZombieCount()}";

            if (_stateText != null)
            {
                int stateIdx = (int)battleMgr.CurrentState;
                string stateName = stateIdx >= 0 && stateIdx < BattleStateNames.Length
                    ? BattleStateNames[stateIdx]
                    : battleMgr.CurrentState.ToString();

                if (battleMgr.CurrentState == BattleState.PreNight)
                {
                    stateName += $"  {battleMgr.NextWaveCountdown:F0}s";
                }

                _stateText.text = $"状态: {stateName}";
            }
        }

        #endregion

        #region 驱动组件

        /// <summary>
        /// 每帧刷新驱动（挂在 Canvas 下的轻量组件）
        /// </summary>
        private class BootstrapDriver : MonoBehaviour
        {
            private void Update()
            {
                RefreshHUD();
            }
        }

        #endregion
    }
}
