// ============================================================
// EventTypes.cs
// 核心事件类型定义
// 所有游戏事件集中定义在这里，便于查找和管理
// ============================================================

using UnityEngine;
using LostIsland.Logic.Wave;
using LostIsland.Logic.Tower;
using LostIsland.Entity;

namespace LostIsland.Core
{
    // ==============================================
    // 游戏全局事件
    // ==============================================

    public struct GameStateChangedEvent : IEvent
    {
        public GameState PreviousState;
        public GameState NewState;
    }

    public struct GamePausedEvent : IEvent
    {
        public bool IsPaused;
    }

    public struct SceneLoadStartedEvent : IEvent
    {
        public string SceneName;
    }

    public struct SceneLoadCompleteEvent : IEvent
    {
        public string SceneName;
    }

    // ==============================================
    // 战斗事件
    // ==============================================

    public struct BattleStateChangedEvent : IEvent
    {
        public BattleState PreviousState;
        public BattleState NewState;
    }

    public struct WaveStartEvent : IEvent
    {
        public int WaveNumber;
        public int TotalWaves;
    }

    public struct WaveClearedEvent : IEvent
    {
        public int WaveNumber;
    }

    public struct BossWaveStartEvent : IEvent
    {
        public int WaveNumber;
        public string BossName;
        public BossTier Tier;
    }

    public struct BossDefeatedEvent : IEvent
    {
        public int WaveNumber;
        public string BossName;
    }

    public struct BattleVictoryEvent : IEvent
    {
        public int TotalWavesCleared;
        public int DayNumber;
    }

    public struct BattleDefeatEvent : IEvent
    {
        public int WaveReached;
        public int DayNumber;
    }

    public struct DamageDealtEvent : IEvent
    {
        public float Damage;
        public bool IsCritical;
        public Vector3 Position;
    }

    public struct PlayerHpChangedEvent : IEvent
    {
        public float CurrentHp;
        public float MaxHp;
        public float HpPercent;
    }

    public struct TowerHpChangedEvent : IEvent
    {
        public float CurrentHp;
        public float MaxHp;
        public float HpPercent;
    }

    public struct RageValueChangedEvent : IEvent
    {
        public float CurrentRage;
        public float MaxRage;
        public float RagePercent;
    }

    public struct RageModeActivatedEvent : IEvent
    {
        public float Duration;
    }

    public struct RageModeEndedEvent : IEvent
    {
    }

    public struct FleshChangedEvent : IEvent
    {
        public int CurrentFlesh;
        public int Delta;
    }

    public struct ZombieKilledEvent : IEvent
    {
        public ZombieType ZombieType;
        public int FleshDrop;
        public int CrystalDrop;
        public Vector3 Position;
    }

    // ==============================================
    // 建筑/塔内事件
    // ==============================================

    public struct FloorUnlockedEvent : IEvent
    {
        public int FloorIndex;
        public string FloorName;
    }

    public struct BuildingBuiltEvent : IEvent
    {
        public int FloorIndex;
        public int SlotIndex;
        public BuildingType BuildingType;
        public int Level;
    }

    public struct BuildingUpgradedEvent : IEvent
    {
        public int FloorIndex;
        public int SlotIndex;
        public BuildingType BuildingType;
        public int NewLevel;
    }

    public struct BuildingBonusAppliedEvent : IEvent
    {
        public BuildingType BuildingType;
        public AttrType AttrType;
        public bool IsFlat;
        public float Value;
        public AttrLayer Layer;
        public bool IsAdding;
    }

    public struct PowerChangedEvent : IEvent
    {
        public float MaxPower;
        public float UsedPower;
        public float RemainingPower;
    }

    public struct TowerBonusChangedEvent : IEvent
    {
        public AttrType AttrType;
        public float PctBonus;
        public float FlatBonus;
    }

    // ==============================================
    // 资源事件
    // ==============================================

    public struct ResourceChangedEvent : IEvent
    {
        public ResourceType ResourceType;
        public int OldValue;
        public int NewValue;
        public int Delta;
    }

    // ==============================================
    // UI事件
    // ==============================================

    public struct UIPanelOpenedEvent : IEvent
    {
        public string PanelName;
    }

    public struct UIPanelClosedEvent : IEvent
    {
        public string PanelName;
    }

    // ==============================================
    // 觉醒事件
    // ==============================================

    public struct AwakeningLevelUpEvent : IEvent
    {
        public int NewLevel;
        public float Multiplier;
    }

    public struct AXPChangedEvent : IEvent
    {
        public long CurrentAXP;
        public long AXPToNext;
        public long Delta;
    }
}
