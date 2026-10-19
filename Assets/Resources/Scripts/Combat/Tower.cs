using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Verdant.Combat
{
    /// <summary>
    /// The brain of a ranged tower (Greenshot): detect enemies in range -> pick a target -> turn the head to aim -> fire at its rate of fire.
    ///
    /// Stats (damage / range / atkSpeed) come from towers.csv through TowerDatabase,
    /// looked up by towerId — if the CSV can't be loaded it falls back to the values in fallbackData.
    /// Values the CSV doesn't have (projectile, targeting mode) are set directly on this component.
    /// </summary>
    [DisallowMultipleComponent]
    public class Tower : MonoBehaviour, IUpgradableTower   // implements the upgrade interface (TowerId / NextLevelPrefab)
    {
        [Header("CSV reference")]
        [Tooltip("This tower's ID in towers.csv, e.g. T1A1 — Damage/Range/Atk Speed are read from this row")]
        [SerializeField] private string towerId = "T1A1";

        [Tooltip("Backup values if the CSV can't be loaded or the ID isn't found (can be empty if you're sure the CSV exists)")]
        [SerializeField] private TowerData fallbackData;

        [Header("Projectile (not in the CSV — set here)")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;

        [Header("Targeting")]
        [SerializeField] private TargetingMode targeting = TargetingMode.Closest;

        [Header("Scene references")]
        [Tooltip("Where arrows are fired from (can be empty — the tower's position is used instead)")]
        [SerializeField] private Transform muzzle;
        [Tooltip("The tower head that turns to follow the target (can be empty if it shouldn't turn)")]
        [SerializeField] private Transform rotatePart;
        [Tooltip("Enemy layer — set to Enemy (if left empty, Enemy is assumed automatically)")]
        [SerializeField] private LayerMask enemyLayer;

        // ── Runtime stats (from the CSV) ──
        private float damage;
        private float range;
        private float fireCooldown;   // seconds between shots

        private float cooldownTimer;
        private IDamageable currentTarget;

        // Non-allocating List + ContactFilter2D overload (recommended for Unity 6)
        private readonly List<Collider2D> hits = new List<Collider2D>();
        private ContactFilter2D filter;

        private bool _statsReady;    // stats are ready to use (from the CSV or the fallback)
        private bool _csvResolved;   // tried the CSV once the DB finished loading (stop retrying)

        // rotatePart's position relative to the muzzle at start (world-space offset + starting rotation).
        // Used to make rotatePart "orbit" around the muzzle (a fixed pivot) when the tower turns to aim.
        private Vector3 rotatePartOffsetFromMuzzle;
        private Quaternion rotatePartInitialRotation;
        private bool hasRotateOrbit;

        private void Awake()
        {
            if (rotatePart != null && muzzle != null)
            {
                rotatePartOffsetFromMuzzle = rotatePart.position - muzzle.position;
                rotatePartInitialRotation = rotatePart.rotation;
                hasRotateOrbit = true;
            }

            // enemyLayer left empty -> assume Enemy (in case it was forgotten in the Inspector and the tower never hits anything)
            if (enemyLayer.value == 0)
            {
                enemyLayer = LayerMask.GetMask("Enemy");
                Debug.LogWarning($"[Tower] '{name}' enemyLayer is not set — using the 'Enemy' layer automatically", this);

                // If no layer called "Enemy" exists in Project Settings, GetMask silently returns 0
                // and OverlapCircle never finds an enemy — warn loudly here.
                if (enemyLayer.value == 0)
                {
                    Debug.LogError($"[Tower] '{name}' could not find a layer named 'Enemy' at all — " +
                                   $"check the layer name in Project Settings, or set enemyLayer in the Inspector yourself", this);
                }
            }

            filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = true; // also detect the enemies' trigger colliders

            ApplyFallback();           // work straight away, even before the CSV is ready
        }

        private void Start() => TryLoadStatsFromCsv();

        // ── API for the upgrade system (upgrade by swapping prefabs) ──
        [Header("Upgrade")]
        [Tooltip("Prefab for the next tower level — leave empty = max level (can't be upgraded further)")]
        [SerializeField] private GameObject nextLevelPrefab;

        public string TowerId => towerId;
        public GameObject NextLevelPrefab => nextLevelPrefab;

        // ── Loading stats ──

        private void ApplyFallback()
        {
            if (fallbackData == null) return;

            damage       = fallbackData.damage;
            range        = fallbackData.range;
            fireCooldown = fallbackData.FireCooldown;
            if (projectilePrefab == null) projectilePrefab = fallbackData.projectilePrefab;
            if (projectileSpeed <= 0f)    projectileSpeed  = fallbackData.projectileSpeed;

            _statsReady = true;
        }

        /// <summary>
        /// Tries to read the stats from TowerDatabase — can be called repeatedly from Update until the DB has loaded.
        /// (Towers placed at runtime through TowerSlot always load in time; towers already placed in the scene
        ///  may Start before TowerDatabase.Start, so they need to retry.)
        /// </summary>
        private void TryLoadStatsFromCsv()
        {
            if (TowerDatabase.Instance == null || !TowerDatabase.Instance.IsLoaded)
                return;   // DB not ready yet — try again in Update

            _csvResolved = true;   // the DB has loaded — stop retrying whether or not the row exists

            var rec = TowerDatabase.Instance.GetByID(towerId);
            if (rec == null)
            {
                Debug.LogError($"[Tower] tower ID '{towerId}' not found in towers.csv — " +
                               $"'{name}' using the fallback values instead", this);
                return;
            }
            else
            {
                Debug.Log($"[Tower] '{name}' loaded stats from the CSV, ID='{towerId}'", this);
            }

            damage       = rec.damage;
            range        = rec.range;
            if (rec.atkSpeed > 0f) fireCooldown = rec.atkSpeed;   // atkSpeed is already seconds per shot
            _statsReady  = true;
        }

        private void Update()
        {
            if (!_csvResolved) TryLoadStatsFromCsv();   // retry until the DB is ready
            if (!_statsReady || range <= 0f) return;

            cooldownTimer -= Time.deltaTime;

            AcquireTarget();
            AimAtTarget();

            if (currentTarget != null && cooldownTimer <= 0f)
            {
                Fire();
                cooldownTimer = fireCooldown;
            }
        }

        // ---------- Picking a target ----------
        private void AcquireTarget()
        {
            // Keep the current target while it's still in range and alive (avoids switching targets too often)
            if (IsValidTarget(currentTarget)) return;
            Debug.Log($"[Tower] '{name}' previous target is no longer valid → finding a new target", this);
            currentTarget = FindBestTarget();
        }

        private bool IsValidTarget(IDamageable t)
        {
            if (t == null || !t.IsAlive) return false;
            float sqr = ((Vector2)(t.Transform.position - transform.position)).sqrMagnitude;
            return sqr <= range * range;
        }

        // Scores every living enemy in range by the targeting mode and returns the best one (or null).
        private IDamageable FindBestTarget()
        {
            int count = Physics2D.OverlapCircle(transform.position, range, filter, hits);

            // Temporary debug: helps track down "No Enemies Detected"
            // If count is always 0 -> check enemyLayer / the Physics2D collision matrix / the enemy's collider
            Debug.Log($"[Tower] '{name}' OverlapCircle: range={range}, enemyLayerMask={enemyLayer.value}, hitCount={count}", this);

            IDamageable best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                var dmg = hits[i].GetComponentInParent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;

                float score = ScoreTarget(dmg);
                if (score > bestScore)
                {
                    best = dmg;
                    bestScore = score;
                }
            }

            Debug.Log($"[Tower] '{name}' found {count} enemies in range → chose target '{best?.Transform.name}'", this);
            return best;
        }

        // Turns the targeting mode into a "score" — higher = more likely to be chosen
        private float ScoreTarget(IDamageable t)
        {
            switch (targeting)
            {
                case TargetingMode.First:     return t.PathProgress;
                case TargetingMode.Last:      return -t.PathProgress;
                case TargetingMode.Strongest: return t.CurrentHealth;
                default: // Closest — nearer = higher score
                    return -Vector2.Distance(transform.position, t.Transform.position);
            }
        }

        // ---------- Turning the tower head ----------
        private void AimAtTarget()
        {
            if (currentTarget == null || rotatePart == null) return;

            Vector2 dir = (Vector2)(currentTarget.Transform.position - rotatePart.position);
            if (dir.sqrMagnitude < 0.0001f) return;

            // -90 because the sprite is assumed to point up (+Y) by default
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Quaternion newRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            DOTween.Kill(rotatePart);   // stops old tweens piling up when the tower switches targets quickly
            rotatePart.DORotateQuaternion(newRotation, 0.1f).SetEase(Ease.OutBounce);   // turn toward the enemy (not instantly -> looks natural)

            // Make rotatePart orbit around the muzzle (a fixed pivot) by the angle turned from the start.
            // The muzzle always stays put while rotatePart swings around it.
            if (hasRotateOrbit)
            {
                Quaternion deltaRotation = newRotation * Quaternion.Inverse(rotatePartInitialRotation);
                rotatePart.position = muzzle.position + deltaRotation * rotatePartOffsetFromMuzzle;
            }
        }

        // ---------- Firing ----------
        private void Fire()
        {
            Debug.Log($"[Tower] '{name}' fires at {currentTarget.Transform.name} (HP={currentTarget.CurrentHealth})");
            if (projectilePrefab == null) return;

            Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;
            Projectile arrow = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            arrow.Launch(currentTarget, damage, projectileSpeed);
        }

        // Draws the firing range in the Scene view to help place towers / design levels
        private void OnDrawGizmosSelected()
        {
            // In Edit mode the CSV isn't loaded yet — show the fallback range so there's something to see
            float r = range > 0f ? range : (fallbackData != null ? fallbackData.range : 0f);
            if (r <= 0f) return;
            Gizmos.color = new Color(0.15f, 1f, 0.55f, 0.35f); // teal-green, Verdant's theme colour
            Gizmos.DrawWireSphere(transform.position, r);
        }
    }
}