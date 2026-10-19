using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// "Backup" (fallback) tower stats stored as an asset (ScriptableObject).
    ///
    /// Note: Verdant's real source of truth is towers.csv (via TowerDatabase).
    /// This asset is only used when testing a single tower, or when the CSV can't be loaded / the ID isn't found
    /// (see Tower.cs — if a matching CSV row exists, it overrides these values).
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData_GreenArrow", menuName = "Verdant/Tower Data (Fallback)")]
    public class TowerData : ScriptableObject
    {
        [Header("General")]
        public string towerName = "Green Arrow";
        [TextArea] public string description = "Basic archer tower. Fires quickly, medium damage.";

        [Header("Attack (fallback — the CSV can override)")]
        public float damage = 10f;
        public float range = 3.5f;
        [Tooltip("Shots per second")]
        public float fireRate = 1.5f;

        [Header("Projectile (Arrow)")]
        public Projectile projectilePrefab;
        public float projectileSpeed = 10f;

        /// <summary>Seconds between shots, calculated from fireRate.</summary>
        public float FireCooldown => 1f / Mathf.Max(0.01f, fireRate);
    }
}
