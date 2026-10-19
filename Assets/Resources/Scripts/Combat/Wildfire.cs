using System.Collections.Generic;
using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// Wildfire tower — sets every enemy in range on fire (a burn / damage-over-time effect).
    ///   - An enemy that enters range is ignited; after BurnSpeedPer10Seconds seconds it takes
    ///     MaxHP x BurnHpPercent / round(BurnSpeedPer10Seconds) damage and its burn ends.
    ///   - If it is still in range it is ignited again straight away, so enemies keep burning while they stay near the tower.
    ///   - Several enemies can burn at once, each on its own timer.
    /// Range comes from towers.csv (via towerId); the burn values are set per level on the prefab.
    /// Shows an attack VFX on the tower while enemies are in range and a burn VFX on each burning enemy.
    /// Upgradable / sellable through the shared system (implements IUpgradableTower).
    /// </summary>
    public class Wildfire : MonoBehaviour, IUpgradableTower
    {
        [Header("Wildfire Settings")]
        [Range(0.1f, 1.0f)]
        [Tooltip("Total burn damage = the target's MaxHP x this value")]
        [SerializeField] private float BurnHpPercent = 1.0f;
        [Tooltip("Seconds per damage tick (also how long one burn lasts)")]
        [SerializeField] private float BurnSpeedPer10Seconds = 1.0f;
        [Tooltip("Enemy layer (leave empty = assume the \"Enemy\" layer)")]
        [SerializeField] private LayerMask EnemyLayer;
        [SerializeField] private string towerId;               // ID in towers.csv, e.g. T2A1
        [SerializeField] private GameObject NextUpgradePrefab; // next level's prefab (empty = max level)
        [Range(2.5f, 4.5f)]
        [Tooltip("Default value — overwritten by the Range from towers.csv if it can be loaded")]
        [SerializeField] private float AttackRange = 2.5f;

        [Header("VFX Settings")]
        [SerializeField] private GameObject VFXPrefab;   // burn effect attached to each burning enemy
        [SerializeField] private GameObject AttackVFX;   // effect on the tower, shown while enemies are in range

        public string TowerId => towerId;
        public GameObject NextLevelPrefab => NextUpgradePrefab;

        // Burn state per target — supports burning several enemies at once (AoE), each with its own timing
        private class BurnState
        {
            public float elapsed;      // how long it has been burning (the burn stops once this reaches the burn time)
            public float tickTimer;    // countdown to the next damage tick
            public GameObject vfx;
        }

        private readonly Dictionary<EnemyBase, BurnState> _burning = new Dictionary<EnemyBase, BurnState>();
        private readonly List<EnemyBase> _toRemove = new List<EnemyBase>();

        private ContactFilter2D _filter;
        private readonly List<Collider2D> _hits = new List<Collider2D>();
        private bool _statsReady, _csvResolved;
        private GameObject attackVFX;   // the spawned instance of AttackVFX

        void Awake()
        {
            if (EnemyLayer.value == 0) EnemyLayer = LayerMask.GetMask("Enemy");
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(EnemyLayer);
            _filter.useTriggers = true;
            attackVFX = Instantiate(AttackVFX,transform.position, AttackVFX.transform.rotation, transform); // the tower's attack VFX
        }

        void Start() => TryLoadStats();

        // Reads the Range from towers.csv by towerId (like CrystalSpringTower). The Burn % / speed values stay
        // as per-level settings on the prefab, because towers.csv has no columns for them.
        void TryLoadStats()
        {
            if (TowerDatabase.Instance == null || !TowerDatabase.Instance.IsLoaded) return;
            _csvResolved = true;

            var rec = TowerDatabase.Instance.GetByID(towerId);
            if (rec == null)
            {
                Debug.LogError($"[Wildfire] ID '{towerId}' not found in towers.csv — using the Inspector values instead", this);
                _statsReady = true;
                return;
            }
            if (rec.range > 0f) AttackRange = rec.range;
            _statsReady = true;
        }

        void Update()
        {
            if (!_csvResolved) TryLoadStats();   // retry until TowerDatabase has loaded

            FindAndIgniteEnemies();
            TickBurningEnemies();
        }

        // Ignites every enemy in range that isn't already burning, and toggles the tower's attack VFX.
        void FindAndIgniteEnemies()
        {
            int count = Physics2D.OverlapCircle(transform.position, AttackRange, _filter, _hits);
            for (int i = 0; i < count; i++)
            {
                var enemy = _hits[i].GetComponentInParent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                if (_burning.ContainsKey(enemy)) continue;   // already burning — don't re-ignite or reset its timer

                attackVFX.SetActive(true); // show the attack VFX when a target is in range
                IgniteEnemy(enemy);
            }
            if (_hits.Count == 0) attackVFX.SetActive(false); // hide the attack VFX when nothing is in range
            // Clear the hits list ready for the next frame
            _hits.Clear();
        }

        // Starts burning one enemy and attaches the burn VFX to it.
        void IgniteEnemy(EnemyBase enemy)
        {
            GameObject vfx = null;
            if (VFXPrefab != null)
            {
                vfx = Instantiate(VFXPrefab, enemy.transform.position, VFXPrefab.transform.rotation);
                vfx.transform.SetParent(enemy.transform);
                Destroy(vfx, BurnSpeedPer10Seconds); // remove the VFX when the burn time is up
            }

            _burning[enemy] = new BurnState
            {
                elapsed = 0f,
                tickTimer = BurnSpeedPer10Seconds,
                vfx = vfx,
            };
        }

        // Advances every burn: deals tick damage when a tick is due, and ends burns that are finished or whose enemy died.
        void TickBurningEnemies()
        {
            _toRemove.Clear();

            foreach (var kv in _burning)
            {
                var enemy = kv.Key;
                var state = kv.Value;

                if (enemy == null || enemy.IsDead)
                {
                    _toRemove.Add(enemy);
                    continue;
                }

                state.elapsed += Time.deltaTime;
                state.tickTimer -= Time.deltaTime;

                if (state.tickTimer <= 0f)
                {
                    state.tickTimer += BurnSpeedPer10Seconds;

                    int ticksTotal = Mathf.Max(1, Mathf.RoundToInt(BurnSpeedPer10Seconds));
                    int tickDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.MaxHP * BurnHpPercent / ticksTotal));
                    enemy.TakeDamage(tickDamage);
                }

                if (state.elapsed >= BurnSpeedPer10Seconds || enemy.IsDead)
                    _toRemove.Add(enemy);
            }

            // Remove finished burns after the loop (a Dictionary can't be changed while it's being iterated).
            foreach (var enemy in _toRemove)
            {
                if (_burning.TryGetValue(enemy, out var state) && state.vfx != null)
                    Destroy(state.vfx);
                _burning.Remove(enemy);
            }
        }

        // Shows the attack range in the Scene view.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, AttackRange);
        }
    }
}
