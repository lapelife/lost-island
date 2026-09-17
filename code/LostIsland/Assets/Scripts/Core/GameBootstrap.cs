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
        private static Text _hpText;
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

            // 1.5 确保场景基础环境（相机/光源），没有场景文件也能正常渲染
            EnsureSceneEnvironment();

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

        #region 场景环境

        /// <summary>
        /// 确保场景存在相机与光源（项目无场景文件时自动补齐）
        /// </summary>
        private static void EnsureSceneEnvironment()
        {
            try
            {
                // 相机：没有则创建 Main Camera
                if (Camera.main == null)
                {
                    var camGo = new GameObject("Main Camera");
                    camGo.tag = "MainCamera";
                    var cam = camGo.AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.Skybox;
                    camGo.AddComponent<AudioListener>();
                    camGo.transform.position = new Vector3(0f, 2f, -10f);
                    camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
                    Debug.Log("[Bootstrap] 场景无相机，已自动创建 Main Camera");
                }

                // 光源：没有则创建方向光
                if (UnityEngine.Object.FindObjectOfType<Light>() == null)
                {
                    var lightGo = new GameObject("Directional Light");
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 1.2f;
                    lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    Debug.Log("[Bootstrap] 场景无光源，已自动创建 Directional Light");
                }

                // 地面：没有则创建简单地面
                if (GameObject.Find("Ground") == null)
                {
                    var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ground.name = "Ground";
                    ground.transform.position = new Vector3(0f, -0.5f, 0f);
                    ground.transform.localScale = new Vector3(60f, 1f, 60f);
                    ground.GetComponent<Renderer>().material.color = new Color(0.35f, 0.4f, 0.3f, 1f);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bootstrap] 场景环境创建失败: {e.Message}");
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

            // 背景（很淡的暗色，仅提升文字可读性，不遮挡场景）
            GameObject background = CreatePanel(canvasGo.transform, "Background", new Color(0.06f, 0.07f, 0.09f, 0.25f));
            RectTransform bgRt = background.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            background.GetComponent<Image>().raycastTarget = false;

            // 主菜单面板
            _mainMenuPanel = CreatePanel(canvasGo.transform, "MainMenuPanel", new Color(0f, 0f, 0f, 0f));
            RectTransform mmRt = _mainMenuPanel.GetComponent<RectTransform>();
            mmRt.anchorMin = Vector2.zero;
            mmRt.anchorMax = Vector2.one;
            mmRt.offsetMin = Vector2.zero;
            mmRt.offsetMax = Vector2.zero;
            _mainMenuPanel.GetComponent<Image>().raycastTarget = false;

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

            // 战斗面板（全透明布局容器，不遮挡场景）
            _battlePanel = CreatePanel(canvasGo.transform, "BattlePanel", new Color(0f, 0f, 0f, 0f));
            _battlePanel.SetActive(false);
            _battlePanel.GetComponent<Image>().raycastTarget = false;
            RectTransform bpRt = _battlePanel.GetComponent<RectTransform>();
            bpRt.anchorMin = Vector2.zero;
            bpRt.anchorMax = Vector2.one;
            bpRt.offsetMin = Vector2.zero;
            bpRt.offsetMax = Vector2.zero;

            // 左上角信息面板（半透明底，含波次/生命/腐肉/敌人）
            GameObject infoPanel = CreatePanel(_battlePanel.transform, "InfoPanel", new Color(0f, 0f, 0f, 0.3f));
            SetAnchored(infoPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(460f, 250f));
            infoPanel.GetComponent<Image>().raycastTarget = false;

            _waveText = CreateText(infoPanel.transform, "WaveText", "第 0 / 300 波", 30, TextAnchor.MiddleLeft, Color.white);
            SetAnchored(_waveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -28f), new Vector2(420f, 44f));

            _hpText = CreateText(infoPanel.transform, "HpText", "生命: -/-", 30, TextAnchor.MiddleLeft, new Color(1f, 0.4f, 0.4f, 1f));
            SetAnchored(_hpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -80f), new Vector2(420f, 44f));

            _fleshText = CreateText(infoPanel.transform, "FleshText", "腐肉: 0", 30, TextAnchor.MiddleLeft, new Color(0.5f, 1f, 0.5f, 1f));
            SetAnchored(_fleshText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -132f), new Vector2(420f, 44f));

            _zombieText = CreateText(infoPanel.transform, "ZombieText", "敌人: 0", 30, TextAnchor.MiddleLeft, new Color(1f, 0.6f, 0.6f, 1f));
            SetAnchored(_zombieText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -184f), new Vector2(420f, 44f));

            // 右上角状态面板（半透明底）
            GameObject statePanel = CreatePanel(_battlePanel.transform, "StatePanel", new Color(0f, 0f, 0f, 0.3f));
            SetAnchored(statePanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(420f, 60f));
            statePanel.GetComponent<Image>().raycastTarget = false;

            _stateText = CreateText(statePanel.transform, "StateText", "状态: 准备阶段（白天）", 28, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 1f, 1f));
            RectTransform stRt = _stateText.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero;
            stRt.anchorMax = Vector2.one;
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;

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

            // 点锚点（anchorMin == anchorMax）时 pivot 跟随锚点方向，
            // 保证 anchoredPosition 相对锚点向内偏移，面板/文字不会延伸到屏幕外
            rt.pivot = (anchorMin == anchorMax) ? anchorMin : new Vector2(0.5f, 0.5f);

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
                // 确保生命值满血显示（防御：不依赖属性事件的触发时序）
                _playerEntity.Health.SetCurrentHP(_playerEntity.Health.MaxHP);
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

                // 3.5 创建 3D 占位视觉（玩家/灯塔）
                CreateBattleVisuals();

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

            // 隐藏入夜提示
            Transform hint = _battlePanel.transform.Find("BattleHint");
            if (hint != null) hint.gameObject.SetActive(false);

            // 创建丧尸占位视觉（红色胶囊群，向灯塔推进）
            CreateZombieVisuals();

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

            if (_hpText != null && _playerEntity != null && _playerEntity.Health != null)
            {
                _hpText.text = $"生命: {_playerEntity.Health.CurrentHP:F0} / {_playerEntity.Health.MaxHP:F0}";
            }

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

        #region 3D 占位视觉

        /// <summary>
        /// 创建简单的 3D 占位对象（玩家/灯塔），提供基础视觉反馈
        /// </summary>
        private static void CreateBattleVisuals()
        {
            try
            {
                // 玩家占位：蓝色胶囊
                var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerGo.name = "PlayerVisual";
                playerGo.transform.position = new Vector3(0f, 1f, 0f);
                playerGo.transform.localScale = new Vector3(1.2f, 2f, 1.2f);
                playerGo.GetComponent<Renderer>().material.color = new Color(0.2f, 0.5f, 1f, 1f);
                UnityEngine.Object.DontDestroyOnLoad(playerGo);

                // 灯塔占位：黄色方块（玩家身后）
                var towerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                towerGo.name = "TowerVisual";
                towerGo.transform.position = new Vector3(0f, 0.5f, 8f);
                towerGo.transform.localScale = new Vector3(2f, 1f, 2f);
                towerGo.GetComponent<Renderer>().material.color = new Color(1f, 0.8f, 0.2f, 1f);
                UnityEngine.Object.DontDestroyOnLoad(towerGo);

                Debug.Log("[Bootstrap] 已创建玩家/灯塔 3D 占位视觉");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bootstrap] 创建 3D 占位视觉失败（不影响游戏逻辑）: {e.Message}");
            }
        }

        /// <summary>
        /// 创建丧尸占位视觉（红色胶囊群）
        /// </summary>
        private static void CreateZombieVisuals()
        {
            try
            {
                if (GameObject.Find("ZombieVisuals") != null) return;

                var group = new GameObject("ZombieVisuals");
                UnityEngine.Object.DontDestroyOnLoad(group);

                Vector3[] positions =
                {
                    new Vector3(-2f, 1f, 6f),
                    new Vector3(2f, 1f, 7f),
                    new Vector3(0f, 1f, 8f),
                    new Vector3(-4f, 1f, 9f),
                    new Vector3(4f, 1f, 10f)
                };

                foreach (var pos in positions)
                {
                    var zombie = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    zombie.name = "ZombieVisual";
                    zombie.transform.SetParent(group.transform, false);
                    zombie.transform.position = pos;
                    zombie.transform.localScale = new Vector3(0.8f, 1.4f, 0.8f);
                    zombie.GetComponent<Renderer>().material.color = new Color(0.85f, 0.2f, 0.15f, 1f);
                }

                Debug.Log("[Bootstrap] 已创建 5 只丧尸占位视觉");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Bootstrap] 创建丧尸占位视觉失败（不影响游戏逻辑）: {e.Message}");
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
