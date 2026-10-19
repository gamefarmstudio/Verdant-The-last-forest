using UnityEngine;
using DG.Tweening;

/// <summary>
/// Melee tower (Forest Knight, T1A) — summons one KnightUnit that stands on the enemy path and
/// blocks enemies; when the knight dies a new one is summoned after respawnDelay.
/// Put it on the Forest Knight tower prefab instead of (or as well as) the ranged Tower.
/// Upgradable / sellable like the ranged towers (implements IUpgradableTower).
/// </summary>
public class MeleeTower : MonoBehaviour, IUpgradableTower   // upgradable/sellable like the ranged towers
{
    // Upgrade system — the knight's stats come from towers.csv via towerId; upgrading swaps the prefab.
    [Header("Upgrade")]
    [Tooltip("ID in towers.csv, e.g. T1A1 — the knight's HP/Damage/Atk Speed/Range are loaded from this row")]
    [SerializeField] private string towerId = "T1A1";
    [Tooltip("Prefab for the next melee tower level (leave empty = max level)")]
    [SerializeField] private GameObject nextLevelPrefab;

    public string TowerId => towerId;
    public GameObject NextLevelPrefab => nextLevelPrefab;

    [Header("Summon")]
    [SerializeField] private KnightUnit knightPrefab;
    // An offset from the tower (was a Transform, but that can't be assigned on a prefab -> "Type mismatch").
    [Tooltip("Adjusts the base position before it snaps onto the path (usually (0,0) is fine)")]
    [SerializeField] private Vector2 rallyOffset = Vector2.zero;
    // Snap the knight's standing point onto "the point on the EnemyPath closest to the tower" automatically
    //  -> wherever the tower is placed, the knight stands on the path; no per-slot setup needed.
    [Tooltip("Automatically keep the knight's standing point on the enemy path nearest the tower")]
    [SerializeField] private bool snapRallyToPath = true;
    // Shift the standing point along the path: + = toward the base (intercept further ahead), - = toward the enemy spawn (world units).
    [Tooltip("Move the knight along the path: + toward the base / - toward the enemy spawn")]
    [SerializeField] private float pathShift = 0f;
    [SerializeField] private float respawnDelay = 5f;   // seconds before a new knight appears after one dies

    private KnightUnit _knight;
    private float _respawnTimer;
    [HideInInspector] public int _Health;   // current knight HP, readable by TowerSlot / the upgrade popup

    void Start() => Summon();

    void Update()
    {
        _Health = _knight != null ? _knight._hp : 0;   // keep the knight's HP readable by TowerSlot / the upgrade popup
        if (_knight != null) return;   // knight still alive (it becomes null once destroyed)

        _respawnTimer -= Time.deltaTime;
        if (_respawnTimer <= 0f) Summon();
    }

    // Spawns a knight at the rally point and configures it from towers.csv.
    void Summon()
    {
        if (knightPrefab == null)
        {
            Debug.LogError("[MeleeTower] Knight Prefab has not been set", this);
            return;
        }

        Vector3 pos = RallyPosition();
        _knight = Instantiate(knightPrefab, pos, Quaternion.identity);

        // Load the knight's stats from towers.csv using this tower's towerId (this level).
        var rec = TowerDatabase.Instance != null ? TowerDatabase.Instance.GetByID(towerId) : null;
        if (rec != null)
            _knight.Configure( rec.hp, Mathf.RoundToInt(rec.damage), rec.atkSpeed, rec.range);

        _knight.Init(pos);
        _knight.OnDeath += HandleKnightDeath;
    }

    // Works out where the knight stands — offset from the tower, then (optionally) snapped onto the nearest path.
    Vector3 RallyPosition()
    {
        Vector3 basePos = transform.position + (Vector3)rallyOffset;
        if (snapRallyToPath)
        {
            var path = FindAnyObjectByType<EnemyPath>();
            if (path != null && path.IsValid) return path.PointAlongPath(basePos, pathShift);
        }
        return basePos;
    }

    void HandleKnightDeath()
    {
        _knight = null;
        _respawnTimer = respawnDelay;
    }

    // The tower was destroyed (e.g. sold) -> destroy its knight too, so it doesn't keep walking around.
    void OnDestroy()
    {
        if (_knight != null) Destroy(_knight.gameObject);
    }

    // Shows the knight's rally point in the Scene view.
    void OnDrawGizmosSelected()
    {
        Vector3 p = RallyPosition();
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireSphere(p, 0.3f);
    }
}
