using System;
using UnityEngine;
using LostIsland.Core;

namespace LostIsland.Logic.Tower
{
    #region 资源类建筑基类

    /// <summary>
    /// 资源类建筑基类
    /// 周期性产出资源
    /// </summary>
    public abstract class ResourceBuildingBase : BuildingBase
    {
        protected float _productionTimer;

        /// <summary>每秒产出量</summary>
        public float ProductionPerSecond => Config != null ? Config.GetProduction(Level) : 0f;

        public override void Update(float dt)
        {
            base.Update(dt);

            if (!IsActive || ProductionPerSecond <= 0f) return;

            _productionTimer += dt;

            // 每秒产出一次
            if (_productionTimer >= 1f)
            {
                float amount = ProductionPerSecond * _productionTimer;
                ProduceResource(amount);
                _productionTimer = 0f;
            }
        }

        protected abstract void ProduceResource(float amount);
    }

    #endregion

    #region 废料回收站

    /// <summary>
    /// 废料回收站：增加腐肉掉落 + 缓慢产出废料
    /// </summary>
    public class ResourceBuilding_ScrapRecycler : ResourceBuildingBase
    {
        protected override void ProduceResource(float amount)
        {
            // 产出废料（通过事件通知资源系统）
            EventBus.Trigger(new ResourceChangedEvent
            {
                ResourceType = ResourceType.Flesh,
                Delta = Mathf.CeilToInt(amount),
                OldValue = 0,
                NewValue = 0
            });
        }
    }

    #endregion

    #region 食物储藏室

    /// <summary>
    /// 食物储藏室：增加食物上限 + 缓慢产出食物
    /// </summary>
    public class ResourceBuilding_FoodStorage : ResourceBuildingBase
    {
        protected override void ProduceResource(float amount)
        {
            EventBus.Trigger(new ResourceChangedEvent
            {
                ResourceType = ResourceType.Food,
                Delta = Mathf.CeilToInt(amount),
                OldValue = 0,
                NewValue = 0
            });
        }
    }

    #endregion

    #region 晶核提炼器

    /// <summary>
    /// 晶核提炼器：增加晶核掉落概率
    /// </summary>
    public class ResourceBuilding_CrystalRefinery : BuildingBase
    {
        // 晶核提炼器不主动产出，而是通过加成提高掉落率
        // 加成在 ApplyBonuses 中处理
    }

    #endregion

    #region 功能类建筑基类

    /// <summary>
    /// 功能类建筑基类
    /// 主要提供被动加成效果
    /// </summary>
    public abstract class UtilityBuildingBase : BuildingBase
    {
        // 功能建筑主要靠加成效果，在 BuildingBase 中已处理
    }

    #endregion

    #region 发电机

    /// <summary>
    /// 发电机：提供电力
    /// </summary>
    public class UtilityBuilding_Generator : UtilityBuildingBase
    {
        public override void OnBuilt()
        {
            base.OnBuilt();
            // 电力系统会自动重新计算
            TowerManager.Instance?.Power.Recalculate();
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            TowerManager.Instance?.Power.Recalculate();
        }

        public override void OnPostUpgrade()
        {
            base.OnPostUpgrade();
            TowerManager.Instance?.Power.Recalculate();
        }
    }

    #endregion

    #region 医疗室

    /// <summary>
    /// 医疗室：增加生命上限 + 回血
    /// </summary>
    public class UtilityBuilding_MedBay : UtilityBuildingBase
    {
        // 生命上限加成通过 Bonuses 实现
        // 回血效果在夜晚/白天自动生效
    }

    #endregion

    #region 工坊

    /// <summary>
    /// 工坊：增加攻击力
    /// </summary>
    public class UtilityBuilding_Workshop : UtilityBuildingBase
    {
        // 攻击力加成通过 Bonuses 实现
    }

    #endregion

    #region 研究室

    /// <summary>
    /// 研究室：解锁科技/提供研究点
    /// </summary>
    public class UtilityBuilding_ResearchLab : UtilityBuildingBase
    {
        // 研究系统在P7阶段实现
    }

    #endregion

    #region 监控室

    /// <summary>
    /// 监控室：增加防御塔射程
    /// </summary>
    public class UtilityBuilding_Surveillance : UtilityBuildingBase
    {
        // 射程加成通过事件或全局加成实现
    }

    #endregion

    #region 探照灯

    /// <summary>
    /// 探照灯：塔顶固定建筑
    /// 照亮周围区域，增加视野
    /// </summary>
    public class UtilityBuilding_Searchlight : UtilityBuildingBase
    {
        public override void OnBuilt()
        {
            base.OnBuilt();
            Debug.Log("[探照灯] 塔顶探照灯已激活");
        }

        public override void OnNightStart(int day, int wave)
        {
            base.OnNightStart(day, wave);
            // 夜晚探照灯开启
        }
    }

    #endregion
}
