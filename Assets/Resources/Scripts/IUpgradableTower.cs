using UnityEngine;

/// <summary>
/// Shared interface for "towers that can be upgraded or sold".
/// Implemented by Verdant.Combat.Tower (ranged), MeleeTower, CrystalSpringTower and Wildfire.
/// TowerSlot / TowerPlacementManager / TowerUpgradePopup only talk to towers through this
/// interface, so they support every tower type without knowing which one it is.
/// </summary>
public interface IUpgradableTower
{
    string TowerId { get; }              // ID in towers.csv (e.g. T1B1 / T1A1)
    GameObject NextLevelPrefab { get; }  // prefab for the next level (null = max level)
}
