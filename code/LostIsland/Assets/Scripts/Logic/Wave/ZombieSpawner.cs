using System;
using System.Collections.Generic;
using UnityEngine;
using LostIsland.Entity;
using LostIsland.Core;

namespace LostIsland.Logic.Wave
{
    /// <summary>
    /// 丧尸生成器：管理丧尸的创建、回收、对象池
    /// 采用分层对象池：每种丧尸类型独立池
    /// </summary>
    public class ZombieSpawner : MonoSingleton<ZombieSpawner>
    {
        #region 对象池配置

        [Header("对象池配置")]
        [Tooltip("每种丧尸初始池大小")]
        [SerializeField] private int _initialPoolSize = 10;

        [Tooltip("每种丧尸最大池大小")]
        [SerializeField] private int _maxPoolSize = 100;

        [Tooltip("扩容步长")]
        [SerializeField] private int _expandStep = 5;

        #endregion

        #region 出生点

        [Header("出生点")]
        [Tooltip("出生点列表")]
        [SerializeField] private SpawnPoint[] _spawnPoints;

        [Tooltip("每波使用的出生点数量（随机选择）")]
        [SerializeField] private int _spawnPointsPerWave = 4;

        #endregion

        #region 对象池

        // 按类型存储的对象池
        private Dictionary<ZombieType, Queue<ZombieController>> _zombiePools
            = new Dictionary<ZombieType, Queue<ZombieController>>();

        // 活跃丧尸列表
        private List<ZombieController> _activeZombies = new List<ZombieController>();

        // BOSS（不进对象池，单独管理）
        private BossController _activeBoss = null;

        // 实体ID计数器
        private int _entityIdCounter = 1000;

        #endregion

        #region 引用

        private WaveManager _waveManager;
        private Vector3 _towerPosition;

        #endregion

        #region 属性

        /// <summary>
        /// 活跃丧尸数量
        /// </summary>
        public int ActiveZombieCount => _activeZombies.Count;

        /// <summary>
        /// 是否有活跃BOSS
        /// </summary>
        public bool HasActiveBoss => _activeBoss != null;

        /// <summary>
        /// 活跃BOSS
        /// </summary>
        public BossController ActiveBoss => _activeBoss;

        /// <summary>
        /// 获取所有活跃丧尸
        /// </summary>
        public List<ZombieController> ActiveZombies => _activeZombies;

        #endregion

        #region 初始化

        protected override void OnInit()
        {
            base.OnInit();

            // 初始化出生点（默认8个，分布在圆周上）
            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                InitDefaultSpawnPoints();
            }
        }

        /// <summary>
        /// 初始化默认出生点（8个，均匀分布在半径10的圆上）
        /// </summary>
        private void InitDefaultSpawnPoints()
        {
            int count = 8;
            float radius = 10f;
            _spawnPoints = new SpawnPoint[count];

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                _spawnPoints[i] = new SpawnPoint
                {
                    Name = $"Spawn_{i}",
                    Position = pos,
                    Radius = 1f,
                    IsActive = true
                };
            }

