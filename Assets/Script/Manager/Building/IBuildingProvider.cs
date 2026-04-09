using System.Collections.Generic;
using UnityEngine;

namespace Script.Manager.Building
{
    public interface IBuildingProvider
    {
        GameObject currentTownCenter { get; set; }
        List<TownCenterBuildingLimits> TownCenterConfigs { get; }
        
        void UpdateAvailableBuildings();
        int CountBuiltBuildings(BuildingType type);
        bool CanBuildingBeUpgraded(BuildingType buildingType, int currentTier);
        int GetMaxAllowedTier(BuildingType buildingType);
    }

    public interface IPlacementProvider
    {
        void StartPlacement(GameObject structurePrefab);
        bool TryPlaceBuilding();
        void CancelPlacement();
    }
}
