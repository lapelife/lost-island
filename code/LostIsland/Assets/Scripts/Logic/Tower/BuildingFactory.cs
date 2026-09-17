using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostIsland.Logic.Tower
{
    /// <summary>
    /// 建筑工厂
    /// 根据建筑类型创建对应的建筑实例
    /// </summary>
    public static class BuildingFactory
    {
        /// <summary>
        /// 创建建筑实例
        /// </summary>
        public static BuildingBase CreateBuilding(BuildingType type)
        {
            switch (type)
            {
                // 防御类
                case BuildingType.MachineGun:
                    return new DefenseTower_MachineGun();
                case BuildingType.Cannon:
                    return new DefenseTower_Cannon();
                case BuildingType.ArrowTower:
                    return new DefenseTower_Arrow();
                case BuildingType.TeslaTower:
                    return new DefenseTower_Tesla();

                // 资源类
                case BuildingType.ScrapRecycler:
                    return new ResourceBuilding_ScrapRecycler();
                case BuildingType.FoodStorage:
                    return new ResourceBuilding_FoodStorage();
                case BuildingType.CrystalRefinery:
                    return new ResourceBuilding_CrystalRefinery();

                // 功能类
                case BuildingType.Generator:
                    return new UtilityBuilding_Generator();
                case BuildingType.MedBay:
                    return new UtilityBuilding_MedBay();
                case BuildingType.Workshop:
                    return new UtilityBuilding_Workshop();
                case BuildingType.ResearchLab:
                    return new UtilityBuilding_ResearchLab();
                case BuildingType.SurveillanceRoom:
                    return new UtilityBuilding_Surveillance();
                case BuildingType.Searchlight:
                    return new UtilityBuilding_Searchlight();

                default:
                    Debug.LogWarning($"[BuildingFactory] 未知建筑类型: {type}");
                    return null;
            }
        }

        /// <summary>
        /// 创建建筑并初始化
        /// </summary>
        public static BuildingBase CreateAndInitialize(BuildingType type, int level, int floorIndex, int slotIndex)
        {
            var building = CreateBuilding(type);
            if (building == null) return null;

            var config = BuildingConfigFactory.GetConfig(type);
            building.Initialize(config, level, floorIndex, slotIndex);

            return building;
        }

        /// <summary>
        /// 获取建筑分类
        /// </summary>
        public static BuildingCategory GetCategory(BuildingType type)
        {
            var config = BuildingConfigFactory.GetConfig(type);
            return config != null ? config.Category : BuildingCategory.Utility;
        }

        /// <summary>
        /// 判断是否是防御类建筑
        /// </summary>
        public static bool IsDefense(BuildingType type)
        {
            return GetCategory(type) == BuildingCategory.Defense;
        }

        /// <summary>
        /// 判断是否是资源类建筑
        /// </summary>
        public static bool IsResource(BuildingType type)
        {
            return GetCategory(type) == BuildingCategory.Resource;
        }

        /// <summary>
        /// 判断是否是功能类建筑
        /// </summary>
        public static bool IsUtility(BuildingType type)
        {
            return GetCategory(type) == BuildingCategory.Utility;
        }
    }
}
