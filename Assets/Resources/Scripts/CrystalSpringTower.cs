using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crystal Spring tower — every attack pulses damage to ALL enemies in range (AoE) and slows them.
/// It can also freeze them (intended for level 3, set freezeChance on that prefab).
///   Damage / Range / Atk Speed come from towers.csv (T2B rows); slow % and freeze are set per level on the prefab.
///   Upgradable / sellable through the shared system (implements IUpgradableTower).
/// </summary>
public class CrystalSpringTower : MonoBehaviour, IUpgradableTower
{
    [Header("CSV reference / upgrade")]
    [Tooltip("ID in towers.csv, e.g. T2B1")]
    [SerializeField] private string towerId = "T2B1";
    [Tooltip("Prefab for the next level (leave empty = max level)")]
    [SerializeField] private GameObject nextLevelPrefab;

    public string TowerId => towerId;
    public GameObject NextLevelPrefab => nextLevelPrefab;

    [Header("Detection")]
    [Tooltip("Enemy layer (leave empty = assume the \"Enemy\" layer)")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Slow (per level)")]
    [Range(0f, 1f)]
    [Tooltip("0.4 = 40% slower")]
    [SerializeField] private float slowPercent = 0.4f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Freeze (level 3 only)")]
    [Range(0f, 1f)]
    [Tooltip("0 = no freeze, 0.25 = 25% chance")]
    [SerializeField] private float freezeChance = 0f;
    [SerializeField] private float freezeDuration = 2f;

    [Header("VFX (optional)")]
    [SerializeField] private GameObject pulseVfxPrefab;   // effect spawned on each attack (can be empty)

    // Runtime stats (from the CSV)
    private float damage, range, fireCooldown;
    private float cooldownTimer;
    private bool _statsReady, _csvResolved;

    private ContactFilter2D _filter;
    private readonly List<Collider2D> _hits = new List<Collider2D>();

    void Awake()
    {
        if (enemyLayer.value == 0) enemyLayer = LayerMask.GetMask("Enemy");
        _filter = new ContactFilter2D();
        _filter.SetLayerMask(enemyLayer);
        _filter.useTriggers = true;
    }

    void Start() => TryLoadStats();

    // Reads damage/range/attack speed from TowerDatabase. Retried from Update until the database has loaded.
    void TryLoadStats()
    {
        if (TowerDatabase.Instance == null || !TowerDatabase.Instance.IsLoaded) return;
        _csvResolved = true;

        var rec = TowerDatabase.Instance.GetByID(towerId);
        if (rec == null)
        {
            Debug.LogError($"[CrystalSpring] ID '{towerId}' not found in towers.csv", this);
            return;
        }
        damage = rec.damage;
        range = rec.range;
        fireCooldown = rec.atkSpeed > 0f ? rec.atkSpeed : 1f;
        _statsReady = true;
    }

    void Update()
    {
        if (!_csvResolved) TryLoadStats();
        if (!_statsReady || range <= 0f) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            Fire();
            cooldownTimer = fireCooldown;
        }
    }

    // Damages and slows (and maybe freezes) every enemy within range.
    void Fire()
    {
        int count = Physics2D.OverlapCircle(transform.position, range, _filter, _hits);
        bool hitAny = false;

        for (int i = 0; i < count; i++)
        {
            var e = _hits[i].GetComponentInParent<EnemyBase>();
            if (e == null || e.IsDead) continue;
            hitAny = true;

            e.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)));
            if (e.IsDead) continue;   // killed by the damage — no need to apply a status

            e.ApplySlow(slowPercent, slowDuration);
            if (freezeChance > 0f && Random.value < freezeChance)
                e.ApplyFreeze(freezeDuration);
        }

        if (hitAny && pulseVfxPrefab != null)
            Destroy(Instantiate(pulseVfxPrefab, transform.position, pulseVfxPrefab.transform.rotation), 1.5f);
    }

    // Shows the attack range in the Scene view (2.5 before the CSV is loaded).
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range > 0f ? range : 2.5f);
    }
}