            Debug.Log($"[ZombieSpawner] 初始化 {count} 个默认出生点");
        }

        /// <summary>
        /// 设置波次管理器
        /// </summary>
        public void SetWaveManager(WaveManager waveManager)
        {
            _waveManager = waveManager;
        }

        /// <summary>
        /// 设置灯塔位置
        /// </summary>
        public void SetTowerPosition(Vector3 position)
        {
            _towerPosition = position;
        }

        #endregion

        #region 对象池操作

        /// <summary>
        /// 从池中获取一个丧尸
        /// </summary>
        public ZombieController GetZombie(ZombieType type)
        {
            // 确保池存在
            EnsurePoolExists(type);

            var pool = _zombiePools[type];
            ZombieController zombie = null;

            if (pool.Count > 0)
            {
                zombie = pool.Dequeue();
                zombie.Reset();
            }
            else
            {
                // 池为空，创建新的
                zombie = CreateZombie(type);
            }

            _activeZombies.Add(zombie);
            return zombie;
        }

        /// <summary>
        /// 回收丧尸到池中
        /// </summary>
        public void ReturnZombie(ZombieController zombie)
        {
            if (zombie == null) return;

            _activeZombies.Remove(zombie);

            ZombieType type = zombie.ZombieType;
            EnsurePoolExists(type);

            var pool = _zombiePools[type];
            if (pool.Count < _maxPoolSize)
            {
                zombie.Reset();
                pool.Enqueue(zombie);
            }
            // 超过最大容量则丢弃（让GC处理）
        }

        /// <summary>
        /// 确保池存在
        /// </summary>
        private void EnsurePoolExists(ZombieType type)
        {
            if (!_zombiePools.ContainsKey(type))
            {
                _zombiePools[type] = new Queue<ZombieController>();

                // 预热池
                for (int i = 0; i < _initialPoolSize; i++)
                {
                    var zombie = CreateZombie(type);
                    zombie.Reset();
                    _zombiePools[type].Enqueue(zombie);
                }
            }
        }

        /// <summary>
        /// 创建一个新的丧尸
        /// </summary>
        private ZombieController CreateZombie(ZombieType type)
        {
            var config = ZombieConfigFactory.GetConfig(type);

            // 创建实体
            int entityId = ++_entityIdCounter;
            EntityBase entity = new EntityBase();
            entity.Initialize(entityId, EntityType.Zombie, config.GetDisplayName());
            entity.SetLevel(1);

            // 创建控制器
            ZombieController controller = new ZombieController(entity, config);

            // 监听死亡回收
            controller.OnDeathCompleted += OnZombieDeathCompleted;

            return controller;
        }

        #endregion

        #region BOSS管理

        /// <summary>
        /// 生成BOSS
        /// </summary>
        public BossController SpawnBoss(ZombieType bossType, BossTier tier, int wave, int day)
        {
            if (_activeBoss != null)
            {
                Debug.LogWarning("[ZombieSpawner] 已有活跃BOSS，先生成新BOSS");
            }

            var config = ZombieConfigFactory.GetConfig(bossType);

            int entityId = ++_entityIdCounter;
            EntityBase entity = new EntityBase();
            entity.Initialize(entityId, EntityType.Boss, config.GetDisplayName());
            entity.SetLevel(1);

            BossController boss = new BossController(entity, config, config.GetDisplayName(), tier);
            boss.TowerPosition = _towerPosition;

            // 计算属性
            float hpMult = 1f;
            float atkMult = 1f;
            float defMult = 1f;
            if (_waveManager != null)
            {
                hpMult = _waveManager.GetHpMultiplier(wave, day);
                atkMult = _waveManager.GetAtkMultiplier(wave, day);
                defMult = _waveManager.GetDefMultiplier(wave, day);
            }

            boss.InitAttributes(wave, day, hpMult, atkMult, defMult);

            // 设置出生位置（前方中央）
            Vector3 spawnPos = _towerPosition + Vector3.forward * 8f;
            boss.Spawn(spawnPos);

            _activeBoss = boss;

            Debug.Log($"[ZombieSpawner] BOSS生成: {config.GetDisplayName()} (Tier={tier}), HP={entity.Health.MaxHP:F0}");

            return boss;
        }

        /// <summary>
        /// 清除BOSS
        /// </summary>
        public void ClearBoss()
        {
            if (_activeBoss != null)
            {
                _activeBoss.Reset();
                _activeBoss = null;
            }
        }

        #endregion

        #region 波次生成

        /// <summary>
        /// 生成一波丧尸
        /// </summary>
        /// <param name="wave">波数</param>
        /// <returns>生成的丧尸列表</returns>
        public List<ZombieController> SpawnWave(int wave, int day)
        {
            List<ZombieController> result = new List<ZombieController>();

            if (_waveManager == null)
            {
                Debug.LogWarning("[ZombieSpawner] WaveManager未设置");
                return result;
            }

            // 获取丧尸类型列表
            var zombieTypes = _waveManager.GenerateWaveZombies(wave);

            // 选择出生点
            var selectedSpawnPoints = SelectSpawnPoints(_spawnPointsPerWave);

            // 属性成长倍率
            float hpMult = _waveManager.GetHpMultiplier(wave, day);
            float atkMult = _waveManager.GetAtkMultiplier(wave, day);
            float defMult = _waveManager.GetDefMultiplier(wave, day);

            // 生成丧尸
            for (int i = 0; i < zombieTypes.Count; i++)
            {
                var type = zombieTypes[i];
                var zombie = GetZombie(type);

                // 初始化属性
                zombie.InitAttributes(wave, day, hpMult, atkMult, defMult);
                zombie.TowerPosition = _towerPosition;

                // 选择出生点
                var spawnPoint = selectedSpawnPoints[i % selectedSpawnPoints.Count];
                Vector3 spawnPos = spawnPoint.Position + GetRandomOffset(spawnPoint.Radius);
                zombie.Spawn(spawnPos);

                result.Add(zombie);
            }

            Debug.Log($"[ZombieSpawner] 第{wave}波生成 {result.Count} 只丧尸");
            return result;
        }

        /// <summary>
        /// 随机选择出生点
        /// </summary>
        private List<SpawnPoint> SelectSpawnPoints(int count)
        {
            List<SpawnPoint> activePoints = new List<SpawnPoint>();
            foreach (var sp in _spawnPoints)
            {
                if (sp.IsActive) activePoints.Add(sp);
            }

            count = Mathf.Min(count, activePoints.Count);

            // Fisher-Yates 洗牌
            for (int i = activePoints.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = activePoints[i];
                activePoints[i] = activePoints[j];
                activePoints[j] = temp;
            }

            return activePoints.GetRange(0, count);
        }

        /// <summary>
        /// 获取随机偏移
        /// </summary>
        private Vector3 GetRandomOffset(float radius)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float dist = UnityEngine.Random.Range(0f, radius);
            return new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
        }

        #endregion

        #region 死亡回调

        /// <summary>
        /// 丧尸死亡完成回调
        /// </summary>
        private void OnZombieDeathCompleted(ZombieController zombie)
        {
            // 触发击杀事件
            int fleshDrop = zombie.Config != null ? zombie.Config.FleshDrop : 1;
            int crystalDrop = 0;
            if (zombie.Config != null && UnityEngine.Random.value < zombie.Config.CrystalDropChance)
            {
                crystalDrop = zombie.Config.CrystalDropAmount;
            }

            EventBus.Trigger(new ZombieKilledEvent
            {
                ZombieType = zombie.ZombieType,
                FleshDrop = fleshDrop,
                CrystalDrop = crystalDrop,
                Position = zombie.MoveComp.Position
            });

            // 回收到对象池
            ReturnZombie(zombie);
        }

        #endregion

        #region 更新

        private void Update()
        {
            float dt = Time.deltaTime;

            // 更新所有活跃丧尸
            for (int i = _activeZombies.Count - 1; i >= 0; i--)
            {
                var zombie = _activeZombies[i];
                zombie.Update(dt);
            }

            // 更新BOSS
            if (_activeBoss != null)
            {
                _activeBoss.Update(dt);
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理所有活跃丧尸
        /// </summary>
        public void ClearAllActive()
        {
            for (int i = _activeZombies.Count - 1; i >= 0; i--)
            {
                var zombie = _activeZombies[i];
                zombie.Reset();
                ReturnZombie(zombie);
            }

            _activeZombies.Clear();
            ClearBoss();
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearPools()
        {
            ClearAllActive();
            _zombiePools.Clear();
            _entityIdCounter = 1000;
        }

        protected override void OnDispose()
        {
            ClearPools();
            base.OnDispose();
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 设置出生点
        /// </summary>
        public void SetSpawnPoints(SpawnPoint[] points)
        {
            _spawnPoints = points;
        }

        /// <summary>
        /// 获取出生点数量
        /// </summary>
        public int SpawnPointCount => _spawnPoints != null ? _spawnPoints.Length : 0;

        #endregion
    }
}
